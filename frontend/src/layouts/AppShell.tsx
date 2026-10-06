import type { ReactNode } from "react";
import type { NavId, Project } from "../data/board";
import { Header } from "./Header";
import { Sidebar } from "./Sidebar";

type AppShellProps = {
  collapsed: boolean;
  onToggle: () => void;
  activeNav: NavId;
  onNavigate: (id: NavId) => void;
  projects: Project[];
  activeProjectId: string;
  onSelectProject: (id: string) => void;
  onAddProject: (name: string) => void;
  query: string;
  onQuery: (value: string) => void;
  children: ReactNode;
};

export function AppShell({
  collapsed,
  onToggle,
  activeNav,
  onNavigate,
  projects,
  activeProjectId,
  onSelectProject,
  onAddProject,
  query,
  onQuery,
  children,
}: AppShellProps) {
  return (
    <div className="page">
      <div className={`shell${collapsed ? " is-collapsed" : ""}`}>
        <Sidebar
          collapsed={collapsed}
          onToggle={onToggle}
          activeNav={activeNav}
          onNavigate={onNavigate}
          projects={projects}
          activeProjectId={activeProjectId}
          onSelectProject={onSelectProject}
          onAddProject={onAddProject}
        />
        <Header query={query} onQuery={onQuery} />
        <main className="workspace">{children}</main>
      </div>
      <p className="watermark">www.nickelfox.com</p>
    </div>
  );
}

