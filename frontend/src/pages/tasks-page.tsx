import { TasksView } from "../features/tasks/tasks-view.tsx";
import type { TaskView } from "../features/work/catalog.ts";

export function TasksPage({ initialView }: { initialView: TaskView }) {
  return <TasksView initialView={initialView} />;
}
