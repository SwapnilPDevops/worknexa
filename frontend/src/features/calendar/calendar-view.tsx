import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  addDays,
  addMonths,
  eachDayOfInterval,
  endOfMonth,
  endOfWeek,
  format,
  isSameDay,
  isSameMonth,
  startOfDay,
  startOfMonth,
  startOfWeek,
} from "date-fns";
import {
  CalendarDays,
  Check,
  CheckSquare,
  ChevronLeft,
  ChevronRight,
  Filter,
  MoreHorizontal,
  Plus,
  Search,
  Star,
  UserPlus,
  Users,
  Video,
  X,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState, type FormEvent } from "react";
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
import {
  calendarViews,
  eventKinds,
  kindClassName,
  scheduleRecurrences,
  type CalendarView as ViewMode,
} from "../schedule/catalog.ts";
import {
  deleteEvent,
  fetchCalendar,
  fetchEvent,
  saveEvent,
  type CalendarEntry,
  type CalendarEventWrite,
} from "../schedule/api.ts";

type TabFilter = "all" | "events" | "meetings" | "tasks";

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

const HOUR_HEIGHT = 80;
const START_HOUR = 8;
const END_HOUR = 19;
const HOURS = Array.from({ length: END_HOUR - START_HOUR + 1 }, (_, i) => START_HOUR + i);

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

