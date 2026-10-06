import { useEffect, useRef, useState } from "react";
import type { ColumnId, Task } from "../../data/board";
import { columns } from "../../data/board";
import { AvatarGroup } from "../../components/ui/AvatarGroup";
import { PriorityBadge } from "../../components/ui/Badge";
import { FolderIcon, Icon } from "../../components/ui/Icon";

type TaskCardProps = {
  task: Task;
  lifted?: boolean;
  onMove: (id: string, column: ColumnId) => void;
  onDragStart: (id: string) => void;
  onDragEnd: () => void;
};

export function TaskCard({ task, lifted, onMove, onDragStart, onDragEnd }: TaskCardProps) {
  const [menuOpen, setMenuOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!menuOpen) return;
    const close = (event: MouseEvent) => {
      if (!menuRef.current?.contains(event.target as Node)) setMenuOpen(false);
    };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, [menuOpen]);

  return (
    <article
      className={`task-card${lifted ? " is-lifted" : ""}${task.image ? ` has-${task.image.kind}` : ""}`}
      draggable
      onDragStart={(event) => {
        event.dataTransfer.setData("text/plain", task.id);
        event.dataTransfer.effectAllowed = "move";
        onDragStart(task.id);
      }}
      onDragEnd={onDragEnd}
    >
      <div className="task-card-top">
        <PriorityBadge priority={task.priority} />
        <div className="task-menu" ref={menuRef}>
          <button type="button" className="more" aria-label={`${task.title} actions`} aria-expanded={menuOpen} onClick={() => setMenuOpen((open) => !open)}>
            . . .
          </button>
          {menuOpen ? (
            <ul className="task-menu-list" role="menu">
              {columns.map((column) => (
                <li key={column.id}>
                  <button
                    type="button"
                    role="menuitem"
                    onClick={() => {
                      onMove(task.id, column.id);
                      setMenuOpen(false);
                    }}
                  >
                    Move to {column.label}
                  </button>
                </li>
              ))}
            </ul>
          ) : null}
        </div>
      </div>
      <h3>{task.title}</h3>
      {task.description ? (
        <p className={task.descriptionColor === "primary" ? "is-primary" : undefined}>{task.description}</p>
      ) : null}
      {task.image?.kind === "single" ? (
        <img className="task-photo" src={task.image.src} width={task.image.width} height={task.image.height} alt="" />
      ) : null}
      {task.image?.kind === "pair" ? (
        <div className="task-photos">
          {task.image.sources.map((src) => (
            <img key={src} src={src} width={131} height={79} alt="" />
          ))}
        </div>
      ) : null}
      {task.image?.kind === "cover" ? (
        <div className="task-cover">
          <img src={task.image.src} width={task.image.width} height={task.image.height} alt="" style={{ left: task.image.offsetX }} />
        </div>
      ) : null}
      <footer className="task-foot">
        <AvatarGroup sources={task.avatars} size={24} overlap={5} />
        <div className="task-meta">
          <span>
            <Icon src="/assets/message.svg" width={16} height={16} alt="" />
            {task.comments} comments
          </span>
          <span>
            <FolderIcon />
            {task.files} files
          </span>
        </div>
      </footer>
      {lifted ? <img src="/assets/grab.svg" width={15.173} height={14.125} alt="" className="grab-cursor" /> : null}
    </article>
  );
}
