import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import type { ButtonHTMLAttributes } from "react";
import { cn } from "../../utils/cn.ts";

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 rounded-[8px] text-xs font-semibold tracking-wide transition-[background-color,color,border-color,transform,box-shadow] duration-150 disabled:pointer-events-none disabled:opacity-50 select-none",
  {
    variants: {
      variant: {
        primary: "bg-[#FED7AA] text-[#111827] border border-[#111827]/15 hover:bg-[#FFEDD5] hover:border-[#111827]/30 shadow-xs active:scale-[0.98]",
        secondary: "border border-[#D1CCC2] bg-[#EBE8E2] text-[#111827] hover:bg-[#DFDCD6] hover:border-[#111827]/25 active:scale-[0.98]",
        ghost: "text-[#4B5563] hover:bg-[#D7D3CB]/60 hover:text-[#111827]",
        danger: "bg-[#DC2626] text-white hover:bg-[#B91C1C]",
        link: "text-[#4B5563] hover:text-[#111827] underline-offset-4 hover:underline p-0 h-auto",
      },
      size: {
        sm: "h-7 px-2.5 text-[11px] rounded-[6px]",
        md: "h-9 px-3.5 text-xs rounded-[8px]",
        icon: "size-8 rounded-[8px]",
      },
    },
    defaultVariants: {
      variant: "primary",
      size: "md",
    },
  },
);

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean;
  };

export function Button({ className, variant, size, asChild = false, type = "button", ...props }: ButtonProps) {
  const Comp = asChild ? Slot : "button";
  return <Comp className={cn(buttonVariants({ variant, size }), className)} type={asChild ? undefined : type} {...props} />;
}
