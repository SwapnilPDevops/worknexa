import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { Route, Routes } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { RequireAuth } from "../../app/require-auth.tsx";
import { permissions } from "../../auth/access.ts";
import { ApiError } from "../../services/api-client.ts";
import { fail } from "../../test/api.ts";
import { adminUser, memberUser, renderWithProviders } from "../../test/render.tsx";
import { taskDetailSchema, taskListItemSchema, workOptionsSchema, type TaskDetail, type TaskListItem } from "../work/api.ts";
import type { TaskView } from "../work/catalog.ts";
import { TasksView } from "./tasks-view.tsx";

const grace = { id: "33333333-3333-3333-3333-333333333333", name: "Grace Hopper" };

const workOptions = workOptionsSchema.parse({
  projects: [{ id: "project-apollo", name: "Apollo" }],
  people: [{ id: adminUser.id, name: adminUser.displayName }, grace],
  teams: [{ id: "team-core", name: "Core" }],
  tags: ["backend"],
});

function taskDetail(overrides: Partial<TaskDetail> = {}): TaskDetail {
  return taskDetailSchema.parse({
    id: "task-1",
    title: "Write release notes",
    status: "Todo",
    priority: "Medium",
    startAt: null,
    dueAt: null,
    assigneeId: null,
    assigneeName: null,
    projectId: null,
    projectName: null,
    parentTaskId: null,
    teamId: null,
    estimatedHours: null,
    actualHours: null,
    tags: [],
    description: null,
    parentTitle: null,
    subtasks: [],
    dependencies: [],
    comments: [],
    attachments: [],
    meetingId: null,
    meetingTitle: null,
    ...overrides,
  });
}

function listItem(task: TaskDetail): TaskListItem {
  return taskListItemSchema.parse(task);
}

type Options = NonNullable<Parameters<typeof renderWithProviders>[1]>;

/** mockApi's Handler type collapses to `unknown`, so handler arguments need an explicit type. */
type MockRequest = { body: unknown; query: URLSearchParams };

function renderTasks(options: Options & { view?: TaskView } = {}) {
  const { view = "list", ...rest } = options;
  return renderWithProviders(
    <Routes>
      <Route element={<RequireAuth />}>
        <Route path="/tasks" element={<TasksView initialView={view} />} />
      </Route>
    </Routes>,
    { route: "/tasks", ...rest },
  );
}

/** A single task whose server-side state can change through the mocked endpoints. */
function taskServer(initial: TaskDetail) {
  const state = { task: initial };
  const routes = {
    "GET /api/tasks": () => [listItem(state.task)],
    "GET /api/work/options": workOptions,
    "GET /api/tasks/:id": () => state.task,
  };
  return { state, routes };
}

async function openTask(user: ReturnType<typeof renderTasks>["user"], title: string) {
  const table = await screen.findByRole("table", { name: "Tasks" });
  await user.click(within(table).getByRole("button", { name: title }));
  const dialog = await screen.findByRole("dialog", { name: title });
  await within(dialog).findByRole("heading", { name: "Comments" });
  return dialog;
}

type RejectionListener = (reason: unknown, promise: Promise<unknown>) => void;
type NodeProcess = {
  listeners(event: "unhandledRejection"): RejectionListener[];
  removeAllListeners(event: "unhandledRejection"): void;
  on(event: "unhandledRejection", listener: RejectionListener): void;
  off(event: "unhandledRejection", listener: RejectionListener): void;
};
const nodeProcess = (globalThis as unknown as { process: NodeProcess }).process;

/** TasksView's onSave awaits mutateAsync without a catch, so a failed save also rejects unhandled; tolerate only those ApiErrors. */
async function tolerateUnhandledApiErrors(run: () => Promise<void>) {
  const original = nodeProcess.listeners("unhandledRejection");
  const listener: RejectionListener = (reason, promise) => {
    if (!(reason instanceof ApiError)) {
      original.forEach((forward) => forward(reason, promise));
    }
  };
  nodeProcess.removeAllListeners("unhandledRejection");
  nodeProcess.on("unhandledRejection", listener);
  try {
    await run();
    await new Promise((resolve) => setTimeout(resolve, 0));
  } finally {
    nodeProcess.off("unhandledRejection", listener);
    original.forEach((restore) => nodeProcess.on("unhandledRejection", restore));
  }
}

