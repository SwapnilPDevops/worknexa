import { keepPreviousData, useQuery, useQueryClient } from "@tanstack/react-query";
import { format } from "date-fns";
import { Pin, Star } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Field, Input, Select } from "../../components/design-system/field.tsx";
import { EmptyState, ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { ApiError } from "../../services/api-client.ts";
import { fetchWorkOptions } from "../work/api.ts";
import { deleteNote, fetchNote, fetchNoteOptions, fetchNotes, saveNote, type NoteItem, type NoteOptions, type NoteWrite } from "./api.ts";
import { clearDraft, readDraft, writeDraft } from "./draft-store.ts";
import { MarkdownEditor } from "./markdown-editor.tsx";
import { useAutosave } from "./use-autosave.ts";

type Draft = {
  title: string;
  content: string;
  category: string;
  isFavorite: boolean;
  isPinned: boolean;
  projectId: string;
  taskId: string;
  meetingId: string;
  learningTopicId: string;
  tags: string;
};

const blankDraft: Draft = {
  title: "",
  content: "",
  category: "",
  isFavorite: false,
  isPinned: false,
  projectId: "",
  taskId: "",
  meetingId: "",
  learningTopicId: "",
  tags: "",
};

function draftFrom(note: NoteItem): Draft {
  return {
    title: note.title,
    content: note.content,
    category: note.category ?? "",
    isFavorite: note.isFavorite,
    isPinned: note.isPinned,
    projectId: note.projectId ?? "",
    taskId: note.taskId ?? "",
    meetingId: note.meetingId ?? "",
    learningTopicId: note.learningTopicId ?? "",
    tags: note.tags.join(", "),
  };
}

function toWrite(draft: Draft): NoteWrite {
  return {
    title: draft.title.trim(),
    content: draft.content,
    category: draft.category.trim() || null,
    isFavorite: draft.isFavorite,
    isPinned: draft.isPinned,
    projectId: draft.projectId || null,
    taskId: draft.taskId || null,
    meetingId: draft.meetingId || null,
    learningTopicId: draft.learningTopicId || null,
    tags: draft.tags.split(",").map((tag) => tag.trim()).filter(Boolean),
  };
}

export function NotesView() {
  const { user } = useAuth();
  const manageable = can(user?.permissions, permissions.notesManage);
  const [params] = useSearchParams();
  const requestedId = params.get("note");
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const [selectedId, setSelectedId] = useState<string | null>(requestedId);
  const [creating, setCreating] = useState(false);
  const [openNote, setOpenNote] = useState<NoteItem | null>(null);
  const notes = useQuery({ queryKey: ["notes", debounced], queryFn: () => fetchNotes(debounced), placeholderData: keepPreviousData });
  const requested = useQuery({ queryKey: ["note", requestedId], queryFn: () => fetchNote(requestedId!), enabled: Boolean(requestedId) });
  const options = useQuery({ queryKey: ["note-options"], queryFn: fetchNoteOptions });
  const people = useQuery({ queryKey: ["work-options"], queryFn: fetchWorkOptions });

  useEffect(() => {
    document.title = "Notes · Worknexa";
  }, []);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(query.trim()), 300);
    return () => window.clearTimeout(handle);
  }, [query]);

  if (!can(user?.permissions, permissions.notesRead)) {
    return <ErrorState title="Access denied" description="Your account does not include notes." />;
  }

  if (notes.isPending && !notes.data) {
    return <LoadingState label="Loading notes" />;
  }

  if ((notes.isError && !notes.data) || !notes.data) {
    const description = notes.error instanceof ApiError ? notes.error.message : "Notes could not be loaded.";
    return <ErrorState title="Notes are unavailable" description={description} onRetry={() => void notes.refetch()} />;
  }

  const selected = notes.data.find((note) => note.id === selectedId) ?? (openNote?.id === selectedId ? openNote : null) ?? (requested.data?.id === selectedId ? requested.data : null);

  return (
    <div className="mx-auto grid max-w-6xl gap-4 lg:grid-cols-[18rem_minmax(0,1fr)]">
      <aside className="space-y-3">
        <header className="flex items-end justify-between gap-2">
          <div>
            <h1 className="text-lg font-semibold">Notes</h1>
            <p className="text-sm text-muted">Search titles, content, tags, and categories.</p>
          </div>
          {manageable ? <Button size="sm" onClick={() => { setCreating(true); setSelectedId(null); }}>New</Button> : null}
        </header>
        <Input aria-label="Search notes" value={query} placeholder="Search notes" onChange={(event) => setQuery(event.target.value)} />
        {notes.data.length === 0 ? (
          <EmptyState title="No notes" description={debounced ? "Nothing matches that search." : "Create a note to keep it here."} />
        ) : (
          <ul className="space-y-2">
            {notes.data.map((note) => (
              <li key={note.id}>
                <button type="button" className="w-full rounded-xl border border-line bg-surface px-3 py-2 text-left" onClick={() => { setCreating(false); setSelectedId(note.id); setOpenNote(note); }}>
                  <span className="flex items-center gap-1 text-sm font-medium">
                    {note.isPinned ? <Pin className="size-3 text-accent" aria-label="Pinned" /> : null}
                    {note.isFavorite ? <Star className="size-3 text-accent" aria-label="Favorite" /> : null}
                    {note.title}
                  </span>
                  <span className="text-xs text-muted">{note.category ?? "Uncategorized"} · {format(new Date(note.updatedAt), "d MMM")}</span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </aside>
      {creating ? (
        <NoteEditor key="new" note={null} options={options.data} people={people.data?.people ?? []} onCreated={(note) => { setCreating(false); setSelectedId(note.id); setOpenNote(note); }} onDeleted={() => undefined} />
      ) : selected ? (
        <NoteEditor key={selected.id} note={selected} options={options.data} people={people.data?.people ?? []} onCreated={() => undefined} onDeleted={() => setSelectedId(null)} />
      ) : (
        <EmptyState title="Choose a note" description="Search or create a note. Unsaved writing stays on this device." />
      )}
    </div>
  );
}

function NoteEditor({
  note,
  options,
  people,
  onCreated,
  onDeleted,
}: {
  note: NoteItem | null;
  options?: NoteOptions;
  people: { id: string; name: string }[];
  onCreated: (note: NoteItem) => void;
  onDeleted: () => void;
}) {
  const queryClient = useQueryClient();
  const storageKey = `note.${note?.id ?? "new"}`;
  const server = useMemo(() => (note ? draftFrom(note) : blankDraft), [note]);
  const restored = useMemo(() => {
    const local = readDraft<Draft>(storageKey);
    return local && JSON.stringify(local) !== JSON.stringify(server) ? local : null;
  }, [server, storageKey]);
  const [draft, setDraft] = useState<Draft>(restored ?? server);
  const [baseline, setBaseline] = useState(() => JSON.stringify(server));
  const [error, setError] = useState<string | null>(null);
  const draftRef = useRef(draft);
  draftRef.current = draft;
  const status = useAutosave(storageKey, draft, baseline, draft.title.trim().length > 0, restored ? "Restored unsaved changes" : null, async (value) => {
    try {
      const saved = await saveNote(toWrite(value), note?.id);
      const next = draftFrom(saved);
      const settled = JSON.stringify(draftRef.current) === JSON.stringify(value) ? next : draftRef.current;
      setDraft(settled);
      setBaseline(JSON.stringify(next));
      if (JSON.stringify(settled) === JSON.stringify(next)) {
        clearDraft(storageKey);
      }
      if (!note) {
        if (JSON.stringify(settled) !== JSON.stringify(next)) {
          writeDraft(`note.${saved.id}`, settled);
        }
        clearDraft("note.new");
        onCreated(saved);
      }
      setError(null);
      await queryClient.invalidateQueries({ queryKey: ["notes"] });
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : "The note could not be saved.");
      throw caught;
    }
  });

  async function remove() {
    if (!note) {
      clearDraft(storageKey);
      onDeleted();
      return;
    }

    await deleteNote(note.id);
    clearDraft(storageKey);
    await queryClient.invalidateQueries({ queryKey: ["notes"] });
    onDeleted();
  }

  return (
    <section className="space-y-4 rounded-xl border border-line bg-surface p-4">
      <header className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-xs text-muted">{status}{note ? ` · Updated ${format(new Date(note.updatedAt), "d MMM, HH:mm")}` : ""}</p>
        <div className="flex gap-2">
          <Button type="button" size="sm" variant={draft.isPinned ? "secondary" : "ghost"} onClick={() => setDraft((current) => ({ ...current, isPinned: !current.isPinned }))}>{draft.isPinned ? "Pinned" : "Pin"}</Button>
          <Button type="button" size="sm" variant={draft.isFavorite ? "secondary" : "ghost"} onClick={() => setDraft((current) => ({ ...current, isFavorite: !current.isFavorite }))}>{draft.isFavorite ? "Favorited" : "Favorite"}</Button>
          <Button type="button" size="sm" variant="ghost" onClick={() => void remove()}>Delete</Button>
        </div>
      </header>
      {error ? <p className="text-sm text-danger">{error}</p> : null}
      <Field label="Title">{({ id }) => <Input id={id} value={draft.title} maxLength={300} onChange={(event) => setDraft((current) => ({ ...current, title: event.target.value }))} />}</Field>
      <div className="grid gap-3 sm:grid-cols-2">
        <Field label="Category">{({ id }) => <Input id={id} list="note-categories" value={draft.category} maxLength={80} onChange={(event) => setDraft((current) => ({ ...current, category: event.target.value }))} />}</Field>
        <Field label="Tags" hint="Separate tags with commas.">{({ id }) => <Input id={id} value={draft.tags} onChange={(event) => setDraft((current) => ({ ...current, tags: event.target.value }))} />}</Field>
        <Field label="Project">{({ id }) => <Select id={id} value={draft.projectId} onChange={(event) => setDraft((current) => ({ ...current, projectId: event.target.value }))}><option value="">None</option>{options?.projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}</Select>}</Field>
        <Field label="Task">{({ id }) => <Select id={id} value={draft.taskId} onChange={(event) => setDraft((current) => ({ ...current, taskId: event.target.value }))}><option value="">None</option>{options?.tasks.map((task) => <option key={task.id} value={task.id}>{task.name}</option>)}</Select>}</Field>
        <Field label="Meeting">{({ id }) => <Select id={id} value={draft.meetingId} onChange={(event) => setDraft((current) => ({ ...current, meetingId: event.target.value }))}><option value="">None</option>{options?.meetings.map((meeting) => <option key={meeting.id} value={meeting.id}>{meeting.name}</option>)}</Select>}</Field>
        <Field label="Learning topic">{({ id }) => <Select id={id} value={draft.learningTopicId} onChange={(event) => setDraft((current) => ({ ...current, learningTopicId: event.target.value }))}><option value="">None</option>{options?.learningTopics.map((topic) => <option key={topic.id} value={topic.id}>{topic.name}</option>)}</Select>}</Field>
      </div>
      <datalist id="note-categories">
        <option value="General" />
        <option value="Meeting" />
        <option value="Research" />
        <option value="Personal" />
      </datalist>
      <MarkdownEditor id="note-content" label="Content" value={draft.content} people={people} onChange={(content) => setDraft((current) => ({ ...current, content }))} />
      <ReferenceLine note={note} />
    </section>
  );
}

function ReferenceLine({ note }: { note: NoteItem | null }) {
  if (!note) {
    return null;
  }

  return (
    <p className="text-xs text-muted">
      Created {format(new Date(note.createdAt), "d MMM yyyy")}
      {note.projectName ? ` · Project ${note.projectName}` : ""}
      {note.taskTitle ? ` · Task ${note.taskTitle}` : ""}
      {note.meetingId ? <> · <Link className="text-accent" to={`/meeting-notes?meeting=${note.meetingId}`}>{note.meetingTitle ?? "Meeting"}</Link></> : ""}
      {note.learningTopicTitle ? ` · Topic ${note.learningTopicTitle}` : ""}
    </p>
  );
}
