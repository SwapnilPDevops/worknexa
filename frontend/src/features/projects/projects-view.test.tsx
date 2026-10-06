import { screen, waitFor, within } from "@testing-library/react";
import { Route, Routes } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { RequireAuth } from "../../app/require-auth.tsx";
import { fail } from "../../test/api.ts";
import { adminUser, memberUser, renderWithProviders } from "../../test/render.tsx";
import { taskDetailSchema, taskListItemSchema, type TaskListItem } from "../work/api.ts";
import { projectDetailSchema, projectListSchema, projectOptionsSchema, type ProjectDetail, type ProjectListItem } from "./api.ts";
import { ProjectsView } from "./projects-view.tsx";

const grace = { id: "33333333-3333-3333-3333-333333333333", name: "Grace Hopper" };

const projectOptions = projectOptionsSchema.parse({
  people: [{ id: adminUser.id, name: adminUser.displayName }, grace],
  teams: [{ id: "team-core", name: "Core" }],
});

function projectDetail(overrides: Partial<ProjectDetail> = {}): ProjectDetail {
  return projectDetailSchema.parse({
    id: "project-apollo",
    name: "Apollo",
    code: "APL",
    description: null,
    ownerId: adminUser.id,
    ownerName: adminUser.displayName,
    teamId: null,
    teamName: null,
    status: "Active",
    priority: "Medium",
    startDate: null,
    targetDate: null,
    technology: null,
    repositoryUrl: null,
    documentationUrl: null,
    members: [{ userId: adminUser.id, name: adminUser.displayName, role: "Owner" }],
    dashboard: { progress: 40, openTasks: 3, completedTasks: 2, blockedTasks: 0, upcomingDeadlines: [], teamWorkload: [] },
    ...overrides,
  });
}

function listItem(project: ProjectDetail): ProjectListItem {
  return projectListSchema.parse({ ...project, progress: project.dashboard.progress, openTasks: project.dashboard.openTasks });
}

type Options = NonNullable<Parameters<typeof renderWithProviders>[1]>;

/** mockApi's Handler type collapses to `unknown`, so handler arguments need an explicit type. */
type MockRequest = { body: unknown };

function renderProjects(options: Options = {}) {
  return renderWithProviders(
    <Routes>
      <Route element={<RequireAuth />}>
        <Route path="/projects" element={<ProjectsView />} />
      </Route>
    </Routes>,
    { route: "/projects", ...options },
  );
}

async function openComposer(user: ReturnType<typeof renderProjects>["user"]) {
  await user.click(await screen.findByRole("button", { name: "New" }));
  return screen.getByRole("dialog", { name: "New project" });
}

