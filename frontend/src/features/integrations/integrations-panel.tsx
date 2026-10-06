import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { formatDistanceToNow } from "date-fns";
import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Badge } from "../../components/design-system/badge.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Card, CardBody, CardHeader } from "../../components/design-system/card.tsx";
import { Dialog } from "../../components/design-system/dialog.tsx";
import { Field, Select } from "../../components/design-system/field.tsx";
import { ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { ApiError } from "../../services/api-client.ts";
import {
  disconnect,
  fetchCalendars,
  fetchIntegrations,
  fetchRuns,
  startConnect,
  syncNow,
  updateConnection,
  type IntegrationConnection,
  type IntegrationProvider,
  type SyncDirection,
  type SyncRun,
} from "./api.ts";

const categories: { id: string; label: string }[] = [
  { id: "Calendar", label: "Calendar" },
  { id: "Email", label: "Email" },
  { id: "Messaging", label: "Messaging" },
  { id: "SourceControl", label: "Source control" },
  { id: "ProjectManagement", label: "Project management" },
];

const directions: { id: SyncDirection; label: string; detail: (name: string) => string }[] = [
  { id: "ImportOnly", label: "Import only", detail: (name) => `${name} events appear in Worknexa as read-only copies.` },
  { id: "ExportOnly", label: "Export only", detail: (name) => `Events you organize in Worknexa are copied to ${name}. ${name} events stay there.` },
  { id: "TwoWay", label: "Two-way", detail: () => `Edits on either side sync. When both sides change the same event, the latest edit wins.` },
];

const failureReasons: Record<string, string> = {
  denied: "Access was not granted, so nothing was connected.",
  signin: "Your Worknexa session ended during sign-in. Sign in and connect again.",
  expired: "The connection request expired or belongs to another session. Start again from here.",
  unavailable: "This integration is not set up on the server.",
  failed: "The provider did not complete sign-in. Try again in a moment.",
};

function errorText(error: unknown, fallback: string) {
  return error instanceof ApiError ? error.message : fallback;
}

function when(value: string | null) {
  return value ? formatDistanceToNow(new Date(value), { addSuffix: true }) : "never";
}

async function beginConnect(provider: string) {
  const { authorizationUrl } = await startConnect(provider, Intl.DateTimeFormat().resolvedOptions().timeZone);
  window.location.assign(authorizationUrl);
}

export function IntegrationsPanel() {
  const [params, setParams] = useSearchParams();
  const overview = useQuery({ queryKey: ["integrations"], queryFn: fetchIntegrations });
  const connect = useMutation({ mutationFn: beginConnect });

  const outcome = params.get("integration");
  const banner = outcome === "connected"
    ? { tone: "signal" as const, text: `${overview.data?.providers.find((item) => item.key === params.get("provider"))?.name ?? "The integration"} is connected. Choose a calendar and direction, then sync.` }
    : outcome === "error"
      ? { tone: "danger" as const, text: failureReasons[params.get("reason") ?? ""] ?? failureReasons.failed }
      : null;

  function dismissBanner() {
    const next = new URLSearchParams(params);
    next.delete("integration");
    next.delete("provider");
    next.delete("reason");
    setParams(next, { replace: true });
  }

  return (
    <Card>
      <CardHeader title="Integrations" description="Connect external tools. Worknexa keeps its own data and tracks what came from where." />
      <CardBody className="grid gap-5">
        {banner ? (
          <div role="status" className={banner.tone === "signal" ? "flex items-start justify-between gap-3 rounded-lg bg-signal-soft px-3 py-2 text-sm text-ink" : "flex items-start justify-between gap-3 rounded-lg bg-danger-soft px-3 py-2 text-sm text-ink"}>
            <span>{banner.text}</span>
            <Button size="sm" variant="ghost" onClick={dismissBanner}>Dismiss</Button>
          </div>
        ) : null}
        {overview.isPending ? <LoadingState label="Loading integrations" /> : null}
        {overview.isError ? <ErrorState title="Integrations are unavailable" description={errorText(overview.error, "Integrations could not be loaded.")} onRetry={() => void overview.refetch()} /> : null}
        {connect.isError ? <p role="alert" className="text-sm text-danger">{errorText(connect.error, "The connection could not start.")}</p> : null}
        {overview.data ? categories.map((category) => {
          const providers = overview.data.providers.filter((provider) => provider.categories[0] === category.id);
          if (providers.length === 0) {
            return null;
          }

          return (
            <section key={category.id} aria-label={category.label} className="grid gap-2">
              <h3 className="text-xs font-semibold tracking-wide text-muted uppercase">{category.label}</h3>
              {providers.map((provider) => (
                <ProviderRow
                  key={provider.key}
                  provider={provider}
                  connection={overview.data.connections.find((item) => item.provider === provider.key)}
                  connecting={connect.isPending && connect.variables === provider.key}
                  onConnect={() => connect.mutate(provider.key)}
                />
              ))}
            </section>
          );
        }) : null}
      </CardBody>
    </Card>
  );
}

function ProviderRow({ provider, connection, connecting, onConnect }: {
  provider: IntegrationProvider;
  connection?: IntegrationConnection;
  connecting: boolean;
  onConnect: () => void;
}) {
  const extra = provider.categories.slice(1).map((id) => categories.find((item) => item.id === id)?.label ?? id);

  return (
    <div className="rounded-lg border border-line">
      <div className="flex flex-wrap items-center justify-between gap-3 px-3 py-3">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm font-medium">{provider.name}</span>
            {!provider.available ? <Badge>Planned</Badge> : null}
            {provider.available && !provider.configured ? <Badge tone="warning">Needs server setup</Badge> : null}
            {connection ? <StatusBadge status={connection.status} /> : null}
          </div>
          <p className="text-xs text-muted">
            {provider.vendor}
            {extra.length > 0 ? ` · also ${extra.join(", ").toLowerCase()}` : ""}
            {connection?.accountName ? ` · ${connection.accountName}` : ""}
          </p>
        </div>
        {provider.available && provider.configured && provider.canManage && !connection ? (
          <Button size="sm" onClick={onConnect} disabled={connecting}>{connecting ? "Opening…" : "Connect"}</Button>
        ) : null}
      </div>
      {provider.available && !provider.configured ? (
        <p className="border-t border-line px-3 py-2 text-xs text-muted">
          An administrator adds the app registration (client ID, secret, and redirect URI) to the server configuration to enable connecting.
        </p>
      ) : null}
      {connection ? (
        <ConnectionDetails
          key={`${connection.id}:${connection.calendarId}:${connection.direction}:${connection.syncEnabled}`}
          provider={provider}
          connection={connection}
        />
      ) : null}
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  if (status === "Connected") {
    return <Badge tone="signal">Connected</Badge>;
  }

  return status === "NeedsReconnect" ? <Badge tone="warning">Reconnect needed</Badge> : <Badge tone="danger">Sync error</Badge>;
}

function ConnectionDetails({ provider, connection }: { provider: IntegrationProvider; connection: IntegrationConnection }) {
  const queryClient = useQueryClient();
  const [direction, setDirection] = useState(connection.direction as SyncDirection);
  const [syncEnabled, setSyncEnabled] = useState(connection.syncEnabled);
  const [calendarId, setCalendarId] = useState(connection.calendarId ?? "");
  const [confirming, setConfirming] = useState(false);
  const usable = connection.status !== "NeedsReconnect" && provider.configured;
  const calendars = useQuery({
    queryKey: ["integration-calendars", connection.id],
    queryFn: async () => {
      try {
        return await fetchCalendars(connection.id);
      } catch (error) {
        if (error instanceof ApiError && error.code === "INTEGRATION_RECONNECT") {
          void queryClient.invalidateQueries({ queryKey: ["integrations"] });
        }
        throw error;
      }
    },
    enabled: usable && provider.canManage,
    retry: false,
  });
  const runs = useQuery({ queryKey: ["integration-runs", connection.id], queryFn: () => fetchRuns(connection.id) });

  function refresh() {
    return Promise.all([
      queryClient.invalidateQueries({ queryKey: ["integrations"] }),
      queryClient.invalidateQueries({ queryKey: ["integration-runs", connection.id] }),
      queryClient.invalidateQueries({ queryKey: ["calendar"] }),
      queryClient.invalidateQueries({ queryKey: ["calendar-event"] }),
      queryClient.invalidateQueries({ queryKey: ["my-day"] }),
    ]);
  }

  const save = useMutation({
    mutationFn: () => updateConnection(connection.id, { direction, syncEnabled, calendarId: calendarId || null }),
    onSuccess: refresh,
  });
  const sync = useMutation({ mutationFn: () => syncNow(connection.id), onSettled: refresh });
  const remove = useMutation({
    mutationFn: () => disconnect(connection.id),
    onSuccess: async () => {
      setConfirming(false);
      await refresh();
    },
  });
  const reconnect = useMutation({ mutationFn: () => beginConnect(provider.key) });

  const changed = direction !== connection.direction || syncEnabled !== connection.syncEnabled || (calendarId !== "" && calendarId !== connection.calendarId);
  const chosen = calendars.data?.find((item) => item.id === calendarId);
  const readOnlyCalendar = chosen !== undefined && !chosen.canEdit;
  const calendarChanging = calendarId !== "" && connection.calendarId !== null && calendarId !== connection.calendarId;

  return (
    <div className="grid gap-4 border-t border-line px-3 py-3">
      {connection.lastError ? <p role="alert" className="rounded-lg bg-warning-soft px-3 py-2 text-sm text-ink">{connection.lastError}</p> : null}
      {connection.status === "NeedsReconnect" ? (
        <div className="flex flex-wrap items-center gap-3">
          <p className="text-sm text-muted">Sign in to {provider.name} again to resume syncing. Imported events stay in place meanwhile.</p>
          {provider.canManage && provider.configured ? <Button size="sm" onClick={() => reconnect.mutate()} disabled={reconnect.isPending}>Reconnect</Button> : null}
        </div>
      ) : null}
      {reconnect.isError ? <p role="alert" className="text-sm text-danger">{errorText(reconnect.error, "The connection could not start.")}</p> : null}

      {usable && provider.canManage ? (
        <form
          className="grid gap-4"
          onSubmit={(event) => {
            event.preventDefault();
            save.mutate();
          }}
        >
          <Field
            label="Calendar"
            hint={calendarChanging ? "Switching calendars removes the copies imported from the current one." : calendars.isError ? errorText(calendars.error, "Calendars could not be loaded.") : undefined}
          >
            {({ id, describedBy }) => (
              <Select id={id} aria-describedby={describedBy} value={calendarId} onChange={(event) => setCalendarId(event.target.value)} disabled={!calendars.data}>
                {calendarId === "" ? <option value="">Default calendar</option> : null}
                {connection.calendarId && !calendars.data ? <option value={connection.calendarId}>{connection.calendarName ?? "Current calendar"}</option> : null}
                {calendars.data?.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}{item.isDefault ? " (default)" : ""}{item.canEdit ? "" : " · read-only"}
                  </option>
                ))}
              </Select>
            )}
          </Field>

          <fieldset className="grid gap-2">
            <legend className="mb-1.5 text-[13px] font-medium">Sync direction</legend>
            {directions.map((item) => (
              <label key={item.id} className="flex cursor-pointer items-start gap-3 rounded-lg border border-line px-3 py-2 has-checked:border-accent has-checked:bg-accent-soft">
                <input type="radio" name={`direction-${connection.id}`} className="mt-1" checked={direction === item.id} onChange={() => setDirection(item.id)} />
                <span>
                  <span className="block text-sm font-medium">{item.label}</span>
                  <span className="block text-xs text-muted">{item.detail(provider.name)}</span>
                </span>
              </label>
            ))}
            {readOnlyCalendar && direction !== "ImportOnly" ? <p className="text-xs text-danger">This calendar is read-only, so only import is possible.</p> : null}
          </fieldset>

          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={syncEnabled} onChange={(event) => setSyncEnabled(event.target.checked)} />
            Sync automatically in the background
          </label>

          {save.isError ? <p role="alert" className="text-sm text-danger">{errorText(save.error, "Settings could not be saved.")}</p> : null}
          <div>
            <Button type="submit" size="sm" disabled={!changed || save.isPending || (readOnlyCalendar && direction !== "ImportOnly")}>
              {save.isPending ? "Saving…" : "Save settings"}
            </Button>
          </div>
        </form>
      ) : null}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-xs text-muted">
          Last synced {when(connection.lastSyncedAt)}
          {connection.nextSyncAt ? ` · next ${when(connection.nextSyncAt)}` : ""}
          {connection.calendarName ? ` · ${connection.calendarName}` : ""}
        </p>
        {provider.canManage ? (
          <div className="flex gap-2">
            {usable ? <Button size="sm" variant="secondary" onClick={() => sync.mutate()} disabled={sync.isPending}>{sync.isPending ? "Syncing…" : "Sync now"}</Button> : null}
            <Button size="sm" variant="ghost" onClick={() => setConfirming(true)}>Disconnect</Button>
          </div>
        ) : null}
      </div>
      {sync.isError ? <p role="alert" className="text-sm text-danger">{errorText(sync.error, "The sync could not start.")}</p> : null}

      <RunLog runs={runs.data} loading={runs.isPending} />

      <Dialog open={confirming} title={`Disconnect ${provider.name}?`} description="Syncing stops and the saved sign-in is deleted." onClose={() => setConfirming(false)}>
        <div className="grid gap-4">
          <p className="text-sm text-ink">Events imported from {provider.name} are removed from Worknexa. Events created in Worknexa stay, and copies already sent to {provider.name} stay there.</p>
          {remove.isError ? <p role="alert" className="text-sm text-danger">{errorText(remove.error, "The integration could not be disconnected.")}</p> : null}
          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={() => setConfirming(false)} data-autofocus>Keep connected</Button>
            <Button variant="danger" onClick={() => remove.mutate()} disabled={remove.isPending}>{remove.isPending ? "Disconnecting…" : "Disconnect"}</Button>
          </div>
        </div>
      </Dialog>
    </div>
  );
}

