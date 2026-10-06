import { act, render, renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { fail, mockApi, noContent } from "../test/api.ts";
import { adminUser } from "../test/render.tsx";
import { AuthProvider, useAuth } from "./auth-context.tsx";

const wrapper = ({ children }: { children: ReactNode }) => <AuthProvider>{children}</AuthProvider>;

describe("useAuth", () => {
  it("throws outside AuthProvider", () => {
    function Consumer() {
      useAuth();
      return null;
    }
    const error = console.error;
    console.error = () => {};
    try {
      expect(() => render(<Consumer />)).toThrow("useAuth must be used within AuthProvider");
    } finally {
      console.error = error;
    }
  });

  it("restores an existing session from /api/auth/me", async () => {
    mockApi({ "GET /api/auth/me": adminUser });
    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.status).toBe("loading");
    await waitFor(() => expect(result.current.status).toBe("authenticated"));
    expect(result.current.user?.email).toBe(adminUser.email);
  });

  it("becomes anonymous when the session check fails", async () => {
    mockApi({ "GET /api/auth/me": fail(401, "Not signed in", "UNAUTHORIZED") });
    const { result } = renderHook(() => useAuth(), { wrapper });

    await waitFor(() => expect(result.current.status).toBe("anonymous"));
    expect(result.current.user).toBeNull();
  });

  it("becomes anonymous when /api/auth/me returns a malformed session", async () => {
    mockApi({ "GET /api/auth/me": { id: "not-a-user" } });
    const { result } = renderHook(() => useAuth(), { wrapper });

    await waitFor(() => expect(result.current.status).toBe("anonymous"));
  });

  it("keeps the access token in memory and uses it as a bearer token", async () => {
    const api = mockApi({
      "GET /api/auth/me": fail(401),
      "POST /api/auth/login": { accessToken: "token-xyz", expiresAt: "2026-10-06T20:00:00Z", user: adminUser },
      "PUT /api/auth/profile": ({ body }: { body: unknown }) => ({ ...adminUser, displayName: (body as { displayName: string }).displayName }),
    });
    const { result } = renderHook(() => useAuth(), { wrapper });
    await waitFor(() => expect(result.current.status).toBe("anonymous"));

    await act(() => result.current.login("admin@worknexa.local", "Passw0rd!"));
    expect(result.current.status).toBe("authenticated");
    expect(sessionStorage.length).toBe(0);
    expect(localStorage.getItem("worknexa.access-token")).toBeNull();

    await act(() => result.current.updateProfile("Ada Lovelace"));
    expect(result.current.user?.displayName).toBe("Ada Lovelace");
    const [profileCall] = api.sent("PUT", "/api/auth/profile");
    expect(profileCall.headers.get("Authorization")).toBe("Bearer token-xyz");
    expect(profileCall.headers.get("X-Worknexa-Request")).toBe("1");
  });

  it("propagates login failures without changing the session", async () => {
    mockApi({ "GET /api/auth/me": fail(401), "POST /api/auth/login": fail(401, "Invalid email or password.") });
    const { result } = renderHook(() => useAuth(), { wrapper });
    await waitFor(() => expect(result.current.status).toBe("anonymous"));

    await expect(result.current.login("a@b.c", "nope")).rejects.toThrow("Invalid email or password.");
    expect(result.current.user).toBeNull();
  });

  it("signs out locally even when the logout request fails", async () => {
    const api = mockApi({ "GET /api/auth/me": adminUser, "POST /api/auth/logout": fail(500, "Server down") });
    const { result } = renderHook(() => useAuth(), { wrapper });
    await waitFor(() => expect(result.current.status).toBe("authenticated"));

    await act(async () => {
      await result.current.logout().catch(() => {});
    });

    expect(result.current.status).toBe("anonymous");
    expect(result.current.user).toBeNull();
    expect(api.sent("POST", "/api/auth/logout")).toHaveLength(1);
  });

  it("clears unsaved note drafts on sign-out but keeps preferences", async () => {
    mockApi({ "GET /api/auth/me": adminUser, "POST /api/auth/logout": noContent });
    localStorage.setItem("worknexa.draft.note-1", JSON.stringify({ value: "private text" }));
    localStorage.setItem("worknexa-theme", "dark");
    const { result } = renderHook(() => useAuth(), { wrapper });
    await waitFor(() => expect(result.current.status).toBe("authenticated"));

    await act(() => result.current.logout());

    expect(localStorage.getItem("worknexa.draft.note-1")).toBeNull();
    expect(localStorage.getItem("worknexa-theme")).toBe("dark");
  });

  it("drops the bearer token after logout", async () => {
    const api = mockApi({
      "GET /api/auth/me": fail(401),
      "POST /api/auth/login": { accessToken: "token-1", expiresAt: "2026-10-06T20:00:00Z", user: adminUser },
      "POST /api/auth/logout": noContent,
      "PUT /api/auth/profile": fail(401),
    });
    const { result } = renderHook(() => useAuth(), { wrapper });
    await waitFor(() => expect(result.current.status).toBe("anonymous"));
    await act(() => result.current.login("admin@worknexa.local", "Passw0rd!"));
    await act(() => result.current.logout());

    await expect(result.current.updateProfile("x")).rejects.toThrow();
    const [profileCall] = api.sent("PUT", "/api/auth/profile");
    expect(profileCall.headers.get("Authorization")).toBeNull();
  });

  it("replaces the session after a password change", async () => {
    const api = mockApi({
      "GET /api/auth/me": adminUser,
      "POST /api/auth/password": { accessToken: "token-rotated", expiresAt: "2026-10-06T20:00:00Z", user: adminUser },
      "PUT /api/auth/profile": adminUser,
    });
    const { result } = renderHook(() => useAuth(), { wrapper });
    await waitFor(() => expect(result.current.status).toBe("authenticated"));

    await act(() => result.current.changePassword("Old-passw0rd", "New-passw0rd!"));
    await act(() => result.current.updateProfile("Ada Admin"));

    expect(api.sent("POST", "/api/auth/password")[0].body).toEqual({ currentPassword: "Old-passw0rd", newPassword: "New-passw0rd!" });
    expect(api.sent("PUT", "/api/auth/profile")[0].headers.get("Authorization")).toBe("Bearer token-rotated");
  });
});
