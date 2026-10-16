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
    <div role="tablist" aria-label={label} className="inline-flex rounded-[8px] border border-[#D1CCC2] bg-[#D7D3CB] p-1 font-mono">
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
            className={
              selected
                ? "rounded-[6px] bg-[#FED7AA] border border-[#111827]/15 px-3 py-1.5 text-xs font-semibold text-[#111827] shadow-xs"
                : "rounded-[6px] px-3 py-1.5 text-xs font-medium text-[#4B5563] hover:text-[#111827] transition-colors"
            }
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
