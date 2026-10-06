import { z } from "zod";
import { readData } from "../../auth/session.ts";
import { getJson, sendJson } from "../../services/api-client.ts";
import { localOffsetMinutes } from "../work/api.ts";

const namedRefSchema = z.object({ id: z.string(), name: z.string() });

const actionSchema = z.object({
  id: z.string(),
  title: z.string(),
  assigneeId: z.string().nullable(),
  assigneeName: z.string().nullable(),
  isCompleted: z.boolean(),
  dueAt: z.string().nullable(),
});

export const meetingListSchema = z.object({
  id: z.string(),
  title: z.string(),
  meetingType: z.string(),
  startAt: z.string(),
  endAt: z.string(),
  durationMinutes: z.number(),
  location: z.string().nullable(),
  meetingLink: z.string().nullable(),
  projectId: z.string().nullable(),
  projectName: z.string().nullable(),
  teamId: z.string().nullable(),
  teamName: z.string().nullable(),
  participants: z.array(namedRefSchema),
});

export type MeetingListItem = z.infer<typeof meetingListSchema>;

export const meetingDetailSchema = meetingListSchema.extend({
  agenda: z.string().nullable(),
  notes: z.string().nullable(),
  decisions: z.string().nullable(),
  followUpAt: z.string().nullable(),
  actionItems: z.array(actionSchema),
  editable: z.boolean(),
});

export type MeetingDetail = z.infer<typeof meetingDetailSchema>;

export type MeetingActionWrite = {
  id: string | null;
  title: string;
  assigneeId: string | null;
  dueAt: string | null;
  isCompleted: boolean;
};

export type MeetingWrite = {
  title: string;
  meetingType: string;
  startAt: string;
  durationMinutes: number;
  projectId: string | null;
  teamId: string | null;
  location: string | null;
  meetingLink: string | null;
  agenda: string | null;
  notes: string | null;
  decisions: string | null;
  followUpAt: string | null;
  participantIds: string[];
  actionItems: MeetingActionWrite[];
};

const standupLineSchema = z.object({
  entryId: z.string().nullable(),
  userId: z.string(),
  memberName: z.string(),
  yesterday: z.string().nullable(),
  today: z.string().nullable(),
  blockers: z.string().nullable(),
  notes: z.string().nullable(),
  status: z.string(),
  mine: z.boolean(),
});

export const standupBoardSchema = z.object({
  date: z.string(),
  standupId: z.string().nullable(),
  teamId: z.string().nullable(),
  teamName: z.string().nullable(),
  meetingId: z.string().nullable(),
  meetingTitle: z.string().nullable(),
  lines: z.array(standupLineSchema),
});

export type StandupBoard = z.infer<typeof standupBoardSchema>;
export type StandupLine = z.infer<typeof standupLineSchema>;

export type StandupWrite = {
  date: string;
  teamId: string | null;
  yesterday: string | null;
  today: string | null;
  blockers: string | null;
  notes: string | null;
  status: string;
};

const standupHitSchema = z.object({
  date: z.string(),
  memberName: z.string(),
  yesterday: z.string().nullable(),
  today: z.string().nullable(),
  blockers: z.string().nullable(),
  notes: z.string().nullable(),
  status: z.string(),
  teamName: z.string().nullable(),
});

export type StandupHit = z.infer<typeof standupHitSchema>;

export function localDateTime(value: string) {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  const pad = (part: number) => String(part).padStart(2, "0");
  const offset = -date.getTimezoneOffset();
  const sign = offset >= 0 ? "+" : "-";
  const absolute = Math.abs(offset);
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:00${sign}${pad(Math.floor(absolute / 60))}:${pad(absolute % 60)}`;
}

export async function fetchMeetings(view: string, date: string) {
  const params = new URLSearchParams({ view, date, offsetMinutes: String(localOffsetMinutes()) });
  return readData(await getJson(`/api/meetings?${params}`), z.array(meetingListSchema));
}

export async function fetchMeeting(id: string) {
  return readData(await getJson(`/api/meetings/${id}`), meetingDetailSchema);
}

export async function saveMeeting(meeting: MeetingWrite, id?: string) {
  const path = id ? `/api/meetings/${id}` : "/api/meetings";
  return readData(await sendJson(path, id ? "PUT" : "POST", meeting), meetingDetailSchema);
}

export async function deleteMeeting(id: string) {
  await sendJson(`/api/meetings/${id}`, "DELETE");
}

export async function fetchStandup(date: string, teamId: string) {
  const params = new URLSearchParams({ date });
  if (teamId) {
    params.set("teamId", teamId);
  }

  return readData(await getJson(`/api/standups?${params}`), standupBoardSchema);
}

export async function saveStandup(entry: StandupWrite) {
  return readData(await sendJson("/api/standups", "POST", entry), standupBoardSchema);
}

export async function searchStandups(query: string) {
  const params = new URLSearchParams();
  if (query) {
    params.set("q", query);
  }

  return readData(await getJson(`/api/standups/search?${params}`), z.array(standupHitSchema));
}
