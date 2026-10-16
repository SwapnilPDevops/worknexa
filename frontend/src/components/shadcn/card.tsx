import type { HTMLAttributes } from "react";
import { cn } from "../../utils/cn.ts";

export function Card({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "rounded-[8px] border border-[#D1CCC2] bg-[#DFDCD6] text-[#111827] shadow-[0_1px_3px_0_rgba(17,24,39,0.05)]",
        className,
      )}
      {...props}
    />
  );
}

export function CardHeader({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("flex flex-col space-y-1.5 p-5 border-b border-[#D1CCC2]/60", className)}
      {...props}
    />
  );
}

export function CardTitle({ className, ...props }: HTMLAttributes<HTMLHeadingElement>) {
  return (
    <h3
      className={cn("text-base font-semibold leading-none tracking-tight text-[#111827]", className)}
      {...props}
    />
  );
}

export function CardDescription({ className, ...props }: HTMLAttributes<HTMLParagraphElement>) {
  return (
    <p
      className={cn("text-xs text-[#4B5563] leading-relaxed", className)}
      {...props}
    />
  );
}

export function CardContent({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("p-5 pt-4", className)} {...props} />;
}

export function CardFooter({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("flex items-center p-5 pt-0 border-t border-[#D1CCC2]/40", className)}
      {...props}
    />
  );
}
