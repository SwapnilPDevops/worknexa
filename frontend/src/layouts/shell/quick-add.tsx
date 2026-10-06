import { useQueryClient } from "@tanstack/react-query";
import { format } from "date-fns";
import { useEffect, useRef, useState } from "react";
import { z } from "zod";
import { useAuth } from "../../auth/auth-context.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Dialog } from "../../components/design-system/dialog.tsx";
import { Field, Input, Textarea } from "../../components/design-system/field.tsx";
import { allowedQuickActions } from "../../features/my-day/quick-actions.ts";
import { useZodForm } from "../../hooks/use-zod-form.ts";
import { ApiError, sendJson } from "../../services/api-client.ts";
import { useShell } from "./shell-context.tsx";

const schema = z.object({
  title: z.string().trim().min(1, "Add a title.").max(300, "Keep the title under 300 characters."),
  notes: z.string().max(4000, "Keep the notes under 4000 characters."),
});

export function QuickAdd() {
  const { quickAddOpen, quickAddKind, setQuickAddOpen } = useShell();
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const actions = allowedQuickActions(user?.permissions);
  const [kind, setKind] = useState(quickAddKind);
  const [error, setError] = useState<string | null>(null);
  const openedAs = useRef(quickAddKind);
  if (quickAddOpen && openedAs.current !== quickAddKind) {
    openedAs.current = quickAddKind;
    if (kind !== quickAddKind) {
      setKind(quickAddKind);
    }
  }
  const form = useZodForm(schema, { defaultValues: { title: "", notes: "" } });
  const { reset } = form;
  const selected = actions.find((action) => action.kind === kind) ?? actions[0];

  useEffect(() => {
    if (!quickAddOpen) {
      return;
    }
    const allowed = allowedQuickActions(user?.permissions);
    setKind(allowed.some((action) => action.kind === quickAddKind) ? quickAddKind : allowed[0]?.kind ?? "task");
    setError(null);
    reset();
  }, [quickAddKind, quickAddOpen, reset, user?.permissions]);

  return (
    <Dialog
      open={quickAddOpen}
      title={selected?.label ?? "Quick add"}
      description="This is saved to your day."
      onClose={() => setQuickAddOpen(false)}
    >
      <form
        className="space-y-4"
        noValidate
        onSubmit={form.handleSubmit(async (values) => {
          setError(null);
          try {
            await sendJson("/api/dashboard/quick-capture", "POST", {
              kind: selected?.kind ?? kind,
              title: values.title,
              notes: values.notes.trim() ? values.notes.trim() : null,
              date: format(new Date(), "yyyy-MM-dd"),
              offsetMinutes: -new Date().getTimezoneOffset(),
            });
            await queryClient.invalidateQueries({ queryKey: ["my-day"] });
            setQuickAddOpen(false);
          } catch (caught) {
            setError(caught instanceof ApiError ? caught.message : "That could not be saved.");
          }
        })}
      >
        <div className="flex flex-wrap gap-1.5" role="tablist" aria-label="What to add">
          {actions.map((action) => {
            const active = action.kind === selected?.kind;
            return (
              <button
                key={action.kind}
                type="button"
                role="tab"
                aria-selected={active}
                className={active ? "rounded-md bg-accent-soft px-2.5 py-1 text-xs font-medium text-accent" : "rounded-md px-2.5 py-1 text-xs text-muted hover:bg-surface-muted"}
                onClick={() => setKind(action.kind)}
              >
                {action.label.replace(/^Add /, "").replace(/^Assign /, "")}
              </button>
            );
          })}
        </div>
        <Field label="Title" error={form.formState.errors.title?.message}>
          {({ id, describedBy }) => (
            <Input id={id} data-autofocus aria-describedby={describedBy} placeholder="What needs a place?" {...form.register("title")} />
          )}
        </Field>
        <Field label="Notes" hint="Optional context." error={form.formState.errors.notes?.message}>
          {({ id, describedBy }) => <Textarea id={id} aria-describedby={describedBy} {...form.register("notes")} />}
        </Field>
        {error ? (
          <p role="alert" className="rounded-lg bg-danger-soft px-3 py-2 text-sm text-danger">
            {error}
          </p>
        ) : null}
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setQuickAddOpen(false)}>
            Cancel
          </Button>
          <Button type="submit" disabled={form.formState.isSubmitting || !selected}>
            {form.formState.isSubmitting ? "Saving" : selected?.label ?? "Save"}
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
