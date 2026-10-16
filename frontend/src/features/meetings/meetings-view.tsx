import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  addDays,
  addMonths,
  eachDayOfInterval,
  endOfMonth,
  endOfWeek,
  format,
  isSameDay,
  isSameMonth,
  startOfMonth,
  startOfWeek,
  subMonths,
} from "date-fns";
import {
  CalendarDays,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Clock,
  ExternalLink,
  FileText,
  ListChecks,
  MapPin,
  Plus,
  Users,
  Video,
} from "lucide-react";
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
import {
  deleteMeeting,
  fetchMeeting,
  fetchMeetings,
  localDateTime,
  saveMeeting,
  type MeetingActionWrite,
  type MeetingDetail,
  type MeetingListItem,
  type MeetingWrite,
} from "./api.ts";
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
  const meetings = useQuery({
    queryKey: ["meetings", view, date],
    queryFn: () => fetchMeetings(view, date),
    placeholderData: keepPreviousData,
  });
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

  // Days ribbon window (8 consecutive days centered or starting relative to cursor)
  const carouselDays = useMemo(() => {
    const start = addDays(cursor, -1);
    return Array.from({ length: 8 }, (_, i) => addDays(start, i));
  }, [cursor]);

  // Months for the quick top month tabs (previous, current, next)
  const monthTabs = useMemo(() => {
    const prev = subMonths(cursor, 1);
    const next = addMonths(cursor, 1);
    return [
      { date: prev, label: format(prev, "MMMM") },
      { date: cursor, label: format(cursor, "MMMM"), isCurrent: true },
      { date: next, label: format(next, "MMMM") },
    ];
  }, [cursor]);

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
    <div className="w-full space-y-6">
      {/* Top Header */}
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 text-xs font-mono font-medium text-[#4B5563] mb-1">
            <span className="inline-block w-2 h-2 rounded-full bg-[#10B981]" />
            <span>Worknexa // Synchronized Schedule</span>
          </div>
          <h1 className="wx-headline-lg text-[#111827]">Meetings</h1>
          <p className="mt-1 text-sm leading-[20px] text-[#4B5563]">
            {view === "calendar" ? format(cursor, "MMMM yyyy") : format(cursor, "EEEE, d MMMM yyyy")}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Button type="button" variant="secondary" size="sm" onClick={() => shift(-1)}>
            <ChevronLeft className="w-3.5 h-3.5 mr-0.5 inline-block" />
            Previous
          </Button>
          <Button type="button" variant="secondary" size="sm" onClick={() => setCursor(new Date())}>
            Today
          </Button>
          <Button type="button" variant="secondary" size="sm" onClick={() => shift(1)}>
            Next
            <ChevronRight className="w-3.5 h-3.5 ml-0.5 inline-block" />
          </Button>
          {manageable ? (
            <Button
              type="button"
              onClick={() => {
                setCreating(true);
                setSelectedId(null);
                setDraft(blankDraft(cursor));
                setMessage(null);
                setParams({});
              }}
            >
              <Plus className="w-4 h-4 mr-1 inline-block" />
              New meeting
            </Button>
          ) : null}
        </div>
      </header>

      {/* Navigation Bar: Month Jumpers & View Switcher */}
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[#D1CCC2]/60 pb-3">
        {/* Month Selector Pills (from reference image: April, May, June, Today) */}
        <div className="flex items-center gap-1 sm:gap-2 text-sm font-medium">
          {monthTabs.map((month) => {
            const isSelected = isSameMonth(month.date, cursor);
            return (
              <button
                key={month.label}
                type="button"
                onClick={() => setCursor(startOfMonth(month.date))}
                className={cn(
                  "px-3 py-1 rounded-full text-xs font-medium transition-colors cursor-pointer",
                  isSelected
                    ? "bg-[#111827] text-white shadow-xs"
                    : "text-[#4B5563] hover:text-[#111827] hover:bg-[#DFDCD6]/80"
                )}
              >
                {month.label}
              </button>
            );
          })}
          <button
            type="button"
            onClick={() => setCursor(new Date())}
            className="px-3 py-1 rounded-full text-xs font-medium text-[#4B5563] hover:text-[#111827] hover:bg-[#DFDCD6]/80 transition-colors cursor-pointer"
          >
            Today
          </button>
        </div>

        {/* View Switcher Tabs */}
        <div className="flex flex-wrap gap-1.5" role="tablist" aria-label="Meeting views">
          {meetingViews.map((item) => (
            <Button
              key={item.id}
              type="button"
              size="sm"
              variant={view === item.id ? "primary" : "secondary"}
              onClick={() => setView(item.id)}
            >
              {item.label}
            </Button>
          ))}
        </div>
      </div>

      {/* Date Ribbon Carousel Strip (Identical to Reference Image) */}
      {view !== "calendar" && (
        <div className="relative w-full">
          <div className="flex items-center gap-2 sm:gap-3 w-full">
            {/* Backward Date Move Button (Left) */}
            <button
              type="button"
              onClick={() => shift(-1)}
              aria-label="Previous day"
              className="flex items-center justify-center min-w-[38px] sm:min-w-[46px] h-[106px] rounded-[12px] border border-[#D1CCC2] bg-[#DFDCD6]/70 hover:bg-white text-[#111827] hover:border-[#111827]/40 transition-colors cursor-pointer shadow-xs flex-shrink-0"
            >
              <ChevronLeft className="w-5 h-5 text-[#4B5563]" />
            </button>

            {/* Day Cards Spanning Full Screen Width */}
            <div className="flex items-center gap-2 sm:gap-3 flex-1 min-w-0 overflow-x-auto no-scrollbar py-1">
              {carouselDays.map((day) => {
                const isSelected = isSameDay(day, cursor);
                // Calculate meetings count on this day
                const dayMeetingsCount = isSelected
                  ? meetings.data.length
                  : meetings.data.filter((m) => {
                      const mDate = new Date(m.startAt);
                      return isSameDay(mDate, day);
                    }).length;

                return (
                  <button
                    key={day.toISOString()}
                    type="button"
                    onClick={() => setCursor(day)}
                    className={cn(
                      "flex flex-col items-center justify-between flex-1 min-w-[72px] sm:min-w-[88px] h-[106px] px-2 py-2.5 rounded-[12px] border transition-all cursor-pointer relative",
                      isSelected
                        ? "bg-white border-[#111827] shadow-[0_4px_12px_rgba(17,24,39,0.1)] -translate-y-0.5"
                        : "bg-[#DFDCD6]/60 border-[#D1CCC2] hover:bg-white/90 hover:border-[#111827]/40"
                    )}
                  >
                    {/* Top: Day and Month */}
                    <div className="text-[12px] font-medium text-[#4B5563] flex items-center gap-1">
                      <span className="font-semibold text-[#111827]">{format(day, "EEE")}</span>
                      <span>{format(day, "MMM")}</span>
                    </div>

                    {/* Center: Large Day Number */}
                    <div
                      className={cn(
                        "text-2xl font-bold font-mono tracking-tight",
                        isSelected ? "text-[#E11D48]" : "text-[#111827]"
                      )}
                    >
                      {format(day, "dd")}
                    </div>

                    {/* Bottom: Pill Badge (Active Red pill vs Neutral pill) */}
                    <div
                      className={cn(
                        "flex items-center justify-center gap-1.5 w-full py-0.5 px-2 rounded-full text-[11px] font-mono font-semibold transition-colors",
                        isSelected
                          ? "bg-[#EF4444] text-white shadow-xs"
                          : "bg-[#D7D3CB] text-[#4B5563]"
                      )}
                    >
                      <CalendarDays className="w-3 h-3 flex-shrink-0" />
                      <span>{dayMeetingsCount}</span>
                    </div>

                    {/* Active Indicator Pointer */}
                    {isSelected && (
                      <div className="absolute -bottom-2 left-1/2 -translate-x-1/2 w-0 h-0 border-x-[6px] border-x-transparent border-t-[8px] border-t-white z-10" />
                    )}
                  </button>
                );
              })}
            </div>

            {/* Forward Date Move Button (Right) */}
            <button
              type="button"
              onClick={() => shift(1)}
              aria-label="Next day"
              className="flex items-center justify-center min-w-[38px] sm:min-w-[46px] h-[106px] rounded-[12px] border border-[#D1CCC2] bg-[#DFDCD6]/70 hover:bg-white text-[#111827] hover:border-[#111827]/40 transition-colors cursor-pointer shadow-xs flex-shrink-0"
            >
              <ChevronRight className="w-5 h-5 text-[#4B5563]" />
            </button>
          </div>
        </div>
      )}

      {/* Main Content Area */}
      {meetings.data.length === 0 ? (
        <div className="w-full rounded-[20px] border border-[#D1CCC2] bg-[#EAE6DE]/70 p-8 sm:p-14 shadow-xs">
          <EmptyState
            title="Nothing on this view"
            description="Switch the date or add a meeting with an agenda, decisions, and follow-up."
          />
        </div>
      ) : view === "calendar" ? (
        <MeetingMonth
          cursor={cursor}
          meetings={meetings.data}
          onOpen={(id) => {
            setCreating(false);
            setSelectedId(id);
            setParams({ meeting: id });
          }}
        />
      ) : (
        /* Connected Schedule Timeline Panel (matching reference image) */
        <div className="rounded-[20px] border border-[#D1CCC2] bg-[#EAE6DE]/70 p-3 sm:p-5 shadow-xs">
          <ul role="list" className="space-y-4">
            {meetings.data.map((meeting) => {
              const startDate = new Date(meeting.startAt);
              const timeFormatted = format(startDate, "h:mm");
              const ampmFormatted = format(startDate, "a").toLowerCase();

              return (
                <li
                  key={meeting.id}
                  className="flex flex-col md:flex-row items-stretch gap-3 md:gap-4"
                >
                  {/* Left Column: Time Indicator (e.g. 5:00 am, 1:00 pm, 3:43 pm) */}
                  <div className="flex md:flex-col items-center justify-between md:justify-center md:w-20 lg:w-24 flex-shrink-0 px-2 py-1 text-left md:text-center">
                    <div className="flex md:flex-col items-baseline md:items-center gap-1 md:gap-0">
                      <span className="text-base lg:text-lg font-bold font-mono text-[#111827] leading-none">
                        {timeFormatted}
                      </span>
                      <span className="text-xs font-semibold font-mono text-[#4B5563] uppercase">
                        {ampmFormatted}
                      </span>
                    </div>
                    <span className="text-[11px] font-mono text-[#6B7280] bg-[#DFDCD6] px-2 py-0.5 rounded-full mt-1">
                      {meeting.durationMinutes}m
                    </span>
                  </div>

                  {/* Right Column: Modular Card with 6 Category Columns */}
                  <div className="flex-1 rounded-[14px] border border-[#D1CCC2]/70 bg-white p-3.5 sm:p-4 shadow-[0_2px_8px_rgba(17,24,39,0.04)] hover:shadow-md hover:border-[#111827]/40 transition-all">
                    {/* Header Row: Title, Meeting Type, Project & Quick Actions */}
                    <div className="flex flex-wrap items-start justify-between gap-2.5 pb-3 border-b border-[#E5E7EB]">
                      <div className="space-y-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <button
                            type="button"
                            className="text-base sm:text-lg font-bold text-[#111827] hover:text-[#000] text-left cursor-pointer transition-colors"
                            onClick={() => {
                              setCreating(false);
                              setSelectedId(meeting.id);
                              setParams({ meeting: meeting.id });
                            }}
                          >
                            {meeting.title}
                          </button>
                          <span className="rounded-[6px] bg-[#FED7AA] px-2.5 py-0.5 text-[11px] font-mono font-semibold text-[#111827] border border-[#111827]/15">
                            {meetingTypeLabel(meeting.meetingType)}
                          </span>
                        </div>

                        <div className="flex flex-wrap items-center gap-3 text-xs text-[#4B5563]">
                          <span className="font-mono tabular-nums">
                            {format(startDate, "d MMM, HH:mm")} · {meeting.durationMinutes} min
                          </span>
                          {meeting.projectName && (
                            <span className="font-medium bg-[#DFDCD6] px-2 py-0.5 rounded-[4px] text-[#111827]">
                              {meeting.projectName}
                            </span>
                          )}
                          {meeting.teamName && (
                            <span className="font-medium text-[#4B5563]">
                              Team: {meeting.teamName}
                            </span>
                          )}
                        </div>
                      </div>

                      {/* Quick Actions */}
                      <div className="flex items-center gap-2">
                        {meeting.meetingLink ? (
                          <a
                            href={meeting.meetingLink}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium rounded-[6px] bg-[#10B981]/15 text-[#059669] hover:bg-[#10B981]/25 transition-colors"
                          >
                            <Video className="w-3.5 h-3.5" />
                            <span>Join</span>
                          </a>
                        ) : null}

                        <Link
                          to={`/meeting-notes?meeting=${meeting.id}`}
                          className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium rounded-[6px] bg-[#DFDCD6] text-[#111827] hover:bg-[#D7D3CB] transition-colors"
                        >
                          <FileText className="w-3.5 h-3.5" />
                          <span>Notes</span>
                        </Link>

                        <button
                          type="button"
                          onClick={() => {
                            setCreating(false);
                            setSelectedId(meeting.id);
                            setParams({ meeting: meeting.id });
                          }}
                          className="px-2.5 py-1 text-xs font-medium rounded-[6px] border border-[#D1CCC2] text-[#111827] hover:border-[#111827] transition-colors cursor-pointer"
                        >
                          Details
                        </button>
                      </div>
                    </div>

                    {/* 6 Modular Category Columns (Exact Match to Reference Layout) */}
                    <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-2 sm:gap-3 pt-3">
                      {/* Column 1: Agenda (Violet) */}
                      <div className="flex items-start gap-2.5 p-2 rounded-[8px] bg-[#FAF8F5] border border-[#EFECE6]">
                        <div className="w-7 h-7 rounded-full bg-purple-100 text-purple-600 flex items-center justify-center flex-shrink-0 mt-0.5">
                          <ListChecks className="w-4 h-4" />
                        </div>
                        <div className="min-w-0">
                          <div className="text-[11px] font-semibold text-[#111827] truncate">Agenda</div>
                          <div className="text-[11px] text-[#6B7280] truncate">
                            {meeting.location ? "Scheduled" : "Planned"}
                          </div>
                        </div>
                      </div>

                      {/* Column 2: Attendees (Pink) */}
                      <div className="flex items-start gap-2.5 p-2 rounded-[8px] bg-[#FAF8F5] border border-[#EFECE6]">
                        <div className="w-7 h-7 rounded-full bg-pink-100 text-pink-600 flex items-center justify-center flex-shrink-0 mt-0.5">
                          <Users className="w-4 h-4" />
                        </div>
                        <div className="min-w-0">
                          <div className="text-[11px] font-semibold text-[#111827] truncate">Attendees</div>
                          <div className="text-[11px] text-[#6B7280] truncate">
                            {meeting.participants.length > 0
                              ? `${meeting.participants.length} invited`
                              : "Open invite"}
                          </div>
                        </div>
                      </div>

                      {/* Column 3: Action Items (Green) */}
                      <div className="flex items-start gap-2.5 p-2 rounded-[8px] bg-[#FAF8F5] border border-[#EFECE6]">
                        <div className="w-7 h-7 rounded-full bg-emerald-100 text-emerald-600 flex items-center justify-center flex-shrink-0 mt-0.5">
                          <CheckCircle2 className="w-4 h-4" />
                        </div>
                        <div className="min-w-0">
                          <div className="text-[11px] font-semibold text-[#111827] truncate">Actions</div>
                          <div className="text-[11px] text-[#6B7280] truncate">Tracked tasks</div>
                        </div>
                      </div>

                      {/* Column 4: Location (Cyan) */}
                      <div className="flex items-start gap-2.5 p-2 rounded-[8px] bg-[#FAF8F5] border border-[#EFECE6]">
                        <div className="w-7 h-7 rounded-full bg-cyan-100 text-cyan-600 flex items-center justify-center flex-shrink-0 mt-0.5">
                          {meeting.meetingLink ? <Video className="w-4 h-4" /> : <MapPin className="w-4 h-4" />}
                        </div>
                        <div className="min-w-0">
                          <div className="text-[11px] font-semibold text-[#111827] truncate">Location</div>
                          <div className="text-[11px] text-[#6B7280] truncate">
                            {meeting.location || (meeting.meetingLink ? "Virtual Call" : "Conference Room")}
                          </div>
                        </div>
                      </div>

                      {/* Column 5: Notes & Decisions (Blue) */}
                      <div className="flex items-start gap-2.5 p-2 rounded-[8px] bg-[#FAF8F5] border border-[#EFECE6]">
                        <div className="w-7 h-7 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center flex-shrink-0 mt-0.5">
                          <FileText className="w-4 h-4" />
                        </div>
                        <div className="min-w-0">
                          <div className="text-[11px] font-semibold text-[#111827] truncate">Notes</div>
                          <div className="text-[11px] text-[#6B7280] truncate">Live record</div>
                        </div>
                      </div>

                      {/* Column 6: Follow-up (Amber) */}
                      <div className="flex items-start gap-2.5 p-2 rounded-[8px] bg-[#FAF8F5] border border-[#EFECE6]">
                        <div className="w-7 h-7 rounded-full bg-amber-100 text-amber-600 flex items-center justify-center flex-shrink-0 mt-0.5">
                          <Clock className="w-4 h-4" />
                        </div>
                        <div className="min-w-0">
                          <div className="text-[11px] font-semibold text-[#111827] truncate">Follow-up</div>
                          <div className="text-[11px] text-[#6B7280] truncate">
                            {meeting.durationMinutes}m slot
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </li>
              );
            })}
          </ul>
        </div>
      )}

      {/* Meeting Drawer for Create & Edit */}
      <Drawer
        open={drawerOpen}
        title={creating ? "New meeting" : draft.title || "Meeting"}
        onClose={() => {
          setCreating(false);
          setSelectedId(null);
          setParams({});
        }}
        className="max-w-lg"
      >
        <form
          className="space-y-4"
          onSubmit={(event) => {
            event.preventDefault();
            void save.mutateAsync({ id: creating ? undefined : selectedId ?? undefined, meeting: toWrite(draft) });
          }}
        >
          {message ? <p className="text-sm text-danger">{message}</p> : null}
          {selectedId && !creating ? (
            <Link className="text-sm text-accent font-medium hover:underline inline-flex items-center gap-1" to={`/meeting-notes?meeting=${selectedId}`}>
              <ExternalLink className="w-3.5 h-3.5" />
              Open meeting notes
            </Link>
          ) : null}
          <Field label="Title">
            {({ id }) => (
              <Input
                id={id}
                value={draft.title}
                onChange={(event) => patch({ title: event.target.value })}
                required
                maxLength={300}
                disabled={!editable}
              />
            )}
          </Field>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Type">
              {({ id }) => (
                <Select
                  id={id}
                  value={draft.meetingType}
                  onChange={(event) => patch({ meetingType: event.target.value })}
                  disabled={!editable}
                >
                  {meetingTypes.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.label}
                    </option>
                  ))}
                </Select>
              )}
            </Field>
            <Field label="Duration" hint="Minutes">
              {({ id }) => (
                <Input
                  id={id}
                  type="number"
                  min={5}
                  max={1440}
                  value={draft.durationMinutes}
                  onChange={(event) => patch({ durationMinutes: event.target.value })}
                  required
                  disabled={!editable}
                />
              )}
            </Field>
            <Field label="Date and time">
              {({ id }) => (
                <Input
                  id={id}
                  type="datetime-local"
                  value={draft.startAt}
                  onChange={(event) => patch({ startAt: event.target.value })}
                  required
                  disabled={!editable}
                />
              )}
            </Field>
            <Field label="Follow-up">
              {({ id }) => (
                <Input
                  id={id}
                  type="datetime-local"
                  value={draft.followUpAt}
                  onChange={(event) => patch({ followUpAt: event.target.value })}
                  disabled={!editable}
                />
              )}
            </Field>
            <Field label="Project">
              {({ id }) => (
                <Select
                  id={id}
                  value={draft.projectId}
                  onChange={(event) => patch({ projectId: event.target.value })}
                  disabled={!editable}
                >
                  <option value="">None</option>
                  {options.data?.projects.map((project) => (
                    <option key={project.id} value={project.id}>
                      {project.name}
                    </option>
                  ))}
                </Select>
              )}
            </Field>
            <Field label="Team">
              {({ id }) => (
                <Select
                  id={id}
                  value={draft.teamId}
                  onChange={(event) => patch({ teamId: event.target.value })}
                  disabled={!editable}
                >
                  <option value="">None</option>
                  {options.data?.teams.map((team) => (
                    <option key={team.id} value={team.id}>
                      {team.name}
                    </option>
                  ))}
                </Select>
              )}
            </Field>
          </div>
          <Field label="Location">
            {({ id }) => (
              <Input
                id={id}
                value={draft.location}
                onChange={(event) => patch({ location: event.target.value })}
                maxLength={300}
                disabled={!editable}
              />
            )}
          </Field>
          <Field label="Meeting link">
            {({ id }) => (
              <Input
                id={id}
                value={draft.meetingLink}
                onChange={(event) => patch({ meetingLink: event.target.value })}
                maxLength={500}
                disabled={!editable}
              />
            )}
          </Field>
          <Field label="Participants">
            {({ id }) => (
              <select
                id={id}
                multiple
                className="min-h-28 w-full rounded-lg border border-line bg-surface px-3 py-2 text-sm"
                value={draft.participantIds}
                onChange={(event) => patch({ participantIds: [...event.target.selectedOptions].map((option) => option.value) })}
                disabled={!editable}
              >
                {options.data?.people.map((person) => (
                  <option key={person.id} value={person.id}>
                    {person.name}
                  </option>
                ))}
              </select>
            )}
          </Field>
          <Field label="Agenda">
            {({ id }) => (
              <Textarea
                id={id}
                value={draft.agenda}
                onChange={(event) => patch({ agenda: event.target.value })}
                disabled={!editable}
              />
            )}
          </Field>
          <Field label="Notes">
            {({ id }) => (
              <Textarea
                id={id}
                value={draft.notes}
                onChange={(event) => patch({ notes: event.target.value })}
                disabled={!editable}
              />
            )}
          </Field>
          <Field label="Decisions">
            {({ id }) => (
              <Textarea
                id={id}
                value={draft.decisions}
                onChange={(event) => patch({ decisions: event.target.value })}
                disabled={!editable}
              />
            )}
          </Field>
          <ActionEditor
            items={draft.actionItems}
            people={options.data?.people ?? []}
            disabled={!editable}
            onChange={(actionItems) => patch({ actionItems })}
          />
          {editable ? (
            <Button type="submit" disabled={save.isPending}>
              {creating ? "Create meeting" : "Save changes"}
            </Button>
          ) : null}
          {editable && selectedId && !creating ? (
            <Button type="button" variant="danger" onClick={() => remove.mutate(selectedId)}>
              Delete
            </Button>
          ) : null}
        </form>
      </Drawer>
    </div>
  );
}

