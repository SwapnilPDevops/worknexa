import { useQuery } from "@tanstack/react-query";
import { getJson } from "../services/api-client.ts";
import { healthResponseSchema, type HealthResponse } from "../types/health.ts";

export function useApiHealth() {
  return useQuery({
    queryKey: ["health"],
    enabled: false,
    queryFn: async (): Promise<HealthResponse> => {
      const body = await getJson("/api/health");
      return healthResponseSchema.parse(body);
    },
  });
}
