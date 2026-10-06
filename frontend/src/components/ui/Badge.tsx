import type { Priority } from "../../data/board";

const labels: Record<Priority, string> = {
  Low: "Low",
  High: "High",
  Completed: "Completed",
};

export function PriorityBadge({ priority }: { priority: Priority }) {
  return <span className={`priority priority-${priority.toLowerCase()}`}>{labels[priority]}</span>;
}

export function CountBadge({ value }: { value: number }) {
  return <span className="count-badge">{value}</span>;
}
