import { can, permissions } from "../../auth/access.ts";
import {
  Bell,
  BookOpen,
  Calendar,
  ChartColumn,
  CircleCheck,
  FolderKanban,
  GraduationCap,
  Inbox,
  ListChecks,
  ListTodo,
  Map,
  MessagesSquare,
  NotebookPen,
  Settings,
  StickyNote,
  Sun,
  TreePalm,
  Users,
  UsersRound,
  type LucideIcon,
} from "lucide-react";

export type NavItem = {
  label: string;
  to: string;
  icon: LucideIcon;
  description: string;
  permission?: string;
};

export type NavSection = {
  label: string;
  items: NavItem[];
};

export const navSections: NavSection[] = [
  {
    label: "My work",
    items: [
      { label: "My Day", to: "/", icon: Sun, description: "A calm view of what needs attention today." },
      { label: "Tasks", to: "/tasks", icon: ListChecks, description: "Work assigned to you, grouped by what is next.", permission: permissions.tasksRead },
      { label: "Todo", to: "/todo", icon: CircleCheck, description: "Short personal items that should stay out of the task board.", permission: permissions.tasksRead },
      { label: "Calendar", to: "/calendar", icon: Calendar, description: "Days, events, and the time you have already promised.", permission: permissions.calendarRead },
      { label: "Reminders", to: "/reminders", icon: Bell, description: "Nudges for work that should not slip.", permission: permissions.tasksRead },
      { label: "Analytics", to: "/analytics", icon: ChartColumn, description: "Trends in your work, team, projects, and learning." },
    ],
  },
  {
    label: "Meetings",
    items: [
      { label: "Meetings", to: "/meetings", icon: Users, description: "Upcoming conversations and who is joining.", permission: permissions.meetingsRead },
      { label: "Standups", to: "/standups", icon: MessagesSquare, description: "A daily check-in for the team.", permission: permissions.meetingsRead },
      { label: "Meeting Notes", to: "/meeting-notes", icon: NotebookPen, description: "Decisions and follow-ups captured in one place.", permission: permissions.meetingsRead },
    ],
  },
  {
    label: "Projects",
    items: [{ label: "Projects", to: "/projects", icon: FolderKanban, description: "The bodies of work your tasks belong to.", permission: permissions.projectsRead }],
  },
  {
    label: "Team",
    items: [
      { label: "Team", to: "/team", icon: UsersRound, description: "People you work with and how the group is organized.", permission: permissions.teamsRead },
      { label: "Team Tasks", to: "/team-tasks", icon: ListTodo, description: "Work shared across the team.", permission: permissions.tasksRead },
      { label: "Leaves", to: "/leaves", icon: TreePalm, description: "Time away, so planning stays honest.", permission: permissions.leavesRead },
    ],
  },
  {
    label: "Learning",
    items: [
      { label: "Learning", to: "/learning", icon: GraduationCap, description: "Topics you are studying alongside the work.", permission: permissions.learningRead },
      { label: "Roadmap", to: "/roadmap", icon: Map, description: "The path from what you know to what you want to learn.", permission: permissions.learningRead },
      { label: "Learning Notes", to: "/learning-notes", icon: BookOpen, description: "Notes tied to a topic, not lost in a general notebook.", permission: permissions.learningRead },
    ],
  },
  {
    label: "Knowledge",
    items: [{ label: "Notes", to: "/notes", icon: StickyNote, description: "Reference you want to find again.", permission: permissions.notesRead }],
  },
  {
    label: "System",
    items: [
      { label: "Notifications", to: "/notifications", icon: Inbox, description: "Signals that something needs you.", permission: permissions.notificationsRead },
      { label: "Settings", to: "/settings", icon: Settings, description: "Appearance and workspace preferences." },
    ],
  },
];

export function visibleNavSections(granted: readonly string[] | undefined) {
  return navSections
    .map((section) => ({
      ...section,
      items: section.items.filter((item) => !item.permission || can(granted, item.permission)),
    }))
    .filter((section) => section.items.length > 0);
}

export function findNavItem(pathname: string) {
  return navSections.flatMap((section) => section.items.map((item) => ({ ...item, section: section.label }))).find((item) => item.to === pathname);
}
