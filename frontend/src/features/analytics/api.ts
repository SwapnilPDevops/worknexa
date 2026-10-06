import { z } from "zod";
import { readData } from "../../auth/session.ts";
import { getJson } from "../../services/api-client.ts";

const hours = z.number().nullable();

const personalSchema = z.object({
  tasksCompleted: z.number(),
  tasksOverdue: z.number(),
  dueInPeriod: z.number(),
  completionRate: z.number(),
  meetingHours: hours,
  focusHours: hours,
  learningHours: hours,
  weeks: z.array(z.object({
    weekStart: z.string(),
    completed: z.number(),
    due: z.number(),
    meetingHours: hours,
    focusHours: hours,
    learningHours: hours,
  })),
});

const teamSchema = z.object({
  teamId: z.string().nullable(),
  teams: z.array(z.object({ id: z.string(), name: z.string() })),
  assigned: z.number(),
  completed: z.number(),
  overdue: z.number(),
  blocked: z.number(),
  members: z.array(z.object({
    userId: z.string(),
    name: z.string(),
    assigned: z.number(),
    inProgress: z.number(),
    completed: z.number(),
    overdue: z.number(),
    blocked: z.number(),
  })),
});

const taskFlagSchema = z.object({
  taskId: z.string(),
  title: z.string(),
  projectId: z.string(),
  projectName: z.string(),
  assigneeName: z.string().nullable(),
  dueAt: z.string().nullable(),
});

const projectsSchema = z.object({
  activeProjects: z.number(),
  completedTasks: z.number(),
  countedTasks: z.number(),
  completionRate: z.number(),
  blockedTasks: z.number(),
  projects: z.array(z.object({
    id: z.string(),
    name: z.string(),
    status: z.string(),
    progress: z.number(),
    elapsed: z.number().nullable(),
    openTasks: z.number(),
    blockedTasks: z.number(),
    targetDate: z.string().nullable(),
  })),
  blocked: z.array(taskFlagSchema),
  deadlines: z.array(taskFlagSchema),
});

const learningSchema = z.object({
  topicsCompleted: z.number(),
  topicsTotal: z.number(),
  progress: z.number(),
  activeDays: z.number(),
  periodDays: z.number(),
  weeks: z.array(z.object({ weekStart: z.string(), activeDays: z.number() })),
});

const analyticsSchema = z.object({
  from: z.string(),
  to: z.string(),
  weeks: z.number(),
  personal: personalSchema.nullable(),
  team: teamSchema.nullable(),
  projects: projectsSchema.nullable(),
  learning: learningSchema.nullable(),
});

export type Analytics = z.infer<typeof analyticsSchema>;
export type PersonalAnalytics = z.infer<typeof personalSchema>;
export type TeamAnalytics = z.infer<typeof teamSchema>;
export type ProjectAnalytics = z.infer<typeof projectsSchema>;
export type LearningAnalytics = z.infer<typeof learningSchema>;
export type TaskFlag = z.infer<typeof taskFlagSchema>;

export async function fetchAnalytics(weeks: number, offsetMinutes: number, teamId: string | null) {
  const params = new URLSearchParams({ weeks: String(weeks), offsetMinutes: String(offsetMinutes) });
  if (teamId) params.set("teamId", teamId);
  return readData(await getJson(`/api/dashboard/analytics?${params}`), analyticsSchema);
}
