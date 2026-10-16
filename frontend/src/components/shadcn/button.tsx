import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import type { ButtonHTMLAttributes } from "react";
import { cn } from "../../utils/cn.ts";

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-[8px] text-xs font-semibold tracking-wide transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-[#111827] disabled:pointer-events-none disabled:opacity-50 select-none",
  {
    variants: {
      variant: {
        default:
          "bg-[#FED7AA] text-[#111827] border border-[#111827]/15 hover:bg-[#FFEDD5] hover:border-[#111827]/30 shadow-xs active:scale-[0.98]",
        destructive:
          "bg-[#DC2626] text-white hover:bg-[#B91C1C] shadow-xs active:scale-[0.98]",
        outline:
          "border border-[#D1CCC2] bg-[#EBE8E2] text-[#111827] hover:bg-[#DFDCD6] hover:border-[#111827]/25 active:scale-[0.98]",
        secondary:
          "bg-[#D7D3CB] text-[#111827] hover:bg-[#DFDCD6] active:scale-[0.98]",
        ghost:
          "text-[#4B5563] hover:bg-[#D7D3CB]/60 hover:text-[#111827]",
        link:
          "text-[#111827] underline-offset-4 hover:underline p-0 h-auto",
      },
      size: {
        default: "h-9 px-4 py-2",
        sm: "h-7 rounded-[6px] px-2.5 text-[11px]",
        lg: "h-10 rounded-[8px] px-6 text-sm",
        icon: "size-8 rounded-[8px]",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  },
);

export interface ButtonProps
  extends ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean;
}

export function Button({
  className,
  variant,
  size,
  asChild = false,
  type = "button",
  ...props
}: ButtonProps) {
  const Comp = asChild ? Slot : "button";
  return (
    <Comp
      className={cn(buttonVariants({ variant, size, className }))}
      type={asChild ? undefined : type}
      {...props}
    />
  );
}

export { buttonVariants };
