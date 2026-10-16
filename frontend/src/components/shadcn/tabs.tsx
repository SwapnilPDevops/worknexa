import { createContext, useContext, useState, type HTMLAttributes, type ReactNode } from "react";
import { cn } from "../../utils/cn.ts";

interface TabsContextValue {
  value: string;
  onValueChange: (value: string) => void;
}

const TabsContext = createContext<TabsContextValue | null>(null);

export interface TabsProps extends HTMLAttributes<HTMLDivElement> {
  value?: string;
  defaultValue?: string;
  onValueChange?: (value: string) => void;
  children: ReactNode;
}

export function Tabs({
  value: controlledValue,
  defaultValue,
  onValueChange,
  className,
  children,
  ...props
}: TabsProps) {
  const [uncontrolledValue, setUncontrolledValue] = useState(defaultValue ?? "");
  const isControlled = controlledValue !== undefined;
  const activeValue = isControlled ? controlledValue : uncontrolledValue;

  const handleValueChange = (val: string) => {
    if (!isControlled) {
      setUncontrolledValue(val);
    }
    onValueChange?.(val);
  };

  return (
    <TabsContext.Provider value={{ value: activeValue, onValueChange: handleValueChange }}>
      <div className={cn("w-full", className)} {...props}>
        {children}
      </div>
    </TabsContext.Provider>
  );
}

export function TabsList({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      role="tablist"
      className={cn(
        "inline-flex h-9 items-center justify-center rounded-[8px] bg-[#D7D3CB] p-1 text-[#4B5563] border border-[#111827]/10",
        className,
      )}
      {...props}
    />
  );
}

export interface TabsTriggerProps extends HTMLAttributes<HTMLButtonElement> {
  value: string;
}

export function TabsTrigger({ value, className, ...props }: TabsTriggerProps) {
  const context = useContext(TabsContext);
  if (!context) throw new Error("TabsTrigger must be used within Tabs");

  const isActive = context.value === value;

  return (
    <button
      type="button"
      role="tab"
      aria-selected={isActive}
      className={cn(
        "inline-flex items-center justify-center whitespace-nowrap rounded-[6px] px-3 py-1 text-xs font-medium transition-all focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-[#111827] disabled:pointer-events-none disabled:opacity-50 cursor-pointer",
        isActive
          ? "bg-[#FED7AA] text-[#111827] font-semibold border border-[#111827]/15 shadow-xs"
          : "hover:text-[#111827] hover:bg-[#EBE8E2]/60",
        className,
      )}
      onClick={() => context.onValueChange(value)}
      {...props}
    />
  );
}

export interface TabsContentProps extends HTMLAttributes<HTMLDivElement> {
  value: string;
}

export function TabsContent({ value, className, ...props }: TabsContentProps) {
  const context = useContext(TabsContext);
  if (!context) throw new Error("TabsContent must be used within Tabs");

  if (context.value !== value) return null;

  return (
    <div
      role="tabpanel"
      className={cn("mt-2 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-[#111827]", className)}
      {...props}
    />
  );
}
