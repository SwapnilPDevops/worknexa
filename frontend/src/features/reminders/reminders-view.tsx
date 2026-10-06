import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { format } from "date-fns";
import { useEffect, useState, type FormEvent } from "react";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Badge } from "../../components/design-system/badge.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Drawer } from "../../components/design-system/drawer.tsx";
import { Field, Input, Select, Textarea } from "../../components/design-system/field.tsx";
import { EmptyState, ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { ApiError } from "../../services/api-client.ts";
import { fromDateTimeInput, toDateTimeInput } from "../work/api.ts";
import { priorities, priorityTone } from "../work/catalog.ts";
import { scheduleRecurrences } from "../schedule/catalog.ts";
import { completeReminder, deleteReminder, fetchReminderLinks, fetchReminders, reopenReminder, saveReminder, type ReminderItem, type ReminderWrite } from "../schedule/api.ts";

type Draft = {
  title: string;
  description: string;
  remindAt: string;
  recurrence: string;
  recurrenceInterval: string;
  priority: string;
  taskId: string;
  meetingId: string;
};

function blankDraft(): Draft {
  const soon = new Date(Date.now() + 60 * 60 * 1000);
  return {
    title: "",
    description: "",
    remindAt: toDateTimeInput(soon.toISOString()),
    recurrence: "None",
    recurrenceInterval: "1",
    priority: "Medium",
    taskId: "",
    meetingId: "",
  };
}

function draftFrom(item: ReminderItem): Draft {
  return {
    title: item.title,
    description: item.description ?? "",
    remindAt: toDateTimeInput(item.remindAt),
    recurrence: item.recurrence,
    recurrenceInterval: String(item.recurrenceInterval ?? 1),
    priority: item.priority,
    taskId: item.taskId ?? "",
    meetingId: item.meetingId ?? "",
  };
}

function toWrite(draft: Draft): ReminderWrite {
  return {
    title: draft.title,
    description: draft.description || null,
    remindAt: fromDateTimeInput(draft.remindAt) ?? new Date().toISOString(),
    recurrence: draft.recurrence,
    recurrenceInterval: draft.recurrence === "Custom" ? Number(draft.recurrenceInterval) : null,
    priority: draft.priority,
    taskId: draft.taskId || null,
    meetingId: draft.meetingId || null,
  };
}

export function RemindersView() {
  const { user } = useAuth();
  const manageable = can(user?.permissions, permissions.tasksManage);
  const [creating, setCreating] = useState(false);
  const [selected, setSelected] = useState<ReminderItem | null>(null);
  const [draft, setDraft] = useState<Draft>(blankDraft);
  const [quickTitle, setQuickTitle] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const queryClient = useQueryClient();
  const reminders = useQuery({ queryKey: ["reminders"], queryFn: fetchReminders });
  const links = useQuery({ queryKey: ["reminder-links"], queryFn: fetchReminderLinks });

  useEffect(() => {
    document.title = "Reminders · Worknexa";
  }, []);

  function refresh() {
    return Promise.all([
      queryClient.invalidateQueries({ queryKey: ["reminders"] }),
      queryClient.invalidateQueries({ queryKey: ["notifications"] }),
      queryClient.invalidateQueries({ queryKey: ["my-day"] }),
      queryClient.invalidateQueries({ queryKey: ["calendar"] }),
    ]);
  }

  const save = useMutation({
    mutationFn: (input: { id?: string; reminder: ReminderWrite }) => saveReminder(input.reminder, input.id),
    onSuccess: async () => {
      setCreating(false);
      setSelected(null);
      setQuickTitle("");
      setMessage(null);
      await refresh();
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The reminder could not be saved."),
  });

  const toggle = useMutation({
    mutationFn: (item: ReminderItem) => (item.isCompleted ? reopenReminder(item.id) : completeReminder(item.id)),
    onSuccess: () => refresh(),
  });

  const remove = useMutation({
    mutationFn: deleteReminder,
    onSuccess: async () => {
      setSelected(null);
      await refresh();
    },
  });

  function openCreate() {
    setDraft({ ...blankDraft(), title: quickTitle });
    setCreating(true);
    setSelected(null);
    setMessage(null);
  }

  async function quickAdd(event: FormEvent) {
    event.preventDefault();
    const next = { ...blankDraft(), title: quickTitle };
    await save.mutateAsync({ reminder: toWrite(next) });
  }

  if (!can(user?.permissions, permissions.tasksRead)) {
    return <ErrorState title="Access denied" description="Your account does not include reminders." />;
  }

  if (reminders.isPending) {
    return <LoadingState label="Loading reminders" />;
  }

  if (reminders.isError || !reminders.data) {
    const description = reminders.error instanceof ApiError ? reminders.error.message : "Reminders could not be loaded.";
    return <ErrorState title="Reminders are unavailable" description={description} onRetry={() => void reminders.refetch()} />;
  }

  const open = reminders.data.filter((item) => !item.isCompleted);
  const done = reminders.data.filter((item) => item.isCompleted);
  const drawerOpen = creating || Boolean(selected);

  return (
    <div className="mx-auto max-w-3xl space-y-5">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Reminders</h1>
          <p className="mt-1 text-sm text-muted">One-time and repeating nudges, including those tied to a task or meeting.</p>
        </div>
      </header>

      {manageable ? (
        <form className="flex flex-wrap items-center gap-2" onSubmit={(event) => void quickAdd(event)}>
          <Input className="min-w-56 flex-1" value={quickTitle} onChange={(event) => setQuickTitle(event.target.value)} placeholder="Quick add a reminder" aria-label="Reminder title" maxLength={300} />
          <Button type="submit" disabled={!quickTitle.trim() || save.isPending}>Add</Button>
          <Button type="button" variant="secondary" onClick={openCreate}>More options</Button>
        </form>
      ) : null}

      {open.length === 0 && done.length === 0 ? (
        <EmptyState title="No reminders yet" description="Add one for later today, or tie it to a task or meeting." />
      ) : (
        <div className="space-y-6">
          <ReminderList title="Open" items={open} onOpen={(item) => { setCreating(false); setSelected(item); setDraft(draftFrom(item)); }} onToggle={(item) => toggle.mutate(item)} />
          <ReminderList title="Completed" items={done} onOpen={(item) => { setCreating(false); setSelected(item); setDraft(draftFrom(item)); }} onToggle={(item) => toggle.mutate(item)} />
        </div>
      )}

      <Drawer open={drawerOpen} title={creating ? "New reminder" : draft.title || "Reminder"} onClose={() => { setCreating(false); setSelected(null); }} className="max-w-lg">
        <form
          className="space-y-4"
          onSubmit={(event) => {
            event.preventDefault();
            void save.mutateAsync({ id: creating ? undefined : selected?.id, reminder: toWrite(draft) });
          }}
        >
          {message ? <p className="text-sm text-danger">{message}</p> : null}
          <Field label="Title">{({ id }) => <Input id={id} value={draft.title} onChange={(event) => setDraft({ ...draft, title: event.target.value })} required maxLength={300} disabled={!manageable} />}</Field>
          <Field label="Description">{({ id }) => <Textarea id={id} value={draft.description} onChange={(event) => setDraft({ ...draft, description: event.target.value })} disabled={!manageable} />}</Field>
          <div className="grid grid-cols-2 gap-3">
            <Field label="When">{({ id }) => <Input id={id} type="datetime-local" value={draft.remindAt} onChange={(event) => setDraft({ ...draft, remindAt: event.target.value })} required disabled={!manageable} />}</Field>
            <Field label="Priority">{({ id }) => <Select id={id} value={draft.priority} onChange={(event) => setDraft({ ...draft, priority: event.target.value })} disabled={!manageable}>{priorities.map((priority) => <option key={priority} value={priority}>{priority}</option>)}</Select>}</Field>
            <Field label="Repeats">{({ id }) => <Select id={id} value={draft.recurrence} onChange={(event) => setDraft({ ...draft, recurrence: event.target.value })} disabled={!manageable}>{scheduleRecurrences.map((item) => <option key={item} value={item}>{item === "None" ? "One-time" : item}</option>)}</Select>}</Field>
            <Field label="Related task">{({ id }) => <Select id={id} value={draft.taskId} onChange={(event) => setDraft({ ...draft, taskId: event.target.value })} disabled={!manageable}><option value="">None</option>{links.data?.tasks.map((task) => <option key={task.id} value={task.id}>{task.name}</option>)}</Select>}</Field>
            <Field label="Related meeting">{({ id }) => <Select id={id} value={draft.meetingId} onChange={(event) => setDraft({ ...draft, meetingId: event.target.value })} disabled={!manageable}><option value="">None</option>{links.data?.meetings.map((meeting) => <option key={meeting.id} value={meeting.id}>{meeting.name}</option>)}</Select>}</Field>
          </div>
          {draft.recurrence === "Custom" ? (
            <Field label="Every" hint="Number of days between repeats.">{({ id }) => <Input id={id} type="number" min={1} max={366} value={draft.recurrenceInterval} onChange={(event) => setDraft({ ...draft, recurrenceInterval: event.target.value })} disabled={!manageable} />}</Field>
          ) : null}
          {manageable ? <Button type="submit" disabled={save.isPending}>{creating ? "Create reminder" : "Save changes"}</Button> : null}
          {manageable && selected && !creating ? <Button type="button" variant="danger" onClick={() => remove.mutate(selected.id)}>Delete</Button> : null}
        </form>
      </Drawer>
    </div>
  );
}

function ReminderList({
  title,
  items,
  onOpen,
  onToggle,
}: {
  title: string;
  items: ReminderItem[];
  onOpen: (item: ReminderItem) => void;
  onToggle: (item: ReminderItem) => void;
}) {
  if (items.length === 0) {
    return null;
  }

  return (
    <section>
      <h2 className="text-sm font-semibold">{title}</h2>
      <ul className="mt-2 divide-y divide-line rounded-xl border border-line bg-surface">
        {items.map((item) => (
          <li key={item.id} className="flex items-center gap-3 px-3 py-2">
            <input type="checkbox" checked={item.isCompleted} aria-label={item.isCompleted ? `Reopen ${item.title}` : `Complete ${item.title}`} onChange={() => onToggle(item)} />
            <button type="button" className="min-w-0 flex-1 text-left" onClick={() => onOpen(item)}>
              <span className="block truncate text-sm font-medium">{item.title}</span>
              <span className="block truncate text-xs text-muted">
                {format(new Date(item.remindAt), "d MMM, HH:mm")}
                {item.recurrence !== "None" ? ` · ${item.recurrence}${item.recurrence === "Custom" && item.recurrenceInterval ? ` every ${item.recurrenceInterval} days` : ""}` : ""}
                {item.taskTitle ? ` · ${item.taskTitle}` : ""}
                {item.meetingTitle ? ` · ${item.meetingTitle}` : ""}
              </span>
            </button>
            <Badge tone={priorityTone(item.priority)}>{item.priority}</Badge>
          </li>
        ))}
      </ul>
    </section>
  );
}
