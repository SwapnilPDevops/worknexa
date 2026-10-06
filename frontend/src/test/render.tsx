import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactElement, ReactNode } from "react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { ThemeProvider } from "../app/theme.tsx";
import { permissions } from "../auth/access.ts";
import { AuthProvider } from "../auth/auth-context.tsx";
import type { UserSession } from "../auth/session.ts";
import { fail, mockApi } from "./api.ts";

export const adminUser: UserSession = {
  id: "11111111-1111-1111-1111-111111111111",
  email: "admin@worknexa.local",
  displayName: "Ada Admin",
  roles: ["Admin"],
  permissions: Object.values(permissions),
};

export const memberUser: UserSession = {
  id: "22222222-2222-2222-2222-222222222222",
  email: "member@worknexa.local",
  displayName: "Max Member",
  roles: ["Member"],
  permissions: [
    permissions.tasksRead,
    permissions.projectsRead,
    permissions.teamsRead,
    permissions.calendarRead,
    permissions.meetingsRead,
    permissions.leavesRead,
    permissions.learningRead,
    permissions.notesRead,
    permissions.notesManage,
    permissions.notificationsRead,
  ],
};

export function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false, refetchOnWindowFocus: false, gcTime: Infinity },
      mutations: { retry: false },
    },
  });
}

function LocationProbe() {
  const location = useLocation();
  return <span hidden data-testid="location">{`${location.pathname}${location.search}`}</span>;
}

type RenderOptions = {
  /** Initial URL, or a location with router state. */
  route?: string | { pathname: string; search?: string; state?: unknown };
  /** Route pattern the UI is mounted on; other paths render only the location probe. */
  path?: string;
  /** Signed-in user returned by /api/auth/me; null means anonymous. */
  user?: UserSession | null;
  /** API routes; see mockApi. */
  api?: Parameters<typeof mockApi>[0];
};

export function Providers({ children, client, route }: { children: ReactNode; client: QueryClient; route: NonNullable<RenderOptions["route"]> }) {
  return (
    <QueryClientProvider client={client}>
      <ThemeProvider>
        <MemoryRouter initialEntries={[route]}>
          <AuthProvider>{children}</AuthProvider>
        </MemoryRouter>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

/** Renders UI with the app's providers, a mocked API and a signed-in admin by default. */
export function renderWithProviders(ui: ReactElement, options: RenderOptions = {}) {
  const { route = "/", path, user = adminUser, api: routes = {} } = options;
  const api = mockApi({
    "GET /api/auth/me": user ?? fail(401, "Not signed in", "UNAUTHORIZED"),
    ...routes,
  });
  const client = createTestQueryClient();
  const content = path ? (
    <>
      <Routes>
        <Route path={path} element={ui} />
        <Route path="*" element={null} />
      </Routes>
      <LocationProbe />
    </>
  ) : (
    <>
      {ui}
      <LocationProbe />
    </>
  );

  const result = render(<Providers client={client} route={route}>{content}</Providers>);
  return { ...result, api, client, user: userEvent.setup() };
}