describe("ProjectsView: create project", () => {
  it("blocks submission until name and code are filled in", async () => {
    const { user, api } = renderProjects({ api: { "GET /api/projects": [], "GET /api/projects/options": projectOptions } });

    const dialog = await openComposer(user);
    const name = within(dialog).getByLabelText("Name");
    const code = within(dialog).getByLabelText("Code");

    await user.click(within(dialog).getByRole("button", { name: "Save" }));
    expect(name).toBeRequired();
    expect(name).toBeInvalid();
    expect(code).toBeInvalid();

    await user.type(name, "Orion");
    await user.click(within(dialog).getByRole("button", { name: "Save" }));
    expect(name).toBeValid();
    expect(code).toBeInvalid();

    expect(api.sent("POST", "/api/projects")).toHaveLength(0);
    expect(screen.getByRole("dialog", { name: "New project" })).toBeInTheDocument();
  });

  it("sends the project, closes the drawer and opens the new project", async () => {
    const projects: ProjectListItem[] = [listItem(projectDetail())];
    let created: ProjectDetail | null = null;
    const { user, api } = renderProjects({
      api: {
        "GET /api/projects": () => projects,
        "GET /api/projects/options": projectOptions,
        "POST /api/projects": ({ body }: MockRequest) => {
          const write = body as { name: string; code: string; description: string; status: string; priority: string; technology: string };
          created = projectDetail({
            id: "project-orion",
            name: write.name,
            code: write.code,
            description: write.description,
            ownerId: grace.id,
            ownerName: grace.name,
            teamId: "team-core",
            teamName: "Core",
            status: write.status,
            priority: write.priority,
            technology: write.technology,
          });
          projects.push(listItem(created));
          return created;
        },
        "GET /api/projects/:id": () => created,
      },
    });

    expect(await screen.findByRole("button", { name: /Apollo/ })).toBeInTheDocument();
    const dialog = await openComposer(user);
    expect(screen.getByTestId("location")).toHaveTextContent("/projects?compose=1");
    expect(within(dialog).getByLabelText("Owner")).toHaveValue(adminUser.id);

    await user.type(within(dialog).getByLabelText("Name"), "  Orion ");
    await user.type(within(dialog).getByLabelText("Code"), "ORN");
    await user.type(within(dialog).getByLabelText("Description"), "Next-gen navigation");
    await user.selectOptions(within(dialog).getByLabelText("Owner"), grace.name);
    await user.selectOptions(within(dialog).getByLabelText("Team"), "Core");
    await user.selectOptions(within(dialog).getByLabelText("Status"), "Active");
    await user.selectOptions(within(dialog).getByLabelText("Priority"), "High");
    await user.type(within(dialog).getByLabelText("Technology"), "React");
    await user.click(within(dialog).getByRole("button", { name: "Save" }));

    expect(await screen.findByRole("heading", { level: 2, name: "Orion" })).toBeInTheDocument();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(screen.getByTestId("location")).toHaveTextContent("/projects?project=project-orion&tab=overview");
    expect(await screen.findByRole("button", { name: /Orion.*ORN/ })).toBeInTheDocument();

    const [request] = api.sent("POST", "/api/projects");
    expect(request.body).toEqual({
      name: "Orion",
      code: "ORN",
      description: "Next-gen navigation",
      ownerId: grace.id,
      teamId: "team-core",
      status: "Active",
      priority: "High",
      startDate: null,
      targetDate: null,
      technology: "React",
      repositoryUrl: null,
      documentationUrl: null,
    });
    expect(api.sent("GET", "/api/projects").length).toBeGreaterThanOrEqual(2);
    expect(api.unhandled).toEqual([]);
  });

  it("defaults the owner to the signed-in user and the status to Planning", async () => {
    const saved = projectDetail({ id: "project-vega", name: "Vega", code: "VGA", status: "Planning" });
    const { user, api } = renderProjects({
      api: {
        "GET /api/projects": [],
        "GET /api/projects/options": projectOptions,
        "POST /api/projects": saved,
        "GET /api/projects/:id": saved,
      },
    });

    const dialog = await openComposer(user);
    await user.type(within(dialog).getByLabelText("Name"), "Vega");
    await user.type(within(dialog).getByLabelText("Code"), "VGA");
    await user.click(within(dialog).getByRole("button", { name: "Save" }));

    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(api.sent("POST", "/api/projects")[0].body).toMatchObject({
      name: "Vega",
      code: "VGA",
      description: null,
      ownerId: adminUser.id,
      teamId: null,
      status: "Planning",
      priority: "Medium",
    });
  });

  it("shows the server error and keeps the drawer open when the create fails", async () => {
    const { user, api } = renderProjects({
      api: {
        "GET /api/projects": [],
        "GET /api/projects/options": projectOptions,
        "POST /api/projects": fail(400, "Project code ORN is already in use.", "VALIDATION_FAILED"),
      },
    });

    const dialog = await openComposer(user);
    await user.type(within(dialog).getByLabelText("Name"), "Orion");
    await user.type(within(dialog).getByLabelText("Code"), "ORN");
    await user.click(within(dialog).getByRole("button", { name: "Save" }));

    expect(await within(dialog).findByText("Project code ORN is already in use.")).toBeInTheDocument();
    expect(screen.getByRole("dialog", { name: "New project" })).toBeInTheDocument();
    expect(within(dialog).getByLabelText("Name")).toHaveValue("Orion");
    expect(within(dialog).getByRole("button", { name: "Save" })).toBeEnabled();
    expect(screen.getByTestId("location")).toHaveTextContent("/projects?compose=1");
    expect(api.sent("POST", "/api/projects")).toHaveLength(1);
  });

  it("surfaces a permission error from the server", async () => {
    const { user } = renderProjects({
      api: {
        "GET /api/projects": [],
        "GET /api/projects/options": projectOptions,
        "POST /api/projects": fail(403, "You do not have permission to manage projects.", "FORBIDDEN"),
      },
    });

    const dialog = await openComposer(user);
    await user.type(within(dialog).getByLabelText("Name"), "Orion");
    await user.type(within(dialog).getByLabelText("Code"), "ORN");
    await user.click(within(dialog).getByRole("button", { name: "Save" }));

    expect(await within(dialog).findByText("You do not have permission to manage projects.")).toBeInTheDocument();
    expect(screen.getByRole("dialog", { name: "New project" })).toBeInTheDocument();
  });

  it("closes the drawer without saving", async () => {
    const { user, api } = renderProjects({ api: { "GET /api/projects": [], "GET /api/projects/options": projectOptions } });

    const dialog = await openComposer(user);
    await user.type(within(dialog).getByLabelText("Name"), "Draft");
    await user.click(within(dialog).getByRole("button", { name: "Close panel" }));

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(screen.getByTestId("location").textContent).toBe("/projects");
    expect(api.sent("POST", "/api/projects")).toHaveLength(0);
  });
});

