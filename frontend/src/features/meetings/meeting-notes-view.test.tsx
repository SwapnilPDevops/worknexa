import { act, fireEvent, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { permissions } from "../../auth/access.ts";
import type { UserSession } from "../../auth/session.ts";
import { fail } from "../../test/api.ts";
import { memberUser, renderWithProviders } from "../../test/render.tsx";
import { localDateTime, type MeetingNoteAction, type MeetingNoteDocument, type MeetingNoteListItem, type MeetingNoteWrite } from "./meeting-notes-api.ts";
import { MeetingNotesView } from "./meeting-notes-view.tsx";

const meetingId = "33333333-3333-3333-3333-333333333333";
const otherMeetingId = "55555555-5555-5555-5555-555555555555";
const notePath = `/api/meeting-notes/${meetingId}`;
const draftKey = `worknexa.draft.meeting-note.${meetingId}`;

const workOptions = {
  projects: [{ id: "project-1", name: "Apollo" }],
  people: [{ id: "person-1", name: "Priya Patel" }, { id: "person-2", name: "Omar Haddad" }],
  teams: [],
  tags: [],
};

const meetings: MeetingNoteListItem[] = [
  { id: meetingId, title: "Sprint planning", startAt: "2026-10-06T10:00:00", updatedAt: "2026-10-06T09:00:00Z" },
  { id: otherMeetingId, title: "Design review", startAt: "2026-10-07T15:30:00", updatedAt: "2026-10-06T09:00:00Z" },
];

const openAction: MeetingNoteAction = {
  id: "action-1",
  title: "Draft the budget",
  description: null,
  assigneeId: "person-1",
  assigneeName: "Priya Patel",
  dueAt: null,
  isCompleted: false,
  taskId: null,
  taskTitle: null,
};

const convertedAction: MeetingNoteAction = {
  ...openAction,
  id: "action-2",
  title: "Book the venue",
  assigneeId: null,
  assigneeName: null,
  taskId: "task-9",
  taskTitle: "Book the venue",
};

function noteDocument(overrides: Partial<MeetingNoteDocument> = {}): MeetingNoteDocument {
  return {
    id: meetingId,
    title: "Sprint planning",
    startAt: "2026-10-06T10:00:00",
    projectId: "project-1",
    projectName: "Apollo",
    agenda: null,
    discussion: null,
    decisions: null,
    followUps: null,
    followUpAt: null,
    actions: [],
    updatedAt: "2026-10-06T09:00:00Z",
    ...overrides,
  };
}

/** A tiny in-memory backend: PUT stores the write (assigning ids to new actions) and convert links a task. */
function meetingNotesServer(initial: MeetingNoteDocument) {
  let current = initial;
  let nextId = 0;
  return {
    "GET /api/work/options": workOptions,
    "GET /api/meeting-notes/:id": () => current,
    "GET /api/meeting-notes": meetings,
    "PUT /api/meeting-notes/:id": ({ body }: { body: unknown }) => {
      const write = body as MeetingNoteWrite;
      current = {
        ...current,
        ...write,
        actions: write.actions.map((action) => ({
          ...action,
          id: action.id ?? `saved-${++nextId}`,
          assigneeName: workOptions.people.find((person) => person.id === action.assigneeId)?.name ?? null,
          taskId: current.actions.find((existing) => existing.id === action.id)?.taskId ?? null,
          taskTitle: current.actions.find((existing) => existing.id === action.id)?.taskTitle ?? null,
        })),
      };
      return current;
    },
    "POST /api/meeting-notes/:id/actions/:actionId/task": ({ params }: { params: Record<string, string> }) => {
      current = {
        ...current,
        actions: current.actions.map((action) => (action.id === params.actionId ? { ...action, taskId: "task-42", taskTitle: action.title } : action)),
      };
      return current;
    },
  };
}

function renderNotes(options: { note?: MeetingNoteDocument; route?: string; user?: UserSession; api?: Record<string, unknown> } = {}) {
  const { note = noteDocument(), route = `/meeting-notes?meeting=${meetingId}`, user, api = {} } = options;
  const result = renderWithProviders(<MeetingNotesView />, {
    route,
    path: "/meeting-notes",
    user,
    api: { ...meetingNotesServer(note), ...api },
  });
  return { ...result, user: userEvent.setup({ advanceTimers: vi.advanceTimersByTime }) };
}

/** The markdown editors render a visual label only, so the textarea is located by its id. */
function editor(id: string) {
  const element = document.getElementById(id);
  if (!(element instanceof HTMLTextAreaElement)) {
    throw new Error(`No markdown editor with id "${id}"`);
  }
  return element;
}

async function flushAutosave() {
  await act(() => vi.advanceTimersByTimeAsync(900));
}

async function openEditor() {
  return screen.findByRole("heading", { level: 2, name: "Sprint planning" });
}

describe("MeetingNotesView", () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe("list and selection", () => {
    it("lists meetings and opens the chosen one, reflecting it in the URL", async () => {
      const { user, api } = renderNotes({ route: "/meeting-notes", note: noteDocument({ agenda: "Review the board", actions: [openAction] }) });

      expect(await screen.findByRole("heading", { level: 1, name: "Choose a meeting" })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /Design review/ })).toBeInTheDocument();

      await user.click(screen.getByRole("button", { name: /Sprint planning/ }));

      expect(await openEditor()).toBeInTheDocument();
      expect(screen.getByText(/· Apollo/)).toBeInTheDocument();
      expect(editor("agenda")).toHaveValue("Review the board");
      expect(screen.getByRole("textbox", { name: "Action title" })).toHaveValue("Draft the budget");
      expect(screen.getByRole("combobox", { name: "Assignee" })).toHaveValue("person-1");
      expect(screen.getByTestId("location")).toHaveTextContent(`/meeting-notes?meeting=${meetingId}`);
      expect(api.sent("GET", notePath)).toHaveLength(1);
      expect(api.unhandled).toEqual([]);
    });

    it("shows the empty state when there are no meetings", async () => {
      renderNotes({ route: "/meeting-notes", api: { "GET /api/meeting-notes": [] } });

      expect(await screen.findByRole("heading", { name: "No meetings" })).toBeInTheDocument();
    });

    it("searches the list with a debounced q parameter", async () => {
      const { user, api } = renderNotes({
        route: "/meeting-notes",
        api: { "GET /api/meeting-notes": ({ query }: { query: URLSearchParams }) => (query.get("q") === "design" ? [meetings[1]] : meetings) },
      });

      await user.click(await screen.findByRole("textbox", { name: "Search meeting notes" }));
      await user.paste("  design ");
      expect(api.sent("GET", "/api/meeting-notes").some((call) => call.query.has("q"))).toBe(false);
      await act(() => vi.advanceTimersByTimeAsync(300));

      await waitFor(() => expect(screen.queryByRole("button", { name: /Sprint planning/ })).not.toBeInTheDocument());
      expect(screen.getByRole("button", { name: /Design review/ })).toBeInTheDocument();
      const searches = api.sent("GET", "/api/meeting-notes").filter((call) => call.query.has("q"));
      expect(searches.map((call) => call.query.get("q"))).toEqual(["design"]);
    });

    it("shows the error state with the server message when the list fails, and recovers on retry", async () => {
      const { user, api } = renderNotes({ api: { "GET /api/meeting-notes": fail(500, "Meeting service is down.") } });

      const alert = await screen.findByRole("alert");
      expect(alert).toHaveTextContent("Meeting notes are unavailable");
      expect(alert).toHaveTextContent("Meeting service is down.");

      api.set({ "GET /api/meeting-notes": meetings });
      await user.click(within(alert).getByRole("button", { name: "Try again" }));

      expect(await screen.findByRole("button", { name: /Design review/ })).toBeInTheDocument();
      expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    });
  });

  describe("capturing notes", () => {
    it("does not save a note that was only opened", async () => {
      const { api } = renderNotes({ note: noteDocument({ agenda: "Existing agenda", actions: [openAction] }) });

      await openEditor();
      await act(() => vi.advanceTimersByTimeAsync(2000));

      expect(api.sent("PUT", notePath)).toHaveLength(0);
    });

    it("autosaves every section and new action items with a trimmed, normalized body", async () => {
      const { user, api } = renderNotes();
      await openEditor();

      // Edits are applied synchronously so the 800ms autosave debounce cannot fire between them.
      fireEvent.change(editor("agenda"), { target: { value: "Review **Q4** goals  " } });
      fireEvent.change(editor("discussion"), { target: { value: "Budget is tight." } });
      fireEvent.change(editor("decisions"), { target: { value: "   " } });
      fireEvent.change(editor("follow-ups"), { target: { value: "- [ ] Send recap" } });
      fireEvent.change(screen.getByLabelText("Follow-up date"), { target: { value: "2026-10-13T09:00" } });
      fireEvent.click(screen.getByRole("button", { name: "Add" }));
      fireEvent.click(screen.getByRole("button", { name: "Add" }));
      const [first] = screen.getAllByRole("textbox", { name: "Action title" });
      fireEvent.change(first, { target: { value: "  Book venue " } });
      fireEvent.change(screen.getAllByRole("textbox", { name: "Action description" })[0], { target: { value: " Two rooms " } });
      fireEvent.change(screen.getAllByRole("combobox", { name: "Assignee" })[0], { target: { value: "person-2" } });
      fireEvent.change(screen.getAllByLabelText("Due")[0], { target: { value: "2026-10-10T09:30" } });
      fireEvent.click(screen.getAllByRole("checkbox", { name: "Done" })[0]);

      expect(screen.getByText("Saving…")).toBeInTheDocument();
      expect(api.sent("PUT", notePath)).toHaveLength(0);
      await flushAutosave();

      await waitFor(() => expect(api.sent("PUT", notePath)).toHaveLength(1));
      const [request] = api.sent("PUT", notePath);
      expect(request.body).toEqual({
        agenda: "Review **Q4** goals",
        discussion: "Budget is tight.",
        decisions: null,
        followUps: "- [ ] Send recap",
        followUpAt: localDateTime("2026-10-13T09:00"),
        actions: [
          {
            id: null,
            title: "Book venue",
            description: "Two rooms",
            assigneeId: "person-2",
            dueAt: localDateTime("2026-10-10T09:30"),
            isCompleted: true,
          },
        ],
      });
      expect(request.headers.get("X-Worknexa-Request")).toBe("1");
      expect(await screen.findByText("Saved")).toBeInTheDocument();

      await user.click(screen.getAllByRole("button", { name: "Convert to task" })[0]);
      await waitFor(() => expect(api.sent("POST", `${notePath}/actions/saved-1/task`)).toHaveLength(1));
    });

    it("refuses to convert an action item that has not been saved yet", async () => {
      const { user, api } = renderNotes();
      await openEditor();

      await user.click(screen.getByRole("button", { name: "Add" }));
      await user.click(screen.getByRole("textbox", { name: "Action title" }));
      await user.paste("Call the vendor");
      await user.click(screen.getByRole("button", { name: "Convert to task" }));

      expect(await screen.findByText("Save the action item before converting it.")).toBeInTheDocument();
      expect(api.calls.filter((call) => call.method === "POST")).toHaveLength(0);
    });

    it("surfaces the server message when saving fails and keeps the draft on the device", async () => {
      const { user, api } = renderNotes({ api: { "PUT /api/meeting-notes/:id": fail(400, "Agenda is too long.", "VALIDATION_FAILED") } });
      await openEditor();

      await user.click(editor("agenda"));
      await user.paste("A very long agenda");
      await flushAutosave();

      expect(await screen.findByText("Agenda is too long.")).toBeInTheDocument();
      expect(await screen.findByText("Kept on this device")).toBeInTheDocument();
      expect(api.sent("PUT", notePath).length).toBeGreaterThan(0);
      expect(JSON.parse(localStorage.getItem(draftKey)!).value.agenda).toBe("A very long agenda");
      expect(editor("agenda")).toHaveValue("A very long agenda");
    });

    it("restores an unsaved local draft and saves it", async () => {
      const server = noteDocument({ agenda: "Server agenda" });
      localStorage.setItem(draftKey, JSON.stringify({
        savedAt: Date.now(),
        value: { agenda: "Offline agenda", discussion: "", decisions: "", followUps: "", followUpAt: "", actions: [] },
      }));
      const { api } = renderNotes({ note: server });
      await openEditor();

      expect(editor("agenda")).toHaveValue("Offline agenda");
      await flushAutosave();

      await waitFor(() => expect(api.sent("PUT", notePath)).toHaveLength(1));
      expect(api.sent("PUT", notePath)[0].body).toMatchObject({ agenda: "Offline agenda", actions: [] });
      expect(await screen.findByText("Saved")).toBeInTheDocument();
    });

    it("ignores a local draft that matches the server copy", async () => {
      localStorage.setItem(draftKey, JSON.stringify({
        savedAt: Date.now(),
        value: { agenda: "Server agenda", discussion: "", decisions: "", followUps: "", followUpAt: "", actions: [] },
      }));
      const { api } = renderNotes({ note: noteDocument({ agenda: "Server agenda" }) });
      await openEditor();

      await act(() => vi.advanceTimersByTimeAsync(2000));
      expect(api.sent("PUT", notePath)).toHaveLength(0);
      expect(editor("agenda")).toHaveValue("Server agenda");
    });
  });

  describe("converting an action item to a task", () => {
    it("posts to the convert endpoint and replaces the button with a link to the task", async () => {
      const { user, api } = renderNotes({ note: noteDocument({ actions: [openAction] }) });
      await openEditor();

      await user.click(screen.getByRole("button", { name: "Convert to task" }));

      const link = await screen.findByRole("link", { name: "Open task: Draft the budget" });
      expect(link).toHaveAttribute("href", "/tasks?task=task-42");
      expect(screen.queryByRole("button", { name: "Convert to task" })).not.toBeInTheDocument();
      const [request] = api.sent("POST", `${notePath}/actions/action-1/task`);
      expect(request.body).toBeNull();
      expect(request.headers.get("X-Worknexa-Request")).toBe("1");

      await act(() => vi.advanceTimersByTimeAsync(2000));
      expect(api.sent("PUT", notePath)).toHaveLength(0);

      await user.click(link);
      expect(screen.getByTestId("location")).toHaveTextContent("/tasks?task=task-42");
    });

    it("surfaces the server message when the conversion fails", async () => {
      const { user } = renderNotes({
        note: noteDocument({ actions: [openAction] }),
        api: { "POST /api/meeting-notes/:id/actions/:actionId/task": fail(409, "This action item already has a task.", "CONFLICT") },
      });
      await openEditor();

      await user.click(screen.getByRole("button", { name: "Convert to task" }));

      expect(await screen.findByText("This action item already has a task.")).toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Convert to task" })).toBeInTheDocument();
      expect(screen.queryByRole("link", { name: /Open task/ })).not.toBeInTheDocument();
    });

    it("falls back to a generic message when the network fails", async () => {
      const { user, api } = renderNotes({ note: noteDocument({ actions: [openAction] }) });
      await openEditor();
      api.set({ "POST /api/meeting-notes/:id/actions/:actionId/task": () => { throw new TypeError("Failed to fetch"); } });

      await user.click(screen.getByRole("button", { name: "Convert to task" }));

      expect(await screen.findByText("The task could not be created.")).toBeInTheDocument();
    });
  });

  describe("authorization", () => {
    it("hides Convert to task for members without task management but still links existing tasks", async () => {
      expect(memberUser.permissions).not.toContain(permissions.tasksManage);
      renderNotes({ user: memberUser, note: noteDocument({ actions: [openAction, convertedAction] }) });
      await openEditor();

      expect(screen.queryByRole("button", { name: "Convert to task" })).not.toBeInTheDocument();
      expect(screen.getByRole("link", { name: "Open task: Book the venue" })).toHaveAttribute("href", "/tasks?task=task-9");
      expect(screen.getAllByRole("textbox", { name: "Action title" })).toHaveLength(2);
      expect(editor("agenda")).toBeEnabled();
    });

    it("shows Convert to task only for actions without a task when the user can manage tasks", async () => {
      renderNotes({ note: noteDocument({ actions: [openAction, convertedAction] }) });
      await openEditor();

      expect(screen.getAllByRole("button", { name: "Convert to task" })).toHaveLength(1);
      expect(screen.getByRole("link", { name: "Open task: Book the venue" })).toBeInTheDocument();
    });

    it("denies access to users without meetings permission", async () => {
      const noMeetings: UserSession = { ...memberUser, permissions: memberUser.permissions.filter((permission) => permission !== permissions.meetingsRead) };
      const { api } = renderNotes({ user: noMeetings });

      await waitFor(() => expect(api.sent("GET", "/api/auth/me")).toHaveLength(1));
      const alert = await screen.findByRole("alert");
      await act(() => vi.advanceTimersByTimeAsync(50));
      expect(alert).toHaveTextContent("Access denied");
      expect(screen.queryByRole("heading", { name: "Meeting notes" })).not.toBeInTheDocument();
    });
  });
});
