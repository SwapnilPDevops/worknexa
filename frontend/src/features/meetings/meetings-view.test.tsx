import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { fail } from "../../test/api.ts";
import { memberUser, renderWithProviders } from "../../test/render.tsx";
import type { MeetingDetail, MeetingListItem } from "./api.ts";
import { MeetingsView } from "./meetings-view.tsx";

const options = {
  projects: [{ id: "project-1", name: "Atlas" }],
  people: [
    { id: "person-1", name: "Ada Admin" },
    { id: "person-2", name: "Max Member" },
  ],
  teams: [{ id: "team-1", name: "Platform" }],
  tags: [],
};

function listItem(overrides: Partial<MeetingListItem> = {}): MeetingListItem {
  return {
    id: "meeting-1",
    title: "Sprint review",
    meetingType: "Review",
    startAt: "2026-10-06T09:00:00Z",
    endAt: "2026-10-06T09:30:00Z",
    durationMinutes: 30,
    location: "Room 4",
    meetingLink: null,
    projectId: null,
    projectName: null,
    teamId: null,
    teamName: null,
    participants: [],
    ...overrides,
  };
}

function detail(overrides: Partial<MeetingDetail> = {}): MeetingDetail {
  return {
    ...listItem(),
    agenda: null,
    notes: null,
    decisions: null,
    followUpAt: null,
    actionItems: [],
    editable: true,
    ...overrides,
  };
}

const created = detail({
  id: "meeting-new",
  title: "Client kickoff",
  meetingType: "Client",
  startAt: "2026-10-07T04:00:00Z",
  endAt: "2026-10-07T04:45:00Z",
  durationMinutes: 45,
  location: "Board room",
  projectId: "project-1",
  projectName: "Atlas",
  teamId: "team-1",
  teamName: "Platform",
  participants: [{ id: "person-2", name: "Max Member" }],
  agenda: "Scope and timeline",
  actionItems: [{ id: "action-1", title: "Send proposal", assigneeId: "person-1", assigneeName: "Ada Admin", isCompleted: false, dueAt: null }],
});

function renderMeetings(options_: Parameters<typeof renderWithProviders>[1] = {}) {
  return renderWithProviders(<MeetingsView />, {
    route: "/meetings",
    ...options_,
    api: {
      "GET /api/work/options": options,
      "GET /api/meetings": [listItem()],
      ...options_.api,
    },
  });
}

type RejectionListener = (reason: unknown) => void;
type NodeProcess = {
  listeners(event: "unhandledRejection"): RejectionListener[];
  removeAllListeners(event: "unhandledRejection"): void;
  on(event: "unhandledRejection", listener: RejectionListener): void;
  off(event: "unhandledRejection", listener: RejectionListener): void;
};

// The view submits with `void save.mutateAsync(...)`, so a failed save also rejects unhandled; collect it instead of failing the run.
function captureUnhandledRejections() {
  const nodeProcess = (globalThis as unknown as { process: NodeProcess }).process;
  const previous = nodeProcess.listeners("unhandledRejection");
  const reasons: unknown[] = [];
  const listener: RejectionListener = (reason) => reasons.push(reason);
  nodeProcess.removeAllListeners("unhandledRejection");
  nodeProcess.on("unhandledRejection", listener);
  return {
    reasons,
    restore() {
      nodeProcess.off("unhandledRejection", listener);
      previous.forEach((item) => nodeProcess.on("unhandledRejection", item));
    },
  };
}

async function fill(user: ReturnType<typeof renderMeetings>["user"], field: HTMLElement, text: string) {
  await user.clear(field);
  await user.click(field);
  await user.paste(text);
}

async function openCreateDrawer(user: ReturnType<typeof renderMeetings>["user"]) {
  await user.click(await screen.findByRole("button", { name: "New meeting" }));
  return screen.getByRole("dialog", { name: "New meeting" });
}

