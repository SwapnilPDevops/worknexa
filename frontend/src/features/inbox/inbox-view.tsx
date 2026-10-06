import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { format } from "date-fns";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Badge } from "../../components/design-system/badge.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Card, CardBody, CardHeader } from "../../components/design-system/card.tsx";
import { EmptyState, ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { Tabs } from "../../components/design-system/tabs.tsx";
import { ApiError } from "../../services/api-client.ts";
import {
  fetchNotificationPreferences,
  fetchNotifications,
  markAllNotificationsRead,
  markNotificationRead,
  markNotificationUnread,
  saveNotificationPreference,
  type NotificationItem,
} from "../schedule/api.ts";
import { fetchActivity } from "./api.ts";
import { activitySentence, notificationLabel } from "./catalog.ts";
import { cn } from "../../utils/cn.ts";

const tabs = [
  { id: "inbox", label: "Inbox" },
  { id: "activity", label: "Activity" },
  { id: "preferences", label: "Preferences" },
];

export function InboxView() {
  const { user } = useAuth();
  const [tab, setTab] = useState("inbox");

  if (!can(user?.permissions, permissions.notificationsRead)) {
    return <ErrorState title="Access denied" description="Your account does not include notifications." />;
  }

  return (
    <div className="mx-auto max-w-3xl space-y-4">
      <div>
        <h1 className="text-lg font-semibold">Notifications</h1>
        <p className="mt-1 text-sm text-muted">What needs you, and what just happened in the workspace.</p>
      </div>
      <Tabs tabs={tabs} value={tab} onChange={setTab} label="Notification views" />
      {tab === "inbox" ? <InboxPanel /> : null}
      {tab === "activity" ? <ActivityPanel userId={user?.id} /> : null}
      {tab === "preferences" ? <PreferencesPanel /> : null}
    </div>
  );
}

function InboxPanel() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const notifications = useQuery({ queryKey: ["notifications"], queryFn: fetchNotifications });
  const refresh = () => queryClient.invalidateQueries({ queryKey: ["notifications"] });
  const markRead = useMutation({ mutationFn: markNotificationRead, onSuccess: refresh });
  const markUnread = useMutation({ mutationFn: markNotificationUnread, onSuccess: refresh });
  const markAll = useMutation({ mutationFn: markAllNotificationsRead, onSuccess: refresh });

  if (notifications.isPending) {
    return <LoadingState label="Loading notifications" />;
  }

  if (notifications.isError || !notifications.data) {
    return <ErrorState title="Notifications are unavailable" description={notifications.error instanceof ApiError ? notifications.error.message : "Notifications could not be loaded."} onRetry={() => { void notifications.refetch(); }} />;
  }

  if (notifications.data.length === 0) {
    return <EmptyState title="You're caught up" description="Task updates, meetings, reminders, and deadlines arrive here." />;
  }

  return (
    <Card>
      <CardHeader title="Inbox" description="Newest first." action={<Button variant="secondary" size="sm" onClick={() => markAll.mutate()} disabled={markAll.isPending}>Mark all read</Button>} />
      <CardBody className="space-y-2">
        {notifications.data.map((item) => (
          <NotificationRow
            key={item.id}
            item={item}
            pending={markRead.isPending || markUnread.isPending}
            onOpen={() => {
              if (!item.isRead) {
                markRead.mutate(item.id);
              }
              if (item.link) {
                navigate(item.link);
              }
            }}
            onUnread={() => markUnread.mutate(item.id)}
          />
        ))}
      </CardBody>
    </Card>
  );
}

function NotificationRow({ item, pending, onOpen, onUnread }: { item: NotificationItem; pending: boolean; onOpen: () => void; onUnread: () => void }) {
  return (
    <div className={cn("rounded-lg border border-line px-3 py-2", item.isRead ? "bg-surface" : "bg-accent-soft")}>
      <button type="button" className="w-full text-left" onClick={onOpen}>
        <span className="flex items-center gap-2">
          <Badge>{notificationLabel(item.type)}</Badge>
          <span className="text-sm font-medium">{item.title}</span>
        </span>
        <span className="mt-1 block text-xs text-muted">{item.body}</span>
        <span className="mt-1 block text-[11px] text-muted">{format(new Date(item.createdAt), "d MMM, HH:mm")}</span>
      </button>
      {item.isRead ? (
        <Button className="mt-2" variant="ghost" size="sm" disabled={pending} onClick={onUnread}>Mark unread</Button>
      ) : null}
    </div>
  );
}

function ActivityPanel({ userId }: { userId: string | undefined }) {
  const activity = useQuery({ queryKey: ["activity"], queryFn: fetchActivity });

  if (activity.isPending) {
    return <LoadingState label="Loading activity" />;
  }

  if (activity.isError || !activity.data) {
    return <ErrorState title="Activity is unavailable" description={activity.error instanceof ApiError ? activity.error.message : "Activity could not be loaded."} onRetry={() => { void activity.refetch(); }} />;
  }

  if (activity.data.length === 0) {
    return <EmptyState title="No activity yet" description="Assignments, completions, and status changes show up on this timeline." />;
  }

  return (
    <Card>
      <CardHeader title="Activity timeline" description="Who did what, and when." />
      <CardBody>
        <ol className="space-y-4 border-l border-line pl-4">
          {activity.data.map((item) => (
            <li key={item.id} className="relative">
              <span className="absolute -left-[1.3rem] top-1 size-2 rounded-full bg-accent" />
              <p className="text-sm">{activitySentence(item.summary, item.actorName, item.actorId, userId)}</p>
              <p className="mt-0.5 text-[11px] text-muted">{item.action} · {format(new Date(item.timestamp), "d MMM, HH:mm")}</p>
            </li>
          ))}
        </ol>
      </CardBody>
    </Card>
  );
}

function PreferencesPanel() {
  const queryClient = useQueryClient();
  const preferences = useQuery({ queryKey: ["notification-preferences"], queryFn: fetchNotificationPreferences });
  const save = useMutation({
    mutationFn: ({ type, enabled }: { type: string; enabled: boolean }) => saveNotificationPreference(type, enabled),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notification-preferences"] }),
  });

  if (preferences.isPending) {
    return <LoadingState label="Loading preferences" />;
  }

  if (preferences.isError || !preferences.data) {
    return <ErrorState title="Preferences are unavailable" description={preferences.error instanceof ApiError ? preferences.error.message : "Preferences could not be loaded."} onRetry={() => { void preferences.refetch(); }} />;
  }

  return (
    <Card>
      <CardHeader title="Notification preferences" description="Muted types stay out of your inbox. Activity is still recorded." />
      <CardBody className="divide-y divide-line p-0">
        {preferences.data.map((preference) => (
          <div key={preference.type} className="flex items-center justify-between gap-3 px-4 py-3">
            <span className="text-sm">{notificationLabel(preference.type)}</span>
            <Button
              variant={preference.enabled ? "secondary" : "ghost"}
              size="sm"
              aria-pressed={preference.enabled}
              disabled={save.isPending}
              onClick={() => save.mutate({ type: preference.type, enabled: !preference.enabled })}
            >
              {preference.enabled ? "On" : "Off"}
            </Button>
          </div>
        ))}
      </CardBody>
    </Card>
  );
}
