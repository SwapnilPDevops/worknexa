import { X } from "lucide-react";
import { useEffect, useId, type ReactNode } from "react";
import { Button } from "./button.tsx";
import { cn } from "../../utils/cn.ts";

type DrawerProps = {
  open: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
  side?: "left" | "right";
  className?: string;
};

export function Drawer({ open, title, onClose, children, side = "right", className }: DrawerProps) {
  const titleId = useId();

  useEffect(() => {
    if (!open) {
      return;
    }
    const previous = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    function onKey(event: KeyboardEvent) {
      if (event.key === "Escape") {
        onClose();
      }
    }
    window.addEventListener("keydown", onKey);
    return () => {
      document.body.style.overflow = previous;
      window.removeEventListener("keydown", onKey);
    };
  }, [open, onClose]);

  if (!open) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-40">
      <button className="absolute inset-0 bg-overlay" aria-label="Close panel" onClick={onClose} />
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        data-side={side}
        className={cn(
          "wx-drawer absolute inset-y-0 flex w-full max-w-sm flex-col border-line bg-surface shadow-[var(--wx-shadow-panel)]",
          side === "left" ? "left-0 border-r" : "right-0 border-l",
          className,
        )}
      >
        <header className="flex h-14 items-center justify-between border-b border-line px-4">
          <h2 id={titleId} className="text-sm font-semibold">
            {title}
          </h2>
          <Button variant="ghost" size="icon" aria-label="Close panel" onClick={onClose}>
            <X size={16} aria-hidden="true" />
          </Button>
        </header>
        <div className="min-h-0 flex-1 overflow-y-auto p-4">{children}</div>
      </div>
    </div>
  );
}
