export const meetingViews = [
  { id: "today", label: "Today" },
  { id: "upcoming", label: "Upcoming" },
  { id: "past", label: "Past" },
  { id: "calendar", label: "Calendar" },
] as const;

export type MeetingView = (typeof meetingViews)[number]["id"];

export const meetingTypes = [
  { id: "OneOnOne", label: "1:1" },
  { id: "Team", label: "Team" },
  { id: "Client", label: "Client" },
  { id: "Project", label: "Project" },
  { id: "Technical", label: "Technical" },
  { id: "Interview", label: "Interview" },
  { id: "Planning", label: "Planning" },
  { id: "Review", label: "Review" },
  { id: "Standup", label: "Standup" },
] as const;

export const standupStatuses = [
  { id: "Clear", label: "Clear" },
  { id: "Blocked", label: "Blocked" },
  { id: "Away", label: "Away" },
] as const;

export function meetingTypeLabel(value: string) {
  return meetingTypes.find((item) => item.id === value)?.label ?? value;
}
