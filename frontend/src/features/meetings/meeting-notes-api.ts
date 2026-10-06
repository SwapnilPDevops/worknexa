import { z } from "zod";
import { readData } from "../../auth/session.ts";
import { getJson, sendJson } from "../../services/api-client.ts";
import { localDateTime } from "../meetings/api.ts";

const actionSchema = z.object({
  id: z.string(),
  title: z.string(),
  description: z.string().nullable(),
  assigneeId: z.string().nullable(),
  assigneeName: z.string().nullable(),
  dueAt: z.string().nullable(),
  isCompleted: z.boolean(),
  taskId: z.string().nullable(),
  taskTitle: z.string().nullable(),
});

export const meetingNoteListSchema = z.object({
  id: z.string(),
  title: z.string(),
  startAt: z.string(),
  updatedAt: z.string(),
});

export type MeetingNoteListItem = z.infer<typeof meetingNoteListSchema>;

export const meetingNoteSchema = z.object({
  id: z.string(),
  title: z.string(),
  startAt: z.string(),
  projectId: z.string().nullable(),
  projectName: z.string().nullable(),
  agenda: z.string().nullable(),
  discussion: z.string().nullable(),
  decisions: z.string().nullable(),
  followUps: z.string().nullable(),
  followUpAt: z.string().nullable(),
  actions: z.array(actionSchema),
  updatedAt: z.string(),
});

export type MeetingNoteDocument = z.infer<typeof meetingNoteSchema>;
export type MeetingNoteAction = z.infer<typeof actionSchema>;

export type MeetingNoteActionWrite = {
  id: string | null;
  title: string;
  description: string | null;
  assigneeId: string | null;
  dueAt: string | null;
  isCompleted: boolean;
};

export type MeetingNoteWrite = {
  agenda: string | null;
  discussion: string | null;
  decisions: string | null;
  followUps: string | null;
  followUpAt: string | null;
  actions: MeetingNoteActionWrite[];
};

export async function fetchMeetingNotes(query: string) {
  const params = new URLSearchParams();
  if (query) {
    params.set("q", query);
  }

  return readData(await getJson(`/api/meeting-notes?${params}`), z.array(meetingNoteListSchema));
}

export async function fetchMeetingNote(id: string) {
  return readData(await getJson(`/api/meeting-notes/${id}`), meetingNoteSchema);
}

export async function saveMeetingNote(id: string, note: MeetingNoteWrite) {
  return readData(await sendJson(`/api/meeting-notes/${id}`, "PUT", note), meetingNoteSchema);
}

export async function convertActionToTask(meetingId: string, actionId: string) {
  return readData(await sendJson(`/api/meeting-notes/${meetingId}/actions/${actionId}/task`, "POST"), meetingNoteSchema);
}

export { localDateTime };
