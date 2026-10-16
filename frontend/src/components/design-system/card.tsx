import type { HTMLAttributes, ReactNode } from "react";
import { cn } from "../../utils/cn.ts";

export type CardVariant = "default" | "elevated" | "compact";

export function Card({
  className,
  variant = "default",
  ...props
}: HTMLAttributes<HTMLElement> & { variant?: CardVariant }) {
  const variantStyles = {
    default: "bg-[#DFDCD6] border border-[#D1CCC2] rounded-[8px] shadow-[0_1px_3px_0_rgba(17,24,39,0.05)]",
    elevated: "bg-[#EBE8E2] border border-[#D1CCC2] rounded-[8px] shadow-[0_4px_14px_-2px_rgba(17,24,39,0.08),0_2px_4px_-2px_rgba(17,24,39,0.04)]",
    compact: "bg-[#D7D3CB] border border-[#111827]/10 rounded-[8px]",
  };

  return (
    <section
      className={cn(variantStyles[variant], className)}
      {...props}
    />
  );
}

export function CardHeader({
  title,
  description,
  action,
  className,
}: {
  title: string;
  description?: string;
  action?: ReactNode;
  className?: string;
}) {
  return (
    <header className={cn("flex items-start justify-between gap-4 border-b border-[#D1CCC2] px-5 py-3.5", className)}>
      <div className="min-w-0">
        <h2 className="text-sm font-semibold tracking-tight text-[#111827]">{title}</h2>
        {description ? <p className="mt-0.5 text-xs text-[#4B5563] leading-relaxed font-serif">{description}</p> : null}
      </div>
      {action ? <div className="shrink-0">{action}</div> : null}
    </header>
  );
}

export function CardBody({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("p-5 sm:p-6", className)} {...props} />;
}