function rowFor(title: string) {
  const table = screen.getByRole("table", { name: "Tasks" });
  return within(table).getByRole("button", { name: title }).closest("tr")!;
}

describe("TasksView: create task", () => {
  it("blocks submission while the required title is empty", async () => {
    const { user, api } = renderTasks({ api: { "GET /api/tasks": [], "GET /api/work/options": workOptions } });

    await screen.findByRole("heading", { name: "No tasks in this view" });
    await user.click(screen.getAllByRole("button", { name: "New task" })[0]);
    const dialog = screen.getByRole("dialog", { name: "New task" });
    const title = within(dialog).getByLabelText("Title");

    await user.click(within(dialog).getByRole("button", { name: "Create task" }));

    expect(title).toBeRequired();
    expect(title).toBeInvalid();
    expect(api.sent("POST", "/api/tasks")).toHaveLength(0);
    expect(screen.getByRole("dialog", { name: "New task" })).toBeInTheDocument();
  });

  it("sends the task, refetches the list and switches the panel to the new task", async () => {
    const rows: TaskListItem[] = [];
    let created: TaskDetail | null = null;
    const { user, api } = renderTasks({
      api: {
        "GET /api/tasks": () => rows,
        "GET /api/work/options": workOptions,
        "POST /api/tasks": ({ body }: MockRequest) => {
          const write = body as { title: string; priority: string; assigneeId: string; projectId: string; tags: string[]; estimatedHours: number };
          created = taskDetail({
            id: "task-new",
            title: write.title,
            priority: write.priority,
            assigneeId: write.assigneeId,
            assigneeName: grace.name,
            projectId: write.projectId,
            projectName: "Apollo",
            tags: write.tags,
            estimatedHours: write.estimatedHours,
            description: "For the Friday review",
          });
          rows.push(listItem(created));
          return created;
        },
        "GET /api/tasks/:id": () => created,
      },
    });

    await screen.findByRole("heading", { name: "No tasks in this view" });
    await user.click(screen.getAllByRole("button", { name: "New task" })[0]);
    const dialog = screen.getByRole("dialog", { name: "New task" });
    await user.type(within(dialog).getByLabelText("Title"), "  Prepare demo ");
    await user.type(within(dialog).getByLabelText("Description"), "For the Friday review");
    await user.selectOptions(within(dialog).getByLabelText("Priority"), "High");
    await user.selectOptions(within(dialog).getByLabelText("Assignee"), grace.name);
    await user.selectOptions(within(dialog).getByLabelText("Project"), "Apollo");
    await user.type(within(dialog).getByLabelText("Estimated hours"), "3");
    await user.type(within(dialog).getByLabelText(/^Tags/), "demo, frontend ,");
    await user.click(within(dialog).getByRole("button", { name: "Create task" }));

    const newDialog = await screen.findByRole("dialog", { name: "Prepare demo" });
    expect(await within(newDialog).findByRole("button", { name: "Save changes" })).toBeInTheDocument();
    expect(within(newDialog).queryByRole("button", { name: "Create task" })).not.toBeInTheDocument();

    const [request] = api.sent("POST", "/api/tasks");
    expect(request.body).toEqual({
      title: "Prepare demo",
      description: "For the Friday review",
      status: "Todo",
      priority: "High",
      startAt: null,
      dueAt: null,
      assigneeId: grace.id,
      projectId: "project-apollo",
      parentTaskId: null,
      teamId: null,
      estimatedHours: 3,
      actualHours: null,
      tags: ["demo", "frontend"],
    });
    expect(await screen.findByRole("table", { name: "Tasks" })).toBeInTheDocument();
    expect(within(rowFor("Prepare demo")).getByText(grace.name)).toBeInTheDocument();
    expect(api.sent("GET", "/api/tasks/task-new").length).toBeGreaterThan(0);
    expect(api.unhandled).toEqual([]);
  });

  it("defaults the assignee to the signed-in user", async () => {
    const { user, api } = renderTasks({
      api: {
        "GET /api/tasks": [],
        "GET /api/work/options": workOptions,
        "POST /api/tasks": taskDetail({ id: "task-new", title: "Solo task", assigneeId: adminUser.id, assigneeName: adminUser.displayName }),
        "GET /api/tasks/:id": taskDetail({ id: "task-new", title: "Solo task", assigneeId: adminUser.id, assigneeName: adminUser.displayName }),
      },
    });

    await screen.findByRole("heading", { name: "No tasks in this view" });
    await user.click(screen.getAllByRole("button", { name: "New task" })[0]);
    const dialog = screen.getByRole("dialog", { name: "New task" });
    expect(within(dialog).getByLabelText("Assignee")).toHaveValue(adminUser.id);
    await user.type(within(dialog).getByLabelText("Title"), "Solo task");
    await user.click(within(dialog).getByRole("button", { name: "Create task" }));

    await screen.findByRole("dialog", { name: "Solo task" });
    expect(api.sent("POST", "/api/tasks")[0].body).toMatchObject({ title: "Solo task", assigneeId: adminUser.id });
  });

  it("shows the server validation message and keeps the form open when the create fails", async () => {
    const { user, api } = renderTasks({
      api: {
        "GET /api/tasks": [],
        "GET /api/work/options": workOptions,
        "POST /api/tasks": fail(400, "Title is required.", "VALIDATION_FAILED"),
      },
    });

    await screen.findByRole("heading", { name: "No tasks in this view" });
    await user.click(screen.getAllByRole("button", { name: "New task" })[0]);
    const dialog = screen.getByRole("dialog", { name: "New task" });
    await user.type(within(dialog).getByLabelText("Title"), "   ");

    await tolerateUnhandledApiErrors(async () => {
      await user.click(within(dialog).getByRole("button", { name: "Create task" }));
      expect(await within(dialog).findByText("Title is required.")).toBeInTheDocument();
    });

    expect(api.sent("POST", "/api/tasks")).toHaveLength(1);
    expect(api.sent("POST", "/api/tasks")[0].body).toMatchObject({ title: "" });
    expect(screen.getByRole("dialog", { name: "New task" })).toBeInTheDocument();
    expect(within(dialog).getByRole("button", { name: "Create task" })).toBeEnabled();
  });

  it("surfaces a permission error from the server", async () => {
    const { user } = renderTasks({
      api: {
        "GET /api/tasks": [],
        "GET /api/work/options": workOptions,
        "POST /api/tasks": fail(403, "You do not have permission to manage tasks.", "FORBIDDEN"),
      },
    });

    await screen.findByRole("heading", { name: "No tasks in this view" });
    await user.click(screen.getAllByRole("button", { name: "New task" })[0]);
    const dialog = screen.getByRole("dialog", { name: "New task" });
    await user.type(within(dialog).getByLabelText("Title"), "Forbidden task");

    await tolerateUnhandledApiErrors(async () => {
      await user.click(within(dialog).getByRole("button", { name: "Create task" }));
      expect(await within(dialog).findByText("You do not have permission to manage tasks.")).toBeInTheDocument();
    });

    expect(within(dialog).getByLabelText("Title")).toHaveValue("Forbidden task");
  });
});

