import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import type { ButtonHTMLAttributes } from "react";
import { cn } from "../../utils/cn.ts";

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 rounded-lg text-sm font-medium transition-[background-color,color,border-color,transform] duration-150 disabled:pointer-events-none disabled:opacity-50",
  {
    variants: {
      variant: {
        primary: "bg-accent text-accent-foreground hover:brightness-110",
        secondary: "border border-line bg-surface text-ink hover:bg-surface-muted",
        ghost: "text-ink hover:bg-surface-muted",
        danger: "bg-danger text-white hover:brightness-110",
      },
      size: {
        sm: "h-8 px-2.5 text-[13px]",
        md: "h-9 px-3",
        icon: "size-9",
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
