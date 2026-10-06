import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ReminderItem } from "../schedule/api.ts";
import { fail } from "../../test/api.ts";
import { memberUser, renderWithProviders } from "../../test/render.tsx";
import { RemindersView } from "./reminders-view.tsx";

const links = {
  tasks: [{ id: "task-1", name: "Ship release notes" }],
  meetings: [{ id: "meeting-1", name: "Sprint review" }],
};

function reminder(overrides: Partial<ReminderItem> = {}): ReminderItem {
  return {
    id: "reminder-1",
    title: "Water the plants",
    description: null,
    remindAt: "2026-10-06T12:00:00Z",
    recurrence: "None",
    recurrenceInterval: null,
    priority: "Medium",
    isCompleted: false,
    taskId: null,
    taskTitle: null,
    meetingId: null,
    meetingTitle: null,
    calendarEventId: null,
    ...overrides,
  };
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

function renderReminders(options: Parameters<typeof renderWithProviders>[1] = {}) {
  return renderWithProviders(<RemindersView />, {
    route: "/reminders",
    ...options,
    api: {
      "GET /api/reminders/links": links,
      "GET /api/reminders": [reminder()],
      ...options.api,
    },
  });
}

async function fill(user: ReturnType<typeof renderReminders>["user"], field: HTMLElement, text: string) {
  await user.clear(field);
  await user.click(field);
  await user.paste(text);
}

async function openCreateDrawer(user: ReturnType<typeof renderReminders>["user"]) {
  await user.click(await screen.findByRole("button", { name: "More options" }));
  return screen.getByRole("dialog", { name: "New reminder" });
}

describe("RemindersView: create reminder", () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true, toFake: ["Date"] });
    vi.setSystemTime(new Date(2026, 9, 6, 10, 0, 0));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe("form", () => {
    it("blocks submission while the required title is missing", async () => {
      const { user, api } = renderReminders();
      const dialog = await openCreateDrawer(user);

      const title = within(dialog).getByLabelText("Title");
      expect(title).toBeRequired();
      await user.click(within(dialog).getByRole("button", { name: "Create reminder" }));

      expect(title).toBeInvalid();
      expect((title as HTMLInputElement).validity.valueMissing).toBe(true);
      expect(api.sent("POST", "/api/reminders")).toHaveLength(0);
      expect(screen.getByRole("dialog", { name: "New reminder" })).toBeInTheDocument();
    });

    it("blocks submission when the reminder time is cleared", async () => {
      const { user, api } = renderReminders();
      const dialog = await openCreateDrawer(user);

      await user.type(within(dialog).getByLabelText("Title"), "Call the bank");
      const when = within(dialog).getByLabelText("When");
      fireEvent.change(when, { target: { value: "" } });
      await user.click(within(dialog).getByRole("button", { name: "Create reminder" }));

      expect((when as HTMLInputElement).validity.valueMissing).toBe(true);
      expect(api.sent("POST", "/api/reminders")).toHaveLength(0);
    });

    it("keeps quick add disabled until a non-blank title is typed", async () => {
      const { user, api } = renderReminders();

      const add = await screen.findByRole("button", { name: "Add" });
      expect(add).toBeDisabled();
      await user.type(screen.getByLabelText("Reminder title"), "   ");
      expect(add).toBeDisabled();
      await user.click(add);
      expect(api.sent("POST", "/api/reminders")).toHaveLength(0);
    });

    it("defaults a new reminder to one hour from now, one-time, medium priority", async () => {
      const { user } = renderReminders();
      const dialog = await openCreateDrawer(user);

      expect(within(dialog).getByLabelText("When")).toHaveValue("2026-10-06T11:00");
      expect(within(dialog).getByLabelText("Repeats")).toHaveValue("None");
      expect(within(dialog).getByLabelText("Priority")).toHaveValue("Medium");
      expect(within(dialog).queryByLabelText(/^Every/)).not.toBeInTheDocument();
    });

    it("sends the full reminder, closes the drawer and lists the new reminder after the refetch", async () => {
      const saved = reminder({
        id: "reminder-new",
        title: "Prepare demo",
        description: "Laptop and slides",
        remindAt: new Date(2026, 9, 8, 9, 15).toISOString(),
        recurrence: "Custom",
        recurrenceInterval: 3,
        priority: "High",
        taskId: "task-1",
        taskTitle: "Ship release notes",
        meetingId: "meeting-1",
        meetingTitle: "Sprint review",
      });
      const { user, api } = renderReminders({ api: { "POST /api/reminders": saved } });
      expect(await screen.findByText("Water the plants")).toBeInTheDocument();
      const dialog = await openCreateDrawer(user);

      await fill(user, within(dialog).getByLabelText("Title"), "Prepare demo");
      await fill(user, within(dialog).getByLabelText("Description"), "Laptop and slides");
      fireEvent.change(within(dialog).getByLabelText("When"), { target: { value: "2026-10-08T09:15" } });
      await user.selectOptions(within(dialog).getByLabelText("Priority"), "High");
      await user.selectOptions(within(dialog).getByLabelText("Repeats"), "Custom");
      await fill(user, within(dialog).getByLabelText(/^Every/), "3");
      await user.selectOptions(within(dialog).getByLabelText("Related task"), "Ship release notes");
      await user.selectOptions(within(dialog).getByLabelText("Related meeting"), "Sprint review");

      api.set({ "GET /api/reminders": [reminder(), saved] });
      await user.click(within(dialog).getByRole("button", { name: "Create reminder" }));

      await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
      const [request] = api.sent("POST", "/api/reminders");
      expect(request.body).toEqual({
        title: "Prepare demo",
        description: "Laptop and slides",
        remindAt: new Date(2026, 9, 8, 9, 15).toISOString(),
        recurrence: "Custom",
        recurrenceInterval: 3,
        priority: "High",
        taskId: "task-1",
        meetingId: "meeting-1",
      });
      expect(await screen.findByText("Prepare demo")).toBeInTheDocument();
      expect(screen.getByText(/Custom every 3 days · Ship release notes · Sprint review/)).toBeInTheDocument();
      expect(screen.getByRole("checkbox", { name: "Complete Prepare demo" })).not.toBeChecked();
      expect(api.sent("GET", "/api/reminders")).toHaveLength(2);
    }, 30000);

    it("quick-adds a reminder with defaults and clears the input", async () => {
      const saved = reminder({ id: "reminder-quick", title: "Stretch", remindAt: new Date(2026, 9, 6, 11, 0).toISOString() });
      const { user, api } = renderReminders({ api: { "POST /api/reminders": saved } });

      await user.type(await screen.findByLabelText("Reminder title"), "Stretch");
      api.set({ "GET /api/reminders": [reminder(), saved] });
      await user.click(screen.getByRole("button", { name: "Add" }));

      expect(await screen.findByText("Stretch")).toBeInTheDocument();
      expect(api.sent("POST", "/api/reminders")[0].body).toEqual({
        title: "Stretch",
        description: null,
        remindAt: new Date(2026, 9, 6, 11, 0).toISOString(),
        recurrence: "None",
        recurrenceInterval: null,
        priority: "Medium",
        taskId: null,
        meetingId: null,
      });
      expect(screen.getByLabelText("Reminder title")).toHaveValue("");
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });

    it("carries the quick-add text into the full form", async () => {
      const { user } = renderReminders();

      await user.type(await screen.findByLabelText("Reminder title"), "Book flights");
      const dialog = await openCreateDrawer(user);

      expect(within(dialog).getByLabelText("Title")).toHaveValue("Book flights");
    });
  });

  describe("error states", () => {
    it("shows the error state when the list fails and refetches on retry", async () => {
      const { user, api } = renderReminders({ api: { "GET /api/reminders": fail(503, "Reminders are temporarily offline", "SERVICE_UNAVAILABLE") } });

      expect(await screen.findByText("Reminders are unavailable")).toBeInTheDocument();
      const alert = screen.getByRole("alert");
      expect(alert).toHaveTextContent("Reminders are temporarily offline");
      expect(api.sent("GET", "/api/reminders")).toHaveLength(1);

      api.set({ "GET /api/reminders": [reminder()] });
      await user.click(within(alert).getByRole("button", { name: "Try again" }));

      expect(await screen.findByText("Water the plants")).toBeInTheDocument();
      expect(screen.queryByRole("alert")).not.toBeInTheDocument();
      expect(api.sent("GET", "/api/reminders")).toHaveLength(2);
    });

    it("shows the server validation message and keeps the form open when saving fails", async () => {
      const { user, api } = renderReminders({
        api: { "POST /api/reminders": fail(400, "Reminder time must be in the future.", "VALIDATION_FAILED") },
      });
      const dialog = await openCreateDrawer(user);
      const rejections = captureUnhandledRejections();
      try {
        await user.type(within(dialog).getByLabelText("Title"), "Too late");
        await user.click(within(dialog).getByRole("button", { name: "Create reminder" }));

        expect(await within(dialog).findByText("Reminder time must be in the future.")).toBeInTheDocument();
        expect(screen.getByRole("dialog", { name: "New reminder" })).toBeInTheDocument();
        expect(within(dialog).getByLabelText("Title")).toHaveValue("Too late");
        expect(within(dialog).getByRole("button", { name: "Create reminder" })).toBeEnabled();
        expect(api.sent("POST", "/api/reminders")).toHaveLength(1);
        await waitFor(() => expect(rejections.reasons).toHaveLength(1));
        expect(rejections.reasons[0]).toMatchObject({ status: 400, code: "VALIDATION_FAILED" });
      } finally {
        rejections.restore();
      }
    });
  });

  describe("authorization", () => {
    it("hides quick add and the create form from users without tasks.manage", async () => {
      renderReminders({ user: memberUser });

      expect(await screen.findByText("Water the plants")).toBeInTheDocument();
      expect(screen.queryByLabelText("Reminder title")).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: "Add" })).not.toBeInTheDocument();
      expect(screen.queryByRole("button", { name: "More options" })).not.toBeInTheDocument();
    });

    it("opens existing reminders read-only for users without tasks.manage", async () => {
      const { user } = renderReminders({ user: memberUser });

      await user.click(await screen.findByRole("button", { name: /Water the plants/ }));

      const dialog = screen.getByRole("dialog", { name: "Water the plants" });
      expect(within(dialog).getByLabelText("Title")).toBeDisabled();
      expect(within(dialog).getByLabelText("When")).toBeDisabled();
      expect(within(dialog).queryByRole("button", { name: "Save changes" })).not.toBeInTheDocument();
      expect(within(dialog).queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
    });
  });

  describe("empty state", () => {
    it("shows the empty state when there are no reminders", async () => {
      renderReminders({ api: { "GET /api/reminders": [] } });

      expect(await screen.findByRole("heading", { name: "No reminders yet" })).toBeInTheDocument();
      expect(screen.queryByRole("heading", { name: "Open" })).not.toBeInTheDocument();
      expect(screen.getByRole("button", { name: "More options" })).toBeEnabled();
    });
  });
});