describe("ProjectsView: create task inside a project", () => {
  const project = projectDetail({ teamId: "team-core", teamName: "Core" });

  function taskRow(id: string, title: string): TaskListItem {
    return taskListItemSchema.parse({
      id,
      title,
      status: "Todo",
      priority: "Medium",
      startAt: null,
      dueAt: null,
      assigneeId: null,
      assigneeName: null,
      projectId: project.id,
      projectName: project.name,
      parentTaskId: null,
      teamId: project.teamId,
      estimatedHours: null,
      actualHours: null,
      tags: [],
    });
  }

  it("ignores an empty title and creates a project task with the project and team ids", async () => {
    const rows: TaskListItem[] = [];
    const { user, api } = renderProjects({
      route: `/projects?project=${project.id}&tab=tasks`,
      api: {
        "GET /api/projects": [listItem(project)],
        "GET /api/projects/options": projectOptions,
        "GET /api/projects/:id": project,
        "GET /api/tasks": () => rows,
        "POST /api/tasks": ({ body }: MockRequest) => {
          const row = taskRow("task-new", (body as { title: string }).title);
          rows.push(row);
          return taskDetailSchema.parse({
            ...row,
            description: null,
            parentTitle: null,
            subtasks: [],
            dependencies: [],
            comments: [],
            attachments: [],
            meetingId: null,
            meetingTitle: null,
          });
        },
      },
    });

    expect(await screen.findByRole("heading", { name: "No tasks" })).toBeInTheDocument();
    expect(api.sent("GET", "/api/tasks")[0].query.get("projectId")).toBe(project.id);

    await user.click(screen.getByRole("button", { name: "Add task" }));
    expect(api.sent("POST", "/api/tasks")).toHaveLength(0);

    const input = screen.getByRole("textbox", { name: "New task title" });
    await user.type(input, " Draft the launch plan ");
    await user.click(screen.getByRole("button", { name: "Add task" }));

    expect(await screen.findByRole("button", { name: /Draft the launch plan/ })).toBeInTheDocument();
    expect(input).toHaveValue("");
    expect(api.sent("POST", "/api/tasks")[0].body).toEqual({
      title: "Draft the launch plan",
      description: null,
      status: "Todo",
      priority: "Medium",
      startAt: null,
      dueAt: null,
      assigneeId: null,
      projectId: project.id,
      parentTaskId: null,
      teamId: "team-core",
      estimatedHours: null,
      actualHours: null,
      tags: [],
    });
    expect(api.unhandled).toEqual([]);
  });

  it("shows the server error when the project task cannot be created", async () => {
    const { user } = renderProjects({
      route: `/projects?project=${project.id}&tab=tasks`,
      api: {
        "GET /api/projects": [listItem(project)],
        "GET /api/projects/options": projectOptions,
        "GET /api/projects/:id": project,
        "GET /api/tasks": [],
        "POST /api/tasks": fail(400, "Title is required.", "VALIDATION_FAILED"),
      },
    });

    await user.type(await screen.findByRole("textbox", { name: "New task title" }), "Launch");
    await user.click(screen.getByRole("button", { name: "Add task" }));

    expect(await screen.findByText("Title is required.")).toBeInTheDocument();
    expect(screen.getByRole("textbox", { name: "New task title" })).toHaveValue("Launch");
  });
});

