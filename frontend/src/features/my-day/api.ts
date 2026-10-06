import { z } from "zod";
import { readData } from "../../auth/session.ts";
import { getJson } from "../../services/api-client.ts";

const taskItemSchema = z.object({
  id: z.string(),
  kind: z.string(),
  title: z.string(),
  priority: z.string(),
  dueAt: z.string().nullable(),
  projectName: z.string().nullable(),
});

const meetingSchema = z.object({
  id: z.string(),
  title: z.string(),
  startAt: z.string(),
  endAt: z.string(),
  projectName: z.string().nullable(),
  participants: z.array(z.string()),
});

const teamItemSchema = z.object({
  id: z.string(),
  title: z.string(),
  assigneeName: z.string().nullable(),
  projectName: z.string().nullable(),
});

const projectSchema = z.object({
  id: z.string(),
  name: z.string(),
  progress: z.number(),
  deadline: z.string().nullable(),
  atRisk: z.boolean(),
});

export const myDaySchema = z.object({
  summary: z.string(),
  counts: z.object({
    tasks: z.number(),
    completed: z.number(),
    overdue: z.number(),
    meetings: z.number(),
    reminders: z.number(),
    teamAttention: z.number(),
  }),
  timeline: z.array(z.object({
    kind: z.string(),
    id: z.string(),
    title: z.string(),
    at: z.string(),
    endAt: z.string().nullable(),
  })),
  tasks: z.object({
    overdue: z.array(taskItemSchema),
    highPriority: z.array(taskItemSchema),
    today: z.array(taskItemSchema),
    upcoming: z.array(taskItemSchema),
  }),
  meetings: z.object({
    today: z.array(meetingSchema),
    upcoming: meetingSchema.nullable(),
  }),
  team: z.object({
    blocked: z.array(teamItemSchema),
    waitingForReview: z.array(teamItemSchema),
    needsOwner: z.array(teamItemSchema),
    recentlyCompleted: z.array(teamItemSchema),
  }),
  projects: z.object({
    active: z.array(projectSchema),
    upcomingDeadlines: z.array(projectSchema),
    atRisk: z.array(projectSchema),
  }),
  learning: z.object({
    topicId: z.string(),
    title: z.string(),
    progress: z.number(),
    goal: z.string(),
  }).nullable(),
});

export type MyDay = z.infer<typeof myDaySchema>;

export async function fetchMyDay(date: string, offsetMinutes: number) {
  const payload = await getJson(`/api/dashboard/my-day?date=${date}&offsetMinutes=${offsetMinutes}`);
  return readData(payload, myDaySchema);
}
