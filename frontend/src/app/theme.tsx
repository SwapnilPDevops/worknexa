import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";

export type ThemePreference = "light" | "dark" | "system";

const storageKey = "worknexa-theme";

type ThemeContextValue = {
  preference: ThemePreference;
  resolved: "light" | "dark";
  setPreference: (preference: ThemePreference) => void;
};

const ThemeContext = createContext<ThemeContextValue | null>(null);

function systemIsDark() {
  return window.matchMedia("(prefers-color-scheme: dark)").matches;
}

function resolveTheme(preference: ThemePreference): "light" | "dark" {
  if (preference === "system") {
    return systemIsDark() ? "dark" : "light";
  }
  return preference;
}

function readPreference(): ThemePreference {
  const stored = localStorage.getItem(storageKey);
  if (stored === "light" || stored === "dark" || stored === "system") {
    return stored;
  }
  return "system";
}

function applyTheme(preference: ThemePreference) {
  const resolved = resolveTheme(preference);
  document.documentElement.dataset.theme = resolved;
  document.documentElement.dataset.themePreference = preference;
  return resolved;
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [preference, setPreferenceState] = useState<ThemePreference>(() => readPreference());
  const [resolved, setResolved] = useState<"light" | "dark">(() => applyTheme(readPreference()));

  useEffect(() => {
    setResolved(applyTheme(preference));
    if (preference !== "system") {
      return;
    }
    const media = window.matchMedia("(prefers-color-scheme: dark)");
    const onChange = () => setResolved(applyTheme("system"));
    media.addEventListener("change", onChange);
    return () => media.removeEventListener("change", onChange);
  }, [preference]);

  const value = useMemo<ThemeContextValue>(
    () => ({
      preference,
      resolved,
      setPreference: (next) => {
        localStorage.setItem(storageKey, next);
        setPreferenceState(next);
      },
    }),
    [preference, resolved],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const value = useContext(ThemeContext);
  if (!value) {
    throw new Error("useTheme must be used within ThemeProvider");
  }
  return value;
}
