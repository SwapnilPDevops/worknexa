import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { addMonths, eachDayOfInterval, endOfMonth, endOfWeek, format, isSameMonth, startOfMonth, startOfWeek } from "date-fns";
import { useEffect, useState, type FormEvent } from "react";
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
import { fetchTeams } from "../teams/api.ts";
import { decideLeave, fetchLeaveOptions, fetchLeaves, fetchTeamAvailability, saveLeave, type LeaveWrite } from "./api.ts";
import { labelFor, leaveTone, leaveViews, type LeaveView } from "./catalog.ts";

type Draft = {
  employeeId: string;
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  reason: string;
};

export function LeavesView() {
  const { user } = useAuth();
  const manageable = can(user?.permissions, permissions.leavesManage);
  const [view, setView] = useState<LeaveView>("list");
  const [month, setMonth] = useState(() => startOfMonth(new Date()));
  const [teamId, setTeamId] = useState("");
  const [creating, setCreating] = useState(false);
  const [draft, setDraft] = useState<Draft>({ employeeId: "", leaveTypeId: "", startDate: "", endDate: "", reason: "" });
  const [message, setMessage] = useState<string | null>(null);
  const queryClient = useQueryClient();
  const rangeStart = format(startOfMonth(month), "yyyy-MM-dd");
  const rangeEnd = format(endOfMonth(month), "yyyy-MM-dd");
  const [week] = useState(() => {
    const today = new Date();
    return {
      start: format(startOfWeek(today, { weekStartsOn: 1 }), "yyyy-MM-dd"),
      end: format(endOfWeek(today, { weekStartsOn: 1 }), "yyyy-MM-dd"),
    };
  });
  const leaves = useQuery({ queryKey: ["leaves", view === "calendar" ? rangeStart : "all"], queryFn: () => fetchLeaves(view === "calendar" ? `?from=${rangeStart}&to=${rangeEnd}` : "") });
  const options = useQuery({ queryKey: ["leave-options"], queryFn: fetchLeaveOptions });
  const teams = useQuery({ queryKey: ["teams"], queryFn: fetchTeams, enabled: view === "availability" });
  const availability = useQuery({
    queryKey: ["team-availability", teamId, week.start, week.end],
    queryFn: () => fetchTeamAvailability(week.start, week.end, teamId),
    enabled: view === "availability",
  });

  useEffect(() => {
    document.title = "Leaves · Worknexa";
  }, []);

  function refresh() {
    return Promise.all([
      queryClient.invalidateQueries({ queryKey: ["leaves"] }),
      queryClient.invalidateQueries({ queryKey: ["team-availability"] }),
      queryClient.invalidateQueries({ queryKey: ["leave-availability"] }),
    ]);
  }

  const save = useMutation({
    mutationFn: (leave: LeaveWrite) => saveLeave(leave),
    onSuccess: async () => {
      setCreating(false);
      setMessage(null);
      await refresh();
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The leave could not be saved."),
  });

  const decide = useMutation({
    mutationFn: (input: { id: string; status: string }) => decideLeave(input.id, input.status),
    onSuccess: () => refresh(),
  });

  if (!can(user?.permissions, permissions.leavesRead)) {
    return <ErrorState title="Access denied" description="Your account does not include leave." />;
  }

  if (leaves.isPending) {
    return <LoadingState label="Loading leave" />;
  }

  if (leaves.isError || !leaves.data) {
    return <ErrorState title="Leave is unavailable" description={leaves.error instanceof ApiError ? leaves.error.message : "Leave could not be loaded."} onRetry={() => void leaves.refetch()} />;
  }

  const days = draft.startDate && draft.endDate && draft.startDate <= draft.endDate
    ? Math.round((Date.parse(draft.endDate) - Date.parse(draft.startDate)) / 86400000) + 1
    : null;

  return (
    <div className="w-full space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="wx-headline-lg text-[#111827]">Leaves</h1>
          <p className="mt-1.5 text-sm leading-[20px] text-[#4B5563]">Time away, so planning stays honest.</p>
        </div>
        <Button onClick={() => { setDraft({ employeeId: manageable ? "" : user?.id ?? "", leaveTypeId: options.data?.leaveTypes[0]?.id ?? "", startDate: "", endDate: "", reason: "" }); setCreating(true); setMessage(null); }}>Request leave</Button>
      </header>

      <div className="flex flex-wrap gap-2" role="tablist" aria-label="Leave views">
        {leaveViews.map((item) => (
          <Button key={item.id} size="sm" variant={view === item.id ? "primary" : "secondary"} aria-pressed={view === item.id} onClick={() => setView(item.id)}>{item.label}</Button>
        ))}
      </div>

      {view === "list" ? (
        leaves.data.length === 0 ? <EmptyState title="No leave yet" description="Request time away, then a manager can approve it." /> : (
          <Table
            caption="Leave requests"
            rows={leaves.data}
            getRowKey={(row) => row.id}
            columns={[
              { key: "employee", header: "Employee", render: (row) => row.employeeName },
              { key: "type", header: "Leave type", render: (row) => row.leaveType },
              { key: "start", header: "Start date", render: (row) => row.startDate },
              { key: "end", header: "End date", render: (row) => row.endDate },
              { key: "days", header: "Days", render: (row) => row.days },
              { key: "reason", header: "Reason", render: (row) => row.reason ?? "—" },
              { key: "status", header: "Status", render: (row) => <Badge tone={leaveTone(row.status)}>{labelFor(row.status)}</Badge> },
              { key: "approved", header: "Approved by", render: (row) => row.approvedByName ?? "—" },
              {
                key: "actions",
                header: "",
                render: (row) => row.status === "Pending" && manageable ? (
                  <span className="flex gap-2">
                    <Button size="sm" onClick={() => decide.mutate({ id: row.id, status: "Approved" })}>Approve</Button>
                    <Button size="sm" variant="secondary" onClick={() => decide.mutate({ id: row.id, status: "Rejected" })}>Reject</Button>
                  </span>
                ) : (row.status === "Pending" || row.status === "Approved") && (manageable || row.employeeId === user?.id) ? (
                  <Button size="sm" variant="ghost" onClick={() => decide.mutate({ id: row.id, status: "Cancelled" })}>Cancel</Button>
                ) : null,
              },
            ]}
          />
        )
      ) : null}

      {view === "calendar" ? (
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <Button variant="secondary" size="sm" onClick={() => setMonth((current) => addMonths(current, -1))}>Previous</Button>
            <p className="text-sm font-medium text-ink">{format(month, "MMMM yyyy")}</p>
            <Button variant="secondary" size="sm" onClick={() => setMonth((current) => addMonths(current, 1))}>Next</Button>
          </div>
          <div className="grid grid-cols-7 gap-px overflow-hidden rounded-xl border border-line bg-line">
            {["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"].map((day) => <div key={day} className="bg-surface-muted px-2 py-1 text-xs text-muted">{day}</div>)}
            {eachDayOfInterval({ start: startOfWeek(month, { weekStartsOn: 1 }), end: endOfWeek(endOfMonth(month), { weekStartsOn: 1 }) }).map((day) => {
              const key = format(day, "yyyy-MM-dd");
              const items = leaves.data.filter((leave) => leave.startDate <= key && leave.endDate >= key && leave.status !== "Rejected" && leave.status !== "Cancelled");
              return (
                <div key={key} className={cn("min-h-20 bg-surface p-1.5", isSameMonth(day, month) ? "" : "opacity-40")}>
                  <p className="text-xs text-muted">{format(day, "d")}</p>
                  {items.slice(0, 2).map((leave) => <p key={leave.id} className="truncate text-[11px] text-warning">{leave.employeeName}</p>)}
                </div>
              );
            })}
          </div>
        </div>
      ) : null}

      {view === "availability" ? (
        <div className="space-y-3">
          <Field label="Team">{({ id }) => <Select id={id} value={teamId} onChange={(event) => setTeamId(event.target.value)}><option value="">Everyone you can see</option>{teams.data?.map((team) => <option key={team.id} value={team.id}>{team.name}</option>)}</Select>}</Field>
          {availability.isPending ? <LoadingState label="Loading availability" /> : null}
          {availability.data ? (
            <Table
              caption="Team availability"
              rows={availability.data}
              getRowKey={(row) => row.userId}
              columns={[
                { key: "name", header: "Member", render: (row) => row.name },
                { key: "availability", header: "This week", render: (row) => <Badge tone={leaveTone(row.availability)}>{labelFor(row.availability)}</Badge> },
                { key: "leave", header: "Leave", render: (row) => row.leaves.length === 0 ? "Available" : row.leaves.map((leave) => `${leave.leaveType} ${leave.startDate}–${leave.endDate}`).join(", ") },
              ]}
            />
          ) : null}
        </div>
      ) : null}

      <Drawer open={creating} title="Request leave" onClose={() => setCreating(false)}>
        <form className="space-y-4" onSubmit={(event: FormEvent) => { event.preventDefault(); void save.mutateAsync({ employeeId: draft.employeeId || user?.id || "", leaveTypeId: draft.leaveTypeId, startDate: draft.startDate, endDate: draft.endDate, reason: draft.reason || null }); }}>
          {message ? <p className="text-sm text-danger">{message}</p> : null}
          <Field label="Employee">{({ id }) => <Select id={id} value={draft.employeeId || user?.id || ""} onChange={(event) => setDraft({ ...draft, employeeId: event.target.value })} disabled={!manageable}>{options.data?.people.map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}</Select>}</Field>
          <Field label="Leave type">{({ id }) => <Select id={id} value={draft.leaveTypeId} onChange={(event) => setDraft({ ...draft, leaveTypeId: event.target.value })} required><option value="">Choose</option>{options.data?.leaveTypes.map((type) => <option key={type.id} value={type.id}>{type.name}</option>)}</Select>}</Field>
          <div className="grid grid-cols-2 gap-3">
            <Field label="Start date">{({ id }) => <Input id={id} type="date" value={draft.startDate} onChange={(event) => setDraft({ ...draft, startDate: event.target.value })} required />}</Field>
            <Field label="End date">{({ id }) => <Input id={id} type="date" value={draft.endDate} onChange={(event) => setDraft({ ...draft, endDate: event.target.value })} required />}</Field>
          </div>
          <p className="text-sm text-muted">{days ? `${days} ${days === 1 ? "day" : "days"}` : "Days are counted from the start through the end date."}</p>
          <Field label="Reason">{({ id }) => <Textarea id={id} value={draft.reason} onChange={(event) => setDraft({ ...draft, reason: event.target.value })} />}</Field>
          <Button type="submit" disabled={save.isPending}>Submit request</Button>
        </form>
      </Drawer>
    </div>
  );
}
