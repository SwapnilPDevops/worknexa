import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { format } from "date-fns";
import { useNavigate } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Drawer } from "../../components/design-system/drawer.tsx";
import { EmptyState } from "../../components/design-system/states.tsx";
import { notificationLabel } from "../../features/inbox/catalog.ts";
import { fetchNotifications, markAllNotificationsRead, markNotificationRead, markNotificationUnread } from "../../features/schedule/api.ts";
import { Badge } from "../../components/design-system/badge.tsx";
import { cn } from "../../utils/cn.ts";
import { useShell } from "./shell-context.tsx";

export function NotificationCenter() {
  const { notificationsOpen, setNotificationsOpen } = useShell();
  const { user } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const enabled = notificationsOpen && can(user?.permissions, permissions.notificationsRead);
  const notifications = useQuery({
    queryKey: ["notifications"],
    queryFn: fetchNotifications,
    enabled,
  });
  const markRead = useMutation({
    mutationFn: markNotificationRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notifications"] }),
  });
  const markAll = useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notifications"] }),
  });
  const markUnread = useMutation({
    mutationFn: markNotificationUnread,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["notifications"] }),
  });
  const items = notifications.data ?? [];

  return (
    <Drawer open={notificationsOpen} title="Notifications" onClose={() => setNotificationsOpen(false)}>
      <div className="mb-3">
        <Button variant="ghost" size="sm" onClick={() => { setNotificationsOpen(false); navigate("/notifications"); }}>Open inbox</Button>
      </div>
      {items.length === 0 ? (
        <EmptyState title="You're caught up" description="Task updates, meetings, reminders, and deadlines arrive here." />
      ) : (
        <div className="space-y-3">
          <Button variant="secondary" size="sm" onClick={() => markAll.mutate()} disabled={markAll.isPending}>Mark all read</Button>
          <ul className="space-y-2">
            {items.map((item) => (
              <li key={item.id} className={cn("rounded-lg border border-line px-3 py-2", item.isRead ? "bg-surface" : "bg-accent-soft")}>
                <button
                  type="button"
                  className="w-full text-left"
                  onClick={() => {
                    if (!item.isRead) {
                      markRead.mutate(item.id);
                    }
                    setNotificationsOpen(false);
                    if (item.link) {
                      navigate(item.link);
                    }
                  }}
                >
                  <Badge>{notificationLabel(item.type)}</Badge>
                  <span className="mt-1 block text-sm font-medium">{item.title}</span>
                  <span className="mt-0.5 block text-xs text-muted">{item.body}</span>
                  <span className="mt-1 block text-[11px] text-muted">{format(new Date(item.createdAt), "d MMM, HH:mm")}</span>
                </button>
                {item.isRead ? (
                  <Button className="mt-2" variant="ghost" size="sm" onClick={() => markUnread.mutate(item.id)}>Mark unread</Button>
                ) : null}
              </li>
            ))}
          </ul>
        </div>
      )}
    </Drawer>
  );
}
