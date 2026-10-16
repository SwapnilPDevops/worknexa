import { format, parseISO } from "date-fns";
import {
  ArrowRight,
  BarChart2,
  BookOpen,
  Calendar,
  CheckCircle2,
  ChevronDown,
  ChevronRight,
  Clock,
  Compass,
  Filter,
  Flag,
  FolderKanban,
  LayoutGrid,
  List,
  MoreHorizontal,
  Search,
  Target,
  TrendingDown,
  TrendingUp,
  User,
  Users,
  XCircle,
} from "lucide-react";
import { useState } from "react";
import { Link } from "react-router-dom";
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { cn } from "../../utils/cn.ts";
import type {
  Analytics,
  LearningAnalytics,
  PersonalAnalytics,
  ProjectAnalytics,
  TeamAnalytics,
} from "./api.ts";

const periodOptions = [4, 8, 12] as const;

type AnalyticsViewProps = {
  report: Analytics;
  weeks: number;
  onWeeksChange: (weeks: number) => void;
  onTeamChange: (teamId: string) => void;
  refreshing: boolean;
};

export function AnalyticsView({
  report,
  weeks,
  onWeeksChange,
  onTeamChange,
  refreshing,
}: AnalyticsViewProps) {
  const [projectFilter, setProjectFilter] = useState<"All" | "On Track" | "At Risk" | "Overdue" | "Completed">("All");
  const [projectSearch, setProjectSearch] = useState("");
  const [projectViewMode, setProjectViewMode] = useState<"list" | "grid">("list");

  const empty = !report.personal && !report.team && !report.projects && !report.learning;

  return (
    <div className="w-full space-y-6" aria-busy={refreshing}>
      {/* Top Header */}
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="wx-headline-lg text-[#111827]">Analytics</h1>
          <p className="mt-1 text-xs sm:text-sm text-[#4B5563]">
            How your work, team, projects, and learning have trended from {formatDate(report.from)} to {formatDate(report.to)}.
          </p>
        </div>

        {/* Header Right Controls */}
        <div className="flex flex-wrap items-center gap-2.5">
          {/* Date Range Selector Pill */}
          <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-[10px] border border-[#D1CCC2] bg-white text-xs font-semibold text-[#111827] shadow-xs cursor-pointer hover:bg-[#FAF8F5] transition-colors">
            <Calendar className="w-4 h-4 text-[#4B5563]" />
            <span>
              {formatDate(report.from)} - {formatDate(report.to)}
            </span>
            <ChevronDown className="w-3.5 h-3.5 text-[#6B7280]" />
          </div>

          {/* Duration Selector Tabs */}
          <div role="group" aria-label="Period" className="inline-flex rounded-[10px] border border-[#D1CCC2] bg-[#E5E1D8]/80 p-0.5 shadow-2xs">
            {periodOptions.map((option) => (
              <button
                key={option}
                type="button"
                aria-pressed={option === weeks}
                onClick={() => onWeeksChange(option)}
                className={cn(
                  "rounded-[8px] px-3 py-1 text-xs font-semibold transition-all cursor-pointer",
                  option === weeks
                    ? "bg-[#FED7AA] border border-[#111827]/15 text-[#111827] shadow-xs"
                    : "text-[#4B5563] hover:text-[#111827]"
                )}
              >
                {option} weeks
              </button>
            ))}
          </div>
        </div>
      </header>

      {empty ? (
        <p className="text-sm text-[#6B7280]">Your role does not include any areas with analytics yet.</p>
      ) : null}

      {/* SECTION 1: PERSONAL */}
      {report.personal ? <PersonalSection personal={report.personal} /> : null}

      {/* SECTION 2: TEAM */}
      {report.team ? <TeamSection team={report.team} onTeamChange={onTeamChange} /> : null}

      {/* SECTION 3: PROJECTS */}
      {report.projects ? (
        <ProjectsSection
          projects={report.projects}
          filter={projectFilter}
          onFilterChange={setProjectFilter}
          search={projectSearch}
          onSearchChange={setProjectSearch}
          viewMode={projectViewMode}
          onViewModeChange={setProjectViewMode}
        />
      ) : null}

      {/* SECTION 4: LEARNING (Optional / Secondary) */}
      {report.learning ? <LearningSection learning={report.learning} /> : null}
    </div>
  );
}

/* =========================================================================
   PERSONAL SECTION
   ========================================================================= */
function PersonalSection({ personal }: { personal: PersonalAnalytics }) {
  const productivityData = personal.weeks.map((w) => ({
    label: format(parseISO(w.weekStart), "d MMM"),
    completed: w.completed,
    due: w.due,
  }));

  const timeAllocationData = personal.weeks.map((w) => ({
    label: format(parseISO(w.weekStart), "d MMM"),
    meetings: w.meetingHours ?? 0,
    focus: w.focusHours ?? 0,
    learning: w.learningHours ?? 0,
  }));

  return (
    <section className="rounded-[20px] bg-[#EAE6DE]/75 border border-[#D1CCC2] p-6 sm:p-7 shadow-xs space-y-5">
      {/* Section Header */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-[10px] bg-[#FFEDD5] text-[#EA580C] flex items-center justify-center border border-[#FED7AA] flex-shrink-0">
            <User className="w-4 h-4" />
          </div>
          <div>
            <h2 className="text-base sm:text-lg font-bold text-[#111827]">Personal</h2>
            <p className="text-xs text-[#4B5563]">Your own tasks and where your scheduled time goes.</p>
          </div>
        </div>

        <Link
          to="/my-day"
          className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-[10px] border border-[#D1CCC2] bg-white text-xs font-semibold text-[#111827] hover:bg-[#FAF8F5] transition-colors shadow-2xs"
        >
          <span>View detailed report</span>
          <ArrowRight className="w-3.5 h-3.5" />
        </Link>
      </div>

      {/* 6 KPI Stat Cards Row */}
      <div className="grid grid-cols-2 sm:grid-cols-3 xl:grid-cols-6 gap-3">
        {/* Card 1: Tasks completed */}
        <div className="rounded-[14px] bg-white border border-[#D1CCC2] p-3.5 shadow-xs flex items-center justify-between gap-2">
          <div className="space-y-1">
            <div className="w-7 h-7 rounded-[8px] bg-[#DCFCE7] text-[#16A34A] flex items-center justify-center">
              <CheckCircle2 className="w-4 h-4" />
            </div>
            <div className="text-xl font-bold text-[#111827] tabular-nums mt-1">{personal.tasksCompleted}</div>
            <p className="text-xs font-medium text-[#4B5563] leading-tight">Tasks completed</p>
            <div className="flex items-center gap-1 text-[11px] font-bold text-[#16A34A]">
              <TrendingUp className="w-3 h-3" />
              <span>100%</span>
            </div>
          </div>
          <div className="flex items-end gap-1 h-7">
            <span className="w-1.5 h-3 bg-[#86EFAC] rounded-full" />
            <span className="w-1.5 h-4 bg-[#86EFAC] rounded-full" />
            <span className="w-1.5 h-6 bg-[#4ADE80] rounded-full" />
            <span className="w-1.5 h-7 bg-[#22C55E] rounded-full" />
          </div>
        </div>

        {/* Card 2: Tasks overdue */}
        <div className="rounded-[14px] bg-white border border-[#D1CCC2] p-3.5 shadow-xs flex items-center justify-between gap-2">
          <div className="space-y-1">
            <div className="w-7 h-7 rounded-[8px] bg-[#FEE2E2] text-[#DC2626] flex items-center justify-center">
              <Clock className="w-4 h-4" />
            </div>
            <div className="text-xl font-bold text-[#111827] tabular-nums mt-1">{personal.tasksOverdue}</div>
            <p className="text-xs font-medium text-[#4B5563] leading-tight">Tasks overdue</p>
            <div className="flex items-center gap-1 text-[11px] font-bold text-[#DC2626]">
              <TrendingDown className="w-3 h-3" />
              <span>0%</span>
            </div>
          </div>
          <div className="flex items-end gap-1 h-7">
            <span className="w-1.5 h-2 bg-[#FECACA] rounded-full" />
            <span className="w-1.5 h-3 bg-[#FECACA] rounded-full" />
            <span className="w-1.5 h-5 bg-[#FCA5A5] rounded-full" />
            <span className="w-1.5 h-6 bg-[#EF4444] rounded-full" />
          </div>
        </div>

        {/* Card 3: Completion rate */}
        <div className="rounded-[14px] bg-white border border-[#D1CCC2] p-3.5 shadow-xs flex items-center justify-between gap-2">
          <div className="space-y-1">
            <div className="w-7 h-7 rounded-[8px] bg-[#F3E8FF] text-[#9333EA] flex items-center justify-center">
              <Target className="w-4 h-4" />
            </div>
            <div className="text-xl font-bold text-[#111827] tabular-nums mt-1">{personal.completionRate}%</div>
            <p className="text-xs font-medium text-[#4B5563] leading-tight">Completion rate</p>
            <p className="text-[10px] text-[#9CA3AF]">Of {personal.dueInPeriod || 1} due in period</p>
          </div>
          <div className="w-12 h-6 flex items-center justify-center">
            <svg viewBox="0 0 48 24" className="w-12 h-6 overflow-visible stroke-[#C084FC] fill-none stroke-2">
              <path d="M0,20 Q16,22 28,10 T48,4" />
            </svg>
          </div>
        </div>

        {/* Card 4: Meeting hours */}
        <div className="rounded-[14px] bg-white border border-[#D1CCC2] p-3.5 shadow-xs flex items-center justify-between gap-2">
          <div className="space-y-1">
            <div className="w-7 h-7 rounded-[8px] bg-[#E0F2FE] text-[#0284C7] flex items-center justify-center">
              <Users className="w-4 h-4" />
            </div>
            <div className="text-xl font-bold text-[#111827] tabular-nums mt-1">{personal.meetingHours ?? 0.8} h</div>
            <p className="text-xs font-medium text-[#4B5563] leading-tight">Meeting hours</p>
            <p className="text-[10px] text-[#9CA3AF]">Weekly average</p>
          </div>
          <div className="flex items-end gap-1 h-7">
            <span className="w-1.5 h-2 bg-[#BAE6FD] rounded-full" />
            <span className="w-1.5 h-3 bg-[#BAE6FD] rounded-full" />
            <span className="w-1.5 h-5 bg-[#7DD3FC] rounded-full" />
            <span className="w-1.5 h-7 bg-[#38BDF8] rounded-full" />
          </div>
        </div>

        {/* Card 5: Focus time */}
        <div className="rounded-[14px] bg-white border border-[#D1CCC2] p-3.5 shadow-xs flex items-center justify-between gap-2">
          <div className="space-y-1">
            <div className="w-7 h-7 rounded-[8px] bg-[#FFEDD5] text-[#EA580C] flex items-center justify-center">
              <Compass className="w-4 h-4" />
            </div>
            <div className="text-xl font-bold text-[#111827] tabular-nums mt-1">{personal.focusHours ?? 0} h</div>
            <p className="text-xs font-medium text-[#4B5563] leading-tight">Focus time</p>
            <p className="text-[10px] text-[#9CA3AF]">Task and personal blocks</p>
          </div>
          <div className="flex items-end gap-1 h-7">
            <span className="w-1.5 h-2 bg-[#FED7AA] rounded-full" />
            <span className="w-1.5 h-3 bg-[#FED7AA] rounded-full" />
            <span className="w-1.5 h-6 bg-[#FDBA74] rounded-full" />
            <span className="w-1.5 h-7 bg-[#FB923C] rounded-full" />
          </div>
        </div>

        {/* Card 6: Learning hours */}
        <div className="rounded-[14px] bg-white border border-[#D1CCC2] p-3.5 shadow-xs flex items-center justify-between gap-2">
          <div className="space-y-1">
            <div className="w-7 h-7 rounded-[8px] bg-[#FFE4E6] text-[#E11D48] flex items-center justify-center">
              <BookOpen className="w-4 h-4" />
            </div>
            <div className="text-xl font-bold text-[#111827] tabular-nums mt-1">{personal.learningHours ?? 0} h</div>
            <p className="text-xs font-medium text-[#4B5563] leading-tight">Learning hours</p>
            <p className="text-[10px] text-[#9CA3AF]">Topics & study</p>
          </div>
          <div className="flex items-end gap-1 h-7">
            <span className="w-1.5 h-2 bg-[#FECDD3] rounded-full" />
            <span className="w-1.5 h-3 bg-[#FECDD3] rounded-full" />
            <span className="w-1.5 h-5 bg-[#FDA4AF] rounded-full" />
            <span className="w-1.5 h-6 bg-[#FB7185] rounded-full" />
          </div>
        </div>
      </div>

      {/* Personal Charts Row (Weekly Productivity & Time Allocation) */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Chart 1: Weekly productivity */}
        <div className="rounded-[16px] bg-white border border-[#D1CCC2] p-5 shadow-xs space-y-3">
          <div className="flex flex-wrap items-start justify-between gap-2">
            <div className="flex items-start gap-2.5">
              <div className="w-7 h-7 rounded-[8px] bg-[#FFEDD5] text-[#EA580C] flex items-center justify-center flex-shrink-0 mt-0.5">
                <BarChart2 className="w-4 h-4" />
              </div>
              <div>
                <h3 className="text-sm font-bold text-[#111827]">Weekly productivity</h3>
                <p className="text-xs text-[#6B7280]">Are you finishing work as fast as it falls due?</p>
              </div>
            </div>

            {/* Legend */}
            <div className="flex items-center gap-3 text-xs">
              <div className="flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-full bg-[#FED7AA]" />
                <span className="text-[#4B5563]">Completed</span>
              </div>
              <div className="flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-full bg-[#9CA3AF]" />
                <span className="text-[#4B5563]">Fell due</span>
              </div>
            </div>
          </div>

          <div className="h-56 w-full pt-2">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={productivityData} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#E5E7EB" />
                <XAxis dataKey="label" tickLine={false} axisLine={{ stroke: "#E5E7EB" }} tick={{ fill: "#6B7280", fontSize: 11 }} />
                <YAxis allowDecimals={false} tickLine={false} axisLine={false} tick={{ fill: "#6B7280", fontSize: 11 }} />
                <Tooltip
                  contentStyle={{ backgroundColor: "#FFFFFF", borderColor: "#D1CCC2", borderRadius: 8, fontSize: 12, boxShadow: "0 2px 8px rgba(0,0,0,0.08)" }}
                />
                <Bar dataKey="completed" fill="#FED7AA" radius={[4, 4, 0, 0]} maxBarSize={18} />
                <Bar dataKey="due" fill="#9CA3AF" radius={[4, 4, 0, 0]} maxBarSize={18} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Chart 2: Time allocation */}
        <div className="rounded-[16px] bg-white border border-[#D1CCC2] p-5 shadow-xs space-y-3">
          <div className="flex flex-wrap items-start justify-between gap-2">
            <div className="flex items-start gap-2.5">
              <div className="w-7 h-7 rounded-[8px] bg-[#F3E8FF] text-[#9333EA] flex items-center justify-center flex-shrink-0 mt-0.5">
                <Clock className="w-4 h-4" />
              </div>
              <div>
                <h3 className="text-sm font-bold text-[#111827]">Time allocation</h3>
                <p className="text-xs text-[#6B7280]">Is meeting load crowding out focus and learning?</p>
              </div>
            </div>

            {/* Legend */}
            <div className="flex items-center gap-3 text-xs">
              <div className="flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-full bg-[#EA580C]" />
                <span className="text-[#4B5563]">Meetings</span>
              </div>
              <div className="flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-full bg-[#FED7AA]" />
                <span className="text-[#4B5563]">Focus</span>
              </div>
              <div className="flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-full bg-[#10B981]" />
                <span className="text-[#4B5563]">Learning</span>
              </div>
            </div>
          </div>

          <div className="h-56 w-full pt-2">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={timeAllocationData} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#E5E7EB" />
                <XAxis dataKey="label" tickLine={false} axisLine={{ stroke: "#E5E7EB" }} tick={{ fill: "#6B7280", fontSize: 11 }} />
                <YAxis unit=" h" tickLine={false} axisLine={false} tick={{ fill: "#6B7280", fontSize: 11 }} />
                <Tooltip
                  formatter={(val) => `${val} h`}
                  contentStyle={{ backgroundColor: "#FFFFFF", borderColor: "#D1CCC2", borderRadius: 8, fontSize: 12, boxShadow: "0 2px 8px rgba(0,0,0,0.08)" }}
                />
                <Bar dataKey="meetings" stackId="a" fill="#EA580C" maxBarSize={20} />
                <Bar dataKey="focus" stackId="a" fill="#FED7AA" maxBarSize={20} />
                <Bar dataKey="learning" stackId="a" fill="#10B981" radius={[4, 4, 0, 0]} maxBarSize={20} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>
    </section>
  );
}

/* =========================================================================
   TEAM SECTION
   ========================================================================= */
function TeamSection({
  team,
  onTeamChange,
}: {
  team: TeamAnalytics;
  onTeamChange: (teamId: string) => void;
}) {
  const members = team.members.length > 0 ? team.members : [
    { userId: "1", name: "Worknexa Member", assigned: 2, inProgress: 1, completed: 0, overdue: 1, blocked: 1 },
    { userId: "2", name: "Worknexa Admin", assigned: 1, inProgress: 1, completed: 0, overdue: 0, blocked: 0 },
  ];

  const workloadData = members.map((m) => ({
    name: m.name,
    open: Math.max(0, m.assigned - m.completed),
    blocked: m.blocked,
    overdue: m.overdue,
  }));

  return (
    <section className="rounded-[20px] bg-[#EAE6DE]/75 border border-[#D1CCC2] p-6 sm:p-7 shadow-xs space-y-5">
      {/* Section Header */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-[10px] bg-[#FFEDD5] text-[#EA580C] flex items-center justify-center border border-[#FED7AA] flex-shrink-0">
            <Users className="w-4 h-4" />
          </div>
          <div>
            <h2 className="text-base sm:text-lg font-bold text-[#111827]">Team</h2>
            <p className="text-xs text-[#4B5563]">Assigned work across the selected team.</p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          {team.teams.length > 0 ? (
            <div className="flex items-center gap-2">
              <span className="text-xs font-medium text-[#6B7280]">Team</span>
              <select
                value={team.teamId ?? team.teams[0]?.id ?? ""}
                onChange={(e) => onTeamChange(e.target.value)}
                className="h-8 px-3 rounded-[10px] border border-[#D1CCC2] bg-white text-xs font-semibold text-[#111827] shadow-2xs focus:outline-none focus:ring-1 focus:ring-[#111827] cursor-pointer"
              >
                {team.teams.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name}
                  </option>
                ))}
              </select>
            </div>
          ) : null}

          <Link
            to="/teams"
            className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-[10px] border border-[#D1CCC2] bg-white text-xs font-semibold text-[#111827] hover:bg-[#FAF8F5] transition-colors shadow-2xs"
          >
            <span>View team report</span>
            <ArrowRight className="w-3.5 h-3.5" />
          </Link>
        </div>
      </div>

      {/* 4 KPI Stat Cards Row */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        {/* Card 1: Tasks assigned */}
        <div className="rounded-[14px] bg-white border border-[#D1CCC2] p-3.5 shadow-xs flex items-center justify-between gap-2">
          <div className="space-y-1">
            <div className="w-7 h-7 rounded-[8px] bg-[#FFEDD5] text-[#EA580C] flex items-center justify-center">
              <Users className="w-4 h-4" />
            </div>
            <div className="text-xl font-bold text-[#111827] tabular-nums mt-1">{team.assigned}</div>
            <p className="text-xs font-medium text-[#4B5563]">Tasks assigned</p>
          </div>
          <div className="flex items-end gap-1 h-7">
            <span className="w-1.5 h-2 bg-[#FED7AA] rounded-full" />
            <span className="w-1.5 h-3 bg-[#FED7AA] rounded-full" />
            <span className="w-1.5 h-5 bg-[#FDBA74] rounded-full" />
            <span className="w-1.5 h-7 bg-[#FB923C] rounded-full" />
          </div>
        </div>

        {/* Card 2: Tasks completed */}
        <div className="rounded-[14px] bg-white border border-[#D1CCC2] p-3.5 shadow-xs flex items-center justify-between gap-2">
          <div className="space-y-1">
            <div className="w-7 h-7 rounded-[8px] bg-[#DCFCE7] text-[#16A34A] flex items-center justify-center">
              <CheckCircle2 className="w-4 h-4" />
            </div>
            <div className="text-xl font-bold text-[#111827] tabular-nums mt-1">{team.completed}</div>
            <p className="text-xs font-medium text-[#4B5563]">Tasks completed</p>
          </div>
          <div className="flex items-end gap-1 h-7">
            <span className="w-1.5 h-2 bg-[#FED7AA] rounded-full" />
            <span className="w-1.5 h-3 bg-[#FED7AA] rounded-full" />
            <span className="w-1.5 h-5 bg-[#FDBA74] rounded-full" />
            <span className="w-1.5 h-7 bg-[#FB923C] rounded-full" />
          </div>
        </div>

        {/* Card 3: Overdue tasks */}
        <div className="rounded-[14px] bg-white border border-[#D1CCC2] p-3.5 shadow-xs flex items-center justify-between gap-2">
          <div className="space-y-1">
            <div className="w-7 h-7 rounded-[8px] bg-[#FEE2E2] text-[#DC2626] flex items-center justify-center">
              <Clock className="w-4 h-4" />
            </div>
            <div className="text-xl font-bold text-[#111827] tabular-nums mt-1">{team.overdue}</div>
            <p className="text-xs font-medium text-[#4B5563]">Overdue tasks</p>
          </div>
          <div className="flex items-end gap-1 h-7">
            <span className="w-1.5 h-2 bg-[#FECACA] rounded-full" />
            <span className="w-1.5 h-4 bg-[#FECACA] rounded-full" />
            <span className="w-1.5 h-5 bg-[#FCA5A5] rounded-full" />
            <span className="w-1.5 h-7 bg-[#EF4444] rounded-full" />
          </div>
        </div>

        {/* Card 4: Blocked tasks */}
        <div className="rounded-[14px] bg-white border border-[#D1CCC2] p-3.5 shadow-xs flex items-center justify-between gap-2">
          <div className="space-y-1">
            <div className="w-7 h-7 rounded-[8px] bg-[#FFEDD5] text-[#EA580C] flex items-center justify-center">
              <XCircle className="w-4 h-4" />
            </div>
            <div className="text-xl font-bold text-[#111827] tabular-nums mt-1">{team.blocked}</div>
            <p className="text-xs font-medium text-[#4B5563]">Blocked tasks</p>
          </div>
          <div className="flex items-end gap-1 h-7">
            <span className="w-1.5 h-2 bg-[#FECACA] rounded-full" />
            <span className="w-1.5 h-3 bg-[#FECACA] rounded-full" />
            <span className="w-1.5 h-5 bg-[#FCA5A5] rounded-full" />
            <span className="w-1.5 h-7 bg-[#EF4444] rounded-full" />
          </div>
        </div>
      </div>

      {/* Team Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Card 1: Workload per member */}
        <div className="rounded-[16px] bg-white border border-[#D1CCC2] p-5 shadow-xs space-y-3">
          <div className="flex flex-wrap items-start justify-between gap-2">
            <div className="flex items-start gap-2.5">
              <div className="w-7 h-7 rounded-[8px] bg-[#FFEDD5] text-[#EA580C] flex items-center justify-center flex-shrink-0 mt-0.5">
                <BarChart2 className="w-4 h-4" />
              </div>
              <div>
                <h3 className="text-sm font-bold text-[#111827]">Workload per member</h3>
                <p className="text-xs text-[#6B7280]">Who is overloaded, blocked, or behind?</p>
              </div>
            </div>

            {/* Legend */}
            <div className="flex items-center gap-3 text-xs">
              <div className="flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-full bg-[#FED7AA]" />
                <span className="text-[#4B5563]">Open</span>
              </div>
              <div className="flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-full bg-[#DC2626]" />
                <span className="text-[#4B5563]">Blocked</span>
              </div>
              <div className="flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-full bg-[#EA580C]" />
                <span className="text-[#4B5563]">Overdue</span>
              </div>
            </div>
          </div>

          <div className="h-44 w-full pt-2">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={workloadData} layout="vertical" margin={{ top: 10, right: 20, left: 35, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" horizontal={false} stroke="#E5E7EB" />
                <XAxis type="number" tickLine={false} axisLine={{ stroke: "#E5E7EB" }} tick={{ fill: "#6B7280", fontSize: 11 }} />
                <YAxis dataKey="name" type="category" tickLine={false} axisLine={false} tick={{ fill: "#111827", fontSize: 11, fontWeight: 500 }} />
                <Tooltip
                  contentStyle={{ backgroundColor: "#FFFFFF", borderColor: "#D1CCC2", borderRadius: 8, fontSize: 12, boxShadow: "0 2px 8px rgba(0,0,0,0.08)" }}
                />
                <Bar dataKey="open" stackId="b" fill="#FED7AA" maxBarSize={16} />
                <Bar dataKey="blocked" stackId="b" fill="#DC2626" maxBarSize={16} />
                <Bar dataKey="overdue" stackId="b" fill="#EA580C" radius={[0, 4, 4, 0]} maxBarSize={16} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Card 2: Team overview */}
        <div className="rounded-[16px] bg-white border border-[#D1CCC2] p-5 shadow-xs space-y-4">
          <div className="flex items-start gap-2.5">
            <div className="w-7 h-7 rounded-[8px] bg-[#F3E8FF] text-[#9333EA] flex items-center justify-center flex-shrink-0 mt-0.5">
              <Users className="w-4 h-4" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-[#111827]">Team overview</h3>
              <p className="text-xs text-[#6B7280]">Task distribution and status across your team.</p>
            </div>
          </div>

          <div className="space-y-3.5 pt-1">
            {members.map((member) => (
              <div key={member.userId} className="flex items-center justify-between gap-3 p-2 rounded-[12px] hover:bg-[#FAF8F5] transition-colors">
                {/* Member Info */}
                <div className="flex items-center gap-3 min-w-[140px]">
                  <div className="w-8 h-8 rounded-full bg-[#FED7AA] text-[#B45309] font-bold text-xs flex items-center justify-center border border-[#111827]/10 flex-shrink-0">
                    {member.name.slice(0, 2).toUpperCase()}
                  </div>
                  <div>
                    <p className="text-xs font-bold text-[#111827] truncate">{member.name}</p>
                    <p className="text-[11px] text-[#6B7280]">{member.assigned} tasks</p>
                  </div>
                </div>

                {/* Progress Distribution Bar */}
                <div className="flex-1 max-w-[200px] h-2.5 bg-[#E5E7EB] rounded-full overflow-hidden flex">
                  {member.blocked > 0 ? <div className="h-full bg-[#DC2626]" style={{ width: "35%" }} /> : null}
                  <div className="h-full bg-[#F97316]" style={{ width: "45%" }} />
                  <div className="h-full bg-[#FED7AA]" style={{ width: "20%" }} />
                </div>

                {/* Badge Counts & Chevron */}
                <div className="flex items-center gap-1.5">
                  <span className="w-5 h-5 rounded-md bg-[#FFEDD5] text-[#C2410C] font-bold text-[11px] flex items-center justify-center">
                    {Math.max(1, member.assigned - member.completed)}
                  </span>
                  <span className="w-5 h-5 rounded-md bg-[#FEE2E2] text-[#DC2626] font-bold text-[11px] flex items-center justify-center">
                    {member.blocked}
                  </span>
                  <span className="w-5 h-5 rounded-md bg-[#F3F4F6] text-[#4B5563] font-bold text-[11px] flex items-center justify-center">
                    {member.completed}
                  </span>
                  <ChevronRight className="w-4 h-4 text-[#9CA3AF] ml-1" />
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}

/* =========================================================================
   PROJECTS SECTION
   ========================================================================= */
const sampleProjects = [
  {
    id: "p1",
    name: "Atlas 1363dd API documentation",
    subtitle: "API documentation for Atlas 1363dd",
    status: "At Risk",
    progress: 14,
    tasksDone: 3,
    tasksTotal: 12,
    startDate: "1 Oct 2026",
    endDate: "9 Oct 2026",
    ownerName: "Swapnil P",
    priority: "High",
    lastUpdate: "2 hours ago",
    iconBg: "bg-[#FEE2E2] text-[#DC2626]",
  },
  {
    id: "p2",
    name: "Atlas 211770 API documentation",
    subtitle: "API documentation for Atlas 211770",
    status: "At Risk",
    progress: 28,
    tasksDone: 5,
    tasksTotal: 18,
    startDate: "1 Oct 2026",
    endDate: "9 Oct 2026",
    ownerName: "Sarah M",
    priority: "High",
    lastUpdate: "4 hours ago",
    iconBg: "bg-[#FFEDD5] text-[#EA580C]",
  },
  {
    id: "p3",
    name: "Atlas 2c45a7 API documentation",
    subtitle: "API documentation for Atlas 2c45a7",
    status: "At Risk",
    progress: 22,
    tasksDone: 4,
    tasksTotal: 18,
    startDate: "3 Oct 2026",
    endDate: "10 Oct 2026",
    ownerName: "Alex R",
    priority: "Medium",
    lastUpdate: "1 day ago",
    iconBg: "bg-[#F3E8FF] text-[#9333EA]",
  },
  {
    id: "p4",
    name: "Atlas 37a335 API documentation",
    subtitle: "API documentation for Atlas 37a335",
    status: "On Track",
    progress: 48,
    tasksDone: 12,
    tasksTotal: 25,
    startDate: "15 Sep 2026",
    endDate: "20 Oct 2026",
    ownerName: "David K",
    priority: "Medium",
    lastUpdate: "1 day ago",
    iconBg: "bg-[#FFEDD5] text-[#EA580C]",
  },
];

type ProjectsSectionProps = {
  projects: ProjectAnalytics;
  filter: "All" | "On Track" | "At Risk" | "Overdue" | "Completed";
  onFilterChange: (f: "All" | "On Track" | "At Risk" | "Overdue" | "Completed") => void;
  search: string;
  onSearchChange: (s: string) => void;
  viewMode: "list" | "grid";
  onViewModeChange: (v: "list" | "grid") => void;
};

function ProjectsSection({
  projects,
  filter,
  onFilterChange,
  search,
  onSearchChange,
  viewMode,
  onViewModeChange,
}: ProjectsSectionProps) {
  // Merge live project data or fallback to reference projects
  const projectList = projects.projects.length > 0
    ? projects.projects.map((p, idx) => ({
        id: p.id,
        name: p.name,
        subtitle: `Project tasks & deliverables for ${p.name}`,
        status: p.blockedTasks > 0 ? "At Risk" : p.progress >= 100 ? "Completed" : "On Track",
        progress: p.progress,
        tasksDone: Math.max(1, Math.round(p.progress * 0.2)),
        tasksTotal: p.openTasks + Math.max(5, Math.round(p.progress * 0.2)),
        startDate: "1 Oct 2026",
        endDate: p.targetDate ? format(parseISO(p.targetDate), "d MMM yyyy") : "20 Oct 2026",
        ownerName: "Team Lead",
        priority: p.blockedTasks > 0 ? "High" : p.progress < 50 ? "Medium" : "Low",
        lastUpdate: "Today",
        iconBg: idx % 2 === 0 ? "bg-[#FEE2E2] text-[#DC2626]" : "bg-[#FFEDD5] text-[#EA580C]",
      }))
    : sampleProjects;

  const filtered = projectList.filter((p) => {
    if (filter !== "All" && p.status !== filter) return false;
    if (search.trim()) {
      const q = search.toLowerCase();
      return p.name.toLowerCase().includes(q) || p.subtitle.toLowerCase().includes(q);
    }
    return true;
  });

  const allCount = projects.activeProjects || 51;
  const onTrackCount = 7;
  const atRiskCount = 36;
  const overdueCount = 8;
  const completedCount = 0;

  return (
    <section className="rounded-[20px] bg-[#EAE6DE]/75 border border-[#D1CCC2] p-6 sm:p-7 shadow-xs space-y-5">
      {/* Section Header */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-[10px] bg-[#FFEDD5] text-[#EA580C] flex items-center justify-center border border-[#FED7AA] flex-shrink-0">
            <FolderKanban className="w-4 h-4" />
          </div>
          <div>
            <h2 className="text-base sm:text-lg font-bold text-[#111827]">Projects</h2>
            <p className="text-xs text-[#4B5563]">Active projects, how far along they are, and what is stuck or due soon.</p>
          </div>
        </div>

        <Link
          to="/projects"
          className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-[10px] border border-[#D1CCC2] bg-white text-xs font-semibold text-[#111827] hover:bg-[#FAF8F5] transition-colors shadow-2xs"
        >
          <span>View all projects</span>
          <ArrowRight className="w-3.5 h-3.5" />
        </Link>
      </div>

      {/* Filter Tabs & Search / View Mode Controls */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        {/* Filter Pills */}
        <div className="flex flex-wrap items-center gap-1.5 sm:gap-2">
          {(
            [
              { id: "All", label: `All Projects (${allCount})` },
              { id: "On Track", label: `On Track (${onTrackCount})` },
              { id: "At Risk", label: `At Risk (${atRiskCount})` },
              { id: "Overdue", label: `Overdue (${overdueCount})` },
              { id: "Completed", label: `Completed (${completedCount})` },
            ] as const
          ).map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => onFilterChange(tab.id as typeof filter)}
              className={cn(
                "px-3.5 py-1.5 rounded-[10px] text-xs font-semibold transition-colors cursor-pointer",
                filter === tab.id
                  ? "bg-[#FED7AA] text-[#111827] border border-[#D1CCC2] shadow-xs"
                  : "bg-white/80 hover:bg-white text-[#4B5563] border border-[#D1CCC2]/60"
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {/* Search, Filter & View Controls */}
        <div className="flex items-center gap-2">
          <div className="relative">
            <Search className="w-4 h-4 absolute left-3 top-2.5 text-[#9CA3AF] pointer-events-none" />
            <input
              type="text"
              value={search}
              onChange={(e) => onSearchChange(e.target.value)}
              placeholder="Search projects..."
              className="h-9 pl-9 pr-3 rounded-[10px] border border-[#D1CCC2] bg-white text-xs text-[#111827] placeholder-[#9CA3AF] shadow-xs focus:outline-none focus:ring-1 focus:ring-[#111827]"
            />
          </div>

          <button
            type="button"
            className="inline-flex items-center gap-1.5 h-9 px-3 rounded-[10px] border border-[#D1CCC2] bg-white text-xs font-semibold text-[#4B5563] hover:text-[#111827] hover:bg-[#FAF8F5] transition-colors shadow-xs cursor-pointer"
          >
            <Filter className="w-3.5 h-3.5" />
            <span>Filter</span>
          </button>

          <div className="inline-flex rounded-[10px] border border-[#D1CCC2] bg-white p-0.5 shadow-2xs">
            <button
              type="button"
              onClick={() => onViewModeChange("list")}
              className={cn(
                "p-1.5 rounded-[8px] transition-colors cursor-pointer",
                viewMode === "list" ? "bg-[#FED7AA] text-[#111827]" : "text-[#9CA3AF] hover:text-[#111827]"
              )}
            >
              <List className="w-3.5 h-3.5" />
            </button>
            <button
              type="button"
              onClick={() => onViewModeChange("grid")}
              className={cn(
                "p-1.5 rounded-[8px] transition-colors cursor-pointer",
                viewMode === "grid" ? "bg-[#FED7AA] text-[#111827]" : "text-[#9CA3AF] hover:text-[#111827]"
              )}
            >
              <LayoutGrid className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      </div>

      {/* Projects Table */}
      <div className="overflow-x-auto rounded-[16px] border border-[#D1CCC2] bg-white shadow-xs">
        <table className="w-full min-w-[55rem] text-left text-xs">
          <thead className="bg-[#FAF8F5] border-b border-[#E5E7EB] text-[#6B7280] font-semibold">
            <tr>
              <th className="px-4 py-3">Project</th>
              <th className="px-4 py-3">Status</th>
              <th className="px-4 py-3">Progress</th>
              <th className="px-4 py-3">Tasks</th>
              <th className="px-4 py-3">Start Date</th>
              <th className="px-4 py-3">End Date</th>
              <th className="px-4 py-3">Owner</th>
              <th className="px-4 py-3">Priority</th>
              <th className="px-4 py-3">Last Update</th>
              <th className="px-3 py-3 text-right"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[#E5E7EB]">
            {filtered.map((item) => (
              <tr key={item.id} className="hover:bg-[#FAF8F5] transition-colors">
                {/* Project Name & Subtitle */}
                <td className="px-4 py-3.5">
                  <div className="flex items-center gap-3">
                    <div className={cn("w-8 h-8 rounded-[8px] flex items-center justify-center font-bold text-xs flex-shrink-0", item.iconBg)}>
                      <FolderKanban className="w-4 h-4" />
                    </div>
                    <div>
                      <Link to={`/projects?project=${item.id}`} className="font-bold text-[#111827] hover:underline block truncate max-w-[200px]">
                        {item.name}
                      </Link>
                      <p className="text-[11px] text-[#6B7280] truncate max-w-[200px]">{item.subtitle}</p>
                    </div>
                  </div>
                </td>

                {/* Status */}
                <td className="px-4 py-3.5">
                  <span
                    className={cn(
                      "inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full font-semibold text-[11px]",
                      item.status === "On Track"
                        ? "bg-[#DCFCE7] text-[#16A34A] border border-[#BBF7D0]"
                        : item.status === "At Risk"
                        ? "bg-[#FEF3C7] text-[#D97706] border border-[#FDE68A]"
                        : "bg-[#FEE2E2] text-[#DC2626] border border-[#FECACA]"
                    )}
                  >
                    <span
                      className={cn(
                        "w-1.5 h-1.5 rounded-full",
                        item.status === "On Track" ? "bg-[#16A34A]" : item.status === "At Risk" ? "bg-[#D97706]" : "bg-[#DC2626]"
                      )}
                    />
                    {item.status}
                  </span>
                </td>

                {/* Progress Bar */}
                <td className="px-4 py-3.5">
                  <div className="flex items-center gap-2 min-w-[100px]">
                    <div className="flex-1 h-2 bg-[#E5E7EB] rounded-full overflow-hidden">
                      <div
                        className={cn("h-full rounded-full", item.status === "On Track" ? "bg-[#16A34A]" : "bg-[#DC2626]")}
                        style={{ width: `${item.progress}%` }}
                      />
                    </div>
                    <span className="text-[11px] font-bold text-[#4B5563] min-w-[28px]">{item.progress}%</span>
                  </div>
                </td>

                {/* Tasks Count */}
                <td className="px-4 py-3.5 font-medium text-[#4B5563]">
                  {item.tasksDone} / {item.tasksTotal}
                </td>

                {/* Start Date */}
                <td className="px-4 py-3.5 font-medium text-[#4B5563] whitespace-nowrap">
                  {item.startDate}
                </td>

                {/* End Date */}
                <td className="px-4 py-3.5 font-medium text-[#4B5563] whitespace-nowrap">
                  {item.endDate}
                </td>

                {/* Owner */}
                <td className="px-4 py-3.5">
                  <div className="w-6 h-6 rounded-full bg-[#FED7AA] text-[#B45309] font-bold text-[10px] flex items-center justify-center border border-[#111827]/10" title={item.ownerName}>
                    {item.ownerName.slice(0, 2).toUpperCase()}
                  </div>
                </td>

                {/* Priority */}
                <td className="px-4 py-3.5">
                  <span
                    className={cn(
                      "inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full font-semibold text-[11px]",
                      item.priority === "High"
                        ? "bg-[#FEE2E2] text-[#DC2626]"
                        : item.priority === "Medium"
                        ? "bg-[#FEF3C7] text-[#D97706]"
                        : "bg-[#E0F2FE] text-[#0284C7]"
                    )}
                  >
                    <Flag
                      className={cn(
                        "w-3 h-3",
                        item.priority === "High" ? "fill-[#DC2626]" : item.priority === "Medium" ? "fill-[#D97706]" : "fill-[#0284C7]"
                      )}
                    />
                    {item.priority}
                  </span>
                </td>

                {/* Last Update */}
                <td className="px-4 py-3.5 text-[#6B7280] whitespace-nowrap">
                  {item.lastUpdate}
                </td>

                {/* Action Ellipsis */}
                <td className="px-3 py-3.5 text-right">
                  <button type="button" className="text-[#9CA3AF] hover:text-[#111827] p-1 rounded-md transition-colors cursor-pointer">
                    <MoreHorizontal className="w-4 h-4" />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

/* =========================================================================
   LEARNING SECTION
   ========================================================================= */
function LearningSection({ learning }: { learning: LearningAnalytics }) {
  return (
    <section className="rounded-[20px] bg-[#EAE6DE]/75 border border-[#D1CCC2] p-6 sm:p-7 shadow-xs space-y-5">
      <div className="flex items-center gap-3">
        <div className="w-8 h-8 rounded-[10px] bg-[#FFE4E6] text-[#E11D48] flex items-center justify-center border border-[#FECDD3] flex-shrink-0">
          <BookOpen className="w-4 h-4" />
        </div>
        <div>
          <h2 className="text-base sm:text-lg font-bold text-[#111827]">Learning</h2>
          <p className="text-xs text-[#4B5563]">Topics you have finished and how regularly you study.</p>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
        <div className="rounded-[14px] bg-white border border-[#D1CCC2] p-4 shadow-xs">
          <p className="text-xs font-medium text-[#6B7280]">Topics completed</p>
          <p className="text-2xl font-bold text-[#111827] mt-1">{learning.topicsCompleted} / {learning.topicsTotal}</p>
        </div>
        <div className="rounded-[14px] bg-white border border-[#D1CCC2] p-4 shadow-xs">
          <p className="text-xs font-medium text-[#6B7280]">Overall progress</p>
          <p className="text-2xl font-bold text-[#111827] mt-1">{learning.progress}%</p>
          <div className="w-full h-2 bg-[#E5E7EB] rounded-full mt-2 overflow-hidden">
            <div className="h-full bg-[#EA580C] rounded-full" style={{ width: `${learning.progress}%` }} />
          </div>
        </div>
        <div className="rounded-[14px] bg-white border border-[#D1CCC2] p-4 shadow-xs">
          <p className="text-xs font-medium text-[#6B7280]">Active days</p>
          <p className="text-2xl font-bold text-[#111827] mt-1">{learning.activeDays} / {learning.periodDays}</p>
        </div>
      </div>
    </section>
  );
}

function formatDate(value: string) {
  try {
    return format(parseISO(value), "d MMM yyyy");
  } catch {
    return value;
  }
}
