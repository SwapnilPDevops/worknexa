export const permissions = {
  usersRead: "users.read",
  usersManage: "users.manage",
  teamsRead: "teams.read",
  teamsManage: "teams.manage",
  tasksRead: "tasks.read",
  tasksManage: "tasks.manage",
  projectsRead: "projects.read",
  projectsManage: "projects.manage",
  calendarRead: "calendar.read",
  calendarManage: "calendar.manage",
  meetingsRead: "meetings.read",
  meetingsManage: "meetings.manage",
  leavesRead: "leaves.read",
  leavesManage: "leaves.manage",
  learningRead: "learning.read",
  learningManage: "learning.manage",
  notesRead: "notes.read",
  notesManage: "notes.manage",
  notificationsRead: "notifications.read",
} as const;

export type Permission = (typeof permissions)[keyof typeof permissions];

export function can(granted: readonly string[] | undefined, permission: string) {
  return granted?.includes(permission) ?? false;
}
