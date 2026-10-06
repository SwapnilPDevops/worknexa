import type { FormEvent, ReactNode } from "react";
import { Button } from "../../components/design-system/button.tsx";
import { Drawer } from "../../components/design-system/drawer.tsx";
import { Field, Input, Select, Textarea } from "../../components/design-system/field.tsx";
import type { LearningTopic } from "./api.ts";
import { learningPriorities, learningStatuses, learningLabel, suggestedProgress } from "./catalog.ts";
import type { TopicDraft } from "./topic-draft.ts";

function blockedParents(topics: LearningTopic[], topicId: string | null) {
  const blocked = new Set<string>();
  if (!topicId) {
    return blocked;
  }

  blocked.add(topicId);
  let grew = true;
  while (grew) {
    grew = false;
    for (const topic of topics) {
      if (topic.parentId && blocked.has(topic.parentId) && !blocked.has(topic.id)) {
        blocked.add(topic.id);
        grew = true;
      }
    }
  }

  return blocked;
}

export function TopicDrawer({
  open,
  title,
  draft,
  topics,
  topicId,
  message,
  saving,
  onChange,
  onClose,
  onSubmit,
  children,
}: {
  open: boolean;
  title: string;
  draft: TopicDraft;
  topics: LearningTopic[];
  topicId: string | null;
  message: string | null;
  saving: boolean;
  onChange: (draft: TopicDraft) => void;
  onClose: () => void;
  onSubmit: (event: FormEvent) => void;
  children?: ReactNode;
}) {
  const parents = topics.filter((topic) => !blockedParents(topics, topicId).has(topic.id));
  return (
    <Drawer open={open} title={title} onClose={onClose} className="max-w-lg">
      <form className="space-y-3" onSubmit={onSubmit}>
        {message ? <p className="text-sm text-danger">{message}</p> : null}
        <Field label="Name">{({ id }) => <Input id={id} value={draft.name} maxLength={300} required onChange={(event) => onChange({ ...draft, name: event.target.value })} />}</Field>
        <Field label="Category">{({ id }) => <Input id={id} value={draft.category} maxLength={120} onChange={(event) => onChange({ ...draft, category: event.target.value })} />}</Field>
        <Field label="Description">{({ id }) => <Textarea id={id} value={draft.description} rows={3} onChange={(event) => onChange({ ...draft, description: event.target.value })} />}</Field>
        <div className="grid gap-3 sm:grid-cols-2">
          <Field label="Status">{({ id }) => (
            <Select id={id} value={draft.status} onChange={(event) => {
              const status = event.target.value;
              onChange({ ...draft, status, progress: draft.childCount > 0 ? draft.progress : String(suggestedProgress(status)) });
            }}>
              {learningStatuses.map((status) => <option key={status} value={status}>{learningLabel(status)}</option>)}
            </Select>
          )}</Field>
          <Field label="Priority">{({ id }) => (
            <Select id={id} value={draft.priority} onChange={(event) => onChange({ ...draft, priority: event.target.value })}>
              {learningPriorities.map((priority) => <option key={priority} value={priority}>{priority}</option>)}
            </Select>
          )}</Field>
          <Field label="Progress" hint={draft.childCount > 0 ? "Progress follows the child topics." : "0 to 100"}>{({ id }) => (
            <Input id={id} type="number" min={0} max={100} value={draft.progress} disabled={draft.childCount > 0} onChange={(event) => onChange({ ...draft, progress: event.target.value })} />
          )}</Field>
          <Field label="Parent">{({ id }) => (
            <Select id={id} value={draft.parentTopicId} onChange={(event) => onChange({ ...draft, parentTopicId: event.target.value })}>
              <option value="">None</option>
              {parents.map((topic) => <option key={topic.id} value={topic.id}>{topic.name}</option>)}
            </Select>
          )}</Field>
          <Field label="Start date">{({ id }) => <Input id={id} type="date" value={draft.startDate} onChange={(event) => onChange({ ...draft, startDate: event.target.value })} />}</Field>
          <Field label="Target date">{({ id }) => <Input id={id} type="date" value={draft.targetDate} onChange={(event) => onChange({ ...draft, targetDate: event.target.value })} />}</Field>
        </div>
        <Field label="Learning goal">{({ id }) => <Textarea id={id} value={draft.goal} rows={2} maxLength={500} onChange={(event) => onChange({ ...draft, goal: event.target.value })} />}</Field>
        <div className="flex justify-end gap-2">
          <Button type="button" variant="ghost" onClick={onClose}>Cancel</Button>
          <Button type="submit" disabled={saving}>{saving ? "Saving…" : "Save topic"}</Button>
        </div>
      </form>
      {children}
    </Drawer>
  );
}
