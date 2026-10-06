import type { ReactNode } from "react";

type OutlineButtonProps = {
  children: ReactNode;
  onClick?: () => void;
  width?: number;
  className?: string;
  pressed?: boolean;
  label: string;
};

export function OutlineButton({ children, onClick, width, className, pressed, label }: OutlineButtonProps) {
  return (
    <button
      type="button"
      className={`outline-button${className ? ` ${className}` : ""}`}
      style={width ? { width } : undefined}
      onClick={onClick}
      aria-pressed={pressed}
      aria-label={label}
    >
      {children}
    </button>
  );
}

export function IconButton({
  children,
  label,
  onClick,
  pressed,
  className,
}: {
  children: ReactNode;
  label: string;
  onClick?: () => void;
  pressed?: boolean;
  className?: string;
}) {
  return (
    <button type="button" className={className ?? "icon-hit"} aria-label={label} aria-pressed={pressed} onClick={onClick}>
      {children}
    </button>
  );
}
