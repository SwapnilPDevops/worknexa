import { screen, waitFor, within } from "@testing-library/react";
import { Route, Routes } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { RequireAuth } from "../../app/require-auth.tsx";
import { fail } from "../../test/api.ts";
import { memberUser, renderWithProviders } from "../../test/render.tsx";
import { captureUnhandledRejections } from "../../test/unhandled-rejections.ts";
import { TeamView } from "./team-view.tsx";

type TeamMember = { userId: string; name: string; email: string; role: string; status: string; designation: string | null };

const ada = { id: "u-ada", name: "Ada Admin", email: "ada@worknexa.local", designation: "Engineering lead" };
const ben = { id: "u-ben", name: "Ben Builder", email: "ben@worknexa.local", designation: "Backend engineer" };

function memberRow(person: typeof ada, role: string): TeamMember {
  return { userId: person.id, name: person.name, email: person.email, role, status: "Active", designation: person.designation };
}

function personRow(person: typeof ada) {
  return { id: person.id, name: person.name, email: person.email, photoUrl: null, designation: person.designation, department: "Engineering", status: "Active" };
}

/** A team backend whose detail and list reflect members added through the API. */
function teamBackend(initial: TeamMember[] = [memberRow(ada, "Owner")]) {
  const members = [...initial];
  const summary = () => ({ id: "team-1", name: "Platform", description: "Core services", memberCount: members.length });
  const detail = () => ({ id: "team-1", name: "Platform", description: "Core services", members: [...members] });
  return {
    members,
    routes: {
      "GET /api/teams/options": () => ({ teams: [summary()], people: [ada, ben].map(({ id, name, designation }) => ({ id, name, designation })) }),
      "GET /api/teams": () => [summary()],
      "GET /api/teams/:id": () => detail(),
      "POST /api/teams/:id/members": ({ body }: { body: unknown }) => {
        const { userId, role } = body as { userId: string; role: string };
        members.push(memberRow([ada, ben].find((person) => person.id === userId)!, role));
        return detail();
      },
      "GET /api/members": [personRow(ada), personRow(ben)],
    },
  };
}

function renderTeam(options: Parameters<typeof renderWithProviders>[1] = {}) {
  return renderWithProviders(
    <Routes>
      <Route element={<RequireAuth />}>
        <Route path="/team" element={<TeamView />} />
      </Route>
    </Routes>,
    { route: "/team", ...options },
  );
}

function teamCard() {
  return screen.getByRole("heading", { name: "Platform", level: 2 }).closest("section")!;
}

async function openTeam(user: ReturnType<typeof renderTeam>["user"]) {
  await user.click(await screen.findByRole("button", { name: /^Platform/ }));
  await screen.findByRole("heading", { name: "Platform", level: 2 });
}