describe("ProjectsView: list states", () => {
  it("shows the empty list and the choose-a-project placeholder", async () => {
    const { api } = renderProjects({ api: { "GET /api/projects": [], "GET /api/projects/options": projectOptions } });

    expect(await screen.findByText("No projects yet.")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Choose a project" })).toBeInTheDocument();
    expect(api.unhandled).toEqual([]);
  });

  it("shows the error state when the list fails, without a retry action", async () => {
    renderProjects({
      api: { "GET /api/projects": fail(500, "The project store is offline.", "SERVER_ERROR"), "GET /api/projects/options": projectOptions },
    });

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent("Projects are unavailable");
    expect(alert).toHaveTextContent("The project store is offline.");
    expect(within(alert).queryByRole("button", { name: "Try again" })).not.toBeInTheDocument();
  });

  it("refetches the list when the search changes after an error", async () => {
    const { user, api } = renderProjects({
      api: { "GET /api/projects": fail(500, "The project store is offline.", "SERVER_ERROR"), "GET /api/projects/options": projectOptions },
    });

    await screen.findByRole("alert");
    api.set({ "GET /api/projects": [listItem(projectDetail())] });
    await user.type(screen.getByRole("textbox", { name: "Search projects" }), "A");

    expect(await screen.findByRole("button", { name: /Apollo/ })).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    expect(api.sent("GET", "/api/projects").at(-1)?.query.get("q")).toBe("A");
  });
});

describe("ProjectsView: authorization", () => {
  it("hides the create control and skips the options lookup for members", async () => {
    const project = projectDetail();
    const { api } = renderProjects({
      user: memberUser,
      route: `/projects?project=${project.id}`,
      api: { "GET /api/projects": [listItem(project)], "GET /api/projects/:id": project },
    });

    expect(await screen.findByRole("heading", { level: 2, name: "Apollo" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "New" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Edit" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
    expect(api.sent("GET", "/api/projects/options")).toHaveLength(0);
    expect(api.unhandled).toEqual([]);
  });

  it("shows edit and delete controls to managers", async () => {
    const project = projectDetail();
    renderProjects({
      route: `/projects?project=${project.id}`,
      api: { "GET /api/projects": [listItem(project)], "GET /api/projects/options": projectOptions, "GET /api/projects/:id": project },
    });

    expect(await screen.findByRole("heading", { level: 2, name: "Apollo" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "New" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Edit" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Delete" })).toBeInTheDocument();
  });

  it("hides the project task form from members without tasks.manage", async () => {
    const project = projectDetail();
    renderProjects({
      user: memberUser,
      route: `/projects?project=${project.id}&tab=tasks`,
      api: { "GET /api/projects": [listItem(project)], "GET /api/projects/:id": project, "GET /api/tasks": [] },
    });

    expect(await screen.findByRole("heading", { name: "No tasks" })).toBeInTheDocument();
    expect(screen.queryByRole("textbox", { name: "New task title" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Add task" })).not.toBeInTheDocument();
  });
});
