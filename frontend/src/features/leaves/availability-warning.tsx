import { useQuery } from "@tanstack/react-query";
import { fetchAvailability } from "./api.ts";

export function AvailabilityWarning({ userId, on }: { userId: string; on: string }) {
  const availability = useQuery({
    queryKey: ["leave-availability", userId, on],
    queryFn: () => fetchAvailability(userId, on),
    enabled: Boolean(userId && on),
  });

  if (!availability.data?.warning) {
    return null;
  }

  return <p className="rounded-lg bg-warning-soft px-3 py-2 text-sm text-warning">{availability.data.warning}</p>;
}
