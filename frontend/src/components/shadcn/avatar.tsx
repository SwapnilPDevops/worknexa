import type { HTMLAttributes, ImgHTMLAttributes } from "react";
import { cn } from "../../utils/cn.ts";

export function Avatar({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "relative flex size-9 shrink-0 overflow-hidden rounded-full border border-[#D1CCC2] bg-[#EBE8E2]",
        className,
      )}
      {...props}
    />
  );
}

export function AvatarImage({ className, ...props }: ImgHTMLAttributes<HTMLImageElement>) {
  return <img className={cn("aspect-square size-full object-cover", className)} alt="" {...props} />;
}

export function AvatarFallback({ className, children, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "flex size-full items-center justify-center rounded-full bg-[#D7D3CB] text-xs font-semibold text-[#111827]",
        className,
      )}
      {...props}
    >
      {children}
    </div>
  );
}
