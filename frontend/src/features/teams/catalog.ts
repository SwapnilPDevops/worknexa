export const memberStatuses = ["Active", "OnLeave", "Inactive"] as const;

export const teamRoles = ["Owner", "Member"] as const;

export const memberTabs = [
  { id: "details", label: "Details" },
  { id: "tasks", label: "Tasks" },
  { id: "projects", label: "Projects" },
  { id: "leaves", label: "Leaves" },
  { id: "standups", label: "Standups" },
  { id: "activity", label: "Activity" },
  { id: "skills", label: "Skills" },
] as const;

export type MemberTab = (typeof memberTabs)[number]["id"];

const labels: Record<string, string> = {
  OnLeave: "On Leave",
  InProgress: "In Progress",
};

export function labelFor(value: string) {
  return labels[value] ?? value;
}

export function statusTone(status: string): "neutral" | "accent" | "warning" | "danger" {
  if (status === "Inactive") {
    return "danger";
  }

  if (status === "OnLeave") {
    return "warning";
  }

  if (status === "Active") {
    return "accent";
  }

  return "neutral";
}
