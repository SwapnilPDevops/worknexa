import type { NavId, Project } from "../data/board";
import { navItems } from "../data/board";
import { CollapseIcon, Icon } from "../components/ui/Icon";
import { ThoughtsCard } from "../features/dashboard/ThoughtsCard";

type SidebarProps = {
  collapsed: boolean;
  onToggle: () => void;
  activeNav: NavId;
  onNavigate: (id: NavId) => void;
  projects: Project[];
  activeProjectId: string;
  onSelectProject: (id: string) => void;
  onAddProject: (name: string) => void;
};

export function Sidebar({
  collapsed,
  onToggle,
  activeNav,
  onNavigate,
  projects,
  activeProjectId,
  onSelectProject,
  onAddProject,
}: SidebarProps) {
  const addProject = () => {
    const name = window.prompt("Project name");
    if (name?.trim()) onAddProject(name.trim());
  };

  return (
    <>
      <div className={`brand${collapsed ? " is-collapsed" : ""}`}>
        <Icon src="/assets/logo.svg" width={24} height={24} alt="" />
        {collapsed ? null : <span className="brand-name">Project M.</span>}
        <button type="button" className={`collapse-button${collapsed ? " is-flipped" : ""}`} aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"} onClick={onToggle}>
          <CollapseIcon />
        </button>
      </div>
      <aside className={`sidebar${collapsed ? " is-collapsed" : ""}`}>
        <nav className="side-nav" aria-label="Primary">
          {navItems.map((item) => (
            <button
              key={item.id}
              type="button"
              className={`nav-item${item.id === activeNav ? " is-current" : ""}`}
              onClick={() => onNavigate(item.id)}
            >
              <Icon src={`/assets/${item.icon}.svg`} width={24} height={24} alt="" />
              {collapsed ? null : <span>{item.label}</span>}
            </button>
          ))}
        </nav>
        <div className="projects-divider" />
        <div className="projects-head">
          {collapsed ? null : <span>my projects</span>}
          <button type="button" className="icon-hit" aria-label="Add project" onClick={addProject}>
            <Icon src="/assets/add-square.svg" width={16} height={16} alt="" />
          </button>
        </div>
        <ul className="project-list">
          {projects.map((project) => {
            const active = project.id === activeProjectId;
            return (
              <li key={project.id}>
                <button
                  type="button"
                  className={`project-item${active ? " is-active" : ""}`}
                  onClick={() => onSelectProject(project.id)}
                >
                  <Icon src={`/assets/${project.dot}.svg`} width={8} height={8} alt="" />
                  {collapsed ? null : <span className="project-name">{project.name}</span>}
                  {active && !collapsed ? <span className="project-more">. . .</span> : null}
                </button>
              </li>
            );
          })}
        </ul>
        {collapsed ? null : <ThoughtsCard />}
      </aside>
    </>
  );
}
