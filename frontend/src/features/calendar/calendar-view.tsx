import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { addDays, addMonths, eachDayOfInterval, endOfMonth, endOfWeek, format, isSameDay, isSameMonth, startOfDay, startOfMonth, startOfWeek } from "date-fns";
import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Drawer } from "../../components/design-system/drawer.tsx";
import { Field, Input, Select, Textarea } from "../../components/design-system/field.tsx";
import { EmptyState, ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { ApiError } from "../../services/api-client.ts";
import { cn } from "../../utils/cn.ts";
import { safeHref } from "../../utils/safe-url.ts";
import { fetchWorkOptions, fromDateTimeInput, toDateTimeInput } from "../work/api.ts";
import { calendarViews, eventKinds, kindClassName, scheduleRecurrences, type CalendarView } from "../schedule/catalog.ts";
import { deleteEvent, fetchCalendar, fetchEvent, saveEvent, type CalendarEntry, type CalendarEventWrite } from "../schedule/api.ts";

type Draft = {
  title: string;
  description: string;
  kind: string;
  startAt: string;
  endAt: string;
  isAllDay: boolean;
  location: string;
  meetingLink: string;
  projectId: string;
  recurrence: string;
  recurrenceInterval: string;
  participantIds: string[];
  remindAt: string;
};

function blankDraft(start: Date): Draft {
  const end = new Date(start.getTime() + 60 * 60 * 1000);
  return {
    title: "",
    description: "",
    kind: "Personal",
    startAt: toDateTimeInput(start.toISOString()),
    endAt: toDateTimeInput(end.toISOString()),
    isAllDay: false,
    location: "",
    meetingLink: "",
    projectId: "",
    recurrence: "None",
    recurrenceInterval: "1",
    participantIds: [],
    remindAt: "",
  };
}

function rangeFor(view: CalendarView, cursor: Date) {
  if (view === "day") {
    const from = startOfDay(cursor);
    return { from, to: addDays(from, 1), label: format(from, "EEEE, d MMMM yyyy") };
  }

  if (view === "week") {
    const from = startOfWeek(cursor, { weekStartsOn: 1 });
    return { from, to: addDays(from, 7), label: `${format(from, "d MMM")} – ${format(addDays(from, 6), "d MMM yyyy")}` };
  }

  if (view === "agenda") {
    const from = startOfDay(cursor);
    return { from, to: addDays(from, 14), label: "Next 14 days" };
  }

  const from = startOfWeek(startOfMonth(cursor), { weekStartsOn: 1 });
  const to = addDays(endOfWeek(endOfMonth(cursor), { weekStartsOn: 1 }), 1);
  return { from, to, label: format(cursor, "MMMM yyyy") };
}

function dateFromParam(value: string | null) {
  if (!value) {
    return null;
  }
  const [year, month, day] = value.split("-").map(Number);
  if (!year || !month || !day) {
    return null;
  }
  return new Date(year, month - 1, day);
}

function covers(entry: CalendarEntry, day: Date) {
  const start = new Date(entry.startAt);
  const end = new Date(entry.endAt);
  const dayEnd = addDays(startOfDay(day), 1);
  return start < dayEnd && end > startOfDay(day);
}

export function CalendarView() {
  const { user } = useAuth();
  const manageable = can(user?.permissions, permissions.calendarManage);
  const [params] = useSearchParams();
  const requestedEvent = params.get("event");
  const [openedEvent, setOpenedEvent] = useState<string | null>(null);
  const [view, setView] = useState<CalendarView>("month");
  const [cursor, setCursor] = useState(() => dateFromParam(params.get("date")) ?? new Date());
  const [creating, setCreating] = useState(false);
  const [selected, setSelected] = useState<CalendarEntry | null>(null);
  const [draft, setDraft] = useState<Draft>(() => blankDraft(new Date()));
  const [message, setMessage] = useState<string | null>(null);
  const queryClient = useQueryClient();
  const range = useMemo(() => rangeFor(view, cursor), [view, cursor]);
  const events = useQuery({
    queryKey: ["calendar", range.from.toISOString(), range.to.toISOString()],
    queryFn: () => fetchCalendar(range.from, range.to),
  });
  const options = useQuery({ queryKey: ["work-options"], queryFn: fetchWorkOptions });
  const detail = useQuery({
    queryKey: ["calendar-event", selected?.originId],
    queryFn: () => fetchEvent(selected!.originId),
    enabled: selected?.origin === "event" && !creating,
  });

  useEffect(() => {
    document.title = "Calendar · Worknexa";
  }, []);

  const requestedDate = dateFromParam(params.get("date"));
  const [seenDate, setSeenDate] = useState(params.get("date"));
  if (requestedDate && seenDate !== params.get("date")) {
    setSeenDate(params.get("date"));
    setCursor(requestedDate);
  }
  if (requestedEvent && openedEvent !== requestedEvent && events.data) {
    const entry = events.data.find((item) => item.origin === "event" && item.originId === requestedEvent);
    if (entry) {
      setOpenedEvent(requestedEvent);
      setCreating(false);
      setSelected(entry);
    }
  }

  useEffect(() => {
    if (!detail.data || creating) {
      return;
    }

    setDraft({
      title: detail.data.title,
      description: detail.data.description ?? "",
      kind: detail.data.kind,
      startAt: toDateTimeInput(detail.data.startAt),
      endAt: toDateTimeInput(detail.data.endAt),
      isAllDay: detail.data.isAllDay,
      location: detail.data.location ?? "",
      meetingLink: detail.data.meetingLink ?? "",
      projectId: detail.data.projectId ?? "",
      recurrence: detail.data.recurrence,
      recurrenceInterval: String(detail.data.recurrenceInterval ?? 1),
      participantIds: detail.data.participants.map((person) => person.id),
      remindAt: toDateTimeInput(detail.data.remindAt),
    });
  }, [detail.data, creating]);

  function refresh() {
    return Promise.all([
      queryClient.invalidateQueries({ queryKey: ["calendar"] }),
      queryClient.invalidateQueries({ queryKey: ["calendar-event"] }),
      queryClient.invalidateQueries({ queryKey: ["reminders"] }),
      queryClient.invalidateQueries({ queryKey: ["notifications"] }),
      queryClient.invalidateQueries({ queryKey: ["my-day"] }),
    ]);
  }

  const save = useMutation({
    mutationFn: (input: { id?: string; event: CalendarEventWrite }) => saveEvent(input.event, input.id),
    onSuccess: async () => {
      setMessage(null);
      setCreating(false);
      setSelected(null);
      await refresh();
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The event could not be saved."),
  });

  const remove = useMutation({
    mutationFn: deleteEvent,
    onSuccess: async () => {
      setSelected(null);
      setCreating(false);
      await refresh();
    },
  });

  function openCreate() {
    setDraft(blankDraft(cursor));
    setCreating(true);
    setSelected(null);
    setMessage(null);
  }

  function openEntry(entry: CalendarEntry) {
    setCreating(false);
    setSelected(entry);
    setMessage(null);
  }

  function shift(direction: number) {
    setCursor((current) => {
      if (view === "month") {
        return addMonths(current, direction);
      }

      if (view === "week") {
        return addDays(current, direction * 7);
      }

      if (view === "agenda") {
        return addDays(current, direction * 14);
      }

      return addDays(current, direction);
    });
  }

  async function onSave(event: FormEvent) {
    event.preventDefault();
    const interval = Number(draft.recurrenceInterval);
    await save.mutateAsync({
      id: creating ? undefined : selected?.origin === "event" ? selected.originId : undefined,
      event: {
        title: draft.title,
        description: draft.description || null,
        kind: draft.kind,
        startAt: fromDateTimeInput(draft.startAt) ?? new Date().toISOString(),
        endAt: fromDateTimeInput(draft.endAt) ?? new Date().toISOString(),
        isAllDay: draft.isAllDay,
        location: draft.location || null,
        meetingLink: draft.meetingLink || null,
        projectId: draft.projectId || null,
        recurrence: draft.recurrence,
        recurrenceInterval: draft.recurrence === "Custom" ? interval : null,
        participantIds: draft.participantIds,
        remindAt: fromDateTimeInput(draft.remindAt),
      },
    });
  }

  if (!can(user?.permissions, permissions.calendarRead)) {
    return <ErrorState title="Access denied" description="Your account does not include the calendar." />;
  }

  if (events.isPending) {
    return <LoadingState label="Loading calendar" />;
  }

  if (events.isError || !events.data) {
    const description = events.error instanceof ApiError ? events.error.message : "The calendar could not be loaded.";
    return <ErrorState title="Calendar is unavailable" description={description} onRetry={() => void events.refetch()} />;
  }

  const rows = events.data;
  const drawerOpen = creating || Boolean(selected);
  const readOnly = Boolean(selected && selected.origin !== "event");
  const canEdit = manageable && (creating || Boolean(detail.data?.editable));

  return (
    <div className="mx-auto max-w-6xl space-y-5">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Calendar</h1>
          <p className="mt-1 text-sm text-muted">Meetings, tasks, personal time, learning, and deadlines in one place.</p>
        </div>
        {manageable ? <Button onClick={openCreate}>New event</Button> : null}
      </header>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap gap-2" role="tablist" aria-label="Calendar views">
          {calendarViews.map((item) => (
            <Button key={item.id} variant={view === item.id ? "primary" : "secondary"} size="sm" aria-pressed={view === item.id} onClick={() => setView(item.id)}>
              {item.label}
            </Button>
          ))}
        </div>
        <div className="flex items-center gap-2">
          <Button variant="secondary" size="sm" onClick={() => shift(-1)} aria-label="Previous">Previous</Button>
          <Button variant="ghost" size="sm" onClick={() => setCursor(new Date())}>Today</Button>
          <Button variant="secondary" size="sm" onClick={() => shift(1)} aria-label="Next">Next</Button>
          <p className="min-w-40 text-sm font-medium">{range.label}</p>
        </div>
      </div>

      <div className="flex flex-wrap gap-2" aria-label="Event kinds">
        {eventKinds.map((kind) => (
          <span key={kind} className={cn("rounded-md px-2 py-1 text-[11px] font-medium", kindClassName(kind))}>{kind}</span>
        ))}
      </div>

      {rows.length === 0 && view === "agenda" ? (
        <EmptyState title="Nothing scheduled" description="Events, meetings, and dated tasks show up here." action={manageable ? <Button onClick={openCreate}>New event</Button> : undefined} />
      ) : view === "month" ? (
        <MonthGrid cursor={cursor} rows={rows} onOpen={openEntry} />
      ) : view === "agenda" ? (
        <Agenda rows={rows} onOpen={openEntry} />
      ) : (
        <DayColumns from={range.from} days={view === "day" ? 1 : 7} rows={rows} onOpen={openEntry} />
      )}

      <Drawer open={drawerOpen} title={creating ? "New event" : selected?.title || "Event"} onClose={() => { setCreating(false); setSelected(null); }} className="max-w-lg">
        {readOnly && selected ? (
          <ReadOnlyEntry entry={selected} />
        ) : (
          <form className="space-y-4" onSubmit={(event) => void onSave(event)}>
            {message ? <p className="text-sm text-danger">{message}</p> : null}
            <Field label="Title">{({ id }) => <Input id={id} value={draft.title} onChange={(event) => setDraft({ ...draft, title: event.target.value })} required maxLength={300} disabled={!canEdit} />}</Field>
            <Field label="Description">{({ id }) => <Textarea id={id} value={draft.description} onChange={(event) => setDraft({ ...draft, description: event.target.value })} disabled={!canEdit} />}</Field>
            <div className="grid grid-cols-2 gap-3">
              <Field label="Kind">{({ id }) => <Select id={id} value={draft.kind} onChange={(event) => setDraft({ ...draft, kind: event.target.value })} disabled={!canEdit}>{eventKinds.map((kind) => <option key={kind} value={kind}>{kind}</option>)}</Select>}</Field>
              <Field label="Project">{({ id }) => <Select id={id} value={draft.projectId} onChange={(event) => setDraft({ ...draft, projectId: event.target.value })} disabled={!canEdit}><option value="">No project</option>{options.data?.projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}</Select>}</Field>
              <Field label="Start">{({ id }) => <Input id={id} type="datetime-local" value={draft.startAt} onChange={(event) => setDraft({ ...draft, startAt: event.target.value })} required disabled={!canEdit} />}</Field>
              <Field label="End">{({ id }) => <Input id={id} type="datetime-local" value={draft.endAt} onChange={(event) => setDraft({ ...draft, endAt: event.target.value })} required disabled={!canEdit} />}</Field>
              <Field label="Location">{({ id }) => <Input id={id} value={draft.location} onChange={(event) => setDraft({ ...draft, location: event.target.value })} maxLength={300} disabled={!canEdit} />}</Field>
              <Field label="Meeting link">{({ id }) => <Input id={id} value={draft.meetingLink} onChange={(event) => setDraft({ ...draft, meetingLink: event.target.value })} maxLength={500} disabled={!canEdit} />}</Field>
              <Field label="Repeats">{({ id }) => <Select id={id} value={draft.recurrence} onChange={(event) => setDraft({ ...draft, recurrence: event.target.value })} disabled={!canEdit}>{scheduleRecurrences.map((item) => <option key={item} value={item}>{item}</option>)}</Select>}</Field>
              <Field label="Reminder">{({ id }) => <Input id={id} type="datetime-local" value={draft.remindAt} onChange={(event) => setDraft({ ...draft, remindAt: event.target.value })} disabled={!canEdit} />}</Field>
            </div>
            {draft.recurrence === "Custom" ? (
              <Field label="Every" hint="Number of days between repeats.">{({ id }) => <Input id={id} type="number" min={1} max={366} value={draft.recurrenceInterval} onChange={(event) => setDraft({ ...draft, recurrenceInterval: event.target.value })} disabled={!canEdit} />}</Field>
            ) : null}
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={draft.isAllDay} onChange={(event) => setDraft({ ...draft, isAllDay: event.target.checked })} disabled={!canEdit} />
              All day
            </label>
            <fieldset className="space-y-2">
              <legend className="text-[13px] font-medium">Participants</legend>
              <div className="max-h-32 space-y-1 overflow-y-auto">
                {(options.data?.people ?? []).map((person) => (
                  <label key={person.id} className="flex items-center gap-2 text-sm">
                    <input
                      type="checkbox"
                      checked={draft.participantIds.includes(person.id)}
                      disabled={!canEdit}
                      onChange={(event) => setDraft({
                        ...draft,
                        participantIds: event.target.checked
                          ? [...draft.participantIds, person.id]
                          : draft.participantIds.filter((id) => id !== person.id),
                      })}
                    />
                    {person.name}
                  </label>
                ))}
              </div>
            </fieldset>
            {canEdit ? <Button type="submit" disabled={save.isPending}>{creating ? "Create event" : "Save changes"}</Button> : null}
            {canEdit && !creating && selected ? <Button type="button" variant="danger" onClick={() => remove.mutate(selected.originId)}>Delete</Button> : null}
          </form>
        )}
      </Drawer>
    </div>
  );
}

function EventChip({ entry, onOpen }: { entry: CalendarEntry; onOpen: (entry: CalendarEntry) => void }) {
  return (
    <button type="button" className={cn("block w-full truncate rounded px-1.5 py-0.5 text-left text-[11px] font-medium", kindClassName(entry.kind))} onClick={() => onOpen(entry)}>
      {entry.isAllDay ? entry.title : `${format(new Date(entry.startAt), "HH:mm")} ${entry.title}`}
    </button>
  );
}

function MonthGrid({ cursor, rows, onOpen }: { cursor: Date; rows: CalendarEntry[]; onOpen: (entry: CalendarEntry) => void }) {
  const days = eachDayOfInterval({
    start: startOfWeek(startOfMonth(cursor), { weekStartsOn: 1 }),
    end: endOfWeek(endOfMonth(cursor), { weekStartsOn: 1 }),
  });

  return (
    <div className="overflow-x-auto rounded-xl border border-line bg-surface">
      <div className="grid min-w-[720px] grid-cols-7 border-b border-line text-[11px] font-medium text-muted">
        {["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map((label) => (
          <div key={label} className="px-2 py-2">{label}</div>
        ))}
      </div>
      <div className="grid min-w-[720px] grid-cols-7">
        {days.map((day) => {
          const items = rows.filter((entry) => covers(entry, day));
          return (
            <div key={day.toISOString()} className={cn("min-h-28 border-r border-t border-line p-1.5", isSameMonth(day, cursor) ? "" : "bg-canvas text-muted")}>
              <p className={cn("mb-1 text-xs", isSameDay(day, new Date()) ? "font-semibold text-accent" : "")}>{format(day, "d")}</p>
              <div className="space-y-1">
                {items.slice(0, 3).map((entry) => <EventChip key={`${entry.origin}-${entry.originId}-${entry.startAt}`} entry={entry} onOpen={onOpen} />)}
                {items.length > 3 ? <p className="text-[11px] text-muted">+{items.length - 3} more</p> : null}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function DayColumns({ from, days, rows, onOpen }: { from: Date; days: number; rows: CalendarEntry[]; onOpen: (entry: CalendarEntry) => void }) {
  const columns = Array.from({ length: days }, (_, index) => addDays(from, index));
  return (
    <div className={cn("grid gap-3", days === 1 ? "grid-cols-1" : "md:grid-cols-7")}>
      {columns.map((day) => {
        const items = rows.filter((entry) => covers(entry, day)).sort((left, right) => left.startAt.localeCompare(right.startAt));
        return (
          <section key={day.toISOString()} className="rounded-xl border border-line bg-surface p-3">
            <h2 className="text-sm font-semibold">{format(day, days === 1 ? "EEEE, d MMMM" : "EEE d")}</h2>
            <div className="mt-3 space-y-2">
              {items.length === 0 ? <p className="text-xs text-muted">Nothing scheduled.</p> : items.map((entry) => <EventChip key={`${entry.origin}-${entry.originId}-${entry.startAt}`} entry={entry} onOpen={onOpen} />)}
            </div>
          </section>
        );
      })}
    </div>
  );
}

function Agenda({ rows, onOpen }: { rows: CalendarEntry[]; onOpen: (entry: CalendarEntry) => void }) {
  const groups = new Map<string, CalendarEntry[]>();
  for (const entry of rows) {
    const key = format(new Date(entry.startAt), "yyyy-MM-dd");
    groups.set(key, [...(groups.get(key) ?? []), entry]);
  }

  return (
    <div className="space-y-4">
      {[...groups.entries()].map(([key, items]) => (
        <section key={key}>
          <h2 className="text-sm font-semibold">{format(new Date(items[0].startAt), "EEEE, d MMMM")}</h2>
          <ul className="mt-2 space-y-2">
            {items.map((entry) => (
              <li key={`${entry.origin}-${entry.originId}-${entry.startAt}`}>
                <button type="button" className="flex w-full items-center justify-between gap-3 rounded-xl border border-line bg-surface px-3 py-2 text-left" onClick={() => onOpen(entry)}>
                  <span>
                    <span className="block text-sm font-medium">{entry.title}</span>
                    <span className="text-xs text-muted">{entry.isAllDay ? "All day" : `${format(new Date(entry.startAt), "HH:mm")} – ${format(new Date(entry.endAt), "HH:mm")}`}</span>
                  </span>
                  <span className={cn("rounded-md px-1.5 py-0.5 text-[11px] font-medium", kindClassName(entry.kind))}>{entry.kind}</span>
                </button>
              </li>
            ))}
          </ul>
        </section>
      ))}
    </div>
  );
}

function ReadOnlyEntry({ entry }: { entry: CalendarEntry }) {
  return (
    <div className="space-y-3 text-sm">
      <span className={cn("inline-flex rounded-md px-1.5 py-0.5 text-[11px] font-medium", kindClassName(entry.kind))}>{entry.kind}</span>
      <p>{entry.isAllDay ? "All day" : `${format(new Date(entry.startAt), "d MMM, HH:mm")} – ${format(new Date(entry.endAt), "d MMM, HH:mm")}`}</p>
      {entry.description ? <p className="text-muted">{entry.description}</p> : null}
      {entry.location ? <p>Location: {entry.location}</p> : null}
      {safeHref(entry.meetingLink) ? <a className="text-accent underline" href={safeHref(entry.meetingLink)} target="_blank" rel="noreferrer">Join</a> : null}
      {entry.projectName ? <p>Project: {entry.projectName}</p> : null}
      {entry.origin === "task" ? <p className="text-muted">This comes from a task.</p> : <Link className="text-accent underline" to={`/meetings?meeting=${entry.originId}`}>Open this meeting</Link>}
    </div>
  );
}
