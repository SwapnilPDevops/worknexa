import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { addDays, format, parseISO } from "date-fns";
import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Field, Input, Select, Textarea } from "../../components/design-system/field.tsx";
import { ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { ApiError } from "../../services/api-client.ts";
import { fetchWorkOptions } from "../work/api.ts";
import { fetchStandup, saveStandup, searchStandups, type StandupLine, type StandupWrite } from "./api.ts";
import { standupStatuses } from "./catalog.ts";

type Mine = {
  yesterday: string;
  today: string;
  blockers: string;
  notes: string;
  status: string;
};

const emptyMine: Mine = { yesterday: "", today: "", blockers: "", notes: "", status: "Clear" };

function dateFromParam(value: string | null) {
  if (!value) {
    return null;
  }
  const [year, month, day] = value.split("-").map(Number);
  if (!year || !month || !day) {
    return null;
  }
  return new Date(year, month - 1, day);
}

function fromLine(line: StandupLine | undefined): Mine {
  if (!line) {
    return emptyMine;
  }

  return {
    yesterday: line.yesterday ?? "",
    today: line.today ?? "",
    blockers: line.blockers ?? "",
    notes: line.notes ?? "",
    status: line.status,
  };
}

export function StandupsView() {
  const { user } = useAuth();
  const [params] = useSearchParams();
  const requestedDate = params.get("date");
  const [cursor, setCursor] = useState(() => dateFromParam(requestedDate) ?? new Date());
  const [seenDate, setSeenDate] = useState(requestedDate);
  const nextDate = dateFromParam(requestedDate);
  if (requestedDate && seenDate !== requestedDate && nextDate) {
    setSeenDate(requestedDate);
    setCursor(nextDate);
  }
  const [teamId, setTeamId] = useState("");
  const [query, setQuery] = useState("");
  const [mine, setMine] = useState<Mine>(emptyMine);
  const [message, setMessage] = useState<string | null>(null);
  const queryClient = useQueryClient();
  const date = format(cursor, "yyyy-MM-dd");
  const options = useQuery({ queryKey: ["work-options"], queryFn: fetchWorkOptions });
  const board = useQuery({ queryKey: ["standup", date, teamId], queryFn: () => fetchStandup(date, teamId) });
  const hits = useQuery({
    queryKey: ["standup-search", query],
    queryFn: () => searchStandups(query.trim()),
    enabled: query.trim().length > 0,
  });

  useEffect(() => {
    document.title = "Standups · Worknexa";
  }, []);

  useEffect(() => {
    setMine(fromLine(board.data?.lines.find((line) => line.mine)));
  }, [board.data]);

  const save = useMutation({
    mutationFn: (entry: StandupWrite) => saveStandup(entry),
    onSuccess: async () => {
      setMessage("Saved.");
      await queryClient.invalidateQueries({ queryKey: ["standup", date, teamId] });
      await queryClient.invalidateQueries({ queryKey: ["standup-search"] });
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The standup could not be saved."),
  });

  function patch(partial: Partial<Mine>) {
    setMine((current) => ({ ...current, ...partial }));
  }

  if (!can(user?.permissions, permissions.meetingsRead)) {
    return <ErrorState title="Access denied" description="Your account does not include standups." />;
  }

  return (
    <div className="mx-auto max-w-5xl space-y-5">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Standups</h1>
          <p className="mt-1 text-sm text-muted">{format(cursor, "EEEE, d MMMM yyyy")}</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button type="button" variant="secondary" size="sm" onClick={() => setCursor((current) => addDays(current, -1))}>Previous</Button>
          <Button type="button" variant="secondary" size="sm" onClick={() => setCursor(new Date())}>Today</Button>
          <Button type="button" variant="secondary" size="sm" onClick={() => setCursor((current) => addDays(current, 1))}>Next</Button>
          <Input className="w-40" type="date" aria-label="Standup date" value={date} onChange={(event) => event.target.value && setCursor(parseISO(event.target.value))} />
        </div>
      </header>

      <div className="grid gap-3 sm:grid-cols-2">
        <Field label="Board">{({ id }) => (
          <Select id={id} value={teamId} onChange={(event) => setTeamId(event.target.value)}>
            <option value="">My standup</option>
            {options.data?.teams.map((team) => <option key={team.id} value={team.id}>{team.name}</option>)}
          </Select>
        )}</Field>
        <Field label="Search history">{({ id }) => <Input id={id} value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Yesterday, blockers, a name" />}</Field>
      </div>

      {board.data?.meetingId ? (
        <p className="rounded-xl border border-line bg-surface px-3 py-2 text-sm">
          Standup meeting: <Link className="text-accent underline" to={`/meetings?meeting=${board.data.meetingId}`}>{board.data.meetingTitle}</Link>
        </p>
      ) : null}

      {board.isPending ? <LoadingState label="Loading standup" /> : null}
      {board.isError ? <ErrorState title="Standup is unavailable" description={board.error instanceof ApiError ? board.error.message : "The board could not be loaded."} onRetry={() => void board.refetch()} /> : null}

      {board.data ? (
        <form
          className="space-y-4 rounded-xl border border-line bg-surface p-4"
          onSubmit={(event) => {
            event.preventDefault();
            void save.mutateAsync({
              date,
              teamId: teamId || null,
              yesterday: mine.yesterday.trim() || null,
              today: mine.today.trim() || null,
              blockers: mine.blockers.trim() || null,
              notes: mine.notes.trim() || null,
              status: mine.status,
            });
          }}
        >
          <h2 className="text-sm font-semibold">{teamId ? "My update" : "My standup"}</h2>
          {message ? <p className="text-sm text-muted">{message}</p> : null}
          <div className="grid gap-3 sm:grid-cols-2">
            <Field label="Yesterday">{({ id }) => <Textarea id={id} value={mine.yesterday} onChange={(event) => patch({ yesterday: event.target.value })} />}</Field>
            <Field label="Today">{({ id }) => <Textarea id={id} value={mine.today} onChange={(event) => patch({ today: event.target.value })} />}</Field>
            <Field label="Blockers">{({ id }) => <Textarea id={id} value={mine.blockers} onChange={(event) => patch({ blockers: event.target.value })} />}</Field>
            <Field label="Notes">{({ id }) => <Textarea id={id} value={mine.notes} onChange={(event) => patch({ notes: event.target.value })} />}</Field>
          </div>
          <Field label="Status">{({ id }) => <Select id={id} value={mine.status} onChange={(event) => patch({ status: event.target.value })}>{standupStatuses.map((item) => <option key={item.id} value={item.id}>{item.label}</option>)}</Select>}</Field>
          <Button type="submit" disabled={save.isPending}>Save standup</Button>
        </form>
      ) : null}

      {teamId && board.data ? <TeamTable lines={board.data.lines} /> : null}

      {query.trim() ? (
        <section className="space-y-2">
          <h2 className="text-sm font-semibold">History</h2>
          {hits.isPending ? <LoadingState label="Searching standups" /> : null}
          {hits.data && hits.data.length === 0 ? <p className="text-sm text-muted">No standup entries match that search.</p> : null}
          <ul className="space-y-2">
            {hits.data?.map((hit, index) => (
              <li key={`${hit.date}-${hit.memberName}-${index}`} className="rounded-xl border border-line bg-surface px-3 py-2 text-sm">
                <button type="button" className="text-left" onClick={() => { setQuery(""); setCursor(parseISO(hit.date)); }}>
                  <span className="font-medium">{hit.memberName}</span>
                  <span className="text-muted"> · {hit.date}{hit.teamName ? ` · ${hit.teamName}` : ""} · {hit.status}</span>
                  {hit.today ? <span className="mt-1 block text-muted">{hit.today}</span> : null}
                  {hit.blockers ? <span className="mt-1 block">Blockers: {hit.blockers}</span> : null}
                </button>
              </li>
            ))}
          </ul>
        </section>
      ) : null}
    </div>
  );
}

function TeamTable({ lines }: { lines: StandupLine[] }) {
  return (
    <div className="overflow-x-auto rounded-xl border border-line">
      <table className="w-full min-w-[40rem] text-left text-sm">
        <thead className="bg-surface-muted text-xs text-muted">
          <tr>
            <th className="px-3 py-2 font-medium">Member</th>
            <th className="px-3 py-2 font-medium">Yesterday</th>
            <th className="px-3 py-2 font-medium">Today</th>
            <th className="px-3 py-2 font-medium">Blockers</th>
            <th className="px-3 py-2 font-medium">Status</th>
          </tr>
        </thead>
        <tbody>
          {lines.map((line) => (
            <tr key={line.userId} className="border-t border-line bg-surface">
              <td className="px-3 py-2 font-medium">{line.memberName}</td>
              <td className="px-3 py-2">{line.yesterday}</td>
              <td className="px-3 py-2">{line.today}</td>
              <td className="px-3 py-2">{line.blockers}</td>
              <td className="px-3 py-2">{line.status}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
