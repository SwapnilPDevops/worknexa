import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/auth-context.tsx";
import { can, permissions } from "../../auth/access.ts";
import { Dialog } from "../../components/design-system/dialog.tsx";
import { useTheme, type ThemePreference } from "../../app/theme.tsx";
import { searchWorkspace } from "../../features/search/api.ts";
import { visibleNavSections } from "./nav.ts";
import { useShell } from "./shell-context.tsx";

type Command = {
  id: string;
  label: string;
  detail: string;
  group: string;
  run: () => void;
};

type PaletteItem = {
  key: string;
  label: string;
  detail: string;
  run: () => void;
};

type Section = {
  key: string;
  label: string;
  items: PaletteItem[];
};

export function CommandPalette() {
  const { commandOpen, setCommandOpen, setQuickAddOpen, toggleSidebar } = useShell();
  const { setPreference } = useTheme();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const activeRef = useRef<HTMLButtonElement>(null);
  const granted = user?.permissions;

  const commands = useMemo<Command[]>(() => {
    const items: Command[] = [];
    const create = (id: string, label: string, kind: string, permission: string) => {
      if (!can(granted, permission)) {
        return;
      }
      items.push({
        id,
        label,
        detail: "Create",
        group: "Create",
        run: () => setQuickAddOpen(true, kind),
      });
    };
    create("create-task", "Create Task", "task", permissions.tasksManage);
    create("create-todo", "Create Todo", "todo", permissions.tasksManage);
    create("create-meeting", "Create Meeting", "meeting", permissions.meetingsManage);
    create("create-reminder", "Create Reminder", "reminder", permissions.tasksManage);
    create("create-note", "Create Note", "note", permissions.notesManage);
    if (can(granted, permissions.projectsManage)) {
      items.push({
        id: "create-project",
        label: "Create Project",
        detail: "Create",
        group: "Create",
        run: () => navigate("/projects?compose=1"),
      });
    }
    if (can(granted, permissions.tasksManage)) {
      items.push({
        id: "assign-task",
        label: "Assign Task",
        detail: "Create",
        group: "Create",
        run: () => setQuickAddOpen(true, "team-task"),
      });
    }
    const open = (id: string, label: string, to: string, permission?: string) => {
      if (permission && !can(granted, permission)) {
        return;
      }
      items.push({
        id,
        label,
        detail: "Open",
        group: "Open",
        run: () => navigate(to),
      });
    };
    open("open-dashboard", "Open Dashboard", "/");
    open("open-calendar", "Open Calendar", "/calendar", permissions.calendarRead);
    open("open-team", "Open Team", "/team", permissions.teamsRead);
    open("open-learning", "Open Learning", "/learning", permissions.learningRead);

    for (const section of visibleNavSections(granted)) {
      for (const item of section.items) {
        items.push({
          id: item.to,
          label: item.label,
          detail: section.label,
          group: section.label,
          run: () => navigate(item.to),
        });
      }
    }
    for (const preference of ["light", "dark", "system"] as ThemePreference[]) {
      items.push({
        id: `theme-${preference}`,
        label: `Use ${preference} theme`,
        detail: "Appearance",
        group: "Appearance",
        run: () => setPreference(preference),
      });
    }
    items.push({ id: "sidebar", label: "Toggle sidebar", detail: "Layout", group: "Layout", run: toggleSidebar });
    return items;
  }, [granted, navigate, setPreference, setQuickAddOpen, toggleSidebar]);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(query.trim()), 200);
    return () => window.clearTimeout(handle);
  }, [query]);

  const search = useQuery({
    queryKey: ["workspace-search", debounced],
    queryFn: () => searchWorkspace(debounced),
    enabled: commandOpen && debounced.length > 0,
  });

  const sections = useMemo<Section[]>(() => {
    const needle = query.trim().toLowerCase();
    const grouped = new Map<string, PaletteItem[]>();
    for (const command of commands) {
      const haystack = `${command.label} ${command.detail}`.toLowerCase();
      if (needle && !haystack.includes(needle)) {
        continue;
      }
      const list = grouped.get(command.group) ?? [];
      list.push({ key: command.id, label: command.label, detail: command.detail, run: command.run });
      grouped.set(command.group, list);
    }
    const commandSections = [...grouped.entries()].map(([label, items]) => ({ key: label, label, items }));
    const entitySections = (debounced.length > 0 ? search.data ?? [] : []).map((group) => ({
      key: group.entity,
      label: group.label,
      items: group.hits.map((hit) => ({
        key: `${hit.entity}-${hit.id}`,
        label: hit.title,
        detail: hit.detail ?? group.label,
        run: () => navigate(hit.route),
      })),
    }));
    return [...commandSections, ...entitySections];
  }, [commands, debounced.length, navigate, query, search.data]);

  const items = sections.flatMap((section) => section.items);
  const signature = items.map((item) => item.key).join("|");
  const [seenSignature, setSeenSignature] = useState(signature);
  if (!commandOpen) {
    if (query) {
      setQuery("");
    }
    if (debounced) {
      setDebounced("");
    }
    if (activeIndex !== 0) {
      setActiveIndex(0);
    }
    if (seenSignature !== signature) {
      setSeenSignature(signature);
    }
  } else if (seenSignature !== signature) {
    setSeenSignature(signature);
    if (activeIndex !== 0) {
      setActiveIndex(0);
    }
  }

  useEffect(() => {
    activeRef.current?.scrollIntoView({ block: "nearest" });
  }, [activeIndex, signature]);

  function runActive() {
    const item = items[activeIndex];
    if (!item) {
      return;
    }
    item.run();
    setCommandOpen(false);
  }

  let cursor = 0;

  return (
    <Dialog open={commandOpen} title="Search" description="Search Worknexa, or run a command." onClose={() => setCommandOpen(false)} className="w-[min(36rem,calc(100vw-2rem))]">
      <label className="sr-only" htmlFor="command-search">
        Search Worknexa
      </label>
      <input
        id="command-search"
        data-autofocus
        value={query}
        placeholder="Search tasks, projects, people, notes…"
        className="h-11 w-full border-b border-line bg-transparent px-4 text-sm outline-none placeholder:text-muted"
        onChange={(event) => setQuery(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === "ArrowDown") {
            event.preventDefault();
            setActiveIndex((index) => (items.length === 0 ? 0 : (index + 1) % items.length));
          }
          if (event.key === "ArrowUp") {
            event.preventDefault();
            setActiveIndex((index) => (items.length === 0 ? 0 : (index - 1 + items.length) % items.length));
          }
          if (event.key === "Enter") {
            event.preventDefault();
            runActive();
          }
        }}
      />
      <div role="listbox" aria-label="Search results" className="max-h-80 overflow-y-auto p-2">
        {search.isError && debounced ? <p className="px-3 py-2 text-sm text-muted">Search is unavailable.</p> : null}
        {query.trim() && (query.trim() !== debounced || search.isFetching) && items.length === 0 ? <p className="px-3 py-2 text-sm text-muted">Searching…</p> : null}
        {items.length === 0 && !(query.trim() && (query.trim() !== debounced || search.isFetching)) ? <p className="px-3 py-6 text-sm text-muted">No matches.</p> : null}
        {sections.map((section) => (
          <section key={section.key} aria-label={section.label} className="mb-2">
            <h3 className="px-3 py-1 text-xs font-medium text-muted">{section.label}</h3>
            <ul>
              {section.items.map((item) => {
                const index = cursor;
                cursor += 1;
                const active = index === activeIndex;
                return (
                  <li key={item.key}>
                    <button
                      type="button"
                      role="option"
                      aria-selected={active}
                      ref={active ? activeRef : undefined}
                      className={
                        active
                          ? "flex w-full items-center justify-between gap-3 rounded-[8px] bg-[#FED7AA] border border-[#111827]/15 px-3 py-2 text-left text-[#111827] shadow-xs"
                          : "flex w-full items-center justify-between gap-3 rounded-[8px] px-3 py-2 text-left text-[#4B5563] hover:bg-[#D7D3CB]/60 hover:text-[#111827] transition-colors"
                      }
                      onMouseEnter={() => setActiveIndex(index)}
                      onClick={() => {
                        item.run();
                        setCommandOpen(false);
                      }}
                    >
                      <span className="truncate text-sm text-ink">{item.label}</span>
                      <span className="shrink-0 text-xs text-muted">{item.detail}</span>
                    </button>
                  </li>
                );
              })}
            </ul>
          </section>
        ))}
      </div>
      <p className="border-t border-line px-4 py-2 text-xs text-muted">↑↓ to move, Enter to open, Esc to close</p>
    </Dialog>
  );
}
