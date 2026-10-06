export const projectStatuses = ["Planning", "Active", "OnHold", "Completed", "Cancelled"] as const;

export const projectPriorities = ["Low", "Medium", "High", "Critical"] as const;

export const memberRoles = ["Owner", "Member", "Viewer"] as const;

export const projectTabs = [
  { id: "overview", label: "Overview" },
  { id: "tasks", label: "Tasks" },
  { id: "board", label: "Board" },
  { id: "timeline", label: "Timeline" },
  { id: "team", label: "Team" },
  { id: "meetings", label: "Meetings" },
  { id: "notes", label: "Notes" },
  { id: "activity", label: "Activity" },
] as const;

export type ProjectTab = (typeof projectTabs)[number]["id"];

const labels: Record<string, string> = {
  OnHold: "On Hold",
  InProgress: "In Progress",
};

export function labelFor(value: string) {
  return labels[value] ?? value;
}

export function statusTone(status: string): "neutral" | "accent" | "warning" | "danger" | "signal" {
  if (status === "Active") return "accent";
  if (status === "OnHold") return "warning";
  if (status === "Completed") return "signal";
  if (status === "Cancelled") return "danger";
  return "neutral";
}
