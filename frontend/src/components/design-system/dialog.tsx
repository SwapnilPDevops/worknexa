import { X } from "lucide-react";
import { useEffect, useId, useRef, type ReactNode } from "react";
import { cn } from "../../utils/cn.ts";
import { Button } from "./button.tsx";

type DialogProps = {
  open: boolean;
  title: string;
  description?: string;
  onClose: () => void;
  children: ReactNode;
  className?: string;
};

export function Dialog({ open, title, description, onClose, children, className }: DialogProps) {
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  useEffect(() => {
    const node = ref.current;
    if (!node) {
      return;
    }
    if (open && !node.open) {
      node.showModal();
      node.querySelector<HTMLElement>("[data-autofocus]")?.focus();
    }
    if (!open && node.open) {
      node.close();
    }
  }, [open]);

  return (
    <dialog
      ref={ref}
      className="wx-dialog"
      aria-labelledby={titleId}
      onClose={onClose}
      onCancel={(event) => {
        event.preventDefault();
        onClose();
      }}
    >
      <div className={cn("w-[min(32rem,calc(100vw-2rem))] rounded-xl border border-line bg-surface p-5 text-ink shadow-[var(--wx-shadow-panel)]", className)}>
        <div className="mb-4 flex items-start justify-between gap-4">
          <div>
            <h2 id={titleId} className="text-base font-semibold">
              {title}
            </h2>
            {description ? <p className="mt-1 text-sm text-muted">{description}</p> : null}
          </div>
          <Button variant="ghost" size="icon" aria-label="Close dialog" onClick={onClose}>
            <X size={16} aria-hidden="true" />
          </Button>
        </div>
        {children}
      </div>
    </dialog>
  );
}
