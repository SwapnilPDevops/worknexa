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
    <div ref={ref} role="menu" aria-label="Account" className="wx-popover absolute top-12 right-0 z-30 w-64 rounded-[8px] border border-[#D1CCC2] bg-[#EBE8E2] p-2 shadow-[var(--wx-shadow-panel)]">
      <div className="flex items-center gap-2 px-2 py-2">
        <Avatar name={user?.displayName ?? "Worknexa"} />
        <div className="min-w-0">
          <p className="truncate text-sm font-semibold text-[#111827]">{user?.displayName}</p>
          <p className="truncate text-xs text-[#4B5563]">{user?.email}</p>
        </div>
      </div>
      <p className="px-2 pt-2 text-[10px] font-semibold tracking-wide text-[#4B5563] uppercase">Theme</p>
      <div className="grid grid-cols-3 gap-1 p-2">
        {(["light", "dark", "system"] as ThemePreference[]).map((option) => (
          <button
            key={option}
            type="button"
            role="menuitemradio"
            aria-checked={preference === option}
            className={
              preference === option
                ? "rounded-[6px] bg-[#FED7AA] border border-[#111827]/15 px-2 py-1 text-xs font-semibold text-[#111827] capitalize shadow-xs"
                : "rounded-[6px] px-2 py-1 text-xs text-[#4B5563] capitalize hover:bg-[#D7D3CB]/60 hover:text-[#111827] transition-colors"
            }
            onClick={() => setPreference(option)}
          >
            {option}
          </button>
        ))}
      </div>
      <Link role="menuitem" to="/profile" className="block rounded-[6px] px-2 py-1.5 text-xs text-[#4B5563] hover:text-[#111827] hover:bg-[#D7D3CB]/60 transition-colors" onClick={() => setUserMenuOpen(false)}>
        Profile
      </Link>
      <Link role="menuitem" to="/settings" className="block rounded-[6px] px-2 py-1.5 text-xs text-[#4B5563] hover:text-[#111827] hover:bg-[#D7D3CB]/60 transition-colors" onClick={() => setUserMenuOpen(false)}>
        Settings
      </Link>
      <Link role="menuitem" to="/foundations" className="block rounded-[6px] px-2 py-1.5 text-xs text-[#4B5563] hover:text-[#111827] hover:bg-[#D7D3CB]/60 transition-colors" onClick={() => setUserMenuOpen(false)}>
        Design system
      </Link>
      <button
        type="button"
        role="menuitem"
        className="block w-full rounded-[6px] px-2 py-1.5 text-left text-xs text-[#4B5563] hover:text-[#111827] hover:bg-[#D7D3CB]/60 transition-colors"
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
