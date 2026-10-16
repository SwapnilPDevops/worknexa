import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { addDays, format, parseISO } from "date-fns";
import {
  Calendar,
  ChevronLeft,
  ChevronRight,
  Flag,
  Layers,
  Lightbulb,
  ListChecks,
  Save,
  Search,
  Sliders,
  Target,
} from "lucide-react";
import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { ApiError } from "../../services/api-client.ts";
import { fetchWorkOptions } from "../work/api.ts";
import { fetchStandup, saveStandup, searchStandups, type StandupLine, type StandupWrite } from "./api.ts";
import { standupStatuses } from "./catalog.ts";
import { cn } from "../../utils/cn.ts";

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
    <div className="w-full space-y-6">
      {/* Top Header */}
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="wx-headline-lg text-[#111827]">Standups</h1>
          <div className="mt-1 flex items-center gap-2 text-sm text-[#4B5563]">
            <Calendar className="w-4 h-4 text-[#4B5563]" />
            <span>{format(cursor, "EEEE, d MMMM yyyy")}</span>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={() => setCursor((current) => addDays(current, -1))}
          >
            <ChevronLeft className="w-3.5 h-3.5 mr-0.5 inline-block" />
            Previous
          </Button>
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={() => setCursor(new Date())}
          >
            Today
          </Button>
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={() => setCursor((current) => addDays(current, 1))}
          >
            Next
            <ChevronRight className="w-3.5 h-3.5 ml-0.5 inline-block" />
          </Button>
          <div className="relative">
            <input
              type="date"
              aria-label="Standup date"
              value={date}
              onChange={(event) => event.target.value && setCursor(parseISO(event.target.value))}
              className="h-9 px-3 py-1.5 rounded-[8px] border border-[#D1CCC2] bg-white text-xs font-mono font-medium text-[#111827] shadow-xs focus:outline-none focus:ring-1 focus:ring-[#111827]"
            />
          </div>
        </div>
      </header>

      {/* Selectors: Board / Project & Search History */}
      <div className="grid gap-4 sm:grid-cols-2">
        {/* Board / Project Selection */}
        <div className="space-y-1.5">
          <label htmlFor="standup-board-select" className="block text-xs font-bold text-[#111827]">
            Board / Project
          </label>
          <div className="relative flex items-center">
            <div className="absolute left-3.5 pointer-events-none text-[#4B5563]">
              <Layers className="w-4 h-4" />
            </div>
            <select
              id="standup-board-select"
              aria-label="Board"
              value={teamId}
              onChange={(event) => setTeamId(event.target.value)}
              className="w-full h-11 pl-10 pr-9 rounded-[12px] border border-[#D1CCC2] bg-white text-sm font-medium text-[#111827] shadow-xs focus:outline-none focus:ring-1 focus:ring-[#111827] cursor-pointer appearance-none"
            >
              <option value="">My standup</option>
              {options.data?.teams.map((team) => (
                <option key={team.id} value={team.id}>
                  {team.name}
                </option>
              ))}
            </select>
            <div className="absolute right-3.5 pointer-events-none text-[#6B7280]">
              <span className="text-xs">▼</span>
            </div>
          </div>
        </div>

        {/* Search History */}
        <div className="space-y-1.5">
          <label htmlFor="standup-search-input" className="block text-xs font-bold text-[#111827]">
            Search history
          </label>
          <div className="relative flex items-center">
            <div className="absolute left-3.5 pointer-events-none text-[#4B5563]">
              <Search className="w-4 h-4" />
            </div>
            <input
              id="standup-search-input"
              aria-label="Search history"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Yesterday, blockers, a name..."
              className="w-full h-11 pl-10 pr-4 rounded-[12px] border border-[#D1CCC2] bg-white text-sm text-[#111827] placeholder-[#9CA3AF] shadow-xs focus:outline-none focus:ring-1 focus:ring-[#111827]"
            />
          </div>
        </div>
      </div>

      {/* Standup Meeting Linked Banner */}
      {board.data?.meetingId ? (
        <div className="flex items-center gap-2 rounded-[12px] border border-[#D1CCC2] bg-[#DFDCD6]/80 px-4 py-3 text-sm text-[#111827] shadow-xs">
          <Calendar className="w-4 h-4 text-[#D97706]" />
          <span>
            Standup meeting:{" "}
            <Link className="text-[#111827] font-semibold underline hover:text-black" to={`/meetings?meeting=${board.data.meetingId}`}>
              {board.data.meetingTitle}
            </Link>
          </span>
        </div>
      ) : null}

      {board.isPending ? <LoadingState label="Loading standup" /> : null}
      {board.isError ? (
        <ErrorState
          title="Standup is unavailable"
          description={board.error instanceof ApiError ? board.error.message : "The board could not be loaded."}
          onRetry={() => void board.refetch()}
        />
      ) : null}

      {/* Main Standup Form Card (Exact match to reference image) */}
      {board.data ? (
        <form
          className="space-y-6 rounded-[20px] border border-[#D1CCC2] bg-[#EAE6DE]/75 p-6 sm:p-8 shadow-xs"
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
          {/* Form Header */}
          <div className="flex items-center justify-between pb-2 border-b border-[#D1CCC2]/50">
            <h2 className="text-sm font-semibold font-mono text-[#111827]">
              {teamId ? "My update" : "My standup"}
            </h2>
            {message ? (
              <span className="text-xs font-mono font-semibold text-[#059669] bg-[#059669]/15 px-3 py-1 rounded-full border border-[#059669]/30">
                {message}
              </span>
            ) : null}
          </div>

          {/* 2x2 Question Grid */}
          <div className="grid gap-5 sm:grid-cols-2">
            {/* Box 1: What did I do yesterday? */}
            <div className="space-y-2">
              <div className="flex items-start gap-3">
                <div className="w-8 h-8 rounded-[8px] bg-[#FED7AA] text-[#B45309] flex items-center justify-center flex-shrink-0 mt-0.5 border border-[#111827]/10">
                  <ListChecks className="w-4 h-4" />
                </div>
                <div>
                  <label htmlFor="standup-yesterday" className="block text-sm font-bold text-[#111827] cursor-pointer">
                    What did I do yesterday?
                  </label>
                  <p className="text-xs text-[#4B5563]">Tasks completed, progress made, meetings, etc.</p>
                </div>
              </div>
              <textarea
                id="standup-yesterday"
                aria-label="Yesterday"
                value={mine.yesterday}
                onChange={(event) => patch({ yesterday: event.target.value })}
                placeholder="Write what you completed yesterday..."
                className="w-full min-h-[115px] rounded-[12px] border border-[#D1CCC2]/80 bg-white p-3.5 text-sm text-[#111827] placeholder-[#9CA3AF] shadow-xs focus:outline-none focus:ring-1 focus:ring-[#111827] resize-y"
              />
            </div>

            {/* Box 2: What am I doing today? */}
            <div className="space-y-2">
              <div className="flex items-start gap-3">
                <div className="w-8 h-8 rounded-[8px] bg-[#DFDCD6] text-[#374151] flex items-center justify-center flex-shrink-0 mt-0.5 border border-[#111827]/10">
                  <Sliders className="w-4 h-4" />
                </div>
                <div>
                  <label htmlFor="standup-today" className="block text-sm font-bold text-[#111827] cursor-pointer">
                    What am I doing today?
                  </label>
                  <p className="text-xs text-[#4B5563]">Tasks you plan to work on today.</p>
                </div>
              </div>
              <textarea
                id="standup-today"
                aria-label="Today"
                value={mine.today}
                onChange={(event) => patch({ today: event.target.value })}
                placeholder="Write what you will work on today..."
                className="w-full min-h-[115px] rounded-[12px] border border-[#D1CCC2]/80 bg-white p-3.5 text-sm text-[#111827] placeholder-[#9CA3AF] shadow-xs focus:outline-none focus:ring-1 focus:ring-[#111827] resize-y"
              />
            </div>

            {/* Box 3: Blockers / Issues */}
            <div className="space-y-2">
              <div className="flex items-start gap-3">
                <div className="w-8 h-8 rounded-[8px] bg-[#FEE2E2] text-[#DC2626] flex items-center justify-center flex-shrink-0 mt-0.5 border border-[#DC2626]/20">
                  <Target className="w-4 h-4" />
                </div>
                <div>
                  <label htmlFor="standup-blockers" className="block text-sm font-bold text-[#111827] cursor-pointer">
                    Blockers / Issues
                  </label>
                  <p className="text-xs text-[#4B5563]">Any blockers, dependencies or help needed?</p>
                </div>
              </div>
              <textarea
                id="standup-blockers"
                aria-label="Blockers"
                value={mine.blockers}
                onChange={(event) => patch({ blockers: event.target.value })}
                placeholder="Write any blockers or issues..."
                className="w-full min-h-[115px] rounded-[12px] border border-[#D1CCC2]/80 bg-white p-3.5 text-sm text-[#111827] placeholder-[#9CA3AF] shadow-xs focus:outline-none focus:ring-1 focus:ring-[#111827] resize-y"
              />
            </div>

            {/* Box 4: Additional Notes */}
            <div className="space-y-2">
              <div className="flex items-start gap-3">
                <div className="w-8 h-8 rounded-[8px] bg-[#FFEDD5] text-[#D97706] flex items-center justify-center flex-shrink-0 mt-0.5 border border-[#D97706]/20">
                  <Lightbulb className="w-4 h-4" />
                </div>
                <div>
                  <label htmlFor="standup-notes" className="block text-sm font-bold text-[#111827] cursor-pointer">
                    Additional Notes
                  </label>
                  <p className="text-xs text-[#4B5563]">Any other updates, risks or context.</p>
                </div>
              </div>
              <textarea
                id="standup-notes"
                aria-label="Notes"
                value={mine.notes}
                onChange={(event) => patch({ notes: event.target.value })}
                placeholder="Write any additional notes..."
                className="w-full min-h-[115px] rounded-[12px] border border-[#D1CCC2]/80 bg-white p-3.5 text-sm text-[#111827] placeholder-[#9CA3AF] shadow-xs focus:outline-none focus:ring-1 focus:ring-[#111827] resize-y"
              />
            </div>
          </div>

          {/* Status Selection (Full Width) */}
          <div className="space-y-2 pt-2">
            <div className="flex items-start gap-3">
              <div className="w-8 h-8 rounded-[8px] bg-[#DFDCD6] text-[#374151] flex items-center justify-center flex-shrink-0 mt-0.5 border border-[#111827]/10">
                <Flag className="w-4 h-4" />
              </div>
              <div>
                <label htmlFor="standup-status" className="block text-sm font-bold text-[#111827] cursor-pointer">
                  Status
                </label>
                <p className="text-xs text-[#4B5563]">Select your current status for today.</p>
              </div>
            </div>
            <div className="relative flex items-center">
              <div className="absolute left-3.5 pointer-events-none">
                <span
                  className={cn(
                    "inline-block w-2.5 h-2.5 rounded-full",
                    mine.status === "Clear"
                      ? "bg-[#10B981]"
                      : mine.status === "Blocked"
                      ? "bg-[#EF4444]"
                      : "bg-[#F59E0B]"
                  )}
                />
              </div>
              <select
                id="standup-status"
                aria-label="Status"
                value={mine.status}
                onChange={(event) => patch({ status: event.target.value })}
                className="w-full h-11 pl-9 pr-8 rounded-[12px] border border-[#D1CCC2]/80 bg-white text-sm font-semibold text-[#111827] shadow-xs focus:outline-none focus:ring-1 focus:ring-[#111827] cursor-pointer appearance-none"
              >
                {standupStatuses.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.label}
                  </option>
                ))}
              </select>
              <div className="absolute right-3.5 pointer-events-none text-[#6B7280]">
                <span className="text-xs">▼</span>
              </div>
            </div>
          </div>

          {/* Submit Button */}
          <div className="pt-2">
            <button
              type="submit"
              disabled={save.isPending}
              className="inline-flex items-center gap-2 px-5 py-2.5 rounded-[10px] bg-[#FED7AA] hover:bg-[#FDBA74] text-[#111827] text-sm font-bold border border-[#111827]/15 shadow-xs transition-colors cursor-pointer disabled:opacity-50"
            >
              <Save className="w-4 h-4 text-[#111827]" />
              <span>Save standup</span>
            </button>
          </div>
        </form>
      ) : null}

      {/* Team Standup Table (when team is selected) */}
      {teamId && board.data ? <TeamTable lines={board.data.lines} /> : null}

      {/* History Search Results */}
      {query.trim() ? (
        <section className="space-y-3 pt-2">
          <div className="flex items-center gap-2">
            <h2 className="text-sm font-bold text-[#111827]">History Results</h2>
            <span className="text-xs font-mono text-[#6B7280]">
              ({hits.data?.length ?? 0} matches)
            </span>
          </div>
          {hits.isPending ? <LoadingState label="Searching standups" /> : null}
          {hits.data && hits.data.length === 0 ? (
            <p className="text-sm text-[#6B7280] italic">No standup entries match that search.</p>
          ) : null}
          <ul className="space-y-2.5">
            {hits.data?.map((hit, index) => (
              <li
                key={`${hit.date}-${hit.memberName}-${index}`}
                className="rounded-[12px] border border-[#D1CCC2] bg-white p-4 text-sm shadow-xs hover:border-[#111827]/40 transition-colors"
              >
                <button
                  type="button"
                  className="w-full text-left cursor-pointer"
                  onClick={() => {
                    setQuery("");
                    setCursor(parseISO(hit.date));
                  }}
                >
                  <div className="flex items-center justify-between gap-2 mb-1">
                    <span className="font-bold text-[#111827]">{hit.memberName}</span>
                    <span
                      className={cn(
                        "px-2 py-0.5 rounded-[6px] text-xs font-mono font-semibold",
                        hit.status === "Clear"
                          ? "bg-emerald-100 text-emerald-800"
                          : hit.status === "Blocked"
                          ? "bg-rose-100 text-rose-800"
                          : "bg-amber-100 text-amber-800"
                      )}
                    >
                      {hit.status}
                    </span>
                  </div>
                  <div className="text-xs font-mono text-[#6B7280] mb-2">
                    {hit.date}
                    {hit.teamName ? ` · ${hit.teamName}` : ""}
                  </div>
                  {hit.today ? (
                    <div className="text-xs text-[#374151] mb-1">
                      <span className="font-semibold text-[#111827]">Today: </span>
                      {hit.today}
                    </div>
                  ) : null}
                  {hit.blockers ? (
                    <div className="text-xs text-[#DC2626]">
                      <span className="font-semibold">Blockers: </span>
                      {hit.blockers}
                    </div>
                  ) : null}
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
    <div className="overflow-x-auto rounded-[16px] border border-[#D1CCC2] bg-white shadow-xs">
      <table className="w-full min-w-[40rem] text-left text-sm">
        <thead className="bg-[#EAE6DE] text-xs font-mono font-semibold text-[#4B5563]">
          <tr>
            <th className="px-4 py-3">Member</th>
            <th className="px-4 py-3">Yesterday</th>
            <th className="px-4 py-3">Today</th>
            <th className="px-4 py-3">Blockers</th>
            <th className="px-4 py-3">Status</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-[#E5E7EB]">
          {lines.map((line) => (
            <tr key={line.userId} className="hover:bg-[#FAF8F5] transition-colors">
              <td className="px-4 py-3 font-semibold text-[#111827]">{line.memberName}</td>
              <td className="px-4 py-3 text-xs text-[#374151]">{line.yesterday || "—"}</td>
              <td className="px-4 py-3 text-xs text-[#374151]">{line.today || "—"}</td>
              <td className="px-4 py-3 text-xs text-[#DC2626] font-medium">{line.blockers || "—"}</td>
              <td className="px-4 py-3 text-xs font-mono">
                <span
                  className={cn(
                    "inline-flex items-center gap-1.5 px-2 py-0.5 rounded-[6px] font-semibold text-[11px]",
                    line.status === "Clear"
                      ? "bg-emerald-100 text-emerald-800"
                      : line.status === "Blocked"
                      ? "bg-rose-100 text-rose-800"
                      : "bg-amber-100 text-amber-800"
                  )}
                >
                  <span
                    className={cn(
                      "w-1.5 h-1.5 rounded-full",
                      line.status === "Clear"
                        ? "bg-emerald-600"
                        : line.status === "Blocked"
                        ? "bg-rose-600"
                        : "bg-amber-600"
                    )}
                  />
                  {line.status}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