function rangeFor(view: ViewMode, cursor: Date) {
  if (view === "day") {
    const from = startOfDay(cursor);
    return { from, to: addDays(from, 1), label: format(from, "EEEE, d MMMM yyyy") };
  }

  if (view === "week") {
    const from = startOfWeek(cursor, { weekStartsOn: 1 });
    return {
      from,
      to: addDays(from, 7),
      label: `${format(from, "d MMM")} – ${format(addDays(from, 6), "d MMM yyyy")}`,
    };
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
  if (!value) return null;
  const [year, month, day] = value.split("-").map(Number);
  if (!year || !month || !day) return null;
  return new Date(year, month - 1, day);
}

function covers(entry: CalendarEntry, day: Date) {
  const start = new Date(entry.startAt);
  const end = new Date(entry.endAt);
  const dayEnd = addDays(startOfDay(day), 1);
  return start < dayEnd && end > startOfDay(day);
}

function formatEventTime(startAt: string, endAt: string, isAllDay: boolean) {
  if (isAllDay) return "All day";
  const start = new Date(startAt);
  const end = new Date(endAt);

  const formatHour = (d: Date) => {
    const mins = d.getMinutes();
    const h = d.getHours() % 12 || 12;
    const ampm = d.getHours() >= 12 ? "PM" : "AM";
    return mins > 0 ? `${h}.${mins < 10 ? `0${mins}` : mins} ${ampm}` : `${h} ${ampm}`;
  };

  return `${formatHour(start)} - ${formatHour(end)}`;
}

function getEventCardStyle(entry: CalendarEntry) {
  if (entry.kind === "Meeting" || entry.origin === "meeting") {
    return {
      card: "bg-[#E0F2FE] border-[#BAE6FD] text-[#0369A1] hover:bg-[#D0ECFE] dark:bg-[#15324d] dark:border-[#234c75] dark:text-[#BAE6FD]",
      badge: "bg-[#BAE6FD] text-[#0C4A6E] dark:bg-[#234c75] dark:text-[#E0F2FE]",
      title: "text-[#0C4A6E] dark:text-[#F0F9FF]",
    };
  }

  if (entry.kind === "Learning" || entry.kind === "Deadline") {
    return {
      card: "bg-[#DCFCE7] border-[#BBF7D0] text-[#15803D] hover:bg-[#CDFAD9] dark:bg-[#143d2c] dark:border-[#1d5c43] dark:text-[#BBF7D0]",
      badge: "bg-[#BBF7D0] text-[#14532D] dark:bg-[#1d5c43] dark:text-[#DCFCE7]",
      title: "text-[#14532D] dark:text-[#F0FDF4]",
    };
  }

  return {
    card: "bg-[#F3E8FF] border-[#E9D5FF] text-[#6B21A8] hover:bg-[#EBD5FF] dark:bg-[#34244a] dark:border-[#523875] dark:text-[#E9D5FF]",
    badge: "bg-[#E9D5FF] text-[#581C87] dark:bg-[#523875] dark:text-[#F3E8FF]",
    title: "text-[#581C87] dark:text-[#FAF5FF]",
  };
}

export function CalendarView() {
  const { user } = useAuth();
  const manageable = can(user?.permissions, permissions.calendarManage);
  const [params] = useSearchParams();
  const requestedEvent = params.get("event");
  const [openedEvent, setOpenedEvent] = useState<string | null>(null);
  const [view, setView] = useState<ViewMode>("week");
  const [cursor, setCursor] = useState(() => dateFromParam(params.get("date")) ?? new Date());
  const [activeTab, setActiveTab] = useState<TabFilter>("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedKind, setSelectedKind] = useState<string>("all");
  const [selectedProject, setSelectedProject] = useState<string>("all");
  const [filterMenuOpen, setFilterMenuOpen] = useState(false);
  const [moreMenuOpen, setMoreMenuOpen] = useState(false);
  const [inviteModalOpen, setInviteModalOpen] = useState(false);
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteSentFeedback, setInviteSentFeedback] = useState(false);
  const [creating, setCreating] = useState(false);
  const [selected, setSelected] = useState<CalendarEntry | null>(null);
  const [draft, setDraft] = useState<Draft>(() => blankDraft(new Date()));
  const [message, setMessage] = useState<string | null>(null);

  const queryClient = useQueryClient();
  const range = useMemo(() => rangeFor(view, cursor), [view, cursor]);
  const filterRef = useRef<HTMLDivElement>(null);
  const moreRef = useRef<HTMLDivElement>(null);

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
    if (!detail.data || creating) return;

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

  // Click outside menus
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (filterRef.current && !filterRef.current.contains(event.target as Node)) {
        setFilterMenuOpen(false);
      }
      if (moreRef.current && !moreRef.current.contains(event.target as Node)) {
        setMoreMenuOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

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
      if (view === "month") return addMonths(current, direction);
      if (view === "week") return addDays(current, direction * 7);
      if (view === "agenda") return addDays(current, direction * 14);
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

  // Filter rows by Tab, Search, Kind, and Project
  const allRows = events.data;
  const filteredRows = allRows.filter((entry) => {
    // Tab filter
    if (activeTab === "events" && entry.origin !== "event" && entry.kind !== "Personal" && entry.kind !== "Deadline") {
      return false;
    }
    if (activeTab === "meetings" && entry.origin !== "meeting" && entry.kind !== "Meeting") {
      return false;
    }
    if (activeTab === "tasks" && entry.origin !== "task" && entry.kind !== "Task") {
      return false;
    }

    // Kind filter
    if (selectedKind !== "all" && entry.kind !== selectedKind) {
      return false;
    }

    // Project filter
    if (selectedProject !== "all" && entry.projectId !== selectedProject) {
      return false;
    }

    // Search query
    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      const matchTitle = entry.title.toLowerCase().includes(q);
      const matchDesc = entry.description?.toLowerCase().includes(q);
      const matchProject = entry.projectName?.toLowerCase().includes(q);
      if (!matchTitle && !matchDesc && !matchProject) return false;
    }

    return true;
  });

  const drawerOpen = creating || Boolean(selected);
  const readOnly = Boolean(selected && selected.origin !== "event");
  const canEdit = manageable && (creating || Boolean(detail.data?.editable));
  const people = options.data?.people ?? [];

  return (
    <div className="w-full flex-1 flex flex-col space-y-5">
      {/* 1. Header Section: Title + Subtitle + Avatars + Invite */}
      <header className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-[#111827] dark:text-white">
            Calendar
          </h1>
          <p className="mt-1 text-xs sm:text-sm text-[#6B7280] dark:text-[#9CA3AF]">
            Stay Organized and On Track with Your Personalized Calendar
          </p>
        </div>

        {/* Right side: User Avatars stack + Invite button */}
        <div className="flex items-center gap-3 self-start sm:self-auto">
          {/* Avatars Stack */}
          <div className="flex items-center -space-x-2 overflow-hidden">
            <div className="inline-flex h-8 w-8 items-center justify-center rounded-full bg-sky-200 text-sky-800 text-xs font-semibold ring-2 ring-white dark:ring-[#16171d]">
              AL
            </div>
            <div className="inline-flex h-8 w-8 items-center justify-center rounded-full bg-emerald-200 text-emerald-800 text-xs font-semibold ring-2 ring-white dark:ring-[#16171d]">
              DT
            </div>
            {people.slice(0, 2).map((person, index) => (
              <div
                key={person.id}
                className={cn(
                  "inline-flex h-8 w-8 items-center justify-center rounded-full text-xs font-semibold ring-2 ring-white dark:ring-[#16171d]",
                  index % 2 === 0 ? "bg-amber-200 text-amber-800" : "bg-purple-200 text-purple-800"
                )}
                title={person.name}
              >
                {person.name.slice(0, 2).toUpperCase()}
              </div>
            ))}
            <div className="inline-flex h-8 w-8 items-center justify-center rounded-full bg-[#E5E7EB] dark:bg-[#282932] text-[#4B5563] dark:text-[#9CA3AF] text-xs font-semibold ring-2 ring-white dark:ring-[#16171d]">
              +20
            </div>
          </div>

          {/* Invite button */}
          <button
            type="button"
            className="inline-flex items-center gap-1.5 rounded-lg border border-[#D1CCC2] dark:border-[#2e303a] bg-white dark:bg-[#1a1b23] px-3 py-1.5 text-xs font-medium text-[#111827] dark:text-white shadow-2xs hover:bg-[#F3F4F6] dark:hover:bg-[#282932] transition-colors cursor-pointer"
            onClick={() => setInviteModalOpen(true)}
          >
            <UserPlus className="h-3.5 w-3.5 text-[#6B7280] dark:text-[#9CA3AF]" />
            <span>Invite</span>
          </button>
        </div>
      </header>

      {/* 2. Tabs Bar + Search + Filter + New Button */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-3 border-b border-[#D1CCC2]/60 dark:border-[#2e303a] pb-3">
        {/* Left: Tab Navigation */}
        <div className="flex items-center gap-2 sm:gap-4 overflow-x-auto text-xs font-medium scrollbar-none">
          <button
            type="button"
            className={cn(
              "flex items-center gap-1.5 py-1.5 px-2.5 rounded-md transition-all cursor-pointer",
              activeTab === "all"
                ? "bg-[#111827] text-white shadow-xs dark:bg-white dark:text-[#111827]"
                : "text-[#6B7280] dark:text-[#9CA3AF] hover:text-[#111827] dark:hover:text-white hover:bg-black/5 dark:hover:bg-white/5"
            )}
            onClick={() => setActiveTab("all")}
          >
            <CalendarDays className="h-3.5 w-3.5" />
            <span>All Scheduled</span>
          </button>

          <button
            type="button"
            className={cn(
              "flex items-center gap-1.5 py-1.5 px-2.5 rounded-md transition-all cursor-pointer",
              activeTab === "events"
                ? "bg-[#111827] text-white shadow-xs dark:bg-white dark:text-[#111827]"
                : "text-[#6B7280] dark:text-[#9CA3AF] hover:text-[#111827] dark:hover:text-white hover:bg-black/5 dark:hover:bg-white/5"
            )}
            onClick={() => setActiveTab("events")}
          >
            <Star className="h-3.5 w-3.5" />
            <span>Events</span>
          </button>

          <button
            type="button"
            className={cn(
              "flex items-center gap-1.5 py-1.5 px-2.5 rounded-md transition-all cursor-pointer",
              activeTab === "meetings"
                ? "bg-[#111827] text-white shadow-xs dark:bg-white dark:text-[#111827]"
                : "text-[#6B7280] dark:text-[#9CA3AF] hover:text-[#111827] dark:hover:text-white hover:bg-black/5 dark:hover:bg-white/5"
            )}
            onClick={() => setActiveTab("meetings")}
          >
            <Video className="h-3.5 w-3.5" />
            <span>Meetings</span>
          </button>

          <button
            type="button"
            className={cn(
              "flex items-center gap-1.5 py-1.5 px-2.5 rounded-md transition-all cursor-pointer",
              activeTab === "tasks"
                ? "bg-[#111827] text-white shadow-xs dark:bg-white dark:text-[#111827]"
                : "text-[#6B7280] dark:text-[#9CA3AF] hover:text-[#111827] dark:hover:text-white hover:bg-black/5 dark:hover:bg-white/5"
            )}
            onClick={() => setActiveTab("tasks")}
          >
            <CheckSquare className="h-3.5 w-3.5" />
            <span>Task Reminders</span>
          </button>
        </div>

        {/* Right: Search, Filter, More, + New button */}
        <div className="flex items-center gap-2 flex-wrap">
          {/* Search Input */}
          <div className="relative min-w-[140px] sm:min-w-[180px]">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-[#9CA3AF]" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search.."
              className="w-full pl-8 pr-2.5 py-1.5 text-xs rounded-lg bg-white dark:bg-[#1a1b23] border border-[#D1CCC2] dark:border-[#2e303a] text-[#111827] dark:text-white placeholder-[#9CA3AF] focus:outline-none focus:ring-1 focus:ring-[#111827] dark:focus:ring-white transition-all"
            />
          </div>

          {/* Filter button & popover */}
          <div className="relative" ref={filterRef}>
            <button
              type="button"
              className={cn(
                "inline-flex items-center gap-1.5 rounded-lg border border-[#D1CCC2] dark:border-[#2e303a] bg-white dark:bg-[#1a1b23] px-2.5 py-1.5 text-xs font-medium text-[#4B5563] dark:text-[#9CA3AF] hover:bg-[#F3F4F6] dark:hover:bg-[#282932] transition-colors cursor-pointer",
                (selectedKind !== "all" || selectedProject !== "all" || filterMenuOpen) &&
                  "bg-[#F3F4F6] dark:bg-[#282932] text-[#111827] dark:text-white"
              )}
              onClick={() => setFilterMenuOpen(!filterMenuOpen)}
            >
              <Filter className="h-3.5 w-3.5" />
              <span>Filter</span>
              {(selectedKind !== "all" || selectedProject !== "all") && (
                <span className="w-1.5 h-1.5 rounded-full bg-blue-600" />
              )}
            </button>

            {filterMenuOpen && (
              <div className="absolute right-0 top-full mt-1.5 w-56 rounded-xl bg-white dark:bg-[#1E2028] border border-[#D1CCC2] dark:border-[#30323F] shadow-xl p-3 z-50 text-xs space-y-3">
                <div>
                  <label className="block text-[11px] font-semibold text-[#6B7280] dark:text-[#9CA3AF] mb-1">
                    Event Kind
                  </label>
                  <select
                    value={selectedKind}
                    onChange={(e) => setSelectedKind(e.target.value)}
                    className="w-full rounded-md border border-[#D1CCC2] dark:border-[#30323F] bg-white dark:bg-[#16171d] px-2 py-1 text-xs text-[#111827] dark:text-white"
                  >
                    <option value="all">All Kinds</option>
                    {eventKinds.map((k) => (
                      <option key={k} value={k}>
                        {k}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-[11px] font-semibold text-[#6B7280] dark:text-[#9CA3AF] mb-1">
                    Project
                  </label>
                  <select
                    value={selectedProject}
                    onChange={(e) => setSelectedProject(e.target.value)}
                    className="w-full rounded-md border border-[#D1CCC2] dark:border-[#30323F] bg-white dark:bg-[#16171d] px-2 py-1 text-xs text-[#111827] dark:text-white"
                  >
                    <option value="all">All Projects</option>
                    {options.data?.projects.map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="flex justify-end pt-1">
                  <button
                    type="button"
                    className="text-[11px] text-blue-600 dark:text-blue-400 hover:underline cursor-pointer"
                    onClick={() => {
                      setSelectedKind("all");
                      setSelectedProject("all");
                    }}
                  >
                    Reset filters
                  </button>
                </div>
              </div>
            )}
          </div>

          {/* More button */}
          <div className="relative" ref={moreRef}>
            <button
              type="button"
              className="p-1.5 rounded-lg border border-[#D1CCC2] dark:border-[#2e303a] bg-white dark:bg-[#1a1b23] text-[#4B5563] dark:text-[#9CA3AF] hover:bg-[#F3F4F6] dark:hover:bg-[#282932] transition-colors cursor-pointer"
              onClick={() => setMoreMenuOpen(!moreMenuOpen)}
              title="More options"
            >
              <MoreHorizontal className="h-4 w-4" />
            </button>

            {moreMenuOpen && (
              <div className="absolute right-0 top-full mt-1.5 w-44 rounded-xl bg-white dark:bg-[#1E2028] border border-[#D1CCC2] dark:border-[#30323F] shadow-xl p-1.5 z-50 text-xs">
                <button
                  type="button"
                  className="w-full text-left px-2.5 py-1.5 rounded-md hover:bg-[#F3F4F6] dark:hover:bg-white/10 text-[#111827] dark:text-white cursor-pointer"
                  onClick={() => {
                    void refresh();
                    setMoreMenuOpen(false);
                  }}
                >
                  Refresh Schedule
                </button>
                <button
                  type="button"
                  className="w-full text-left px-2.5 py-1.5 rounded-md hover:bg-[#F3F4F6] dark:hover:bg-white/10 text-[#111827] dark:text-white cursor-pointer"
                  onClick={() => {
                    window.print();
                    setMoreMenuOpen(false);
                  }}
                >
                  Print Calendar
                </button>
              </div>
            )}
          </div>

          {/* + New Button */}
          {manageable && (
            <button
              type="button"
              onClick={openCreate}
              className="inline-flex items-center gap-1.5 rounded-lg bg-[#111827] dark:bg-white px-3.5 py-1.5 text-xs font-semibold text-white dark:text-[#111827] hover:bg-black dark:hover:bg-[#E5E7EB] transition-colors shadow-2xs cursor-pointer"
            >
              <Plus className="h-3.5 w-3.5 stroke-[2.5]" />
              <span>New</span>
            </button>
          )}
        </div>
      </div>

      {/* 3. Date Navigation & View Switcher Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        {/* Month Label + Today button */}
        <div className="flex items-center gap-3">
          <h2 className="text-lg sm:text-xl font-bold tracking-tight text-[#111827] dark:text-white">
            {format(cursor, "MMMM yyyy")}
          </h2>
          <button
            type="button"
            onClick={() => setCursor(new Date())}
            className="rounded-md border border-[#D1CCC2] dark:border-[#2e303a] bg-white dark:bg-[#1a1b23] px-2.5 py-1 text-xs font-medium text-[#4B5563] dark:text-[#9CA3AF] hover:bg-[#F3F4F6] dark:hover:bg-[#282932] transition-colors cursor-pointer shadow-2xs"
          >
            Today
          </button>
        </div>

        {/* View Switcher (Day, Week, Month) + Date range badge */}
        <div className="flex items-center gap-2.5 flex-wrap">
          {/* Segmented control: Day | Week | Month */}
          <div className="inline-flex rounded-lg border border-[#D1CCC2] dark:border-[#2e303a] bg-white dark:bg-[#1a1b23] p-0.5 shadow-2xs">
            {calendarViews
              .filter((v) => v.id !== "agenda")
              .map((v) => (
                <button
                  key={v.id}
                  type="button"
                  className={cn(
                    "px-3 py-1 text-xs font-medium rounded-md transition-all cursor-pointer",
                    view === v.id
                      ? "bg-[#111827] text-white dark:bg-white dark:text-[#111827] shadow-2xs font-semibold"
                      : "text-[#6B7280] dark:text-[#9CA3AF] hover:text-[#111827] dark:hover:text-white"
                  )}
                  onClick={() => setView(v.id)}
                >
                  {v.label}
                </button>
              ))}
          </div>

          {/* Date Range Badge with Calendar icon */}
          <div className="inline-flex items-center gap-1.5 rounded-lg border border-[#D1CCC2] dark:border-[#2e303a] bg-white dark:bg-[#1a1b23] px-3 py-1 text-xs font-medium text-[#4B5563] dark:text-[#9CA3AF] shadow-2xs">
            <CalendarDays className="h-3.5 w-3.5 text-[#6B7280] dark:text-[#9CA3AF]" />
            <span>{range.label}</span>
          </div>
        </div>
      </div>

      {/* 4. Main Calendar Content Grid */}
      <main className="flex-1 rounded-xl border border-[#D1CCC2] dark:border-[#2e303a] bg-white dark:bg-[#16171d] shadow-xs overflow-hidden">
        {view === "week" ? (
          <WeekTimelineGrid
            from={range.from}
            rows={filteredRows}
            onOpen={openEntry}
            onPrev={() => shift(-1)}
            onNext={() => shift(1)}
          />
        ) : view === "day" ? (
          <DayTimelineGrid
            day={cursor}
            rows={filteredRows}
            onOpen={openEntry}
            onPrev={() => shift(-1)}
            onNext={() => shift(1)}
          />
        ) : view === "month" ? (
          <MonthGrid cursor={cursor} rows={filteredRows} onOpen={openEntry} onPrev={() => shift(-1)} onNext={() => shift(1)} />
        ) : (
          <Agenda rows={filteredRows} onOpen={openEntry} />
        )}
      </main>

      {/* Event Details / Create Drawer */}
      <Drawer
        open={drawerOpen}
        title={creating ? "New event" : selected?.title || "Event"}
        onClose={() => {
          setCreating(false);
          setSelected(null);
        }}
        className="max-w-lg"
      >
        {readOnly && selected ? (
          <ReadOnlyEntry entry={selected} />
        ) : (
          <form className="space-y-4" onSubmit={(event) => void onSave(event)}>
            {message ? <p className="text-sm text-danger">{message}</p> : null}
            <Field label="Title">
              {({ id }) => (
                <Input
                  id={id}
                  value={draft.title}
                  onChange={(event) => setDraft({ ...draft, title: event.target.value })}
                  required
                  maxLength={300}
                  disabled={!canEdit}
                  placeholder="e.g. Design Team Stand-up Meeting"
                />
              )}
            </Field>
            <Field label="Description">
              {({ id }) => (
                <Textarea
                  id={id}
                  value={draft.description}
                  onChange={(event) => setDraft({ ...draft, description: event.target.value })}
                  disabled={!canEdit}
                  placeholder="Meeting notes or details"
                />
              )}
            </Field>
            <div className="grid grid-cols-2 gap-3">
              <Field label="Kind">
                {({ id }) => (
                  <Select
                    id={id}
                    value={draft.kind}
                    onChange={(event) => setDraft({ ...draft, kind: event.target.value })}
                    disabled={!canEdit}
                  >
                    {eventKinds.map((kind) => (
                      <option key={kind} value={kind}>
                        {kind}
                      </option>
                    ))}
                  </Select>
                )}
              </Field>
              <Field label="Project">
                {({ id }) => (
                  <Select
                    id={id}
                    value={draft.projectId}
                    onChange={(event) => setDraft({ ...draft, projectId: event.target.value })}
                    disabled={!canEdit}
                  >
                    <option value="">No project</option>
                    {options.data?.projects.map((project) => (
                      <option key={project.id} value={project.id}>
                        {project.name}
                      </option>
                    ))}
                  </Select>
                )}
              </Field>
              <Field label="Start">
                {({ id }) => (
                  <Input
                    id={id}
                    type="datetime-local"
                    value={draft.startAt}
                    onChange={(event) => setDraft({ ...draft, startAt: event.target.value })}
                    required
                    disabled={!canEdit}
                  />
                )}
              </Field>
              <Field label="End">
                {({ id }) => (
                  <Input
                    id={id}
                    type="datetime-local"
                    value={draft.endAt}
                    onChange={(event) => setDraft({ ...draft, endAt: event.target.value })}
                    required
                    disabled={!canEdit}
                  />
                )}
              </Field>
              <Field label="Location">
                {({ id }) => (
                  <Input
                    id={id}
                    value={draft.location}
                    onChange={(event) => setDraft({ ...draft, location: event.target.value })}
                    maxLength={300}
                    disabled={!canEdit}
                  />
                )}
              </Field>
              <Field label="Meeting link">
                {({ id }) => (
                  <Input
                    id={id}
                    value={draft.meetingLink}
                    onChange={(event) => setDraft({ ...draft, meetingLink: event.target.value })}
                    maxLength={500}
                    disabled={!canEdit}
                    placeholder="https://meet.google.com/..."
                  />
                )}
              </Field>
              <Field label="Repeats">
                {({ id }) => (
                  <Select
                    id={id}
                    value={draft.recurrence}
                    onChange={(event) => setDraft({ ...draft, recurrence: event.target.value })}
                    disabled={!canEdit}
                  >
                    {scheduleRecurrences.map((item) => (
                      <option key={item} value={item}>
                        {item}
                      </option>
                    ))}
                  </Select>
                )}
              </Field>
              <Field label="Reminder">
                {({ id }) => (
                  <Input
                    id={id}
                    type="datetime-local"
                    value={draft.remindAt}
                    onChange={(event) => setDraft({ ...draft, remindAt: event.target.value })}
                    disabled={!canEdit}
                  />
                )}
              </Field>
            </div>
            {draft.recurrence === "Custom" ? (
              <Field label="Every" hint="Number of days between repeats.">
                {({ id }) => (
                  <Input
                    id={id}
                    type="number"
                    min={1}
                    max={366}
                    value={draft.recurrenceInterval}
                    onChange={(event) => setDraft({ ...draft, recurrenceInterval: event.target.value })}
                    disabled={!canEdit}
                  />
                )}
              </Field>
            ) : null}
            <label className="flex items-center gap-2 text-sm text-[#4B5563] dark:text-[#9CA3AF] cursor-pointer">
              <input
                type="checkbox"
                checked={draft.isAllDay}
                onChange={(event) => setDraft({ ...draft, isAllDay: event.target.checked })}
                disabled={!canEdit}
                className="rounded"
              />
              All day
            </label>
            <fieldset className="space-y-2">
              <legend className="text-[13px] font-medium text-[#111827] dark:text-white">Participants</legend>
              <div className="max-h-32 space-y-1 overflow-y-auto">
                {people.map((person) => (
                  <label key={person.id} className="flex items-center gap-2 text-xs text-[#4B5563] dark:text-[#9CA3AF] cursor-pointer">
                    <input
                      type="checkbox"
                      checked={draft.participantIds.includes(person.id)}
                      disabled={!canEdit}
                      onChange={(event) =>
                        setDraft({
                          ...draft,
                          participantIds: event.target.checked
                            ? [...draft.participantIds, person.id]
                            : draft.participantIds.filter((id) => id !== person.id),
                        })
                      }
                      className="rounded"
                    />
                    {person.name}
                  </label>
                ))}
              </div>
            </fieldset>
            {canEdit ? (
              <Button type="submit" disabled={save.isPending}>
                {creating ? "Create event" : "Save changes"}
              </Button>
            ) : null}
            {canEdit && !creating && selected ? (
              <Button type="button" variant="danger" onClick={() => remove.mutate(selected.originId)}>
                Delete
              </Button>
            ) : null}
          </form>
        )}
      </Drawer>

      {/* Invite Colleague Modal */}
      {inviteModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
          <div className="w-full max-w-md rounded-2xl border border-[#D1CCC2] dark:border-[#30323F] bg-white dark:bg-[#1E2028] p-6 shadow-2xl space-y-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Users className="h-5 w-5 text-blue-600 dark:text-blue-400" />
                <h3 className="text-base font-semibold text-[#111827] dark:text-white">Invite to Calendar</h3>
              </div>
              <button
                type="button"
                onClick={() => {
                  setInviteModalOpen(false);
                  setInviteSentFeedback(false);
                }}
                className="p-1 rounded-md text-[#9CA3AF] hover:text-[#111827] dark:hover:text-white"
              >
                <X className="h-4 w-4" />
              </button>
            </div>

            <p className="text-xs text-[#6B7280] dark:text-[#9CA3AF]">
              Share your calendar with team members so they can view and collaborate on events and meetings.
            </p>

            {inviteSentFeedback ? (
              <div className="p-3 rounded-lg bg-emerald-50 dark:bg-emerald-950/30 border border-emerald-200 dark:border-emerald-800 text-xs text-emerald-700 dark:text-emerald-300 flex items-center gap-2">
                <Check className="h-4 w-4" />
                Invitation sent successfully!
              </div>
            ) : (
              <div className="space-y-3">
                <Input
                  type="email"
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                  placeholder="colleague@worknexa.com"
                  required
                />
                <Button
                  className="w-full"
                  onClick={() => {
                    if (inviteEmail.trim()) {
                      setInviteSentFeedback(true);
                      setTimeout(() => {
                        setInviteModalOpen(false);
                        setInviteSentFeedback(false);
                        setInviteEmail("");
                      }, 1800);
                    }
                  }}
                >
                  Send Invitation
                </Button>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

// ==========================================
// Week Timeline Grid (Matching Reference UI)
// ==========================================
function WeekTimelineGrid({
  from,
  rows,
  onOpen,
  onPrev,
  onNext,
}: {
  from: Date;
  rows: CalendarEntry[];
  onOpen: (entry: CalendarEntry) => void;
  onPrev: () => void;
  onNext: () => void;
}) {
  const days = Array.from({ length: 7 }, (_, i) => addDays(from, i));
  const today = new Date();

  // Current time scrubber calculations
  const now = new Date();
  const currentHourFraction = now.getHours() + now.getMinutes() / 60;
  const isTimeInView = currentHourFraction >= START_HOUR && currentHourFraction <= END_HOUR + 1;
  const currentTop = (currentHourFraction - START_HOUR) * HOUR_HEIGHT;

  return (
    <div className="overflow-x-auto select-none">
      <div className="min-w-[900px]">
        {/* Header Row: Navigation Arrows + 7 Day Columns */}
        <div className="grid grid-cols-[70px_repeat(7,1fr)] border-b border-[#E5E7EB] dark:border-[#2e303a] bg-white dark:bg-[#1a1b23]">
          {/* Top-Left Arrow Navigation Box */}
          <div className="flex items-center justify-center border-r border-[#E5E7EB] dark:border-[#2e303a] p-2">
            <div className="inline-flex rounded-md border border-[#D1CCC2] dark:border-[#2e303a] bg-white dark:bg-[#16171d] shadow-2xs">
              <button
                type="button"
                onClick={onPrev}
                aria-label="Previous week"
                className="p-1 text-[#6B7280] hover:text-[#111827] dark:hover:text-white border-r border-[#D1CCC2] dark:border-[#2e303a] hover:bg-[#F3F4F6] dark:hover:bg-white/5 transition-colors cursor-pointer"
              >
                <ChevronLeft className="h-3.5 w-3.5" />
              </button>
              <button
                type="button"
                onClick={onNext}
                aria-label="Next week"
                className="p-1 text-[#6B7280] hover:text-[#111827] dark:hover:text-white hover:bg-[#F3F4F6] dark:hover:bg-white/5 transition-colors cursor-pointer"
              >
                <ChevronRight className="h-3.5 w-3.5" />
              </button>
            </div>
          </div>

          {/* 7 Day Column Headers */}
          {days.map((day) => {
            const isToday = isSameDay(day, today);
            return (
              <div
                key={day.toISOString()}
                className={cn(
                  "py-3 px-2 text-center border-r border-[#E5E7EB] dark:border-[#2e303a] transition-colors",
                  isToday
                    ? "bg-[#F3F4F6] dark:bg-[#282932] font-bold text-[#111827] dark:text-white"
                    : "text-[#6B7280] dark:text-[#9CA3AF]"
                )}
              >
                <span className="text-[11px] sm:text-xs font-semibold tracking-wider">
                  {format(day, "EEE d").toUpperCase()}
                </span>
              </div>
            );
          })}
        </div>

        {/* Timeline Body: Hours on left, Day columns on right */}
        <div className="relative grid grid-cols-[70px_repeat(7,1fr)]">
          {/* Time Labels Column */}
          <div className="border-r border-[#E5E7EB] dark:border-[#2e303a] bg-white dark:bg-[#16171d]">
            {HOURS.map((hour) => {
              const displayHour = hour % 12 === 0 ? 12 : hour % 12;
              const ampm = hour >= 12 ? "PM" : "AM";
              return (
                <div
                  key={hour}
                  style={{ height: `${HOUR_HEIGHT}px` }}
                  className="relative border-b border-[#E5E7EB] dark:border-[#2e303a] pr-2 text-right"
                >
                  <span className="text-[11px] font-medium text-[#9CA3AF] -translate-y-2 block">
                    {displayHour} {ampm}
                  </span>
                </div>
              );
            })}

            {/* Current time pill badge on time axis */}
            {isTimeInView && (
              <div
                className="absolute right-1 z-30 -translate-y-1/2"
                style={{ top: `${currentTop}px` }}
              >
                <span className="rounded-md border border-[#D1CCC2] dark:border-[#30323F] bg-white dark:bg-[#1E2028] px-1.5 py-0.5 text-[10px] font-semibold text-[#4B5563] dark:text-[#E5E7EB] shadow-xs">
                  {format(now, "h.mm a")}
                </span>
              </div>
            )}
          </div>

          {/* 7 Day Columns with Grid Lines & Events */}
          {days.map((day) => {
            const isToday = isSameDay(day, today);
            const dayEvents = rows.filter((entry) => covers(entry, day) && !entry.isAllDay);
            const allDayEvents = rows.filter((entry) => covers(entry, day) && entry.isAllDay);

            return (
              <div
                key={day.toISOString()}
                className={cn(
                  "relative border-r border-[#E5E7EB] dark:border-[#2e303a]",
                  isToday && "bg-[#F9FAFB]/40 dark:bg-white/[0.02]"
                )}
              >
                {/* Horizontal Hour Grid Lines */}
                {HOURS.map((hour) => (
                  <div
                    key={hour}
                    style={{ height: `${HOUR_HEIGHT}px` }}
                    className="border-b border-[#E5E7EB] dark:border-[#2e303a]"
                  />
                ))}

                {/* All-Day Events Strip at the very top */}
                {allDayEvents.length > 0 && (
                  <div className="absolute top-1 left-1 right-1 z-20 space-y-1">
                    {allDayEvents.map((event) => {
                      const style = getEventCardStyle(event);
                      return (
                        <button
                          key={event.originId}
                          type="button"
                          onClick={() => onOpen(event)}
                          className={cn(
                            "w-full text-left px-2 py-0.5 rounded text-[10px] font-semibold truncate border shadow-2xs cursor-pointer",
                            style.card
                          )}
                        >
                          {event.title}
                        </button>
                      );
                    })}
                  </div>
                )}

                {/* Current Time Line across Today's Column */}
                {isToday && isTimeInView && (
                  <div
                    className="absolute left-0 right-0 z-20 pointer-events-none flex items-center"
                    style={{ top: `${currentTop}px` }}
                  >
                    <div className="w-1.5 h-1.5 rounded-full bg-indigo-600 dark:bg-indigo-400" />
                    <div className="h-[2px] bg-indigo-500/80 dark:bg-indigo-400/90 flex-1" />
                    <div className="w-1.5 h-1.5 rounded-full bg-indigo-600 dark:bg-indigo-400" />
                  </div>
                )}

                {/* Timed Events Rendered with Calculated Offsets & Heights */}
                {dayEvents.map((event) => {
                  const start = new Date(event.startAt);
                  const end = new Date(event.endAt);
                  const startFrac = Math.max(START_HOUR, start.getHours() + start.getMinutes() / 60);
                  const endFrac = Math.min(END_HOUR + 1, end.getHours() + end.getMinutes() / 60);
                  const durationHours = Math.max(0.5, endFrac - startFrac);

                  const top = (startFrac - START_HOUR) * HOUR_HEIGHT + 3;
                  const height = Math.max(38, durationHours * HOUR_HEIGHT - 6);
                  const style = getEventCardStyle(event);

                  return (
                    <button
                      key={`${event.originId}-${event.startAt}`}
                      type="button"
                      style={{ top: `${top}px`, height: `${height}px` }}
                      onClick={() => onOpen(event)}
                      className={cn(
                        "absolute left-1 right-1 z-10 text-left p-2 rounded-lg border shadow-2xs transition-all hover:scale-[1.01] hover:shadow-md cursor-pointer overflow-hidden flex flex-col justify-start",
                        style.card
                      )}
                    >
                      {/* Time pill badge inside card */}
                      <span
                        className={cn(
                          "inline-block w-fit px-1.5 py-0.5 rounded-md text-[10px] font-semibold tracking-tight uppercase leading-none mb-1",
                          style.badge
                        )}
                      >
                        {formatEventTime(event.startAt, event.endAt, event.isAllDay)}
                      </span>

                      {/* Event Title */}
                      <span className={cn("text-xs font-semibold leading-snug line-clamp-2", style.title)}>
                        {event.title}
                      </span>
                    </button>
                  );
                })}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}

// ==========================================
// Day Timeline Grid (Day View)
// ==========================================
function DayTimelineGrid({
  day,
  rows,
  onOpen,
  onPrev,
  onNext,
}: {
  day: Date;
  rows: CalendarEntry[];
  onOpen: (entry: CalendarEntry) => void;
  onPrev: () => void;
  onNext: () => void;
}) {
  const dayEvents = rows.filter((entry) => covers(entry, day));

  return (
    <div className="p-4 sm:p-6 space-y-4">
      <div className="flex items-center justify-between border-b border-[#E5E7EB] dark:border-[#2e303a] pb-3">
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={onPrev}
            className="p-1 rounded-md border border-[#D1CCC2] dark:border-[#2e303a] text-[#4B5563] hover:bg-[#F3F4F6] dark:hover:bg-white/5 cursor-pointer"
          >
            <ChevronLeft className="h-4 w-4" />
          </button>
          <button
            type="button"
            onClick={onNext}
            className="p-1 rounded-md border border-[#D1CCC2] dark:border-[#2e303a] text-[#4B5563] hover:bg-[#F3F4F6] dark:hover:bg-white/5 cursor-pointer"
          >
            <ChevronRight className="h-4 w-4" />
          </button>
          <h3 className="text-base font-semibold text-[#111827] dark:text-white">
            {format(day, "EEEE, d MMMM yyyy")}
          </h3>
        </div>
        <span className="text-xs text-[#6B7280] dark:text-[#9CA3AF] font-medium">
          {dayEvents.length} scheduled item{dayEvents.length === 1 ? "" : "s"}
        </span>
      </div>

      {dayEvents.length === 0 ? (
        <div className="py-12">
          <EmptyState title="Nothing scheduled" description="No events or meetings planned for this day." />
        </div>
      ) : (
        <div className="space-y-3">
          {dayEvents.map((event) => {
            const style = getEventCardStyle(event);
            return (
              <button
                key={`${event.originId}-${event.startAt}`}
                type="button"
                onClick={() => onOpen(event)}
                className={cn(
                  "w-full text-left p-3.5 rounded-xl border shadow-xs transition-all hover:shadow-md cursor-pointer flex flex-col sm:flex-row sm:items-center justify-between gap-3",
                  style.card
                )}
              >
                <div>
                  <span className={cn("px-2 py-0.5 rounded-md text-[10px] font-semibold uppercase", style.badge)}>
                    {formatEventTime(event.startAt, event.endAt, event.isAllDay)}
                  </span>
                  <h4 className={cn("text-sm font-semibold mt-1", style.title)}>{event.title}</h4>
                  {event.description && (
                    <p className="text-xs text-[#6B7280] dark:text-[#9CA3AF] mt-1 line-clamp-2">
                      {event.description}
                    </p>
                  )}
                </div>
                {event.projectName && (
                  <span className="text-xs font-medium text-[#4B5563] dark:text-[#9CA3AF] shrink-0">
                    {event.projectName}
                  </span>
                )}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}

// ==========================================
// Month Grid (Month View)
// ==========================================
function MonthGrid({
  cursor,
  rows,
  onOpen,
  onPrev,
  onNext,
}: {
  cursor: Date;
  rows: CalendarEntry[];
  onOpen: (entry: CalendarEntry) => void;
  onPrev: () => void;
  onNext: () => void;
}) {
  const days = eachDayOfInterval({
    start: startOfWeek(startOfMonth(cursor), { weekStartsOn: 1 }),
    end: endOfWeek(endOfMonth(cursor), { weekStartsOn: 1 }),
  });

  return (
    <div className="overflow-x-auto select-none">
      <div className="min-w-[720px]">
        {/* Month Navigation Row */}
        <div className="flex items-center justify-between p-3 border-b border-[#E5E7EB] dark:border-[#2e303a] bg-[#F9FAFB] dark:bg-[#1a1b23]">
          <span className="text-xs font-semibold text-[#111827] dark:text-white uppercase tracking-wider">
            {format(cursor, "MMMM yyyy")}
          </span>
          <div className="inline-flex rounded-md border border-[#D1CCC2] dark:border-[#2e303a] bg-white dark:bg-[#16171d]">
            <button
              type="button"
              onClick={onPrev}
              className="p-1 text-[#6B7280] hover:text-[#111827] dark:hover:text-white border-r border-[#D1CCC2] dark:border-[#2e303a] cursor-pointer"
            >
              <ChevronLeft className="h-3.5 w-3.5" />
            </button>
            <button
              type="button"
              onClick={onNext}
              className="p-1 text-[#6B7280] hover:text-[#111827] dark:hover:text-white cursor-pointer"
            >
              <ChevronRight className="h-3.5 w-3.5" />
            </button>
          </div>
        </div>

        {/* Days Header */}
        <div className="grid grid-cols-7 border-b border-[#E5E7EB] dark:border-[#2e303a] bg-white dark:bg-[#16171d] text-[11px] font-semibold text-[#6B7280] dark:text-[#9CA3AF] uppercase text-center py-2.5">
          {["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map((label) => (
            <div key={label}>{label}</div>
          ))}
        </div>

        {/* Day Grid Cells */}
        <div className="grid grid-cols-7">
          {days.map((day) => {
            const items = rows.filter((entry) => covers(entry, day));
            const isToday = isSameDay(day, new Date());
            const inCurrentMonth = isSameMonth(day, cursor);

            return (
              <div
                key={day.toISOString()}
                className={cn(
                  "min-h-28 border-r border-b border-[#E5E7EB] dark:border-[#2e303a] p-2 transition-colors",
                  inCurrentMonth ? "bg-white dark:bg-[#16171d]" : "bg-[#F9FAFB]/60 dark:bg-[#13141a]/40 text-[#9CA3AF]"
                )}
              >
                <div className="flex items-center justify-between mb-1.5">
                  <span
                    className={cn(
                      "text-xs font-semibold rounded-md px-1.5 py-0.5",
                      isToday
                        ? "bg-[#111827] text-white dark:bg-white dark:text-[#111827]"
                        : "text-[#4B5563] dark:text-[#9CA3AF]"
                    )}
                  >
                    {format(day, "d")}
                  </span>
                  {items.length > 0 && (
                    <span className="text-[10px] text-[#9CA3AF] font-mono">{items.length}</span>
                  )}
                </div>

                <div className="space-y-1">
                  {items.slice(0, 3).map((entry) => {
                    const style = getEventCardStyle(entry);
                    return (
                      <button
                        key={`${entry.originId}-${entry.startAt}`}
                        type="button"
                        onClick={() => onOpen(entry)}
                        className={cn(
                          "w-full text-left truncate px-1.5 py-0.5 rounded text-[10px] font-medium border shadow-2xs cursor-pointer block",
                          style.card
                        )}
                      >
                        {entry.isAllDay ? entry.title : `${format(new Date(entry.startAt), "HH:mm")} ${entry.title}`}
                      </button>
                    );
                  })}
                  {items.length > 3 && (
                    <p className="text-[10px] font-medium text-[#6B7280] dark:text-[#9CA3AF] pl-1">
                      +{items.length - 3} more
                    </p>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}

// ==========================================
// Agenda View
// ==========================================
function Agenda({ rows, onOpen }: { rows: CalendarEntry[]; onOpen: (entry: CalendarEntry) => void }) {
  const groups = new Map<string, CalendarEntry[]>();
  for (const entry of rows) {
    const key = format(new Date(entry.startAt), "yyyy-MM-dd");
    groups.set(key, [...(groups.get(key) ?? []), entry]);
  }

  if (rows.length === 0) {
    return (
      <div className="p-8">
        <EmptyState title="Nothing scheduled" description="No events or meetings scheduled in this period." />
      </div>
    );
  }

  return (
    <div className="p-4 sm:p-6 space-y-6">
      {[...groups.entries()].map(([key, items]) => (
        <section key={key} className="space-y-2">
          <h3 className="text-xs font-semibold uppercase tracking-wider text-[#6B7280] dark:text-[#9CA3AF] border-b border-[#E5E7EB] dark:border-[#2e303a] pb-1">
            {format(new Date(items[0].startAt), "EEEE, d MMMM yyyy")}
          </h3>
          <div className="space-y-2">
            {items.map((entry) => {
              const style = getEventCardStyle(entry);
              return (
                <button
                  key={`${entry.originId}-${entry.startAt}`}
                  type="button"
                  onClick={() => onOpen(entry)}
                  className={cn(
                    "flex w-full items-center justify-between gap-3 rounded-xl border p-3 text-left transition-all hover:shadow-xs cursor-pointer",
                    style.card
                  )}
                >
                  <div>
                    <h4 className={cn("text-sm font-semibold", style.title)}>{entry.title}</h4>
                    <span className="text-xs text-[#6B7280] dark:text-[#9CA3AF]">
                      {formatEventTime(entry.startAt, entry.endAt, entry.isAllDay)}
                    </span>
                  </div>
                  <span className={cn("rounded-md px-2 py-0.5 text-[11px] font-semibold", style.badge)}>
                    {entry.kind}
                  </span>
                </button>
              );
            })}
          </div>
        </section>
      ))}
    </div>
  );
}

// ==========================================
// Read-Only Preview for Tasks / Meetings
// ==========================================
function ReadOnlyEntry({ entry }: { entry: CalendarEntry }) {
  return (
    <div className="space-y-3 text-sm">
      <span className={cn("inline-flex rounded-md px-2 py-0.5 text-[11px] font-medium", kindClassName(entry.kind))}>
        {entry.kind}
      </span>
      <p className="text-[#111827] dark:text-white font-medium">
        {entry.isAllDay
          ? "All day"
          : `${format(new Date(entry.startAt), "d MMM, HH:mm")} – ${format(new Date(entry.endAt), "d MMM, HH:mm")}`}
      </p>
      {entry.description ? (
        <p className="text-xs text-[#6B7280] dark:text-[#9CA3AF] leading-relaxed">{entry.description}</p>
      ) : null}
      {entry.location ? (
        <p className="text-xs text-[#4B5563] dark:text-[#9CA3AF]">
          Location: <span className="font-medium text-[#111827] dark:text-white">{entry.location}</span>
        </p>
      ) : null}
      {safeHref(entry.meetingLink) ? (
        <a className="text-xs text-accent underline block" href={safeHref(entry.meetingLink)} target="_blank" rel="noreferrer">
          Join Meeting Link
        </a>
      ) : null}
      {entry.projectName ? (
        <p className="text-xs text-[#4B5563] dark:text-[#9CA3AF]">
          Project: <span className="font-medium text-[#111827] dark:text-white">{entry.projectName}</span>
        </p>
      ) : null}
      {entry.origin === "task" ? (
        <p className="text-xs text-[#6B7280] dark:text-[#9CA3AF] italic">This event originates from a task.</p>
      ) : (
        <Link className="text-xs font-semibold text-accent underline block" to={`/meetings?meeting=${entry.originId}`}>
          Open this meeting
        </Link>
      )}
    </div>
  );
}
