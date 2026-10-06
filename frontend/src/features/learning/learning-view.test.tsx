import { screen, waitFor, within } from "@testing-library/react";
import { Route, Routes } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { RequireAuth } from "../../app/require-auth.tsx";
import { fail } from "../../test/api.ts";
import { memberUser, renderWithProviders } from "../../test/render.tsx";
import type { LearningTopic, LearningTopicWrite } from "./api.ts";
import { LearningView } from "./learning-view.tsx";

function topic(overrides: Partial<LearningTopic> = {}): LearningTopic {
  return {
    id: "topic-react",
    name: "React",
    category: "Frontend",
    description: null,
    status: "Learning",
    progress: 40,
    priority: "High",
    startDate: "2026-09-01",
    targetDate: "2026-12-01",
    parentId: null,
    parentName: null,
    goal: null,
    childCount: 0,
    ...overrides,
  };
}

const emptyDashboard = { progress: 0, currentTopics: [], goals: [], recentlyStudied: [], upcomingTargets: [] };

/** A learning backend whose topic list includes every topic created through the API. */
function learningBackend(initial: LearningTopic[] = [topic()]) {
  const topics = [...initial];
  return {
    "GET /api/learning/dashboard": emptyDashboard,
    "GET /api/learning/topics/:id": ({ params }: { params: Record<string, string> }) => ({ ...topics.find((item) => item.id === params.id)!, resources: [] }),
    "GET /api/learning/topics": () => [...topics],
    "POST /api/learning/topics": ({ body }: { body: unknown }) => {
      const input = body as LearningTopicWrite;
      const created = topic({
        id: `topic-${topics.length + 1}`,
        name: input.name,
        category: input.category,
        description: input.description,
        status: input.status,
        progress: input.progress ?? 0,
        priority: input.priority,
        startDate: input.startDate,
        targetDate: input.targetDate,
        parentId: input.parentTopicId,
        parentName: topics.find((item) => item.id === input.parentTopicId)?.name ?? null,
        goal: input.goal,
      });
      topics.push(created);
      return { ...created, resources: [] };
    },
  };
}

function renderLearning(options: Parameters<typeof renderWithProviders>[1] = {}) {
  return renderWithProviders(
    <Routes>
      <Route element={<RequireAuth />}>
        <Route path="/learning" element={<LearningView />} />
      </Route>
    </Routes>,
    { route: "/learning", ...options },
  );
}

async function openNewTopic(user: ReturnType<typeof renderLearning>["user"]) {
  await user.click(await screen.findByRole("button", { name: "New topic" }));
  return within(await screen.findByRole("dialog", { name: "New topic" }));
}

