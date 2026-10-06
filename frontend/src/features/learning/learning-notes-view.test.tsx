import { act, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { RequireAuth } from "../../app/require-auth.tsx";
import { permissions } from "../../auth/access.ts";
import type { UserSession } from "../../auth/session.ts";
import { fail } from "../../test/api.ts";
import { memberUser, renderWithProviders } from "../../test/render.tsx";
import type { NoteItem, NoteWrite } from "../notes/api.ts";
import { LearningNotesView } from "./learning-notes-view.tsx";

const readOnlyUser: UserSession = {
  id: "33333333-3333-3333-3333-333333333333",
  email: "reader@worknexa.local",
  displayName: "Rita Reader",
  roles: ["Viewer"],
  permissions: [permissions.learningRead, permissions.notesRead],
};

const topics = [
  { id: "topic-react", name: "React" },
  { id: "topic-ts", name: "TypeScript" },
];

function note(overrides: Partial<NoteItem> = {}): NoteItem {
  return {
    id: "note-1",
    title: "Hooks recap",
    content: "useEffect runs after paint",
    category: "Learning",
    isFavorite: false,
    isPinned: false,
    projectId: null,
    projectName: null,
    taskId: null,
    taskTitle: null,
    meetingId: null,
    meetingTitle: null,
    learningTopicId: "topic-react",
    learningTopicTitle: "React",
    tags: ["react"],
    createdAt: "2026-10-01T09:00:00Z",
    updatedAt: "2026-10-02T09:00:00Z",
    ...overrides,
  };
}

function topicDetail(id: string) {
  const { name } = topics.find((item) => item.id === id)!;
  return { id, name, category: null, description: null, status: "Learning", progress: 10, priority: "Medium", startDate: null, targetDate: null, parentId: null, parentName: null, goal: null, childCount: 0, resources: [] };
}

/** A notes backend whose list includes every note created through the API. */
function notesBackend(initial: NoteItem[] = []) {
  const notes = [...initial];
  return {
    "GET /api/notes/options": { projects: [], tasks: [], meetings: [], learningTopics: topics, tags: [] },
    "GET /api/notes": () => [...notes],
    "POST /api/notes": ({ body }: { body: unknown }) => {
      const input = body as NoteWrite;
      const created = note({
        id: `note-${notes.length + 1}`,
        title: input.title,
        content: input.content,
        category: input.category,
        isFavorite: input.isFavorite,
        learningTopicId: input.learningTopicId,
        learningTopicTitle: topics.find((item) => item.id === input.learningTopicId)?.name ?? null,
        tags: input.tags,
        createdAt: "2026-10-06T10:00:00Z",
        updatedAt: "2026-10-06T10:00:00Z",
      });
      notes.unshift(created);
      return created;
    },
    "GET /api/work/options": { projects: [], people: [{ id: "u-ada", name: "Ada Admin" }], teams: [], tags: [] },
    "GET /api/learning/topics/:id": ({ params }: { params: Record<string, string> }) => topicDetail(params.id),
  };
}

function renderNotes(options: Parameters<typeof renderWithProviders>[1] = {}) {
  const view = renderWithProviders(
    <Routes>
      <Route element={<RequireAuth />}>
        <Route path="/learning-notes" element={<LearningNotesView />} />
      </Route>
    </Routes>,
    { route: "/learning-notes", ...options },
  );
  return { ...view, user: userEvent.setup({ advanceTimers: vi.advanceTimersByTime }) };
}

/** Lets the 800 ms autosave debounce elapse. */
async function passAutosaveDelay() {
  await act(() => vi.advanceTimersByTimeAsync(1500));
}

async function startNewNote(user: ReturnType<typeof renderNotes>["user"]) {
  await user.click(await screen.findByRole("button", { name: "New" }));
  return within(screen.getByLabelText("Title").closest("section")!);
}

/** The markdown body; its "Note" caption is a plain span, so the textarea has no accessible name. */
function noteBody(editor: ReturnType<typeof within>) {
  return editor.getByRole("textbox", { name: "" });
}

function noteList() {
  return screen.getByRole("heading", { name: "Learning notes" }).closest("aside")!;
}

async function findNoteList() {
  return within((await screen.findByRole("heading", { name: "Learning notes" })).closest("aside")!);
}

beforeEach(() => {
  vi.useFakeTimers({ shouldAdvanceTime: true });
  vi.setSystemTime(new Date("2026-10-06T10:00:00Z"));
});

afterEach(() => {
  vi.useRealTimers();
});

describe("LearningNotesView – create note", () => {
  it("shows the empty states when there are no learning notes", async () => {
    const { api } = renderNotes({ api: notesBackend() });

    expect(await screen.findByRole("heading", { name: "No learning notes" })).toBeInTheDocument();
    expect(screen.getByText("Write a note and attach it to a topic.")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Choose a note" })).toBeInTheDocument();
    expect(api.sent("GET", "/api/notes")[0].query.get("learningOnly")).toBe("true");
    expect(api.unhandled).toEqual([]);
  });

  it("does not save a note without a title or without a topic", async () => {
    const { user, api } = renderNotes({ api: notesBackend() });
    const editor = await startNewNote(user);

    expect(editor.getByLabelText("Topic")).toHaveValue("topic-react");
    await user.type(noteBody(editor), "Draft without a title");
    await passAutosaveDelay();

    expect(api.sent("POST", "/api/notes")).toHaveLength(0);
    expect(editor.getByText("Kept on this device")).toBeInTheDocument();

    await user.selectOptions(editor.getByLabelText("Topic"), "Choose a topic");
    await user.type(editor.getByLabelText("Title"), "Context API");
    await passAutosaveDelay();

    expect(api.sent("POST", "/api/notes")).toHaveLength(0);
    expect(editor.getByText("Choose a topic to attach this note and its resources.")).toBeInTheDocument();
  });

  it("autosaves a titled note once, sends the full body and lists it after the refetch", async () => {
    const { user, api } = renderNotes({ api: notesBackend([note()]) });
    const editor = await startNewNote(user);

    await user.type(editor.getByLabelText("Title"), "  Suspense  ");
    await user.selectOptions(editor.getByLabelText("Topic"), "TypeScript");
    await user.type(editor.getByLabelText("Tags"), "react, async, ");
    await user.type(noteBody(editor), "Fallbacks render while loading");
    expect(api.sent("POST", "/api/notes")).toHaveLength(0);
    await passAutosaveDelay();

    await waitFor(() => expect(api.sent("POST", "/api/notes")).toHaveLength(1));
    expect(api.sent("POST", "/api/notes")[0].body).toEqual({
      title: "Suspense",
      content: "Fallbacks render while loading",
      category: "Learning",
      isFavorite: false,
      isPinned: false,
      projectId: null,
      taskId: null,
      meetingId: null,
      learningTopicId: "topic-ts",
      tags: ["react", "async"],
    });

    const list = within(noteList());
    expect(await list.findByRole("button", { name: /Suspense/ })).toHaveTextContent("TypeScript");
    expect(list.getByRole("button", { name: /Hooks recap/ })).toBeInTheDocument();
    expect(await screen.findByText(/Updated 6 Oct/)).toBeInTheDocument();
    expect(screen.getByLabelText("Title")).toHaveValue("Suspense");
    expect(screen.queryByText("Kept on this device")).not.toBeInTheDocument();

    await passAutosaveDelay();
    expect(api.sent("POST", "/api/notes")).toHaveLength(1);
    expect(api.sent("PUT", "/api/notes/:id")).toHaveLength(0);
  });

  it("surfaces a failed save and keeps the editor and its content", async () => {
    const { user, api } = renderNotes({
      api: { ...notesBackend(), "POST /api/notes": fail(400, "The title is too long.", "VALIDATION_FAILED") },
    });
    const editor = await startNewNote(user);

    await user.type(editor.getByLabelText("Title"), "Too long");
    await passAutosaveDelay();

    expect(await editor.findByText("The title is too long.")).toBeInTheDocument();
    expect(editor.getByText("Kept on this device")).toBeInTheDocument();
    expect(editor.getByLabelText("Title")).toHaveValue("Too long");
    expect(api.sent("POST", "/api/notes")).toHaveLength(1);
    expect(api.sent("GET", "/api/notes")).toHaveLength(1);
    expect(screen.getByRole("heading", { name: "No learning notes" })).toBeInTheDocument();
  });

  // Known bug: when the "new" editor unmounts, useAutosave's cleanup flush rewrites the
  // learning-note.new draft that the save handler just cleared. Update this test when it is fixed.
  it("known bug: pressing New after creating a note restores that note and autosaves a duplicate", async () => {
    const { user, api } = renderNotes({ api: notesBackend() });
    const first = await startNewNote(user);
    await user.type(first.getByLabelText("Title"), "First note");
    await passAutosaveDelay();
    await within(noteList()).findByRole("button", { name: /First note/ });
    expect(api.sent("POST", "/api/notes")).toHaveLength(1);

    const second = await startNewNote(user);
    expect(second.getByLabelText("Title")).toHaveValue("First note");
    await passAutosaveDelay();

    await waitFor(() => expect(within(noteList()).getAllByRole("button", { name: /First note/ })).toHaveLength(2));
    const posts = api.sent("POST", "/api/notes");
    expect(posts).toHaveLength(2);
    expect(posts[1].body).toEqual(posts[0].body);
  });

  it("shows the error state when the list fails and refetches on retry", async () => {
    const backend = notesBackend([note()]);
    const { user, api } = renderNotes({ api: { ...backend, "GET /api/notes": fail(500, "Notes are offline.", "SERVER_ERROR") } });

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent("Learning notes are unavailable");
    expect(alert).toHaveTextContent("Notes are offline.");

    api.set({ "GET /api/notes": backend["GET /api/notes"] });
    await user.click(within(alert).getByRole("button", { name: "Try again" }));

    expect(await (await findNoteList()).findByRole("button", { name: /Hooks recap/ })).toBeInTheDocument();
    expect(api.sent("GET", "/api/notes")).toHaveLength(2);
  });

  it("lets members with notes.manage create notes but not manage topic resources", async () => {
    const { user } = renderNotes({ user: memberUser, api: notesBackend() });
    const editor = await startNewNote(user);

    expect(editor.getByLabelText("Title")).toBeEnabled();
    expect(await editor.findByText("No resources yet.")).toBeInTheDocument();
    expect(editor.queryByLabelText("Resource")).not.toBeInTheDocument();
  });

  it("shows notes read-only to users without notes.manage", async () => {
    const { user, api } = renderNotes({ user: readOnlyUser, api: notesBackend([note()]) });

    await user.click(await (await findNoteList()).findByRole("button", { name: /Hooks recap/ }));

    expect(screen.queryByRole("button", { name: "New" })).not.toBeInTheDocument();
    const editor = within(screen.getByLabelText("Title").closest("section")!);
    expect(editor.getByLabelText("Title")).toBeDisabled();
    expect(editor.getByLabelText("Title")).toHaveValue("Hooks recap");
    expect(editor.getByLabelText("Topic")).toBeDisabled();
    expect(noteBody(editor)).toBeDisabled();
    expect(editor.getByRole("button", { name: "Favorite" })).toBeDisabled();
    expect(editor.queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
    await passAutosaveDelay();
    expect(api.sent("PUT", "/api/notes/:id")).toHaveLength(0);
  });
});
