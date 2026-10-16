import type { InputHTMLAttributes } from "react";
import { cn } from "../../utils/cn.ts";

export type InputProps = InputHTMLAttributes<HTMLInputElement>;

export function Input({ className, type = "text", ...props }: InputProps) {
  return (
    <input
      type={type}
      className={cn(
        "flex h-9 w-full rounded-[8px] border border-[#D1CCC2] bg-[#EBE8E2] px-3 py-1 text-sm text-[#111827] shadow-xs transition-colors file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-[#4B5563]/60 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-[#111827] focus-visible:border-[#111827] disabled:cursor-not-allowed disabled:opacity-50",
        className,
      )}
      {...props}
    />
  );
}
