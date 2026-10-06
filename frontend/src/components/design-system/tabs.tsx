type TabItem = {
  id: string;
  label: string;
};

type TabsProps = {
  tabs: TabItem[];
  value: string;
  onChange: (id: string) => void;
  label: string;
};

export function Tabs({ tabs, value, onChange, label }: TabsProps) {
  return (
    <div role="tablist" aria-label={label} className="inline-flex rounded-lg border border-line bg-surface-muted p-0.5">
      {tabs.map((tab) => {
        const selected = tab.id === value;
        return (
          <button
            key={tab.id}
            type="button"
            role="tab"
            id={`tab-${tab.id}`}
            aria-selected={selected}
            tabIndex={selected ? 0 : -1}
            className={selected ? "rounded-md bg-surface px-3 py-1.5 text-sm font-medium text-ink shadow-[var(--wx-shadow-rest)]" : "rounded-md px-3 py-1.5 text-sm text-muted hover:text-ink"}
            onClick={() => onChange(tab.id)}
            onKeyDown={(event) => {
              const index = tabs.findIndex((item) => item.id === tab.id);
              if (event.key === "ArrowRight") {
                onChange(tabs[(index + 1) % tabs.length]?.id ?? tab.id);
              }
              if (event.key === "ArrowLeft") {
                onChange(tabs[(index - 1 + tabs.length) % tabs.length]?.id ?? tab.id);
              }
            }}
          >
            {tab.label}
          </button>
        );
      })}
    </div>
  );
}
