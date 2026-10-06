export type Priority = "Low" | "High" | "Completed";
export type ColumnId = "todo" | "progress" | "done";
export type NavId = "home" | "messages" | "tasks" | "members" | "settings";
export type Period = "Today" | "This week" | "This month";

export type TaskImage =
  | { kind: "single"; src: string; width: number; height: number }
  | { kind: "pair"; sources: [string, string] }
  | { kind: "cover"; src: string; width: number; height: number; offsetX: number };

export type Task = {
  id: string;
  title: string;
  description?: string;
  descriptionColor?: "primary" | "secondary";
  priority: Priority;
  comments: number;
  files: number;
  avatars: string[];
  image?: TaskImage;
  column: ColumnId;
};

export type Project = {
  id: string;
  name: string;
  dot: string;
};

export const navItems: { id: NavId; label: string; icon: string }[] = [
  { id: "home", label: "Home", icon: "home" },
  { id: "messages", label: "Messages", icon: "messages" },
  { id: "tasks", label: "Tasks", icon: "tasks" },
  { id: "members", label: "Members", icon: "members" },
  { id: "settings", label: "Settings", icon: "settings" },
];

export const projects: Project[] = [
  { id: "mobile-app", name: "Mobile App", dot: "dot-green" },
  { id: "website", name: "Website Redesign", dot: "dot-orange" },
  { id: "design-system", name: "Design System", dot: "dot-lilac" },
  { id: "wireframes", name: "Wireframes", dot: "dot-blue" },
];

export const members = [
  "/assets/member-1.png",
  "/assets/member-2.png",
  "/assets/member-3.png",
  "/assets/member-4.png",
];

export const columns: { id: ColumnId; label: string; dot: string; line: string; accent: string }[] = [
  { id: "todo", label: "To Do", dot: "dot-todo", line: "line-todo", accent: "var(--color-todo)" },
  { id: "progress", label: "On Progress", dot: "dot-orange", line: "line-progress", accent: "var(--color-progress)" },
  { id: "done", label: "Done", dot: "dot-blue", line: "line-done", accent: "var(--color-complete)" },
];

export const initialTasks: Task[] = [
  {
    id: "brainstorming",
    title: "Brainstorming",
    description: "Brainstorming brings team members' diverse experience into play.",
    priority: "Low",
    comments: 12,
    files: 0,
    avatars: ["/assets/avatar-c.png", "/assets/avatar-b.png", "/assets/avatar-a.png"],
    column: "todo",
  },
  {
    id: "research",
    title: "Research",
    description: "User research helps you to create an optimal product for users.",
    priority: "High",
    comments: 10,
    files: 3,
    avatars: ["/assets/avatar-h.png", "/assets/avatar-g.png"],
    column: "todo",
  },
  {
    id: "wireframes",
    title: "Wireframes",
    description: "Low fidelity wireframes include the most basic content and visuals.",
    priority: "High",
    comments: 2,
    files: 0,
    avatars: ["/assets/avatar-c.png", "/assets/avatar-g.png", "/assets/avatar-d.png"],
    column: "todo",
  },
  {
    id: "onboarding",
    title: "Onboarding Illustrations",
    priority: "Low",
    comments: 14,
    files: 15,
    avatars: ["/assets/avatar-a.png", "/assets/avatar-b.png", "/assets/avatar-c.png"],
    image: { kind: "single", src: "/assets/photo-onboarding.png", width: 274, height: 110 },
    column: "progress",
  },
  {
    id: "moodboard",
    title: "Moodboard",
    priority: "Low",
    comments: 9,
    files: 10,
    avatars: ["/assets/avatar-a.png"],
    image: { kind: "pair", sources: ["/assets/photo-mood-1.png", "/assets/photo-mood-2.png"] },
    column: "progress",
  },
  {
    id: "mobile-design",
    title: "Mobile App Design",
    priority: "Completed",
    comments: 12,
    files: 15,
    avatars: ["/assets/avatar-f.png", "/assets/avatar-b.png"],
    image: { kind: "cover", src: "/assets/photo-mobile.png", width: 304, height: 228, offsetX: -15 },
    column: "done",
  },
  {
    id: "design-system-task",
    title: "Design System",
    description: "It just needs to adapt the UI from what you did before",
    descriptionColor: "primary",
    priority: "Completed",
    comments: 12,
    files: 15,
    avatars: ["/assets/avatar-c.png", "/assets/avatar-e.png", "/assets/avatar-d.png"],
    column: "done",
  },
];

export const priorityFilters: Array<Priority | "All"> = ["All", "Low", "High", "Completed"];
export const periods: Period[] = ["Today", "This week", "This month"];
