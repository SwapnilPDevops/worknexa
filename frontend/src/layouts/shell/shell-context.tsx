import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";

type ShellContextValue = {
  sidebarCollapsed: boolean;
  toggleSidebar: () => void;
  mobileNavOpen: boolean;
  setMobileNavOpen: (open: boolean) => void;
  commandOpen: boolean;
  setCommandOpen: (open: boolean) => void;
  notificationsOpen: boolean;
  setNotificationsOpen: (open: boolean) => void;
  userMenuOpen: boolean;
  setUserMenuOpen: (open: boolean) => void;
  quickAddOpen: boolean;
  quickAddKind: string;
  setQuickAddOpen: (open: boolean, kind?: string) => void;
};

const ShellContext = createContext<ShellContextValue | null>(null);
const sidebarKey = "worknexa-sidebar";

export function ShellProvider({ children }: { children: ReactNode }) {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(() => localStorage.getItem(sidebarKey) === "collapsed");
  const [mobileNavOpen, setMobileNavOpen] = useState(false);
  const [commandOpen, setCommandOpen] = useState(false);
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const [quickAddOpen, setQuickAddOpenState] = useState(false);
  const [quickAddKind, setQuickAddKind] = useState("task");

  const value = useMemo<ShellContextValue>(
    () => ({
      sidebarCollapsed,
      toggleSidebar: () => {
        setSidebarCollapsed((current) => {
          const next = !current;
          localStorage.setItem(sidebarKey, next ? "collapsed" : "expanded");
          return next;
        });
      },
      mobileNavOpen,
      setMobileNavOpen,
      commandOpen,
      setCommandOpen: (open) => {
        setCommandOpen(open);
        if (open) {
          setNotificationsOpen(false);
          setUserMenuOpen(false);
          setQuickAddOpenState(false);
        }
      },
      notificationsOpen,
      setNotificationsOpen: (open) => {
        setNotificationsOpen(open);
        if (open) {
          setCommandOpen(false);
          setUserMenuOpen(false);
          setQuickAddOpenState(false);
        }
      },
      userMenuOpen,
      setUserMenuOpen: (open) => {
        setUserMenuOpen(open);
        if (open) {
          setCommandOpen(false);
          setNotificationsOpen(false);
          setQuickAddOpenState(false);
        }
      },
      quickAddOpen,
      quickAddKind,
      setQuickAddOpen: (open, kind) => {
        if (open) {
          setQuickAddKind(kind ?? "task");
          setCommandOpen(false);
          setNotificationsOpen(false);
          setUserMenuOpen(false);
        }
        setQuickAddOpenState(open);
      },
    }),
    [commandOpen, mobileNavOpen, notificationsOpen, quickAddKind, quickAddOpen, sidebarCollapsed, userMenuOpen],
  );

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      const meta = event.metaKey || event.ctrlKey;
      if (meta && event.key.toLowerCase() === "k") {
        event.preventDefault();
        value.setCommandOpen(!value.commandOpen);
      }
      if (meta && event.key.toLowerCase() === "b") {
        event.preventDefault();
        if (window.matchMedia("(min-width: 1024px)").matches) {
          value.toggleSidebar();
        } else {
          value.setMobileNavOpen(!value.mobileNavOpen);
        }
      }
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [value]);

  return <ShellContext.Provider value={value}>{children}</ShellContext.Provider>;
}

export function useShell() {
  const context = useContext(ShellContext);
  if (!context) {
    throw new Error("useShell must be used within ShellProvider");
  }
  return context;
}