describe("MeetingsView: create meeting", () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true, toFake: ["Date"] });
    vi.setSystemTime(new Date(2026, 9, 6, 10, 0, 0));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe("form", () => {
    it("blocks submission while the required title is missing", async () => {
      const { user, api } = renderMeetings();
      const dialog = await openCreateDrawer(user);

      const title = within(dialog).getByLabelText("Title");
      expect(title).toBeRequired();
      await user.click(within(dialog).getByRole("button", { name: "Create meeting" }));

      expect(title).toBeInvalid();
      expect((title as HTMLInputElement).validity.valueMissing).toBe(true);
      expect(api.sent("POST", "/api/meetings")).toHaveLength(0);
      expect(screen.getByRole("dialog", { name: "New meeting" })).toBeInTheDocument();
    });

    it("blocks submission when the duration is cleared or below five minutes", async () => {
      const { user, api } = renderMeetings();
      const dialog = await openCreateDrawer(user);

      await user.type(within(dialog).getByLabelText("Title"), "Quick sync");
      const duration = within(dialog).getByLabelText(/^Duration/);
      await user.clear(duration);
      await user.click(within(dialog).getByRole("button", { name: "Create meeting" }));
      expect((duration as HTMLInputElement).validity.valueMissing).toBe(true);

      await user.type(duration, "3");
      await user.click(within(dialog).getByRole("button", { name: "Create meeting" }));
      expect((duration as HTMLInputElement).validity.rangeUnderflow).toBe(true);

      expect(api.sent("POST", "/api/meetings")).toHaveLength(0);
    });

    it("defaults the start to the viewed day and the type to Team", async () => {
      const { user, api } = renderMeetings();
      const dialog = await openCreateDrawer(user);

      expect(within(dialog).getByLabelText("Date and time")).toHaveValue("2026-10-06T10:00");
      expect(within(dialog).getByLabelText("Type")).toHaveValue("Team");
      expect(within(dialog).getByLabelText(/^Duration/)).toHaveValue(30);
      const [listRequest] = api.sent("GET", "/api/meetings");
      expect(listRequest.query.get("view")).toBe("today");
      expect(listRequest.query.get("date")).toBe("2026-10-06");
    });

    it("sends the full meeting, then shows it in the list and opens the saved meeting", async () => {
      const { user, api } = renderMeetings({
        api: {
          "POST /api/meetings": created,
          "GET /api/meetings/:id": ({ params }: { params: Record<string, string> }) => (params.id === created.id ? created : fail(404, "Not found", "NOT_FOUND")),
        },
      });
      expect(await screen.findByText("Sprint review")).toBeInTheDocument();
      const dialog = await openCreateDrawer(user);

      await fill(user, within(dialog).getByLabelText("Title"), "  Client kickoff  ");
      await user.selectOptions(within(dialog).getByLabelText("Type"), "Client");
      await fill(user, within(dialog).getByLabelText(/^Duration/), "45");
      fireEvent.change(within(dialog).getByLabelText("Date and time"), { target: { value: "2026-10-07T09:30" } });
      await user.selectOptions(within(dialog).getByLabelText("Project"), "Atlas");
      await user.selectOptions(within(dialog).getByLabelText("Team"), "Platform");
      await fill(user, within(dialog).getByLabelText("Location"), "Board room");
      await user.selectOptions(within(dialog).getByLabelText("Participants"), ["Max Member"]);
      await fill(user, within(dialog).getByLabelText("Agenda"), "Scope and timeline");
      await user.click(within(dialog).getByRole("button", { name: "Add" }));
      await fill(user, within(dialog).getByLabelText("Action title"), "Send proposal");
      await user.selectOptions(within(dialog).getByLabelText("Assignee"), "Ada Admin");

      api.set({ "GET /api/meetings": [listItem(), created] });
      await user.click(within(dialog).getByRole("button", { name: "Create meeting" }));

      await waitFor(() => expect(api.sent("POST", "/api/meetings")).toHaveLength(1));
      const [request] = api.sent("POST", "/api/meetings");
      expect(request.body).toEqual({
        title: "Client kickoff",
        meetingType: "Client",
        startAt: expect.stringMatching(/^2026-10-07T09:30:00[+-]\d{2}:\d{2}$/),
        durationMinutes: 45,
        projectId: "project-1",
        teamId: "team-1",
        location: "Board room",
        meetingLink: null,
        agenda: "Scope and timeline",
        notes: null,
        decisions: null,
        followUpAt: null,
        participantIds: ["person-2"],
        actionItems: [{ id: null, title: "Send proposal", assigneeId: "person-1", dueAt: null, isCompleted: false }],
      });

      const list = await screen.findByRole("list");
      expect(await within(list).findByText("Client kickoff")).toBeInTheDocument();
      expect(within(list).getByText("Client")).toBeInTheDocument();
      expect(await screen.findByRole("dialog", { name: "Client kickoff" })).toBeInTheDocument();
      expect(screen.queryByRole("dialog", { name: "New meeting" })).not.toBeInTheDocument();
      expect(screen.getByRole("button", { name: "Save changes" })).toBeInTheDocument();
      await waitFor(() => expect(screen.getByTestId("location")).toHaveTextContent("/meetings?meeting=meeting-new"));
      expect(api.sent("GET", "/api/meetings").length).toBeGreaterThanOrEqual(2);
    }, 30000);

    it("drops blank action items from the request", async () => {
      const { user, api } = renderMeetings({ api: { "POST /api/meetings": created, "GET /api/meetings/:id": created } });
      const dialog = await openCreateDrawer(user);

      await user.type(within(dialog).getByLabelText("Title"), "Planning");
      await user.click(within(dialog).getByRole("button", { name: "Add" }));
      await user.click(within(dialog).getByRole("button", { name: "Create meeting" }));

      await waitFor(() => expect(api.sent("POST", "/api/meetings")).toHaveLength(1));
      expect(api.sent("POST", "/api/meetings")[0].body).toMatchObject({ title: "Planning", meetingType: "Team", durationMinutes: 30, actionItems: [] });
    });
  });

  describe("error states", () => {
    it("shows the error state when the list fails and refetches on retry", async () => {
      const { user, api } = renderMeetings({ api: { "GET /api/meetings": fail(500, "Meetings service is down", "SERVER_ERROR") } });

      expect(await screen.findByText("Meetings are unavailable")).toBeInTheDocument();
      const alert = screen.getByRole("alert");
      expect(alert).toHaveTextContent("Meetings service is down");
      expect(api.sent("GET", "/api/meetings")).toHaveLength(1);

      api.set({ "GET /api/meetings": [listItem()] });
      await user.click(within(alert).getByRole("button", { name: "Try again" }));

      expect(await screen.findByText("Sprint review")).toBeInTheDocument();
      expect(screen.queryByRole("alert")).not.toBeInTheDocument();
      expect(api.sent("GET", "/api/meetings")).toHaveLength(2);
    });

    it("shows the server validation message and keeps the form open when saving fails", async () => {
      const { user, api } = renderMeetings({
        api: { "POST /api/meetings": fail(400, "Duration must be between 5 minutes and 24 hours.", "VALIDATION_FAILED") },
      });
      const dialog = await openCreateDrawer(user);
      const rejections = captureUnhandledRejections();
      try {
        await user.type(within(dialog).getByLabelText("Title"), "Too long");
        await user.click(within(dialog).getByRole("button", { name: "Create meeting" }));

        expect(await within(dialog).findByText("Duration must be between 5 minutes and 24 hours.")).toBeInTheDocument();
        expect(screen.getByRole("dialog", { name: "New meeting" })).toBeInTheDocument();
        expect(within(dialog).getByLabelText("Title")).toHaveValue("Too long");
        expect(within(dialog).getByRole("button", { name: "Create meeting" })).toBeEnabled();
        expect(api.sent("POST", "/api/meetings")).toHaveLength(1);
        expect(screen.getByTestId("location")).toHaveTextContent(/^\/meetings$/);
        await waitFor(() => expect(rejections.reasons).toHaveLength(1));
        expect(rejections.reasons[0]).toMatchObject({ status: 400, code: "VALIDATION_FAILED" });
      } finally {
        rejections.restore();
      }
    });
  });

  describe("authorization", () => {
    it("hides the create control from users without meetings.manage", async () => {
      renderMeetings({ user: memberUser });

      expect(await screen.findByText("Sprint review")).toBeInTheDocument();
      expect(screen.getByRole("heading", { name: "Meetings" })).toBeInTheDocument();
      expect(screen.queryByRole("button", { name: "New meeting" })).not.toBeInTheDocument();
    });

    it("opens meetings read-only for users without meetings.manage", async () => {
      const { user } = renderMeetings({
        user: memberUser,
        api: { "GET /api/meetings/:id": detail({ editable: false }) },
      });

      await user.click(await screen.findByRole("button", { name: /Sprint review/ }));

      const dialog = await screen.findByRole("dialog", { name: "Sprint review" });
      await waitFor(() => expect(within(dialog).getByLabelText("Title")).toHaveValue("Sprint review"));
      expect(within(dialog).getByLabelText("Title")).toBeDisabled();
      expect(within(dialog).getByLabelText("Type")).toBeDisabled();
      expect(within(dialog).queryByRole("button", { name: "Save changes" })).not.toBeInTheDocument();
      expect(within(dialog).queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
      expect(within(dialog).queryByRole("button", { name: "Add" })).not.toBeInTheDocument();
    });

    it("denies access to users without meetings.read", async () => {
      renderMeetings({ user: { ...memberUser, permissions: [] } });

      await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("Access denied"));
      await new Promise((resolve) => setTimeout(resolve, 50));
      expect(screen.getByRole("alert")).toHaveTextContent("Your account does not include meetings.");
      expect(screen.queryByRole("heading", { name: "Meetings" })).not.toBeInTheDocument();
    });
  });

  describe("empty state", () => {
    it("shows the empty state when there are no meetings and still allows creating one", async () => {
      renderMeetings({ api: { "GET /api/meetings": [] } });

      expect(await screen.findByRole("heading", { name: "Nothing on this view" })).toBeInTheDocument();
      expect(screen.queryByRole("list")).not.toBeInTheDocument();
      expect(screen.getByRole("button", { name: "New meeting" })).toBeEnabled();
    });
  });
});