describe("TasksView: complete task", () => {
  it("completes a task from the panel and shows the done state", async () => {
    const server = taskServer(taskDetail());
    const { user, api } = renderTasks({
      api: {
        ...server.routes,
        "POST /api/tasks/:id/complete": () => {
          server.state.task = { ...server.state.task, status: "Completed" };
          return server.state.task;
        },
      },
    });

    const dialog = await openTask(user, "Write release notes");
    await user.click(within(dialog).getByRole("button", { name: "Complete" }));

    expect(await within(dialog).findByRole("button", { name: "Reopen" })).toBeInTheDocument();
    const [request] = api.sent("POST", "/api/tasks/task-1/complete");
    expect(request.body).toBeNull();
    expect(api.sent("POST", "/api/tasks/:id/complete")).toHaveLength(1);
    expect(within(dialog).getByLabelText("Status")).toHaveValue("Completed");
    await waitFor(() => expect(within(rowFor("Write release notes")).getByText("Completed")).toBeInTheDocument());
    expect(api.unhandled).toEqual([]);
  });

  it("reopens a completed task", async () => {
    const server = taskServer(taskDetail({ status: "Completed" }));
    const { user, api } = renderTasks({
      api: {
        ...server.routes,
        "POST /api/tasks/:id/reopen": () => {
          server.state.task = { ...server.state.task, status: "Todo" };
          return server.state.task;
        },
      },
    });

    const dialog = await openTask(user, "Write release notes");
    await user.click(within(dialog).getByRole("button", { name: "Reopen" }));

    expect(await within(dialog).findByRole("button", { name: "Complete" })).toBeInTheDocument();
    expect(api.sent("POST", "/api/tasks/task-1/reopen")).toHaveLength(1);
    await waitFor(() => expect(within(rowFor("Write release notes")).getByText("Todo")).toBeInTheDocument());
  });

  it("shows the error and keeps the task open when completing fails", async () => {
    const server = taskServer(taskDetail());
    const { user } = renderTasks({
      api: {
        ...server.routes,
        "POST /api/tasks/:id/complete": fail(400, "Finish the blocking task first.", "VALIDATION_FAILED"),
      },
    });

    const dialog = await openTask(user, "Write release notes");
    await user.click(within(dialog).getByRole("button", { name: "Complete" }));

    expect(await within(dialog).findByText("Finish the blocking task first.")).toBeInTheDocument();
    expect(within(dialog).getByRole("button", { name: "Complete" })).toBeInTheDocument();
    expect(within(rowFor("Write release notes")).getByText("Todo")).toBeInTheDocument();
  });

  it("moves a card to the Completed column on the kanban board", async () => {
    const server = taskServer(taskDetail());
    const { api } = renderTasks({
      view: "kanban",
      api: {
        ...server.routes,
        "POST /api/tasks/:id/status": ({ body }: MockRequest) => {
          server.state.task = { ...server.state.task, status: (body as { status: string }).status };
          return server.state.task;
        },
      },
    });

    const column = (await screen.findByRole("heading", { name: "Completed · 0" })).closest("section")!;
    fireEvent.drop(column, { dataTransfer: { getData: () => "task-1" } });

    expect(await screen.findByRole("heading", { name: "Completed · 1" })).toBeInTheDocument();
    const [request] = api.sent("POST", "/api/tasks/task-1/status");
    expect(request.body).toEqual({ status: "Completed" });
    expect(api.sent("GET", "/api/tasks")[0].query.get("view")).toBe("kanban");
  });
});

