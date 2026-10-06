import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { fail } from "../../test/api.ts";
import { memberUser, renderWithProviders } from "../../test/render.tsx";
import type { StandupBoard, StandupLine } from "./api.ts";
import { StandupsView } from "./standups-view.tsx";

const options = {
  projects: [],
  people: [],
  teams: [{ id: "team-1", name: "Platform" }],
  tags: [],
};

function board(overrides: Partial<StandupBoard> = {}): StandupBoard {
  return {
    date: "2026-10-06",
    standupId: null,
    teamId: null,
    teamName: null,
    meetingId: null,
    meetingTitle: null,
    lines: [],
    ...overrides,
  };
}

function line(overrides: Partial<StandupLine> = {}): StandupLine {
  return {
    entryId: "entry-1",
    userId: "33333333-3333-3333-3333-333333333333",
    memberName: "Grace Hopper",
    yesterday: "Reviewed PRs",
    today: "Pairing on search",
    blockers: null,
    notes: null,
    status: "Clear",
    mine: false,
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

function renderStandups(options_: Parameters<typeof renderWithProviders>[1] = {}) {
  return renderWithProviders(<StandupsView />, {
    route: "/standups?date=2026-10-06",
    ...options_,
    api: {
      "GET /api/work/options": options,
      "GET /api/standups/search": [],
      "GET /api/standups": board(),
      ...options_.api,
    },
  });
}

async function findForm() {
  const heading = await screen.findByRole("heading", { name: /^My (standup|update)$/ });
  return heading.closest("form") as HTMLFormElement;
}

describe("StandupsView: create standup", () => {
  describe("form", () => {
    it("loads the board for the requested date", async () => {
      const { api } = renderStandups();

      expect(await screen.findByText("Tuesday, 6 October 2026")).toBeInTheDocument();
      await findForm();
      const [request] = api.sent("GET", "/api/standups");
      expect(request.query.get("date")).toBe("2026-10-06");
      expect(request.query.has("teamId")).toBe(false);
      expect(screen.getByLabelText("Standup date")).toHaveValue("2026-10-06");
    });

    it("has no required fields: an empty update is sent with nulls and the default status", async () => {
      const { user, api } = renderStandups({ api: { "POST /api/standups": board() } });
      const form = await findForm();

      await user.click(within(form).getByRole("button", { name: "Save standup" }));

      expect(await within(form).findByText("Saved.")).toBeInTheDocument();
      expect(api.sent("POST", "/api/standups")[0].body).toEqual({
        date: "2026-10-06",
        teamId: null,
        yesterday: null,
        today: null,
        blockers: null,
        notes: null,
        status: "Clear",
      });
    });

    it("sends my update for the team board and shows it in the team table after the refetch", async () => {
      const mine = line({
        entryId: "entry-mine",
        userId: "11111111-1111-1111-1111-111111111111",
        memberName: "Ada Admin",
        yesterday: "Fixed login bug",
        today: "Write tests",
        blockers: "Waiting on API keys",
        status: "Blocked",
        mine: true,
      });
      const teamBoard = board({ standupId: "standup-1", teamId: "team-1", teamName: "Platform", lines: [line()] });
      const { user, api } = renderStandups({
        api: {
          "GET /api/standups": ({ query }: { query: URLSearchParams }) => (query.get("teamId") === "team-1" ? teamBoard : board()),
          "POST /api/standups": { ...teamBoard, lines: [line(), mine] },
        },
      });
      await findForm();

      await user.selectOptions(screen.getByLabelText("Board"), "Platform");
      const table = await screen.findByRole("table");
      expect(within(table).getByText("Grace Hopper")).toBeInTheDocument();
      expect(within(table).queryByText("Ada Admin")).not.toBeInTheDocument();
      const form = await findForm();
      expect(within(form).getByRole("heading", { name: "My update" })).toBeInTheDocument();

      await user.click(within(form).getByLabelText("Yesterday"));
      await user.paste("  Fixed login bug ");
      await user.click(within(form).getByLabelText("Today"));
      await user.paste("Write tests");
      await user.click(within(form).getByLabelText("Blockers"));
      await user.paste("Waiting on API keys");
      await user.selectOptions(within(form).getByLabelText("Status"), "Blocked");

      api.set({ "GET /api/standups": ({ query }: { query: URLSearchParams }) => (query.get("teamId") === "team-1" ? { ...teamBoard, lines: [line(), mine] } : board()) });
      await user.click(within(form).getByRole("button", { name: "Save standup" }));

      expect(await within(form).findByText("Saved.")).toBeInTheDocument();
      expect(api.sent("POST", "/api/standups")[0].body).toEqual({
        date: "2026-10-06",
        teamId: "team-1",
        yesterday: "Fixed login bug",
        today: "Write tests",
        blockers: "Waiting on API keys",
        notes: null,
        status: "Blocked",
      });
      const row = (await within(table).findByText("Ada Admin")).closest("tr") as HTMLTableRowElement;
      expect(within(row).getByText("Waiting on API keys")).toBeInTheDocument();
      expect(within(row).getByText("Blocked")).toBeInTheDocument();
      expect(within(form).getByLabelText("Yesterday")).toHaveValue("Fixed login bug");
    }, 30000);

    it("prefills the form from my existing entry", async () => {
      renderStandups({ api: { "GET /api/standups": board({ lines: [line({ mine: true, yesterday: "Deployed", today: "Monitoring", status: "Away" })] }) } });
      const form = await findForm();

      await waitFor(() => expect(within(form).getByLabelText("Yesterday")).toHaveValue("Deployed"));
      expect(within(form).getByLabelText("Today")).toHaveValue("Monitoring");
      expect(within(form).getByLabelText("Status")).toHaveValue("Away");
    });
  });

  describe("error states", () => {
    it("shows the error state when the board fails and refetches on retry", async () => {
      const { user, api } = renderStandups({ api: { "GET /api/standups": fail(500, "Standups could not be read", "SERVER_ERROR") } });

      expect(await screen.findByText("Standup is unavailable")).toBeInTheDocument();
      const alert = screen.getByRole("alert");
      expect(alert).toHaveTextContent("Standups could not be read");
      expect(screen.queryByRole("button", { name: "Save standup" })).not.toBeInTheDocument();
      expect(api.sent("GET", "/api/standups")).toHaveLength(1);

      api.set({ "GET /api/standups": board() });
      await user.click(within(alert).getByRole("button", { name: "Try again" }));

      expect(await screen.findByRole("button", { name: "Save standup" })).toBeInTheDocument();
      expect(screen.queryByRole("alert")).not.toBeInTheDocument();
      expect(api.sent("GET", "/api/standups")).toHaveLength(2);
    });

    it("shows the server validation message and keeps my input when saving fails", async () => {
      const { user, api } = renderStandups({
        api: { "POST /api/standups": fail(400, "Today must be 4000 characters or fewer.", "VALIDATION_FAILED") },
      });
      const form = await findForm();
      const rejections = captureUnhandledRejections();
      try {
        await user.type(within(form).getByLabelText("Today"), "A very long plan");
        await user.click(within(form).getByRole("button", { name: "Save standup" }));

        expect(await within(form).findByText("Today must be 4000 characters or fewer.")).toBeInTheDocument();
        expect(within(form).queryByText("Saved.")).not.toBeInTheDocument();
        expect(within(form).getByLabelText("Today")).toHaveValue("A very long plan");
        expect(within(form).getByRole("button", { name: "Save standup" })).toBeEnabled();
        expect(api.sent("POST", "/api/standups")).toHaveLength(1);
        expect(api.sent("GET", "/api/standups")).toHaveLength(1);
        await waitFor(() => expect(rejections.reasons).toHaveLength(1));
        expect(rejections.reasons[0]).toMatchObject({ status: 400, code: "VALIDATION_FAILED" });
      } finally {
        rejections.restore();
      }
    });
  });

  describe("authorization", () => {
    it("lets a member with only read permissions post their own standup", async () => {
      const { user, api } = renderStandups({ user: memberUser, api: { "POST /api/standups": board() } });
      const form = await findForm();

      await user.type(within(form).getByLabelText("Today"), "Reading docs");
      await user.click(within(form).getByRole("button", { name: "Save standup" }));

      expect(await within(form).findByText("Saved.")).toBeInTheDocument();
      expect(api.sent("POST", "/api/standups")[0].body).toMatchObject({ teamId: null, today: "Reading docs" });
    });

    it("denies access to users without meetings.read", async () => {
      const { api } = renderStandups({ user: { ...memberUser, permissions: [] } });

      await waitFor(() => expect(api.sent("GET", "/api/auth/me")).toHaveLength(1));
      await new Promise((resolve) => setTimeout(resolve, 50));
      expect(screen.getByRole("alert")).toHaveTextContent("Your account does not include standups.");
      expect(screen.queryByRole("button", { name: "Save standup" })).not.toBeInTheDocument();
    });
  });

  describe("empty state", () => {
    it("shows a blank form with the default status when I have no entry", async () => {
      renderStandups();
      const form = await findForm();

      expect(within(form).getByLabelText("Yesterday")).toHaveValue("");
      expect(within(form).getByLabelText("Today")).toHaveValue("");
      expect(within(form).getByLabelText("Status")).toHaveValue("Clear");
      expect(screen.queryByRole("table")).not.toBeInTheDocument();
    });

    it("shows the empty team table when nobody on the team has posted", async () => {
      const { user } = renderStandups({
        api: { "GET /api/standups": ({ query }: { query: URLSearchParams }) => (query.get("teamId") ? board({ teamId: "team-1", teamName: "Platform" }) : board()) },
      });
      await findForm();

      await user.selectOptions(screen.getByLabelText("Board"), "Platform");

      const table = await screen.findByRole("table");
      expect(within(table).getAllByRole("row")).toHaveLength(1);
    });

    it("shows the no-results message when history search finds nothing", async () => {
      const { user, api } = renderStandups();
      await findForm();

      await user.type(screen.getByLabelText("Search history"), "deploy");

      expect(await screen.findByText("No standup entries match that search.")).toBeInTheDocument();
      expect(api.sent("GET", "/api/standups/search").at(-1)?.query.get("q")).toBe("deploy");
    });
  });
});
