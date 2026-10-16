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
    <div className="min-h-dvh bg-canvas text-[#111827] antialiased">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:rounded-[8px] focus:bg-[#111827] focus:px-3 focus:py-2 focus:text-[#FED7AA] font-mono text-xs">
        Skip to content
      </a>
      <div className="lg:flex">
        <Sidebar />
        <div className="min-w-0 flex-1 flex flex-col min-h-dvh">
          <Header />
          <main id="main" className="flex-1 w-full px-4 py-6 sm:px-6 lg:px-8">
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
