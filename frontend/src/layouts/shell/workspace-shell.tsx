import { Outlet } from "react-router-dom";
import { Drawer } from "../../components/design-system/drawer.tsx";
import { CommandPalette } from "./command-palette.tsx";
import { Header } from "./header.tsx";
import { NotificationCenter } from "./notification-center.tsx";
import { QuickAdd } from "./quick-add.tsx";
import { ShellProvider, useShell } from "./shell-context.tsx";
import { Sidebar, SidebarNav } from "./sidebar.tsx";

export function WorkspaceShell() {
  return (
    <ShellProvider>
      <ShellFrame />
    </ShellProvider>
  );
}

function ShellFrame() {
  const { mobileNavOpen, setMobileNavOpen } = useShell();

  return (
    <div className="min-h-dvh bg-canvas text-ink">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:rounded-md focus:bg-surface focus:px-3 focus:py-2">
        Skip to content
      </a>
      <div className="lg:flex">
        <Sidebar />
        <div className="min-w-0 flex-1">
          <Header />
          <main id="main" className="px-4 py-6 sm:px-6 lg:px-8">
            <Outlet />
          </main>
        </div>
      </div>
      <Drawer open={mobileNavOpen} title="Worknexa" side="left" onClose={() => setMobileNavOpen(false)}>
        <SidebarNav collapsed={false} onNavigate={() => setMobileNavOpen(false)} />
      </Drawer>
      <CommandPalette />
      <NotificationCenter />
      <QuickAdd />
    </div>
  );
}
