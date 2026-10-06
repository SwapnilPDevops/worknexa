import { z } from "zod";
import { readData } from "../../auth/session.ts";
import { getJson, sendJson } from "../../services/api-client.ts";

const leaveSchema = z.object({
  id: z.string(),
  employeeId: z.string(),
  employeeName: z.string(),
  leaveTypeId: z.string(),
  leaveType: z.string(),
  startDate: z.string(),
  endDate: z.string(),
  days: z.number(),
  reason: z.string().nullable(),
  status: z.string(),
  approvedById: z.string().nullable(),
  approvedByName: z.string().nullable(),
});

const optionsSchema = z.object({
  leaveTypes: z.array(z.object({ id: z.string(), name: z.string(), isPaid: z.boolean() })),
  people: z.array(z.object({ id: z.string(), name: z.string() })),
});

const availabilitySchema = z.object({
  userId: z.string(),
  name: z.string(),
  availability: z.string(),
  leaves: z.array(z.object({
    leaveType: z.string(),
    startDate: z.string(),
    endDate: z.string(),
    status: z.string(),
  })),
});

export type LeaveItem = z.infer<typeof leaveSchema>;
export type LeaveOptions = z.infer<typeof optionsSchema>;

export type LeaveWrite = {
  employeeId: string;
  leaveTypeId: string;
  startDate: string;
  endDate: string;
  reason: string | null;
};

export async function fetchLeaves(query = "") {
  return readData(await getJson(`/api/leaves${query}`), z.array(leaveSchema));
}

export async function fetchLeaveOptions() {
  return readData(await getJson("/api/leaves/options"), optionsSchema);
}

export async function saveLeave(leave: LeaveWrite, id?: string) {
  return readData(await sendJson(id ? `/api/leaves/${id}` : "/api/leaves", id ? "PUT" : "POST", leave), leaveSchema);
}

export async function decideLeave(id: string, status: string) {
  return readData(await sendJson(`/api/leaves/${id}/decision`, "POST", { status }), leaveSchema);
}

export async function fetchAvailability(userId: string, on: string) {
  const params = new URLSearchParams({ userId, on });
  return readData(await getJson(`/api/leaves/availability?${params}`), z.object({ warning: z.string().nullable() }));
}

export async function fetchTeamAvailability(from: string, to: string, teamId = "") {
  const params = new URLSearchParams({ from, to });
  if (teamId) {
    params.set("teamId", teamId);
  }

  return readData(await getJson(`/api/leaves/availability/team?${params}`), z.array(availabilitySchema));
}
