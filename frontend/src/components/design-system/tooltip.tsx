import type { ReactNode } from "react";
import { cn } from "../../utils/cn.ts";

type TooltipProps = {
  label: string;
  children: ReactNode;
  side?: "top" | "right";
  className?: string;
};

export function Tooltip({ label, children, side = "top", className }: TooltipProps) {
  return (
    <span className={cn("group/tip relative inline-flex", className)}>
      {children}
      <span
        role="tooltip"
        className={cn(
          "pointer-events-none absolute z-50 whitespace-nowrap rounded-md bg-ink px-2 py-1 text-xs text-canvas opacity-0 transition-opacity duration-150 group-hover/tip:opacity-100 group-focus-within/tip:opacity-100",
          side === "right" ? "left-full top-1/2 ml-2 -translate-y-1/2" : "bottom-full left-1/2 mb-2 -translate-x-1/2",
        )}
      >
        {label}
      </span>
    </span>
  );
}