describe("TasksView: assign task", () => {
  it("sends the chosen assignee id and shows the new assignee", async () => {
    const server = taskServer(taskDetail());
    const { user, api } = renderTasks({
      api: {
        ...server.routes,
        "PUT /api/tasks/:id/assignee": ({ body }: MockRequest) => {
          const { assigneeId } = body as { assigneeId: string | null };
          server.state.task = { ...server.state.task, assigneeId, assigneeName: assigneeId === grace.id ? grace.name : null };
          return server.state.task;
        },
      },
    });

    const dialog = await openTask(user, "Write release notes");
    expect(within(rowFor("Write release notes")).getByText("Unassigned")).toBeInTheDocument();
    await user.selectOptions(within(dialog).getByLabelText("Assignee"), grace.name);
    await user.click(within(dialog).getByRole("button", { name: "Assign" }));

    await waitFor(() => expect(within(rowFor("Write release notes")).getByText(grace.name)).toBeInTheDocument());
    const [request] = api.sent("PUT", "/api/tasks/task-1/assignee");
    expect(request.body).toEqual({ assigneeId: grace.id });
    expect(within(dialog).getByLabelText("Assignee")).toHaveValue(grace.id);
  });

  it("sends null to unassign a task", async () => {
    const server = taskServer(taskDetail({ assigneeId: grace.id, assigneeName: grace.name }));
    const { user, api } = renderTasks({
      api: {
        ...server.routes,
        "PUT /api/tasks/:id/assignee": () => {
          server.state.task = { ...server.state.task, assigneeId: null, assigneeName: null };
          return server.state.task;
        },
      },
    });

    const dialog = await openTask(user, "Write release notes");
    expect(within(dialog).getByLabelText("Assignee")).toHaveValue(grace.id);
    await user.selectOptions(within(dialog).getByLabelText("Assignee"), "Unassigned");
    await user.click(within(dialog).getByRole("button", { name: "Assign" }));

    await waitFor(() => expect(within(rowFor("Write release notes")).getByText("Unassigned")).toBeInTheDocument());
    expect(api.sent("PUT", "/api/tasks/task-1/assignee")[0].body).toEqual({ assigneeId: null });
  });

  it("shows the server error when the assignment is rejected", async () => {
    const server = taskServer(taskDetail());
    const { user } = renderTasks({
      api: {
        ...server.routes,
        "PUT /api/tasks/:id/assignee": fail(403, "Only project members can be assigned.", "FORBIDDEN"),
      },
    });

    const dialog = await openTask(user, "Write release notes");
    await user.selectOptions(within(dialog).getByLabelText("Assignee"), grace.name);
    await user.click(within(dialog).getByRole("button", { name: "Assign" }));

    expect(await within(dialog).findByText("Only project members can be assigned.")).toBeInTheDocument();
    expect(within(rowFor("Write release notes")).getByText("Unassigned")).toBeInTheDocument();
  });

  it("warns when the chosen assignee is on leave on the due date", async () => {
    const server = taskServer(taskDetail({ dueAt: "2026-10-08T10:00:00Z" }));
    const { user, api } = renderTasks({
      api: {
        ...server.routes,
        "GET /api/leaves/availability": ({ query }: MockRequest) => ({ warning: query.get("userId") === grace.id ? "Grace Hopper is on leave that day." : null }),
      },
    });

    const dialog = await openTask(user, "Write release notes");
    await user.selectOptions(within(dialog).getByLabelText("Assignee"), grace.name);

    expect(await within(dialog).findByText("Grace Hopper is on leave that day.")).toBeInTheDocument();
    expect(api.sent("GET", "/api/leaves/availability").at(-1)?.query.get("userId")).toBe(grace.id);
  });
});

