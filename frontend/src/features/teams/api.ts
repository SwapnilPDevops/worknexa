import { z } from "zod";
import { readData } from "../../auth/session.ts";
import { getJson, sendJson } from "../../services/api-client.ts";

const teamMemberSchema = z.object({
  userId: z.string(),
  name: z.string(),
  email: z.string(),
  role: z.string(),
  status: z.string(),
  designation: z.string().nullable(),
});

const teamListSchema = z.object({
  id: z.string(),
  name: z.string(),
  description: z.string().nullable(),
  memberCount: z.number(),
});

const teamDetailSchema = teamListSchema.omit({ memberCount: true }).extend({
  members: z.array(teamMemberSchema),
});

const personSchema = z.object({
  id: z.string(),
  name: z.string(),
  designation: z.string().nullable(),
});

const memberListSchema = z.object({
  id: z.string(),
  name: z.string(),
  email: z.string(),
  photoUrl: z.string().nullable(),
  designation: z.string().nullable(),
  department: z.string().nullable(),
  status: z.string(),
});

const memberTeamSchema = z.object({
  teamId: z.string(),
  name: z.string(),
  role: z.string(),
});

export const memberProfileSchema = z.object({
  id: z.string(),
  name: z.string(),
  email: z.string(),
  phone: z.string().nullable(),
  photoUrl: z.string().nullable(),
  designation: z.string().nullable(),
  department: z.string().nullable(),
  joinedOn: z.string().nullable(),
  managerId: z.string().nullable(),
  managerName: z.string().nullable(),
  status: z.string(),
  notes: z.string().nullable(),
  skills: z.array(z.string()),
  technologies: z.array(z.string()),
  teams: z.array(memberTeamSchema),
});

const projectSchema = z.object({
  id: z.string(),
  name: z.string(),
  code: z.string(),
  status: z.string(),
  role: z.string(),
});

const standupSchema = z.object({
  id: z.string(),
  standupDate: z.string(),
  teamName: z.string().nullable(),
  status: z.string(),
  yesterday: z.string().nullable(),
  today: z.string().nullable(),
  blockers: z.string().nullable(),
});

const activitySchema = z.object({
  action: z.string(),
  summary: z.string(),
  occurredAt: z.string(),
});

const workloadSchema = z.object({
  userId: z.string(),
  name: z.string(),
  assigned: z.number(),
  inProgress: z.number(),
  completed: z.number(),
  overdue: z.number(),
  blocked: z.number(),
});

export type TeamListItem = z.infer<typeof teamListSchema>;
export type TeamDetail = z.infer<typeof teamDetailSchema>;
export type MemberListItem = z.infer<typeof memberListSchema>;
export type MemberProfile = z.infer<typeof memberProfileSchema>;
export type TeamWorkloadRow = z.infer<typeof workloadSchema>;

export type TeamWrite = {
  name: string;
  description: string | null;
};

export type MemberWrite = {
  name: string;
  email: string;
  phone: string | null;
  photoUrl: string | null;
  designation: string | null;
  department: string | null;
  joinedOn: string | null;
  managerId: string | null;
  status: string;
  notes: string | null;
  skills: string[];
  technologies: string[];
};

export async function fetchTeams() {
  return readData(await getJson("/api/teams"), z.array(teamListSchema));
}

export async function fetchTeamOptions() {
  return readData(await getJson("/api/teams/options"), z.object({
    teams: z.array(teamListSchema),
    people: z.array(personSchema),
  }));
}

export async function fetchTeam(id: string) {
  return readData(await getJson(`/api/teams/${id}`), teamDetailSchema);
}

export async function saveTeam(team: TeamWrite, id?: string) {
  return readData(await sendJson(id ? `/api/teams/${id}` : "/api/teams", id ? "PUT" : "POST", team), teamDetailSchema);
}

export async function deleteTeam(id: string) {
  await sendJson(`/api/teams/${id}`, "DELETE");
}

export async function addTeamMember(teamId: string, userId: string, role: string) {
  return readData(await sendJson(`/api/teams/${teamId}/members`, "POST", { userId, role }), teamDetailSchema);
}

export async function removeTeamMember(teamId: string, userId: string) {
  return readData(await sendJson(`/api/teams/${teamId}/members/${userId}`, "DELETE"), teamDetailSchema);
}

export async function fetchMembers(query = "") {
  const path = query ? `/api/members?q=${encodeURIComponent(query)}` : "/api/members";
  return readData(await getJson(path), z.array(memberListSchema));
}

export async function fetchMember(id: string) {
  return readData(await getJson(`/api/members/${id}`), memberProfileSchema);
}

export async function saveMember(id: string, member: MemberWrite) {
  return readData(await sendJson(`/api/members/${id}`, "PUT", member), memberProfileSchema);
}

export async function fetchMemberProjects(id: string) {
  return readData(await getJson(`/api/members/${id}/projects`), z.array(projectSchema));
}

export async function fetchMemberStandups(id: string) {
  return readData(await getJson(`/api/members/${id}/standups`), z.array(standupSchema));
}

export async function fetchMemberActivity(id: string) {
  return readData(await getJson(`/api/members/${id}/activity`), z.array(activitySchema));
}

export async function fetchTeamWorkload(teamId: string) {
  return readData(await getJson(`/api/teams/workload?teamId=${teamId}`), z.array(workloadSchema));
}

export type MemberProject = z.infer<typeof projectSchema>;
export type MemberStandup = z.infer<typeof standupSchema>;
export type MemberActivity = z.infer<typeof activitySchema>;
