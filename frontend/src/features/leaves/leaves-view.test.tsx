import { screen, waitFor, within } from "@testing-library/react";
import { Route, Routes } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { RequireAuth } from "../../app/require-auth.tsx";
import { fail } from "../../test/api.ts";
import { adminUser, memberUser, renderWithProviders } from "../../test/render.tsx";
import { captureUnhandledRejections } from "../../test/unhandled-rejections.ts";
import type { LeaveItem } from "./api.ts";
import { LeavesView } from "./leaves-view.tsx";

const options = {
  leaveTypes: [
    { id: "type-annual", name: "Annual leave", isPaid: true },
    { id: "type-sick", name: "Sick leave", isPaid: true },
  ],
  people: [
    { id: adminUser.id, name: adminUser.displayName },
    { id: memberUser.id, name: memberUser.displayName },
    { id: "u-cleo", name: "Cleo Coder" },
  ],
};

function leave(overrides: Partial<LeaveItem> = {}): LeaveItem {
  return {
    id: "leave-1",
    employeeId: "u-cleo",
    employeeName: "Cleo Coder",
    leaveTypeId: "type-annual",
    leaveType: "Annual leave",
    startDate: "2026-11-02",
    endDate: "2026-11-03",
    days: 2,
    reason: "Family visit",
    status: "Pending",
    approvedById: null,
    approvedByName: null,
    ...overrides,
  };
}

/** A leave backend whose list includes every request created through the API. */
function leaveBackend(initial: LeaveItem[] = [leave()]) {
  const leaves = [...initial];
  return {
    "GET /api/leaves/options": options,
    "GET /api/leaves": () => [...leaves],
    "POST /api/leaves": ({ body }: { body: unknown }) => {
      const input = body as { employeeId: string; leaveTypeId: string; startDate: string; endDate: string; reason: string | null };
      const created = leave({
        id: `leave-${leaves.length + 1}`,
        employeeId: input.employeeId,
        employeeName: options.people.find((person) => person.id === input.employeeId)?.name ?? "Unknown",
        leaveTypeId: input.leaveTypeId,
        leaveType: options.leaveTypes.find((type) => type.id === input.leaveTypeId)?.name ?? "Unknown",
        startDate: input.startDate,
        endDate: input.endDate,
        days: 3,
        reason: input.reason,
      });
      leaves.push(created);
      return created;
    },
  };
}

function renderLeaves(options: Parameters<typeof renderWithProviders>[1] = {}) {
  return renderWithProviders(
    <Routes>
      <Route element={<RequireAuth />}>
        <Route path="/leaves" element={<LeavesView />} />
      </Route>
    </Routes>,
    { route: "/leaves", ...options },
  );
}

async function openRequestDrawer(user: ReturnType<typeof renderLeaves>["user"]) {
  await user.click(await screen.findByRole("button", { name: "Request leave" }));
  return within(await screen.findByRole("dialog", { name: "Request leave" }));
}

async function fillDates(user: ReturnType<typeof renderLeaves>["user"], dialog: ReturnType<typeof within>, start: string, end: string) {
  await user.type(dialog.getByLabelText("Start date"), start);
  await user.type(dialog.getByLabelText("End date"), end);
}

