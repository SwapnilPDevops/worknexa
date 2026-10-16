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
              <p className="px-2 pb-1.5 text-[11px] font-semibold tracking-[0.14em] text-muted/80 uppercase">{section.label}</p>
            )}
            <ul className="space-y-1">
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
                        "flex h-8.5 items-center gap-2.5 rounded-[8px] px-2.5 text-xs text-[#4B5563] transition-[background-color,color,border-color] duration-150 hover:bg-[#EBE8E2] hover:text-[#111827]",
                        collapsed && "justify-center px-0",
                        isActive && "bg-[#FED7AA] font-semibold text-[#111827] border border-[#111827]/15 shadow-xs [&>svg]:text-[#111827]",
                      )
                    }
                  >
                    <Icon size={16} aria-hidden="true" className="shrink-0" />
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
      className="sticky top-0 hidden h-dvh flex-col border-r border-[#D1CCC2] bg-[#DFDCD6] transition-[width] duration-200 lg:flex"
      style={{ width: sidebarCollapsed ? "var(--wx-sidebar-collapsed)" : "var(--wx-sidebar)" }}
    >
      <Brand collapsed={sidebarCollapsed} />
      <SidebarNav collapsed={sidebarCollapsed} />
      <div className="border-t border-[#D1CCC2] p-2">
        <Button variant="ghost" className={cn("w-full text-[#4B5563] hover:text-[#111827] hover:bg-[#EBE8E2]", sidebarCollapsed ? "px-0" : "justify-start")} onClick={toggleSidebar} aria-pressed={sidebarCollapsed}>
          {sidebarCollapsed ? <PanelLeftOpen size={16} aria-hidden="true" /> : <PanelLeftClose size={16} aria-hidden="true" />}
          {sidebarCollapsed ? <span className="sr-only">Expand sidebar</span> : <span className="text-xs">Collapse</span>}
        </Button>
      </div>
    </aside>
  );
}

export function Brand({ collapsed }: { collapsed?: boolean }) {
  return (
    <Link to="/" className={cn("flex h-14 items-center gap-2.5 border-b border-[#D1CCC2] px-3.5", collapsed && "justify-center px-0")}>
      <img src="/brand/worknexa-mark.svg" alt="" width={28} height={28} className="size-7 shrink-0" />
      {collapsed ? <span className="sr-only">Worknexa</span> : <BrandText />}
    </Link>
  );
}

function BrandText() {
  return (
    <span className="min-w-0">
      <span className="block text-sm font-semibold tracking-tight text-[#111827]">Worknexa</span>
      <span className="block truncate text-[10px] text-[#4B5563] font-mono">SYS.DOC // WORKNEXA</span>
    </span>
  );
}
