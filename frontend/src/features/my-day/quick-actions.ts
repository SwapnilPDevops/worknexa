import { can, permissions, type Permission } from "../../auth/access.ts";

export type QuickAction = {
  kind: string;
  label: string;
  permission: Permission;
};

export const quickActions: readonly QuickAction[] = [
  { kind: "task", label: "Add Task", permission: permissions.tasksManage },
  { kind: "todo", label: "Add Todo", permission: permissions.tasksManage },
  { kind: "meeting", label: "Add Meeting", permission: permissions.meetingsManage },
  { kind: "reminder", label: "Add Reminder", permission: permissions.tasksManage },
  { kind: "note", label: "Add Note", permission: permissions.notesManage },
  { kind: "team-task", label: "Assign Team Task", permission: permissions.tasksManage },
  { kind: "learning-note", label: "Add Learning Note", permission: permissions.learningManage },
];

export function allowedQuickActions(granted: readonly string[] | undefined) {
  return quickActions.filter((action) => can(granted, action.permission));
}
