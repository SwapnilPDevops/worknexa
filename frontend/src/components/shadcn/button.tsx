import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import type { ButtonHTMLAttributes } from "react";
import { cn } from "../../utils/cn.ts";

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 rounded-md text-sm font-medium transition-colors disabled:pointer-events-none disabled:opacity-50",
  {
    variants: {
      variant: {
        default: "bg-shadcn-primary text-shadcn-primary-foreground hover:bg-shadcn-primary/90",
        outline: "border border-shadcn-border bg-shadcn-background text-shadcn-foreground hover:bg-shadcn-muted",
      },
      size: {
        default: "h-9 px-4",
        icon: "size-9",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  },
);

type ShadcnButtonProps = ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean;
  };

export function ShadcnButton({ className, variant, size, asChild = false, type = "button", ...props }: ShadcnButtonProps) {
  const Comp = asChild ? Slot : "button";
  return <Comp className={cn(buttonVariants({ variant, size }), className)} type={asChild ? undefined : type} {...props} />;
}