function RunLog({ runs, loading }: { runs?: SyncRun[]; loading: boolean }) {
  if (loading) {
    return <p className="text-xs text-muted">Loading sync log…</p>;
  }

  if (!runs || runs.length === 0) {
    return <p className="text-xs text-muted">No syncs yet.</p>;
  }

  return (
    <details className="rounded-lg border border-line">
      <summary className="cursor-pointer px-3 py-2 text-[13px] font-medium">Sync log</summary>
      <ul className="divide-y divide-line">
        {runs.slice(0, 10).map((run) => {
          const counts = [
            ["imported", run.imported],
            ["updated", run.updated],
            ["exported", run.exported],
            ["removed", run.removed],
            ["skipped", run.skipped],
            ["conflicts", run.conflicts],
            ["failed", run.failed],
          ].filter(([, count]) => Number(count) > 0).map(([label, count]) => `${count} ${label}`);
          return (
            <li key={run.id} className="grid gap-1 px-3 py-2 text-xs">
              <div className="flex flex-wrap items-center gap-2">
                <RunBadge status={run.status} />
                <span className="text-muted">{run.trigger === "Manual" ? "Manual" : "Scheduled"} · {when(run.startedAt)}</span>
                <span>{counts.length > 0 ? counts.join(", ") : "No changes"}</span>
              </div>
              {run.error ? <p className="text-danger">{run.error}</p> : null}
              {run.failures.length > 0 ? (
                <ul className="list-disc pl-5 text-muted">
                  {run.failures.map((failure, index) => (
                    <li key={index}>{failure.operation}{failure.title ? ` "${failure.title}"` : ""}: {failure.message}</li>
                  ))}
                </ul>
              ) : null}
            </li>
          );
        })}
      </ul>
    </details>
  );
}

function RunBadge({ status }: { status: string }) {
  const tone = status === "Succeeded" ? "signal" : status === "PartiallySucceeded" ? "warning" : status === "Running" ? "accent" : "danger";
  const label = status === "PartiallySucceeded" ? "Partial" : status;
  return <Badge tone={tone}>{label}</Badge>;
}
