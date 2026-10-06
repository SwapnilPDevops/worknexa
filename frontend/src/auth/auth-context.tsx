import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { clearAllDrafts } from "../features/notes/draft-store.ts";
import { getJson, sendJson, setAccessToken } from "../services/api-client.ts";
import { loginResponseSchema, readData, userSessionSchema, type UserSession } from "./session.ts";

type AuthStatus = "loading" | "anonymous" | "authenticated";

type AuthContextValue = {
  status: AuthStatus;
  user: UserSession | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  updateProfile: (displayName: string) => Promise<void>;
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>("loading");
  const [user, setUser] = useState<UserSession | null>(null);

  useEffect(() => {
    let active = true;
    getJson("/api/auth/me")
      .then((payload) => {
        if (!active) {
          return;
        }
        setUser(readData(payload, userSessionSchema));
        setStatus("authenticated");
      })
      .catch(() => {
        if (!active) {
          return;
        }
        setAccessToken(null);
        setUser(null);
        setStatus("anonymous");
      });
    return () => {
      active = false;
    };
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user,
      login: async (email, password) => {
        const payload = await sendJson("/api/auth/login", "POST", { email, password });
        const session = readData(payload, loginResponseSchema);
        setAccessToken(session.accessToken);
        setUser(session.user);
        setStatus("authenticated");
      },
      logout: async () => {
        try {
          await sendJson("/api/auth/logout", "POST");
        } finally {
          setAccessToken(null);
          clearAllDrafts();
          setUser(null);
          setStatus("anonymous");
        }
      },
      updateProfile: async (displayName) => {
        const payload = await sendJson("/api/auth/profile", "PUT", { displayName });
        setUser(readData(payload, userSessionSchema));
      },
      changePassword: async (currentPassword, newPassword) => {
        const payload = await sendJson("/api/auth/password", "POST", { currentPassword, newPassword });
        const session = readData(payload, loginResponseSchema);
        setAccessToken(session.accessToken);
        setUser(session.user);
      },
    }),
    [status, user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return value;
}
