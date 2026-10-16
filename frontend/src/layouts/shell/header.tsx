import { useQuery } from "@tanstack/react-query";
import { Bell, Menu, Plus, Search } from "lucide-react";
import { useLocation } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { fetchNotifications } from "../../features/schedule/api.ts";
import { Avatar } from "../../components/design-system/avatar.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { findNavItem } from "./nav.ts";
import { useShell } from "./shell-context.tsx";
import { UserMenu } from "./user-menu.tsx";

function modifierLabel() {
  return /Mac|iPhone|iPad/.test(navigator.userAgent) ? "⌘K" : "Ctrl K";
}

export function Header() {
  const { pathname } = useLocation();
  const item = findNavItem(pathname);
  const { setCommandOpen, setNotificationsOpen, setQuickAddOpen, setUserMenuOpen, setMobileNavOpen, userMenuOpen } = useShell();
  const { user } = useAuth();
  const notifications = useQuery({
    queryKey: ["notifications"],
    queryFn: fetchNotifications,
    enabled: can(user?.permissions, permissions.notificationsRead),
  });
  const unread = notifications.data?.some((item) => !item.isRead) ?? false;

  return (
    <header className="sticky top-0 z-20 flex h-14 items-center gap-3 border-b border-[#D1CCC2] bg-[#DFDCD6]/90 px-4 backdrop-blur-[4px] sm:px-6">
      <Button className="lg:hidden" variant="ghost" size="icon" aria-label="Open navigation" onClick={() => setMobileNavOpen(true)}>
        <Menu size={18} aria-hidden="true" />
      </Button>
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-semibold text-[#111827] tracking-tight">{item?.label ?? "Worknexa"}</p>
      </div>
      <button
        type="button"
        className="hidden h-9 min-w-64 items-center gap-2.5 rounded-[8px] border border-[#D1CCC2] bg-[#EBE8E2] px-3 text-xs text-[#4B5563] transition-colors hover:border-[#111827]/40 hover:text-[#111827] md:flex cursor-pointer"
        onClick={() => setCommandOpen(true)}
      >
        <Search size={14} className="text-[#4B5563]" aria-hidden="true" />
        <span className="flex-1 text-left">Search or jump</span>
        <kbd className="rounded-[4px] border border-[#D1CCC2] bg-[#DFDCD6] px-1.5 py-0.5 text-[10px] font-mono text-[#4B5563]">{modifierLabel()}</kbd>
      </button>
      <Button className="md:hidden" variant="ghost" size="icon" aria-label="Search" onClick={() => setCommandOpen(true)}>
        <Search size={18} aria-hidden="true" />
      </Button>
      <Button variant="secondary" size="sm" onClick={() => setQuickAddOpen(true)}>
        <Plus size={15} aria-hidden="true" />
        <span className="hidden sm:inline">Quick add</span>
      </Button>
      <Button variant="ghost" size="icon" className="relative text-[#4B5563] hover:text-[#111827]" aria-label={unread ? "Open notifications, unread" : "Open notifications"} onClick={() => setNotificationsOpen(true)}>
        <Bell size={18} aria-hidden="true" />
        {unread ? <span className="absolute right-1.5 top-1.5 size-2 rounded-full bg-danger ring-2 ring-[#DFDCD6]" /> : null}
      </Button>
      <div className="relative">
        <button
          type="button"
          className="rounded-full ring-1 ring-[#D1CCC2] hover:ring-[#111827] transition-all cursor-pointer"
          aria-haspopup="menu"
          aria-expanded={userMenuOpen}
          aria-label="Open account menu"
          onClick={() => setUserMenuOpen(!userMenuOpen)}
        >
          <Avatar name={user?.displayName ?? "Worknexa"} />
        </button>
        <UserMenu />
      </div>
    </header>
  );
}