describe("TeamView – add team member", () => {
  it("lists teams and people and invites the user to choose one", async () => {
    const backend = teamBackend();
    const { api } = renderTeam({ api: backend.routes });

    expect(await screen.findByRole("heading", { name: "Team", level: 1 })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /^Platform/ })).toHaveTextContent("1 person");
    expect(screen.getByRole("button", { name: /Ben Builder/ })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Choose a person or a team" })).toBeInTheDocument();
    expect(api.unhandled).toEqual([]);
  });

  it("shows empty messages when there are no teams or people", async () => {
    renderTeam({ api: { "GET /api/teams/options": { teams: [], people: [] }, "GET /api/teams": [], "GET /api/members": [] } });

    expect(await screen.findByText("No teams yet.")).toBeInTheDocument();
    expect(screen.getByText("No matching people.")).toBeInTheDocument();
  });

  it("requires a person before adding a member", async () => {
    const backend = teamBackend();
    const { user, api } = renderTeam({ api: backend.routes });
    await openTeam(user);

    const card = within(teamCard());
    await user.click(card.getByRole("button", { name: "Add" }));

    expect(card.getByLabelText("Person")).toBeInvalid();
    expect(api.sent("POST", "/api/teams/:id/members")).toHaveLength(0);
  });

  it("adds the chosen person with the chosen role and shows them after the refetch", async () => {
    const backend = teamBackend();
    const { user, api } = renderTeam({ api: backend.routes });
    await openTeam(user);

    const card = within(teamCard());
    expect(card.queryByRole("button", { name: /Ben Builder/ })).not.toBeInTheDocument();
    await user.selectOptions(card.getByLabelText("Person"), "Ben Builder");
    await user.selectOptions(card.getByLabelText("Role"), "Owner");
    await user.click(card.getByRole("button", { name: "Add" }));

    expect(await within(teamCard()).findByRole("button", { name: /Ben Builder/ })).toBeInTheDocument();
    const [request] = api.sent("POST", "/api/teams/team-1/members");
    expect(request.body).toEqual({ userId: "u-ben", role: "Owner" });
    expect(within(teamCard()).getAllByRole("button", { name: "Remove" })).toHaveLength(2);
    expect(await screen.findByText("2 people")).toBeInTheDocument();
    expect(api.unhandled).toEqual([]);
  });

  it("does not surface a failed add: the error escapes as an unhandled rejection and the list stays unchanged", async () => {
    const rejections = captureUnhandledRejections();
    const backend = teamBackend();
    const { user, api } = renderTeam({
      api: { ...backend.routes, "POST /api/teams/:id/members": fail(409, "Already a member.", "CONFLICT") },
    });
    await openTeam(user);

    const card = within(teamCard());
    await user.selectOptions(card.getByLabelText("Person"), "Ben Builder");
    await user.click(card.getByRole("button", { name: "Add" }));

    await waitFor(() => expect(rejections).toHaveLength(1));
    expect(rejections[0]).toMatchObject({ name: "ApiError", status: 409, message: "Already a member." });
    expect(api.sent("POST", "/api/teams/team-1/members")).toHaveLength(1);
    // The form stays usable, but nothing tells the user why the person was not added.
    expect(card.getByLabelText("Person")).toHaveValue("u-ben");
    expect(screen.queryByText("Already a member.")).not.toBeInTheDocument();
    expect(card.queryByRole("button", { name: /Ben Builder/ })).not.toBeInTheDocument();
  });

  it("shows the error state when the team list fails and refetches on retry", async () => {
    const backend = teamBackend();
    const { user, api } = renderTeam({ api: { ...backend.routes, "GET /api/teams": fail(500, "Server error", "SERVER_ERROR") } });

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent("The team is unavailable");
    expect(api.sent("GET", "/api/teams")).toHaveLength(1);

    api.set({ "GET /api/teams": backend.routes["GET /api/teams"] });
    await user.click(within(alert).getByRole("button", { name: "Try again" }));

    expect(await screen.findByRole("button", { name: /^Platform/ })).toBeInTheDocument();
    expect(api.sent("GET", "/api/teams")).toHaveLength(2);
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("shows the error state when the people list fails", async () => {
    const backend = teamBackend();
    renderTeam({ api: { ...backend.routes, "GET /api/members": fail(500) } });

    expect(await screen.findByRole("alert")).toHaveTextContent("People and teams could not be loaded.");
  });

  it("hides team management controls from members without teams.manage", async () => {
    const backend = teamBackend();
    const { user } = renderTeam({ user: memberUser, api: backend.routes });
    await openTeam(user);

    const card = within(teamCard());
    expect(card.getByText("Ada Admin")).toBeInTheDocument();
    expect(card.queryByLabelText("Person")).not.toBeInTheDocument();
    expect(card.queryByRole("button", { name: "Add" })).not.toBeInTheDocument();
    expect(card.queryByRole("button", { name: "Remove" })).not.toBeInTheDocument();
    expect(card.queryByRole("button", { name: "Edit" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "New team" })).not.toBeInTheDocument();
  });
});
