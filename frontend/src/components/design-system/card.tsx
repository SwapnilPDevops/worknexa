import type { HTMLAttributes, ReactNode } from "react";
import { cn } from "../../utils/cn.ts";

export function Card({ className, ...props }: HTMLAttributes<HTMLElement>) {
  return (
    <section
      className={cn("rounded-xl border border-line bg-surface shadow-[var(--wx-shadow-rest)]", className)}
      {...props}
    />
  );
}

export function CardHeader({ title, description, action }: { title: string; description?: string; action?: ReactNode }) {
  return (
    <header className="flex items-start justify-between gap-4 border-b border-line px-4 py-3">
      <div>
        <h2 className="text-sm font-semibold text-ink">{title}</h2>
        {description ? <p className="mt-1 text-[13px] text-muted">{description}</p> : null}
      </div>
      {action}
    </header>
  );
}

export function CardBody({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("px-4 py-4", className)} {...props} />;
}
