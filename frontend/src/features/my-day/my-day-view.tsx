import { format, parseISO } from "date-fns";
import { Badge } from "../../components/design-system/badge.tsx";
import { Card, CardBody, CardHeader } from "../../components/design-system/card.tsx";
import { Progress } from "../../components/design-system/progress.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { allowedQuickActions } from "./quick-actions.ts";
import type { MyDay } from "./api.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { useShell } from "../../layouts/shell/shell-context.tsx";
import { cn } from "../../utils/cn.ts";
import { ShinyText, SpotlightCard } from "../../components/react-bits/index.ts";

const kindLabels: Record<string, string> = {
  meeting: "Meeting",
  event: "Event",
  deadline: "Deadline",
  reminder: "Reminder",
};

export function MyDayView({ day, name, now }: { day: MyDay; name: string; now: Date }) {
  const hour = now.getHours();
  const hello = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
  const taskGroups = [
    ["Overdue", day.tasks.overdue],
    ["High priority", day.tasks.highPriority],
    ["Today", day.tasks.today],
    ["Upcoming", day.tasks.upcoming],
  ] as const;
  const teamGroups = [
    ["Blocked team tasks", day.team.blocked],
    ["Waiting for review", day.team.waitingForReview],
    ["Needs an owner", day.team.needsOwner],
    ["Recently completed", day.team.recentlyCompleted],
  ] as const;
  const hasTasks = taskGroups.some(([, items]) => items.length > 0);
  const hasTeam = teamGroups.some(([, items]) => items.length > 0);

  return (
    <div className="space-y-6">
      <header>
        <p className="text-xs font-mono font-semibold text-[#4B5563] tracking-wider uppercase"><ShinyText>{format(now, "EEEE, d MMMM yyyy")}</ShinyText></p>
        <h1 className="mt-1.5 wx-headline-lg text-[#111827]">
          {hello}, {name}
        </h1>
        <p className="mt-2 max-w-2xl text-base leading-[24px] text-[#4B5563]">{day.summary}</p>
      </header>

      <section aria-label="Today's summary" className="grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-6">
        <Stat label="Tasks" value={day.counts.tasks} />
        <Stat label="Completed" value={day.counts.completed} tone="text-signal" />
        <Stat label="Overdue" value={day.counts.overdue} tone={day.counts.overdue > 0 ? "text-danger" : undefined} />
        <Stat label="Meetings" value={day.counts.meetings} />
        <Stat label="Reminders" value={day.counts.reminders} />
        <Stat label="Team" value={day.counts.teamAttention} hint="Needs attention" tone={day.counts.teamAttention > 0 ? "text-warning" : undefined} />
      </section>

      <div className="grid items-start gap-5 xl:grid-cols-5">
        <div className="space-y-5 xl:col-span-3">
          <Card>
            <CardHeader title="Today's timeline" description="Events, meetings, deadlines, and reminders." />
            <CardBody>
              {day.timeline.length === 0 ? (
                <Quiet>Nothing is scheduled for today.</Quiet>
              ) : (
                <ol className="space-y-3.5">
                  {day.timeline.map((item) => (
                    <li key={`${item.kind}-${item.id}`} className="grid grid-cols-[4.5rem_1fr] gap-3 items-start">
                      <time className="pt-0.5 text-xs font-medium text-muted tabular-nums" dateTime={item.at}>{formatTime(item.at)}</time>
                      <div className="min-w-0">
                        <div className="flex flex-wrap items-center gap-2">
                          <p className="text-sm font-medium text-white">{item.title}</p>
                          <Badge tone={item.kind === "deadline" ? "warning" : "accent"}>{kindLabels[item.kind] ?? item.kind}</Badge>
                        </div>
                        {item.endAt ? <p className="mt-0.5 text-xs text-muted">Until {formatTime(item.endAt)}</p> : null}
                      </div>
                    </li>
                  ))}
                </ol>
              )}
            </CardBody>
          </Card>

          <Card>
            <CardHeader title="My tasks" description="What is late, urgent, due, or coming up." />
            <CardBody className="space-y-5">
              {hasTasks ? taskGroups.map(([label, items]) => items.length > 0 ? (
                <TaskGroup key={label} label={label} items={items} />
              ) : null) : <Quiet>No tasks need attention today.</Quiet>}
            </CardBody>
          </Card>
        </div>

        <div className="space-y-5 xl:col-span-2">
          <Card>
            <CardHeader title="Meetings" description="Today, and the next one after now." />
            <CardBody className="space-y-3.5">
              {day.meetings.today.length === 0 ? <Quiet>No meetings today.</Quiet> : day.meetings.today.map((meeting) => (
                <MeetingRow key={meeting.id} meeting={meeting} featured={day.meetings.upcoming?.id === meeting.id} />
              ))}
              {day.meetings.upcoming && !day.meetings.today.some((meeting) => meeting.id === day.meetings.upcoming?.id) ? (
                <div>
                  <p className="mb-2 text-[11px] font-semibold tracking-wide text-muted uppercase">Upcoming</p>
                  <MeetingRow meeting={day.meetings.upcoming} featured />
                </div>
              ) : null}
            </CardBody>
          </Card>

          <Card>
            <CardHeader title="Team" description="Blocked work, reviews, and what just finished." />
            <CardBody className="space-y-4">
              {hasTeam ? teamGroups.map(([label, items]) => items.length > 0 ? (
                <div key={label}>
                  <p className="text-[11px] font-semibold tracking-wide text-muted uppercase">{label}</p>
                  <ul className="mt-2 space-y-2">
                    {items.map((item) => (
                      <li key={item.id} className="rounded-xl border border-[rgba(55,65,81,0.4)] bg-[#111113] px-3.5 py-2.5">
                        <p className="text-sm font-medium text-white">{item.title}</p>
                        <p className="mt-0.5 text-xs text-muted">{[item.assigneeName ?? "Unassigned", item.projectName].filter(Boolean).join(" · ")}</p>
                      </li>
                    ))}
                  </ul>
                </div>
              ) : null) : <Quiet>Nothing on your teams needs attention.</Quiet>}
            </CardBody>
          </Card>

          <Card>
            <CardHeader title="Learning" description="The topic you are in the middle of." />
            <CardBody>
              {day.learning ? (
                <div className="space-y-3">
                  <p className="text-sm font-medium text-white">{day.learning.title}</p>
                  <Progress value={day.learning.progress} label="Progress" />
                  <p className="text-xs text-muted"><span className="font-medium text-white">Today's goal: </span>{day.learning.goal}</p>
                </div>
              ) : <Quiet>No learning topic is in progress.</Quiet>}
            </CardBody>
          </Card>
        </div>
      </div>

      <Card>
        <CardHeader title="Projects" description="Active work, progress, deadlines, and risk." />
        <CardBody className="space-y-5">
          {day.projects.active.length === 0 ? <Quiet>No active projects.</Quiet> : (
            <ul className="grid gap-3.5 md:grid-cols-2">
              {day.projects.active.map((project) => (
                <li key={project.id} className="rounded-xl border border-[rgba(55,65,81,0.4)] bg-[#111113] p-3.5">
                  <div className="mb-3 flex items-start justify-between gap-3">
                    <p className="text-sm font-medium text-white">{project.name}</p>
                    {project.atRisk ? <Badge tone="danger">At risk</Badge> : null}
                  </div>
                  <Progress value={project.progress} label="Progress" />
                  <p className="mt-2 text-xs text-muted">{project.deadline ? `Deadline ${formatDay(project.deadline)}` : "No deadline"}</p>
                </li>
              ))}
            </ul>
          )}
          <div className="grid gap-4 sm:grid-cols-2 pt-2 border-t border-[rgba(55,65,81,0.4)]">
            <div>
              <p className="text-[11px] font-semibold tracking-wide text-muted uppercase">Upcoming deadlines</p>
              {day.projects.upcomingDeadlines.length === 0 ? <Quiet>No deadlines in the next two weeks.</Quiet> : (
                <ul className="mt-2 space-y-1.5">
                  {day.projects.upcomingDeadlines.map((project) => (
                    <li key={project.id} className="flex items-center justify-between gap-3 text-sm">
                      <span className="truncate text-white">{project.name}</span>
                      <span className="shrink-0 text-xs text-muted tabular-nums">{project.deadline ? formatDay(project.deadline) : ""}</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
            <div>
              <p className="text-[11px] font-semibold tracking-wide text-muted uppercase">At risk</p>
              {day.projects.atRisk.length === 0 ? <Quiet>No projects are at risk.</Quiet> : (
                <ul className="mt-2 space-y-1.5">
                  {day.projects.atRisk.map((project) => (
                    <li key={project.id} className="flex items-center justify-between gap-3 text-sm">
                      <span className="truncate text-white">{project.name}</span>
                      <span className="shrink-0 text-xs text-danger font-medium">{project.progress}%</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        </CardBody>
      </Card>

      <QuickActions />
    </div>
  );
}

function QuickActions() {
  const { user } = useAuth();
  const { setQuickAddOpen } = useShell();
  const actions = allowedQuickActions(user?.permissions);
  if (actions.length === 0) {
    return null;
  }

  return (
    <Card>
      <CardHeader title="Quick actions" description="Capture something for today." />
      <CardBody>
        <div className="flex flex-wrap gap-2.5">
          {actions.map((action) => (
            <Button key={action.kind} variant="secondary" size="sm" onClick={() => setQuickAddOpen(true, action.kind)}>
              {action.label}
            </Button>
          ))}
        </div>
      </CardBody>
    </Card>
  );
}

function Stat({ label, value, hint, tone }: { label: string; value: number; hint?: string; tone?: string }) {
  return (
    <SpotlightCard className="p-3.5 shadow-xs">
      <p className="text-[10px] font-mono font-semibold tracking-wider text-[#4B5563] uppercase">{label}</p>
      <p className={cn("mt-2 text-2xl font-mono font-semibold tabular-nums text-[#111827]", tone)}>{value}</p>
      {hint ? <p className="mt-1 text-[11px] text-[#4B5563]">{hint}</p> : null}
    </SpotlightCard>
  );
}

function TaskGroup({ label, items }: { label: string; items: MyDay["tasks"]["today"] }) {
  return (
    <div>
      <p className="text-[10px] font-mono font-semibold tracking-wide text-[#4B5563] uppercase">{label}</p>
      <ul className="mt-2 divide-y divide-[#D1CCC2]">
        {items.map((item) => (
          <li key={item.id} className="flex items-start justify-between gap-3 py-2.5">
            <div className="min-w-0">
              <p className="truncate text-sm font-medium text-[#111827]">{item.title}</p>
              <p className="mt-0.5 text-xs text-[#4B5563] font-serif">{[item.kind === "todo" ? "Todo" : item.projectName, item.dueAt ? formatTime(item.dueAt) : null].filter(Boolean).join(" · ")}</p>
            </div>
            <Badge tone={item.priority === "High" ? "danger" : "neutral"}>{item.priority}</Badge>
          </li>
        ))}
      </ul>
    </div>
  );
}

function MeetingRow({ meeting, featured }: { meeting: MyDay["meetings"]["today"][number]; featured?: boolean }) {
  return (
    <article className={featured ? "rounded-[8px] border border-[#111827]/30 bg-[#FED7AA]/40 p-3.5" : "rounded-[8px] border border-[#D1CCC2] bg-[#EBE8E2] p-3.5"}>
      <div className="flex items-start justify-between gap-3">
        <p className="text-sm font-medium text-[#111827]">{meeting.title}</p>
        <time className="shrink-0 text-xs text-[#4B5563] font-mono tabular-nums" dateTime={meeting.startAt}>{formatTime(meeting.startAt)}</time>
      </div>
      <p className="mt-1 text-xs text-[#4B5563] font-serif">{meeting.participants.join(", ") || "No participants"}</p>
      {meeting.projectName ? <p className="mt-1 text-xs text-[#111827] font-mono font-semibold">{meeting.projectName}</p> : null}
    </article>
  );
}

function Quiet({ children }: { children: string }) {
  return <p className="text-xs text-muted">{children}</p>;
}

function formatTime(value: string) {
  return format(parseISO(value), "h:mm a");
}

function formatDay(value: string) {
  const [year, month, day] = value.split("-").map(Number);
  return format(new Date(year ?? 2026, (month ?? 1) - 1, day ?? 1), "d MMM");
}
