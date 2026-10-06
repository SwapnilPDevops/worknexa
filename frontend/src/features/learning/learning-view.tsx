import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { format } from "date-fns";
import { useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Badge } from "../../components/design-system/badge.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Card, CardBody, CardHeader } from "../../components/design-system/card.tsx";
import { Input } from "../../components/design-system/field.tsx";
import { Progress } from "../../components/design-system/progress.tsx";
import { EmptyState, ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { Table } from "../../components/design-system/table.tsx";
import { ApiError } from "../../services/api-client.ts";
import { priorityTone } from "../work/catalog.ts";
import { deleteLearningTopic, fetchLearningDashboard, fetchLearningTopics, saveLearningTopic, type LearningTopic } from "./api.ts";
import { learningLabel, learningTone } from "./catalog.ts";
import { TopicResources } from "./topic-resources.tsx";
import { TopicDrawer } from "./topic-drawer.tsx";
import { blankTopic, draftFromTopic, toTopicWrite, type TopicDraft } from "./topic-draft.ts";

export function LearningView() {
  const { user } = useAuth();
  const manageable = can(user?.permissions, permissions.learningManage);
  const queryClient = useQueryClient();
  const [today] = useState(() => format(new Date(), "yyyy-MM-dd"));
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const [topicId, setTopicId] = useState<string | null>(null);
  const [draft, setDraft] = useState<TopicDraft>(blankTopic);
  const [message, setMessage] = useState<string | null>(null);
  const [params] = useSearchParams();
  const requestedTopic = params.get("topic");
  const [openedTopic, setOpenedTopic] = useState<string | null>(null);
  const dashboard = useQuery({ queryKey: ["learning-dashboard", today], queryFn: () => fetchLearningDashboard(today) });
  const topics = useQuery({ queryKey: ["learning-topics", query], queryFn: () => fetchLearningTopics(query) });

  const refresh = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["learning-dashboard"] }),
      queryClient.invalidateQueries({ queryKey: ["learning-topics"] }),
      queryClient.invalidateQueries({ queryKey: ["learning-roadmap"] }),
    ]);
  };

  const save = useMutation({
    mutationFn: () => saveLearningTopic(toTopicWrite(draft), topicId ?? undefined),
    onSuccess: async () => {
      setOpen(false);
      setMessage(null);
      await refresh();
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The topic could not be saved."),
  });

  const remove = useMutation({
    mutationFn: () => deleteLearningTopic(topicId ?? ""),
    onSuccess: async () => {
      setOpen(false);
      await refresh();
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The topic could not be removed."),
  });

  if (requestedTopic && openedTopic !== requestedTopic && topics.data) {
    const topic = topics.data.find((item) => item.id === requestedTopic);
    if (topic) {
      setOpenedTopic(requestedTopic);
      setTopicId(topic.id);
      setDraft(draftFromTopic(topic));
      setMessage(null);
      setOpen(true);
    }
  }

  if (!can(user?.permissions, permissions.learningRead)) {
    return <ErrorState title="Access denied" description="Your account does not include learning." />;
  }

  if (dashboard.isPending || topics.isPending) {
    return <LoadingState label="Loading learning" />;
  }

  if (dashboard.isError || !dashboard.data || topics.isError || !topics.data) {
    const error = dashboard.error ?? topics.error;
    return <ErrorState title="Learning is unavailable" description={error instanceof ApiError ? error.message : "Learning could not be loaded."} onRetry={() => { void dashboard.refetch(); void topics.refetch(); }} />;
  }

  function beginCreate(parentId = "") {
    setTopicId(null);
    setDraft({ ...blankTopic, parentTopicId: parentId, category: topics?.data?.find((topic) => topic.id === parentId)?.category ?? "" });
    setMessage(null);
    setOpen(true);
  }

  function beginEdit(topic: LearningTopic) {
    setTopicId(topic.id);
    setDraft(draftFromTopic(topic));
    setMessage(null);
    setOpen(true);
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    save.mutate();
  }

  const board = dashboard.data;
  return (
    <div className="mx-auto max-w-6xl space-y-5">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Learning</h1>
          <p className="mt-1 text-sm text-muted">Topics you are studying, and what comes next.</p>
        </div>
        <div className="flex gap-2">
          <Button asChild variant="secondary"><Link to="/roadmap">Roadmap</Link></Button>
          <Button asChild variant="secondary"><Link to="/learning-notes">Notes</Link></Button>
          {manageable ? <Button onClick={() => beginCreate()}>New topic</Button> : null}
        </div>
      </header>

      <div className="grid gap-3 lg:grid-cols-2">
        <Card>
          <CardHeader title="Progress" description="Average of your top-level topics." />
          <CardBody><Progress value={board.progress} label="Overall" /></CardBody>
        </Card>
        <Card>
          <CardHeader title="Current topics" description="Learning and practicing." />
          <CardBody>{board.currentTopics.length === 0 ? <p className="text-sm text-muted">Nothing is in progress.</p> : <TopicLines topics={board.currentTopics} onOpen={beginEdit} />}</CardBody>
        </Card>
        <Card>
          <CardHeader title="Learning goals" />
          <CardBody>
            {board.goals.length === 0 ? <p className="text-sm text-muted">Add a goal on a topic.</p> : (
              <ul className="space-y-2">
                {board.goals.map((goal) => (
                  <li key={goal.topicId}>
                    <p className="text-sm font-medium">{goal.name}</p>
                    <p className="text-sm text-muted">{goal.goal}</p>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>
        <Card>
          <CardHeader title="Recently studied" />
          <CardBody>{board.recentlyStudied.length === 0 ? <p className="text-sm text-muted">Study a topic to see it here.</p> : <TopicLines topics={board.recentlyStudied} onOpen={beginEdit} />}</CardBody>
        </Card>
        <Card className="lg:col-span-2">
          <CardHeader title="Upcoming targets" />
          <CardBody>
            {board.upcomingTargets.length === 0 ? <p className="text-sm text-muted">Set a target date to plan the next step.</p> : (
              <ul className="grid gap-2 sm:grid-cols-2">
                {board.upcomingTargets.map((target) => (
                  <li key={target.topicId} className="rounded-lg border border-line px-3 py-2">
                    <p className="text-sm font-medium">{target.name}</p>
                    <p className="text-xs text-muted">{target.targetDate} · {target.progress}%</p>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-semibold">Topics</h2>
        <Input aria-label="Search topics" className="max-w-xs" value={query} placeholder="Search topics" onChange={(event) => setQuery(event.target.value)} />
      </div>
      {topics.data.length === 0 ? (
        <EmptyState title="No topics" description={query ? "Nothing matches that search." : "Create a topic, or build the example roadmap."} action={<Button asChild variant="secondary"><Link to="/roadmap">Open roadmap</Link></Button>} />
      ) : (
        <Table
          caption="Learning topics"
          rows={topics.data}
          getRowKey={(row) => row.id}
          columns={[
            { key: "name", header: "Name", render: (row) => <button type="button" className="text-left font-medium" onClick={() => beginEdit(row)}>{row.parentName ? `${row.parentName} / ${row.name}` : row.name}</button> },
            { key: "category", header: "Category", render: (row) => row.category ?? "—" },
            { key: "status", header: "Status", render: (row) => <Badge tone={learningTone(row.status)}>{learningLabel(row.status)}</Badge> },
            { key: "progress", header: "Progress", render: (row) => `${row.progress}%` },
            { key: "priority", header: "Priority", render: (row) => <Badge tone={priorityTone(row.priority)}>{row.priority}</Badge> },
            { key: "dates", header: "Dates", render: (row) => `${row.startDate ?? "—"} → ${row.targetDate ?? "—"}` },
          ]}
        />
      )}

      <TopicDrawer open={open} title={topicId ? "Edit topic" : "New topic"} draft={draft} topics={topics.data} topicId={topicId} message={message} saving={save.isPending} onChange={setDraft} onClose={() => setOpen(false)} onSubmit={submit}>
        {topicId ? (
          <div className="mt-6 space-y-4 border-t border-line pt-4">
            <TopicResources topicId={topicId} manageable={manageable} />
            {manageable ? <Button type="button" variant="danger" size="sm" disabled={remove.isPending} onClick={() => remove.mutate()}>Delete topic</Button> : null}
          </div>
        ) : null}
      </TopicDrawer>
    </div>
  );
}

function TopicLines({ topics, onOpen }: { topics: LearningTopic[]; onOpen: (topic: LearningTopic) => void }) {
  return (
    <ul className="space-y-2">
      {topics.map((topic) => (
        <li key={topic.id}>
          <button type="button" className="w-full text-left" onClick={() => onOpen(topic)}>
            <span className="flex items-center justify-between gap-2 text-sm">
              <span className="font-medium">{topic.name}</span>
              <Badge tone={learningTone(topic.status)}>{learningLabel(topic.status)}</Badge>
            </span>
            <span className="text-xs text-muted">{topic.progress}%</span>
          </button>
        </li>
      ))}
    </ul>
  );
}
