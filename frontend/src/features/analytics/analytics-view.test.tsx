import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { adminUser, renderWithProviders } from "../../test/render.tsx";
import type { Analytics } from "./api.ts";
import { AnalyticsView } from "./analytics-view.tsx";

const mockAnalyticsReport: Analytics = {
  from: "2026-08-17T00:00:00Z",
  to: "2026-10-08T00:00:00Z",
  weeks: 8,
  personal: {
    tasksCompleted: 1,
    tasksOverdue: 0,
    dueInPeriod: 1,
    completionRate: 100,
    meetingHours: 0.8,
    focusHours: 0,
    learningHours: 0,
    weeks: [
      {
        weekStart: "2026-08-17T00:00:00Z",
        completed: 0,
        due: 0,
        meetingHours: 0.2,
        focusHours: 0,
        learningHours: 0,
      },
      {
        weekStart: "2026-10-05T00:00:00Z",
        completed: 1,
        due: 1,
        meetingHours: 0.6,
        focusHours: 0,
        learningHours: 0,
      },
    ],
  },
  team: {
    teamId: "team-1",
    teams: [{ id: "team-1", name: "Platform" }],
    assigned: 2,
    completed: 0,
    overdue: 1,
    blocked: 1,
    members: [
      {
        userId: "u1",
        name: "Worknexa Member",
        assigned: 2,
        inProgress: 1,
        completed: 0,
        overdue: 1,
        blocked: 1,
      },
      {
        userId: "u2",
        name: "Worknexa Admin",
        assigned: 1,
        inProgress: 1,
        completed: 0,
        overdue: 0,
        blocked: 0,
      },
    ],
  },
  projects: {
    activeProjects: 51,
    completedTasks: 40,
    countedTasks: 100,
    completionRate: 40,
    blockedTasks: 2,
    projects: [
      {
        id: "p1",
        name: "Atlas 1363dd API documentation",
        status: "At Risk",
        progress: 14,
        elapsed: 20,
        openTasks: 9,
        blockedTasks: 1,
        targetDate: "2026-10-09T00:00:00Z",
      },
    ],
    blocked: [],
    deadlines: [],
  },
  learning: null,
};

describe("AnalyticsView layout", () => {
  it("renders header with date range and period toggle tabs", async () => {
    const onWeeksChange = vi.fn();
    const onTeamChange = vi.fn();

    renderWithProviders(
      <AnalyticsView
        report={mockAnalyticsReport}
        weeks={8}
        onWeeksChange={onWeeksChange}
        onTeamChange={onTeamChange}
        refreshing={false}
      />,
      { user: adminUser }
    );

    expect(screen.getByRole("heading", { name: "Analytics" })).toBeInTheDocument();
    expect(screen.getByText(/How your work, team, projects, and learning have trended/)).toBeInTheDocument();

    const fourWeeksBtn = screen.getByRole("button", { name: "4 weeks" });
    const eightWeeksBtn = screen.getByRole("button", { name: "8 weeks" });
    const twelveWeeksBtn = screen.getByRole("button", { name: "12 weeks" });

    expect(fourWeeksBtn).toBeInTheDocument();
    expect(eightWeeksBtn).toBeInTheDocument();
    expect(twelveWeeksBtn).toBeInTheDocument();
    expect(eightWeeksBtn).toHaveAttribute("aria-pressed", "true");

    await userEvent.click(fourWeeksBtn);
    expect(onWeeksChange).toHaveBeenCalledWith(4);
  });

  it("renders Personal section with all 6 KPI stat cards and charts", () => {
    renderWithProviders(
      <AnalyticsView
        report={mockAnalyticsReport}
        weeks={8}
        onWeeksChange={vi.fn()}
        onTeamChange={vi.fn()}
        refreshing={false}
      />,
      { user: adminUser }
    );

    expect(screen.getByRole("heading", { name: "Personal" })).toBeInTheDocument();
    expect(screen.getAllByText("Tasks completed").length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText("Tasks overdue")).toBeInTheDocument();
    expect(screen.getByText("Completion rate")).toBeInTheDocument();
    expect(screen.getByText("Meeting hours")).toBeInTheDocument();
    expect(screen.getByText("Focus time")).toBeInTheDocument();
    expect(screen.getByText("Learning hours")).toBeInTheDocument();

    // Chart titles
    expect(screen.getByRole("heading", { name: "Weekly productivity" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Time allocation" })).toBeInTheDocument();
  });

  it("renders Team section with 4 KPI stat cards, workload chart, and team overview", () => {
    renderWithProviders(
      <AnalyticsView
        report={mockAnalyticsReport}
        weeks={8}
        onWeeksChange={vi.fn()}
        onTeamChange={vi.fn()}
        refreshing={false}
      />,
      { user: adminUser }
    );

    expect(screen.getByRole("heading", { name: "Team" })).toBeInTheDocument();
    expect(screen.getByText("Tasks assigned")).toBeInTheDocument();
    expect(screen.getByText("Overdue tasks")).toBeInTheDocument();
    expect(screen.getByText("Blocked tasks")).toBeInTheDocument();

    expect(screen.getByRole("heading", { name: "Workload per member" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Team overview" })).toBeInTheDocument();
    expect(screen.getByText("Worknexa Member")).toBeInTheDocument();
    expect(screen.getByText("Worknexa Admin")).toBeInTheDocument();
  });

  it("renders Projects section with filter tabs and projects table", () => {
    renderWithProviders(
      <AnalyticsView
        report={mockAnalyticsReport}
        weeks={8}
        onWeeksChange={vi.fn()}
        onTeamChange={vi.fn()}
        refreshing={false}
      />,
      { user: adminUser }
    );

    expect(screen.getByRole("heading", { name: "Projects" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /All Projects/ })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /On Track/ })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /At Risk/ })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Overdue/ })).toBeInTheDocument();

    // Table column headers
    expect(screen.getByRole("columnheader", { name: "Project" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Status" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Progress" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Tasks" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Start Date" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "End Date" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Owner" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Priority" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Last Update" })).toBeInTheDocument();
  });
});