describe("LearningView – create topic", () => {
  it("lists topics and calls only the dashboard and topic endpoints on mount", async () => {
    const { api } = renderLearning({ api: learningBackend() });

    const table = await screen.findByRole("table", { name: "Learning topics" });
    expect(within(table).getByRole("button", { name: "React" })).toBeInTheDocument();
    expect(api.sent("GET", "/api/learning/dashboard")[0].query.get("date")).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    expect(api.unhandled).toEqual([]);
  });

  it("shows the empty state when there are no topics", async () => {
    renderLearning({ api: learningBackend([]) });

    expect(await screen.findByRole("heading", { name: "No topics" })).toBeInTheDocument();
    expect(screen.getByText("Create a topic, or build the example roadmap.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Open roadmap" })).toHaveAttribute("href", "/roadmap");
  });

  it("requires a name before saving", async () => {
    const { user, api } = renderLearning({ api: learningBackend() });
    const dialog = await openNewTopic(user);

    await user.click(dialog.getByRole("button", { name: "Save topic" }));

    expect(dialog.getByLabelText("Name")).toBeInvalid();
    expect(api.sent("POST", "/api/learning/topics")).toHaveLength(0);
    expect(screen.getByRole("dialog", { name: "New topic" })).toBeInTheDocument();
  });

  it("rejects progress outside 0 to 100", async () => {
    const { user, api } = renderLearning({ api: learningBackend() });
    const dialog = await openNewTopic(user);

    await user.type(dialog.getByLabelText("Name"), "TypeScript");
    await user.clear(dialog.getByLabelText("Progress"));
    await user.type(dialog.getByLabelText("Progress"), "150");
    await user.click(dialog.getByRole("button", { name: "Save topic" }));

    expect(dialog.getByLabelText("Progress")).toBeInvalid();
    expect(api.sent("POST", "/api/learning/topics")).toHaveLength(0);
  });

  it("creates a topic with the entered fields, closes the drawer and lists it after the refetch", async () => {
    const { user, api } = renderLearning({ api: learningBackend() });
    const dialog = await openNewTopic(user);

    await user.type(dialog.getByLabelText("Name"), "  React Query  ");
    await user.type(dialog.getByLabelText("Category"), "Frontend");
    await user.type(dialog.getByLabelText("Description"), "Server state caching");
    await user.selectOptions(dialog.getByLabelText("Status"), "Learning");
    expect(dialog.getByLabelText("Progress")).toHaveValue(25);
    await user.selectOptions(dialog.getByLabelText("Priority"), "Critical");
    await user.selectOptions(dialog.getByLabelText("Parent"), "React");
    await user.type(dialog.getByLabelText("Start date"), "2026-10-06");
    await user.type(dialog.getByLabelText("Target date"), "2026-11-30");
    await user.type(dialog.getByLabelText("Learning goal"), "Ship a cached dashboard");
    await user.click(dialog.getByRole("button", { name: "Save topic" }));

    await waitFor(() => expect(screen.queryByRole("dialog", { name: "New topic" })).not.toBeInTheDocument());
    const [request] = api.sent("POST", "/api/learning/topics");
    expect(request.body).toEqual({
      name: "React Query",
      category: "Frontend",
      description: "Server state caching",
      status: "Learning",
      progress: 25,
      startDate: "2026-10-06",
      targetDate: "2026-11-30",
      priority: "Critical",
      parentTopicId: "topic-react",
      goal: "Ship a cached dashboard",
    });

    const table = screen.getByRole("table", { name: "Learning topics" });
    expect(await within(table).findByRole("button", { name: "React / React Query" })).toBeInTheDocument();
    expect(api.sent("GET", "/api/learning/topics")).toHaveLength(2);
    expect(api.sent("GET", "/api/learning/dashboard")).toHaveLength(2);
  });

  it("sends nulls for optional fields left blank", async () => {
    const { user, api } = renderLearning({ api: learningBackend() });
    const dialog = await openNewTopic(user);

    await user.type(dialog.getByLabelText("Name"), "Rust");
    await user.click(dialog.getByRole("button", { name: "Save topic" }));

    await waitFor(() => expect(api.sent("POST", "/api/learning/topics")).toHaveLength(1));
    expect(api.sent("POST", "/api/learning/topics")[0].body).toEqual({
      name: "Rust",
      category: null,
      description: null,
      status: "NotStarted",
      progress: 0,
      startDate: null,
      targetDate: null,
      priority: "Medium",
      parentTopicId: null,
      goal: null,
    });
  });

  it("surfaces a server rejection and keeps the drawer open", async () => {
    const { user, api } = renderLearning({
      api: { ...learningBackend(), "POST /api/learning/topics": fail(409, "A topic with this name already exists.", "CONFLICT") },
    });
    const dialog = await openNewTopic(user);

    await user.type(dialog.getByLabelText("Name"), "React");
    await user.click(dialog.getByRole("button", { name: "Save topic" }));

    expect(await dialog.findByText("A topic with this name already exists.")).toBeInTheDocument();
    expect(screen.getByRole("dialog", { name: "New topic" })).toBeInTheDocument();
    expect(dialog.getByLabelText("Name")).toHaveValue("React");
    expect(dialog.getByRole("button", { name: "Save topic" })).toBeEnabled();
    expect(api.sent("GET", "/api/learning/topics")).toHaveLength(1);
  });

  it("clears a previous error when the drawer is reopened", async () => {
    const { user } = renderLearning({
      api: { ...learningBackend(), "POST /api/learning/topics": fail(400, "Name is too long.", "VALIDATION_FAILED") },
    });
    let dialog = await openNewTopic(user);
    await user.type(dialog.getByLabelText("Name"), "x");
    await user.click(dialog.getByRole("button", { name: "Save topic" }));
    expect(await dialog.findByText("Name is too long.")).toBeInTheDocument();

    await user.click(dialog.getByRole("button", { name: "Cancel" }));
    dialog = await openNewTopic(user);

    expect(dialog.queryByText("Name is too long.")).not.toBeInTheDocument();
    expect(dialog.getByLabelText("Name")).toHaveValue("");
  });

  it("shows the error state when the topic list fails and refetches on retry", async () => {
    const backend = learningBackend();
    const { user, api } = renderLearning({ api: { ...backend, "GET /api/learning/topics": fail(500, "Topics are offline.", "SERVER_ERROR") } });

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent("Learning is unavailable");
    expect(alert).toHaveTextContent("Topics are offline.");

    api.set({ "GET /api/learning/topics": backend["GET /api/learning/topics"] });
    await user.click(within(alert).getByRole("button", { name: "Try again" }));

    expect(await screen.findByRole("table", { name: "Learning topics" })).toBeInTheDocument();
    expect(api.sent("GET", "/api/learning/topics")).toHaveLength(2);
  });

  it("hides topic creation and deletion from members without learning.manage", async () => {
    const { user } = renderLearning({ user: memberUser, api: learningBackend() });

    const table = await screen.findByRole("table", { name: "Learning topics" });
    expect(screen.queryByRole("button", { name: "New topic" })).not.toBeInTheDocument();

    await user.click(within(table).getByRole("button", { name: "React" }));
    const dialog = within(await screen.findByRole("dialog", { name: "Edit topic" }));
    expect(await dialog.findByText("No resources yet.")).toBeInTheDocument();
    expect(dialog.queryByRole("button", { name: "Delete topic" })).not.toBeInTheDocument();
    expect(dialog.queryByLabelText("Resource")).not.toBeInTheDocument();
    // The topic fields and the save button are not gated by learning.manage.
    expect(dialog.getByRole("button", { name: "Save topic" })).toBeEnabled();
    expect(dialog.getByLabelText("Name")).toBeEnabled();
  });
});