function ActionEditor({
  items,
  people,
  disabled,
  onChange,
}: {
  items: ActionDraft[];
  people: { id: string; name: string }[];
  disabled: boolean;
  onChange: (items: ActionDraft[]) => void;
}) {
  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <span className="text-[13px] font-medium text-[#111827]">Action items</span>
        {disabled ? null : (
          <Button
            type="button"
            size="sm"
            variant="secondary"
            onClick={() => onChange([...items, { title: "", assigneeId: "", dueAt: "", isCompleted: false }])}
          >
            Add
          </Button>
        )}
      </div>
      {items.map((item, index) => (
        <div key={index} className="grid grid-cols-1 gap-2 rounded-lg border border-line p-2 sm:grid-cols-[1fr_8rem_auto] bg-[#FAF8F5]">
          <Input
            aria-label="Action title"
            value={item.title}
            placeholder="What needs doing"
            onChange={(event) =>
              onChange(items.map((current, currentIndex) => (currentIndex === index ? { ...current, title: event.target.value } : current)))
            }
            disabled={disabled}
          />
          <Select
            aria-label="Assignee"
            value={item.assigneeId}
            onChange={(event) =>
              onChange(items.map((current, currentIndex) => (currentIndex === index ? { ...current, assigneeId: event.target.value } : current)))
            }
            disabled={disabled}
          >
            <option value="">Unassigned</option>
            {people.map((person) => (
              <option key={person.id} value={person.id}>
                {person.name}
              </option>
            ))}
          </Select>
          <label className="flex items-center gap-2 text-xs text-muted">
            <input
              type="checkbox"
              checked={item.isCompleted}
              onChange={(event) =>
                onChange(items.map((current, currentIndex) => (currentIndex === index ? { ...current, isCompleted: event.target.checked } : current)))
              }
              disabled={disabled}
            />
            Done
          </label>
        </div>
      ))}
    </div>
  );
}

