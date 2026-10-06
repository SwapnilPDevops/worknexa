import type { ColumnId, Period, Priority, Project, Task } from "../../data/board";
import { members, periods, priorityFilters } from "../../data/board";
import { OutlineButton } from "../../components/ui/Button";
import { Dropdown } from "../../components/ui/Dropdown";
import { GridIcon, Icon, LinkIcon } from "../../components/ui/Icon";
import { AvatarGroup } from "../../components/ui/AvatarGroup";
import { Board } from "./Board";

type DashboardProps = {
  project: Project;
  tasks: Task[];
  pristine: boolean;
  priority: Priority | "All";
  period: Period;
  view: "board" | "list";
  liftedId: string | null;
  editing: boolean;
  onPriority: (value: Priority | "All") => void;
  onPeriod: (value: Period) => void;
  onView: (view: "board" | "list") => void;
  onMove: (id: string, column: ColumnId) => void;
  onAdd: (title: string) => void;
  onLiftClear: () => void;
  onRename: (name: string) => void;
  onEditing: (editing: boolean) => void;
};

export function Dashboard({
  project,
  tasks,
  pristine,
  priority,
  period,
  view,
  liftedId,
  editing,
  onPriority,
  onPeriod,
  onView,
  onMove,
  onAdd,
  onLiftClear,
  onRename,
  onEditing,
}: DashboardProps) {
  return (
    <div className="page-container">
      <div className="title-row">
        <div className="title-block">
          {editing ? (
            <input
              className="title-input"
              defaultValue={project.name}
              aria-label="Project name"
              autoFocus
              onBlur={(event) => {
                onRename(event.target.value.trim() || project.name);
                onEditing(false);
              }}
              onKeyDown={(event) => {
                if (event.key === "Enter") (event.target as HTMLInputElement).blur();
                if (event.key === "Escape") onEditing(false);
              }}
            />
          ) : (
            <h1>{project.name}</h1>
          )}
          <button type="button" className="icon-hit" aria-label="Rename project" onClick={() => onEditing(true)}>
            <Icon src="/assets/edit.svg" width={30} height={30} alt="" />
          </button>
          <button
            type="button"
            className="icon-hit"
            aria-label="Copy project link"
            onClick={() => navigator.clipboard?.writeText(window.location.href)}
          >
            <LinkIcon />
          </button>
        </div>
        <div className="invite-row">
          <button type="button" className="invite">
            <Icon src="/assets/invite.svg" width={18} height={18} alt="" />
            Invite
          </button>
          <div className="member-stack">
            <AvatarGroup sources={members} size={38} overlap={8} />
            <span className="plus-count">
              <img src="/assets/plus-count.svg" width={38} height={38} alt="" />
              <span>+2</span>
            </span>
          </div>
        </div>
      </div>

      <div className="toolbar">
        <div className="toolbar-filters">
          <Dropdown
            label="Filter"
            value={priority}
            display={priority === "All" ? "Filter" : priority}
            options={priorityFilters}
            width={122}
            icon={<Icon src="/assets/filter.svg" width={16} height={16} alt="" />}
            onChange={(value) => onPriority(value as Priority | "All")}
          />
          <Dropdown
            label="Today"
            value={period}
            options={periods}
            width={122}
            icon={<Icon src="/assets/calendar-sm.svg" width={16} height={16} alt="" />}
            onChange={(value) => onPeriod(value as Period)}
          />
        </div>
        <div className="toolbar-actions">
          <OutlineButton label="Share" width={97}>
            <Icon src="/assets/share.svg" width={16} height={16} alt="" />
            <span>Share</span>
          </OutlineButton>
          <span className="toolbar-rule" />
          <button
            type="button"
            className={`view-button${view === "board" ? " is-active" : ""}`}
            aria-label="Board view"
            aria-pressed={view === "board"}
            onClick={() => onView("board")}
          >
            <span className="view-glyph">
              <Icon src="/assets/board-view.svg" width={20} height={20} alt="" />
            </span>
          </button>
          <button
            type="button"
            className={`view-grid${view === "list" ? " is-active" : ""}`}
            aria-label="List view"
            aria-pressed={view === "list"}
            onClick={() => onView("list")}
          >
            <GridIcon />
          </button>
        </div>
      </div>

      <div className={view === "list" ? "board-wrap is-list" : "board-wrap"}>
        <Board
          tasks={tasks}
          pristine={pristine && view === "board"}
          onMove={onMove}
          onAdd={onAdd}
          liftedId={view === "board" ? liftedId : null}
          onLiftClear={onLiftClear}
        />
      </div>
    </div>
  );
}
