import { screen, waitFor } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Route, Routes } from "react-router-dom";
import { fail } from "../test/api.ts";
import { adminUser, renderWithProviders } from "../test/render.tsx";
import { LoginPage } from "./login-page.tsx";

const loginResponse = { accessToken: "token-123", expiresAt: "2026-10-06T20:00:00Z", user: adminUser };

function renderLogin(options: Parameters<typeof renderWithProviders>[1] = {}) {
  return renderWithProviders(
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="*" element={<p>Workspace home</p>} />
    </Routes>,
    { route: "/login", user: null, ...options },
  );
}

describe("LoginPage", () => {
  it("shows validation messages and does not call the API for empty input", async () => {
    const { user, api } = renderLogin();

    await user.click(await screen.findByRole("button", { name: "Sign in" }));

    expect(await screen.findByText("Enter a valid email.")).toBeInTheDocument();
    expect(screen.getByText("Enter your password.")).toBeInTheDocument();
    expect(api.sent("POST", "/api/auth/login")).toHaveLength(0);
  });

  it("rejects a malformed email", async () => {
    const { user, api } = renderLogin();

    await user.type(await screen.findByLabelText("Email"), "not-an-email");
    await user.type(screen.getByLabelText("Password"), "secret");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByText("Enter a valid email.")).toBeInTheDocument();
    expect(api.sent("POST", "/api/auth/login")).toHaveLength(0);
  });

  it("signs in, sends the CSRF header and the bearer token afterwards, and redirects home", async () => {
    const { user, api } = renderLogin({
      api: {
        "POST /api/auth/login": loginResponse,
        "GET /api/ping": { ok: true },
      },
    });

    await user.type(await screen.findByLabelText("Email"), "  admin@worknexa.local ");
    await user.type(screen.getByLabelText("Password"), "Passw0rd!");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByText("Workspace home")).toBeInTheDocument();
    const [request] = api.sent("POST", "/api/auth/login");
    expect(request.body).toEqual({ email: "admin@worknexa.local", password: "Passw0rd!" });
    expect(request.headers.get("X-Worknexa-Request")).toBe("1");
    expect(sessionStorage.getItem("worknexa.access-token")).toBeNull();
  });

  it("redirects back to the page the user originally requested", async () => {
    const { user } = renderLogin({
      route: { pathname: "/login", state: { from: "/projects" } },
      api: { "POST /api/auth/login": loginResponse },
    });

    await user.type(await screen.findByLabelText("Email"), "admin@worknexa.local");
    await user.type(screen.getByLabelText("Password"), "Passw0rd!");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    await waitFor(() => expect(screen.getByTestId("location")).toHaveTextContent("/projects"));
  });

  it("never redirects back to the login page itself", async () => {
    const { user } = renderLogin({
      route: { pathname: "/login", state: { from: "/login" } },
      api: { "POST /api/auth/login": loginResponse },
    });

    await user.type(await screen.findByLabelText("Email"), "admin@worknexa.local");
    await user.type(screen.getByLabelText("Password"), "Passw0rd!");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    await waitFor(() => expect(screen.getByTestId("location").textContent).toBe("/"));
  });

  it("shows the server error message for invalid credentials and stays on the form", async () => {
    const { user } = renderLogin({
      api: { "POST /api/auth/login": fail(401, "Invalid email or password.", "AUTHENTICATION_FAILED") },
    });

    await user.type(await screen.findByLabelText("Email"), "admin@worknexa.local");
    await user.type(screen.getByLabelText("Password"), "wrong");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Invalid email or password.");
    expect(screen.getByRole("button", { name: "Sign in" })).toBeEnabled();
    expect(screen.queryByText("Workspace home")).not.toBeInTheDocument();
  });

  it("shows the rate-limit message when too many attempts are made", async () => {
    const { user } = renderLogin({
      api: { "POST /api/auth/login": fail(429, "Too many sign-in attempts. Try again in a minute.", "RATE_LIMITED") },
    });

    await user.type(await screen.findByLabelText("Email"), "admin@worknexa.local");
    await user.type(screen.getByLabelText("Password"), "wrong");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Too many sign-in attempts");
  });

  it("falls back to a generic message when the network fails", async () => {
    const { user, api } = renderLogin();
    api.set({ "POST /api/auth/login": () => { throw new TypeError("Failed to fetch"); } });

    await user.type(await screen.findByLabelText("Email"), "admin@worknexa.local");
    await user.type(screen.getByLabelText("Password"), "Passw0rd!");
    await user.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Sign-in failed.");
  });

  it("skips the form when a session already exists", async () => {
    renderLogin({ user: adminUser });

    expect(await screen.findByText("Workspace home")).toBeInTheDocument();
  });
});
