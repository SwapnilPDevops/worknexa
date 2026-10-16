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
      <div className={cn("w-[min(32rem,calc(100vw-2rem))] rounded-[8px] border border-[#D1CCC2] bg-[#EBE8E2] p-5 text-[#111827] shadow-[var(--wx-shadow-panel)]", className)}>
        <div className="mb-4 flex items-start justify-between gap-4">
          <div>
            <h2 id={titleId} className="text-sm font-mono font-semibold text-[#111827]">
              {title}
            </h2>
            {description ? <p className="mt-1 text-xs text-[#4B5563] font-serif">{description}</p> : null}
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
