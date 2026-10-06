import type { LearningTopic, LearningTopicWrite } from "./api.ts";

export type TopicDraft = {
  name: string;
  category: string;
  description: string;
  status: string;
  progress: string;
  startDate: string;
  targetDate: string;
  priority: string;
  parentTopicId: string;
  goal: string;
  childCount: number;
};

export const blankTopic: TopicDraft = {
  name: "",
  category: "",
  description: "",
  status: "NotStarted",
  progress: "0",
  startDate: "",
  targetDate: "",
  priority: "Medium",
  parentTopicId: "",
  goal: "",
  childCount: 0,
};

export function draftFromTopic(topic: LearningTopic): TopicDraft {
  return {
    name: topic.name,
    category: topic.category ?? "",
    description: topic.description ?? "",
    status: topic.status,
    progress: String(topic.progress),
    startDate: topic.startDate ?? "",
    targetDate: topic.targetDate ?? "",
    priority: topic.priority,
    parentTopicId: topic.parentId ?? "",
    goal: topic.goal ?? "",
    childCount: topic.childCount,
  };
}

export function toTopicWrite(draft: TopicDraft): LearningTopicWrite {
  const progress = Number(draft.progress);
  return {
    name: draft.name.trim(),
    category: draft.category.trim() || null,
    description: draft.description.trim() || null,
    status: draft.status,
    progress: Number.isFinite(progress) ? progress : 0,
    startDate: draft.startDate || null,
    targetDate: draft.targetDate || null,
    priority: draft.priority,
    parentTopicId: draft.parentTopicId || null,
    goal: draft.goal.trim() || null,
  };
}
