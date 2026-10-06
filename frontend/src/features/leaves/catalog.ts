export const leaveStatuses = ["Pending", "Approved", "Rejected", "Cancelled"] as const;

export const leaveViews = [
  { id: "calendar", label: "Calendar" },
  { id: "list", label: "List" },
  { id: "availability", label: "Team availability" },
] as const;

export type LeaveView = (typeof leaveViews)[number]["id"];

const labels: Record<string, string> = {
  OnLeave: "On leave",
};

export function labelFor(value: string) {
  return labels[value] ?? value;
}

export function leaveTone(status: string): "neutral" | "accent" | "warning" | "danger" {
  if (status === "Approved" || status === "OnLeave") {
    return "warning";
  }

  if (status === "Rejected" || status === "Cancelled") {
    return "danger";
  }

  if (status === "Pending") {
    return "accent";
  }

  return "neutral";
}
