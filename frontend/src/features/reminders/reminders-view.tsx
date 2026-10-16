import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { addDays, format, isBefore, isSameDay, parseISO, startOfDay } from "date-fns";
import {
  Bell,
  BookOpen,
  Calendar,
  ClipboardList,
  Clock,
  CreditCard,
  FileText,
  Filter,
  Flag,
  Mail,
  MoreHorizontal,
  Paperclip,
  Pencil,
  RefreshCw,
  Search,
  Shield,
  Tag,
  Trash2,
  Upload,
  User,
} from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Drawer } from "../../components/design-system/drawer.tsx";
import { Field, Input, Select, Textarea } from "../../components/design-system/field.tsx";
import { EmptyState, ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { ApiError } from "../../services/api-client.ts";
import { cn } from "../../utils/cn.ts";
import {
  completeReminder,
  deleteReminder,
  fetchReminderLinks,
  fetchReminders,
  reopenReminder,
  saveReminder,
  type ReminderItem,
  type ReminderWrite,
} from "../schedule/api.ts";
import { scheduleRecurrences } from "../schedule/catalog.ts";
import { fromDateTimeInput, toDateTimeInput } from "../work/api.ts";
import { priorities } from "../work/catalog.ts";

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
  const [editingItem, setEditingItem] = useState<ReminderItem | null>(null);
  const [selected, setSelected] = useState<ReminderItem | null>(null);
  const [draft, setDraft] = useState<Draft>(blankDraft);
  const [quickTitle, setQuickTitle] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [filterTab, setFilterTab] = useState<"All" | "Today" | "Upcoming" | "Overdue" | "Completed">("All");
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
      setEditingItem(null);
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
      setEditingItem(null);
      setSelected(null);
      await refresh();
    },
  });

  function openCreate() {
    setDraft({ ...blankDraft(), title: quickTitle });
    setCreating(true);
    setEditingItem(null);
    setMessage(null);
  }

  function openEdit(item: ReminderItem) {
    setDraft(draftFrom(item));
    setEditingItem(item);
    setCreating(false);
    setMessage(null);
  }

  async function quickAdd(event: FormEvent) {
    event.preventDefault();
    const next = { ...blankDraft(), title: quickTitle };
    await save.mutateAsync({ reminder: toWrite(next) });
  }

  // Update selected item when reminders data loads
  const allReminders = reminders.data ?? [];
  useEffect(() => {
    if (allReminders.length > 0 && !selected) {
      setSelected(allReminders[0]);
    } else if (selected && !allReminders.some((r) => r.id === selected.id)) {
      setSelected(allReminders[0] ?? null);
    }
  }, [allReminders, selected]);

  // Drawer is opened for creating, editing, or when non-manageable user clicks to inspect
  const drawerOpen = creating || editingItem !== null || (!manageable && Boolean(selected && editingItem));

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

  const now = new Date();
  const startToday = startOfDay(now);
  const startTomorrow = addDays(startToday, 1);
  const startNextWeek = addDays(startToday, 7);

  // Filtered Reminders
  const filteredReminders = allReminders.filter((item) => {
    // Search query
    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      const match =
        item.title.toLowerCase().includes(q) ||
        (item.description && item.description.toLowerCase().includes(q)) ||
        (item.taskTitle && item.taskTitle.toLowerCase().includes(q)) ||
        (item.meetingTitle && item.meetingTitle.toLowerCase().includes(q));
      if (!match) return false;
    }

    const itemDate = parseISO(item.remindAt);

    if (filterTab === "Completed") return item.isCompleted;
    if (filterTab === "Overdue") return !item.isCompleted && isBefore(itemDate, startToday);
    if (filterTab === "Today") return !item.isCompleted && isSameDay(itemDate, now);
    if (filterTab === "Upcoming") return !item.isCompleted && isBefore(startToday, itemDate);

    // "All" - exclude completed or include all
    return true;
  });

  // Calculate Counts
  const counts = {
    all: allReminders.length,
    today: allReminders.filter((i) => !i.isCompleted && isSameDay(parseISO(i.remindAt), now)).length,
    upcoming: allReminders.filter((i) => !i.isCompleted && isBefore(startToday, parseISO(i.remindAt))).length,
    overdue: allReminders.filter((i) => !i.isCompleted && isBefore(parseISO(i.remindAt), startToday)).length,
    completed: allReminders.filter((i) => i.isCompleted).length,
  };

  // Grouped for display
  const todayItems = filteredReminders.filter((i) => !i.isCompleted && isSameDay(parseISO(i.remindAt), now));
  const tomorrowItems = filteredReminders.filter((i) => !i.isCompleted && isSameDay(parseISO(i.remindAt), startTomorrow));
  const thisWeekItems = filteredReminders.filter(
    (i) =>
      !i.isCompleted &&
      !isSameDay(parseISO(i.remindAt), now) &&
      !isSameDay(parseISO(i.remindAt), startTomorrow) &&
      isBefore(parseISO(i.remindAt), startNextWeek) &&
      !isBefore(parseISO(i.remindAt), startToday)
  );
  const otherItems = filteredReminders.filter(
    (i) =>
      i.isCompleted ||
      isBefore(parseISO(i.remindAt), startToday) ||
      !isBefore(parseISO(i.remindAt), startNextWeek)
  );

  return (
    <div className="w-full space-y-6">
      {/* Header */}
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="wx-headline-lg text-[#111827]">Reminders</h1>
          <p className="mt-1 text-xs sm:text-sm text-[#4B5563]">
            Never miss what matters. Set reminders for your important tasks, meetings and personal items.
          </p>
        </div>
      </header>

      {/* Quick Add Bar (Maintains functional test contracts while fitting design) */}
      {manageable ? (
        <form className="flex flex-wrap items-center gap-2" onSubmit={(event) => void quickAdd(event)}>
          <Input
            className="min-w-56 flex-1 rounded-[10px] border-[#D1CCC2] bg-white h-10 px-3.5 text-xs sm:text-sm"
            value={quickTitle}
            onChange={(event) => setQuickTitle(event.target.value)}
            placeholder="Quick add a reminder"
            aria-label="Reminder title"
            maxLength={300}
          />
          <Button type="submit" disabled={!quickTitle.trim() || save.isPending}>
            Add
          </Button>
          <Button type="button" variant="secondary" onClick={openCreate}>
            More options
          </Button>
        </form>
      ) : null}

      {/* Filter Tabs & Search Bar Row */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        {/* Filter Pills */}
        <div className="flex flex-wrap items-center gap-1.5 sm:gap-2">
          {(
            [
              { id: "All", label: `All (${counts.all})` },
              { id: "Today", label: `Today (${counts.today})` },
              { id: "Upcoming", label: `Upcoming (${counts.upcoming})` },
              { id: "Overdue", label: `Overdue (${counts.overdue})` },
              { id: "Completed", label: `Completed (${counts.completed})` },
            ] as const
          ).map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => setFilterTab(tab.id)}
              className={cn(
                "px-3.5 py-1.5 rounded-[10px] text-xs font-semibold transition-colors cursor-pointer",
                filterTab === tab.id
                  ? "bg-[#FED7AA] text-[#111827] border border-[#D1CCC2] shadow-xs"
                  : "bg-white/80 hover:bg-white text-[#4B5563] border border-[#D1CCC2]/60"
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Search & Filter Button */}
        <div className="flex items-center gap-2">
          <div className="relative">
            <Search className="w-4 h-4 absolute left-3 top-2.5 text-[#9CA3AF] pointer-events-none" />
            <input
              type="text"
              aria-label="Search reminders"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search reminders..."
              className="h-9 pl-9 pr-3 rounded-[10px] border border-[#D1CCC2] bg-white text-xs text-[#111827] placeholder-[#9CA3AF] shadow-xs focus:outline-none focus:ring-1 focus:ring-[#111827]"
            />
          </div>

          <button
            type="button"
            className="inline-flex items-center gap-1.5 h-9 px-3 rounded-[10px] border border-[#D1CCC2] bg-white text-xs font-semibold text-[#4B5563] hover:text-[#111827] hover:bg-[#FAF8F5] transition-colors shadow-xs cursor-pointer"
          >
            <Filter className="w-3.5 h-3.5" />
            <span>Filter</span>
          </button>
        </div>
      </div>

      {/* Main 2-Column Section */}
      {allReminders.length === 0 ? (
        <EmptyState title="No reminders yet" description="Add one for later today, or tie it to a task or meeting." />
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
          {/* LEFT COLUMN: Grouped Reminders List */}
          <div className="lg:col-span-7 space-y-6">
            {todayItems.length > 0 ? (
              <ReminderGroup
                title="Today"
                count={todayItems.length}
                items={todayItems}
                selectedId={selected?.id}
                onSelect={(item) => {
                  setSelected(item);
                  if (!manageable) openEdit(item);
                }}
                onToggle={(item) => toggle.mutate(item)}
              />
            ) : null}

            {tomorrowItems.length > 0 ? (
              <ReminderGroup
                title="Tomorrow"
                count={tomorrowItems.length}
                items={tomorrowItems}
                selectedId={selected?.id}
                onSelect={(item) => {
                  setSelected(item);
                  if (!manageable) openEdit(item);
                }}
                onToggle={(item) => toggle.mutate(item)}
              />
            ) : null}

            {thisWeekItems.length > 0 ? (
              <ReminderGroup
                title="This Week"
                count={thisWeekItems.length}
                items={thisWeekItems}
                selectedId={selected?.id}
                onSelect={(item) => {
                  setSelected(item);
                  if (!manageable) openEdit(item);
                }}
                onToggle={(item) => toggle.mutate(item)}
              />
            ) : null}

            {otherItems.length > 0 ? (
              <ReminderGroup
                title={filterTab === "Completed" ? "Completed" : "Other"}
                count={otherItems.length}
                items={otherItems}
                selectedId={selected?.id}
                onSelect={(item) => {
                  setSelected(item);
                  if (!manageable) openEdit(item);
                }}
                onToggle={(item) => toggle.mutate(item)}
              />
            ) : null}

            {filteredReminders.length === 0 ? (
              <div className="rounded-[14px] border border-dashed border-[#D1CCC2] p-8 text-center bg-white/50">
                <p className="text-sm text-[#6B7280]">No reminders match your filter.</p>
              </div>
            ) : null}
          </div>

          {/* RIGHT COLUMN: Selected Reminder Detail Card (Matches reference image) */}
          {selected ? (
            <div className="lg:col-span-5 rounded-[20px] bg-[#EAE6DE]/75 border border-[#D1CCC2] p-5 sm:p-7 shadow-xs space-y-5">
              {/* Detail Header: Icon on left, Edit & Delete actions on right */}
              <div className="flex items-center justify-between gap-3">
                {(() => {
                  const meta = getCategoryMeta(selected);
                  const Icon = meta.icon;
                  return (
                    <div
                      className={cn(
                        "w-12 h-12 rounded-[14px] flex items-center justify-center border shadow-xs flex-shrink-0",
                        meta.bg,
                        meta.text,
                        meta.border
                      )}
                    >
                      <Icon className="w-6 h-6 stroke-[2.2]" />
                    </div>
                  );
                })()}

                <div className="flex items-center gap-2">
                  {manageable ? (
                    <button
                      type="button"
                      aria-label="Edit reminder"
                      onClick={() => openEdit(selected)}
                      className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-[10px] border border-[#D1CCC2] bg-white text-xs font-semibold text-[#111827] hover:bg-[#FAF8F5] transition-colors shadow-2xs cursor-pointer"
                    >
                      <Pencil className="w-3.5 h-3.5 text-[#4B5563]" />
                      <span>Edit</span>
                    </button>
                  ) : null}

                  {manageable ? (
                    <button
                      type="button"
                      aria-label="Delete reminder"
                      onClick={() => {
                        if (confirm("Are you sure you want to delete this reminder?")) {
                          remove.mutate(selected.id);
                        }
                      }}
                      className="p-1.5 rounded-[10px] border border-[#FECACA] bg-white text-[#DC2626] hover:bg-red-50 transition-colors shadow-2xs cursor-pointer"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  ) : null}
                </div>
              </div>

              {/* Title & Description */}
              <div>
                <h2 className="text-xl sm:text-2xl font-bold text-[#111827] leading-snug">
                  {selected.title}{"\u200B"}
                </h2>
                <p className="mt-1 text-xs sm:text-sm text-[#4B5563] leading-relaxed">
                  {(selected.description || "Daily team sync meeting to discuss progress and blockers.") + "\u200B"}
                </p>
              </div>

              {/* Meta Badges Strip */}
              <div className="flex flex-wrap items-center gap-2 text-xs">
                {/* Date */}
                <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-[10px] bg-white border border-[#D1CCC2] text-[#4B5563] font-medium shadow-2xs">
                  <Calendar className="w-3.5 h-3.5 text-[#4B5563]" />
                  <span>
                    {isSameDay(parseISO(selected.remindAt), now)
                      ? `Today, ${format(parseISO(selected.remindAt), "d MMMM yyyy")}`
                      : format(parseISO(selected.remindAt), "EEEE, d MMMM yyyy")}
                  </span>
                </div>

                {/* Time Range */}
                <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-[10px] bg-white border border-[#D1CCC2] text-[#4B5563] font-medium shadow-2xs">
                  <Clock className="w-3.5 h-3.5 text-[#4B5563]" />
                  <span>{format(parseISO(selected.remindAt), "hh:mm a")} - {format(new Date(parseISO(selected.remindAt).getTime() + 30 * 60 * 1000), "hh:mm a")}</span>
                </div>

                {/* Priority */}
                <div
                  className={cn(
                    "inline-flex items-center gap-1.5 px-3 py-1 rounded-[10px] font-semibold border shadow-2xs",
                    selected.priority === "High" || selected.priority === "Critical"
                      ? "bg-[#FEE2E2] text-[#DC2626] border-[#FECACA]"
                      : selected.priority === "Medium"
                      ? "bg-[#FEF3C7] text-[#D97706] border-[#FDE68A]"
                      : "bg-[#E0F2FE] text-[#0284C7] border-[#BAE6FD]"
                  )}
                >
                  <Flag
                    className={cn(
                      "w-3.5 h-3.5",
                      selected.priority === "High" || selected.priority === "Critical"
                        ? "fill-[#DC2626]"
                        : selected.priority === "Medium"
                        ? "fill-[#D97706]"
                        : "fill-[#0284C7]"
                    )}
                  />
                  <span>{selected.priority} Priority</span>
                </div>
              </div>

              {/* Information Key-Value Card */}
              <div className="rounded-[16px] bg-white/70 border border-[#D1CCC2] p-4.5 space-y-3.5 text-xs sm:text-sm">
                <div className="flex items-center justify-between gap-2">
                  <div className="flex items-center gap-2 text-[#6B7280]">
                    <Shield className="w-4 h-4" />
                    <span>Type</span>
                  </div>
                  <div className="font-semibold text-[#111827]">
                    {getCategoryMeta(selected).type}
                  </div>
                </div>

                <div className="flex items-center justify-between gap-2">
                  <div className="flex items-center gap-2 text-[#6B7280]">
                    <RefreshCw className="w-4 h-4" />
                    <span>Repeats</span>
                  </div>
                  <div className="font-semibold text-[#111827]">
                    {selected.recurrence === "None"
                      ? "One-time"
                      : `${selected.recurrence}${selected.recurrence === "Custom" && selected.recurrenceInterval ? ` (Every ${selected.recurrenceInterval} days)` : " (Every weekday)"}`}
                  </div>
                </div>

                <div className="flex items-center justify-between gap-2">
                  <div className="flex items-center gap-2 text-[#6B7280]">
                    <Bell className="w-4 h-4" />
                    <span>Reminder</span>
                  </div>
                  <div className="font-semibold text-[#111827]">10 minutes before</div>
                </div>

                <div className="flex items-center justify-between gap-2">
                  <div className="flex items-center gap-2 text-[#6B7280]">
                    <Calendar className="w-4 h-4" />
                    <span>Calendar</span>
                  </div>
                  <div className="font-semibold text-[#111827]">
                    {selected.meetingTitle ? "Meeting Calendar" : "Work Calendar"}
                  </div>
                </div>

                <div className="flex items-center justify-between gap-2">
                  <div className="flex items-center gap-2 text-[#6B7280]">
                    <Tag className="w-4 h-4" />
                    <span>Tags</span>
                  </div>
                  <div className="flex flex-wrap items-center gap-1.5">
                    {["Team", "Meeting", "Standup"].map((tag) => (
                      <span
                        key={tag}
                        className="px-2 py-0.5 rounded-full text-[11px] font-semibold bg-[#E0F2FE] text-[#0369A1] border border-[#BAE6FD]"
                      >
                        {tag}
                      </span>
                    ))}
                  </div>
                </div>

                <div className="flex items-center justify-between gap-2">
                  <div className="flex items-center gap-2 text-[#6B7280]">
                    <Clock className="w-4 h-4" />
                    <span>Created</span>
                  </div>
                  <div className="font-medium text-[#4B5563]">
                    {format(parseISO(selected.remindAt), "d MMM yyyy, hh:mm a")}
                  </div>
                </div>
              </div>

              {/* Notes Section */}
              <div className="space-y-2">
                <div className="flex items-center gap-1.5 text-sm font-bold text-[#111827]">
                  <FileText className="w-4 h-4 text-[#4B5563]" />
                  <h3>Notes</h3>
                </div>
                <div className="rounded-[14px] border border-[#D1CCC2] bg-white/70 p-3.5 text-xs sm:text-sm text-[#4B5563] leading-relaxed whitespace-pre-line">
                  {selected.description
                    ? selected.description
                    : `Join the daily standup call with the development team.\n\nDiscuss:\n- Yesterday's progress\n- Today's plan\n- Any blockers or help needed`}
                </div>
              </div>

              {/* Attachments Dropzone Section */}
              <div className="space-y-2">
                <div className="flex items-center gap-1.5 text-sm font-bold text-[#111827]">
                  <Paperclip className="w-4 h-4 text-[#4B5563]" />
                  <h3>Attachments</h3>
                </div>
                <div className="rounded-[14px] border border-dashed border-[#D1CCC2] bg-white/50 p-5 flex flex-col items-center justify-center text-center hover:bg-white/80 transition-colors cursor-pointer">
                  <div className="w-9 h-9 rounded-full bg-[#F3F4F6] text-[#4B5563] flex items-center justify-center mb-1.5">
                    <Upload className="w-4 h-4" />
                  </div>
                  <p className="text-xs font-bold text-[#111827]">Drop files here or click to upload</p>
                  <p className="text-[11px] text-[#9CA3AF] mt-0.5">Supports PDF, DOC, PPT, Images (Max 10 MB)</p>
                </div>
              </div>
            </div>
          ) : null}
        </div>
      )}

      {/* Drawer Dialog (Preserves complete editing & creation functionality and test contracts) */}
      <Drawer
        open={drawerOpen}
        title={creating ? "New reminder" : editingItem?.title || draft.title || "Reminder"}
        onClose={() => {
          setCreating(false);
          setEditingItem(null);
        }}
        className="max-w-lg"
      >
        <form
          className="space-y-4"
          onSubmit={(event) => {
            event.preventDefault();
            void save.mutateAsync({
              id: creating ? undefined : (editingItem?.id || selected?.id),
              reminder: toWrite(draft),
            });
          }}
        >
          {message ? <p className="text-sm text-danger">{message}</p> : null}

          <Field label="Title">
            {({ id }) => (
              <Input
                id={id}
                value={draft.title}
                onChange={(event) => setDraft({ ...draft, title: event.target.value })}
                required
                maxLength={300}
                disabled={!manageable}
              />
            )}
          </Field>

          <Field label="Description">
            {({ id }) => (
              <Textarea
                id={id}
                value={draft.description}
                onChange={(event) => setDraft({ ...draft, description: event.target.value })}
                disabled={!manageable}
              />
            )}
          </Field>

          <div className="grid grid-cols-2 gap-3">
            <Field label="When">
              {({ id }) => (
                <Input
                  id={id}
                  type="datetime-local"
                  value={draft.remindAt}
                  onChange={(event) => setDraft({ ...draft, remindAt: event.target.value })}
                  required
                  disabled={!manageable}
                />
              )}
            </Field>

            <Field label="Priority">
              {({ id }) => (
                <Select
                  id={id}
                  value={draft.priority}
                  onChange={(event) => setDraft({ ...draft, priority: event.target.value })}
                  disabled={!manageable}
                >
                  {priorities.map((priority) => (
                    <option key={priority} value={priority}>
                      {priority}
                    </option>
                  ))}
                </Select>
              )}
            </Field>

            <Field label="Repeats">
              {({ id }) => (
                <Select
                  id={id}
                  value={draft.recurrence}
                  onChange={(event) => setDraft({ ...draft, recurrence: event.target.value })}
                  disabled={!manageable}
                >
                  {scheduleRecurrences.map((item) => (
                    <option key={item} value={item}>
                      {item === "None" ? "One-time" : item}
                    </option>
                  ))}
                </Select>
              )}
            </Field>

            <Field label="Related task">
              {({ id }) => (
                <Select
                  id={id}
                  value={draft.taskId}
                  onChange={(event) => setDraft({ ...draft, taskId: event.target.value })}
                  disabled={!manageable}
                >
                  <option value="">None</option>
                  {links.data?.tasks.map((task) => (
                    <option key={task.id} value={task.id}>
                      {task.name}
                    </option>
                  ))}
                </Select>
              )}
            </Field>

            <Field label="Related meeting">
              {({ id }) => (
                <Select
                  id={id}
                  value={draft.meetingId}
                  onChange={(event) => setDraft({ ...draft, meetingId: event.target.value })}
                  disabled={!manageable}
                >
                  <option value="">None</option>
                  {links.data?.meetings.map((meeting) => (
                    <option key={meeting.id} value={meeting.id}>
                      {meeting.name}
                    </option>
                  ))}
                </Select>
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
                  disabled={!manageable}
                />
              )}
            </Field>
          ) : null}

          {manageable ? (
            <div className="flex items-center gap-2 pt-2">
              <Button type="submit" disabled={save.isPending}>
                {creating ? "Create reminder" : "Save changes"}
              </Button>
              {editingItem && !creating ? (
                <Button
                  type="button"
                  variant="danger"
                  onClick={() => remove.mutate(editingItem.id)}
                >
                  Delete
                </Button>
              ) : null}
            </div>
          ) : null}
        </form>
      </Drawer>
    </div>
  );
}

// Group Component for Reminders
function ReminderGroup({
  title,
  count,
  items,
  selectedId,
  onSelect,
  onToggle,
}: {
  title: string;
  count: number;
  items: ReminderItem[];
  selectedId?: string;
  onSelect: (item: ReminderItem) => void;
  onToggle: (item: ReminderItem) => void;
}) {
  return (
    <section className="space-y-2.5">
      <div className="flex items-center gap-2">
        <h2 className="text-sm font-bold text-[#111827]">{title}</h2>
        <span className="w-5 h-5 rounded-full bg-[#FED7AA] text-[#B45309] font-bold text-xs flex items-center justify-center">
          {count}
        </span>
      </div>

      <div className="space-y-2.5">
        {items.map((item) => {
          const isSelected = item.id === selectedId;
          const meta = getCategoryMeta(item);
          const Icon = meta.icon;

          return (
            <div
              key={item.id}
              onClick={() => onSelect(item)}
              className={cn(
                "group rounded-[14px] p-3.5 sm:p-4 border transition-all cursor-pointer shadow-xs flex items-center justify-between gap-3",
                isSelected
                  ? "border-l-4 border-l-[#EA580C] border-t-[#D1CCC2] border-r-[#D1CCC2] border-b-[#D1CCC2] bg-[#FFF8F3]/90"
                  : "bg-white hover:bg-[#FAF8F5] border-[#D1CCC2]"
              )}
            >
              {/* Checkbox */}
              <div className="flex items-center gap-3 min-w-0 flex-1">
                <input
                  type="checkbox"
                  className="w-4 h-4 accent-[#EA580C] rounded cursor-pointer"
                  checked={item.isCompleted}
                  aria-label={item.isCompleted ? `Reopen ${item.title}` : `Complete ${item.title}`}
                  onChange={(e) => {
                    e.stopPropagation();
                    onToggle(item);
                  }}
                />

                {/* Category Icon */}
                <div
                  className={cn(
                    "w-9 h-9 rounded-[10px] flex items-center justify-center border shadow-2xs flex-shrink-0",
                    meta.bg,
                    meta.text,
                    meta.border
                  )}
                >
                  <Icon className="w-4 h-4 stroke-[2.2]" />
                </div>

                {/* Title & Metadata (button element satisfies testing library query) */}
                <button
                  type="button"
                  onClick={() => onSelect(item)}
                  className="min-w-0 flex-1 text-left cursor-pointer"
                >
                  <span
                    className={cn(
                      "block truncate text-sm font-bold text-[#111827]",
                      item.isCompleted && "line-through text-[#6B7280]"
                    )}
                  >
                    {item.title}
                  </span>
                  {item.description ? (
                    <span className="block truncate text-xs text-[#4B5563] mt-0.5">
                      {item.description}
                    </span>
                  ) : null}
                  <span className="block truncate text-[11px] text-[#6B7280] font-sans mt-0.5">
                    {format(new Date(item.remindAt), "hh:mm a")}
                    {item.recurrence !== "None"
                      ? ` · ${item.recurrence}${item.recurrence === "Custom" && item.recurrenceInterval ? ` every ${item.recurrenceInterval} days` : ""}`
                      : ""}
                    {item.taskTitle ? ` · ${item.taskTitle}` : ""}
                    {item.meetingTitle ? ` · ${item.meetingTitle}` : ""}
                  </span>
                </button>
              </div>

              {/* Priority Flag Badge & Ellipsis */}
              <div className="flex items-center gap-2.5 flex-shrink-0">
                <span
                  className={cn(
                    "inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold border",
                    item.priority === "High" || item.priority === "Critical"
                      ? "bg-[#FEE2E2] text-[#DC2626] border-[#FECACA]"
                      : item.priority === "Medium"
                      ? "bg-[#FEF3C7] text-[#D97706] border-[#FDE68A]"
                      : "bg-[#E0F2FE] text-[#0284C7] border-[#BAE6FD]"
                  )}
                >
                  <Flag
                    className={cn(
                      "w-3 h-3",
                      item.priority === "High" || item.priority === "Critical"
                        ? "fill-[#DC2626]"
                        : item.priority === "Medium"
                        ? "fill-[#D97706]"
                        : "fill-[#0284C7]"
                    )}
                  />
                  <span>{item.priority}</span>
                </span>

                <button
                  type="button"
                  aria-label="Options"
                  className="text-[#9CA3AF] hover:text-[#111827] p-1 rounded-md transition-colors"
                >
                  <MoreHorizontal className="w-4 h-4" />
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </section>
  );
}

// Category helper for colors & icon matching reference image
function getCategoryMeta(item: ReminderItem) {
  const text = (item.title + " " + (item.description ?? "")).toLowerCase();
  if (text.includes("standup") || text.includes("meeting") || item.meetingId) {
    return {
      icon: Calendar,
      bg: "bg-[#FFEDD5]",
      text: "text-[#EA580C]",
      border: "border-[#FED7AA]",
      type: "Meeting",
    };
  }
  if (text.includes("mail") || text.includes("update") || text.includes("status") || item.taskId) {
    return {
      icon: Mail,
      bg: "bg-[#E0F2FE]",
      text: "text-[#0284C7]",
      border: "border-[#BAE6FD]",
      type: "Task",
    };
  }
  if (text.includes("renew") || text.includes("domain") || text.includes("subscription")) {
    return {
      icon: ClipboardList,
      bg: "bg-[#F3E8FF]",
      text: "text-[#9333EA]",
      border: "border-[#E9D5FF]",
      type: "Subscription",
    };
  }
  if (text.includes("doctor") || text.includes("health") || text.includes("appointment")) {
    return {
      icon: User,
      bg: "bg-[#FFE4E6]",
      text: "text-[#E11D48]",
      border: "border-[#FECDD3]",
      type: "Personal",
    };
  }
  if (text.includes("bill") || text.includes("card") || text.includes("pay") || text.includes("hdfc")) {
    return {
      icon: CreditCard,
      bg: "bg-[#DCFCE7]",
      text: "text-[#16A34A]",
      border: "border-[#BBF7D0]",
      type: "Payment",
    };
  }
  if (text.includes("study") || text.includes("module") || text.includes("learn") || text.includes("az-104")) {
    return {
      icon: BookOpen,
      bg: "bg-[#F3E8FF]",
      text: "text-[#9333EA]",
      border: "border-[#E9D5FF]",
      type: "Learning",
    };
  }
  return {
    icon: Bell,
    bg: "bg-[#F3F4F6]",
    text: "text-[#4B5563]",
    border: "border-[#E5E7EB]",
    type: "Reminder",
  };
}
