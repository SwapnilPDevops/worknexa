import { z } from "zod";
import { readData } from "../../auth/session.ts";
import { getJson, sendJson } from "../../services/api-client.ts";
import { format } from "date-fns";

const entrySchema = z.object({
  source: z.string(),
  origin: z.string(),
  originId: z.string(),
  kind: z.string(),
  title: z.string(),
  description: z.string().nullable(),
  startAt: z.string(),
  endAt: z.string(),
  isAllDay: z.boolean(),
  location: z.string().nullable(),
  meetingLink: z.string().nullable(),
  projectId: z.string().nullable(),
  projectName: z.string().nullable(),
  editable: z.boolean(),
});

export type CalendarEntry = z.infer<typeof entrySchema>;

const participantSchema = z.object({ id: z.string(), name: z.string() });

export const eventDetailSchema = z.object({
  id: z.string(),
  title: z.string(),
  description: z.string().nullable(),
  kind: z.string(),
  startAt: z.string(),
  endAt: z.string(),
  isAllDay: z.boolean(),
  location: z.string().nullable(),
  meetingLink: z.string().nullable(),
  projectId: z.string().nullable(),
  projectName: z.string().nullable(),
  recurrence: z.string(),
  recurrenceInterval: z.number().nullable(),
  participants: z.array(participantSchema),
  remindAt: z.string().nullable(),
  editable: z.boolean(),
});

export type CalendarEventDetail = z.infer<typeof eventDetailSchema>;

export type CalendarEventWrite = {
  title: string;
  description: string | null;
  kind: string;
  startAt: string;
  endAt: string;
  isAllDay: boolean;
  location: string | null;
  meetingLink: string | null;
  projectId: string | null;
  recurrence: string;
  recurrenceInterval: number | null;
  participantIds: string[];
  remindAt: string | null;
};

const reminderSchema = z.object({
  id: z.string(),
  title: z.string(),
  description: z.string().nullable(),
  remindAt: z.string(),
  recurrence: z.string(),
  recurrenceInterval: z.number().nullable(),
  priority: z.string(),
  isCompleted: z.boolean(),
  taskId: z.string().nullable(),
  taskTitle: z.string().nullable(),
  meetingId: z.string().nullable(),
  meetingTitle: z.string().nullable(),
  calendarEventId: z.string().nullable(),
});

export type ReminderItem = z.infer<typeof reminderSchema>;

export type ReminderWrite = {
  title: string;
  description: string | null;
  remindAt: string;
  recurrence: string;
  recurrenceInterval: number | null;
  priority: string;
  taskId: string | null;
  meetingId: string | null;
};

const linksSchema = z.object({
  tasks: z.array(participantSchema),
  meetings: z.array(participantSchema),
});

export type ReminderLinks = z.infer<typeof linksSchema>;

const notificationSchema = z.object({
  id: z.string(),
  title: z.string(),
  body: z.string(),
  isRead: z.boolean(),
  readAt: z.string().nullable(),
  link: z.string().nullable(),
  createdAt: z.string(),
  type: z.string(),
});

export type NotificationItem = z.infer<typeof notificationSchema>;

export function offsetStamp(date: Date) {
  return format(date, "yyyy-MM-dd'T'HH:mm:ssXXX");
}

export async function fetchCalendar(from: Date, to: Date) {
  const params = new URLSearchParams({ from: offsetStamp(from), to: offsetStamp(to) });
  return readData(await getJson(`/api/calendar?${params}`), z.array(entrySchema));
}

export async function fetchEvent(id: string) {
  return readData(await getJson(`/api/calendar/events/${id}`), eventDetailSchema);
}

export async function saveEvent(event: CalendarEventWrite, id?: string) {
  const path = id ? `/api/calendar/events/${id}` : "/api/calendar/events";
  return readData(await sendJson(path, id ? "PUT" : "POST", event), eventDetailSchema);
}

export async function deleteEvent(id: string) {
  await sendJson(`/api/calendar/events/${id}`, "DELETE");
}

export async function fetchReminders() {
  return readData(await getJson("/api/reminders"), z.array(reminderSchema));
}

export async function fetchReminderLinks() {
  return readData(await getJson("/api/reminders/links"), linksSchema);
}

export async function saveReminder(reminder: ReminderWrite, id?: string) {
  const path = id ? `/api/reminders/${id}` : "/api/reminders";
  return readData(await sendJson(path, id ? "PUT" : "POST", reminder), reminderSchema);
}

export async function completeReminder(id: string) {
  return readData(await sendJson(`/api/reminders/${id}/complete`, "POST"), reminderSchema);
}

export async function reopenReminder(id: string) {
  return readData(await sendJson(`/api/reminders/${id}/reopen`, "POST"), reminderSchema);
}

export async function deleteReminder(id: string) {
  await sendJson(`/api/reminders/${id}`, "DELETE");
}

export async function fetchNotifications() {
  return readData(await getJson("/api/notifications"), z.array(notificationSchema));
}

export async function markNotificationRead(id: string) {
  await sendJson(`/api/notifications/${id}/read`, "POST");
}

export async function markAllNotificationsRead() {
  await sendJson("/api/notifications/read", "POST");
}

export async function markNotificationUnread(id: string) {
  await sendJson(`/api/notifications/${id}/unread`, "POST");
}

const preferenceSchema = z.object({
  type: z.string(),
  enabled: z.boolean(),
});

export type NotificationPreference = z.infer<typeof preferenceSchema>;

export async function fetchNotificationPreferences() {
  return readData(await getJson("/api/notifications/preferences"), z.array(preferenceSchema));
}

export async function saveNotificationPreference(type: string, enabled: boolean) {
  await sendJson("/api/notifications/preferences", "PUT", { type, enabled });
}
