import { useId, type ComponentProps, type ReactNode } from "react";
import { cn } from "../../utils/cn.ts";

const controlClass =
  "h-9 w-full rounded-lg border border-line bg-surface px-3 text-sm text-ink outline-none transition-colors placeholder:text-muted focus:border-accent";

type FieldProps = {
  label: string;
  hint?: string;
  error?: string;
  children: (props: { id: string; describedBy?: string }) => ReactNode;
};

export function Field({ label, hint, error, children }: FieldProps) {
  const id = useId();
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [hintId, errorId].filter(Boolean).join(" ") || undefined;

  return (
    <div className="block">
      <label className="mb-1.5 block text-[13px] font-medium text-ink" htmlFor={id}>{label}</label>
      {children({ id, describedBy })}
      {hint ? (
        <span id={hintId} className="mt-1.5 block text-xs text-muted">
          {hint}
        </span>
      ) : null}
      {error ? (
        <span id={errorId} className="mt-1.5 block text-xs text-danger">
          {error}
        </span>
      ) : null}
    </div>
  );
}

export function Input({ className, ...props }: ComponentProps<"input">) {
  return <input className={cn(controlClass, className)} {...props} />;
}

export function Textarea({ className, ...props }: ComponentProps<"textarea">) {
  return <textarea className={cn(controlClass, "min-h-24 py-2", className)} {...props} />;
}

export function Select({ className, ...props }: ComponentProps<"select">) {
  return <select className={cn(controlClass, className)} {...props} />;
}
