import { z } from "zod";
import { readData } from "../../auth/session.ts";
import { getJson, sendJson } from "../../services/api-client.ts";

export const learningTopicSchema = z.object({
  id: z.string(),
  name: z.string(),
  category: z.string().nullable(),
  description: z.string().nullable(),
  status: z.string(),
  progress: z.number(),
  priority: z.string(),
  startDate: z.string().nullable(),
  targetDate: z.string().nullable(),
  parentId: z.string().nullable(),
  parentName: z.string().nullable(),
  goal: z.string().nullable(),
  childCount: z.number(),
});

export type LearningTopic = z.infer<typeof learningTopicSchema>;

const resourceSchema = z.object({
  id: z.string(),
  title: z.string(),
  url: z.string().nullable(),
  type: z.string(),
});

export type LearningResource = z.infer<typeof resourceSchema>;

const detailSchema = learningTopicSchema.extend({
  resources: z.array(resourceSchema),
});

export type LearningTopicDetail = z.infer<typeof detailSchema>;

const dashboardSchema = z.object({
  progress: z.number(),
  currentTopics: z.array(learningTopicSchema),
  goals: z.array(z.object({ topicId: z.string(), name: z.string(), goal: z.string() })),
  recentlyStudied: z.array(learningTopicSchema),
  upcomingTargets: z.array(z.object({ topicId: z.string(), name: z.string(), targetDate: z.string(), progress: z.number() })),
});

export type LearningDashboard = z.infer<typeof dashboardSchema>;

export type RoadmapNode = {
  id: string;
  name: string;
  category: string | null;
  status: string;
  progress: number;
  priority: string;
  children: RoadmapNode[];
};

const roadmapNodeSchema: z.ZodType<RoadmapNode> = z.lazy(() =>
  z.object({
    id: z.string(),
    name: z.string(),
    category: z.string().nullable(),
    status: z.string(),
    progress: z.number(),
    priority: z.string(),
    children: z.array(roadmapNodeSchema),
  }),
);

export type LearningTopicWrite = {
  name: string;
  category: string | null;
  description: string | null;
  status: string;
  progress: number | null;
  startDate: string | null;
  targetDate: string | null;
  priority: string;
  parentTopicId: string | null;
  goal: string | null;
};

export async function fetchLearningDashboard(date: string) {
  return readData(await getJson(`/api/learning/dashboard?date=${date}`), dashboardSchema);
}

export async function fetchLearningTopics(query: string) {
  const params = new URLSearchParams();
  if (query) {
    params.set("q", query);
  }

  return readData(await getJson(`/api/learning/topics?${params}`), z.array(learningTopicSchema));
}

export async function fetchLearningTopic(id: string) {
  return readData(await getJson(`/api/learning/topics/${id}`), detailSchema);
}

export async function saveLearningTopic(topic: LearningTopicWrite, id?: string) {
  const path = id ? `/api/learning/topics/${id}` : "/api/learning/topics";
  return readData(await sendJson(path, id ? "PUT" : "POST", topic), detailSchema);
}

export async function deleteLearningTopic(id: string) {
  await sendJson(`/api/learning/topics/${id}`, "DELETE");
}

export async function addLearningResource(topicId: string, resource: { title: string; url: string | null; type: string }) {
  return readData(await sendJson(`/api/learning/topics/${topicId}/resources`, "POST", resource), detailSchema);
}

export async function deleteLearningResource(topicId: string, resourceId: string) {
  await sendJson(`/api/learning/topics/${topicId}/resources/${resourceId}`, "DELETE");
}

export async function fetchRoadmap() {
  return readData(await getJson("/api/learning/roadmap"), z.array(roadmapNodeSchema));
}

export async function createExampleRoadmap() {
  return readData(await sendJson("/api/learning/roadmap/example", "POST"), z.array(roadmapNodeSchema));
}
