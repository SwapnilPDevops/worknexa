export const notificationLabels: Record<string, string> = {
  TaskAssigned: "Task assigned",
  TaskCompleted: "Task completed",
  TaskOverdue: "Task overdue",
  MeetingApproaching: "Meeting approaching",
  ReminderDue: "Reminder due",
  ProjectDeadline: "Project deadline",
  TeamMemberBlocked: "Team member blocked",
  LeaveUpdate: "Leave update",
  LearningReminder: "Learning reminder",
};

export function notificationLabel(type: string) {
  return notificationLabels[type] ?? type;
}

export function activitySentence(summary: string, actorName: string, actorId: string | null, currentUserId: string | undefined) {
  const prefix = `${actorName} `;
  if (actorId && currentUserId && actorId === currentUserId && actorName.length > 0 && summary.startsWith(prefix)) {
    return `You ${summary.slice(prefix.length)}`;
  }

  return summary;
}
