import type { CSSProperties, ReactNode } from "react";
import { cn } from "../../utils/cn.ts";

interface ShinyTextProps {
  children: ReactNode;
  disabled?: boolean;
  speed?: number;
  className?: string;
}

export function ShinyText({
  children,
  disabled = false,
  speed = 5,
  className,
}: ShinyTextProps) {
  const animationDuration = `${speed}s`;

  return (
    <span
      className={cn(
        "inline-block bg-clip-text font-medium text-transparent",
        !disabled && "animate-shiny-text",
        className,
      )}
      style={
        {
          backgroundImage:
            "linear-gradient(120deg, rgba(17, 24, 39, 0.9) 0%, rgba(17, 24, 39, 0.9) 35%, rgba(254, 215, 170, 0.95) 50%, rgba(17, 24, 39, 0.9) 65%, rgba(17, 24, 39, 0.9) 100%)",
          backgroundSize: "200% 100%",
          WebkitBackgroundClip: "text",
          animationDuration,
        } as CSSProperties
      }
    >
      {children}
    </span>
  );
}
