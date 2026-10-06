export const calendarViews = [
  { id: "month", label: "Month" },
  { id: "week", label: "Week" },
  { id: "day", label: "Day" },
  { id: "agenda", label: "Agenda" },
] as const;

export type CalendarView = (typeof calendarViews)[number]["id"];

export const eventKinds = ["Meeting", "Task", "Personal", "Learning", "Deadline"] as const;

export const scheduleRecurrences = ["None", "Daily", "Weekly", "Monthly", "Custom"] as const;

const kindClass: Record<string, string> = {
  Meeting: "bg-accent-soft text-accent",
  Task: "bg-signal-soft text-signal",
  Personal: "bg-surface-muted text-ink",
  Learning: "bg-warning-soft text-warning",
  Deadline: "bg-danger-soft text-danger",
};

export function kindClassName(kind: string) {
  return kindClass[kind] ?? kindClass.Personal;
}
