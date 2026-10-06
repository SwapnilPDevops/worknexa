import { format, parseISO } from "date-fns";
import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { Card, CardBody, CardHeader } from "../../components/design-system/card.tsx";
import { Select } from "../../components/design-system/field.tsx";
import { Progress } from "../../components/design-system/progress.tsx";
import { cn } from "../../utils/cn.ts";
import type { Analytics, LearningAnalytics, PersonalAnalytics, ProjectAnalytics, TaskFlag, TeamAnalytics } from "./api.ts";
import { LearningConsistencyChart, ProjectScheduleChart, TeamWorkloadChart, TimeAllocationChart, WeeklyProductivityChart } from "./analytics-charts.tsx";

const periodOptions = [4, 8, 12] as const;

type AnalyticsViewProps = {
  report: Analytics;
  weeks: number;
  onWeeksChange: (weeks: number) => void;
  onTeamChange: (teamId: string) => void;
  refreshing: boolean;
};

export function AnalyticsView({ report, weeks, onWeeksChange, onTeamChange, refreshing }: AnalyticsViewProps) {
  const empty = !report.personal && !report.team && !report.projects && !report.learning;
  return (
    <div className="mx-auto max-w-6xl space-y-8" aria-busy={refreshing}>
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink sm:text-3xl">Analytics</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-muted">
            How your work, team, projects, and learning have trended from {formatDate(report.from)} to {formatDate(report.to)}.
          </p>
        </div>
        <div role="group" aria-label="Period" className="inline-flex rounded-lg border border-line bg-surface-muted p-0.5">
          {periodOptions.map((option) => (
            <button
              key={option}
              type="button"
              aria-pressed={option === weeks}
              className={option === weeks ? "rounded-md bg-surface px-3 py-1.5 text-sm font-medium text-ink shadow-[var(--wx-shadow-rest)]" : "rounded-md px-3 py-1.5 text-sm text-muted hover:text-ink"}
              onClick={() => onWeeksChange(option)}
            >
              {option} weeks
            </button>
          ))}
        </div>
      </header>

      {empty ? <p className="text-sm text-muted">Your role does not include any areas with analytics yet.</p> : null}
      {report.personal ? <PersonalSection personal={report.personal} /> : null}
      {report.team ? <TeamSection team={report.team} onTeamChange={onTeamChange} /> : null}
      {report.projects ? <ProjectsSection projects={report.projects} /> : null}
      {report.learning ? <LearningSection learning={report.learning} /> : null}
    </div>
  );
}

function PersonalSection({ personal }: { personal: PersonalAnalytics }) {
  const noCalendar = personal.meetingHours === null;
  return (
    <Section title="Personal" description="Your own tasks and where your scheduled time goes.">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-6">
        <Stat label="Tasks completed" value={personal.tasksCompleted} />
        <Stat label="Tasks overdue" value={personal.tasksOverdue} tone={personal.tasksOverdue > 0 ? "text-danger" : undefined} hint="Open and past due" />
        <Stat
          label="Completion rate"
          value={personal.dueInPeriod === 0 ? "—" : `${personal.completionRate}%`}
          hint={personal.dueInPeriod === 0 ? "Nothing fell due yet" : `Of ${personal.dueInPeriod} due in period`}
        />
        <Stat label="Meeting hours" value={hours(personal.meetingHours)} hint={noCalendar ? "Needs calendar access" : undefined} />
        <Stat label="Focus time" value={hours(personal.focusHours)} hint={noCalendar ? "Needs calendar access" : "Task and personal blocks"} />
        <Stat label="Learning hours" value={hours(personal.learningHours)} hint={noCalendar ? "Needs calendar access" : undefined} />
      </div>
      <div className="grid items-start gap-4 lg:grid-cols-2">
        <WeeklyProductivityChart personal={personal} />
        {noCalendar ? null : <TimeAllocationChart personal={personal} />}
      </div>
    </Section>
  );
}

