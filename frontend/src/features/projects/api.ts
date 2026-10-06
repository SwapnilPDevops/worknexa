import { z } from "zod";
import { readData } from "../../auth/session.ts";
import { getJson, sendJson } from "../../services/api-client.ts";

const namedRefSchema = z.object({ id: z.string(), name: z.string() });

const memberSchema = z.object({
  userId: z.string(),
  name: z.string(),
  role: z.string(),
});

const deadlineSchema = z.object({
  taskId: z.string(),
  title: z.string(),
  dueAt: z.string(),
  assigneeName: z.string().nullable(),
});

const workloadSchema = z.object({
  assigneeId: z.string().nullable(),
  name: z.string(),
  openTasks: z.number(),
  blockedTasks: z.number(),
});

const dashboardSchema = z.object({
  progress: z.number(),
  openTasks: z.number(),
  completedTasks: z.number(),
  blockedTasks: z.number(),
  upcomingDeadlines: z.array(deadlineSchema),
  teamWorkload: z.array(workloadSchema),
});

export const projectListSchema = z.object({
  id: z.string(),
  name: z.string(),
  code: z.string(),
  status: z.string(),
  priority: z.string(),
  ownerId: z.string(),
  ownerName: z.string(),
  teamId: z.string().nullable(),
  teamName: z.string().nullable(),
  startDate: z.string().nullable(),
  targetDate: z.string().nullable(),
  progress: z.number(),
  openTasks: z.number(),
});

export type ProjectListItem = z.infer<typeof projectListSchema>;

export const projectDetailSchema = z.object({
  id: z.string(),
  name: z.string(),
  code: z.string(),
  description: z.string().nullable(),
  ownerId: z.string(),
  ownerName: z.string(),
  teamId: z.string().nullable(),
  teamName: z.string().nullable(),
  status: z.string(),
  priority: z.string(),
  startDate: z.string().nullable(),
  targetDate: z.string().nullable(),
  technology: z.string().nullable(),
  repositoryUrl: z.string().nullable(),
  documentationUrl: z.string().nullable(),
  members: z.array(memberSchema),
  dashboard: dashboardSchema,
});

export type ProjectDetail = z.infer<typeof projectDetailSchema>;

export const projectOptionsSchema = z.object({
  people: z.array(namedRefSchema),
  teams: z.array(namedRefSchema),
});

export type ProjectOptions = z.infer<typeof projectOptionsSchema>;

export type ProjectWrite = {
  name: string;
  code: string;
  description: string | null;
  ownerId: string | null;
  teamId: string | null;
  status: string;
  priority: string;
  startDate: string | null;
  targetDate: string | null;
  technology: string | null;
  repositoryUrl: string | null;
  documentationUrl: string | null;
};

const activitySchema = z.object({
  at: z.string(),
  action: z.string(),
  summary: z.string(),
  actorName: z.string(),
});

const meetingSchema = z.object({
  id: z.string(),
  title: z.string(),
  meetingType: z.string(),
  startAt: z.string(),
});

const noteSchema = z.object({
  id: z.string(),
  title: z.string(),
  updatedAt: z.string(),
  isPinned: z.boolean(),
});

export async function fetchProjects(query: string) {
  const path = query ? `/api/projects?q=${encodeURIComponent(query)}` : "/api/projects";
  return readData(await getJson(path), z.array(projectListSchema));
}

export async function fetchProjectOptions() {
  return readData(await getJson("/api/projects/options"), projectOptionsSchema);
}

export async function fetchProject(id: string) {
  return readData(await getJson(`/api/projects/${id}`), projectDetailSchema);
}

export async function saveProject(project: ProjectWrite, id?: string) {
  const path = id ? `/api/projects/${id}` : "/api/projects";
  return readData(await sendJson(path, id ? "PUT" : "POST", project), projectDetailSchema);
}

export async function deleteProject(id: string) {
  await sendJson(`/api/projects/${id}`, "DELETE");
}

export async function addProjectMember(projectId: string, userId: string, role: string) {
  return readData(await sendJson(`/api/projects/${projectId}/members`, "POST", { userId, role }), projectDetailSchema);
}

export async function removeProjectMember(projectId: string, userId: string) {
  return readData(await sendJson(`/api/projects/${projectId}/members/${userId}`, "DELETE"), projectDetailSchema);
}

export async function fetchProjectActivity(id: string) {
  return readData(await getJson(`/api/projects/${id}/activity`), z.array(activitySchema));
}

export async function fetchProjectMeetings(id: string) {
  return readData(await getJson(`/api/projects/${id}/meetings`), z.array(meetingSchema));
}

export async function fetchProjectNotes(id: string) {
  return readData(await getJson(`/api/projects/${id}/notes`), z.array(noteSchema));
}
