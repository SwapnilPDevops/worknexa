import { format, parseISO } from "date-fns";
import { Bar, BarChart, CartesianGrid, Tooltip, XAxis, YAxis } from "recharts";
import { ChartCard } from "../../components/charts/chart-card.tsx";
import { ChartFrame } from "../../components/charts/ChartFrame.tsx";
import { axisLine, axisTick, chartColors, gridStroke, reducedMotion, tooltipStyle } from "../../components/charts/chart-theme.ts";
import type { LearningAnalytics, PersonalAnalytics, ProjectAnalytics, TeamAnalytics } from "./api.ts";

const animate = !reducedMotion;

export function WeeklyProductivityChart({ personal }: { personal: PersonalAnalytics }) {
  const data = personal.weeks.map((week, index) => ({ ...week, label: weekLabel(week.weekStart, index === personal.weeks.length - 1) }));
  const completed = sum(data.map((week) => week.completed));
  const due = sum(data.map((week) => week.due));
  const keptUp = data.filter((week) => week.completed + week.due > 0 && week.completed >= week.due).length;
  const active = data.filter((week) => week.completed + week.due > 0).length;
  const insight = due === 0
    ? `You completed ${plural(completed, "task")} and nothing fell due in this period.`
    : completed >= due
    ? `You completed ${plural(completed, "task")} while ${due} fell due, keeping pace in ${keptUp} of ${active} active weeks.`
    : `${plural(due, "task")} fell due and you completed ${completed}. Clear overdue work before you take on more.`;

  return (
    <ChartCard
      title="Weekly productivity"
      question="Are you finishing work as fast as it falls due?"
      insight={insight}
      empty={completed + due === 0 ? "No tasks were completed or fell due in this period." : undefined}
      legend={[{ label: "Completed", color: chartColors.accent }, { label: "Fell due", color: chartColors.muted }]}
      rows={data}
      getRowKey={(row) => row.weekStart}
      columns={[
        { key: "week", header: "Week", render: (row) => row.label },
        { key: "completed", header: "Completed", render: (row) => row.completed },
        { key: "due", header: "Fell due", render: (row) => row.due },
      ]}
    >
      <ChartFrame>
        <BarChart data={data} accessibilityLayer={false} margin={{ top: 4, right: 4, bottom: 0, left: -20 }}>
          <CartesianGrid stroke={gridStroke} strokeDasharray="3 3" vertical={false} />
          <XAxis dataKey="label" tick={axisTick} tickLine={false} axisLine={axisLine} />
          <YAxis allowDecimals={false} tick={axisTick} tickLine={false} axisLine={false} />
          <Tooltip {...tooltipStyle} />
          <Bar dataKey="completed" name="Completed" fill={chartColors.accent} radius={[4, 4, 0, 0]} maxBarSize={22} isAnimationActive={animate} />
          <Bar dataKey="due" name="Fell due" fill={chartColors.muted} fillOpacity={0.4} radius={[4, 4, 0, 0]} maxBarSize={22} isAnimationActive={animate} />
        </BarChart>
      </ChartFrame>
    </ChartCard>
  );
}

export function TimeAllocationChart({ personal }: { personal: PersonalAnalytics }) {
  const data = personal.weeks.map((week, index) => ({
    label: weekLabel(week.weekStart, index === personal.weeks.length - 1),
    weekStart: week.weekStart,
    meeting: week.meetingHours ?? 0,
    focus: week.focusHours ?? 0,
    learning: week.learningHours ?? 0,
  }));
  const meeting = personal.meetingHours ?? 0;
  const focus = personal.focusHours ?? 0;
  const learning = personal.learningHours ?? 0;
  const scheduled = meeting + focus + learning;
  const share = scheduled === 0 ? 0 : Math.round((meeting / scheduled) * 100);
  const perWeek = round(focus / Math.max(1, data.length));
  const insight = `Meetings took ${share}% of your scheduled time and focus blocks averaged ${perWeek} h a week. ${
    meeting > focus ? "Meetings outweigh focus time, so protect a few focus blocks next week." : "Focus time leads, which leaves room for deep work."
  }`;

  return (
    <ChartCard
      title="Time allocation"
      question="Is meeting load crowding out focus and learning?"
      insight={insight}
      empty={scheduled === 0 ? "Nothing with a duration is on your calendar for this period." : undefined}
      legend={[
        { label: "Meetings", color: chartColors.warning },
        { label: "Focus", color: chartColors.accent },
        { label: "Learning", color: chartColors.signal },
      ]}
      note="Focus time counts calendar blocks marked Task or Personal. All-day items are left out."
      rows={data}
      getRowKey={(row) => row.weekStart}
      columns={[
        { key: "week", header: "Week", render: (row) => row.label },
        { key: "meeting", header: "Meetings", render: (row) => `${row.meeting} h` },
        { key: "focus", header: "Focus", render: (row) => `${row.focus} h` },
        { key: "learning", header: "Learning", render: (row) => `${row.learning} h` },
      ]}
    >
      <ChartFrame>
        <BarChart data={data} accessibilityLayer={false} margin={{ top: 4, right: 4, bottom: 0, left: -20 }}>
          <CartesianGrid stroke={gridStroke} strokeDasharray="3 3" vertical={false} />
          <XAxis dataKey="label" tick={axisTick} tickLine={false} axisLine={axisLine} />
          <YAxis tick={axisTick} tickLine={false} axisLine={false} unit=" h" />
          <Tooltip {...tooltipStyle} formatter={(value) => `${String(value)} h`} />
          <Bar dataKey="meeting" name="Meetings" stackId="time" fill={chartColors.warning} maxBarSize={28} isAnimationActive={animate} />
          <Bar dataKey="focus" name="Focus" stackId="time" fill={chartColors.accent} maxBarSize={28} isAnimationActive={animate} />
          <Bar dataKey="learning" name="Learning" stackId="time" fill={chartColors.signal} radius={[4, 4, 0, 0]} maxBarSize={28} isAnimationActive={animate} />
        </BarChart>
      </ChartFrame>
    </ChartCard>
  );
}

