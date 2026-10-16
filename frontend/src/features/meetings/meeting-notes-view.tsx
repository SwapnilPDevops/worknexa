import { keepPreviousData, useQuery, useQueryClient } from "@tanstack/react-query";
import { format } from "date-fns";
import {
  Check,
  Columns2,
  Copy,
  Info,
  ListOrdered,
  MoreHorizontal,
  PanelLeft,
  Pin,
  Search,
  SquarePen,
  Trash2,
  Users,
} from "lucide-react";
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
import {
  convertActionToTask,
  fetchMeetingNote,
  fetchMeetingNotes,
  localDateTime,
  saveMeetingNote,
  type MeetingNoteAction,
  type MeetingNoteDocument,
  type MeetingNoteWrite,
} from "./meeting-notes-api.ts";
import { cn } from "../../utils/cn.ts";

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
    actions: draft.actions
      .filter((action) => action.title.trim())
      .map((action) => ({
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
  const [isSidebarOpen, setIsSidebarOpen] = useState(true);
  const [pinnedNoteIds, setPinnedNoteIds] = useState<Set<string>>(() =>
    requestedMeeting ? new Set([requestedMeeting]) : new Set()
  );

  if (requestedMeeting && seenMeeting !== requestedMeeting) {
    setSeenMeeting(requestedMeeting);
    setSelectedId(requestedMeeting);
  }

  const notes = useQuery({
    queryKey: ["meeting-notes", debounced],
    queryFn: () => fetchMeetingNotes(debounced),
    placeholderData: keepPreviousData,
  });

  const detail = useQuery({
    queryKey: ["meeting-note", selectedId],
    queryFn: () => fetchMeetingNote(selectedId!),
    enabled: Boolean(selectedId),
  });

  const options = useQuery({ queryKey: ["work-options"], queryFn: fetchWorkOptions });

  useEffect(() => {
    document.title = "Meeting notes · Worknexa";
  }, []);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(query.trim()), 300);
    return () => window.clearTimeout(handle);
  }, [query]);

  function handleTogglePin(id: string) {
    setPinnedNoteIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  if (!can(user?.permissions, permissions.meetingsRead)) {
    return <ErrorState title="Access denied" description="Your account does not include meetings." />;
  }

  if (notes.isPending && !notes.data) {
    return <LoadingState label="Loading meeting notes" />;
  }

  if ((notes.isError && !notes.data) || !notes.data) {
    const description = notes.error instanceof ApiError ? notes.error.message : "Meeting notes could not be loaded.";
    return (
      <ErrorState
        title="Meeting notes are unavailable"
        description={description}
        onRetry={() => void notes.refetch()}
      />
    );
  }

  return (
    <div className="w-full flex-1 rounded-xl border border-[#D1CCC2]/80 dark:border-[#2e303a] bg-[#EBE8E2]/60 dark:bg-[#16171d] shadow-sm overflow-hidden min-h-[calc(100vh-140px)] flex flex-col md:flex-row">
      {/* Left Column: All Notes Sidebar */}
      <aside
        className={cn(
          "border-r border-[#D1CCC2]/70 dark:border-[#2e303a] bg-[#EBE8E2]/90 dark:bg-[#1a1b23] flex flex-col shrink-0 transition-all duration-200",
          isSidebarOpen ? "w-full md:w-72 lg:w-80" : "hidden md:hidden"
        )}
      >
        {/* Top bar: Hamburger / Panel toggle + All Notes + Compose */}
        <div className="flex items-center justify-between px-3.5 py-3 border-b border-[#D1CCC2]/60 dark:border-[#2e303a]">
          <div className="flex items-center gap-2">
            <button
              type="button"
              className="p-1.5 rounded-md hover:bg-black/5 dark:hover:bg-white/10 text-[#4B5563] dark:text-[#9CA3AF] transition-colors cursor-pointer"
              title="Toggle sidebar"
              onClick={() => setIsSidebarOpen((prev) => !prev)}
            >
              <PanelLeft className="w-4 h-4" />
            </button>
            <h1 className="text-base font-semibold tracking-tight text-[#111827] dark:text-white">All Notes</h1>
          </div>
          <button
            type="button"
            aria-label="New note"
            title="New note"
            className="p-1.5 rounded-md hover:bg-black/5 dark:hover:bg-white/10 text-[#4B5563] dark:text-[#9CA3AF] transition-colors cursor-pointer"
            onClick={() => {
              if (notes.data && notes.data.length > 0) {
                setSelectedId(notes.data[0].id);
                setParams({ meeting: notes.data[0].id });
              }
            }}
          >
            <SquarePen className="w-4 h-4" />
          </button>
        </div>

        {/* Search bar */}
        <div className="p-3 border-b border-[#D1CCC2]/50 dark:border-[#2e303a]">
          <div className="relative">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-[#9CA3AF]" />
            <input
              type="text"
              aria-label="Search meeting notes"
              value={query}
              placeholder="Search all notes and tags"
              onChange={(event) => setQuery(event.target.value)}
              className="w-full pl-8.5 pr-3 py-1.5 text-xs rounded-md bg-[#DFDCD6]/90 dark:bg-[#1f2028] border border-[#D1CCC2] dark:border-[#2e303a] text-[#111827] dark:text-white placeholder-[#9CA3AF] focus:outline-none focus:ring-1 focus:ring-[#111827] dark:focus:ring-white transition-all"
            />
          </div>
        </div>

        {/* Notes List */}
        <div className="flex-1 overflow-y-auto p-2 space-y-1">
          {notes.data.length === 0 ? (
            <div className="py-8">
              <EmptyState title="No meetings" description="Notes appear here after a meeting is scheduled." />
            </div>
          ) : (
            <ul className="space-y-1">
              {notes.data.map((meeting) => {
                const isSelected = meeting.id === selectedId;
                const isPinned = pinnedNoteIds.has(meeting.id);
                return (
                  <li key={meeting.id}>
                    <button
                      type="button"
                      className={cn(
                        "w-full rounded-lg px-3 py-2.5 text-left transition-all cursor-pointer relative group border",
                        isSelected
                          ? "bg-[#DFDCD6] dark:bg-[#282932] border-[#C4BEB1] dark:border-[#3e404d] text-[#111827] dark:text-white shadow-2xs"
                          : "bg-transparent border-transparent hover:bg-[#DFDCD6]/50 dark:hover:bg-[#1f2028] text-[#4B5563] dark:text-[#9CA3AF]"
                      )}
                      onClick={() => {
                        setSelectedId(meeting.id);
                        setParams({ meeting: meeting.id });
                      }}
                    >
                      <div className="flex items-start justify-between gap-1.5">
                        <span className="block text-sm font-semibold text-[#111827] dark:text-white truncate">
                          {meeting.title}
                        </span>
                        {isPinned && (
                          <Pin className="w-3.5 h-3.5 text-emerald-500 fill-emerald-500 rotate-45 shrink-0 mt-0.5" />
                        )}
                      </div>
                      <p className="mt-0.5 text-xs text-[#6B7280] dark:text-[#9CA3AF] line-clamp-2 leading-relaxed">
                        {format(new Date(meeting.startAt), "d MMM yyyy, HH:mm")}
                      </p>
                    </button>
                  </li>
                );
              })}
            </ul>
          )}
        </div>
      </aside>

      {/* Right Column: Note Document Canvas */}
      <main className="flex-1 flex flex-col min-w-0 bg-[#E6E1DA]/40 dark:bg-[#16171d] overflow-y-auto">
        {detail.data ? (
          <NoteEditor
            key={detail.data.id}
            note={detail.data}
            people={options.data?.people ?? []}
            canConvert={can(user?.permissions, permissions.tasksManage)}
            userInitial={user?.displayName?.slice(0, 1).toUpperCase() ?? "S"}
            userName={user?.displayName ?? "User"}
            isPinned={pinnedNoteIds.has(detail.data.id)}
            onTogglePin={() => handleTogglePin(detail.data.id)}
            onToggleSidebar={() => setIsSidebarOpen((prev) => !prev)}
          />
        ) : selectedId && detail.isPending ? (
          <div className="p-8">
            <LoadingState label="Loading notes" />
          </div>
        ) : (
          <div className="flex-1 flex items-center justify-center p-8">
            <EmptyState title="Choose a meeting" description="Open a meeting to capture the discussion." />
          </div>
        )}
      </main>
    </div>
  );
}

function NoteEditor({
  note,
  people,
  canConvert,
  userInitial,
  userName,
  isPinned,
  onTogglePin,
  onToggleSidebar,
}: {
  note: MeetingNoteDocument;
  people: { id: string; name: string }[];
  canConvert: boolean;
  userInitial: string;
  userName: string;
  isPinned: boolean;
  onTogglePin: () => void;
  onToggleSidebar: () => void;
}) {
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
  const [, setBaseline] = useState(() => JSON.stringify(server));
  const [error, setError] = useState<string | null>(null);
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isMarkdownMode, setIsMarkdownMode] = useState(false);
  const [isPublished, setIsPublished] = useState(false);
  const [showOutline, setShowOutline] = useState(false);
  const [showInfo, setShowInfo] = useState(false);
  const [copiedFeedback, setCopiedFeedback] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  const draftRef = useRef(draft);
  useEffect(() => {
    draftRef.current = draft;
  }, [draft]);

  const status = useAutosave(
    storageKey,
    draft,
    JSON.stringify(server),
    true,
    restored ? "Restored unsaved changes" : null,
    async (value) => {
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
    }
  );

  // Close popup menu on outside click
  useEffect(() => {
    if (!isMenuOpen) return;
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsMenuOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [isMenuOpen]);

  function patchAction(index: number, partial: Partial<ActionDraft>) {
    setDraft((current) => ({
      ...current,
      actions: current.actions.map((action, actionIndex) =>
        actionIndex === index ? { ...action, ...partial } : action
      ),
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

  function handleCopyLink() {
    void navigator.clipboard?.writeText(window.location.href);
    setCopiedFeedback(true);
    setTimeout(() => setCopiedFeedback(false), 2000);
  }

  return (
    <section className="flex flex-col min-h-full">
      {/* Top Toolbar matching reference UI */}
      <header className="sticky top-0 z-20 flex items-center justify-between px-4 sm:px-6 py-2.5 border-b border-[#D1CCC2]/60 dark:border-[#2e303a] bg-[#E6E1DA]/90 dark:bg-[#16171d]/90 backdrop-blur-xs">
        {/* Left: Split view toggle + Autosave status */}
        <div className="flex items-center gap-3">
          <button
            type="button"
            className="p-1.5 rounded-md hover:bg-black/5 dark:hover:bg-white/10 text-[#6B7280] dark:text-[#9CA3AF] transition-colors cursor-pointer"
            title="Toggle sidebar"
            onClick={onToggleSidebar}
          >
            <Columns2 className="w-4 h-4" />
          </button>
          <span className="text-xs font-mono text-[#6B7280] dark:text-[#9CA3AF] px-2 py-0.5 rounded-full bg-black/5 dark:bg-white/10">
            {status}
          </span>
        </div>

        {/* Right: Actions menu, Outline, Info, User Avatar */}
        <div className="flex items-center gap-1.5 sm:gap-2 relative">
          {/* "..." More menu button */}
          <div className="relative" ref={menuRef}>
            <button
              type="button"
              aria-label="More options"
              title="More options"
              className={cn(
                "p-1.5 rounded-md text-[#6B7280] dark:text-[#9CA3AF] hover:bg-black/5 dark:hover:bg-white/10 transition-colors cursor-pointer",
                isMenuOpen && "bg-black/10 dark:bg-white/10 text-[#111827] dark:text-white"
              )}
              onClick={() => setIsMenuOpen((prev) => !prev)}
            >
              <MoreHorizontal className="w-4 h-4" />
            </button>

            {/* Floating Dropdown Menu from screenshot */}
            {isMenuOpen && (
              <div className="absolute right-0 top-full mt-1.5 w-52 rounded-xl bg-[#1E2028] text-[#F3F4F6] border border-[#30323F] shadow-2xl p-1.5 z-50 text-xs">
                {/* Pin to top */}
                <button
                  type="button"
                  className="w-full flex items-center justify-between px-2.5 py-1.5 rounded-md hover:bg-white/10 transition-colors text-left cursor-pointer"
                  onClick={() => {
                    onTogglePin();
                    setIsMenuOpen(false);
                  }}
                >
                  <span>Pin to top</span>
                  {isPinned ? (
                    <span className="w-3.5 h-3.5 rounded-xs bg-emerald-500/20 text-emerald-400 flex items-center justify-center">
                      <Check className="w-3 h-3 stroke-[3]" />
                    </span>
                  ) : (
                    <span className="w-3.5 h-3.5 rounded-xs border border-white/30" />
                  )}
                </button>

                {/* Markdown */}
                <button
                  type="button"
                  className="w-full flex items-center justify-between px-2.5 py-1.5 rounded-md hover:bg-white/10 transition-colors text-left cursor-pointer"
                  onClick={() => setIsMarkdownMode((prev) => !prev)}
                >
                  <span>Markdown</span>
                  {isMarkdownMode ? (
                    <span className="w-3.5 h-3.5 rounded-xs bg-emerald-500/20 text-emerald-400 flex items-center justify-center">
                      <Check className="w-3 h-3 stroke-[3]" />
                    </span>
                  ) : (
                    <span className="w-3.5 h-3.5 rounded-xs border border-white/30" />
                  )}
                </button>

                {/* Copy Internal Link */}
                <button
                  type="button"
                  className="w-full flex items-center justify-between px-2.5 py-1.5 rounded-md hover:bg-white/10 transition-colors text-left cursor-pointer"
                  onClick={() => {
                    handleCopyLink();
                    setIsMenuOpen(false);
                  }}
                >
                  <span>Copy Internal Link</span>
                </button>

                <div className="h-px bg-[#30323F] my-1" />

                {/* Publish */}
                <button
                  type="button"
                  className="w-full flex items-center justify-between px-2.5 py-1.5 rounded-md hover:bg-white/10 transition-colors text-left cursor-pointer"
                  onClick={() => setIsPublished((prev) => !prev)}
                >
                  <span>Publish</span>
                  {isPublished ? (
                    <span className="w-3.5 h-3.5 rounded-xs bg-emerald-500/20 text-emerald-400 flex items-center justify-center">
                      <Check className="w-3 h-3 stroke-[3]" />
                    </span>
                  ) : (
                    <span className="w-3.5 h-3.5 rounded-xs border border-white/30" />
                  )}
                </button>

                {/* History */}
                <button
                  type="button"
                  className="w-full flex items-center justify-between px-2.5 py-1.5 rounded-md hover:bg-white/10 transition-colors text-left cursor-pointer"
                  onClick={() => {
                    setShowInfo(true);
                    setIsMenuOpen(false);
                  }}
                >
                  <span>History…</span>
                </button>

                {/* Copy Link */}
                <button
                  type="button"
                  className="w-full flex items-center justify-between px-2.5 py-1.5 rounded-md hover:bg-white/10 transition-colors text-left cursor-pointer"
                  onClick={() => {
                    handleCopyLink();
                    setIsMenuOpen(false);
                  }}
                >
                  <span>Copy Link</span>
                  <Copy className="w-3.5 h-3.5 text-white/70" />
                </button>

                {/* Collaborate */}
                <button
                  type="button"
                  className="w-full flex items-center justify-between px-2.5 py-1.5 rounded-md hover:bg-white/10 transition-colors text-left cursor-pointer"
                  onClick={() => {
                    setShowInfo(true);
                    setIsMenuOpen(false);
                  }}
                >
                  <span>Collaborate</span>
                  <Users className="w-3.5 h-3.5 text-white/70" />
                </button>

                <div className="h-px bg-[#30323F] my-1" />

                {/* Move to Trash */}
                <button
                  type="button"
                  className="w-full flex items-center justify-between px-2.5 py-1.5 rounded-md hover:bg-white/10 transition-colors text-rose-400 hover:text-rose-300 text-left cursor-pointer"
                  onClick={() => {
                    if (window.confirm("Are you sure you want to discard unsaved draft changes?")) {
                      clearDraft(storageKey);
                      setDraft(server);
                    }
                    setIsMenuOpen(false);
                  }}
                >
                  <span>Move to Trash</span>
                  <Trash2 className="w-3.5 h-3.5 text-rose-400" />
                </button>
              </div>
            )}
          </div>

          {/* Outline button */}
          <button
            type="button"
            className={cn(
              "p-1.5 rounded-md text-[#6B7280] dark:text-[#9CA3AF] hover:bg-black/5 dark:hover:bg-white/10 transition-colors cursor-pointer",
              showOutline && "bg-black/10 dark:bg-white/10 text-[#111827] dark:text-white"
            )}
            title="Table of contents"
            onClick={() => setShowOutline((prev) => !prev)}
          >
            <ListOrdered className="w-4 h-4" />
          </button>

          {/* Info button */}
          <button
            type="button"
            className={cn(
              "p-1.5 rounded-md text-[#6B7280] dark:text-[#9CA3AF] hover:bg-black/5 dark:hover:bg-white/10 transition-colors cursor-pointer",
              showInfo && "bg-black/10 dark:bg-white/10 text-[#111827] dark:text-white"
            )}
            title="Note information"
            onClick={() => setShowInfo((prev) => !prev)}
          >
            <Info className="w-4 h-4" />
          </button>

          {/* User circular avatar badge ("S") */}
          <div
            className="w-6.5 h-6.5 rounded-full bg-[#f87171] text-white flex items-center justify-center text-xs font-semibold select-none shadow-xs ml-0.5"
            title={userName}
          >
            {userInitial}
          </div>
        </div>
      </header>

      {/* Main Document Content Canvas */}
      <div className="flex-1 max-w-4xl w-full mx-auto p-4 sm:p-8 space-y-6">
        {error ? <p className="text-sm text-danger">{error}</p> : null}

        {copiedFeedback && (
          <div className="fixed bottom-6 right-6 z-50 px-3 py-1.5 rounded-md bg-[#111827] text-white text-xs shadow-lg animate-in fade-in">
            Link copied to clipboard
          </div>
        )}

        {/* Note Title and Project metadata */}
        <div className="space-y-1">
          <h2 className="text-2xl sm:text-3xl font-bold tracking-tight text-[#111827] dark:text-white">
            {note.title}
          </h2>
          <p className="text-xs text-[#6B7280] dark:text-[#9CA3AF]">
            {format(new Date(note.startAt), "d MMM yyyy, HH:mm")}
            {note.projectName ? ` · ${note.projectName}` : ""}
          </p>
        </div>

        {/* Outline popover / jump menu */}
        {showOutline && (
          <div className="p-3.5 rounded-lg border border-[#D1CCC2] dark:border-[#2e303a] bg-[#DFDCD6]/60 dark:bg-[#1a1b23] text-xs space-y-1.5">
            <div className="font-semibold text-[#111827] dark:text-white mb-1">Document Sections</div>
            <a href="#agenda-section" className="block text-[#4B5563] dark:text-[#9CA3AF] hover:text-[#111827] dark:hover:text-white">
              1. Agenda
            </a>
            <a href="#discussion-section" className="block text-[#4B5563] dark:text-[#9CA3AF] hover:text-[#111827] dark:hover:text-white">
              2. Discussion
            </a>
            <a href="#decisions-section" className="block text-[#4B5563] dark:text-[#9CA3AF] hover:text-[#111827] dark:hover:text-white">
              3. Decisions
            </a>
            <a href="#actions-section" className="block text-[#4B5563] dark:text-[#9CA3AF] hover:text-[#111827] dark:hover:text-white">
              4. Action items ({draft.actions.length})
            </a>
            <a href="#followups-section" className="block text-[#4B5563] dark:text-[#9CA3AF] hover:text-[#111827] dark:hover:text-white">
              5. Follow-ups
            </a>
          </div>
        )}

        {/* Info panel */}
        {showInfo && (
          <div className="p-3.5 rounded-lg border border-[#D1CCC2] dark:border-[#2e303a] bg-[#DFDCD6]/60 dark:bg-[#1a1b23] text-xs space-y-2">
            <div className="font-semibold text-[#111827] dark:text-white">Note Metadata</div>
            <div className="grid grid-cols-2 gap-2 text-[#4B5563] dark:text-[#9CA3AF]">
              <div>
                Project: <span className="font-medium text-[#111827] dark:text-white">{note.projectName || "General"}</span>
              </div>
              <div>
                Updated: <span className="font-medium text-[#111827] dark:text-white">{format(new Date(note.updatedAt), "d MMM yyyy, HH:mm")}</span>
              </div>
              <div>
                Action items: <span className="font-medium text-[#111827] dark:text-white">{draft.actions.length}</span>
              </div>
              <div>
                Status: <span className="font-medium text-[#111827] dark:text-white">{status}</span>
              </div>
            </div>
          </div>
        )}

        {/* Note Sections */}
        <div className="space-y-6">
          <div id="agenda-section">
            <MarkdownEditor
              id="agenda"
              label="Agenda"
              value={draft.agenda}
              people={people}
              onChange={(agenda) => setDraft((current) => ({ ...current, agenda }))}
            />
          </div>

          <div id="discussion-section">
            <MarkdownEditor
              id="discussion"
              label="Discussion"
              value={draft.discussion}
              people={people}
              onChange={(discussion) => setDraft((current) => ({ ...current, discussion }))}
            />
          </div>

          <div id="decisions-section">
            <MarkdownEditor
              id="decisions"
              label="Decisions"
              value={draft.decisions}
              people={people}
              onChange={(decisions) => setDraft((current) => ({ ...current, decisions }))}
            />
          </div>

          {/* Action Items List */}
          <div id="actions-section" className="space-y-3 rounded-lg border border-[#D1CCC2]/80 dark:border-[#2e303a] bg-[#DFDCD6]/30 dark:bg-[#1a1b23]/40 p-4">
            <div className="flex items-center justify-between">
              <span className="text-sm font-semibold text-[#111827] dark:text-white">Action items</span>
              <Button
                type="button"
                size="sm"
                variant="secondary"
                onClick={() =>
                  setDraft((current) => ({
                    ...current,
                    actions: [
                      ...current.actions,
                      {
                        id: `new-${crypto.randomUUID()}`,
                        title: "",
                        description: "",
                        assigneeId: "",
                        dueAt: "",
                        isCompleted: false,
                        taskId: null,
                        taskTitle: null,
                      },
                    ],
                  }))
                }
              >
                Add
              </Button>
            </div>

            {draft.actions.map((action, index) => (
              <div key={action.id} className="space-y-2.5 rounded-lg border border-[#D1CCC2] dark:border-[#2e303a] bg-surface p-3 shadow-2xs">
                <Input
                  aria-label="Action title"
                  value={action.title}
                  placeholder="What needs doing"
                  onChange={(event) => patchAction(index, { title: event.target.value })}
                />
                <Input
                  aria-label="Action description"
                  value={action.description}
                  placeholder="Details"
                  onChange={(event) => patchAction(index, { description: event.target.value })}
                />
                <div className="grid gap-2 sm:grid-cols-2">
                  <Select
                    aria-label="Assignee"
                    value={action.assigneeId}
                    onChange={(event) => patchAction(index, { assigneeId: event.target.value })}
                  >
                    <option value="">Unassigned</option>
                    {people.map((person) => (
                      <option key={person.id} value={person.id}>
                        {person.name}
                      </option>
                    ))}
                  </Select>
                  <Input
                    aria-label="Due"
                    type="datetime-local"
                    value={action.dueAt}
                    onChange={(event) => patchAction(index, { dueAt: event.target.value })}
                  />
                </div>
                <div className="flex flex-wrap items-center justify-between gap-2 pt-1 border-t border-[#D1CCC2]/40 dark:border-[#2e303a]">
                  <label className="flex items-center gap-2 text-xs text-[#4B5563] dark:text-[#9CA3AF] cursor-pointer">
                    <input
                      type="checkbox"
                      aria-label="Done"
                      checked={action.isCompleted}
                      onChange={(event) => patchAction(index, { isCompleted: event.target.checked })}
                    />
                    Done
                  </label>
                  <div className="flex items-center gap-2">
                    {action.taskId ? (
                      <Link className="text-xs font-medium text-accent hover:underline" to={`/tasks?task=${action.taskId}`}>
                        Open task{action.taskTitle ? `: ${action.taskTitle}` : ""}
                      </Link>
                    ) : canConvert ? (
                      <Button type="button" size="sm" variant="secondary" onClick={() => void convert(action)}>
                        Convert to task
                      </Button>
                    ) : null}
                    <Button
                      type="button"
                      size="sm"
                      variant="ghost"
                      onClick={() =>
                        setDraft((current) => ({
                          ...current,
                          actions: current.actions.filter((_, actionIndex) => actionIndex !== index),
                        }))
                      }
                    >
                      Remove
                    </Button>
                  </div>
                </div>
              </div>
            ))}
          </div>

          <div id="followups-section">
            <MarkdownEditor
              id="follow-ups"
              label="Follow-ups"
              value={draft.followUps}
              people={people}
              onChange={(followUps) => setDraft((current) => ({ ...current, followUps }))}
            />
          </div>

          <Field label="Follow-up date">
            {({ id }) => (
              <Input
                id={id}
                type="datetime-local"
                value={draft.followUpAt}
                onChange={(event) => setDraft((current) => ({ ...current, followUpAt: event.target.value }))}
              />
            )}
          </Field>
        </div>
      </div>
    </section>
  );
}

