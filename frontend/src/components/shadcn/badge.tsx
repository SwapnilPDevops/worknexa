import { cva, type VariantProps } from "class-variance-authority";
import type { HTMLAttributes } from "react";
import { cn } from "../../utils/cn.ts";

const badgeVariants = cva(
  "inline-flex items-center rounded-[6px] border px-2 py-0.5 text-[11px] font-semibold transition-colors focus:outline-none focus:ring-1 focus:ring-[#111827]",
  {
    variants: {
      variant: {
        default:
          "border-[#111827]/15 bg-[#FED7AA] text-[#111827] shadow-xs",
        secondary:
          "border-[#111827]/10 bg-[#D7D3CB] text-[#111827]",
        destructive:
          "border-transparent bg-[#DC2626] text-white shadow-xs",
        outline:
          "border-[#D1CCC2] text-[#111827] bg-[#EBE8E2]",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  },
);

export interface BadgeProps
  extends HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {}

export function Badge({ className, variant, ...props }: BadgeProps) {
  return <div className={cn(badgeVariants({ variant }), className)} {...props} />;
}

export { badgeVariants };