export function TeamWorkloadChart({ team }: { team: TeamAnalytics }) {
  const data = team.members
    .map((member) => ({ ...member, open: member.assigned - member.completed }))
    .sort((left, right) => right.open - left.open || left.name.localeCompare(right.name));
  const busiest = data[0];
  const blockedPeople = data.filter((member) => member.blocked > 0).length;
  const overduePeople = data.filter((member) => member.overdue > 0).length;
  const insight = busiest && busiest.open > 0
    ? `${busiest.name} carries the most open work (${plural(busiest.open, "task")}). ${
      blockedPeople > 0 ? `${plural(blockedPeople, "person", "people")} ${blockedPeople === 1 ? "has" : "have"} blocked tasks to clear.` : "Nobody is blocked."
    } ${overduePeople > 0 ? `${plural(overduePeople, "person", "people")} ${overduePeople === 1 ? "is" : "are"} behind on due dates.` : ""}`.trim()
    : "Nobody on this team has open work.";

  return (
    <ChartCard
      title="Workload per member"
      question="Who is overloaded, blocked, or behind?"
      insight={insight}
      empty={data.length === 0 ? "This team has no members yet." : undefined}
      legend={[
        { label: "Open", color: chartColors.accent },
        { label: "Blocked", color: chartColors.danger },
        { label: "Overdue", color: chartColors.warning },
      ]}
      note="Blocked and overdue tasks are also counted as open."
      rows={data}
      getRowKey={(row) => row.userId}
      columns={[
        { key: "name", header: "Member", render: (row) => row.name },
        { key: "open", header: "Open", render: (row) => row.open },
        { key: "blocked", header: "Blocked", render: (row) => row.blocked },
        { key: "overdue", header: "Overdue", render: (row) => row.overdue },
        { key: "completed", header: "Completed", render: (row) => row.completed },
      ]}
    >
      <ChartFrame height={Math.max(160, data.length * 52 + 32)}>
        <BarChart data={data} layout="vertical" accessibilityLayer={false} barGap={2} margin={{ top: 0, right: 8, bottom: 0, left: 0 }}>
          <CartesianGrid stroke={gridStroke} strokeDasharray="3 3" horizontal={false} />
          <XAxis type="number" allowDecimals={false} tick={axisTick} tickLine={false} axisLine={axisLine} />
          <YAxis type="category" dataKey="name" width={120} tick={axisTick} tickLine={false} axisLine={false} tickFormatter={(value: string) => truncate(value, 16)} />
          <Tooltip {...tooltipStyle} />
          <Bar dataKey="open" name="Open" fill={chartColors.accent} radius={[0, 4, 4, 0]} maxBarSize={12} isAnimationActive={animate} />
          <Bar dataKey="blocked" name="Blocked" fill={chartColors.danger} radius={[0, 4, 4, 0]} maxBarSize={12} isAnimationActive={animate} />
          <Bar dataKey="overdue" name="Overdue" fill={chartColors.warning} radius={[0, 4, 4, 0]} maxBarSize={12} isAnimationActive={animate} />
        </BarChart>
      </ChartFrame>
    </ChartCard>
  );
}

