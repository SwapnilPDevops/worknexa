import { keepPreviousData, useQuery, useQueryClient } from "@tanstack/react-query";
import { format } from "date-fns";
import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Field, Input, Select } from "../../components/design-system/field.tsx";
import { EmptyState, ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { ApiError } from "../../services/api-client.ts";
import { fetchWorkOptions, toDateTimeInput } from "../work/api.ts";
import { MarkdownEditor } from "../notes/markdown-editor.tsx";
import { clearDraft, readDraft } from "../notes/draft-store.ts";
import { useAutosave } from "../notes/use-autosave.ts";
import { convertActionToTask, fetchMeetingNote, fetchMeetingNotes, localDateTime, saveMeetingNote, type MeetingNoteAction, type MeetingNoteDocument, type MeetingNoteWrite } from "./meeting-notes-api.ts";

type ActionDraft = {
  id: string;
  title: string;
  description: string;
  assigneeId: string;
  dueAt: string;
  isCompleted: boolean;
  taskId: string | null;
  taskTitle: string | null;
};

type Draft = {
  agenda: string;
  discussion: string;
  decisions: string;
  followUps: string;
  followUpAt: string;
  actions: ActionDraft[];
};

function draftFrom(note: MeetingNoteDocument): Draft {
  return {
    agenda: note.agenda ?? "",
    discussion: note.discussion ?? "",
    decisions: note.decisions ?? "",
    followUps: note.followUps ?? "",
    followUpAt: toDateTimeInput(note.followUpAt),
    actions: note.actions.map(actionFrom),
  };
}

function actionFrom(action: MeetingNoteAction): ActionDraft {
  return {
    id: action.id,
    title: action.title,
    description: action.description ?? "",
    assigneeId: action.assigneeId ?? "",
    dueAt: toDateTimeInput(action.dueAt),
    isCompleted: action.isCompleted,
    taskId: action.taskId,
    taskTitle: action.taskTitle,
  };
}

function toWrite(draft: Draft): MeetingNoteWrite {
  return {
    agenda: draft.agenda.trim() || null,
    discussion: draft.discussion.trim() || null,
    decisions: draft.decisions.trim() || null,
    followUps: draft.followUps.trim() || null,
    followUpAt: localDateTime(draft.followUpAt),
    actions: draft.actions.filter((action) => action.title.trim()).map((action) => ({
      id: action.id.startsWith("new-") ? null : action.id,
      title: action.title.trim(),
      description: action.description.trim() || null,
      assigneeId: action.assigneeId || null,
      dueAt: localDateTime(action.dueAt),
      isCompleted: action.isCompleted,
    })),
  };
}

function withActionIds(current: Draft, saved: Draft): Draft {
  let cursor = 0;
  return {
    ...current,
    actions: current.actions.map((action) => {
      if (!action.title.trim()) {
        return action;
      }

      const match = saved.actions[cursor];
      cursor += 1;
      return match ? { ...action, id: match.id, taskId: match.taskId, taskTitle: match.taskTitle } : action;
    }),
  };
}

function mergeTasks(local: Draft, server: Draft): Draft {
  return {
    ...local,
    actions: local.actions.map((action) => {
      const match = server.actions.find((item) => item.id === action.id);
      return match?.taskId ? { ...action, taskId: match.taskId, taskTitle: match.taskTitle } : action;
    }),
  };
}

export function MeetingNotesView() {
  const { user } = useAuth();
  const [params, setParams] = useSearchParams();
  const requestedMeeting = params.get("meeting");
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const [selectedId, setSelectedId] = useState<string | null>(requestedMeeting);
  const [seenMeeting, setSeenMeeting] = useState(requestedMeeting);
  if (requestedMeeting && seenMeeting !== requestedMeeting) {
    setSeenMeeting(requestedMeeting);
    setSelectedId(requestedMeeting);
  }
  const notes = useQuery({ queryKey: ["meeting-notes", debounced], queryFn: () => fetchMeetingNotes(debounced), placeholderData: keepPreviousData });
  const detail = useQuery({ queryKey: ["meeting-note", selectedId], queryFn: () => fetchMeetingNote(selectedId!), enabled: Boolean(selectedId) });
  const options = useQuery({ queryKey: ["work-options"], queryFn: fetchWorkOptions });

  useEffect(() => {
    document.title = "Meeting notes · Worknexa";
  }, []);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(query.trim()), 300);
    return () => window.clearTimeout(handle);
  }, [query]);

  if (!can(user?.permissions, permissions.meetingsRead)) {
    return <ErrorState title="Access denied" description="Your account does not include meetings." />;
  }

  if (notes.isPending && !notes.data) {
    return <LoadingState label="Loading meeting notes" />;
  }

  if ((notes.isError && !notes.data) || !notes.data) {
    const description = notes.error instanceof ApiError ? notes.error.message : "Meeting notes could not be loaded.";
    return <ErrorState title="Meeting notes are unavailable" description={description} onRetry={() => void notes.refetch()} />;
  }

  return (
    <div className="mx-auto grid max-w-6xl gap-4 lg:grid-cols-[18rem_minmax(0,1fr)]">
      <aside className="space-y-3">
        <header>
          <h1 className="text-lg font-semibold">Meeting notes</h1>
          <p className="text-sm text-muted">Agenda, discussion, decisions, actions, and follow-ups.</p>
        </header>
        <Input aria-label="Search meeting notes" value={query} placeholder="Search notes" onChange={(event) => setQuery(event.target.value)} />
        {notes.data.length === 0 ? (
          <EmptyState title="No meetings" description="Notes appear here after a meeting is scheduled." />
        ) : (
          <ul className="space-y-2">
            {notes.data.map((meeting) => (
              <li key={meeting.id}>
                <button
                  type="button"
                  className="w-full rounded-xl border border-line bg-surface px-3 py-2 text-left"
                  onClick={() => { setSelectedId(meeting.id); setParams({ meeting: meeting.id }); }}
                >
                  <span className="block text-sm font-medium">{meeting.title}</span>
                  <span className="text-xs text-muted">{format(new Date(meeting.startAt), "d MMM yyyy, HH:mm")}</span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </aside>
      {detail.data ? (
        <NoteEditor
          key={detail.data.id}
          note={detail.data}
          people={options.data?.people ?? []}
          canConvert={can(user?.permissions, permissions.tasksManage)}
        />
      ) : selectedId && detail.isPending ? (
        <LoadingState label="Loading notes" />
      ) : (
        <EmptyState title="Choose a meeting" description="Open a meeting to capture the discussion." />
      )}
    </div>
  );
}

function NoteEditor({ note, people, canConvert }: { note: MeetingNoteDocument; people: { id: string; name: string }[]; canConvert: boolean }) {
  const queryClient = useQueryClient();
  const storageKey = `meeting-note.${note.id}`;
  const server = useMemo(() => draftFrom(note), [note]);
  const restored = useMemo(() => {
    const local = readDraft<Draft>(storageKey);
    if (!local || JSON.stringify(local) === JSON.stringify(server)) {
      return null;
    }

    return mergeTasks(local, server);
  }, [server, storageKey]);
  const [draft, setDraft] = useState<Draft>(restored ?? server);
  const [baseline, setBaseline] = useState(() => JSON.stringify(server));
  const [error, setError] = useState<string | null>(null);
  const draftRef = useRef(draft);
  draftRef.current = draft;
  const status = useAutosave(storageKey, draft, baseline, true, restored ? "Restored unsaved changes" : null, async (value) => {
    try {
      const saved = await saveMeetingNote(note.id, toWrite(value));
      const next = draftFrom(saved);
      const settled = JSON.stringify(draftRef.current) === JSON.stringify(value) ? next : withActionIds(draftRef.current, next);
      setDraft(settled);
      setBaseline(JSON.stringify(next));
      if (settled === next) {
        clearDraft(storageKey);
      }
      setError(null);
      queryClient.setQueryData(["meeting-note", note.id], saved);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : "The notes could not be saved.");
      throw caught;
    }
  });

  function patchAction(index: number, partial: Partial<ActionDraft>) {
    setDraft((current) => ({
      ...current,
      actions: current.actions.map((action, actionIndex) => actionIndex === index ? { ...action, ...partial } : action),
    }));
  }

  async function convert(action: ActionDraft) {
    if (action.id.startsWith("new-")) {
      setError("Save the action item before converting it.");
      return;
    }

    setError(null);
    try {
      const saved = await convertActionToTask(note.id, action.id);
      const next = draftFrom(saved);
      setDraft(next);
      setBaseline(JSON.stringify(next));
      clearDraft(storageKey);
      queryClient.setQueryData(["meeting-note", note.id], saved);
      await queryClient.invalidateQueries({ queryKey: ["tasks"] });
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : "The task could not be created.");
    }
  }

  return (
    <section className="space-y-4 rounded-xl border border-line bg-surface p-4">
      <header className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <h2 className="text-base font-semibold">{note.title}</h2>
          <p className="text-xs text-muted">{format(new Date(note.startAt), "d MMM yyyy, HH:mm")}{note.projectName ? ` · ${note.projectName}` : ""}</p>
        </div>
        <p className="text-xs text-muted">{status}</p>
      </header>
      {error ? <p className="text-sm text-danger">{error}</p> : null}
      <MarkdownEditor id="agenda" label="Agenda" value={draft.agenda} people={people} onChange={(agenda) => setDraft((current) => ({ ...current, agenda }))} />
      <MarkdownEditor id="discussion" label="Discussion" value={draft.discussion} people={people} onChange={(discussion) => setDraft((current) => ({ ...current, discussion }))} />
      <MarkdownEditor id="decisions" label="Decisions" value={draft.decisions} people={people} onChange={(decisions) => setDraft((current) => ({ ...current, decisions }))} />
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <span className="text-[13px] font-medium">Action items</span>
          <Button type="button" size="sm" variant="secondary" onClick={() => setDraft((current) => ({ ...current, actions: [...current.actions, { id: `new-${crypto.randomUUID()}`, title: "", description: "", assigneeId: "", dueAt: "", isCompleted: false, taskId: null, taskTitle: null }] }))}>Add</Button>
        </div>
        {draft.actions.map((action, index) => (
          <div key={action.id} className="space-y-2 rounded-lg border border-line p-3">
            <Input aria-label="Action title" value={action.title} placeholder="What needs doing" onChange={(event) => patchAction(index, { title: event.target.value })} />
            <Input aria-label="Action description" value={action.description} placeholder="Details" onChange={(event) => patchAction(index, { description: event.target.value })} />
            <div className="grid gap-2 sm:grid-cols-2">
              <Select aria-label="Assignee" value={action.assigneeId} onChange={(event) => patchAction(index, { assigneeId: event.target.value })}>
                <option value="">Unassigned</option>
                {people.map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}
              </Select>
              <Input aria-label="Due" type="datetime-local" value={action.dueAt} onChange={(event) => patchAction(index, { dueAt: event.target.value })} />
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <label className="flex items-center gap-2 text-xs text-muted">
                <input type="checkbox" checked={action.isCompleted} onChange={(event) => patchAction(index, { isCompleted: event.target.checked })} />
                Done
              </label>
              {action.taskId ? (
                <Link className="text-sm text-accent" to={`/tasks?task=${action.taskId}`}>Open task{action.taskTitle ? `: ${action.taskTitle}` : ""}</Link>
              ) : canConvert ? (
                <Button type="button" size="sm" variant="secondary" onClick={() => void convert(action)}>Convert to task</Button>
              ) : null}
              <Button type="button" size="sm" variant="ghost" onClick={() => setDraft((current) => ({ ...current, actions: current.actions.filter((_, actionIndex) => actionIndex !== index) }))}>Remove</Button>
            </div>
          </div>
        ))}
      </div>
      <MarkdownEditor id="follow-ups" label="Follow-ups" value={draft.followUps} people={people} onChange={(followUps) => setDraft((current) => ({ ...current, followUps }))} />
      <Field label="Follow-up date">{({ id }) => <Input id={id} type="datetime-local" value={draft.followUpAt} onChange={(event) => setDraft((current) => ({ ...current, followUpAt: event.target.value }))} />}</Field>
    </section>
  );
}
