import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { addDays, addMonths, eachDayOfInterval, endOfMonth, endOfWeek, format, isSameMonth, startOfMonth, startOfWeek } from "date-fns";
import { useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Drawer } from "../../components/design-system/drawer.tsx";
import { Field, Input, Select, Textarea } from "../../components/design-system/field.tsx";
import { EmptyState, ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { ApiError } from "../../services/api-client.ts";
import { cn } from "../../utils/cn.ts";
import { fetchWorkOptions, toDateTimeInput } from "../work/api.ts";
import { deleteMeeting, fetchMeeting, fetchMeetings, localDateTime, saveMeeting, type MeetingActionWrite, type MeetingDetail, type MeetingListItem, type MeetingWrite } from "./api.ts";
import { meetingTypeLabel, meetingTypes, meetingViews, type MeetingView } from "./catalog.ts";

type ActionDraft = { id?: string; title: string; assigneeId: string; dueAt: string; isCompleted: boolean };

type Draft = {
  title: string;
  meetingType: string;
  startAt: string;
  durationMinutes: string;
  projectId: string;
  teamId: string;
  location: string;
  meetingLink: string;
  agenda: string;
  notes: string;
  decisions: string;
  followUpAt: string;
  participantIds: string[];
  actionItems: ActionDraft[];
};

function blankDraft(start = new Date()): Draft {
  return {
    title: "",
    meetingType: "Team",
    startAt: toDateTimeInput(start.toISOString()),
    durationMinutes: "30",
    projectId: "",
    teamId: "",
    location: "",
    meetingLink: "",
    agenda: "",
    notes: "",
    decisions: "",
    followUpAt: "",
    participantIds: [],
    actionItems: [],
  };
}

function draftFrom(meeting: MeetingDetail): Draft {
  return {
    title: meeting.title,
    meetingType: meeting.meetingType,
    startAt: toDateTimeInput(meeting.startAt),
    durationMinutes: String(meeting.durationMinutes),
    projectId: meeting.projectId ?? "",
    teamId: meeting.teamId ?? "",
    location: meeting.location ?? "",
    meetingLink: meeting.meetingLink ?? "",
    agenda: meeting.agenda ?? "",
    notes: meeting.notes ?? "",
    decisions: meeting.decisions ?? "",
    followUpAt: toDateTimeInput(meeting.followUpAt),
    participantIds: meeting.participants.map((person) => person.id),
    actionItems: meeting.actionItems.map((item) => ({
      id: item.id,
      title: item.title,
      assigneeId: item.assigneeId ?? "",
      dueAt: toDateTimeInput(item.dueAt),
      isCompleted: item.isCompleted,
    })),
  };
}

function toWrite(draft: Draft): MeetingWrite {
  const actions: MeetingActionWrite[] = draft.actionItems
    .filter((item) => item.title.trim())
    .map((item) => ({
      id: item.id ?? null,
      title: item.title.trim(),
      assigneeId: item.assigneeId || null,
      dueAt: localDateTime(item.dueAt),
      isCompleted: item.isCompleted,
    }));
  return {
    title: draft.title.trim(),
    meetingType: draft.meetingType,
    startAt: localDateTime(draft.startAt) ?? new Date().toISOString(),
    durationMinutes: Number(draft.durationMinutes) || 30,
    projectId: draft.projectId || null,
    teamId: draft.teamId || null,
    location: draft.location.trim() || null,
    meetingLink: draft.meetingLink.trim() || null,
    agenda: draft.agenda.trim() || null,
    notes: draft.notes.trim() || null,
    decisions: draft.decisions.trim() || null,
    followUpAt: localDateTime(draft.followUpAt),
    participantIds: draft.participantIds,
    actionItems: actions,
  };
}

export function MeetingsView() {
  const { user } = useAuth();
  const manageable = can(user?.permissions, permissions.meetingsManage);
  const [params, setParams] = useSearchParams();
  const [view, setView] = useState<MeetingView>("today");
  const [cursor, setCursor] = useState(() => new Date());
  const [creating, setCreating] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(params.get("meeting"));
  const [draft, setDraft] = useState<Draft>(() => blankDraft());
  const [message, setMessage] = useState<string | null>(null);
  const queryClient = useQueryClient();
  const date = format(cursor, "yyyy-MM-dd");
  const meetings = useQuery({ queryKey: ["meetings", view, date], queryFn: () => fetchMeetings(view, date), placeholderData: keepPreviousData });
  const options = useQuery({ queryKey: ["work-options"], queryFn: fetchWorkOptions });
  const detail = useQuery({
    queryKey: ["meeting", selectedId],
    queryFn: () => fetchMeeting(selectedId!),
    enabled: Boolean(selectedId) && !creating,
  });

  useEffect(() => {
    document.title = "Meetings · Worknexa";
  }, []);

  useEffect(() => {
    const requested = params.get("meeting");
    if (requested) {
      setSelectedId(requested);
      setCreating(false);
    }
  }, [params]);

  useEffect(() => {
    if (!detail.data || creating) {
      return;
    }

    setDraft(draftFrom(detail.data));
  }, [detail.data, creating]);

  const save = useMutation({
    mutationFn: (input: { id?: string; meeting: MeetingWrite }) => saveMeeting(input.meeting, input.id),
    onSuccess: async (saved) => {
      setMessage(null);
      setCreating(false);
      setSelectedId(saved.id);
      setParams(saved.id ? { meeting: saved.id } : {});
      await queryClient.invalidateQueries({ queryKey: ["meetings"] });
      await queryClient.invalidateQueries({ queryKey: ["meeting", saved.id] });
      await queryClient.invalidateQueries({ queryKey: ["calendar"] });
      await queryClient.invalidateQueries({ queryKey: ["standup"] });
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The meeting could not be saved."),
  });

  const remove = useMutation({
    mutationFn: deleteMeeting,
    onSuccess: async () => {
      setCreating(false);
      setSelectedId(null);
      setParams({});
      await queryClient.invalidateQueries({ queryKey: ["meetings"] });
      await queryClient.invalidateQueries({ queryKey: ["calendar"] });
    },
  });

  function patch(partial: Partial<Draft>) {
    setDraft((current) => ({ ...current, ...partial }));
  }

  function shift(direction: number) {
    setCursor((current) => (view === "calendar" ? addMonths(current, direction) : addDays(current, direction)));
  }

  if (!can(user?.permissions, permissions.meetingsRead)) {
    return <ErrorState title="Access denied" description="Your account does not include meetings." />;
  }

  if (meetings.isPending && !meetings.data) {
    return <LoadingState label="Loading meetings" />;
  }

  if ((meetings.isError && !meetings.data) || !meetings.data) {
    const description = meetings.error instanceof ApiError ? meetings.error.message : "Meetings could not be loaded.";
    return <ErrorState title="Meetings are unavailable" description={description} onRetry={() => void meetings.refetch()} />;
  }

  const editable = creating ? manageable : Boolean(detail.data?.editable && manageable);
  const drawerOpen = creating || Boolean(selectedId);

  return (
    <div className="mx-auto max-w-5xl space-y-5">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Meetings</h1>
          <p className="mt-1 text-sm text-muted">{view === "calendar" ? format(cursor, "MMMM yyyy") : format(cursor, "EEEE, d MMMM yyyy")}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button type="button" variant="secondary" size="sm" onClick={() => shift(-1)}>Previous</Button>
          <Button type="button" variant="secondary" size="sm" onClick={() => setCursor(new Date())}>Today</Button>
          <Button type="button" variant="secondary" size="sm" onClick={() => shift(1)}>Next</Button>
          {manageable ? <Button type="button" onClick={() => { setCreating(true); setSelectedId(null); setDraft(blankDraft(cursor)); setMessage(null); setParams({}); }}>New meeting</Button> : null}
        </div>
      </header>

      <div className="flex flex-wrap gap-2" role="tablist" aria-label="Meeting views">
        {meetingViews.map((item) => (
          <Button key={item.id} type="button" size="sm" variant={view === item.id ? "primary" : "secondary"} onClick={() => setView(item.id)}>{item.label}</Button>
        ))}
      </div>

      {meetings.data.length === 0 ? (
        <EmptyState title="Nothing on this view" description="Switch the date or add a meeting with an agenda, decisions, and follow-up." />
      ) : view === "calendar" ? (
        <MeetingMonth cursor={cursor} meetings={meetings.data} onOpen={(id) => { setCreating(false); setSelectedId(id); setParams({ meeting: id }); }} />
      ) : (
        <ul className="space-y-2">
          {meetings.data.map((meeting) => (
            <li key={meeting.id}>
              <button type="button" className="flex w-full flex-wrap items-center justify-between gap-3 rounded-xl border border-line bg-surface px-3 py-2 text-left" onClick={() => { setCreating(false); setSelectedId(meeting.id); setParams({ meeting: meeting.id }); }}>
                <span>
                  <span className="block text-sm font-medium">{meeting.title}</span>
                  <span className="text-xs text-muted">{format(new Date(meeting.startAt), "d MMM, HH:mm")} · {meeting.durationMinutes} min{meeting.location ? ` · ${meeting.location}` : ""}</span>
                </span>
                <span className="rounded-md bg-accent-soft px-1.5 py-0.5 text-[11px] font-medium text-accent">{meetingTypeLabel(meeting.meetingType)}</span>
              </button>
            </li>
          ))}
        </ul>
      )}

      <Drawer open={drawerOpen} title={creating ? "New meeting" : draft.title || "Meeting"} onClose={() => { setCreating(false); setSelectedId(null); setParams({}); }} className="max-w-lg">
        <form
          className="space-y-4"
          onSubmit={(event) => {
            event.preventDefault();
            void save.mutateAsync({ id: creating ? undefined : selectedId ?? undefined, meeting: toWrite(draft) });
          }}
        >
          {message ? <p className="text-sm text-danger">{message}</p> : null}
          {selectedId && !creating ? <Link className="text-sm text-accent" to={`/meeting-notes?meeting=${selectedId}`}>Open meeting notes</Link> : null}
          <Field label="Title">{({ id }) => <Input id={id} value={draft.title} onChange={(event) => patch({ title: event.target.value })} required maxLength={300} disabled={!editable} />}</Field>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Type">{({ id }) => <Select id={id} value={draft.meetingType} onChange={(event) => patch({ meetingType: event.target.value })} disabled={!editable}>{meetingTypes.map((item) => <option key={item.id} value={item.id}>{item.label}</option>)}</Select>}</Field>
            <Field label="Duration" hint="Minutes">{({ id }) => <Input id={id} type="number" min={5} max={1440} value={draft.durationMinutes} onChange={(event) => patch({ durationMinutes: event.target.value })} required disabled={!editable} />}</Field>
            <Field label="Date and time">{({ id }) => <Input id={id} type="datetime-local" value={draft.startAt} onChange={(event) => patch({ startAt: event.target.value })} required disabled={!editable} />}</Field>
            <Field label="Follow-up">{({ id }) => <Input id={id} type="datetime-local" value={draft.followUpAt} onChange={(event) => patch({ followUpAt: event.target.value })} disabled={!editable} />}</Field>
            <Field label="Project">{({ id }) => <Select id={id} value={draft.projectId} onChange={(event) => patch({ projectId: event.target.value })} disabled={!editable}><option value="">None</option>{options.data?.projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}</Select>}</Field>
            <Field label="Team">{({ id }) => <Select id={id} value={draft.teamId} onChange={(event) => patch({ teamId: event.target.value })} disabled={!editable}><option value="">None</option>{options.data?.teams.map((team) => <option key={team.id} value={team.id}>{team.name}</option>)}</Select>}</Field>
          </div>
          <Field label="Location">{({ id }) => <Input id={id} value={draft.location} onChange={(event) => patch({ location: event.target.value })} maxLength={300} disabled={!editable} />}</Field>
          <Field label="Meeting link">{({ id }) => <Input id={id} value={draft.meetingLink} onChange={(event) => patch({ meetingLink: event.target.value })} maxLength={500} disabled={!editable} />}</Field>
          <Field label="Participants">{({ id }) => (
            <select id={id} multiple className="min-h-28 w-full rounded-lg border border-line bg-surface px-3 py-2 text-sm" value={draft.participantIds} onChange={(event) => patch({ participantIds: [...event.target.selectedOptions].map((option) => option.value) })} disabled={!editable}>
              {options.data?.people.map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}
            </select>
          )}</Field>
          <Field label="Agenda">{({ id }) => <Textarea id={id} value={draft.agenda} onChange={(event) => patch({ agenda: event.target.value })} disabled={!editable} />}</Field>
          <Field label="Notes">{({ id }) => <Textarea id={id} value={draft.notes} onChange={(event) => patch({ notes: event.target.value })} disabled={!editable} />}</Field>
          <Field label="Decisions">{({ id }) => <Textarea id={id} value={draft.decisions} onChange={(event) => patch({ decisions: event.target.value })} disabled={!editable} />}</Field>
          <ActionEditor items={draft.actionItems} people={options.data?.people ?? []} disabled={!editable} onChange={(actionItems) => patch({ actionItems })} />
          {editable ? <Button type="submit" disabled={save.isPending}>{creating ? "Create meeting" : "Save changes"}</Button> : null}
          {editable && selectedId && !creating ? <Button type="button" variant="danger" onClick={() => remove.mutate(selectedId)}>Delete</Button> : null}
        </form>
      </Drawer>
    </div>
  );
}

function ActionEditor({ items, people, disabled, onChange }: { items: ActionDraft[]; people: { id: string; name: string }[]; disabled: boolean; onChange: (items: ActionDraft[]) => void }) {
  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <span className="text-[13px] font-medium">Action items</span>
        {disabled ? null : <Button type="button" size="sm" variant="secondary" onClick={() => onChange([...items, { title: "", assigneeId: "", dueAt: "", isCompleted: false }])}>Add</Button>}
      </div>
      {items.map((item, index) => (
        <div key={index} className="grid grid-cols-1 gap-2 rounded-lg border border-line p-2 sm:grid-cols-[1fr_8rem_auto]">
          <Input aria-label="Action title" value={item.title} placeholder="What needs doing" onChange={(event) => onChange(items.map((current, currentIndex) => currentIndex === index ? { ...current, title: event.target.value } : current))} disabled={disabled} />
          <Select aria-label="Assignee" value={item.assigneeId} onChange={(event) => onChange(items.map((current, currentIndex) => currentIndex === index ? { ...current, assigneeId: event.target.value } : current))} disabled={disabled}>
            <option value="">Unassigned</option>
            {people.map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}
          </Select>
          <label className="flex items-center gap-2 text-xs text-muted">
            <input type="checkbox" checked={item.isCompleted} onChange={(event) => onChange(items.map((current, currentIndex) => currentIndex === index ? { ...current, isCompleted: event.target.checked } : current))} disabled={disabled} />
            Done
          </label>
        </div>
      ))}
    </div>
  );
}

function MeetingMonth({ cursor, meetings, onOpen }: { cursor: Date; meetings: MeetingListItem[]; onOpen: (id: string) => void }) {
  const days = useMemo(() => {
    const start = startOfWeek(startOfMonth(cursor), { weekStartsOn: 1 });
    const end = endOfWeek(endOfMonth(cursor), { weekStartsOn: 1 });
    return eachDayOfInterval({ start, end });
  }, [cursor]);

  return (
    <div className="grid grid-cols-7 gap-1">
      {["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map((label) => <div key={label} className="px-1 text-[11px] text-muted">{label}</div>)}
      {days.map((day) => {
        const items = meetings.filter((meeting) => {
          const start = new Date(meeting.startAt);
          const end = new Date(meeting.endAt);
          const dayEnd = addDays(day, 1);
          return start < dayEnd && end > day;
        });
        return (
          <div key={day.toISOString()} className={cn("min-h-20 rounded-lg border border-line p-1", isSameMonth(day, cursor) ? "bg-surface" : "bg-surface-muted")}>
            <div className="text-[11px] text-muted">{format(day, "d")}</div>
            {items.slice(0, 3).map((meeting) => (
              <button key={meeting.id} type="button" className="mt-1 block w-full truncate rounded bg-accent-soft px-1 text-left text-[11px] text-accent" onClick={() => onOpen(meeting.id)}>{meeting.title}</button>
            ))}
          </div>
        );
      })}
    </div>
  );
}
