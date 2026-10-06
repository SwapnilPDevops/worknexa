import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { format } from "date-fns";
import { useEffect, useState, type FormEvent } from "react";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Badge } from "../../components/design-system/badge.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Field, Input, Select } from "../../components/design-system/field.tsx";
import { ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { ApiError } from "../../services/api-client.ts";
import { completeTodo, deleteTodo, fetchTodos, fromDateTimeInput, localOffsetMinutes, reopenTodo, saveTodo, toDateTimeInput, type TodoItem, type TodoWrite } from "../work/api.ts";
import { labelFor, priorities, priorityTone, recurrences, todoBuckets } from "../work/catalog.ts";

const addTargets = ["Today", "Tomorrow", "ThisWeek", "Upcoming"] as const;

export function TodosView() {
  const { user } = useAuth();
  const manageable = can(user?.permissions, permissions.tasksManage);
  const now = new Date();
  const date = format(now, "yyyy-MM-dd");
  const offsetMinutes = localOffsetMinutes(now);
  const queryClient = useQueryClient();
  const todos = useQuery({ queryKey: ["todos", date, offsetMinutes], queryFn: () => fetchTodos(date, offsetMinutes) });
  const [title, setTitle] = useState("");
  const [target, setTarget] = useState<(typeof addTargets)[number]>("Today");
  const [priority, setPriority] = useState("Medium");
  const [recurrence, setRecurrence] = useState("None");
  const [tags, setTags] = useState("");
  const [remindAt, setRemindAt] = useState("");
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    document.title = "Todo · Worknexa";
  }, []);

  function refresh() {
    return Promise.all([
      queryClient.invalidateQueries({ queryKey: ["todos"] }),
      queryClient.invalidateQueries({ queryKey: ["my-day"] }),
    ]);
  }

  const create = useMutation({
    mutationFn: (todo: TodoWrite) => saveTodo(todo),
    onSuccess: async () => {
      setTitle("");
      setTags("");
      setRemindAt("");
      setMessage(null);
      await refresh();
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The todo could not be saved."),
  });

  if (!can(user?.permissions, permissions.tasksRead)) {
    return <ErrorState title="Access denied" description="Your account does not include personal todos." />;
  }

  if (todos.isPending) {
    return <LoadingState label="Loading todos" />;
  }

  if (todos.isError || !todos.data) {
    const description = todos.error instanceof ApiError ? todos.error.message : "Todos could not be loaded.";
    return <ErrorState title="Todo is unavailable" description={description} onRetry={() => void todos.refetch()} />;
  }

  function onAdd(event: FormEvent) {
    event.preventDefault();
    create.mutate({
      title: title.trim(),
      priority,
      dueAt: dueFor(target, new Date()),
      remindAt: fromDateTimeInput(remindAt),
      recurrence,
      tags: tags.split(",").map((tag) => tag.trim()).filter(Boolean),
    });
  }

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Todo</h1>
        <p className="mt-1 text-sm text-muted">Personal items. They stay off the task board.</p>
      </header>

      {manageable ? (
        <form className="space-y-3 rounded-xl border border-line bg-surface p-4" onSubmit={onAdd}>
          {message ? <p className="text-sm text-danger">{message}</p> : null}
          <div className="flex flex-col gap-2 sm:flex-row">
            <Input aria-label="Todo title" value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Add a personal item" required maxLength={300} />
            <Button type="submit" disabled={create.isPending}>Add</Button>
          </div>
          <div className="grid gap-2 sm:grid-cols-2">
            <Field label="When">{({ id }) => <Select id={id} value={target} onChange={(event) => setTarget(event.target.value as (typeof addTargets)[number])}>{addTargets.map((item) => <option key={item} value={item}>{labelFor(item)}</option>)}</Select>}</Field>
            <Field label="Priority">{({ id }) => <Select id={id} value={priority} onChange={(event) => setPriority(event.target.value)}>{priorities.map((item) => <option key={item} value={item}>{item}</option>)}</Select>}</Field>
            <Field label="Reminder">{({ id }) => <Input id={id} type="datetime-local" value={remindAt} onChange={(event) => setRemindAt(event.target.value)} />}</Field>
            <Field label="Repeats">{({ id }) => <Select id={id} value={recurrence} onChange={(event) => setRecurrence(event.target.value)}>{recurrences.map((item) => <option key={item} value={item}>{item}</option>)}</Select>}</Field>
          </div>
          <Field label="Tags" hint="Separate tags with commas.">{({ id }) => <Input id={id} value={tags} onChange={(event) => setTags(event.target.value)} />}</Field>
        </form>
      ) : null}

      {todoBuckets.map((bucket) => {
        const items = todos.data.filter((item) => item.bucket === bucket);
        if (items.length === 0 && bucket === "Overdue") {
          return null;
        }

        return (
          <section key={bucket} className="space-y-2">
            <h2 className="text-sm font-semibold text-ink">{labelFor(bucket)}</h2>
            {items.length === 0 ? <p className="text-sm text-muted">Nothing here yet.</p> : (
              <ul className="space-y-2">
                {items.map((item) => <TodoRow key={item.id} item={item} manageable={manageable} onChanged={refresh} />)}
              </ul>
            )}
          </section>
        );
      })}
    </div>
  );
}

