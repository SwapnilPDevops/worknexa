import { screen, waitFor } from "@testing-library/react";
import { Route, Routes } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { LoginPage } from "../pages/login-page.tsx";
import { adminUser, memberUser, renderWithProviders } from "../test/render.tsx";
import { RequireAuth } from "./require-auth.tsx";
import { AppRouter } from "./router.tsx";

function GuardedRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<RequireAuth />}>
        <Route path="/" element={<p>My day</p>} />
        <Route path="/projects" element={<p>Projects page</p>} />
      </Route>
    </Routes>
  );
}

const loginResponse = { accessToken: "token-abc", expiresAt: "2026-10-06T20:00:00Z", user: memberUser };

describe("routing", () => {
  it("shows a loading state while the session is being restored", async () => {
    renderWithProviders(<GuardedRoutes />, { route: "/projects", api: { "GET /api/auth/me": () => new Promise(() => {}) } });

    expect(screen.getByRole("status")).toHaveTextContent("Checking your session");
    expect(screen.queryByText("Projects page")).not.toBeInTheDocument();
  });

  it("redirects anonymous users to the login page", async () => {
    renderWithProviders(<GuardedRoutes />, { route: "/projects", user: null });

    expect(await screen.findByRole("button", { name: "Sign in" })).toBeInTheDocument();
    expect(screen.getByTestId("location")).toHaveTextContent("/login");
    expect(screen.queryByText("Projects page")).not.toBeInTheDocument();
  });

  it("returns to the protected page after signing in", async () => {
    const { user } = renderWithProviders(<GuardedRoutes />, {
      route: "/projects",
      user: null,
      api: { "POST /api/auth/login": loginResponse },
    });

    await user.type(await screen.findByLabelText("Email"), "member@worknexa.local");
    await user.type(screen.getByLabelText("Password"), "Passw0rd!");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByText("Projects page")).toBeInTheDocument();
    expect(screen.getByTestId("location")).toHaveTextContent("/projects");
  });

  it("renders protected pages for a restored session", async () => {
    renderWithProviders(<GuardedRoutes />, { route: "/projects", user: adminUser });

    expect(await screen.findByText("Projects page")).toBeInTheDocument();
  });

  it("treats a failed session check as signed out", async () => {
    renderWithProviders(<GuardedRoutes />, { route: "/", user: null });

    await waitFor(() => expect(screen.getByTestId("location")).toHaveTextContent("/login"));
  });

  it("guards every workspace route of the real router", async () => {
    for (const route of ["/", "/tasks", "/projects", "/meetings", "/team", "/leaves", "/learning", "/settings", "/does-not-exist"]) {
      const view = renderWithProviders(<AppRouter />, { route, user: null });
      expect(await screen.findByRole("button", { name: "Sign in" })).toBeInTheDocument();
      expect(view.api.unhandled).toEqual([]);
      view.unmount();
    }
  });
});
