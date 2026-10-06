import { Route, Routes } from "react-router-dom";
import { RequireAuth } from "./require-auth.tsx";
import { AnalyticsPage } from "../pages/analytics-page.tsx";
import { WorkspaceShell } from "../layouts/shell/workspace-shell.tsx";
import { FoundationsPage } from "../pages/foundations-page.tsx";
import { LoginPage } from "../pages/login-page.tsx";
import { LearningNotesPage } from "../pages/learning-notes-page.tsx";
import { LearningPage } from "../pages/learning-page.tsx";
import { LeavesPage } from "../pages/leaves-page.tsx";
import { ProfilePage } from "../pages/profile-page.tsx";
import { ProjectsPage } from "../pages/projects-page.tsx";
import { TeamPage } from "../pages/team-page.tsx";
import { TeamTasksPage } from "../pages/team-tasks-page.tsx";
import { RoadmapPage } from "../pages/roadmap-page.tsx";
import { SettingsPage } from "../pages/settings-page.tsx";
import { MyDayPage } from "../pages/my-day-page.tsx";
import { CalendarPage } from "../pages/calendar-page.tsx";
import { MeetingsPage } from "../pages/meetings-page.tsx";
import { MeetingNotesPage } from "../pages/meeting-notes-page.tsx";
import { NotesPage } from "../pages/notes-page.tsx";
import { NotificationsPage } from "../pages/notifications-page.tsx";
import { StandupsPage } from "../pages/standups-page.tsx";
import { RemindersPage } from "../pages/reminders-page.tsx";
import { TasksPage } from "../pages/tasks-page.tsx";
import { TodosPage } from "../pages/todos-page.tsx";
import { WorkspacePage } from "../pages/workspace-page.tsx";
import { navSections } from "../layouts/shell/nav.ts";

const childPaths = navSections.flatMap((section) => section.items.map((item) => item.to)).filter((path) => path !== "/");
const placeholderPaths = childPaths.filter((path) => !["/settings", "/tasks", "/todo", "/team", "/team-tasks", "/leaves", "/calendar", "/reminders", "/meetings", "/standups", "/meeting-notes", "/notes", "/projects", "/learning", "/roadmap", "/learning-notes", "/notifications", "/analytics"].includes(path));

export function AppRouter() {
  return (
    <Routes>
      <Route path="login" element={<LoginPage />} />
      <Route element={<RequireAuth />}>
      <Route element={<WorkspaceShell />}>
        <Route index element={<MyDayPage />} />
        <Route path="tasks" element={<TasksPage initialView="list" />} />
        <Route path="todo" element={<TodosPage />} />
        <Route path="calendar" element={<CalendarPage />} />
        <Route path="reminders" element={<RemindersPage />} />
        <Route path="meetings" element={<MeetingsPage />} />
        <Route path="meeting-notes" element={<MeetingNotesPage />} />
        <Route path="notes" element={<NotesPage />} />
        <Route path="projects" element={<ProjectsPage />} />
        <Route path="team" element={<TeamPage />} />
        <Route path="team-tasks" element={<TeamTasksPage />} />
        <Route path="leaves" element={<LeavesPage />} />
        <Route path="learning" element={<LearningPage />} />
        <Route path="roadmap" element={<RoadmapPage />} />
        <Route path="learning-notes" element={<LearningNotesPage />} />
        <Route path="standups" element={<StandupsPage />} />
        <Route path="notifications" element={<NotificationsPage />} />
        <Route path="analytics" element={<AnalyticsPage />} />
        {placeholderPaths.map((path) => (
            <Route key={path} path={path.slice(1)} element={<WorkspacePage />} />
          ))}
        <Route path="settings" element={<SettingsPage />} />
        <Route path="profile" element={<ProfilePage />} />
        <Route path="foundations" element={<FoundationsPage />} />
        <Route path="*" element={<WorkspacePage />} />
      </Route>
      </Route>
    </Routes>
  );
}