function TeamSection({ team, onTeamChange }: { team: TeamAnalytics; onTeamChange: (teamId: string) => void }) {
  const picker = team.teams.length > 1 ? (
    <label className="flex items-center gap-2 text-sm text-muted">
      Team
      <Select className="w-48" value={team.teamId ?? ""} onChange={(event) => onTeamChange(event.target.value)}>
        {team.teams.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
      </Select>
    </label>
  ) : null;

  return (
    <Section title="Team" description={team.teams.length === 1 ? `Assigned work across ${team.teams[0]?.name}.` : "Assigned work across the selected team."} action={picker}>
      {team.teamId === null ? <p className="text-sm text-muted">You are not on a team yet.</p> : (
        <>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            <Stat label="Tasks assigned" value={team.assigned} />
            <Stat label="Tasks completed" value={team.completed} />
            <Stat label="Overdue tasks" value={team.overdue} tone={team.overdue > 0 ? "text-danger" : undefined} />
            <Stat label="Blocked tasks" value={team.blocked} tone={team.blocked > 0 ? "text-warning" : undefined} />
          </div>
          <TeamWorkloadChart team={team} />
        </>
      )}
    </Section>
  );
}

function ProjectsSection({ projects }: { projects: ProjectAnalytics }) {
  return (
    <Section title="Projects" description="Active projects, how far along they are, and what is stuck or due soon.">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
        <Stat label="Active projects" value={projects.activeProjects} />
        <Stat label="Task completion" value={projects.countedTasks === 0 ? "—" : `${projects.completionRate}%`} hint={`${projects.completedTasks} of ${projects.countedTasks} tasks`} />
        <Stat label="Blocked work" value={projects.blockedTasks} tone={projects.blockedTasks > 0 ? "text-warning" : undefined} hint="Blocked tasks" />
      </div>
      <div className="grid items-start gap-4 lg:grid-cols-5">
        <div className="lg:col-span-3">
          <ProjectScheduleChart projects={projects} />
        </div>
        <div className="space-y-4 lg:col-span-2">
          <FlagList title="Upcoming deadlines" description="Open project tasks due in the next 14 days." items={projects.deadlines} empty="Nothing is due in the next two weeks." />
          <FlagList title="Blocked work" description="Longest blocked first." items={projects.blocked} empty="No project tasks are blocked." tone="text-warning" />
        </div>
      </div>
    </Section>
  );
}

function LearningSection({ learning }: { learning: LearningAnalytics }) {
  return (
    <Section title="Learning" description="Topics you have finished and how regularly you study.">
      <div className="grid items-start gap-4 lg:grid-cols-5">
        <div className="grid gap-3 sm:grid-cols-3 lg:col-span-2 lg:grid-cols-1">
          <Stat label="Topics completed" value={`${learning.topicsCompleted} / ${learning.topicsTotal}`} />
          <article className="rounded-xl border border-line bg-surface px-3 py-3 shadow-[var(--wx-shadow-rest)]">
            <p className="mb-3 text-[11px] font-semibold tracking-wide text-muted uppercase">Learning progress</p>
            <Progress value={learning.progress} label="Across top-level topics" />
          </article>
          <Stat label="Active days" value={`${learning.activeDays} / ${learning.periodDays}`} hint="Days with learning activity" />
        </div>
        <div className="lg:col-span-3">
          <LearningConsistencyChart learning={learning} />
        </div>
      </div>
    </Section>
  );
}

function Section({ title, description, action, children }: { title: string; description: string; action?: ReactNode; children: ReactNode }) {
  const id = `analytics-${title.toLowerCase()}`;
  return (
    <section aria-labelledby={id} className="space-y-4">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h2 id={id} className="text-lg font-semibold tracking-tight text-ink">{title}</h2>
          <p className="mt-0.5 text-sm text-muted">{description}</p>
        </div>
        {action}
      </div>
      {children}
    </section>
  );
}

function Stat({ label, value, hint, tone }: { label: string; value: number | string; hint?: string; tone?: string }) {
  return (
    <article className="rounded-xl border border-line bg-surface px-3 py-3 shadow-[var(--wx-shadow-rest)]">
      <p className="text-[11px] font-semibold tracking-wide text-muted uppercase">{label}</p>
      <p className={cn("mt-2 text-2xl font-semibold tabular-nums text-ink", tone)}>{value}</p>
      {hint ? <p className="mt-1 text-[11px] text-muted">{hint}</p> : null}
    </article>
  );
}

function FlagList({ title, description, items, empty, tone }: { title: string; description: string; items: TaskFlag[]; empty: string; tone?: string }) {
  return (
    <Card>
      <CardHeader title={title} description={description} />
      <CardBody>
        {items.length === 0 ? <p className="text-sm text-muted">{empty}</p> : (
          <ul className="divide-y divide-line">
            {items.map((item) => (
              <li key={item.taskId} className="flex items-start justify-between gap-3 py-2 first:pt-0 last:pb-0">
                <div className="min-w-0">
                  <Link to={`/tasks?task=${item.taskId}`} className="block truncate text-sm font-medium text-ink hover:text-accent">{item.title}</Link>
                  <p className="mt-0.5 truncate text-xs text-muted">
                    <Link to={`/projects?project=${item.projectId}`} className="hover:text-ink">{item.projectName}</Link>
                    {` · ${item.assigneeName ?? "Unassigned"}`}
                  </p>
                </div>
                {item.dueAt ? <time className={cn("shrink-0 text-xs tabular-nums text-muted", tone)} dateTime={item.dueAt}>{format(parseISO(item.dueAt), "d MMM")}</time> : null}
              </li>
            ))}
          </ul>
        )}
      </CardBody>
    </Card>
  );
}

function hours(value: number | null) {
  return value === null ? "—" : `${value} h`;
}

function formatDate(value: string) {
  return format(parseISO(value), "d MMM yyyy");
}