describe("TasksView: list states", () => {
  it("shows the empty state with a create action for managers", async () => {
    const { user } = renderTasks({ api: { "GET /api/tasks": [], "GET /api/work/options": workOptions } });

    const heading = await screen.findByRole("heading", { name: "No tasks in this view" });
    const emptyState = heading.parentElement!;
    await user.click(within(emptyState).getByRole("button", { name: "New task" }));

    expect(screen.getByRole("dialog", { name: "New task" })).toBeInTheDocument();
  });

  it("shows the error state when the list fails and refetches on retry", async () => {
    const { user, api } = renderTasks({
      api: { "GET /api/tasks": fail(500, "The task store is offline.", "SERVER_ERROR"), "GET /api/work/options": workOptions },
    });

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent("Tasks are unavailable");
    expect(alert).toHaveTextContent("The task store is offline.");
    expect(api.sent("GET", "/api/tasks")).toHaveLength(1);

    api.set({ "GET /api/tasks": [listItem(taskDetail())] });
    await user.click(within(alert).getByRole("button", { name: "Try again" }));

    expect(await screen.findByRole("button", { name: "Write release notes" })).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    expect(api.sent("GET", "/api/tasks")).toHaveLength(2);
  });

  it("falls back to a generic message when the response does not match the schema", async () => {
    renderTasks({ api: { "GET /api/tasks": [{ id: "task-1" }], "GET /api/work/options": workOptions } });

    expect(await screen.findByRole("alert")).toHaveTextContent("Tasks could not be loaded.");
  });
});

describe("TasksView: authorization", () => {
  it("hides create controls from members without tasks.manage", async () => {
    renderTasks({ user: memberUser, api: { "GET /api/tasks": [], "GET /api/work/options": workOptions } });

    const heading = await screen.findByRole("heading", { name: "No tasks in this view" });
    expect(within(heading.parentElement!).queryByRole("button")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "New task" })).not.toBeInTheDocument();
  });

  it("shows a read-only task panel to members", async () => {
    const server = taskServer(taskDetail({ assigneeId: grace.id, assigneeName: grace.name }));
    const { user, api } = renderTasks({ user: memberUser, api: server.routes });

    const dialog = await openTask(user, "Write release notes");

    for (const name of ["Save changes", "Complete", "Assign", "Add subtask", "Delete"]) {
      expect(within(dialog).queryByRole("button", { name })).not.toBeInTheDocument();
    }
    expect(within(dialog).queryByRole("textbox", { name: "Comment" })).not.toBeInTheDocument();
    expect(api.calls.filter((call) => call.method !== "GET")).toEqual([]);
  });

  it("denies access to users without tasks.read", async () => {
    renderTasks({
      user: { ...memberUser, permissions: [permissions.projectsRead] },
      api: { "GET /api/tasks": [], "GET /api/work/options": workOptions },
    });

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent("Access denied");
    expect(screen.queryByRole("heading", { name: "Tasks" })).not.toBeInTheDocument();
  });
});
