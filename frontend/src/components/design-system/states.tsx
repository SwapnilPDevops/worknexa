import type { ReactNode } from "react";
import { Button } from "./button.tsx";

export function EmptyState({
  title,
  description,
  action,
}: {
  title: string;
  description: string;
  action?: ReactNode;
}) {
  return (
    <div className="mx-auto flex max-w-md flex-col items-start rounded-xl border border-dashed border-line bg-surface px-6 py-10">
      <h1 className="text-xl font-semibold tracking-tight text-ink">{title}</h1>
      <p className="mt-2 text-sm text-muted">{description}</p>
      {action ? <div className="mt-5">{action}</div> : null}
    </div>
  );
}

export function LoadingState({ label = "Loading" }: { label?: string }) {
  return (
    <div className="space-y-3" role="status" aria-live="polite" aria-busy="true">
      <span className="sr-only">{label}</span>
      <div className="wx-skeleton h-8 w-40 rounded-md" />
      <div className="wx-skeleton h-24 rounded-xl" />
      <div className="wx-skeleton h-24 rounded-xl" />
    </div>
  );
}

export function ErrorState({
  title = "Something went wrong",
  description,
  onRetry,
}: {
  title?: string;
  description: string;
  onRetry?: () => void;
}) {
  return (
    <div role="alert" className="rounded-xl border border-danger/30 bg-danger-soft px-5 py-4">
      <h2 className="text-sm font-semibold text-danger">{title}</h2>
      <p className="mt-1 text-sm text-ink">{description}</p>
      {onRetry ? (
        <Button className="mt-3" variant="secondary" onClick={onRetry}>
          Try again
        </Button>
      ) : null}
    </div>
  );
}
