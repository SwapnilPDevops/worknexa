import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { permissions } from "../../auth/access.ts";
import type { SearchGroup } from "../../features/search/api.ts";
import { fail } from "../../test/api.ts";
import { adminUser, memberUser, renderWithProviders } from "../../test/render.tsx";
import { CommandPalette } from "./command-palette.tsx";
import { ShellProvider } from "./shell-context.tsx";

const budgetResults: SearchGroup[] = [
  {
    entity: "task",
    label: "Tasks",
    hits: [
      { entity: "task", id: "task-1", title: "Budget review", detail: "Due tomorrow", route: "/tasks?task=task-1" },
      { entity: "task", id: "task-2", title: "Budget sign-off", detail: null, route: "/tasks?task=task-2" },
    ],
  },
  {
    entity: "meetingNote",
    label: "Meeting notes",
    hits: [{ entity: "meetingNote", id: "meeting-1", title: "Budget planning", detail: "2026-10-06", route: "/meeting-notes?meeting=meeting-1" }],
  },
];

function renderPalette(options: Parameters<typeof renderWithProviders>[1] = {}) {
  return renderWithProviders(
    <ShellProvider>
      <CommandPalette />
    </ShellProvider>,
    {
      api: { "GET /api/search": ({ query }: { query: URLSearchParams }) => (query.get("q") === "budget" ? budgetResults : []) },
      ...options,
    },
  );
}

async function openPalette(user: ReturnType<typeof renderPalette>["user"]) {
  await user.keyboard("{Control>}k{/Control}");
  const dialog = await screen.findByRole("dialog", { name: "Search" });
  expect(dialog).toHaveAttribute("open");
  return dialog;
}

function lastSearch(api: ReturnType<typeof renderPalette>["api"]) {
  return api.sent("GET", "/api/search").at(-1);
}

describe("CommandPalette search", () => {
  it("opens with Ctrl+K, focuses the search box and does not search an empty query", async () => {
    const { user, api } = renderPalette();

    const dialog = await openPalette(user);

    const input = within(dialog).getByRole("textbox", { name: "Search Worknexa" });
    expect(input).toHaveFocus();
    expect(api.sent("GET", "/api/search")).toHaveLength(0);

    await user.keyboard("{Control>}k{/Control}");
    await waitFor(() => expect(dialog).not.toHaveAttribute("open"));
  });

  it("searches with the debounced query and renders hits grouped by entity", async () => {
    const { user, api } = renderPalette();
    const dialog = await openPalette(user);

    await user.type(within(dialog).getByRole("textbox", { name: "Search Worknexa" }), "budget");

    const tasks = await within(dialog).findByRole("region", { name: "Tasks" });
    const request = lastSearch(api);
    expect(request?.query.get("q")).toBe("budget");
    expect(request?.query.get("limit")).toBe("5");
    expect(within(tasks).getAllByRole("option").map((option) => option.textContent)).toEqual(["Budget reviewDue tomorrow", "Budget sign-offTasks"]);
    const notes = within(dialog).getByRole("region", { name: "Meeting notes" });
    expect(within(notes).getByRole("option", { name: /Budget planning/ })).toHaveTextContent("2026-10-06");
    expect(within(dialog).getAllByRole("option")[0]).toHaveAttribute("aria-selected", "true");
    expect(within(dialog).queryByText("No matches.")).not.toBeInTheDocument();
  });

  it("navigates to a clicked result and closes", async () => {
    const { user } = renderPalette();
    const dialog = await openPalette(user);

    await user.type(within(dialog).getByRole("textbox", { name: "Search Worknexa" }), "budget");
    await user.click(await within(dialog).findByRole("option", { name: /Budget planning/ }));

    expect(screen.getByTestId("location")).toHaveTextContent("/meeting-notes?meeting=meeting-1");
    await waitFor(() => expect(dialog).not.toHaveAttribute("open"));
  });

  it("moves through results with the arrow keys and opens the active one with Enter", async () => {
    const { user } = renderPalette();
    const dialog = await openPalette(user);

    await user.type(within(dialog).getByRole("textbox", { name: "Search Worknexa" }), "budget");
    await within(dialog).findByRole("option", { name: /Budget planning/ });
    await user.keyboard("{ArrowDown}");

    expect(within(dialog).getByRole("option", { name: /Budget sign-off/ })).toHaveAttribute("aria-selected", "true");
    await user.keyboard("{Enter}");

    expect(screen.getByTestId("location")).toHaveTextContent("/tasks?task=task-2");
    await waitFor(() => expect(dialog).not.toHaveAttribute("open"));
  });

  it("shows the empty message when nothing matches", async () => {
    const { user, api } = renderPalette();
    const dialog = await openPalette(user);

    await user.type(within(dialog).getByRole("textbox", { name: "Search Worknexa" }), "zzqx");

    expect(await within(dialog).findByText("No matches.")).toBeInTheDocument();
    expect(lastSearch(api)?.query.get("q")).toBe("zzqx");
    expect(within(dialog).queryAllByRole("option")).toHaveLength(0);
  });

  it("shows that search is unavailable when the request fails", async () => {
    const { user } = renderPalette({ api: { "GET /api/search": fail(500, "Search index offline.") } });
    const dialog = await openPalette(user);

    await user.type(within(dialog).getByRole("textbox", { name: "Search Worknexa" }), "budget");

    expect(await within(dialog).findByText("Search is unavailable.")).toBeInTheDocument();
    expect(within(dialog).queryAllByRole("option")).toHaveLength(0);
  });

  it("keeps matching commands available when search fails", async () => {
    const { user } = renderPalette({ api: { "GET /api/search": fail(503, "Search index offline.") } });
    const dialog = await openPalette(user);

    await user.type(within(dialog).getByRole("textbox", { name: "Search Worknexa" }), "calendar");

    expect(await within(dialog).findByText("Search is unavailable.")).toBeInTheDocument();
    expect(within(dialog).getByRole("option", { name: /^Open Calendar/ })).toBeInTheDocument();
  });

  it("clears the query when the palette is closed and reopened", async () => {
    const { user } = renderPalette();
    const dialog = await openPalette(user);

    await user.type(within(dialog).getByRole("textbox", { name: "Search Worknexa" }), "budget");
    await within(dialog).findByRole("option", { name: /Budget review/ });
    await user.click(within(dialog).getByRole("button", { name: "Close dialog" }));
    await waitFor(() => expect(dialog).not.toHaveAttribute("open"));

    await openPalette(user);
    expect(within(dialog).getByRole("textbox", { name: "Search Worknexa" })).toHaveValue("");
    expect(within(dialog).queryByRole("option", { name: /Budget review/ })).not.toBeInTheDocument();
  });
});