describe("LeavesView – request leave", () => {
  it("lists existing leave requests", async () => {
    const { api } = renderLeaves({ api: leaveBackend() });

    const table = await screen.findByRole("table", { name: "Leave requests" });
    expect(within(table).getByRole("cell", { name: "Cleo Coder" })).toBeInTheDocument();
    expect(within(table).getByRole("cell", { name: "Family visit" })).toBeInTheDocument();
    expect(api.unhandled).toEqual([]);
  });

  it("shows the empty state when there is no leave", async () => {
    renderLeaves({ api: leaveBackend([]) });

    expect(await screen.findByRole("heading", { name: "No leave yet" })).toBeInTheDocument();
    expect(screen.queryByRole("table")).not.toBeInTheDocument();
  });

  it("requires start and end dates before submitting", async () => {
    const { user, api } = renderLeaves({ api: leaveBackend() });
    const dialog = await openRequestDrawer(user);

    await user.click(dialog.getByRole("button", { name: "Submit request" }));

    expect(dialog.getByLabelText("Start date")).toBeInvalid();
    expect(dialog.getByLabelText("End date")).toBeInvalid();
    expect(api.sent("POST", "/api/leaves")).toHaveLength(0);
    expect(screen.getByRole("dialog", { name: "Request leave" })).toBeInTheDocument();
  });

  it("requires a leave type", async () => {
    const { user, api } = renderLeaves({ api: leaveBackend() });
    const dialog = await openRequestDrawer(user);

    await user.selectOptions(dialog.getByLabelText("Leave type"), "Choose");
    await fillDates(user, dialog, "2026-12-01", "2026-12-03");
    await user.click(dialog.getByRole("button", { name: "Submit request" }));

    expect(dialog.getByLabelText("Leave type")).toBeInvalid();
    expect(api.sent("POST", "/api/leaves")).toHaveLength(0);
  });

  it("does not count days for an end date before the start date and relies on the server to reject it", async () => {
    const rejections = captureUnhandledRejections();
    const { user, api } = renderLeaves({
      api: { ...leaveBackend(), "POST /api/leaves": fail(400, "The end date must be on or after the start date.", "VALIDATION_FAILED") },
    });
    const dialog = await openRequestDrawer(user);

    await fillDates(user, dialog, "2026-12-10", "2026-12-08");

    expect(dialog.getByText("Days are counted from the start through the end date.")).toBeInTheDocument();
    await user.click(dialog.getByRole("button", { name: "Submit request" }));

    expect(await dialog.findByText("The end date must be on or after the start date.")).toBeInTheDocument();
    expect(api.sent("POST", "/api/leaves")[0].body).toMatchObject({ startDate: "2026-12-10", endDate: "2026-12-08" });
    expect(screen.getByRole("dialog", { name: "Request leave" })).toBeInTheDocument();
    await waitFor(() => expect(rejections).toHaveLength(1));
  });

  it("submits a valid request for the chosen employee, closes the drawer and shows the new row", async () => {
    const { user, api } = renderLeaves({ api: leaveBackend() });
    const dialog = await openRequestDrawer(user);

    expect(dialog.getByLabelText("Employee")).toBeEnabled();
    await user.selectOptions(dialog.getByLabelText("Employee"), "Max Member");
    await user.selectOptions(dialog.getByLabelText("Leave type"), "Sick leave");
    await fillDates(user, dialog, "2026-12-01", "2026-12-03");
    expect(dialog.getByText("3 days")).toBeInTheDocument();
    await user.type(dialog.getByLabelText("Reason"), "Recovery");
    await user.click(dialog.getByRole("button", { name: "Submit request" }));

    await waitFor(() => expect(screen.queryByRole("dialog", { name: "Request leave" })).not.toBeInTheDocument());
    const [request] = api.sent("POST", "/api/leaves");
    expect(request.body).toEqual({ employeeId: memberUser.id, leaveTypeId: "type-sick", startDate: "2026-12-01", endDate: "2026-12-03", reason: "Recovery" });

    const table = await screen.findByRole("table", { name: "Leave requests" });
    expect(await within(table).findByRole("cell", { name: "Recovery" })).toBeInTheDocument();
    expect(within(table).getByRole("cell", { name: "Sick leave" })).toBeInTheDocument();
    expect(api.sent("GET", "/api/leaves")).toHaveLength(2);
  });

  it("defaults the employee to the signed-in manager and sends a null reason when empty", async () => {
    const { user, api } = renderLeaves({ api: leaveBackend() });
    const dialog = await openRequestDrawer(user);

    expect(dialog.getByLabelText("Employee")).toHaveValue(adminUser.id);
    expect(dialog.getByLabelText("Leave type")).toHaveValue("type-annual");
    await fillDates(user, dialog, "2026-12-01", "2026-12-01");
    expect(dialog.getByText("1 day")).toBeInTheDocument();
    await user.click(dialog.getByRole("button", { name: "Submit request" }));

    await waitFor(() => expect(api.sent("POST", "/api/leaves")).toHaveLength(1));
    expect(api.sent("POST", "/api/leaves")[0].body).toEqual({ employeeId: adminUser.id, leaveTypeId: "type-annual", startDate: "2026-12-01", endDate: "2026-12-01", reason: null });
  });

  it("surfaces a server rejection and keeps the drawer open with the input intact", async () => {
    const rejections = captureUnhandledRejections();
    const { user, api } = renderLeaves({
      api: { ...leaveBackend(), "POST /api/leaves": fail(409, "This leave overlaps an existing request.", "CONFLICT") },
    });
    const dialog = await openRequestDrawer(user);

    await fillDates(user, dialog, "2026-11-02", "2026-11-03");
    await user.click(dialog.getByRole("button", { name: "Submit request" }));

    expect(await dialog.findByText("This leave overlaps an existing request.")).toBeInTheDocument();
    expect(dialog.getByLabelText("Start date")).toHaveValue("2026-11-02");
    expect(dialog.getByRole("button", { name: "Submit request" })).toBeEnabled();
    expect(api.sent("GET", "/api/leaves")).toHaveLength(1);
    await waitFor(() => expect(rejections).toEqual([expect.objectContaining({ name: "ApiError", status: 409 })]));
  });

  it("shows the error state when the list fails and refetches on retry", async () => {
    const backend = leaveBackend();
    const { user, api } = renderLeaves({ api: { ...backend, "GET /api/leaves": fail(503, "Leave service is down.", "UNAVAILABLE") } });

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent("Leave is unavailable");
    expect(alert).toHaveTextContent("Leave service is down.");

    api.set({ "GET /api/leaves": backend["GET /api/leaves"] });
    await user.click(within(alert).getByRole("button", { name: "Try again" }));

    expect(await screen.findByRole("table", { name: "Leave requests" })).toBeInTheDocument();
    expect(api.sent("GET", "/api/leaves")).toHaveLength(2);
  });

  it("lets a member request leave only for themselves and hides approval controls", async () => {
    const own = leave({ id: "leave-own", employeeId: memberUser.id, employeeName: memberUser.displayName, reason: "Own trip" });
    const { user, api } = renderLeaves({ user: memberUser, api: leaveBackend([leave(), own]) });

    const table = await screen.findByRole("table", { name: "Leave requests" });
    expect(within(table).queryByRole("button", { name: "Approve" })).not.toBeInTheDocument();
    expect(within(table).queryByRole("button", { name: "Reject" })).not.toBeInTheDocument();
    const ownRow = within(table).getByRole("cell", { name: "Own trip" }).closest("tr")!;
    const otherRow = within(table).getByRole("cell", { name: "Family visit" }).closest("tr")!;
    expect(within(ownRow).getByRole("button", { name: "Cancel" })).toBeInTheDocument();
    expect(within(otherRow).queryByRole("button", { name: "Cancel" })).not.toBeInTheDocument();

    const dialog = await openRequestDrawer(user);
    expect(dialog.getByLabelText("Employee")).toBeDisabled();
    expect(dialog.getByLabelText("Employee")).toHaveValue(memberUser.id);
    await fillDates(user, dialog, "2026-12-01", "2026-12-02");
    await user.click(dialog.getByRole("button", { name: "Submit request" }));

    await waitFor(() => expect(api.sent("POST", "/api/leaves")).toHaveLength(1));
    expect(api.sent("POST", "/api/leaves")[0].body).toMatchObject({ employeeId: memberUser.id });
  });

  it("shows approve and reject for pending requests to managers", async () => {
    renderLeaves({ api: leaveBackend() });

    const table = await screen.findByRole("table", { name: "Leave requests" });
    expect(within(table).getByRole("button", { name: "Approve" })).toBeInTheDocument();
    expect(within(table).getByRole("button", { name: "Reject" })).toBeInTheDocument();
  });
});
