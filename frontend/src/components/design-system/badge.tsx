import type { HTMLAttributes } from "react";
import { cn } from "../../utils/cn.ts";

const tones = {
  neutral: "bg-surface-muted text-muted",
  accent: "bg-accent-soft text-accent",
  signal: "bg-signal-soft text-signal",
  warning: "bg-warning-soft text-warning",
  danger: "bg-danger-soft text-danger",
};

type BadgeProps = HTMLAttributes<HTMLSpanElement> & {
  tone?: keyof typeof tones;
};

export function Badge({ tone = "neutral", className, ...props }: BadgeProps) {
  return (
    <span
      className={cn("inline-flex items-center rounded-md px-1.5 py-0.5 text-[11px] font-medium", tones[tone], className)}
      {...props}
    />
  );
}
