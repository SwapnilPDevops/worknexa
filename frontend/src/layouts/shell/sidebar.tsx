import { PanelLeftClose, PanelLeftOpen } from "lucide-react";
import type { KeyboardEvent } from "react";
import { Link, NavLink } from "react-router-dom";
import { useAuth } from "../../auth/auth-context.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { cn } from "../../utils/cn.ts";
import { visibleNavSections } from "./nav.ts";
import { useShell } from "./shell-context.tsx";

type SidebarNavProps = {
  collapsed: boolean;
  onNavigate?: () => void;
};

export function SidebarNav({ collapsed, onNavigate }: SidebarNavProps) {
  const { user } = useAuth();
  const sections = visibleNavSections(user?.permissions);

  return (
    <nav aria-label="Primary" className="flex min-h-0 flex-1 flex-col" onKeyDown={moveFocus}>
      <div className="min-h-0 flex-1 space-y-5 overflow-y-auto px-2 py-3">
        {sections.map((section) => (
          <div key={section.label}>
            {collapsed ? null : (
              <p className="px-2 pb-1 text-[11px] font-semibold tracking-[0.14em] text-muted uppercase">{section.label}</p>
            )}
            <ul className="space-y-0.5">
              {section.items.map((item) => {
                const Icon = item.icon;
                const link = (
                  <NavLink
                    to={item.to}
                    end={item.to === "/"}
                    onClick={onNavigate}
                    title={collapsed ? item.label : undefined}
                    aria-label={collapsed ? item.label : undefined}
                    className={({ isActive }) =>
                      cn(
                        "flex h-8 items-center gap-2 rounded-lg px-2 text-[13px] text-muted transition-colors hover:bg-surface-muted hover:text-ink",
                        collapsed && "justify-center px-0",
                        isActive && "bg-accent-soft font-medium text-accent",
                      )
                    }
                  >
                    <Icon size={16} aria-hidden="true" />
                    {collapsed ? null : <span className="truncate">{item.label}</span>}
                  </NavLink>
                );
                return (
                  <li key={item.to}>{link}</li>
                );
              })}
            </ul>
          </div>
        ))}
      </div>
    </nav>
  );
}

function moveFocus(event: KeyboardEvent<HTMLElement>) {
  if (event.key !== "ArrowDown" && event.key !== "ArrowUp") {
    return;
  }
  const links = [...event.currentTarget.querySelectorAll("a")];
  const index = links.indexOf(document.activeElement as HTMLAnchorElement);
  if (index < 0) {
    return;
  }
  event.preventDefault();
  const next = event.key === "ArrowDown" ? (links[index + 1] ?? links[0]) : (links[index - 1] ?? links[links.length - 1]);
  next?.focus();
}

export function Sidebar() {
  const { sidebarCollapsed, toggleSidebar } = useShell();

  return (
    <aside
      className="sticky top-0 hidden h-dvh flex-col border-r border-line bg-surface transition-[width] duration-200 lg:flex"
      style={{ width: sidebarCollapsed ? "var(--wx-sidebar-collapsed)" : "var(--wx-sidebar)" }}
    >
      <Brand collapsed={sidebarCollapsed} />
      <SidebarNav collapsed={sidebarCollapsed} />
      <div className="border-t border-line p-2">
        <Button variant="ghost" className={cn("w-full", sidebarCollapsed ? "px-0" : "justify-start")} onClick={toggleSidebar} aria-pressed={sidebarCollapsed}>
          {sidebarCollapsed ? <PanelLeftOpen size={16} aria-hidden="true" /> : <PanelLeftClose size={16} aria-hidden="true" />}
          {sidebarCollapsed ? <span className="sr-only">Expand sidebar</span> : <span>Collapse</span>}
        </Button>
      </div>
    </aside>
  );
}

export function Brand({ collapsed }: { collapsed?: boolean }) {
  return (
    <Link to="/" className={cn("flex h-14 items-center gap-2.5 border-b border-line px-3", collapsed && "justify-center px-0")}>
      <img src="/brand/worknexa-mark.svg" alt="" width={32} height={32} className="size-8 shrink-0" />
      {collapsed ? <span className="sr-only">Worknexa</span> : <BrandText />}
    </Link>
  );
}

function BrandText() {
  return (
    <span className="min-w-0">
      <span className="block text-sm font-semibold tracking-tight text-ink">Worknexa</span>
      <span className="block truncate text-[11px] text-muted">Your Work. One Intelligent Place.</span>
    </span>
  );
}
