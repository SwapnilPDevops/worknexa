import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../auth/auth-context.tsx";
import { LoadingState } from "../components/design-system/states.tsx";

export function RequireAuth() {
  const { status, user } = useAuth();
  const location = useLocation();

  if (status === "loading") {
    return (
      <div className="mx-auto max-w-3xl px-6 py-16">
        <LoadingState label="Checking your session" />
      </div>
    );
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return <Outlet />;
}
