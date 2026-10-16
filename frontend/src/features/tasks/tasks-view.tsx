import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { addMonths, eachDayOfInterval, endOfMonth, endOfWeek, format, isSameMonth, startOfMonth, startOfWeek } from "date-fns";
import { useEffect, useState, type DragEvent, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Badge } from "../../components/design-system/badge.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Drawer } from "../../components/design-system/drawer.tsx";
import { Field, Input, Select, Textarea } from "../../components/design-system/field.tsx";
import { EmptyState, ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { Table } from "../../components/design-system/table.tsx";
import { ApiError } from "../../services/api-client.ts";
import { cn } from "../../utils/cn.ts";
import {
  addTaskDependency,
  assignTask,
  commentOnTask,
  completeTask,
  deleteTask,
  deleteTaskFile,
  downloadTaskFile,
  fetchTask,
  fetchTasks,
  fetchWorkOptions,
  fromDateTimeInput,
  localOffsetMinutes,
  removeTaskDependency,
  reopenTask,
  saveTask,
  setTaskStatus,
  toDateTimeInput,
  uploadTaskFile,
  type TaskDetail,
  type TaskListItem,
  type TaskWrite,
} from "../work/api.ts";
import { AvailabilityWarning } from "../leaves/availability-warning.tsx";
import { labelFor, priorities, priorityTone, taskStatuses, taskViews, type TaskView } from "../work/catalog.ts";

type Draft = {
  title: string;
  description: string;
  status: string;
  priority: string;
  startAt: string;
  dueAt: string;
  assigneeId: string;
  projectId: string;
  parentTaskId: string;
  teamId: string;
  estimatedHours: string;
  actualHours: string;
  tags: string;
};

type Filters = {
  status: string;
  priority: string;
  projectId: string;
  assigneeId: string;
  dueFrom: string;
  dueTo: string;
  tag: string;
};

const emptyFilters: Filters = { status: "", priority: "", projectId: "", assigneeId: "", dueFrom: "", dueTo: "", tag: "" };

export function TasksView({ initialView }: { initialView: TaskView }) {
  const { user } = useAuth();
  const manageable = can(user?.permissions, permissions.tasksManage);
  const [view, setView] = useState<TaskView>(initialView);
  const [filters, setFilters] = useState<Filters>(emptyFilters);
  const [month, setMonth] = useState(() => startOfMonth(new Date()));
  const [params] = useSearchParams();
  const requestedTask = params.get("task");
  const [selectedId, setSelectedId] = useState<string | null>(requestedTask);
  const [creating, setCreating] = useState(false);
  const [seenTask, setSeenTask] = useState(requestedTask);
  if (requestedTask && seenTask !== requestedTask) {
    setSeenTask(requestedTask);
    setSelectedId(requestedTask);
    setCreating(false);
  }
  const [draft, setDraft] = useState<Draft>(() => blankDraft(user?.id ?? ""));
  const [comment, setComment] = useState("");
  const [dependencyId, setDependencyId] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const queryClient = useQueryClient();
  const offsetMinutes = localOffsetMinutes();
  const query = queryString(view, filters, month, offsetMinutes);
  const tasks = useQuery({ queryKey: ["tasks", query], queryFn: () => fetchTasks(query) });
  const options = useQuery({ queryKey: ["work-options"], queryFn: fetchWorkOptions });
  const detail = useQuery({
    queryKey: ["task", selectedId],
    queryFn: () => fetchTask(selectedId!),
    enabled: Boolean(selectedId) && !creating,
  });

  useEffect(() => {
    document.title = `${taskViews.find((item) => item.id === view)?.label ?? "Tasks"} · Worknexa`;
  }, [view]);

  useEffect(() => {
    if (detail.data) {
      setDraft(draftFrom(detail.data));
    }
  }, [detail.data]);

  function refresh() {
    return Promise.all([
      queryClient.invalidateQueries({ queryKey: ["tasks"] }),
      queryClient.invalidateQueries({ queryKey: ["task"] }),
      queryClient.invalidateQueries({ queryKey: ["my-day"] }),
      queryClient.invalidateQueries({ queryKey: ["work-options"] }),
    ]);
  }

  const save = useMutation({
    mutationFn: (input: { id?: string; task: TaskWrite }) => saveTask(input.task, input.id),
    onSuccess: async (task) => {
      setMessage(null);
      setCreating(false);
      setSelectedId(task.id);
      await refresh();
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The task could not be saved."),
  });

  const move = useMutation({
    mutationFn: (input: { id: string; status: string }) => setTaskStatus(input.id, input.status),
    onSuccess: () => refresh(),
  });

  function openCreate(parentTaskId = "") {
    const teamId = initialView === "team" ? options.data?.teams[0]?.id ?? "" : "";
    setDraft({ ...blankDraft(user?.id ?? "", teamId), parentTaskId });
    setCreating(true);
    setSelectedId(null);
    setMessage(null);
  }

  function openTask(id: string) {
    setCreating(false);
    setSelectedId(id);
    setMessage(null);
    setComment("");
  }

  async function onSave(event: FormEvent) {
    event.preventDefault();
    await save.mutateAsync({ id: creating ? undefined : selectedId ?? undefined, task: toWrite(draft) });
  }

  if (!can(user?.permissions, permissions.tasksRead)) {
    return <ErrorState title="Access denied" description="Your account does not include tasks." />;
  }

  if (tasks.isPending) {
    return <LoadingState label="Loading tasks" />;
  }

  if (tasks.isError || !tasks.data) {
    const description = tasks.error instanceof ApiError ? tasks.error.message : "Tasks could not be loaded.";
    return <ErrorState title="Tasks are unavailable" description={description} onRetry={() => void tasks.refetch()} />;
  }

  const rows = tasks.data;
  const drawerOpen = creating || Boolean(selectedId);

  return (
    <div className="w-full space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="wx-headline-lg text-[#111827]">Tasks</h1>
          <p className="mt-1.5 text-sm leading-[20px] text-[#4B5563]">Structured work for a project, a person, or a team.</p>
        </div>
        {manageable ? <Button onClick={() => openCreate()}>New task</Button> : null}
      </header>

      <div className="flex flex-wrap gap-2" role="tablist" aria-label="Task views">
        {taskViews.map((item) => (
          <Button key={item.id} variant={view === item.id ? "primary" : "secondary"} size="sm" onClick={() => setView(item.id)} aria-pressed={view === item.id}>
            {item.label}
          </Button>
        ))}
      </div>

      <FilterBar filters={filters} options={options.data} onChange={setFilters} />

      {rows.length === 0 ? (
        <EmptyState title="No tasks in this view" description="Create a task, or widen the filters." action={manageable ? <Button onClick={() => openCreate()}>New task</Button> : undefined} />
      ) : view === "kanban" ? (
        <Kanban rows={rows} canMove={manageable} onOpen={openTask} onMove={(id, status) => move.mutate({ id, status })} />
      ) : view === "calendar" ? (
        <Calendar month={month} rows={rows} onMonth={setMonth} onOpen={openTask} />
      ) : (
        <TaskTable rows={rows} onOpen={openTask} />
      )}

      <Drawer open={drawerOpen} title={creating ? (draft.parentTaskId ? "New subtask" : "New task") : draft.title || "Task"} onClose={() => { setCreating(false); setSelectedId(null); }} className="max-w-lg">
        <form className="space-y-4" onSubmit={(event) => void onSave(event)}>
          {message ? <p className="text-sm text-danger">{message}</p> : null}
          <Field label="Title">{({ id }) => <Input id={id} value={draft.title} onChange={(event) => setDraft({ ...draft, title: event.target.value })} required maxLength={300} />}</Field>
          <Field label="Description">{({ id }) => <Textarea id={id} value={draft.description} onChange={(event) => setDraft({ ...draft, description: event.target.value })} />}</Field>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Status">{({ id }) => <Select id={id} value={draft.status} onChange={(event) => setDraft({ ...draft, status: event.target.value })}>{taskStatuses.map((status) => <option key={status} value={status}>{labelFor(status)}</option>)}</Select>}</Field>
            <Field label="Priority">{({ id }) => <Select id={id} value={draft.priority} onChange={(event) => setDraft({ ...draft, priority: event.target.value })}>{priorities.map((priority) => <option key={priority} value={priority}>{priority}</option>)}</Select>}</Field>
            <Field label="Start">{({ id }) => <Input id={id} type="datetime-local" value={draft.startAt} onChange={(event) => setDraft({ ...draft, startAt: event.target.value })} />}</Field>
            <Field label="Due">{({ id }) => <Input id={id} type="datetime-local" value={draft.dueAt} onChange={(event) => setDraft({ ...draft, dueAt: event.target.value })} />}</Field>
            <Field label="Assignee">{({ id }) => <Select id={id} value={draft.assigneeId} onChange={(event) => setDraft({ ...draft, assigneeId: event.target.value })}><option value="">Unassigned</option>{options.data?.people.map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}</Select>}</Field>
            <Field label="Project">{({ id }) => <Select id={id} value={draft.projectId} onChange={(event) => setDraft({ ...draft, projectId: event.target.value })}><option value="">No project</option>{options.data?.projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}</Select>}</Field>
            <Field label="Parent task">{({ id }) => <Select id={id} value={draft.parentTaskId} onChange={(event) => setDraft({ ...draft, parentTaskId: event.target.value })}><option value="">None</option>{rows.filter((row) => row.id !== selectedId).map((row) => <option key={row.id} value={row.id}>{row.title}</option>)}</Select>}</Field>
            <Field label="Team">{({ id }) => <Select id={id} value={draft.teamId} onChange={(event) => setDraft({ ...draft, teamId: event.target.value })}><option value="">Personal</option>{options.data?.teams.map((team) => <option key={team.id} value={team.id}>{team.name}</option>)}</Select>}</Field>
            <Field label="Estimated hours">{({ id }) => <Input id={id} type="number" min="0" step="0.25" value={draft.estimatedHours} onChange={(event) => setDraft({ ...draft, estimatedHours: event.target.value })} />}</Field>
            <Field label="Actual hours">{({ id }) => <Input id={id} type="number" min="0" step="0.25" value={draft.actualHours} onChange={(event) => setDraft({ ...draft, actualHours: event.target.value })} />}</Field>
          </div>
          <Field label="Tags" hint="Separate tags with commas.">{({ id }) => <Input id={id} value={draft.tags} onChange={(event) => setDraft({ ...draft, tags: event.target.value })} />}</Field>
          {draft.assigneeId && draft.dueAt ? <AvailabilityWarning userId={draft.assigneeId} on={draft.dueAt.slice(0, 10)} /> : null}
          {manageable ? <Button type="submit" disabled={save.isPending}>{creating ? "Create task" : "Save changes"}</Button> : null}
        </form>

        {detail.data && !creating ? (
          <TaskExtras
            task={detail.data}
            rows={rows}
            manageable={manageable}
            comment={comment}
            dependencyId={dependencyId}
            assigneeId={draft.assigneeId}
            onComment={setComment}
            onDependency={setDependencyId}
            onChanged={refresh}
            onSubtask={() => openCreate(detail.data!.id)}
            onDeleted={() => { setSelectedId(null); void refresh(); }}
          />
        ) : null}
      </Drawer>
    </div>
  );
}

function TaskExtras({
  task,
  rows,
  manageable,
  comment,
  dependencyId,
  assigneeId,
  onComment,
  onDependency,
  onChanged,
  onSubtask,
  onDeleted,
}: {
  task: TaskDetail;
  rows: TaskListItem[];
  manageable: boolean;
  comment: string;
  dependencyId: string;
  assigneeId: string;
  onComment: (value: string) => void;
  onDependency: (value: string) => void;
  onChanged: () => Promise<unknown>;
  onSubtask: () => void;
  onDeleted: () => void;
}) {
  const [confirming, setConfirming] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function run(action: () => Promise<unknown>) {
    setError(null);
    try {
      await action();
      await onChanged();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : "That change could not be saved.");
    }
  }

  return (
    <div className="mt-6 space-y-5 border-t border-line pt-5">
      {task.meetingId ? (
        <p className="text-sm">
          <Link className="text-accent" to={`/meeting-notes?meeting=${task.meetingId}`}>From meeting{task.meetingTitle ? `: ${task.meetingTitle}` : ""}</Link>
        </p>
      ) : null}
      {error ? <p className="text-sm text-danger">{error}</p> : null}
      {manageable ? (
        <div className="flex flex-wrap gap-2">
          {task.status === "Completed" ? (
            <Button variant="secondary" size="sm" onClick={() => void run(() => reopenTask(task.id))}>Reopen</Button>
          ) : (
            <Button variant="secondary" size="sm" onClick={() => void run(() => completeTask(task.id))}>Complete</Button>
          )}
          <Button variant="secondary" size="sm" onClick={() => void run(() => assignTask(task.id, assigneeId || null))}>Assign</Button>
          <Button variant="secondary" size="sm" onClick={onSubtask}>Add subtask</Button>
          {confirming ? (
            <Button variant="danger" size="sm" onClick={() => void run(async () => { await deleteTask(task.id); onDeleted(); })}>Confirm delete</Button>
          ) : (
            <Button variant="ghost" size="sm" onClick={() => setConfirming(true)}>Delete</Button>
          )}
        </div>
      ) : null}

      <section className="space-y-2">
        <h3 className="text-sm font-semibold text-ink">Subtasks</h3>
        {task.subtasks.length === 0 ? <p className="text-sm text-muted">No subtasks yet.</p> : (
          <ul className="space-y-1 text-sm">
            {task.subtasks.map((child) => <li key={child.id}>{child.title} · {labelFor(child.status)}</li>)}
          </ul>
        )}
      </section>

      <section className="space-y-2">
        <h3 className="text-sm font-semibold text-ink">Dependencies</h3>
        {task.dependencies.length === 0 ? <p className="text-sm text-muted">This task does not wait on another task.</p> : (
          <ul className="space-y-1 text-sm">
            {task.dependencies.map((link) => (
              <li key={link.id} className="flex items-center justify-between gap-2">
                <span>{link.title} · {labelFor(link.status)}</span>
                {manageable ? <Button variant="ghost" size="sm" onClick={() => void run(() => removeTaskDependency(task.id, link.id))}>Remove</Button> : null}
              </li>
            ))}
          </ul>
        )}
        {manageable ? (
          <div className="flex gap-2">
            <Select aria-label="Dependency" value={dependencyId} onChange={(event) => onDependency(event.target.value)}>
              <option value="">Choose a task</option>
              {rows.filter((row) => row.id !== task.id).map((row) => <option key={row.id} value={row.id}>{row.title}</option>)}
            </Select>
            <Button variant="secondary" size="sm" disabled={!dependencyId} onClick={() => void run(async () => { await addTaskDependency(task.id, dependencyId); onDependency(""); })}>Add</Button>
          </div>
        ) : null}
      </section>

      <section className="space-y-2">
        <h3 className="text-sm font-semibold text-ink">Comments</h3>
        {task.comments.length === 0 ? <p className="text-sm text-muted">No comments yet.</p> : (
          <ul className="space-y-2">
            {task.comments.map((item) => (
              <li key={item.id} className="rounded-lg bg-surface-muted px-3 py-2">
                <p className="text-sm text-ink">{item.body}</p>
                <p className="mt-1 text-xs text-muted">{item.authorName} · {format(new Date(item.createdAt), "d MMM, HH:mm")}</p>
              </li>
            ))}
          </ul>
        )}
        {manageable ? (
          <div className="flex gap-2">
            <Input aria-label="Comment" value={comment} onChange={(event) => onComment(event.target.value)} placeholder="Add a comment" />
            <Button variant="secondary" size="sm" disabled={!comment.trim()} onClick={() => void run(async () => { await commentOnTask(task.id, comment.trim()); onComment(""); })}>Send</Button>
          </div>
        ) : null}
      </section>

      <section className="space-y-2">
        <h3 className="text-sm font-semibold text-ink">Attachments</h3>
        {task.attachments.length === 0 ? <p className="text-sm text-muted">No files attached.</p> : (
          <ul className="space-y-1 text-sm">
            {task.attachments.map((file) => (
              <li key={file.id} className="flex items-center justify-between gap-2">
                <button type="button" className="text-accent" onClick={() => void downloadTaskFile(task.id, file.id, file.fileName)}>{file.fileName}</button>
                {manageable ? <Button variant="ghost" size="sm" onClick={() => void run(() => deleteTaskFile(task.id, file.id))}>Remove</Button> : null}
              </li>
            ))}
          </ul>
        )}
        {manageable ? (
          <Input aria-label="Attachment" type="file" onChange={(event) => {
            const file = event.target.files?.[0];
            if (file) {
              void run(() => uploadTaskFile(task.id, file));
            }
            event.target.value = "";
          }} />
        ) : null}
      </section>
    </div>
  );
}

function FilterBar({ filters, options, onChange }: { filters: Filters; options?: { projects: { id: string; name: string }[]; people: { id: string; name: string }[]; tags: string[] }; onChange: (filters: Filters) => void }) {
  return (
    <div className="grid gap-2 sm:grid-cols-3 xl:grid-cols-6">
      <Select aria-label="Filter by status" value={filters.status} onChange={(event) => onChange({ ...filters, status: event.target.value })}>
        <option value="">All statuses</option>
        {taskStatuses.map((status) => <option key={status} value={status}>{labelFor(status)}</option>)}
      </Select>
      <Select aria-label="Filter by priority" value={filters.priority} onChange={(event) => onChange({ ...filters, priority: event.target.value })}>
        <option value="">All priorities</option>
        {priorities.map((priority) => <option key={priority} value={priority}>{priority}</option>)}
      </Select>
      <Select aria-label="Filter by project" value={filters.projectId} onChange={(event) => onChange({ ...filters, projectId: event.target.value })}>
        <option value="">All projects</option>
        {options?.projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}
      </Select>
      <Select aria-label="Filter by assignee" value={filters.assigneeId} onChange={(event) => onChange({ ...filters, assigneeId: event.target.value })}>
        <option value="">All assignees</option>
        {options?.people.map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}
      </Select>
      <Input aria-label="Due from" type="date" value={filters.dueFrom} onChange={(event) => onChange({ ...filters, dueFrom: event.target.value })} />
      <Input aria-label="Due to" type="date" value={filters.dueTo} onChange={(event) => onChange({ ...filters, dueTo: event.target.value })} />
      <Input aria-label="Tag" value={filters.tag} placeholder="Tag" onChange={(event) => onChange({ ...filters, tag: event.target.value })} list="task-tags" />
      <datalist id="task-tags">{options?.tags.map((tag) => <option key={tag} value={tag} />)}</datalist>
    </div>
  );
}

function TaskTable({ rows, onOpen }: { rows: TaskListItem[]; onOpen: (id: string) => void }) {
  return (
    <Table
      caption="Tasks"
      rows={rows}
      getRowKey={(row) => row.id}
      columns={[
        { key: "title", header: "Title", render: (row) => <button type="button" className="font-medium text-white hover:text-accent transition-colors cursor-pointer text-left" onClick={() => onOpen(row.id)}>{row.title}</button> },
        { key: "status", header: "Status", render: (row) => labelFor(row.status) },
        { key: "priority", header: "Priority", render: (row) => <Badge tone={priorityTone(row.priority)}>{row.priority}</Badge> },
        { key: "assignee", header: "Assignee", render: (row) => row.assigneeName ?? "Unassigned" },
        { key: "project", header: "Project", render: (row) => row.projectName ?? "—" },
        { key: "due", header: "Due", render: (row) => row.dueAt ? format(new Date(row.dueAt), "d MMM, HH:mm") : "—" },
      ]}
    />
  );
}

function Kanban({ rows, canMove, onOpen, onMove }: { rows: TaskListItem[]; canMove: boolean; onOpen: (id: string) => void; onMove: (id: string, status: string) => void }) {
  return (
    <div className="flex gap-3.5 overflow-x-auto pb-3">
      {taskStatuses.map((status) => {
        const cards = rows.filter((row) => row.status === status);
        return (
          <section
            key={status}
            className="w-72 shrink-0 rounded-[8px] border border-[#D1CCC2] bg-[#D7D3CB] p-3 shadow-xs"
            onDragOver={(event) => event.preventDefault()}
            onDrop={(event) => onDrop(event, status, onMove)}
          >
            <h2 className="px-1 py-1 text-[11px] font-mono font-semibold tracking-wider text-[#4B5563] uppercase">{labelFor(status)} · {cards.length}</h2>
            <div className="space-y-2.5 mt-1.5">
              {cards.map((card) => (
                <article
                  key={card.id}
                  draggable={canMove}
                  onDragStart={(event) => event.dataTransfer.setData("text/plain", card.id)}
                  className="rounded-[8px] border border-[#D1CCC2] bg-[#EBE8E2] p-3.5 shadow-xs transition-all hover:border-[#111827]/40"
                >
                  <button type="button" className="text-left text-sm font-medium text-[#111827] hover:underline transition-colors w-full cursor-pointer" onClick={() => onOpen(card.id)}>{card.title}</button>
                  <div className="mt-2 flex flex-wrap gap-1">
                    <Badge tone={priorityTone(card.priority)}>{card.priority}</Badge>
                    {card.assigneeName ? <Badge>{card.assigneeName}</Badge> : null}
                  </div>
                </article>
              ))}
            </div>
          </section>
        );
      })}
    </div>
  );
}

function Calendar({ month, rows, onMonth, onOpen }: { month: Date; rows: TaskListItem[]; onMonth: (month: Date) => void; onOpen: (id: string) => void }) {
  const days = eachDayOfInterval({ start: startOfWeek(startOfMonth(month), { weekStartsOn: 1 }), end: endOfWeek(endOfMonth(month), { weekStartsOn: 1 }) });
  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <Button variant="secondary" size="sm" onClick={() => onMonth(addMonths(month, -1))}>Previous</Button>
        <h2 className="text-sm font-semibold text-ink">{format(month, "MMMM yyyy")}</h2>
        <Button variant="secondary" size="sm" onClick={() => onMonth(addMonths(month, 1))}>Next</Button>
      </div>
      <div className="grid grid-cols-7 gap-1 text-xs">
        {["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map((day) => <div key={day} className="px-1 py-1 text-muted">{day}</div>)}
        {days.map((day) => {
          const key = format(day, "yyyy-MM-dd");
          const items = rows.filter((row) => row.dueAt && format(new Date(row.dueAt), "yyyy-MM-dd") === key);
          return (
            <div key={key} className={cn("min-h-20 rounded-lg border border-line p-1", isSameMonth(day, month) ? "bg-surface" : "bg-surface-muted text-muted")}>
              <p className="text-[11px]">{format(day, "d")}</p>
              {items.map((item) => (
                <button key={item.id} type="button" className="mt-1 block w-full truncate text-left text-[11px] text-ink" onClick={() => onOpen(item.id)}>{item.title}</button>
              ))}
            </div>
          );
        })}
      </div>
    </div>
  );
}

function onDrop(event: DragEvent, status: string, onMove: (id: string, status: string) => void) {
  event.preventDefault();
  const id = event.dataTransfer.getData("text/plain");
  if (id) {
    onMove(id, status);
  }
}

function blankDraft(assigneeId: string, teamId = "", parentTaskId = ""): Draft {
  return {
    title: "",
    description: "",
    status: "Todo",
    priority: "Medium",
    startAt: "",
    dueAt: "",
    assigneeId,
    projectId: "",
    parentTaskId,
    teamId,
    estimatedHours: "",
    actualHours: "",
    tags: "",
  };
}

function draftFrom(task: TaskDetail): Draft {
  return {
    title: task.title,
    description: task.description ?? "",
    status: task.status,
    priority: task.priority,
    startAt: toDateTimeInput(task.startAt),
    dueAt: toDateTimeInput(task.dueAt),
    assigneeId: task.assigneeId ?? "",
    projectId: task.projectId ?? "",
    parentTaskId: task.parentTaskId ?? "",
    teamId: task.teamId ?? "",
    estimatedHours: task.estimatedHours?.toString() ?? "",
    actualHours: task.actualHours?.toString() ?? "",
    tags: task.tags.join(", "),
  };
}

function toWrite(draft: Draft): TaskWrite {
  return {
    title: draft.title.trim(),
    description: draft.description.trim() || null,
    status: draft.status,
    priority: draft.priority,
    startAt: fromDateTimeInput(draft.startAt),
    dueAt: fromDateTimeInput(draft.dueAt),
    assigneeId: draft.assigneeId || null,
    projectId: draft.projectId || null,
    parentTaskId: draft.parentTaskId || null,
    teamId: draft.teamId || null,
    estimatedHours: numberOrNull(draft.estimatedHours),
    actualHours: numberOrNull(draft.actualHours),
    tags: draft.tags.split(",").map((tag) => tag.trim()).filter(Boolean),
  };
}

function numberOrNull(value: string) {
  if (!value.trim()) {
    return null;
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

function queryString(view: TaskView, filters: Filters, month: Date, offsetMinutes: number) {
  const params = new URLSearchParams({ view, date: format(view === "calendar" ? month : new Date(), "yyyy-MM-dd"), offsetMinutes: String(offsetMinutes) });
  if (filters.status) params.set("status", filters.status);
  if (filters.priority) params.set("priority", filters.priority);
  if (filters.projectId) params.set("projectId", filters.projectId);
  if (filters.assigneeId && view !== "mine") params.set("assigneeId", filters.assigneeId);
  if (view !== "calendar" && filters.dueFrom) params.set("dueFrom", new Date(`${filters.dueFrom}T00:00:00`).toISOString());
  if (view !== "calendar" && filters.dueTo) {
    const end = new Date(`${filters.dueTo}T00:00:00`);
    end.setDate(end.getDate() + 1);
    params.set("dueTo", end.toISOString());
  }
  if (filters.tag) params.set("tag", filters.tag);
  return `?${params.toString()}`;
}
