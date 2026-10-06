import { z } from "zod";
import { readData } from "../../auth/session.ts";
import { getJson } from "../../services/api-client.ts";

const activitySchema = z.object({
  id: z.string(),
  actorId: z.string().nullable(),
  actorName: z.string(),
  action: z.string(),
  entity: z.string(),
  entityId: z.string(),
  timestamp: z.string(),
  metadata: z.string().nullable(),
  summary: z.string(),
});

export type ActivityItem = z.infer<typeof activitySchema>;

export async function fetchActivity() {
  return readData(await getJson("/api/activity"), z.array(activitySchema));
}
