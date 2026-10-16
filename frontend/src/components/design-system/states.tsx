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
    <div className="mx-auto flex max-w-md flex-col items-center text-center rounded-[8px] border border-dashed border-[#D1CCC2] bg-[#DFDCD6]/80 px-6 py-12">
      <h1 className="text-base font-mono font-semibold tracking-tight text-[#111827]">{title}</h1>
      <p className="mt-2 text-sm text-[#4B5563] leading-relaxed max-w-sm font-serif">{description}</p>
      {action ? <div className="mt-6">{action}</div> : null}
    </div>
  );
}

export function LoadingState({ label = "Loading" }: { label?: string }) {
  return (
    <div className="space-y-4" role="status" aria-live="polite" aria-busy="true">
      <span className="sr-only">{label}</span>
      <div className="wx-skeleton h-8 w-44 rounded-[8px]" />
      <div className="wx-skeleton h-28 rounded-[8px] border border-[#D1CCC2]" />
      <div className="wx-skeleton h-28 rounded-[8px] border border-[#D1CCC2]" />
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
    <div role="alert" className="rounded-[8px] border border-rose-200 bg-rose-50/90 px-5 py-4">
      <h2 className="text-sm font-semibold text-rose-800 font-mono">{title}</h2>
      <p className="mt-1 text-sm text-rose-700 font-serif">{description}</p>
      {onRetry ? (
        <Button className="mt-3.5" variant="secondary" size="sm" onClick={onRetry}>
          Try again
        </Button>
      ) : null}
    </div>
  );
}
