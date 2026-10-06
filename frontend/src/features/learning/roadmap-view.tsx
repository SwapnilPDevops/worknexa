import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Badge } from "../../components/design-system/badge.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { EmptyState, ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { Progress } from "../../components/design-system/progress.tsx";
import { ApiError } from "../../services/api-client.ts";
import { createExampleRoadmap, fetchLearningTopics, fetchRoadmap, saveLearningTopic, type RoadmapNode } from "./api.ts";
import { learningLabel, learningTone } from "./catalog.ts";
import { TopicDrawer } from "./topic-drawer.tsx";
import { blankTopic, draftFromTopic, toTopicWrite, type TopicDraft } from "./topic-draft.ts";

export function RoadmapView() {
  const { user } = useAuth();
  const manageable = can(user?.permissions, permissions.learningManage);
  const queryClient = useQueryClient();
  const roadmap = useQuery({ queryKey: ["learning-roadmap"], queryFn: fetchRoadmap });
  const topics = useQuery({ queryKey: ["learning-topics", ""], queryFn: () => fetchLearningTopics("") });
  const [open, setOpen] = useState(false);
  const [topicId, setTopicId] = useState<string | null>(null);
  const [draft, setDraft] = useState<TopicDraft>(blankTopic);
  const [message, setMessage] = useState<string | null>(null);

  const refresh = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["learning-roadmap"] }),
      queryClient.invalidateQueries({ queryKey: ["learning-topics"] }),
      queryClient.invalidateQueries({ queryKey: ["learning-dashboard"] }),
    ]);
  };

  const example = useMutation({
    mutationFn: createExampleRoadmap,
    onSuccess: () => refresh(),
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The example roadmap could not be created."),
  });

  const save = useMutation({
    mutationFn: () => saveLearningTopic(toTopicWrite(draft), topicId ?? undefined),
    onSuccess: async () => {
      setOpen(false);
      setMessage(null);
      await refresh();
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The topic could not be saved."),
  });

  if (!can(user?.permissions, permissions.learningRead)) {
    return <ErrorState title="Access denied" description="Your account does not include learning." />;
  }

  if (roadmap.isPending || topics.isPending) {
    return <LoadingState label="Loading roadmap" />;
  }

  if (roadmap.isError || !roadmap.data || topics.isError || !topics.data) {
    const error = roadmap.error ?? topics.error;
    return <ErrorState title="Roadmap is unavailable" description={error instanceof ApiError ? error.message : "The roadmap could not be loaded."} onRetry={() => { void roadmap.refetch(); void topics.refetch(); }} />;
  }

  function beginCreate(parentId = "") {
    const parent = topics.data?.find((topic) => topic.id === parentId);
    setTopicId(null);
    setDraft({ ...blankTopic, parentTopicId: parentId, category: parent?.category ?? parent?.name ?? "" });
    setMessage(null);
    setOpen(true);
  }

  function beginEdit(node: RoadmapNode) {
    const topic = topics.data?.find((item) => item.id === node.id);
    if (!topic) {
      return;
    }

    setTopicId(topic.id);
    setDraft(draftFromTopic(topic));
    setMessage(null);
    setOpen(true);
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    save.mutate();
  }

  return (
    <div className="mx-auto max-w-3xl space-y-5">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Roadmap</h1>
          <p className="mt-1 text-sm text-muted">Parent topics group the skills underneath them.</p>
        </div>
        <div className="flex gap-2">
          <Button asChild variant="secondary"><Link to="/learning">Dashboard</Link></Button>
          {manageable ? <Button variant="secondary" disabled={example.isPending} onClick={() => example.mutate()}>Use the example</Button> : null}
          {manageable ? <Button onClick={() => beginCreate()}>New topic</Button> : null}
        </div>
      </header>
      {message && !open ? <p className="text-sm text-danger">{message}</p> : null}
      {roadmap.data.length === 0 ? (
        <EmptyState title="No roadmap yet" description="Add a parent topic, or start from the Frontend, Backend, Cloud, and Architecture example." action={manageable ? <Button disabled={example.isPending} onClick={() => example.mutate()}>Use the example</Button> : undefined} />
      ) : (
        <ul className="space-y-3">
          {roadmap.data.map((node) => <RoadmapBranch key={node.id} node={node} depth={0} manageable={manageable} onOpen={beginEdit} onAddChild={beginCreate} />)}
        </ul>
      )}
      <TopicDrawer open={open} title={topicId ? "Edit topic" : "New topic"} draft={draft} topics={topics.data} topicId={topicId} message={message} saving={save.isPending} onChange={setDraft} onClose={() => setOpen(false)} onSubmit={submit} />
    </div>
  );
}

function RoadmapBranch({
  node,
  depth,
  manageable,
  onOpen,
  onAddChild,
}: {
  node: RoadmapNode;
  depth: number;
  manageable: boolean;
  onOpen: (node: RoadmapNode) => void;
  onAddChild: (parentId: string) => void;
}) {
  return (
    <li style={{ marginLeft: depth === 0 ? 0 : 16 }}>
      <div className="rounded-xl border border-line bg-surface px-3 py-3">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <button type="button" className="text-left" onClick={() => onOpen(node)}>
            <span className="block text-sm font-medium">{node.name}</span>
            <span className="text-xs text-muted">{node.category ?? "Uncategorized"}</span>
          </button>
          <div className="flex items-center gap-2">
            <Badge tone={learningTone(node.status)}>{learningLabel(node.status)}</Badge>
            {manageable ? <Button type="button" size="sm" variant="ghost" onClick={() => onAddChild(node.id)}>Add child</Button> : null}
          </div>
        </div>
        <div className="mt-2"><Progress value={node.progress} label="Progress" /></div>
      </div>
      {node.children.length > 0 ? (
        <ul className="mt-2 space-y-2">
          {node.children.map((child) => <RoadmapBranch key={child.id} node={child} depth={depth + 1} manageable={manageable} onOpen={onOpen} onAddChild={onAddChild} />)}
        </ul>
      ) : null}
    </li>
  );
}
