export const taskStatuses = ["Backlog", "Todo", "InProgress", "Blocked", "Review", "Completed", "Cancelled"] as const;

export const priorities = ["Low", "Medium", "High", "Critical"] as const;

export const recurrences = ["None", "Daily", "Weekly", "Monthly"] as const;

export const todoBuckets = ["Overdue", "Today", "Tomorrow", "ThisWeek", "Upcoming", "Completed"] as const;

export const taskViews = [
  { id: "list", label: "List" },
  { id: "kanban", label: "Kanban" },
  { id: "calendar", label: "Calendar" },
  { id: "mine", label: "My Tasks" },
  { id: "team", label: "Team Tasks" },
  { id: "overdue", label: "Overdue" },
] as const;

export type TaskView = (typeof taskViews)[number]["id"];

export type TaskStatus = (typeof taskStatuses)[number];

export type WorkPriority = (typeof priorities)[number];

const statusLabels: Record<string, string> = {
  InProgress: "In Progress",
  ThisWeek: "This week",
};

export function labelFor(value: string) {
  return statusLabels[value] ?? value;
}

export function priorityTone(priority: string): "neutral" | "accent" | "warning" | "danger" {
  if (priority === "Critical") {
    return "danger";
  }

  if (priority === "High") {
    return "warning";
  }

  if (priority === "Medium") {
    return "accent";
  }

  return "neutral";
}
