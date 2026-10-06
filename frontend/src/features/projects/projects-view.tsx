import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { format, parseISO } from "date-fns";
import { useEffect, useMemo, useState, type DragEvent, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Badge } from "../../components/design-system/badge.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Card, CardBody, CardHeader } from "../../components/design-system/card.tsx";
import { Drawer } from "../../components/design-system/drawer.tsx";
import { Field, Input, Select, Textarea } from "../../components/design-system/field.tsx";
import { Progress } from "../../components/design-system/progress.tsx";
import { EmptyState, ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { Tabs } from "../../components/design-system/tabs.tsx";
import { ApiError } from "../../services/api-client.ts";
import { cn } from "../../utils/cn.ts";
import { safeHref } from "../../utils/safe-url.ts";
import { addTaskDependency, fetchTask, fetchTasks, removeTaskDependency, saveTask, setTaskStatus, type TaskListItem, type TaskWrite } from "../work/api.ts";
import { labelFor as taskLabel, priorityTone, taskStatuses } from "../work/catalog.ts";
import {
  addProjectMember,
  deleteProject,
  fetchProject,
  fetchProjectActivity,
  fetchProjectMeetings,
  fetchProjectNotes,
  fetchProjectOptions,
  fetchProjects,
  removeProjectMember,
  saveProject,
  type ProjectDetail,
  type ProjectWrite,
} from "./api.ts";
import { labelFor, memberRoles, projectPriorities, projectStatuses, projectTabs, statusTone, type ProjectTab } from "./catalog.ts";

type Draft = {
  name: string;
  code: string;
  description: string;
  ownerId: string;
  teamId: string;
  status: string;
  priority: string;
  startDate: string;
  targetDate: string;
  technology: string;
  repositoryUrl: string;
  documentationUrl: string;
};

const blankDraft: Draft = {
  name: "",
  code: "",
  description: "",
  ownerId: "",
  teamId: "",
  status: "Planning",
  priority: "Medium",
  startDate: "",
  targetDate: "",
  technology: "",
  repositoryUrl: "",
  documentationUrl: "",
};

function draftFrom(project: ProjectDetail): Draft {
  return {
    name: project.name,
    code: project.code,
    description: project.description ?? "",
    ownerId: project.ownerId,
    teamId: project.teamId ?? "",
    status: project.status,
    priority: project.priority,
    startDate: project.startDate ?? "",
    targetDate: project.targetDate ?? "",
    technology: project.technology ?? "",
    repositoryUrl: project.repositoryUrl ?? "",
    documentationUrl: project.documentationUrl ?? "",
  };
}

function toWrite(draft: Draft): ProjectWrite {
  return {
    name: draft.name.trim(),
    code: draft.code.trim(),
    description: draft.description.trim() || null,
    ownerId: draft.ownerId || null,
    teamId: draft.teamId || null,
    status: draft.status,
    priority: draft.priority,
    startDate: draft.startDate || null,
    targetDate: draft.targetDate || null,
    technology: draft.technology.trim() || null,
    repositoryUrl: draft.repositoryUrl.trim() || null,
    documentationUrl: draft.documentationUrl.trim() || null,
  };
}

export function ProjectsView() {
  const { user } = useAuth();
  const manageable = can(user?.permissions, permissions.projectsManage);
  const tasksManage = can(user?.permissions, permissions.tasksManage);
  const [params, setParams] = useSearchParams();
  const selectedId = params.get("project");
  const tab = (projectTabs.some((item) => item.id === params.get("tab")) ? params.get("tab") : "overview") as ProjectTab;
  const [search, setSearch] = useState("");
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState<Draft>(blankDraft);
  const [message, setMessage] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const projects = useQuery({ queryKey: ["projects", search], queryFn: () => fetchProjects(search) });
  const options = useQuery({ queryKey: ["project-options"], queryFn: fetchProjectOptions, enabled: manageable });
  const detail = useQuery({ queryKey: ["project", selectedId], queryFn: () => fetchProject(selectedId!), enabled: Boolean(selectedId) });

  useEffect(() => {
    document.title = "Projects · Worknexa";
  }, []);

  useEffect(() => {
    if (editing && detail.data && selectedId) {
      setDraft(draftFrom(detail.data));
    }
  }, [detail.data, editing, selectedId]);

  const save = useMutation({
    mutationFn: () => saveProject(toWrite(draft), editing && selectedId ? selectedId : undefined),
    onSuccess: async (saved) => {
      setMessage(null);
      setEditing(false);
      setParams({ project: saved.id, tab });
      await queryClient.invalidateQueries({ queryKey: ["projects"] });
      await queryClient.invalidateQueries({ queryKey: ["project", saved.id] });
      await queryClient.invalidateQueries({ queryKey: ["work-options"] });
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "Could not save the project."),
  });

  const remove = useMutation({
    mutationFn: () => deleteProject(selectedId!),
    onSuccess: async () => {
      setParams({});
      await queryClient.invalidateQueries({ queryKey: ["projects"] });
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "Could not delete the project."),
  });

  function openCreate() {
    setDraft({ ...blankDraft, ownerId: user?.id ?? "" });
    setEditing(false);
    setMessage(null);
    setParams((current) => {
      const next = new URLSearchParams(current);
      next.set("compose", "1");
      return next;
    });
  }

  const composing = params.get("compose") === "1" || editing;

  return (
    <div className="grid gap-6 lg:grid-cols-[18rem_minmax(0,1fr)]">
      <aside className="space-y-3">
        <div className="flex items-center justify-between gap-2">
          <h1 className="text-lg font-semibold text-ink">Projects</h1>
          {manageable ? <Button size="sm" onClick={openCreate}>New</Button> : null}
        </div>
        <Input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search name, code, or technology" aria-label="Search projects" />
        {projects.isPending ? <LoadingState label="Loading projects" /> : null}
        {projects.isError ? <ErrorState title="Projects are unavailable" description={projects.error instanceof ApiError ? projects.error.message : "Try again."} /> : null}
        {projects.data?.length === 0 ? <p className="text-sm text-muted">No projects yet.</p> : null}
        <ul className="space-y-1">
          {projects.data?.map((project) => (
            <li key={project.id}>
              <button
                type="button"
                className={cn("w-full rounded-lg px-3 py-2 text-left", project.id === selectedId ? "bg-surface-muted" : "hover:bg-surface-muted")}
                onClick={() => setParams({ project: project.id, tab })}
              >
                <span className="flex items-center justify-between gap-2">
                  <span className="truncate text-sm font-medium text-ink">{project.name}</span>
                  <Badge tone={statusTone(project.status)}>{labelFor(project.status)}</Badge>
                </span>
                <span className="mt-1 block text-xs text-muted">{project.code} · {project.progress}%</span>
              </button>
            </li>
          ))}
        </ul>
      </aside>
      <section>
        {!selectedId ? <EmptyState title="Choose a project" description="Open a project to see its work, people, and progress." /> : null}
        {selectedId && detail.isPending ? <LoadingState label="Loading project" /> : null}
        {selectedId && detail.isError ? <ErrorState title="This project is unavailable" description={detail.error instanceof ApiError ? detail.error.message : "Try again."} /> : null}
        {detail.data ? (
          <div className="space-y-4">
            <header className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <p className="text-xs tracking-wide text-muted uppercase">{detail.data.code}</p>
                <h2 className="text-2xl font-semibold text-ink">{detail.data.name}</h2>
                <p className="mt-1 text-sm text-muted">{detail.data.ownerName}{detail.data.teamName ? ` · ${detail.data.teamName}` : ""}</p>
              </div>
              <div className="flex gap-2">
                {manageable ? <Button variant="secondary" onClick={() => { setEditing(true); setDraft(draftFrom(detail.data)); setMessage(null); }}>Edit</Button> : null}
                {manageable ? <Button variant="danger" onClick={() => remove.mutate()} disabled={remove.isPending}>Delete</Button> : null}
              </div>
            </header>
            <div className="overflow-x-auto">
              <Tabs tabs={[...projectTabs]} value={tab} label="Project sections" onChange={(id) => setParams({ project: detail.data.id, tab: id })} />
            </div>
            {tab === "overview" ? <Overview project={detail.data} /> : null}
            {tab === "tasks" || tab === "board" || tab === "timeline" ? (
              <ProjectTasks projectId={detail.data.id} teamId={detail.data.teamId} mode={tab} canManage={tasksManage} />
            ) : null}
            {tab === "team" ? <Team project={detail.data} people={options.data?.people ?? []} canManage={manageable} /> : null}
            {tab === "meetings" ? <Meetings projectId={detail.data.id} /> : null}
            {tab === "notes" ? <Notes projectId={detail.data.id} /> : null}
            {tab === "activity" ? <Activity projectId={detail.data.id} /> : null}
          </div>
        ) : null}
      </section>
      <Drawer open={composing} title={editing ? "Edit project" : "New project"} onClose={() => { setEditing(false); setParams((current) => { const next = new URLSearchParams(current); next.delete("compose"); return next; }); }}>
        <form className="space-y-3" onSubmit={(event: FormEvent) => { event.preventDefault(); save.mutate(); }}>
          <Field label="Name">{({ id }) => <Input id={id} value={draft.name} onChange={(event) => setDraft({ ...draft, name: event.target.value })} required />}</Field>
          <Field label="Code">{({ id }) => <Input id={id} value={draft.code} onChange={(event) => setDraft({ ...draft, code: event.target.value })} required maxLength={32} />}</Field>
          <Field label="Description">{({ id }) => <Textarea id={id} value={draft.description} onChange={(event) => setDraft({ ...draft, description: event.target.value })} />}</Field>
          <Field label="Owner">{({ id }) => (
            <Select id={id} value={draft.ownerId} onChange={(event) => setDraft({ ...draft, ownerId: event.target.value })}>
              <option value="">Current user</option>
              {options.data?.people.map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}
            </Select>
          )}</Field>
          <Field label="Team">{({ id }) => (
            <Select id={id} value={draft.teamId} onChange={(event) => setDraft({ ...draft, teamId: event.target.value })}>
              <option value="">No team</option>
              {options.data?.teams.map((team) => <option key={team.id} value={team.id}>{team.name}</option>)}
            </Select>
          )}</Field>
          <Field label="Status">{({ id }) => (
            <Select id={id} value={draft.status} onChange={(event) => setDraft({ ...draft, status: event.target.value })}>
              {projectStatuses.map((status) => <option key={status} value={status}>{labelFor(status)}</option>)}
            </Select>
          )}</Field>
          <Field label="Priority">{({ id }) => (
            <Select id={id} value={draft.priority} onChange={(event) => setDraft({ ...draft, priority: event.target.value })}>
              {projectPriorities.map((priority) => <option key={priority} value={priority}>{priority}</option>)}
            </Select>
          )}</Field>
          <Field label="Start date">{({ id }) => <Input id={id} type="date" value={draft.startDate} onChange={(event) => setDraft({ ...draft, startDate: event.target.value })} />}</Field>
          <Field label="Target date">{({ id }) => <Input id={id} type="date" value={draft.targetDate} onChange={(event) => setDraft({ ...draft, targetDate: event.target.value })} />}</Field>
          <Field label="Technology">{({ id }) => <Input id={id} value={draft.technology} onChange={(event) => setDraft({ ...draft, technology: event.target.value })} />}</Field>
          <Field label="Repository URL">{({ id }) => <Input id={id} value={draft.repositoryUrl} onChange={(event) => setDraft({ ...draft, repositoryUrl: event.target.value })} />}</Field>
          <Field label="Documentation URL">{({ id }) => <Input id={id} value={draft.documentationUrl} onChange={(event) => setDraft({ ...draft, documentationUrl: event.target.value })} />}</Field>
          {message ? <p className="text-sm text-danger">{message}</p> : null}
          <Button type="submit" disabled={save.isPending}>Save</Button>
        </form>
      </Drawer>
    </div>
  );
}

function Overview({ project }: { project: ProjectDetail }) {
  const dashboard = project.dashboard;
  return (
    <div className="space-y-4">
      <Progress value={dashboard.progress} label="Completion" />
      <div className="grid gap-3 sm:grid-cols-3">
        <Metric label="Open tasks" value={dashboard.openTasks} />
        <Metric label="Completed tasks" value={dashboard.completedTasks} />
        <Metric label="Blocked tasks" value={dashboard.blockedTasks} />
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader title="Upcoming deadlines" description="Open work due in the next two weeks." />
          <CardBody>
            {dashboard.upcomingDeadlines.length === 0 ? <p className="text-sm text-muted">No upcoming deadlines.</p> : (
              <ul className="space-y-2 text-sm">
                {dashboard.upcomingDeadlines.map((item) => (
                  <li key={item.taskId} className="flex justify-between gap-3">
                    <span>{item.title}</span>
                    <span className="text-muted">{format(parseISO(item.dueAt), "MMM d")}{item.assigneeName ? ` · ${item.assigneeName}` : ""}</span>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>
        <Card>
          <CardHeader title="Team workload" description="Open tasks by assignee." />
          <CardBody>
            {dashboard.teamWorkload.length === 0 ? <p className="text-sm text-muted">No open work.</p> : (
              <ul className="space-y-2 text-sm">
                {dashboard.teamWorkload.map((row) => (
                  <li key={row.assigneeId ?? "none"} className="flex justify-between gap-3">
                    <span>{row.name}</span>
                    <span className="text-muted">{row.openTasks} open{row.blockedTasks ? ` · ${row.blockedTasks} blocked` : ""}</span>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>
      </div>
      <Card>
        <CardHeader title="About" />
        <CardBody className="space-y-2 text-sm text-ink">
          <p>{project.description || "No description yet."}</p>
          <p className="text-muted">{labelFor(project.status)} · {project.priority}{project.technology ? ` · ${project.technology}` : ""}</p>
          <p className="text-muted">{project.startDate ? `Starts ${project.startDate}` : "No start date"}{project.targetDate ? ` · Target ${project.targetDate}` : ""}</p>
          {safeHref(project.repositoryUrl) ? <a className="block text-accent" href={safeHref(project.repositoryUrl)} target="_blank" rel="noreferrer">Repository</a> : null}
          {safeHref(project.documentationUrl) ? <a className="block text-accent" href={safeHref(project.documentationUrl)} target="_blank" rel="noreferrer">Documentation</a> : null}
        </CardBody>
      </Card>
    </div>
  );
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <Card>
      <CardBody>
        <p className="text-xs text-muted">{label}</p>
        <p className="mt-1 text-2xl font-semibold text-ink">{value}</p>
      </CardBody>
    </Card>
  );
}

function ProjectTasks({ projectId, teamId, mode, canManage }: { projectId: string; teamId: string | null; mode: "tasks" | "board" | "timeline"; canManage: boolean }) {
  const queryClient = useQueryClient();
  const tasks = useQuery({
    queryKey: ["tasks", "project", projectId],
    queryFn: () => fetchTasks(`?view=list&projectId=${projectId}&date=${format(new Date(), "yyyy-MM-dd")}&offsetMinutes=${-new Date().getTimezoneOffset()}`),
  });
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [title, setTitle] = useState("");
  const [dependencyId, setDependencyId] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const detail = useQuery({ queryKey: ["task", selectedId], queryFn: () => fetchTask(selectedId!), enabled: Boolean(selectedId) && mode === "tasks" });

  const create = useMutation({
    mutationFn: () => saveTask(taskWrite(title, projectId, teamId)),
    onSuccess: async () => {
      setTitle("");
      setMessage(null);
      await queryClient.invalidateQueries({ queryKey: ["tasks", "project", projectId] });
      await queryClient.invalidateQueries({ queryKey: ["project", projectId] });
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "Could not create the task."),
  });
  const move = useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) => setTaskStatus(id, status),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["tasks", "project", projectId] });
      await queryClient.invalidateQueries({ queryKey: ["project", projectId] });
    },
  });
  const link = useMutation({
    mutationFn: () => addTaskDependency(selectedId!, dependencyId),
    onSuccess: async () => {
      setDependencyId("");
      await queryClient.invalidateQueries({ queryKey: ["task", selectedId] });
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "Could not add the dependency."),
  });
  const unlink = useMutation({
    mutationFn: (dependencyIdToRemove: string) => removeTaskDependency(selectedId!, dependencyIdToRemove),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ["task", selectedId] }),
  });

  if (tasks.isPending) return <LoadingState label="Loading tasks" />;
  if (tasks.isError) return <ErrorState title="Tasks are unavailable" description={tasks.error instanceof ApiError ? tasks.error.message : "Try again."} />;
  const rows = tasks.data ?? [];

  return (
    <div className="space-y-4">
      {canManage ? (
        <form className="flex gap-2" onSubmit={(event) => { event.preventDefault(); if (title.trim()) create.mutate(); }}>
          <Input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="New task title" aria-label="New task title" />
          <Button type="submit" disabled={create.isPending}>Add task</Button>
        </form>
      ) : null}
      {message ? <p className="text-sm text-danger">{message}</p> : null}
      {rows.length === 0 ? <EmptyState title="No tasks" description="Tasks added here use the same task list as the rest of the workspace." /> : null}
      {mode === "tasks" && rows.length > 0 ? (
        <ul className="divide-y divide-line rounded-xl border border-line">
          {rows.map((task) => (
            <li key={task.id}>
              <button type="button" className="flex w-full items-center justify-between gap-3 px-3 py-2 text-left text-sm" onClick={() => setSelectedId(task.id)}>
                <span>{task.title}</span>
                <span className="text-muted">{taskLabel(task.status)}{task.assigneeName ? ` · ${task.assigneeName}` : ""}</span>
              </button>
            </li>
          ))}
        </ul>
      ) : null}
      {mode === "board" ? <Board rows={rows} canMove={canManage} onOpen={setSelectedId} onMove={(id, status) => move.mutate({ id, status })} /> : null}
      {mode === "timeline" ? <Timeline rows={rows} /> : null}
      {mode === "tasks" && detail.data ? (
        <Card>
          <CardHeader title={detail.data.title} description="Dependencies stay on the task." />
          <CardBody className="space-y-3 text-sm">
            {detail.data.dependencies.length === 0 ? <p className="text-muted">No dependencies.</p> : (
              <ul className="space-y-1">
                {detail.data.dependencies.map((item) => (
                  <li key={item.id} className="flex items-center justify-between gap-2">
                    <span>Depends on {item.title}</span>
                    {canManage ? <Button size="sm" variant="ghost" onClick={() => unlink.mutate(item.id)}>Remove</Button> : null}
                  </li>
                ))}
              </ul>
            )}
            {canManage ? (
              <div className="flex gap-2">
                <Select value={dependencyId} onChange={(event) => setDependencyId(event.target.value)} aria-label="Depends on">
                  <option value="">Depends on</option>
                  {rows.filter((task) => task.id !== selectedId).map((task) => <option key={task.id} value={task.id}>{task.title}</option>)}
                </Select>
                <Button size="sm" variant="secondary" disabled={!dependencyId || link.isPending} onClick={() => link.mutate()}>Add</Button>
              </div>
            ) : null}
            <Link className="text-accent" to={`/tasks?task=${detail.data.id}`}>Open in Tasks</Link>
          </CardBody>
        </Card>
      ) : null}
    </div>
  );
}

function taskWrite(title: string, projectId: string, teamId: string | null): TaskWrite {
  return {
    title: title.trim(),
    description: null,
    status: "Todo",
    priority: "Medium",
    startAt: null,
    dueAt: null,
    assigneeId: null,
    projectId,
    parentTaskId: null,
    teamId,
    estimatedHours: null,
    actualHours: null,
    tags: [],
  };
}

function Board({ rows, canMove, onOpen, onMove }: { rows: TaskListItem[]; canMove: boolean; onOpen: (id: string) => void; onMove: (id: string, status: string) => void }) {
  return (
    <div className="flex gap-3 overflow-x-auto pb-2">
      {taskStatuses.map((status) => (
        <div
          key={status}
          className="w-56 shrink-0 rounded-xl border border-line bg-surface-muted p-2"
          onDragOver={(event) => event.preventDefault()}
          onDrop={(event: DragEvent) => {
            const id = event.dataTransfer.getData("text/plain");
            if (id && canMove) onMove(id, status);
          }}
        >
          <p className="px-1 pb-2 text-xs font-medium text-muted">{taskLabel(status)}</p>
          <div className="space-y-2">
            {rows.filter((task) => task.status === status).map((task) => (
              <button
                key={task.id}
                type="button"
                draggable={canMove}
                onDragStart={(event) => event.dataTransfer.setData("text/plain", task.id)}
                onClick={() => onOpen(task.id)}
                className="block w-full rounded-lg border border-line bg-surface px-2 py-2 text-left text-sm"
              >
                <span className="block">{task.title}</span>
                <Badge className="mt-1" tone={priorityTone(task.priority)}>{task.priority}</Badge>
              </button>
            ))}
          </div>
        </div>
      ))}
    </div>
  );
}

function Timeline({ rows }: { rows: TaskListItem[] }) {
  const dated = useMemo(() => rows.filter((task) => task.startAt || task.dueAt), [rows]);
  if (dated.length === 0) return <EmptyState title="Nothing on the timeline" description="Add a start or due date on a task to place it here." />;
  const times = dated.flatMap((task) => [task.startAt, task.dueAt].filter((value): value is string => Boolean(value)).map((value) => new Date(value).getTime()));
  const min = Math.min(...times);
  const max = Math.max(...times);
  const span = Math.max(max - min, 1);
  return (
    <ul className="space-y-3">
      {dated.map((task) => {
        const start = new Date(task.startAt ?? task.dueAt!).getTime();
        const end = new Date(task.dueAt ?? task.startAt!).getTime();
        const left = ((Math.min(start, end) - min) / span) * 100;
        const width = Math.max(((Math.abs(end - start) || span * 0.04) / span) * 100, 4);
        return (
          <li key={task.id}>
            <div className="mb-1 flex justify-between text-sm">
              <span>{task.title}</span>
              <span className="text-muted">{taskLabel(task.status)}</span>
            </div>
            <div className="relative h-2 rounded-full bg-surface-muted">
              <div className="absolute h-2 rounded-full bg-accent" style={{ left: `${left}%`, width: `${width}%` }} />
            </div>
          </li>
        );
      })}
    </ul>
  );
}

function Team({ project, people, canManage }: { project: ProjectDetail; people: { id: string; name: string }[]; canManage: boolean }) {
  const queryClient = useQueryClient();
  const [userId, setUserId] = useState("");
  const [role, setRole] = useState("Member");
  const [message, setMessage] = useState<string | null>(null);
  const add = useMutation({
    mutationFn: () => addProjectMember(project.id, userId, role),
    onSuccess: async () => {
      setUserId("");
      setMessage(null);
      await queryClient.invalidateQueries({ queryKey: ["project", project.id] });
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "Could not add that person."),
  });
  const remove = useMutation({
    mutationFn: (memberId: string) => removeProjectMember(project.id, memberId),
    onSuccess: async () => queryClient.invalidateQueries({ queryKey: ["project", project.id] }),
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "Could not remove that person."),
  });
  return (
    <div className="space-y-4">
      <ul className="divide-y divide-line rounded-xl border border-line">
        {project.members.map((member) => (
          <li key={member.userId} className="flex items-center justify-between gap-3 px-3 py-2 text-sm">
            <span>{member.name}</span>
            <span className="flex items-center gap-2">
              <Badge>{member.role}</Badge>
              {canManage && member.userId !== project.ownerId ? <Button size="sm" variant="ghost" onClick={() => remove.mutate(member.userId)}>Remove</Button> : null}
            </span>
          </li>
        ))}
      </ul>
      {canManage ? (
        <form className="flex flex-wrap gap-2" onSubmit={(event) => { event.preventDefault(); if (userId) add.mutate(); }}>
          <Select value={userId} onChange={(event) => setUserId(event.target.value)} aria-label="Person">
            <option value="">Add a person</option>
            {people.filter((person) => !project.members.some((member) => member.userId === person.id)).map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}
          </Select>
          <Select value={role} onChange={(event) => setRole(event.target.value)} aria-label="Role">
            {memberRoles.map((item) => <option key={item} value={item}>{item}</option>)}
          </Select>
          <Button type="submit" disabled={add.isPending}>Add</Button>
        </form>
      ) : null}
      {message ? <p className="text-sm text-danger">{message}</p> : null}
    </div>
  );
}

function Meetings({ projectId }: { projectId: string }) {
  const meetings = useQuery({ queryKey: ["project-meetings", projectId], queryFn: () => fetchProjectMeetings(projectId) });
  if (meetings.isPending) return <LoadingState label="Loading meetings" />;
  if (meetings.isError) return <ErrorState title="Meetings are unavailable" description="Try again." />;
  if (meetings.data.length === 0) return <EmptyState title="No meetings" description="Meetings linked to this project show up here." />;
  return (
    <ul className="divide-y divide-line rounded-xl border border-line">
      {meetings.data.map((meeting) => (
        <li key={meeting.id} className="flex items-center justify-between px-3 py-2 text-sm">
          <Link className="text-ink" to="/meetings">{meeting.title}</Link>
          <span className="text-muted">{format(parseISO(meeting.startAt), "MMM d, p")}</span>
        </li>
      ))}
    </ul>
  );
}

function Notes({ projectId }: { projectId: string }) {
  const notes = useQuery({ queryKey: ["project-notes", projectId], queryFn: () => fetchProjectNotes(projectId) });
  if (notes.isPending) return <LoadingState label="Loading notes" />;
  if (notes.isError) return <ErrorState title="Notes are unavailable" description="Try again." />;
  if (notes.data.length === 0) return <EmptyState title="No notes" description="Notes you link to this project show up here." />;
  return (
    <ul className="divide-y divide-line rounded-xl border border-line">
      {notes.data.map((note) => (
        <li key={note.id} className="flex items-center justify-between px-3 py-2 text-sm">
          <Link className="text-ink" to="/notes">{note.title}</Link>
          <span className="text-muted">{note.isPinned ? "Pinned" : format(parseISO(note.updatedAt), "MMM d")}</span>
        </li>
      ))}
    </ul>
  );
}

function Activity({ projectId }: { projectId: string }) {
  const activity = useQuery({ queryKey: ["project-activity", projectId], queryFn: () => fetchProjectActivity(projectId) });
  if (activity.isPending) return <LoadingState label="Loading activity" />;
  if (activity.isError) return <ErrorState title="Activity is unavailable" description="Try again." />;
  if (activity.data.length === 0) return <EmptyState title="No activity" description="Changes to the project and its tasks will appear here." />;
  return (
    <ul className="space-y-3">
      {activity.data.map((item, index) => (
        <li key={`${item.at}-${index}`} className="text-sm">
          <p className="text-ink">{item.summary}</p>
          <p className="text-muted">{labelFor(item.action)} · {item.actorName} · {format(parseISO(item.at), "MMM d, p")}</p>
        </li>
      ))}
    </ul>
  );
}
