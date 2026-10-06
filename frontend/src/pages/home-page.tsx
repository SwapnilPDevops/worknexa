import { useMemo, useState } from "react";
import { AppShell } from "../layouts/AppShell";
import { Dashboard } from "../features/dashboard/Dashboard";
import {
  initialTasks,
  projects as seedProjects,
  type ColumnId,
  type NavId,
  type Period,
  type Priority,
  type Project,
  type Task,
} from "../data/board";

export function HomePage() {
  const [collapsed, setCollapsed] = useState(false);
  const [activeNav, setActiveNav] = useState<NavId>("home");
  const [projectList, setProjectList] = useState<Project[]>(seedProjects);
  const [projectId, setProjectId] = useState("mobile-app");
  const [tasks, setTasks] = useState<Task[]>(initialTasks);
  const [query, setQuery] = useState("");
  const [priority, setPriority] = useState<Priority | "All">("All");
  const [period, setPeriod] = useState<Period>("Today");
  const [view, setView] = useState<"board" | "list">("board");
  const [liftedId, setLiftedId] = useState<string | null>("research");
  const [dirty, setDirty] = useState(false);
  const [editing, setEditing] = useState(false);

  const project = projectList.find((item) => item.id === projectId) ?? projectList[0];

  const visibleTasks = useMemo(() => {
    if (project.id !== "mobile-app") return [];
    const needle = query.trim().toLowerCase();
    return tasks.filter((task) => {
      const matchesPriority = priority === "All" || task.priority === priority;
      const haystack = `${task.title} ${task.description ?? ""}`.toLowerCase();
      const matchesQuery = needle.length === 0 || haystack.includes(needle);
      return matchesPriority && matchesQuery;
    });
  }, [project.id, priority, query, tasks]);

  const pristine =
    !dirty && query.trim() === "" && priority === "All" && project.id === "mobile-app" && view === "board";

  const moveTask = (id: string, column: ColumnId) => {
    setTasks((current) => current.map((task) => (task.id === id ? { ...task, column } : task)));
    setDirty(true);
    setLiftedId(null);
  };

  const addTask = (title: string) => {
    const task: Task = {
      id: `task-${Date.now()}`,
      title,
      description: "New task",
      priority: "Low",
      comments: 0,
      files: 0,
      avatars: ["/assets/avatar-c.png"],
      column: "todo",
    };
    setTasks((current) => [task, ...current]);
    setDirty(true);
    setLiftedId(null);
  };

  return (
    <AppShell
      collapsed={collapsed}
      onToggle={() => setCollapsed((value) => !value)}
      activeNav={activeNav}
      onNavigate={setActiveNav}
      projects={projectList}
      activeProjectId={project.id}
      onSelectProject={setProjectId}
      onAddProject={(name) => {
        const id = name.toLowerCase().replace(/\s+/g, "-");
        setProjectList((current) => [...current, { id, name, dot: "dot-blue" }]);
        setProjectId(id);
      }}
      query={query}
      onQuery={setQuery}
      >
      <Dashboard
        project={project}
        tasks={visibleTasks}
        pristine={pristine}
        priority={priority}
        period={period}
        view={view}
        liftedId={pristine ? liftedId : null}
        editing={editing}
        onPriority={setPriority}
        onPeriod={setPeriod}
        onView={setView}
        onMove={moveTask}
        onAdd={addTask}
        onLiftClear={() => {
          setLiftedId(null);
          setDirty(true);
        }}
        onRename={(name) => {
          setProjectList((current) => current.map((item) => (item.id === project.id ? { ...item, name } : item)));
        }}
        onEditing={setEditing}
      />
    </AppShell>
  );
}
