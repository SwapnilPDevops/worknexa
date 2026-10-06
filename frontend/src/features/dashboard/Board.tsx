import { useState } from "react";
import type { ColumnId, Task } from "../../data/board";
import { columns } from "../../data/board";
import { CountBadge } from "../../components/ui/Badge";
import { Icon } from "../../components/ui/Icon";
import { TaskCard } from "./TaskCard";

type BoardProps = {
  tasks: Task[];
  pristine: boolean;
  onMove: (id: string, column: ColumnId) => void;
  onAdd: (title: string) => void;
  liftedId: string | null;
  onLiftClear: () => void;
};

const designCounts: Record<ColumnId, number> = {
  todo: 4,
  progress: 3,
  done: 2,
};

export function Board({ tasks, pristine, onMove, onAdd, liftedId, onLiftClear }: BoardProps) {
  const [adding, setAdding] = useState(false);
  const [draft, setDraft] = useState("");
  const [over, setOver] = useState<ColumnId | null>(null);

  return (
    <div className="board">
      {columns.map((column) => {
        const cards = tasks.filter((task) => task.column === column.id);
        const count = pristine ? designCounts[column.id] : cards.length;
        return (
          <section
            key={column.id}
            className={`column${over === column.id ? " is-over" : ""}`}
            aria-label={column.label}
            onDragOver={(event) => {
              event.preventDefault();
              setOver(column.id);
            }}
            onDragLeave={() => setOver((current) => (current === column.id ? null : current))}
            onDrop={(event) => {
              event.preventDefault();
              const id = event.dataTransfer.getData("text/plain");
              if (id) onMove(id, column.id);
              setOver(null);
              onLiftClear();
            }}
          >
            <header className="column-head">
              <div className="column-title">
                <Icon src={`/assets/${column.dot}.svg`} width={8} height={8} alt="" />
                <h2>{column.label}</h2>
                <CountBadge value={count} />
              </div>
              {column.id === "todo" ? (
                <button type="button" className="icon-hit" aria-label="Add task" onClick={() => setAdding(true)}>
                  <Icon src="/assets/add-square-bulk.svg" width={24} height={24} alt="" />
                </button>
              ) : (
                <span className="column-add-spacer" />
              )}
              <img src={`/assets/${column.line}.svg`} width={314} height={3} alt="" className="column-line" />
            </header>
            <div className="column-body">
              {column.id === "todo" && adding ? (
                <form
                  className="new-task"
                  onSubmit={(event) => {
                    event.preventDefault();
                    if (draft.trim()) onAdd(draft.trim());
                    setDraft("");
                    setAdding(false);
                  }}
                >
                  <input
                    value={draft}
                    onChange={(event) => setDraft(event.target.value)}
                    placeholder="Task title"
                    aria-label="Task title"
                    autoFocus
                  />
                  <div className="new-task-actions">
                    <button type="submit">Add</button>
                    <button type="button" onClick={() => setAdding(false)}>
                      Cancel
                    </button>
                  </div>
                </form>
              ) : null}
              {cards.map((task) => {
                const lifted = task.id === liftedId;
                if (lifted) {
                  return (
                    <div className="lift-slot" key={task.id}>
                      <div className="drop-placeholder" />
                      <TaskCard task={task} lifted onMove={onMove} onDragStart={onLiftClear} onDragEnd={onLiftClear} />
                    </div>
                  );
                }
                return <TaskCard key={task.id} task={task} onMove={onMove} onDragStart={onLiftClear} onDragEnd={() => undefined} />;
              })}
              {column.id === "progress" && pristine ? <div className="column-peek" aria-hidden="true" /> : null}
              {cards.length === 0 ? <p className="column-empty">No tasks</p> : null}
            </div>
          </section>
        );
      })}
    </div>
  );
}