function MeetingMonth({
  cursor,
  meetings,
  onOpen,
}: {
  cursor: Date;
  meetings: MeetingListItem[];
  onOpen: (id: string) => void;
}) {
  const today = useMemo(() => new Date(), []);
  const days = useMemo(() => {
    const start = startOfWeek(startOfMonth(cursor), { weekStartsOn: 1 });
    const end = endOfWeek(endOfMonth(cursor), { weekStartsOn: 1 });
    return eachDayOfInterval({ start, end });
  }, [cursor]);

  return (
    <div className="grid grid-cols-7 gap-1.5 p-3 rounded-[16px] border border-[#D1CCC2] bg-[#DFDCD6]/60">
      {["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map((label) => (
        <div key={label} className="px-1 py-1 text-[11px] font-mono font-semibold text-[#4B5563] text-center">
          {label}
        </div>
      ))}
      {days.map((day) => {
        const items = meetings.filter((meeting) => {
          const start = new Date(meeting.startAt);
          const end = new Date(meeting.endAt);
          const dayEnd = addDays(day, 1);
          return start < dayEnd && end > day;
        });
        const isCurrentMonth = isSameMonth(day, cursor);
        const isToday = isSameDay(day, today);

        return (
          <div
            key={day.toISOString()}
            className={cn(
              "min-h-24 rounded-lg border p-1.5 transition-colors",
              isCurrentMonth ? "bg-white border-[#D1CCC2]/60" : "bg-[#DFDCD6]/40 border-transparent opacity-60",
              isToday ? "ring-1 ring-[#111827]" : ""
            )}
          >
            <div className="flex items-center justify-between text-[11px] font-mono font-medium text-[#4B5563] mb-1">
              <span className={cn(isToday ? "bg-[#111827] text-white px-1 rounded-sm" : "")}>{format(day, "d")}</span>
              {items.length > 0 && <span className="text-[10px] text-[#6B7280]">{items.length}</span>}
            </div>
            <div className="space-y-1">
              {items.slice(0, 3).map((meeting) => (
                <button
                  key={meeting.id}
                  type="button"
                  className="block w-full truncate rounded-[4px] bg-[#FED7AA]/60 hover:bg-[#FED7AA] px-1.5 py-0.5 text-left text-[11px] font-medium text-[#111827] border border-[#111827]/10 transition-colors cursor-pointer"
                  onClick={() => onOpen(meeting.id)}
                >
                  {meeting.title}
                </button>
              ))}
            </div>
          </div>
        );
      })}
    </div>
  );
}
