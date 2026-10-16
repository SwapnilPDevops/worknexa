import type { HTMLAttributes } from "react";
import { cn } from "../../utils/cn.ts";

const tones = {
  neutral: "bg-[#D7D3CB] text-[#111827] border border-[#111827]/10 font-mono",
  accent: "bg-[#FED7AA] text-[#111827] border border-[#111827]/20 font-mono font-semibold",
  signal: "bg-emerald-100 text-emerald-800 border border-emerald-200 font-mono",
  warning: "bg-amber-100 text-amber-800 border border-amber-200 font-mono",
  danger: "bg-rose-100 text-rose-800 border border-rose-200 font-mono",
};

type BadgeProps = HTMLAttributes<HTMLSpanElement> & {
  tone?: keyof typeof tones;
};

export function Badge({ tone = "neutral", className, ...props }: BadgeProps) {
  return (
    <span
      className={cn("inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium leading-4 tracking-tight", tones[tone], className)}
      {...props}
    />
  );
}