describe("CommandPalette commands and permissions", () => {
  it("offers every create command to an admin and runs navigation commands", async () => {
    const { user } = renderPalette({ user: adminUser });
    const dialog = await openPalette(user);

    await user.type(within(dialog).getByRole("textbox", { name: "Search Worknexa" }), "create");

    const create = await within(dialog).findByRole("region", { name: "Create" });
    const labels = within(create).getAllByRole("option").map((option) => option.firstChild?.textContent);
    expect(labels).toEqual(["Create Task", "Create Todo", "Create Meeting", "Create Reminder", "Create Note", "Create Project", "Assign Task"]);

    await user.click(within(create).getByRole("option", { name: /^Create Project/ }));
    expect(screen.getByTestId("location")).toHaveTextContent("/projects?compose=1");
  });

  it("only offers create commands a member is allowed to run", async () => {
    expect(memberUser.permissions).toContain(permissions.notesManage);
    expect(memberUser.permissions).not.toContain(permissions.tasksManage);
    const { user } = renderPalette({ user: memberUser });
    const dialog = await openPalette(user);

    await user.type(within(dialog).getByRole("textbox", { name: "Search Worknexa" }), "create");

    const create = await within(dialog).findByRole("region", { name: "Create" });
    expect(within(create).getAllByRole("option").map((option) => option.firstChild?.textContent)).toEqual(["Create Note"]);
  });

  it("hides navigation entries the user cannot read", async () => {
    const { user } = renderPalette({ user: { ...memberUser, permissions: [permissions.notesRead] } });
    const dialog = await openPalette(user);

    await user.type(within(dialog).getByRole("textbox", { name: "Search Worknexa" }), "notes");

    expect(await within(dialog).findByRole("option", { name: /^Notes/ })).toBeInTheDocument();
    expect(within(dialog).queryByRole("option", { name: /^Meeting Notes/ })).not.toBeInTheDocument();
    expect(within(dialog).queryByRole("option", { name: /^Learning Notes/ })).not.toBeInTheDocument();
  });
});
