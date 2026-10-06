import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { format } from "date-fns";
import { useEffect, useState, type FormEvent } from "react";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Field, Input, Select } from "../../components/design-system/field.tsx";
import { EmptyState, ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { Table } from "../../components/design-system/table.tsx";
import { ApiError } from "../../services/api-client.ts";
import { AvailabilityWarning } from "../leaves/availability-warning.tsx";
import { fetchAvailability } from "../leaves/api.ts";
import { assignTask, fetchTasks, fetchWorkOptions, localOffsetMinutes, saveTask } from "../work/api.ts";
import { labelFor } from "../work/catalog.ts";
import { fetchTeamWorkload, fetchTeams } from "./api.ts";

export function TeamTasksView() {
  const { user } = useAuth();
  const manageable = can(user?.permissions, permissions.tasksManage);
  const [teamId, setTeamId] = useState("");
  const [title, setTitle] = useState("");
  const [assigneeId, setAssigneeId] = useState("");
  const [dueOn, setDueOn] = useState("");
  const [warnings, setWarnings] = useState<Record<string, string>>({});
  const [message, setMessage] = useState<string | null>(null);
  const queryClient = useQueryClient();
  const teams = useQuery({ queryKey: ["teams"], queryFn: fetchTeams });
  const options = useQuery({ queryKey: ["work-options"], queryFn: fetchWorkOptions });
  const selected = teamId || teams.data?.[0]?.id || "";
  const workload = useQuery({ queryKey: ["team-workload", selected], queryFn: () => fetchTeamWorkload(selected), enabled: Boolean(selected) });
  const tasks = useQuery({
    queryKey: ["team-tasks", selected],
    queryFn: () => fetchTasks(`?view=team&teamId=${selected}&date=${format(new Date(), "yyyy-MM-dd")}&offsetMinutes=${localOffsetMinutes()}`),
    enabled: Boolean(selected),
  });

  useEffect(() => {
    document.title = "Team Tasks · Worknexa";
  }, []);

  function refresh() {
    return Promise.all([
      queryClient.invalidateQueries({ queryKey: ["team-workload"] }),
      queryClient.invalidateQueries({ queryKey: ["team-tasks"] }),
      queryClient.invalidateQueries({ queryKey: ["tasks"] }),
    ]);
  }

  const create = useMutation({
    mutationFn: () => saveTask({
      title,
      description: null,
      status: "Todo",
      priority: "Medium",
      startAt: null,
      dueAt: dueOn ? new Date(`${dueOn}T17:00:00`).toISOString() : null,
      assigneeId: assigneeId || null,
      projectId: null,
      parentTaskId: null,
      teamId: selected,
      estimatedHours: null,
      actualHours: null,
      tags: [],
    }),
    onSuccess: async () => {
      setTitle("");
      setMessage(null);
      await refresh();
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The task could not be created."),
  });

  async function onAssign(taskId: string, nextAssignee: string, dueAt: string | null) {
    setMessage(null);
    if (nextAssignee && dueAt) {
      const availability = await fetchAvailability(nextAssignee, dueAt.slice(0, 10));
      setWarnings((current) => ({ ...current, [taskId]: availability.warning ?? "" }));
    } else {
      setWarnings((current) => ({ ...current, [taskId]: "" }));
    }

    await assignTask(taskId, nextAssignee || null);
    await refresh();
  }

  if (!can(user?.permissions, permissions.tasksRead)) {
    return <ErrorState title="Access denied" description="Your account does not include tasks." />;
  }

  if (teams.isPending) {
    return <LoadingState label="Loading team tasks" />;
  }

  if (!selected) {
    return <EmptyState title="No team yet" description="Create a team before assigning shared work." />;
  }

  return (
    <div className="mx-auto max-w-6xl space-y-5">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Team Tasks</h1>
          <p className="mt-1 text-sm text-muted">Workload across the team, using the same tasks as the rest of the workspace.</p>
        </div>
        <Field label="Team">{({ id }) => (
          <Select id={id} value={selected} onChange={(event) => setTeamId(event.target.value)}>
            {teams.data?.map((team) => <option key={team.id} value={team.id}>{team.name}</option>)}
          </Select>
        )}</Field>
      </header>

      {workload.isPending ? <LoadingState label="Loading workload" /> : null}
      {workload.data ? (
        <Table
          caption="Team workload"
          rows={workload.data}
          getRowKey={(row) => row.userId}
          columns={[
            { key: "member", header: "Member", render: (row) => row.name },
            { key: "assigned", header: "Assigned", render: (row) => row.assigned },
            { key: "progress", header: "In Progress", render: (row) => row.inProgress },
            { key: "completed", header: "Completed", render: (row) => row.completed },
            { key: "overdue", header: "Overdue", render: (row) => row.overdue },
            { key: "blocked", header: "Blocked", render: (row) => row.blocked },
          ]}
        />
      ) : null}

      {manageable ? (
        <form className="grid gap-3 rounded-xl border border-line bg-surface p-4 sm:grid-cols-[1fr_12rem_10rem_auto]" onSubmit={(event: FormEvent) => { event.preventDefault(); void create.mutateAsync(); }}>
          <Field label="Task">{({ id }) => <Input id={id} value={title} onChange={(event) => setTitle(event.target.value)} required />}</Field>
          <Field label="Assignee">{({ id }) => <Select id={id} value={assigneeId} onChange={(event) => setAssigneeId(event.target.value)}><option value="">Unassigned</option>{options.data?.people.map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}</Select>}</Field>
          <Field label="Due">{({ id }) => <Input id={id} type="date" value={dueOn} onChange={(event) => setDueOn(event.target.value)} />}</Field>
          <div className="flex items-end"><Button type="submit" disabled={create.isPending}>Assign task</Button></div>
          {assigneeId && dueOn ? <div className="sm:col-span-4"><AvailabilityWarning userId={assigneeId} on={dueOn} /></div> : null}
          {message ? <p className="text-sm text-danger sm:col-span-4">{message}</p> : null}
        </form>
      ) : null}

      {tasks.isPending ? <LoadingState label="Loading tasks" /> : null}
      {tasks.data && tasks.data.length === 0 ? <EmptyState title="No team tasks" description="Create a task for this team, or open Tasks and set the team there." /> : null}
      {tasks.data && tasks.data.length > 0 ? (
        <Table
          caption="Team tasks"
          rows={tasks.data}
          getRowKey={(row) => row.id}
          columns={[
            { key: "title", header: "Task", render: (row) => row.title },
            { key: "status", header: "Status", render: (row) => labelFor(row.status) },
            { key: "due", header: "Due", render: (row) => row.dueAt ? format(new Date(row.dueAt), "d MMM yyyy") : "—" },
            {
              key: "assignee",
              header: "Assignee",
              render: (row) => (
                <div className="space-y-1">
                  <Select aria-label={`Assignee for ${row.title}`} value={row.assigneeId ?? ""} disabled={!manageable} onChange={(event) => void onAssign(row.id, event.target.value, row.dueAt)}>
                    <option value="">Unassigned</option>
                    {options.data?.people.map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}
                  </Select>
                  {warnings[row.id] ? <p className="text-xs text-warning">{warnings[row.id]}</p> : null}
                </div>
              ),
            },
          ]}
        />
      ) : null}
    </div>
  );
}