export function ProjectScheduleChart({ projects }: { projects: ProjectAnalytics }) {
  const data = projects.projects;
  const behind = data.filter((project) => project.elapsed !== null && project.elapsed - project.progress >= 10);
  const worst = behind[0];
  const dated = data.filter((project) => project.elapsed !== null).length;
  const insight = worst
    ? `${plural(behind.length, "project")} ${behind.length === 1 ? "is" : "are"} behind schedule. ${worst.name} has used ${worst.elapsed}% of its time for ${worst.progress}% of the work.`
    : dated > 0
      ? "Every project with dates is keeping pace with its schedule."
      : "Add start and target dates to projects to compare progress against time.";
  const more = projects.activeProjects - data.length;
  const started = data.some((project) => project.progress > 0 || (project.elapsed ?? 0) > 0);

  return (
    <ChartCard
      title="Project progress"
      question="Which projects are falling behind their schedule?"
      insight={insight}
      empty={data.length === 0
        ? "There are no active projects."
        : started ? undefined : `None of the ${plural(projects.activeProjects, "active project")} has completed work or started its schedule yet.`}
      legend={[{ label: "Work done", color: chartColors.accent }, { label: "Time used", color: chartColors.muted }]}
      note={`Most at-risk first.${more > 0 ? ` ${more} more active ${more === 1 ? "project is" : "projects are"} on track.` : ""} Projects without start and target dates show work done only.`}
      rows={data}
      getRowKey={(row) => row.id}
      columns={[
        { key: "name", header: "Project", render: (row) => row.name },
        { key: "progress", header: "Work done", render: (row) => `${row.progress}%` },
        { key: "elapsed", header: "Time used", render: (row) => row.elapsed === null ? "No dates" : `${row.elapsed}%` },
        { key: "open", header: "Open tasks", render: (row) => row.openTasks },
        { key: "blocked", header: "Blocked", render: (row) => row.blockedTasks },
      ]}
    >
      <ChartFrame height={Math.max(160, data.length * 52 + 32)}>
        <BarChart data={data} layout="vertical" accessibilityLayer={false} barGap={2} margin={{ top: 0, right: 8, bottom: 0, left: 0 }}>
          <CartesianGrid stroke={gridStroke} strokeDasharray="3 3" horizontal={false} />
          <XAxis type="number" domain={[0, 100]} unit="%" tick={axisTick} tickLine={false} axisLine={axisLine} />
          <YAxis type="category" dataKey="name" width={120} tick={axisTick} tickLine={false} axisLine={false} tickFormatter={(value: string) => truncate(value, 16)} />
          <Tooltip {...tooltipStyle} formatter={(value) => value === null || value === undefined ? "No dates" : `${String(value)}%`} />
          <Bar dataKey="progress" name="Work done" fill={chartColors.accent} radius={[0, 4, 4, 0]} maxBarSize={12} isAnimationActive={animate} />
          <Bar dataKey="elapsed" name="Time used" fill={chartColors.muted} fillOpacity={0.4} radius={[0, 4, 4, 0]} maxBarSize={12} isAnimationActive={animate} />
        </BarChart>
      </ChartFrame>
    </ChartCard>
  );
}

export function LearningConsistencyChart({ learning }: { learning: LearningAnalytics }) {
  const data = learning.weeks.map((week, index) => ({ ...week, label: weekLabel(week.weekStart, index === learning.weeks.length - 1) }));
  const activeWeeks = data.filter((week) => week.activeDays > 0).length;
  const thisWeek = data.at(-1)?.activeDays ?? 0;
  const insight = `You studied on ${learning.activeDays} of the last ${learning.periodDays} days, in ${activeWeeks} of ${data.length} weeks. This week so far: ${plural(thisWeek, "day")}.`;

  return (
    <ChartCard
      title="Learning consistency"
      question="Is learning a steady habit or an occasional burst?"
      insight={insight}
      empty={learning.activeDays === 0 ? "No learning activity in this period. Update a topic, write a learning note, or block learning time on the calendar." : undefined}
      note="A day counts when you update a topic, write a learning note, or have learning time on the calendar."
      rows={data}
      getRowKey={(row) => row.weekStart}
      columns={[
        { key: "week", header: "Week", render: (row) => row.label },
        { key: "days", header: "Active days", render: (row) => row.activeDays },
      ]}
    >
      <ChartFrame height={200}>
        <BarChart data={data} accessibilityLayer={false} margin={{ top: 4, right: 4, bottom: 0, left: -20 }}>
          <CartesianGrid stroke={gridStroke} strokeDasharray="3 3" vertical={false} />
          <XAxis dataKey="label" tick={axisTick} tickLine={false} axisLine={axisLine} />
          <YAxis domain={[0, 7]} ticks={[0, 2, 4, 6]} allowDecimals={false} tick={axisTick} tickLine={false} axisLine={false} />
          <Tooltip {...tooltipStyle} />
          <Bar dataKey="activeDays" name="Active days" fill={chartColors.signal} radius={[4, 4, 0, 0]} maxBarSize={28} isAnimationActive={animate} />
        </BarChart>
      </ChartFrame>
    </ChartCard>
  );
}

function weekLabel(weekStart: string, current: boolean) {
  return current ? "This week" : format(parseISO(weekStart), "d MMM");
}

function sum(values: number[]) {
  return values.reduce((total, value) => total + value, 0);
}

function round(value: number) {
  return Math.round(value * 10) / 10;
}

function plural(count: number, singular: string, many = `${singular}s`) {
  return `${count} ${count === 1 ? singular : many}`;
}

function truncate(value: string, length: number) {
  return value.length > length ? `${value.slice(0, length - 1)}…` : value;
}
