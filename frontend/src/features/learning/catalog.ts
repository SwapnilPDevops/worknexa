export const learningStatuses = ["NotStarted", "Learning", "Practicing", "Completed"] as const;

export const learningPriorities = ["Low", "Medium", "High", "Critical"] as const;

export const resourceTypes = ["Article", "Video", "Course", "Book", "Other"] as const;

const statusLabels: Record<string, string> = {
  NotStarted: "Not Started",
  Learning: "Learning",
  Practicing: "Practicing",
  Completed: "Completed",
};

export function learningLabel(value: string) {
  return statusLabels[value] ?? value;
}

export function learningTone(status: string): "neutral" | "accent" | "warning" | "signal" {
  if (status === "Completed") {
    return "signal";
  }

  if (status === "Practicing") {
    return "warning";
  }

  if (status === "Learning") {
    return "accent";
  }

  return "neutral";
}

export function suggestedProgress(status: string) {
  if (status === "Learning") {
    return 25;
  }

  if (status === "Practicing") {
    return 60;
  }

  if (status === "Completed") {
    return 100;
  }

  return 0;
}
