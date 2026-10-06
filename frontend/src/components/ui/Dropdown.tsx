import { useEffect, useId, useRef, useState, type ReactNode } from "react";

type DropdownProps = {
  label: string;
  value: string;
  display?: string;
  options: string[];
  onChange: (value: string) => void;
  icon: ReactNode;
  width: number;
};

export function Dropdown({ label, value, display, options, onChange, icon, width }: DropdownProps) {
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const listId = useId();

  useEffect(() => {
    if (!open) return;
    const onPointer = (event: MouseEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false);
    };
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") setOpen(false);
    };
    document.addEventListener("mousedown", onPointer);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onPointer);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  return (
    <div className="dropdown" ref={rootRef} style={{ width }}>
      <button
        type="button"
        className="outline-button"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={listId}
        onClick={() => setOpen((current) => !current)}
      >
        {icon}
        <span>{display ?? value}</span>
        <img src="/assets/arrow-down.svg" width={16} height={16} alt="" className="outline-chevron" />
      </button>
      {open ? (
        <ul className="dropdown-menu" id={listId} role="listbox" aria-label={label}>
          {options.map((option) => (
            <li key={option}>
              <button
                type="button"
                role="option"
                aria-selected={option === value}
                className={option === value ? "is-selected" : undefined}
                onClick={() => {
                  onChange(option);
                  setOpen(false);
                }}
              >
                {option}
              </button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
