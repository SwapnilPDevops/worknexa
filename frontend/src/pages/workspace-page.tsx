import { useEffect } from "react";
import { Link, useLocation } from "react-router-dom";
import { can } from "../auth/access.ts";
import { useAuth } from "../auth/auth-context.tsx";
import { Button } from "../components/design-system/button.tsx";
import { EmptyState, ErrorState } from "../components/design-system/states.tsx";
import { findNavItem } from "../layouts/shell/nav.ts";
import { useShell } from "../layouts/shell/shell-context.tsx";

export function WorkspacePage() {
  const { pathname } = useLocation();
  const item = findNavItem(pathname);
  const { setQuickAddOpen } = useShell();
  const { user } = useAuth();

  useEffect(() => {
    document.title = item ? `${item.label} · Worknexa` : "Worknexa";
  }, [item]);

  if (item?.permission && !can(user?.permissions, item.permission)) {
    return <ErrorState title="Access denied" description="Your account does not include this area." />;
  }

  if (!item) {
    return (
      <div className="space-y-4">
        <ErrorState title="Page not found" description="That destination is not part of Worknexa." />
        <Link to="/" className="text-sm font-medium text-accent">
          Back to My Day
        </Link>
      </div>
    );
  }

  return (
    <EmptyState
      title={item.label}
      description={item.description}
      action={
        <Button onClick={() => setQuickAddOpen(true)}>Quick add</Button>
      }
    />
  );
}
