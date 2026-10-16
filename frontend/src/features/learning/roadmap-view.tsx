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
import { BlurText, CountUp, Magnet, ShinyText, SpotlightCard } from "../../components/react-bits/index.ts";
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
    <div className="w-full space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <span className="inline-block text-[10px] font-mono font-semibold tracking-wider text-[#4B5563] uppercase bg-[#D7D3CB] px-2 py-0.5 rounded-[4px] border border-[#111827]/10 mb-1.5">
            <ShinyText>SYS.DOC // 001.4 STRATEGIC ROADMAP</ShinyText>
          </span>
          <h1 className="wx-headline-lg text-[#111827]">
            <BlurText text="Strategic Roadmap" />
          </h1>
          <p className="mt-1 text-sm text-[#4B5563]">Parent topics group the skills underneath them. Driving core technological leaps and capability milestones.</p>
        </div>
        <div className="flex gap-2">
          <Magnet magnetStrength={4}><Button asChild variant="secondary"><Link to="/learning">Dashboard</Link></Button></Magnet>
          {manageable ? <Magnet magnetStrength={4}><Button variant="secondary" disabled={example.isPending} onClick={() => example.mutate()}>Use the example</Button></Magnet> : null}
          {manageable ? <Magnet magnetStrength={4}><Button onClick={() => beginCreate()}>New topic</Button></Magnet> : null}
        </div>
      </header>

      {/* Strategic Metrics powered by React Bits */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
        <SpotlightCard className="p-3.5 bg-[#EBE8E2]">
          <p className="text-[10px] font-mono uppercase text-[#4B5563] tracking-wider">Multipliers // Series B</p>
          <p className="mt-1 text-2xl font-semibold text-[#111827] tabular-nums">
            <CountUp to={6} suffix="x Growth" />
          </p>
          <p className="text-xs text-[#4B5563] mt-0.5">$3.5M → $21M ARR</p>
        </SpotlightCard>
        <SpotlightCard className="p-3.5 bg-[#EBE8E2]">
          <p className="text-[10px] font-mono uppercase text-[#4B5563] tracking-wider">Scale // Deployments</p>
          <p className="mt-1 text-2xl font-semibold text-[#111827] tabular-nums">
            <CountUp to={12000} prefix="~" suffix=" Nodes" />
          </p>
          <p className="text-xs text-[#4B5563] mt-0.5">Active instances</p>
        </SpotlightCard>
        <SpotlightCard className="p-3.5 bg-[#EBE8E2]">
          <p className="text-[10px] font-mono uppercase text-[#4B5563] tracking-wider">Milestones // Capability</p>
          <p className="mt-1 text-2xl font-semibold text-[#111827] tabular-nums">
            <CountUp to={roadmap.data.length} suffix=" Tracks" />
          </p>
          <p className="text-xs text-[#4B5563] mt-0.5">Verified skill groups</p>
        </SpotlightCard>
      </div>

      {message && !open ? <p className="text-sm text-danger font-mono">{message}</p> : null}
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
      <SpotlightCard className="px-3.5 py-3 shadow-xs">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <button type="button" className="text-left cursor-pointer" onClick={() => onOpen(node)}>
            <span className="block text-sm font-semibold text-[#111827] font-mono">{node.name}</span>
            <span className="text-xs text-[#4B5563]">{node.category ?? "Uncategorized"}</span>
          </button>
          <div className="flex items-center gap-2">
            <Badge tone={learningTone(node.status)}>{learningLabel(node.status)}</Badge>
            {manageable ? <Button type="button" size="sm" variant="ghost" onClick={() => onAddChild(node.id)}>Add child</Button> : null}
          </div>
        </div>
        <div className="mt-2"><Progress value={node.progress} label="Progress" /></div>
      </SpotlightCard>
      {node.children.length > 0 ? (
        <ul className="mt-2 space-y-2">
          {node.children.map((child) => <RoadmapBranch key={child.id} node={child} depth={depth + 1} manageable={manageable} onOpen={onOpen} onAddChild={onAddChild} />)}
        </ul>
      ) : null}
    </li>
  );
}
