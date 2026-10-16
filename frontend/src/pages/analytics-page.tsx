import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { ErrorState, LoadingState } from "../components/design-system/states.tsx";
import { fetchAnalytics } from "../features/analytics/api.ts";
import { AnalyticsView } from "../features/analytics/analytics-view.tsx";
import { localOffsetMinutes } from "../features/work/api.ts";
import { ApiError } from "../services/api-client.ts";

export function AnalyticsPage() {
  const [weeks, setWeeks] = useState(8);
  const [teamId, setTeamId] = useState<string | null>(null);
  const offsetMinutes = localOffsetMinutes();
  const query = useQuery({
    queryKey: ["analytics", weeks, offsetMinutes, teamId],
    queryFn: () => fetchAnalytics(weeks, offsetMinutes, teamId),
    placeholderData: keepPreviousData,
  });

  useEffect(() => {
    document.title = "Analytics · Worknexa";
  }, []);

  if (query.isPending) {
    return <div className="w-full"><LoadingState label="Loading analytics" /></div>;
  }

  if (query.isError || !query.data) {
    const description = query.error instanceof ApiError ? query.error.message : "Analytics could not be loaded.";
    return <ErrorState title="Analytics is unavailable" description={description} onRetry={() => void query.refetch()} />;
  }

  return (
    <AnalyticsView
      report={query.data}
      weeks={weeks}
      onWeeksChange={setWeeks}
      onTeamChange={setTeamId}
      refreshing={query.isFetching}
    />
  );
}
