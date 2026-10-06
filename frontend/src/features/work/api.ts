import { z } from "zod";
import { readData } from "../../auth/session.ts";
import { getBlob, getJson, sendForm, sendJson } from "../../services/api-client.ts";

const namedRefSchema = z.object({ id: z.string(), name: z.string() });

export const workOptionsSchema = z.object({
  projects: z.array(namedRefSchema),
  people: z.array(namedRefSchema),
  teams: z.array(namedRefSchema),
  tags: z.array(z.string()),
});

export type WorkOptions = z.infer<typeof workOptionsSchema>;

export const taskListItemSchema = z.object({
  id: z.string(),
  title: z.string(),
  status: z.string(),
  priority: z.string(),
  startAt: z.string().nullable(),
  dueAt: z.string().nullable(),
  assigneeId: z.string().nullable(),
  assigneeName: z.string().nullable(),
  projectId: z.string().nullable(),
  projectName: z.string().nullable(),
  parentTaskId: z.string().nullable(),
  teamId: z.string().nullable(),
  estimatedHours: z.number().nullable(),
  actualHours: z.number().nullable(),
  tags: z.array(z.string()),
});

export type TaskListItem = z.infer<typeof taskListItemSchema>;

export const taskDetailSchema = taskListItemSchema.extend({
  description: z.string().nullable(),
  parentTitle: z.string().nullable(),
  subtasks: z.array(z.object({ id: z.string(), title: z.string(), status: z.string() })),
  dependencies: z.array(z.object({
    id: z.string(),
    dependsOnTaskId: z.string(),
    title: z.string(),
    status: z.string(),
  })),
  comments: z.array(z.object({
    id: z.string(),
    body: z.string(),
    authorName: z.string(),
    createdAt: z.string(),
  })),
  attachments: z.array(z.object({
    id: z.string(),
    fileName: z.string(),
    contentType: z.string(),
    sizeBytes: z.number(),
  })),
  meetingId: z.string().nullable(),
  meetingTitle: z.string().nullable(),
});

export type TaskDetail = z.infer<typeof taskDetailSchema>;

export const todoSchema = z.object({
  id: z.string(),
  title: z.string(),
  isCompleted: z.boolean(),
  priority: z.string(),
  recurrence: z.string(),
  dueAt: z.string().nullable(),
  remindAt: z.string().nullable(),
  bucket: z.string(),
  tags: z.array(z.string()),
});

export type TodoItem = z.infer<typeof todoSchema>;

export type TaskWrite = {
  title: string;
  description: string | null;
  status: string;
  priority: string;
  startAt: string | null;
  dueAt: string | null;
  assigneeId: string | null;
  projectId: string | null;
  parentTaskId: string | null;
  teamId: string | null;
  estimatedHours: number | null;
  actualHours: number | null;
  tags: string[];
};

export type TodoWrite = {
  title: string;
  priority: string;
  dueAt: string | null;
  remindAt: string | null;
  recurrence: string;
  tags: string[];
};

export function localOffsetMinutes(now = new Date()) {
  return -now.getTimezoneOffset();
}

export function toDateTimeInput(value: string | null) {
  if (!value) {
    return "";
  }

  const date = new Date(value);
  const pad = (part: number) => String(part).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function fromDateTimeInput(value: string) {
  return value ? new Date(value).toISOString() : null;
}

export async function fetchWorkOptions() {
  return readData(await getJson("/api/work/options"), workOptionsSchema);
}

export async function fetchTasks(query: string) {
  return readData(await getJson(`/api/tasks${query}`), z.array(taskListItemSchema));
}

export async function fetchTask(id: string) {
  return readData(await getJson(`/api/tasks/${id}`), taskDetailSchema);
}

export async function saveTask(task: TaskWrite, id?: string) {
  const path = id ? `/api/tasks/${id}` : "/api/tasks";
  return readData(await sendJson(path, id ? "PUT" : "POST", task), taskDetailSchema);
}

export async function setTaskStatus(id: string, status: string) {
  return readData(await sendJson(`/api/tasks/${id}/status`, "POST", { status }), taskDetailSchema);
}

export async function completeTask(id: string) {
  return readData(await sendJson(`/api/tasks/${id}/complete`, "POST"), taskDetailSchema);
}

export async function reopenTask(id: string) {
  return readData(await sendJson(`/api/tasks/${id}/reopen`, "POST"), taskDetailSchema);
}

export async function deleteTask(id: string) {
  await sendJson(`/api/tasks/${id}`, "DELETE");
}

export async function assignTask(id: string, assigneeId: string | null) {
  return readData(await sendJson(`/api/tasks/${id}/assignee`, "PUT", { assigneeId }), taskDetailSchema);
}

export async function commentOnTask(id: string, body: string) {
  return sendJson(`/api/tasks/${id}/comments`, "POST", { body });
}

export async function addTaskDependency(id: string, dependsOnTaskId: string) {
  return readData(await sendJson(`/api/tasks/${id}/dependencies`, "POST", { dependsOnTaskId }), taskDetailSchema);
}

export async function removeTaskDependency(id: string, dependencyId: string) {
  await sendJson(`/api/tasks/${id}/dependencies/${dependencyId}`, "DELETE");
}

export async function uploadTaskFile(id: string, file: File) {
  const form = new FormData();
  form.append("file", file);
  return sendForm(`/api/tasks/${id}/attachments`, form);
}

export async function downloadTaskFile(taskId: string, attachmentId: string, fileName: string) {
  const blob = await getBlob(`/api/tasks/${taskId}/attachments/${attachmentId}`);
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

export async function deleteTaskFile(taskId: string, attachmentId: string) {
  await sendJson(`/api/tasks/${taskId}/attachments/${attachmentId}`, "DELETE");
}

export async function fetchTodos(date: string, offsetMinutes: number) {
  return readData(await getJson(`/api/todos?date=${date}&offsetMinutes=${offsetMinutes}`), z.array(todoSchema));
}

export async function saveTodo(todo: TodoWrite, id?: string) {
  const path = id ? `/api/todos/${id}` : "/api/todos";
  return readData(await sendJson(path, id ? "PUT" : "POST", todo), todoSchema);
}

export async function completeTodo(id: string) {
  return readData(await sendJson(`/api/todos/${id}/complete`, "POST"), todoSchema);
}

export async function reopenTodo(id: string) {
  return readData(await sendJson(`/api/todos/${id}/reopen`, "POST"), todoSchema);
}

export async function deleteTodo(id: string) {
  await sendJson(`/api/todos/${id}`, "DELETE");
}
