import { z } from "zod";
import { readData } from "../../auth/session.ts";
import { getJson, sendJson } from "../../services/api-client.ts";

const failureSchema = z.object({
  operation: z.string(),
  title: z.string().nullable(),
  message: z.string(),
});

const runSchema = z.object({
  id: z.string(),
  trigger: z.string(),
  direction: z.string(),
  status: z.string(),
  startedAt: z.string(),
  finishedAt: z.string().nullable(),
  imported: z.number(),
  updated: z.number(),
  exported: z.number(),
  removed: z.number(),
  skipped: z.number(),
  conflicts: z.number(),
  failed: z.number(),
  error: z.string().nullable(),
  failures: z.array(failureSchema),
});

const connectionSchema = z.object({
  id: z.string(),
  provider: z.string(),
  category: z.string(),
  status: z.string(),
  direction: z.string(),
  syncEnabled: z.boolean(),
  accountName: z.string().nullable(),
  calendarId: z.string().nullable(),
  calendarName: z.string().nullable(),
  timeZone: z.string().nullable(),
  lastSyncedAt: z.string().nullable(),
  nextSyncAt: z.string().nullable(),
  consecutiveFailures: z.number(),
  lastError: z.string().nullable(),
  lastRun: runSchema.nullable(),
});

const overviewSchema = z.object({
  providers: z.array(z.object({
    key: z.string(),
    name: z.string(),
    vendor: z.string(),
    categories: z.array(z.string()),
    available: z.boolean(),
    configured: z.boolean(),
    canManage: z.boolean(),
  })),
  connections: z.array(connectionSchema),
});

const calendarSchema = z.object({ id: z.string(), name: z.string(), isDefault: z.boolean(), canEdit: z.boolean() });

export type IntegrationOverview = z.infer<typeof overviewSchema>;
export type IntegrationProvider = IntegrationOverview["providers"][number];
export type IntegrationConnection = z.infer<typeof connectionSchema>;
export type SyncRun = z.infer<typeof runSchema>;
export type ExternalCalendar = z.infer<typeof calendarSchema>;
export type SyncDirection = "ImportOnly" | "ExportOnly" | "TwoWay";

export async function fetchIntegrations() {
  return readData(await getJson("/api/integrations"), overviewSchema);
}

export async function startConnect(provider: string, timeZone: string) {
  return readData(await sendJson(`/api/integrations/${provider}/connect`, "POST", { timeZone }), z.object({ authorizationUrl: z.string() }));
}

export async function fetchCalendars(connectionId: string) {
  return readData(await getJson(`/api/integrations/connections/${connectionId}/calendars`), z.array(calendarSchema));
}

export async function updateConnection(connectionId: string, settings: { direction: SyncDirection; syncEnabled: boolean; calendarId: string | null }) {
  return readData(await sendJson(`/api/integrations/connections/${connectionId}`, "PUT", settings), connectionSchema);
}

export async function syncNow(connectionId: string) {
  return readData(await sendJson(`/api/integrations/connections/${connectionId}/sync`, "POST"), runSchema);
}

export async function fetchRuns(connectionId: string) {
  return readData(await getJson(`/api/integrations/connections/${connectionId}/runs`), z.array(runSchema));
}

export async function disconnect(connectionId: string) {
  return readData(await sendJson(`/api/integrations/connections/${connectionId}`, "DELETE"), z.object({ removedEvents: z.number() }));
}
