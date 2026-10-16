import { useId, type ComponentProps, type ReactNode } from "react";
import { cn } from "../../utils/cn.ts";

const controlClass =
  "h-9 w-full rounded-[8px] border border-[#D1CCC2] bg-[#EBE8E2] px-3 text-sm text-[#111827] outline-none transition-colors placeholder:text-[#4B5563] focus:border-[#111827] focus:ring-1 focus:ring-[#111827]/25";

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
      <label className="mb-1.5 block text-[13px] font-medium text-[#111827]" htmlFor={id}>{label}</label>
      {children({ id, describedBy })}
      {hint ? (
        <span id={hintId} className="mt-1.5 block text-xs text-[#4B5563] font-serif">
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
  return <textarea className={cn(controlClass, "min-h-24 py-2 leading-relaxed", className)} {...props} />;
}

export function Select({ className, ...props }: ComponentProps<"select">) {
  return <select className={cn(controlClass, className)} {...props} />;
}
