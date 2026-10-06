import { z } from "zod";
import { readData } from "../../auth/session.ts";
import { getJson } from "../../services/api-client.ts";

const searchHitSchema = z.object({
  entity: z.string(),
  id: z.string(),
  title: z.string(),
  detail: z.string().nullable(),
  route: z.string(),
});

const searchGroupSchema = z.object({
  entity: z.string(),
  label: z.string(),
  hits: z.array(searchHitSchema),
});

export type SearchHit = z.infer<typeof searchHitSchema>;
export type SearchGroup = z.infer<typeof searchGroupSchema>;

export async function searchWorkspace(query: string) {
  const params = new URLSearchParams({ q: query, limit: "5" });
  return readData(await getJson(`/api/search?${params}`), z.array(searchGroupSchema));
}
