import { useEffect, useRef } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/auth-context.tsx";
import { Avatar } from "../../components/design-system/avatar.tsx";
import { useTheme, type ThemePreference } from "../../app/theme.tsx";
import { useShell } from "./shell-context.tsx";

export function UserMenu() {
  const { userMenuOpen, setUserMenuOpen } = useShell();
  const { preference, setPreference } = useTheme();
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!userMenuOpen) {
      return;
    }
    function onPointer(event: MouseEvent) {
      if (!ref.current?.contains(event.target as Node)) {
        setUserMenuOpen(false);
      }
    }
    function onKey(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setUserMenuOpen(false);
      }
    }
    window.addEventListener("mousedown", onPointer);
    window.addEventListener("keydown", onKey);
    return () => {
      window.removeEventListener("mousedown", onPointer);
      window.removeEventListener("keydown", onKey);
    };
  }, [setUserMenuOpen, userMenuOpen]);

  if (!userMenuOpen) {
    return null;
  }

  return (
    <div ref={ref} role="menu" aria-label="Account" className="wx-popover absolute top-12 right-0 z-30 w-64 rounded-xl border border-line bg-surface p-2 shadow-[var(--wx-shadow-panel)]">
      <div className="flex items-center gap-2 px-2 py-2">
        <Avatar name={user?.displayName ?? "Worknexa"} />
        <div className="min-w-0">
          <p className="truncate text-sm font-medium">{user?.displayName}</p>
          <p className="truncate text-xs text-muted">{user?.email}</p>
        </div>
      </div>
      <p className="px-2 pt-2 text-[11px] font-semibold tracking-wide text-muted uppercase">Theme</p>
      <div className="grid grid-cols-3 gap-1 p-2">
        {(["light", "dark", "system"] as ThemePreference[]).map((option) => (
          <button
            key={option}
            type="button"
            role="menuitemradio"
            aria-checked={preference === option}
            className={preference === option ? "rounded-md bg-accent-soft px-2 py-1 text-xs font-medium text-accent capitalize" : "rounded-md px-2 py-1 text-xs text-muted capitalize hover:bg-surface-muted"}
            onClick={() => setPreference(option)}
          >
            {option}
          </button>
        ))}
      </div>
      <Link role="menuitem" to="/profile" className="block rounded-md px-2 py-2 text-sm hover:bg-surface-muted" onClick={() => setUserMenuOpen(false)}>
        Profile
      </Link>
      <Link role="menuitem" to="/settings" className="block rounded-md px-2 py-2 text-sm hover:bg-surface-muted" onClick={() => setUserMenuOpen(false)}>
        Settings
      </Link>
      <Link role="menuitem" to="/foundations" className="block rounded-md px-2 py-2 text-sm hover:bg-surface-muted" onClick={() => setUserMenuOpen(false)}>
        Design system
      </Link>
      <button
        type="button"
        role="menuitem"
        className="block w-full rounded-md px-2 py-2 text-left text-sm hover:bg-surface-muted"
        onClick={() => {
          setUserMenuOpen(false);
          void logout().then(() => navigate("/login", { replace: true }));
        }}
      >
        Sign out
      </button>
    </div>
  );
}