function TodoRow({ item, manageable, onChanged }: { item: TodoItem; manageable: boolean; onChanged: () => Promise<unknown> }) {
  const [open, setOpen] = useState(false);
  const [priority, setPriority] = useState(item.priority);
  const [dueAt, setDueAt] = useState(toDateTimeInput(item.dueAt));
  const [remindAt, setRemindAt] = useState(toDateTimeInput(item.remindAt));
  const [recurrence, setRecurrence] = useState(item.recurrence);
  const [tags, setTags] = useState(item.tags.join(", "));
  const [error, setError] = useState<string | null>(null);

  async function run(action: () => Promise<unknown>) {
    setError(null);
    try {
      await action();
      await onChanged();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : "That change could not be saved.");
    }
  }

  return (
    <li className="rounded-xl border border-line bg-surface px-3 py-3">
      <div className="flex items-start gap-3">
        <input
          type="checkbox"
          className="mt-1"
          checked={item.isCompleted}
          aria-label={item.isCompleted ? `Reopen ${item.title}` : `Complete ${item.title}`}
          disabled={!manageable}
          onChange={() => void run(() => item.isCompleted ? reopenTodo(item.id) : completeTodo(item.id))}
        />
        <div className="min-w-0 flex-1">
          <p className="text-sm font-medium text-ink">{item.title}</p>
          <div className="mt-1 flex flex-wrap gap-1">
            <Badge tone={priorityTone(item.priority)}>{item.priority}</Badge>
            {item.dueAt ? <Badge>{format(new Date(item.dueAt), "d MMM, HH:mm")}</Badge> : null}
            {item.recurrence !== "None" ? <Badge tone="accent">{item.recurrence}</Badge> : null}
            {item.tags.map((tag) => <Badge key={tag}>{tag}</Badge>)}
          </div>
        </div>
        <Button variant="ghost" size="sm" onClick={() => setOpen((value) => !value)}>{open ? "Close" : "Edit"}</Button>
      </div>
      {open ? (
        <form
          className="mt-3 grid gap-2 sm:grid-cols-2"
          onSubmit={(event) => {
            event.preventDefault();
            void run(() => saveTodo({
              title: item.title,
              priority,
              dueAt: fromDateTimeInput(dueAt),
              remindAt: fromDateTimeInput(remindAt),
              recurrence,
              tags: tags.split(",").map((tag) => tag.trim()).filter(Boolean),
            }, item.id));
          }}
        >
          {error ? <p className="text-sm text-danger sm:col-span-2">{error}</p> : null}
          <Field label="Priority">{({ id }) => <Select id={id} value={priority} onChange={(event) => setPriority(event.target.value)}>{priorities.map((value) => <option key={value} value={value}>{value}</option>)}</Select>}</Field>
          <Field label="Repeats">{({ id }) => <Select id={id} value={recurrence} onChange={(event) => setRecurrence(event.target.value)}>{recurrences.map((value) => <option key={value} value={value}>{value}</option>)}</Select>}</Field>
          <Field label="Due">{({ id }) => <Input id={id} type="datetime-local" value={dueAt} onChange={(event) => setDueAt(event.target.value)} />}</Field>
          <Field label="Reminder">{({ id }) => <Input id={id} type="datetime-local" value={remindAt} onChange={(event) => setRemindAt(event.target.value)} />}</Field>
          <Field label="Tags">{({ id }) => <Input id={id} value={tags} onChange={(event) => setTags(event.target.value)} />}</Field>
          <div className="flex items-end gap-2">
            {manageable ? <Button type="submit" size="sm">Save</Button> : null}
            {manageable ? <Button type="button" variant="danger" size="sm" onClick={() => void run(() => deleteTodo(item.id))}>Delete</Button> : null}
          </div>
        </form>
      ) : null}
    </li>
  );
}

function dueFor(target: (typeof addTargets)[number], now: Date) {
  if (target === "Today") {
    return null;
  }

  const due = new Date(now);
  due.setHours(17, 0, 0, 0);
  if (target === "Tomorrow") {
    due.setDate(due.getDate() + 1);
    return due.toISOString();
  }

  if (target === "ThisWeek") {
    const untilSunday = (7 - due.getDay()) % 7;
    due.setDate(due.getDate() + (untilSunday > 1 ? untilSunday : 2));
    return due.toISOString();
  }

  due.setDate(due.getDate() + 8);
  return due.toISOString();
}
