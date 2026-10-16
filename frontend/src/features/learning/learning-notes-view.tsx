import { keepPreviousData, useQuery, useQueryClient } from "@tanstack/react-query";
import { format } from "date-fns";
import { Star } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Field, Input, Select } from "../../components/design-system/field.tsx";
import { EmptyState, ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { ApiError } from "../../services/api-client.ts";
import { clearDraft, readDraft, writeDraft } from "../notes/draft-store.ts";
import { MarkdownEditor } from "../notes/markdown-editor.tsx";
import { deleteNote, fetchNote, fetchNoteOptions, fetchNotes, saveNote, type NoteItem, type NoteWrite } from "../notes/api.ts";
import { useAutosave } from "../notes/use-autosave.ts";
import { fetchWorkOptions } from "../work/api.ts";
import { TopicResources } from "./topic-resources.tsx";
import { cn } from "../../utils/cn.ts";

type Draft = {
  title: string;
  content: string;
  category: string;
  isFavorite: boolean;
  tags: string;
  learningTopicId: string;
};

function blankDraft(topicId: string): Draft {
  return { title: "", content: "", category: "Learning", isFavorite: false, tags: "", learningTopicId: topicId };
}

function draftFrom(note: NoteItem): Draft {
  return {
    title: note.title,
    content: note.content,
    category: note.category ?? "",
    isFavorite: note.isFavorite,
    tags: note.tags.join(", "),
    learningTopicId: note.learningTopicId ?? "",
  };
}

function toWrite(draft: Draft): NoteWrite {
  return {
    title: draft.title.trim(),
    content: draft.content,
    category: draft.category.trim() || null,
    isFavorite: draft.isFavorite,
    isPinned: false,
    projectId: null,
    taskId: null,
    meetingId: null,
    learningTopicId: draft.learningTopicId || null,
    tags: draft.tags.split(",").map((tag) => tag.trim()).filter(Boolean),
  };
}

export function LearningNotesView() {
  const { user } = useAuth();
  const canEdit = can(user?.permissions, permissions.notesManage);
  const [params] = useSearchParams();
  const requestedId = params.get("note");
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const [favorites, setFavorites] = useState(false);
  const [creating, setCreating] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(requestedId);
  const [openNote, setOpenNote] = useState<NoteItem | null>(null);
  const timer = useRef<number | null>(null);
  const notes = useQuery({
    queryKey: ["learning-notes", debounced, favorites],
    queryFn: () => fetchNotes(debounced, { learningOnly: true, favorites }),
    placeholderData: keepPreviousData,
  });
  const requested = useQuery({ queryKey: ["note", requestedId], queryFn: () => fetchNote(requestedId!), enabled: Boolean(requestedId) });
  const options = useQuery({ queryKey: ["note-options"], queryFn: fetchNoteOptions });
  const people = useQuery({ queryKey: ["work-options"], queryFn: fetchWorkOptions });

  if (!can(user?.permissions, permissions.learningRead)) {
    return <ErrorState title="Access denied" description="Your account does not include learning." />;
  }

  if (notes.isPending && !notes.data) {
    return <LoadingState label="Loading learning notes" />;
  }

  if ((notes.isError && !notes.data) || !notes.data) {
    return <ErrorState title="Learning notes are unavailable" description={notes.error instanceof ApiError ? notes.error.message : "Learning notes could not be loaded."} onRetry={() => void notes.refetch()} />;
  }

  const selected = notes.data.find((note) => note.id === selectedId) ?? (openNote?.id === selectedId ? openNote : null) ?? (requested.data?.id === selectedId ? requested.data : null);

  function onSearch(value: string) {
    setQuery(value);
    if (timer.current) {
      window.clearTimeout(timer.current);
    }

    timer.current = window.setTimeout(() => setDebounced(value), 250);
  }

  return (
    <div className="grid w-full gap-4 lg:grid-cols-[18rem_minmax(0,1fr)]">
      <aside className="space-y-3">
        <header className="flex items-end justify-between gap-2">
          <div>
            <h1 className="text-xl font-semibold tracking-tight text-[#111827]">Learning notes</h1>
            <p className="text-xs text-[#4B5563] font-serif">Markdown, tags, and favorites, linked to a topic.</p>
          </div>
          {canEdit ? <Button size="sm" onClick={() => { setCreating(true); setSelectedId(null); }}>New</Button> : null}
        </header>
        <Input aria-label="Search learning notes" value={query} placeholder="Search notes" onChange={(event) => onSearch(event.target.value)} />
        <Button size="sm" variant={favorites ? "secondary" : "ghost"} aria-pressed={favorites} onClick={() => setFavorites((current) => !current)}>Favorites</Button>
        {notes.data.length === 0 ? (
          <EmptyState title="No learning notes" description={debounced || favorites ? "Nothing matches that search." : "Write a note and attach it to a topic."} />
        ) : (
          <ul className="space-y-2">
            {notes.data.map((note) => (
              <li key={note.id}>
                <button
                  type="button"
                  className={cn(
                    "w-full rounded-[8px] border px-3.5 py-2.5 text-left transition-all cursor-pointer",
                    note.id === selectedId
                      ? "bg-[#FED7AA] border-[#111827]/15 text-[#111827] shadow-xs"
                      : "bg-[#EBE8E2] border-[#D1CCC2] text-[#4B5563] hover:text-[#111827] hover:border-[#111827]/30"
                  )}
                  onClick={() => { setCreating(false); setSelectedId(note.id); setOpenNote(note); }}
                >
                  <span className="flex items-center gap-1.5 text-sm font-medium text-[#111827]">
                    {note.isFavorite ? <Star className="size-3 text-[#111827]" aria-label="Favorite" /> : null}
                    {note.title}
                  </span>
                  <span className="text-xs text-[#4B5563] font-serif">{note.learningTopicTitle ?? "No topic"} · {format(new Date(note.updatedAt), "d MMM")}</span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </aside>
      {creating && options.data ? (
        <LearningNoteEditor key="new" note={null} topics={options.data.learningTopics} people={people.data?.people ?? []} canEdit={canEdit} canManageTopics={can(user?.permissions, permissions.learningManage)} onCreated={(note) => { setCreating(false); setSelectedId(note.id); setOpenNote(note); }} onDeleted={() => setCreating(false)} />
      ) : selected && options.data ? (
        <LearningNoteEditor key={selected.id} note={selected} topics={options.data.learningTopics} people={people.data?.people ?? []} canEdit={canEdit} canManageTopics={can(user?.permissions, permissions.learningManage)} onCreated={() => undefined} onDeleted={() => setSelectedId(null)} />
      ) : (
        <EmptyState title="Choose a note" description="Search by title, content, or tag. Favorite notes stay easy to find." />
      )}
    </div>
  );
}

function LearningNoteEditor({
  note,
  topics,
  people,
  canEdit,
  canManageTopics,
  onCreated,
  onDeleted,
}: {
  note: NoteItem | null;
  topics: { id: string; name: string }[];
  people: { id: string; name: string }[];
  canEdit: boolean;
  canManageTopics: boolean;
  onCreated: (note: NoteItem) => void;
  onDeleted: () => void;
}) {
  const queryClient = useQueryClient();
  const storageKey = `learning-note.${note?.id ?? "new"}`;
  const server = useMemo(() => (note ? draftFrom(note) : blankDraft(topics[0]?.id ?? "")), [note, topics]);
  const restored = useMemo(() => {
    const local = readDraft<Draft>(storageKey);
    return local && JSON.stringify(local) !== JSON.stringify(server) ? local : null;
  }, [server, storageKey]);
  const [draft, setDraft] = useState<Draft>(restored ?? server);
  const [baseline, setBaseline] = useState(() => JSON.stringify(server));
  const [error, setError] = useState<string | null>(null);
  const draftRef = useRef(draft);
  useEffect(() => {
    draftRef.current = draft;
  }, [draft]);
  const status = useAutosave(storageKey, draft, baseline, canEdit && draft.title.trim().length > 0 && draft.learningTopicId.length > 0, restored ? "Restored unsaved changes" : null, async (value) => {
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
          writeDraft(`learning-note.${saved.id}`, settled);
        }
        clearDraft("learning-note.new");
        onCreated(saved);
      }
      setError(null);
      await queryClient.invalidateQueries({ queryKey: ["learning-notes"] });
      await queryClient.invalidateQueries({ queryKey: ["notes"] });
      await queryClient.invalidateQueries({ queryKey: ["learning-dashboard"] });
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
    await queryClient.invalidateQueries({ queryKey: ["learning-notes"] });
    onDeleted();
  }

  return (
    <section className="space-y-4 rounded-xl border border-line bg-surface p-4">
      <header className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-xs text-muted">{status}{note ? ` · Updated ${format(new Date(note.updatedAt), "d MMM, HH:mm")}` : ""}</p>
        <div className="flex gap-2">
          <Button type="button" size="sm" variant={draft.isFavorite ? "secondary" : "ghost"} disabled={!canEdit} onClick={() => setDraft((current) => ({ ...current, isFavorite: !current.isFavorite }))}>{draft.isFavorite ? "Favorited" : "Favorite"}</Button>
          {canEdit ? <Button type="button" size="sm" variant="ghost" onClick={() => void remove()}>Delete</Button> : null}
        </div>
      </header>
      {error ? <p className="text-sm text-danger">{error}</p> : null}
      <Field label="Title">{({ id }) => <Input id={id} value={draft.title} maxLength={300} disabled={!canEdit} onChange={(event) => setDraft((current) => ({ ...current, title: event.target.value }))} />}</Field>
      <div className="grid gap-3 sm:grid-cols-2">
        <Field label="Topic">{({ id }) => (
          <Select id={id} value={draft.learningTopicId} disabled={!canEdit} onChange={(event) => setDraft((current) => ({ ...current, learningTopicId: event.target.value }))}>
            <option value="">Choose a topic</option>
            {topics.map((topic) => <option key={topic.id} value={topic.id}>{topic.name}</option>)}
          </Select>
        )}</Field>
        <Field label="Tags" hint="Separate tags with commas.">{({ id }) => <Input id={id} value={draft.tags} disabled={!canEdit} onChange={(event) => setDraft((current) => ({ ...current, tags: event.target.value }))} />}</Field>
      </div>
      <MarkdownEditor id="learning-note-content" label="Note" value={draft.content} people={people} disabled={!canEdit} onChange={(content) => setDraft((current) => ({ ...current, content }))} />
      {draft.learningTopicId ? <TopicResources topicId={draft.learningTopicId} manageable={canManageTopics} /> : <p className="text-sm text-muted">Choose a topic to attach this note and its resources.</p>}
    </section>
  );
}
