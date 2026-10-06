import { useQuery } from "@tanstack/react-query";
import { format } from "date-fns";
import { useEffect } from "react";
import { useAuth } from "../auth/auth-context.tsx";
import { ErrorState } from "../components/design-system/states.tsx";
import { fetchMyDay } from "../features/my-day/api.ts";
import { MyDayView } from "../features/my-day/my-day-view.tsx";
import { ApiError } from "../services/api-client.ts";

export function MyDayPage() {
  const { user } = useAuth();
  const now = new Date();
  const date = format(now, "yyyy-MM-dd");
  const offsetMinutes = -now.getTimezoneOffset();
  const query = useQuery({
    queryKey: ["my-day", date, offsetMinutes],
    queryFn: () => fetchMyDay(date, offsetMinutes),
  });

  useEffect(() => {
    document.title = "My Day · Worknexa";
  }, []);

  if (query.isPending) {
    return (
      <div className="mx-auto max-w-6xl space-y-4" role="status" aria-live="polite" aria-busy="true">
        <span className="sr-only">Loading your day</span>
        <div className="wx-skeleton h-8 w-64 rounded-md" />
        <div className="wx-skeleton h-4 w-96 max-w-full rounded-md" />
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-6">
          {Array.from({ length: 6 }, (_, index) => <div key={index} className="wx-skeleton h-20 rounded-xl" />)}
        </div>
        <div className="wx-skeleton h-56 rounded-xl" />
      </div>
    );
  }

  if (query.isError || !query.data) {
    const description = query.error instanceof ApiError ? query.error.message : "Your day could not be loaded.";
    return <ErrorState title="My Day is unavailable" description={description} onRetry={() => void query.refetch()} />;
  }

  return <MyDayView day={query.data} name={user?.displayName ?? "there"} now={now} />;
}
