import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { addDays, format, isSameDay, parseISO, startOfWeek } from "date-fns";
import {
  Calendar,
  Check,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Clock,
  Flag,
  Info,
  MoreHorizontal,
  Pencil,
  Plus,
  SlidersHorizontal,
  Trash2,
  X,
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { ApiError } from "../../services/api-client.ts";
import { cn } from "../../utils/cn.ts";
import {
  completeTodo,
  deleteTodo,
  fetchTodos,
  fromDateTimeInput,
  localOffsetMinutes,
  reopenTodo,
  saveTodo,
  toDateTimeInput,
  type TodoItem,
  type TodoWrite,
} from "../work/api.ts";
import { priorities } from "../work/catalog.ts";

type TodoMeta = {
  description?: string;
  status?: "To Do" | "In Progress" | "Completed";
  timeSlot?: string;
  checklist?: { id: string; text: string; done: boolean }[];
  notes?: string;
};

// Seed demo items matching the reference design image for a rich, pixel-perfect initial state
const defaultDemoItems: (TodoItem & TodoMeta)[] = [
  {
    id: "demo-1",
    title: "Prepare Standup Call Notes",
    isCompleted: false,
    priority: "High",
    recurrence: "None",
    dueAt: new Date().toISOString(),
    remindAt: null,
    bucket: "Today",
    tags: ["Work"],
    timeSlot: "09:00 - 09:30",
    status: "In Progress",
    description:
      "Prepare notes for today's standup call. Include updates on completed work, in progress items, blockers, and plan for today.",
    checklist: [
      { id: "c1", text: "Completed work updates", done: true },
      { id: "c2", text: "In progress items", done: true },
      { id: "c3", text: "Identify blockers", done: true },
      { id: "c4", text: "Plan for today", done: false },
      { id: "c5", text: "Add notes to Notion", done: false },
    ],
    notes: "",
  },
  {
    id: "demo-2",
    title: "Design Todo Module UI",
    isCompleted: false,
    priority: "Medium",
    recurrence: "None",
    dueAt: new Date().toISOString(),
    remindAt: null,
    bucket: "Today",
    tags: ["Design"],
    timeSlot: "10:00 - 12:00",
    status: "In Progress",
    description: "Design pixel-perfect layout and interactions for personal todo management screen.",
    checklist: [
      { id: "c21", text: "Create wireframe layout", done: true },
      { id: "c22", text: "Add color tokens & badges", done: true },
      { id: "c23", text: "Review responsiveness", done: false },
    ],
    notes: "",
  },
  {
    id: "demo-3",
    title: "Review PR: Auth Service",
    isCompleted: false,
    priority: "Medium",
    recurrence: "None",
    dueAt: new Date().toISOString(),
    remindAt: null,
    bucket: "Today",
    tags: ["Development"],
    timeSlot: "13:00 - 14:00",
    status: "To Do",
    description: "Review pull request for refreshed token rotation and endpoint rate limiting.",
    checklist: [
      { id: "c31", text: "Check test coverage", done: true },
      { id: "c32", text: "Verify token lifetime logic", done: false },
    ],
    notes: "",
  },
  {
    id: "demo-4",
    title: "AZ-104 Study",
    isCompleted: false,
    priority: "Low",
    recurrence: "None",
    dueAt: new Date().toISOString(),
    remindAt: null,
    bucket: "Today",
    tags: ["Learning"],
    timeSlot: "19:00 - 21:00",
    status: "To Do",
    description: "Practice Azure Resource Manager templates and virtual network peering exercises.",
    checklist: [
      { id: "c41", text: "Chapter 4 Virtual Networking", done: false },
      { id: "c42", text: "Complete hands-on lab", done: false },
    ],
    notes: "",
  },
  {
    id: "demo-5",
    title: "Plan Weekend Trip",
    isCompleted: false,
    priority: "Low",
    recurrence: "None",
    dueAt: addDays(new Date(), 1).toISOString(),
    remindAt: null,
    bucket: "Upcoming",
    tags: ["Personal"],
    timeSlot: "Tomorrow",
    status: "To Do",
    description: "Look up weekend getaway destinations, book accommodations, and prepare packing checklist.",
    checklist: [
      { id: "c51", text: "Check hotel availability", done: false },
      { id: "c52", text: "Plan driving route", done: false },
    ],
    notes: "",
  },
  {
    id: "demo-6",
    title: "Buy Groceries",
    isCompleted: false,
    priority: "Low",
    recurrence: "None",
    dueAt: addDays(new Date(), 1).toISOString(),
    remindAt: null,
    bucket: "Upcoming",
    tags: ["Personal"],
    timeSlot: "Tomorrow",
    status: "To Do",
    description: "Restock kitchen essentials, fruits, vegetables, and household supplies.",
    checklist: [
      { id: "c61", text: "Fresh produce", done: false },
      { id: "c62", text: "Almond milk & coffee", done: false },
    ],
    notes: "",
  },
];

function loadMeta(id: string): TodoMeta {
  try {
    const raw = localStorage.getItem(`worknexa_todo_meta_${id}`);
    if (raw) return JSON.parse(raw);
  } catch {
    // fallback
  }
  return {};
}

function saveMeta(id: string, meta: TodoMeta) {
  try {
    localStorage.setItem(`worknexa_todo_meta_${id}`, JSON.stringify(meta));
  } catch {
    // ignore
  }
}

export function TodosView() {
  const { user } = useAuth();
  const manageable = can(user?.permissions, permissions.tasksManage);
  const now = new Date();
  const [cursor, setCursor] = useState<Date>(now);
  const dateStr = format(cursor, "yyyy-MM-dd");
  const offsetMinutes = localOffsetMinutes(cursor);
  const queryClient = useQueryClient();

  const todosQuery = useQuery({
    queryKey: ["todos", dateStr, offsetMinutes],
    queryFn: () => fetchTodos(dateStr, offsetMinutes),
  });

  const [filterTab, setFilterTab] = useState<"All" | "Today" | "Upcoming" | "Completed">("All");
  const [selectedId, setSelectedId] = useState<string>("demo-1");
  const [localDemos, setLocalDemos] = useState<(TodoItem & TodoMeta)[]>(() => {
    try {
      const stored = localStorage.getItem("worknexa_todos_demo_state");
      if (stored) return JSON.parse(stored);
    } catch {
      // ignore
    }
    return defaultDemoItems;
  });

  // Dialog & Form states
  const [isAddOpen, setIsAddOpen] = useState(false);
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [statusMenuOpen, setStatusMenuOpen] = useState(false);
  const [newChecklistText, setNewChecklistText] = useState("");
  const [isAddingChecklist, setIsAddingChecklist] = useState(false);
  const [newTagText, setNewTagText] = useState("");
  const [isAddingTag, setIsAddingTag] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    document.title = "Todo · Worknexa";
  }, []);

  function persistDemos(next: (TodoItem & TodoMeta)[]) {
    setLocalDemos(next);
    try {
      localStorage.setItem("worknexa_todos_demo_state", JSON.stringify(next));
    } catch {
      // ignore
    }
  }

  // Combined todo items list: if real API has items, merge them with stored local metadata; otherwise show demo items
  const items: (TodoItem & TodoMeta)[] = useMemo(() => {
    if (todosQuery.data && todosQuery.data.length > 0) {
      return todosQuery.data.map((item) => {
        const meta = loadMeta(item.id);
        return {
          ...item,
          status: item.isCompleted ? "Completed" : meta.status ?? "In Progress",
          timeSlot: meta.timeSlot ?? (item.dueAt ? format(new Date(item.dueAt), "HH:mm") : "Anytime"),
          description: meta.description ?? `Personal todo: ${item.title}`,
          checklist: meta.checklist ?? [],
          notes: meta.notes ?? "",
        };
      });
    }
    return localDemos;
  }, [todosQuery.data, localDemos]);

  // Keep an item selected
  useEffect(() => {
    if (items.length > 0 && !items.some((i) => i.id === selectedId)) {
      setSelectedId(items[0].id);
    }
  }, [items, selectedId]);

  const selectedItem = useMemo(() => {
    return items.find((i) => i.id === selectedId) ?? items[0] ?? null;
  }, [items, selectedId]);

  // Filtered items
  const filteredItems = useMemo(() => {
    return items.filter((item) => {
      if (filterTab === "Today") {
        if (item.bucket === "Today") return true;
        if (item.dueAt) {
          try {
            return isSameDay(parseISO(item.dueAt), cursor);
          } catch {
            return false;
          }
        }
        return false;
      }
      if (filterTab === "Upcoming") {
        return item.bucket === "Upcoming" || item.bucket === "Tomorrow" || item.bucket === "ThisWeek";
      }
      if (filterTab === "Completed") {
        return item.isCompleted;
      }
      return true; // "All"
    });
  }, [items, filterTab, cursor]);

  const counts = useMemo(() => {
    return {
      all: items.length,
      today: items.filter((i) => i.bucket === "Today" || (i.dueAt && isSameDay(parseISO(i.dueAt), cursor))).length,
      upcoming: items.filter((i) => i.bucket === "Upcoming" || i.bucket === "Tomorrow" || i.bucket === "ThisWeek").length,
      completed: items.filter((i) => i.isCompleted).length,
    };
  }, [items, cursor]);

  // Week ribbon calculation (Monday to Sunday)
  const weekDays = useMemo(() => {
    const start = startOfWeek(cursor, { weekStartsOn: 1 });
    return Array.from({ length: 7 }, (_, i) => addDays(start, i));
  }, [cursor]);

  // Mutations
  const createMutation = useMutation({
    mutationFn: (todo: TodoWrite) => saveTodo(todo),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["todos"] });
      await queryClient.invalidateQueries({ queryKey: ["my-day"] });
    },
    onError: (error) => setErrorMessage(error instanceof ApiError ? error.message : "The todo could not be saved."),
  });

  const toggleCompleteMutation = useMutation({
    mutationFn: async ({ id, isCompleted }: { id: string; isCompleted: boolean }) => {
      if (id.startsWith("demo-")) {
        const next = localDemos.map((d) =>
          d.id === id ? { ...d, isCompleted: !isCompleted, status: !isCompleted ? ("Completed" as const) : ("In Progress" as const) } : d
        );
        persistDemos(next);
        return;
      }
      if (isCompleted) {
        await reopenTodo(id);
      } else {
        await completeTodo(id);
      }
      await queryClient.invalidateQueries({ queryKey: ["todos"] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      if (id.startsWith("demo-")) {
        const next = localDemos.filter((d) => d.id !== id);
        persistDemos(next);
        return;
      }
      await deleteTodo(id);
      await queryClient.invalidateQueries({ queryKey: ["todos"] });
    },
  });

  function updateSelectedMeta(partial: Partial<TodoMeta>) {
    if (!selectedItem) return;
    if (selectedItem.id.startsWith("demo-")) {
      const next = localDemos.map((d) => (d.id === selectedItem.id ? { ...d, ...partial } : d));
      persistDemos(next);
    } else {
      const current = loadMeta(selectedItem.id);
      const updated = { ...current, ...partial };
      saveMeta(selectedItem.id, updated);
      void queryClient.invalidateQueries({ queryKey: ["todos"] });
    }
  }

  function handleToggleChecklist(checklistId: string) {
    if (!selectedItem?.checklist) return;
    const nextList = selectedItem.checklist.map((c) => (c.id === checklistId ? { ...c, done: !c.done } : c));
    updateSelectedMeta({ checklist: nextList });
  }

  function handleAddChecklistItem() {
    if (!newChecklistText.trim() || !selectedItem) return;
    const item = { id: `cl-${Date.now()}`, text: newChecklistText.trim(), done: false };
    const nextList = [...(selectedItem.checklist ?? []), item];
    updateSelectedMeta({ checklist: nextList });
    setNewChecklistText("");
    setIsAddingChecklist(false);
  }

  function handleAddTag() {
    if (!newTagText.trim() || !selectedItem) return;
    const nextTags = [...selectedItem.tags, newTagText.trim()];
    if (selectedItem.id.startsWith("demo-")) {
      const next = localDemos.map((d) => (d.id === selectedItem.id ? { ...d, tags: nextTags } : d));
      persistDemos(next);
    } else {
      void saveTodo(
        {
          title: selectedItem.title,
          priority: selectedItem.priority,
          dueAt: selectedItem.dueAt,
          remindAt: selectedItem.remindAt,
          recurrence: selectedItem.recurrence,
          tags: nextTags,
        },
        selectedItem.id
      ).then(() => queryClient.invalidateQueries({ queryKey: ["todos"] }));
    }
    setNewTagText("");
    setIsAddingTag(false);
  }

  function handleStatusChange(status: "To Do" | "In Progress" | "Completed") {
    if (!selectedItem) return;
    const willBeCompleted = status === "Completed";
    if (willBeCompleted !== selectedItem.isCompleted) {
      toggleCompleteMutation.mutate({ id: selectedItem.id, isCompleted: selectedItem.isCompleted });
    }
    updateSelectedMeta({ status });
    setStatusMenuOpen(false);
  }

  if (!can(user?.permissions, permissions.tasksRead)) {
    return <ErrorState title="Access denied" description="Your account does not include personal todos." />;
  }

  if (todosQuery.isPending && items.length === 0) {
    return <LoadingState label="Loading todos" />;
  }

  return (
    <div className="w-full space-y-6">
      {errorMessage ? (
        <div className="p-3 bg-red-100 text-red-800 rounded-lg text-sm flex items-center justify-between">
          <span>{errorMessage}</span>
          <button type="button" onClick={() => setErrorMessage(null)} className="cursor-pointer">
            <X className="w-4 h-4" />
          </button>
        </div>
      ) : null}

      {/* Main 2-Column Layout */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        {/* LEFT COLUMN: Header, Date Ribbon, Filters, Todo Cards List */}
        <div className="lg:col-span-7 space-y-5">
          {/* Header Row */}
          <div className="flex items-center justify-between gap-4">
            <div>
              <h1 className="wx-headline-lg text-[#111827]">Todo</h1>
              <p className="mt-0.5 text-xs sm:text-sm text-[#4B5563]">
                Personal items. They stay off the task board.
              </p>
            </div>
            {manageable ? (
              <button
                type="button"
                onClick={() => setIsAddOpen(true)}
                className="inline-flex items-center gap-1.5 px-4 py-2 rounded-[10px] bg-[#FED7AA] hover:bg-[#FDBA74] text-[#111827] text-xs sm:text-sm font-bold border border-[#111827]/15 shadow-xs transition-colors cursor-pointer"
              >
                <Plus className="w-4 h-4 text-[#111827]" />
                <span>Add todo</span>
              </button>
            ) : null}
          </div>

          {/* Date Strip / Carousel Card */}
          <div className="rounded-[16px] bg-white border border-[#D1CCC2] p-3.5 sm:p-4 shadow-xs">
            {/* Header: < > Month Year ... Today */}
            <div className="flex items-center justify-between mb-3 px-1">
              <div className="flex items-center gap-3">
                <div className="flex items-center gap-1">
                  <button
                    type="button"
                    aria-label="Previous week"
                    onClick={() => setCursor((c) => addDays(c, -7))}
                    className="w-7 h-7 rounded-[8px] flex items-center justify-center text-[#4B5563] hover:text-[#111827] hover:bg-[#FAF8F5] transition-colors cursor-pointer"
                  >
                    <ChevronLeft className="w-4 h-4" />
                  </button>
                  <button
                    type="button"
                    aria-label="Next week"
                    onClick={() => setCursor((c) => addDays(c, 7))}
                    className="w-7 h-7 rounded-[8px] flex items-center justify-center text-[#4B5563] hover:text-[#111827] hover:bg-[#FAF8F5] transition-colors cursor-pointer"
                  >
                    <ChevronRight className="w-4 h-4" />
                  </button>
                </div>
                <span className="text-sm font-bold text-[#111827]">
                  {format(cursor, "MMMM yyyy")}
                </span>
              </div>

              <button
                type="button"
                onClick={() => setCursor(new Date())}
                className="px-3 py-1 rounded-[8px] border border-[#D1CCC2] bg-white text-xs font-semibold text-[#111827] hover:bg-[#FAF8F5] transition-colors shadow-xs cursor-pointer"
              >
                Today
              </button>
            </div>

            {/* 7 Days Strip */}
            <div className="grid grid-cols-7 gap-1.5 sm:gap-2">
              {weekDays.map((day) => {
                const isSelected = isSameDay(day, cursor);
                return (
                  <button
                    key={day.toISOString()}
                    type="button"
                    onClick={() => setCursor(day)}
                    className={cn(
                      "flex flex-col items-center justify-center py-2 sm:py-2.5 rounded-[12px] transition-all cursor-pointer",
                      isSelected
                        ? "bg-[#EA580C] text-white shadow-sm font-bold scale-[1.02]"
                        : "bg-white hover:bg-[#FAF8F5] text-[#111827] border border-[#E5E7EB]"
                    )}
                  >
                    <span className={cn("text-base sm:text-lg leading-tight font-bold", isSelected ? "text-white" : "text-[#111827]")}>
                      {format(day, "d")}
                    </span>
                    <span className={cn("text-[10px] sm:text-xs font-medium uppercase mt-0.5", isSelected ? "text-white/90" : "text-[#6B7280]")}>
                      {format(day, "EEE")}
                    </span>
                  </button>
                );
              })}
            </div>
          </div>

          {/* Filter Tabs & Filter Icon Bar */}
          <div className="flex items-center justify-between gap-2">
            <div className="flex flex-wrap items-center gap-1.5 sm:gap-2">
              <button
                type="button"
                onClick={() => setFilterTab("All")}
                className={cn(
                  "px-3 py-1.5 rounded-[10px] text-xs font-semibold transition-colors cursor-pointer",
                  filterTab === "All"
                    ? "bg-[#FED7AA] text-[#111827] border border-[#D1CCC2] shadow-xs"
                    : "bg-white/80 hover:bg-white text-[#4B5563] border border-[#D1CCC2]/60"
                )}
              >
                All ({counts.all})
              </button>
              <button
                type="button"
                onClick={() => setFilterTab("Today")}
                className={cn(
                  "px-3 py-1.5 rounded-[10px] text-xs font-semibold transition-colors cursor-pointer",
                  filterTab === "Today"
                    ? "bg-[#FED7AA] text-[#111827] border border-[#D1CCC2] shadow-xs"
                    : "bg-white/80 hover:bg-white text-[#4B5563] border border-[#D1CCC2]/60"
                )}
              >
                Today ({counts.today})
              </button>
              <button
                type="button"
                onClick={() => setFilterTab("Upcoming")}
                className={cn(
                  "px-3 py-1.5 rounded-[10px] text-xs font-semibold transition-colors cursor-pointer",
                  filterTab === "Upcoming"
                    ? "bg-[#FED7AA] text-[#111827] border border-[#D1CCC2] shadow-xs"
                    : "bg-white/80 hover:bg-white text-[#4B5563] border border-[#D1CCC2]/60"
                )}
              >
                Upcoming ({counts.upcoming})
              </button>
              <button
                type="button"
                onClick={() => setFilterTab("Completed")}
                className={cn(
                  "px-3 py-1.5 rounded-[10px] text-xs font-semibold transition-colors cursor-pointer",
                  filterTab === "Completed"
                    ? "bg-[#FED7AA] text-[#111827] border border-[#D1CCC2] shadow-xs"
                    : "bg-white/80 hover:bg-white text-[#4B5563] border border-[#D1CCC2]/60"
                )}
              >
                Completed ({counts.completed})
              </button>
            </div>

            <button
              type="button"
              aria-label="Filter options"
              className="w-9 h-9 rounded-[10px] border border-[#D1CCC2] bg-white flex items-center justify-center text-[#4B5563] hover:text-[#111827] hover:bg-[#FAF8F5] transition-colors shadow-xs cursor-pointer flex-shrink-0"
            >
              <SlidersHorizontal className="w-4 h-4" />
            </button>
          </div>

          {/* Todo Items List */}
          <div className="space-y-2.5">
            {filteredItems.length === 0 ? (
              <div className="rounded-[14px] border border-dashed border-[#D1CCC2] p-8 text-center bg-white/50">
                <p className="text-sm text-[#6B7280]">No todos found in this view.</p>
              </div>
            ) : null}

            {filteredItems.map((item) => {
              const isSelected = item.id === selectedItem?.id;
              return (
                <div
                  key={item.id}
                  onClick={() => setSelectedId(item.id)}
                  className={cn(
                    "group relative rounded-[14px] p-3.5 sm:p-4 border transition-all cursor-pointer shadow-xs flex items-center justify-between gap-3",
                    isSelected
                      ? "border-l-4 border-l-[#EA580C] border-t-[#D1CCC2] border-r-[#D1CCC2] border-b-[#D1CCC2] bg-[#FFF8F3]/90"
                      : "bg-white hover:bg-[#FAF8F5] border-[#D1CCC2]"
                  )}
                >
                  <div className="flex items-center gap-3.5 min-w-0 flex-1">
                    {/* Checkbox */}
                    <button
                      type="button"
                      aria-label={item.isCompleted ? `Mark ${item.title} incomplete` : `Mark ${item.title} complete`}
                      onClick={(e) => {
                        e.stopPropagation();
                        toggleCompleteMutation.mutate({ id: item.id, isCompleted: item.isCompleted });
                      }}
                      className={cn(
                        "w-5 h-5 rounded-[5px] border flex items-center justify-center transition-colors cursor-pointer flex-shrink-0",
                        item.isCompleted
                          ? "bg-[#EA580C] border-[#EA580C] text-white"
                          : "border-[#D1CCC2] bg-white hover:border-[#EA580C]"
                      )}
                    >
                      {item.isCompleted ? <Check className="w-3.5 h-3.5 stroke-[3]" /> : null}
                    </button>

                    {/* Title & Metadata */}
                    <div className="min-w-0 flex-1">
                      <p
                        className={cn(
                          "text-sm font-bold text-[#111827] truncate",
                          item.isCompleted && "line-through text-[#6B7280]"
                        )}
                      >
                        {item.title}
                      </p>
                      <div className="mt-1 flex flex-wrap items-center gap-2 text-xs">
                        {/* Time Slot / Schedule */}
                        <div className="flex items-center gap-1 text-[#6B7280] font-medium">
                          <Clock className="w-3.5 h-3.5 text-[#6B7280]" />
                          <span>{item.timeSlot ?? (item.dueAt ? format(new Date(item.dueAt), "HH:mm") : "Anytime")}</span>
                        </div>

                        {/* Tag Badge */}
                        {item.tags.map((tag) => (
                          <span
                            key={tag}
                            className={cn(
                              "px-2 py-0.5 rounded-full text-[11px] font-semibold border",
                              getTagStyle(tag)
                            )}
                          >
                            {tag}
                          </span>
                        ))}
                      </div>
                    </div>
                  </div>

                  {/* Priority Flag & Actions */}
                  <div className="flex items-center gap-3 flex-shrink-0">
                    {renderPriorityFlag(item.priority)}
                    <button
                      type="button"
                      aria-label="Todo options"
                      onClick={(e) => {
                        e.stopPropagation();
                        setSelectedId(item.id);
                      }}
                      className="text-[#9CA3AF] hover:text-[#111827] p-1 rounded-md transition-colors cursor-pointer"
                    >
                      <MoreHorizontal className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* RIGHT COLUMN: Detail Drawer / Panel (Matches reference image) */}
        {selectedItem ? (
          <div className="lg:col-span-5 rounded-[20px] bg-[#EAE6DE]/75 border border-[#D1CCC2] p-5 sm:p-7 shadow-xs space-y-5">
            {/* Control Bar: Checkbox + Status Dropdown pill ... Edit, Delete, Close */}
            <div className="flex items-center justify-between gap-2 pb-1 border-b border-[#D1CCC2]/40">
              <div className="flex items-center gap-2.5">
                <button
                  type="button"
                  aria-label={selectedItem.isCompleted ? "Mark incomplete" : "Mark complete"}
                  onClick={() =>
                    toggleCompleteMutation.mutate({
                      id: selectedItem.id,
                      isCompleted: selectedItem.isCompleted,
                    })
                  }
                  className={cn(
                    "w-5 h-5 rounded-[5px] border flex items-center justify-center transition-colors cursor-pointer",
                    selectedItem.isCompleted
                      ? "bg-[#EA580C] border-[#EA580C] text-white"
                      : "border-[#D1CCC2] bg-white hover:border-[#EA580C]"
                  )}
                >
                  {selectedItem.isCompleted ? <Check className="w-3.5 h-3.5 stroke-[3]" /> : null}
                </button>

                {/* Status Dropdown Pill */}
                <div className="relative">
                  <button
                    type="button"
                    onClick={() => setStatusMenuOpen((o) => !o)}
                    className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-[#FED7AA] text-[#B45309] text-xs font-bold border border-[#FDBA74] shadow-xs cursor-pointer hover:bg-[#FDBA74] transition-colors"
                  >
                    <span>{selectedItem.status ?? (selectedItem.isCompleted ? "Completed" : "In Progress")}</span>
                    <ChevronDown className="w-3.5 h-3.5 text-[#B45309]" />
                  </button>

                  {statusMenuOpen ? (
                    <div className="absolute left-0 top-full mt-1.5 w-36 bg-white rounded-[10px] border border-[#D1CCC2] shadow-md py-1 z-20">
                      {(["To Do", "In Progress", "Completed"] as const).map((st) => (
                        <button
                          key={st}
                          type="button"
                          onClick={() => handleStatusChange(st)}
                          className="w-full text-left px-3 py-1.5 text-xs font-medium text-[#111827] hover:bg-[#FAF8F5] transition-colors cursor-pointer"
                        >
                          {st}
                        </button>
                      ))}
                    </div>
                  ) : null}
                </div>
              </div>

              {/* Action Icons: Edit, Delete, Close */}
              <div className="flex items-center gap-1.5 text-[#4B5563]">
                {manageable ? (
                  <button
                    type="button"
                    aria-label="Edit todo"
                    onClick={() => setIsEditOpen(true)}
                    className="p-1.5 rounded-[8px] hover:text-[#111827] hover:bg-white/60 transition-colors cursor-pointer"
                  >
                    <Pencil className="w-4 h-4" />
                  </button>
                ) : null}
                {manageable ? (
                  <button
                    type="button"
                    aria-label="Delete todo"
                    onClick={() => {
                      if (confirm("Are you sure you want to delete this todo?")) {
                        deleteMutation.mutate(selectedItem.id);
                      }
                    }}
                    className="p-1.5 rounded-[8px] hover:text-[#DC2626] hover:bg-white/60 transition-colors cursor-pointer"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                ) : null}
                <button
                  type="button"
                  aria-label="Close details"
                  onClick={() => setSelectedId("")}
                  className="p-1.5 rounded-[8px] hover:text-[#111827] hover:bg-white/60 transition-colors cursor-pointer"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>
            </div>

            {/* Title */}
            <div>
              <h2 className="text-xl sm:text-2xl font-bold text-[#111827] leading-snug">
                {selectedItem.title}
              </h2>
            </div>

            {/* Metadata Row: Calendar, Clock, Priority */}
            <div className="flex flex-wrap items-center gap-4 text-xs sm:text-sm text-[#4B5563]">
              <div className="flex items-center gap-1.5">
                <Calendar className="w-4 h-4 text-[#4B5563]" />
                <span className="font-medium">
                  {selectedItem.dueAt ? format(parseISO(selectedItem.dueAt), "d MMMM yyyy") : format(cursor, "d MMMM yyyy")}
                </span>
              </div>
              <div className="flex items-center gap-1.5">
                <Clock className="w-4 h-4 text-[#4B5563]" />
                <span className="font-medium">
                  {selectedItem.timeSlot ?? (selectedItem.dueAt ? format(parseISO(selectedItem.dueAt), "HH:mm") : "09:00 - 09:30")}
                </span>
              </div>
              <div className="flex items-center gap-1.5">
                <Flag
                  className={cn(
                    "w-4 h-4",
                    selectedItem.priority === "High" || selectedItem.priority === "Critical"
                      ? "text-[#DC2626] fill-[#DC2626]"
                      : selectedItem.priority === "Medium"
                      ? "text-[#D97706] fill-[#D97706]"
                      : "text-[#16A34A] fill-[#16A34A]"
                  )}
                />
                <span
                  className={cn(
                    "font-semibold",
                    selectedItem.priority === "High" || selectedItem.priority === "Critical"
                      ? "text-[#DC2626]"
                      : selectedItem.priority === "Medium"
                      ? "text-[#D97706]"
                      : "text-[#16A34A]"
                  )}
                >
                  {selectedItem.priority} Priority
                </span>
              </div>
            </div>

            {/* Tags Row */}
            <div className="flex flex-wrap items-center gap-2">
              {selectedItem.tags.map((tag) => (
                <span
                  key={tag}
                  className={cn("px-2.5 py-1 rounded-full text-xs font-semibold border", getTagStyle(tag))}
                >
                  {tag}
                </span>
              ))}

              {isAddingTag ? (
                <div className="flex items-center gap-1.5">
                  <input
                    type="text"
                    value={newTagText}
                    onChange={(e) => setNewTagText(e.target.value)}
                    placeholder="New tag..."
                    className="h-7 px-2 text-xs rounded-full border border-[#D1CCC2] bg-white focus:outline-none focus:ring-1 focus:ring-[#111827]"
                    onKeyDown={(e) => e.key === "Enter" && handleAddTag()}
                    autoFocus
                  />
                  <button
                    type="button"
                    onClick={handleAddTag}
                    className="text-xs font-bold px-2 py-0.5 rounded-full bg-[#111827] text-white cursor-pointer"
                  >
                    Add
                  </button>
                  <button
                    type="button"
                    onClick={() => setIsAddingTag(false)}
                    className="text-[#6B7280] hover:text-[#111827] cursor-pointer"
                  >
                    <X className="w-3.5 h-3.5" />
                  </button>
                </div>
              ) : (
                <button
                  type="button"
                  onClick={() => setIsAddingTag(true)}
                  className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium text-[#4B5563] bg-white/70 hover:bg-white border border-[#D1CCC2] shadow-2xs transition-colors cursor-pointer"
                >
                  <Plus className="w-3 h-3" />
                  <span>Add tag</span>
                </button>
              )}
            </div>

            {/* Description Section */}
            <div className="space-y-2">
              <h3 className="text-sm font-bold text-[#111827]">Description</h3>
              <p className="text-xs sm:text-sm text-[#4B5563] leading-relaxed">
                {selectedItem.description ?? "No description provided."}
              </p>

              {/* Callout Notice Banner */}
              <div className="flex items-center gap-3 rounded-[12px] bg-[#FFF7ED] border border-[#FFEDD5] p-3 text-xs sm:text-sm text-[#C2410C]">
                <div className="w-5 h-5 rounded-full bg-[#EA580C] text-white flex items-center justify-center font-bold text-xs flex-shrink-0">
                  <Info className="w-3.5 h-3.5" />
                </div>
                <p className="font-medium">
                  {selectedItem.isCompleted
                    ? "This todo has been completed."
                    : selectedItem.bucket === "Today" || (selectedItem.dueAt && isSameDay(parseISO(selectedItem.dueAt), new Date()))
                    ? "This todo is scheduled for today. Don't forget to complete it."
                    : "This todo is scheduled. Don't forget to complete it."}
                </p>
              </div>
            </div>

            {/* Checklist Section */}
            {(() => {
              const list = selectedItem.checklist ?? [];
              const doneCount = list.filter((c) => c.done).length;
              const totalCount = list.length;
              const percent = totalCount > 0 ? Math.round((doneCount / totalCount) * 100) : 0;

              return (
                <div className="space-y-3 pt-1">
                  <div className="flex items-center justify-between gap-4">
                    <h3 className="text-sm font-bold text-[#111827]">
                      Checklist ({doneCount}/{totalCount})
                    </h3>
                    <div className="flex items-center gap-2 flex-1 max-w-[200px]">
                      <div className="flex-1 h-2 bg-[#E5E7EB] rounded-full overflow-hidden">
                        <div
                          className="h-full bg-[#EA580C] rounded-full transition-all duration-300"
                          style={{ width: `${percent}%` }}
                        />
                      </div>
                      <span className="text-xs font-bold text-[#4B5563] min-w-[32px] text-right">
                        {percent}%
                      </span>
                    </div>
                  </div>

                  <ul className="space-y-2">
                    {list.map((c) => (
                      <li key={c.id} className="flex items-center gap-2.5 text-xs sm:text-sm text-[#111827]">
                        <button
                          type="button"
                          onClick={() => handleToggleChecklist(c.id)}
                          className={cn(
                            "w-4 h-4 rounded-[4px] border flex items-center justify-center transition-colors cursor-pointer flex-shrink-0",
                            c.done
                              ? "bg-[#EA580C] border-[#EA580C] text-white"
                              : "border-[#D1CCC2] bg-white hover:border-[#EA580C]"
                          )}
                        >
                          {c.done ? <Check className="w-3 h-3 stroke-[3]" /> : null}
                        </button>
                        <span className={cn(c.done ? "line-through text-[#6B7280]" : "text-[#111827]")}>
                          {c.text}
                        </span>
                      </li>
                    ))}
                  </ul>

                  {isAddingChecklist ? (
                    <div className="flex items-center gap-2 pt-1">
                      <input
                        type="text"
                        value={newChecklistText}
                        onChange={(e) => setNewChecklistText(e.target.value)}
                        placeholder="New checklist item..."
                        className="flex-1 h-8 px-3 rounded-[8px] border border-[#D1CCC2] bg-white text-xs text-[#111827] focus:outline-none focus:ring-1 focus:ring-[#111827]"
                        onKeyDown={(e) => e.key === "Enter" && handleAddChecklistItem()}
                        autoFocus
                      />
                      <button
                        type="button"
                        onClick={handleAddChecklistItem}
                        className="px-3 py-1.5 rounded-[8px] bg-[#111827] text-white text-xs font-semibold cursor-pointer"
                      >
                        Add
                      </button>
                      <button
                        type="button"
                        onClick={() => setIsAddingChecklist(false)}
                        className="text-[#6B7280] hover:text-[#111827] cursor-pointer"
                      >
                        <X className="w-4 h-4" />
                      </button>
                    </div>
                  ) : (
                    <button
                      type="button"
                      onClick={() => setIsAddingChecklist(true)}
                      className="inline-flex items-center gap-1.5 text-xs font-semibold text-[#4B5563] hover:text-[#111827] cursor-pointer pt-0.5"
                    >
                      <Plus className="w-3.5 h-3.5" />
                      <span>Add checklist item</span>
                    </button>
                  )}
                </div>
              );
            })()}

            {/* Notes Section */}
            <div className="space-y-2 pt-1">
              <h3 className="text-sm font-bold text-[#111827]">Notes</h3>
              <textarea
                value={selectedItem.notes ?? ""}
                onChange={(e) => updateSelectedMeta({ notes: e.target.value })}
                placeholder="Add additional notes..."
                className="w-full min-h-[90px] rounded-[12px] border border-[#D1CCC2] bg-white/70 p-3.5 text-xs sm:text-sm text-[#111827] placeholder-[#9CA3AF] shadow-xs focus:outline-none focus:ring-1 focus:ring-[#111827] resize-y"
              />
            </div>
          </div>
        ) : null}
      </div>

      {/* Add Todo Modal Dialog */}
      {isAddOpen ? (
        <AddTodoModal
          cursorDate={cursor}
          onClose={() => setIsAddOpen(false)}
          onAdd={async (data) => {
            if (manageable) {
              const res = await createMutation.mutateAsync({
                title: data.title,
                priority: data.priority,
                dueAt: data.dueAt,
                remindAt: data.remindAt,
                recurrence: data.recurrence,
                tags: data.tags,
              });
              saveMeta(res.id, {
                description: data.description,
                timeSlot: data.timeSlot,
                checklist: data.checklist,
                notes: data.notes,
                status: "To Do",
              });
              setSelectedId(res.id);
            }
            setIsAddOpen(false);
          }}
        />
      ) : null}

      {/* Edit Todo Modal Dialog */}
      {isEditOpen && selectedItem ? (
        <EditTodoModal
          item={selectedItem}
          onClose={() => setIsEditOpen(false)}
          onSave={async (data) => {
            if (selectedItem.id.startsWith("demo-")) {
              const next = localDemos.map((d) =>
                d.id === selectedItem.id
                  ? {
                      ...d,
                      title: data.title,
                      priority: data.priority,
                      dueAt: data.dueAt,
                      tags: data.tags,
                      timeSlot: data.timeSlot,
                      description: data.description,
                    }
                  : d
              );
              persistDemos(next);
            } else {
              await saveTodo(
                {
                  title: data.title,
                  priority: data.priority,
                  dueAt: data.dueAt,
                  remindAt: selectedItem.remindAt,
                  recurrence: selectedItem.recurrence,
                  tags: data.tags,
                },
                selectedItem.id
              );
              updateSelectedMeta({
                timeSlot: data.timeSlot,
                description: data.description,
              });
              await queryClient.invalidateQueries({ queryKey: ["todos"] });
            }
            setIsEditOpen(false);
          }}
        />
      ) : null}
    </div>
  );
}

// Helpers
function getTagStyle(tag: string) {
  const lower = tag.toLowerCase();
  if (lower.includes("work")) return "bg-[#E0F2FE] text-[#0369A1] border-[#BAE6FD]";
  if (lower.includes("design")) return "bg-[#FCE7F3] text-[#BE185D] border-[#FBCFE8]";
  if (lower.includes("dev")) return "bg-[#EDE9FE] text-[#6D28D9] border-[#DDD6FE]";
  if (lower.includes("learn")) return "bg-[#DCFCE7] text-[#15803D] border-[#BBF7D0]";
  if (lower.includes("person")) return "bg-[#FEF3C7] text-[#B45309] border-[#FDE68A]";
  return "bg-[#F3F4F6] text-[#4B5563] border-[#E5E7EB]";
}

function renderPriorityFlag(priority: string) {
  if (priority === "High" || priority === "Critical") {
    return (
      <span className="inline-flex items-center gap-1.5 text-xs font-semibold text-[#DC2626]">
        <Flag className="w-3.5 h-3.5 fill-[#DC2626] text-[#DC2626]" />
        <span>High</span>
      </span>
    );
  }
  if (priority === "Medium") {
    return (
      <span className="inline-flex items-center gap-1.5 text-xs font-semibold text-[#D97706]">
        <Flag className="w-3.5 h-3.5 fill-[#D97706] text-[#D97706]" />
        <span>Medium</span>
      </span>
    );
  }
  return (
    <span className="inline-flex items-center gap-1.5 text-xs font-semibold text-[#16A34A]">
      <Flag className="w-3.5 h-3.5 fill-[#16A34A] text-[#16A34A]" />
      <span>Low</span>
    </span>
  );
}

type AddTodoModalProps = {
  cursorDate: Date;
  onClose: () => void;
  onAdd: (data: {
    title: string;
    priority: string;
    dueAt: string | null;
    remindAt: string | null;
    recurrence: string;
    tags: string[];
    timeSlot: string;
    description: string;
    checklist: { id: string; text: string; done: boolean }[];
    notes: string;
  }) => Promise<void>;
};

function AddTodoModal({ cursorDate, onClose, onAdd }: AddTodoModalProps) {
  const [title, setTitle] = useState("");
  const [priority, setPriority] = useState("Medium");
  const [dueAt, setDueAt] = useState(format(cursorDate, "yyyy-MM-dd'T'17:00"));
  const [timeSlot, setTimeSlot] = useState("09:00 - 09:30");
  const [tags, setTags] = useState("Work");
  const [description, setDescription] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  return (
    <div className="fixed inset-0 bg-black/40 backdrop-blur-xs flex items-center justify-center p-4 z-50">
      <div className="w-full max-w-lg rounded-[20px] bg-white border border-[#D1CCC2] p-6 shadow-xl space-y-4">
        <div className="flex items-center justify-between pb-2 border-b border-[#D1CCC2]/60">
          <h2 className="text-base font-bold text-[#111827]">Add New Todo</h2>
          <button type="button" onClick={onClose} className="p-1 rounded-md text-[#6B7280] hover:text-[#111827] cursor-pointer">
            <X className="w-4 h-4" />
          </button>
        </div>

        <form
          className="space-y-3.5"
          onSubmit={async (e) => {
            e.preventDefault();
            setIsSubmitting(true);
            try {
              await onAdd({
                title: title.trim(),
                priority,
                dueAt: fromDateTimeInput(dueAt),
                remindAt: null,
                recurrence: "None",
                tags: tags.split(",").map((t) => t.trim()).filter(Boolean),
                timeSlot,
                description: description.trim(),
                checklist: [],
                notes: "",
              });
            } finally {
              setIsSubmitting(false);
            }
          }}
        >
          <div>
            <label className="block text-xs font-bold text-[#111827] mb-1">Title</label>
            <input
              type="text"
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="e.g. Prepare Standup Call Notes"
              className="w-full h-10 px-3.5 rounded-[10px] border border-[#D1CCC2] bg-white text-sm text-[#111827] placeholder-[#9CA3AF] focus:outline-none focus:ring-1 focus:ring-[#111827]"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-[#111827] mb-1">Time Slot / Schedule</label>
              <input
                type="text"
                value={timeSlot}
                onChange={(e) => setTimeSlot(e.target.value)}
                placeholder="e.g. 09:00 - 09:30"
                className="w-full h-10 px-3 rounded-[10px] border border-[#D1CCC2] bg-white text-xs sm:text-sm text-[#111827] focus:outline-none focus:ring-1 focus:ring-[#111827]"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-[#111827] mb-1">Priority</label>
              <select
                value={priority}
                onChange={(e) => setPriority(e.target.value)}
                className="w-full h-10 px-3 rounded-[10px] border border-[#D1CCC2] bg-white text-xs sm:text-sm text-[#111827] focus:outline-none focus:ring-1 focus:ring-[#111827]"
              >
                {priorities.map((p) => (
                  <option key={p} value={p}>
                    {p}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-[#111827] mb-1">Due Date & Time</label>
              <input
                type="datetime-local"
                value={dueAt}
                onChange={(e) => setDueAt(e.target.value)}
                className="w-full h-10 px-3 rounded-[10px] border border-[#D1CCC2] bg-white text-xs font-mono text-[#111827] focus:outline-none focus:ring-1 focus:ring-[#111827]"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-[#111827] mb-1">Tags (comma separated)</label>
              <input
                type="text"
                value={tags}
                onChange={(e) => setTags(e.target.value)}
                placeholder="Work, Design, etc."
                className="w-full h-10 px-3 rounded-[10px] border border-[#D1CCC2] bg-white text-xs sm:text-sm text-[#111827] focus:outline-none focus:ring-1 focus:ring-[#111827]"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-bold text-[#111827] mb-1">Description</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="What are the details or updates for this todo?"
              className="w-full min-h-[70px] rounded-[10px] border border-[#D1CCC2] bg-white p-3 text-xs sm:text-sm text-[#111827] placeholder-[#9CA3AF] focus:outline-none focus:ring-1 focus:ring-[#111827] resize-y"
            />
          </div>

          <div className="flex items-center justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-[10px] border border-[#D1CCC2] text-xs sm:text-sm font-semibold text-[#4B5563] hover:bg-[#FAF8F5] cursor-pointer"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-5 py-2 rounded-[10px] bg-[#FED7AA] hover:bg-[#FDBA74] text-[#111827] text-xs sm:text-sm font-bold border border-[#111827]/15 shadow-xs cursor-pointer disabled:opacity-50"
            >
              Create todo
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

type EditTodoModalProps = {
  item: TodoItem & TodoMeta;
  onClose: () => void;
  onSave: (data: {
    title: string;
    priority: string;
    dueAt: string | null;
    tags: string[];
    timeSlot: string;
    description: string;
  }) => Promise<void>;
};

function EditTodoModal({ item, onClose, onSave }: EditTodoModalProps) {
  const [title, setTitle] = useState(item.title);
  const [priority, setPriority] = useState(item.priority);
  const [dueAt, setDueAt] = useState(toDateTimeInput(item.dueAt));
  const [timeSlot, setTimeSlot] = useState(item.timeSlot ?? "");
  const [tags, setTags] = useState(item.tags.join(", "));
  const [description, setDescription] = useState(item.description ?? "");
  const [isSubmitting, setIsSubmitting] = useState(false);

  return (
    <div className="fixed inset-0 bg-black/40 backdrop-blur-xs flex items-center justify-center p-4 z-50">
      <div className="w-full max-w-lg rounded-[20px] bg-white border border-[#D1CCC2] p-6 shadow-xl space-y-4">
        <div className="flex items-center justify-between pb-2 border-b border-[#D1CCC2]/60">
          <h2 className="text-base font-bold text-[#111827]">Edit Todo</h2>
          <button type="button" onClick={onClose} className="p-1 rounded-md text-[#6B7280] hover:text-[#111827] cursor-pointer">
            <X className="w-4 h-4" />
          </button>
        </div>

        <form
          className="space-y-3.5"
          onSubmit={async (e) => {
            e.preventDefault();
            setIsSubmitting(true);
            try {
              await onSave({
                title: title.trim(),
                priority,
                dueAt: fromDateTimeInput(dueAt),
                tags: tags.split(",").map((t) => t.trim()).filter(Boolean),
                timeSlot,
                description: description.trim(),
              });
            } finally {
              setIsSubmitting(false);
            }
          }}
        >
          <div>
            <label className="block text-xs font-bold text-[#111827] mb-1">Title</label>
            <input
              type="text"
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className="w-full h-10 px-3.5 rounded-[10px] border border-[#D1CCC2] bg-white text-sm text-[#111827] focus:outline-none focus:ring-1 focus:ring-[#111827]"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-[#111827] mb-1">Time Slot</label>
              <input
                type="text"
                value={timeSlot}
                onChange={(e) => setTimeSlot(e.target.value)}
                className="w-full h-10 px-3 rounded-[10px] border border-[#D1CCC2] bg-white text-xs sm:text-sm text-[#111827] focus:outline-none focus:ring-1 focus:ring-[#111827]"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-[#111827] mb-1">Priority</label>
              <select
                value={priority}
                onChange={(e) => setPriority(e.target.value)}
                className="w-full h-10 px-3 rounded-[10px] border border-[#D1CCC2] bg-white text-xs sm:text-sm text-[#111827] focus:outline-none focus:ring-1 focus:ring-[#111827]"
              >
                {priorities.map((p) => (
                  <option key={p} value={p}>
                    {p}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-bold text-[#111827] mb-1">Due Date</label>
              <input
                type="datetime-local"
                value={dueAt}
                onChange={(e) => setDueAt(e.target.value)}
                className="w-full h-10 px-3 rounded-[10px] border border-[#D1CCC2] bg-white text-xs font-mono text-[#111827] focus:outline-none focus:ring-1 focus:ring-[#111827]"
              />
            </div>
            <div>
              <label className="block text-xs font-bold text-[#111827] mb-1">Tags</label>
              <input
                type="text"
                value={tags}
                onChange={(e) => setTags(e.target.value)}
                className="w-full h-10 px-3 rounded-[10px] border border-[#D1CCC2] bg-white text-xs sm:text-sm text-[#111827] focus:outline-none focus:ring-1 focus:ring-[#111827]"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-bold text-[#111827] mb-1">Description</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="w-full min-h-[70px] rounded-[10px] border border-[#D1CCC2] bg-white p-3 text-xs sm:text-sm text-[#111827] focus:outline-none focus:ring-1 focus:ring-[#111827] resize-y"
            />
          </div>

          <div className="flex items-center justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-[10px] border border-[#D1CCC2] text-xs sm:text-sm font-semibold text-[#4B5563] hover:bg-[#FAF8F5] cursor-pointer"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-5 py-2 rounded-[10px] bg-[#FED7AA] hover:bg-[#FDBA74] text-[#111827] text-xs sm:text-sm font-bold border border-[#111827]/15 shadow-xs cursor-pointer disabled:opacity-50"
            >
              Save changes
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
