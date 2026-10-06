import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { format, parseISO } from "date-fns";
import { useEffect, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { can, permissions } from "../../auth/access.ts";
import { useAuth } from "../../auth/auth-context.tsx";
import { Badge } from "../../components/design-system/badge.tsx";
import { Button } from "../../components/design-system/button.tsx";
import { Card, CardBody, CardHeader } from "../../components/design-system/card.tsx";
import { Drawer } from "../../components/design-system/drawer.tsx";
import { Field, Input, Select, Textarea } from "../../components/design-system/field.tsx";
import { EmptyState, ErrorState, LoadingState } from "../../components/design-system/states.tsx";
import { Tabs } from "../../components/design-system/tabs.tsx";
import { ApiError } from "../../services/api-client.ts";
import { fetchTasks, localOffsetMinutes } from "../work/api.ts";
import { labelFor as taskLabel } from "../work/catalog.ts";
import { fetchLeaves } from "../leaves/api.ts";
import { labelFor as leaveLabel } from "../leaves/catalog.ts";
import {
  addTeamMember,
  deleteTeam,
  fetchMember,
  fetchMemberActivity,
  fetchMemberProjects,
  fetchMemberStandups,
  fetchMembers,
  fetchTeam,
  fetchTeamOptions,
  fetchTeams,
  removeTeamMember,
  saveMember,
  saveTeam,
  type MemberProfile,
  type MemberWrite,
} from "./api.ts";
import { labelFor, memberStatuses, memberTabs, statusTone, teamRoles, type MemberTab } from "./catalog.ts";

type TeamDraft = { name: string; description: string };
type ProfileDraft = {
  name: string;
  email: string;
  phone: string;
  photoUrl: string;
  designation: string;
  department: string;
  joinedOn: string;
  managerId: string;
  status: string;
  notes: string;
  skills: string[];
  technologies: string[];
};

export function TeamView() {
  const { user } = useAuth();
  const manageable = can(user?.permissions, permissions.teamsManage);
  const [params, setParams] = useSearchParams();
  const memberId = params.get("member");
  const teamId = params.get("team");
  const [tab, setTab] = useState<MemberTab>("details");
  const [query, setQuery] = useState("");
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState(false);
  const [teamDraft, setTeamDraft] = useState<TeamDraft>({ name: "", description: "" });
  const [profileDraft, setProfileDraft] = useState<ProfileDraft | null>(null);
  const [memberUserId, setMemberUserId] = useState("");
  const [memberRole, setMemberRole] = useState("Member");
  const [skill, setSkill] = useState("");
  const [technology, setTechnology] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const queryClient = useQueryClient();

  const teams = useQuery({ queryKey: ["teams"], queryFn: fetchTeams });
  const options = useQuery({ queryKey: ["team-options"], queryFn: fetchTeamOptions });
  const people = useQuery({ queryKey: ["members", query], queryFn: () => fetchMembers(query) });
  const team = useQuery({ queryKey: ["team", teamId], queryFn: () => fetchTeam(teamId!), enabled: Boolean(teamId) });
  const profile = useQuery({ queryKey: ["member", memberId], queryFn: () => fetchMember(memberId!), enabled: Boolean(memberId) });
  const tasks = useQuery({
    queryKey: ["member-tasks", memberId],
    queryFn: () => fetchTasks(`?view=list&assigneeId=${memberId}&date=${format(new Date(), "yyyy-MM-dd")}&offsetMinutes=${localOffsetMinutes()}`),
    enabled: Boolean(memberId) && tab === "tasks",
  });
  const projects = useQuery({ queryKey: ["member-projects", memberId], queryFn: () => fetchMemberProjects(memberId!), enabled: Boolean(memberId) && tab === "projects" });
  const leaves = useQuery({ queryKey: ["member-leaves", memberId], queryFn: () => fetchLeaves(`?employeeId=${memberId}`), enabled: Boolean(memberId) && tab === "leaves" });
  const standups = useQuery({ queryKey: ["member-standups", memberId], queryFn: () => fetchMemberStandups(memberId!), enabled: Boolean(memberId) && tab === "standups" });
  const activity = useQuery({ queryKey: ["member-activity", memberId], queryFn: () => fetchMemberActivity(memberId!), enabled: Boolean(memberId) && tab === "activity" });

  useEffect(() => {
    document.title = "Team · Worknexa";
  }, []);

  useEffect(() => {
    if (profile.data) {
      setProfileDraft(draftFrom(profile.data));
    }
  }, [profile.data]);

  function refresh() {
    return Promise.all([
      queryClient.invalidateQueries({ queryKey: ["teams"] }),
      queryClient.invalidateQueries({ queryKey: ["team"] }),
      queryClient.invalidateQueries({ queryKey: ["team-options"] }),
      queryClient.invalidateQueries({ queryKey: ["members"] }),
      queryClient.invalidateQueries({ queryKey: ["member"] }),
      queryClient.invalidateQueries({ queryKey: ["member-activity"] }),
    ]);
  }

  const saveTeamMutation = useMutation({
    mutationFn: () => saveTeam({ name: teamDraft.name, description: teamDraft.description || null }, creating ? undefined : teamId ?? undefined),
    onSuccess: async (saved) => {
      setCreating(false);
      setEditing(false);
      setMessage(null);
      setParams({ team: saved.id });
      await refresh();
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The team could not be saved."),
  });

  const saveProfile = useMutation({
    mutationFn: () => saveMember(memberId!, toWrite(profileDraft!)),
    onSuccess: async () => {
      setMessage(null);
      await refresh();
    },
    onError: (error) => setMessage(error instanceof ApiError ? error.message : "The profile could not be saved."),
  });

  if (!can(user?.permissions, permissions.teamsRead)) {
    return <ErrorState title="Access denied" description="Your account does not include teams." />;
  }

  if (teams.isPending || people.isPending) {
    return <LoadingState label="Loading the team" />;
  }

  if (teams.isError || people.isError) {
    return <ErrorState title="The team is unavailable" description="People and teams could not be loaded." onRetry={() => { void teams.refetch(); void people.refetch(); }} />;
  }

  return (
    <div className="mx-auto max-w-6xl space-y-5">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-ink">Team</h1>
          <p className="mt-1 text-sm text-muted">People you work with, and the groups they belong to.</p>
        </div>
        {manageable ? <Button onClick={() => { setEditing(false); setCreating(true); setTeamDraft({ name: "", description: "" }); setMessage(null); }}>New team</Button> : null}
      </header>

      <div className="grid gap-4 lg:grid-cols-[16rem_1fr]">
        <div className="space-y-4">
          <Card>
            <CardHeader title="Teams" />
            <CardBody className="space-y-1">
              {teams.data.length === 0 ? <p className="text-sm text-muted">No teams yet.</p> : teams.data.map((item) => (
                <button key={item.id} type="button" className={teamId === item.id ? "block w-full rounded-lg bg-surface-muted px-2 py-1.5 text-left text-sm font-medium text-ink" : "block w-full rounded-lg px-2 py-1.5 text-left text-sm text-ink hover:bg-surface-muted"} onClick={() => setParams({ team: item.id })}>
                  {item.name}
                  <span className="mt-0.5 block text-xs text-muted">{item.memberCount} {item.memberCount === 1 ? "person" : "people"}</span>
                </button>
              ))}
            </CardBody>
          </Card>
          <Card>
            <CardHeader title="People" />
            <CardBody className="space-y-2">
              <Input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search name or email" aria-label="Search people" />
              {people.data.length === 0 ? <p className="text-sm text-muted">No matching people.</p> : people.data.map((person) => (
                <button key={person.id} type="button" className={memberId === person.id ? "flex w-full items-center gap-2 rounded-lg bg-surface-muted px-2 py-1.5 text-left" : "flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left hover:bg-surface-muted"} onClick={() => { setTab("details"); setParams({ member: person.id }); }}>
                  <Avatar name={person.name} photoUrl={person.photoUrl} />
                  <span>
                    <span className="block text-sm font-medium text-ink">{person.name}</span>
                    <span className="block text-xs text-muted">{person.designation ?? person.email}</span>
                  </span>
                </button>
              ))}
            </CardBody>
          </Card>
        </div>

        <div className="space-y-4">
          {team.data ? (
            <Card>
              <CardHeader title={team.data.name} description={team.data.description ?? undefined} action={manageable ? <Button size="sm" variant="secondary" onClick={() => { setCreating(false); setEditing(true); setTeamDraft({ name: team.data.name, description: team.data.description ?? "" }); setMessage(null); }}>Edit</Button> : undefined} />
              <CardBody className="space-y-3">
                {team.data.members.map((member) => (
                  <div key={member.userId} className="flex items-center justify-between gap-3">
                    <button type="button" className="text-left" onClick={() => setParams({ member: member.userId })}>
                      <span className="block text-sm font-medium text-ink">{member.name}</span>
                      <span className="block text-xs text-muted">{member.designation ?? member.email}</span>
                    </button>
                    <span className="flex items-center gap-2">
                      <Badge tone={statusTone(member.status)}>{labelFor(member.status)}</Badge>
                      <Badge>{member.role}</Badge>
                      {manageable ? <Button size="sm" variant="ghost" onClick={() => void removeTeamMember(team.data.id, member.userId).then(() => refresh())}>Remove</Button> : null}
                    </span>
                  </div>
                ))}
                {manageable ? (
                  <form className="flex flex-wrap items-end gap-2" onSubmit={(event) => { event.preventDefault(); void addTeamMember(team.data.id, memberUserId, memberRole).then(() => refresh()); }}>
                    <Field label="Person">{({ id }) => <Select id={id} value={memberUserId} onChange={(event) => setMemberUserId(event.target.value)} required><option value="">Choose</option>{options.data?.people.map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}</Select>}</Field>
                    <Field label="Role">{({ id }) => <Select id={id} value={memberRole} onChange={(event) => setMemberRole(event.target.value)}>{teamRoles.map((role) => <option key={role} value={role}>{role}</option>)}</Select>}</Field>
                    <Button type="submit" size="sm">Add</Button>
                  </form>
                ) : null}
              </CardBody>
            </Card>
          ) : null}

          {memberId && profile.isPending ? <LoadingState label="Loading profile" /> : null}
          {profile.isError ? <ErrorState title="Profile unavailable" description={profile.error instanceof ApiError ? profile.error.message : "This person could not be loaded."} /> : null}
          {profile.data && profileDraft ? (
            <Card>
              <CardHeader title={profile.data.name} description={profile.data.designation ?? profile.data.email} action={<Badge tone={statusTone(profile.data.status)}>{labelFor(profile.data.status)}</Badge>} />
              <CardBody className="space-y-4">
                <div className="flex items-center gap-3">
                  <Avatar name={profile.data.name} photoUrl={profile.data.photoUrl} large />
                  <div className="text-sm text-muted">
                    <p>{profile.data.email}</p>
                    <p>{profile.data.department ?? "No department"}{profile.data.managerName ? ` · Manager ${profile.data.managerName}` : ""}</p>
                  </div>
                </div>
                <Tabs tabs={[...memberTabs]} value={tab} onChange={(id) => setTab(id as MemberTab)} label="Member profile" />
                {tab === "details" || tab === "skills" ? (
                  <form className="space-y-4" onSubmit={(event: FormEvent) => { event.preventDefault(); void saveProfile.mutateAsync(); }}>
                    {message ? <p className="text-sm text-danger">{message}</p> : null}
                    {tab === "details" ? (
                      <div className="grid gap-3 sm:grid-cols-2">
                        <Field label="Name">{({ id }) => <Input id={id} value={profileDraft.name} onChange={(event) => setProfileDraft({ ...profileDraft, name: event.target.value })} required disabled={!manageable} />}</Field>
                        <Field label="Email">{({ id }) => <Input id={id} type="email" value={profileDraft.email} onChange={(event) => setProfileDraft({ ...profileDraft, email: event.target.value })} required disabled={!manageable} />}</Field>
                        <Field label="Phone">{({ id }) => <Input id={id} value={profileDraft.phone} onChange={(event) => setProfileDraft({ ...profileDraft, phone: event.target.value })} disabled={!manageable} />}</Field>
                        <Field label="Photo">{({ id }) => <Input id={id} value={profileDraft.photoUrl} onChange={(event) => setProfileDraft({ ...profileDraft, photoUrl: event.target.value })} placeholder="https://" disabled={!manageable} />}</Field>
                        <Field label="Designation">{({ id }) => <Input id={id} value={profileDraft.designation} onChange={(event) => setProfileDraft({ ...profileDraft, designation: event.target.value })} disabled={!manageable} />}</Field>
                        <Field label="Department">{({ id }) => <Input id={id} value={profileDraft.department} onChange={(event) => setProfileDraft({ ...profileDraft, department: event.target.value })} disabled={!manageable} />}</Field>
                        <Field label="Joining date">{({ id }) => <Input id={id} type="date" value={profileDraft.joinedOn} onChange={(event) => setProfileDraft({ ...profileDraft, joinedOn: event.target.value })} disabled={!manageable} />}</Field>
                        <Field label="Manager">{({ id }) => <Select id={id} value={profileDraft.managerId} onChange={(event) => setProfileDraft({ ...profileDraft, managerId: event.target.value })} disabled={!manageable}><option value="">None</option>{options.data?.people.filter((person) => person.id !== profile.data.id).map((person) => <option key={person.id} value={person.id}>{person.name}</option>)}</Select>}</Field>
                        <Field label="Status">{({ id }) => <Select id={id} value={profileDraft.status} onChange={(event) => setProfileDraft({ ...profileDraft, status: event.target.value })} disabled={!manageable}>{memberStatuses.map((status) => <option key={status} value={status}>{labelFor(status)}</option>)}</Select>}</Field>
                        <div className="sm:col-span-2">
                          <Field label="Notes">{({ id }) => <Textarea id={id} value={profileDraft.notes} onChange={(event) => setProfileDraft({ ...profileDraft, notes: event.target.value })} disabled={!manageable} />}</Field>
                        </div>
                      </div>
                    ) : (
                      <div className="grid gap-4 sm:grid-cols-2">
                        <NameList label="Skills" values={profileDraft.skills} draft={skill} onDraft={setSkill} onAdd={() => { if (skill.trim()) { setProfileDraft({ ...profileDraft, skills: [...profileDraft.skills, skill.trim()] }); setSkill(""); } }} onRemove={(name) => setProfileDraft({ ...profileDraft, skills: profileDraft.skills.filter((item) => item !== name) })} disabled={!manageable} />
                        <NameList label="Technologies" values={profileDraft.technologies} draft={technology} onDraft={setTechnology} onAdd={() => { if (technology.trim()) { setProfileDraft({ ...profileDraft, technologies: [...profileDraft.technologies, technology.trim()] }); setTechnology(""); } }} onRemove={(name) => setProfileDraft({ ...profileDraft, technologies: profileDraft.technologies.filter((item) => item !== name) })} disabled={!manageable} />
                      </div>
                    )}
                    {manageable ? <Button type="submit" disabled={saveProfile.isPending}>Save profile</Button> : null}
                  </form>
                ) : null}
                {tab === "tasks" ? <SimpleList rows={(tasks.data ?? []).map((task) => ({ id: task.id, title: task.title, meta: taskLabel(task.status), href: `/tasks?task=${task.id}` }))} empty="No tasks assigned." /> : null}
                {tab === "projects" ? <SimpleList rows={(projects.data ?? []).map((project) => ({ id: project.id, title: project.name, meta: `${project.code} · ${project.role}`, href: `/projects?project=${project.id}` }))} empty="No projects yet." /> : null}
                {tab === "leaves" ? <SimpleList rows={(leaves.data ?? []).map((leave) => ({ id: leave.id, title: `${leave.leaveType} · ${leave.days} days`, meta: `${leave.startDate} to ${leave.endDate} · ${leaveLabel(leave.status)}` }))} empty="No leave recorded." /> : null}
                {tab === "standups" ? <SimpleList rows={(standups.data ?? []).map((entry) => ({ id: entry.id, title: entry.standupDate, meta: [entry.teamName, entry.today].filter(Boolean).join(" · ") }))} empty="No standup entries." /> : null}
                {tab === "activity" ? <SimpleList rows={(activity.data ?? []).map((item, index) => ({ id: `${item.occurredAt}-${index}`, title: item.summary, meta: `${item.action} · ${format(parseISO(item.occurredAt), "d MMM yyyy HH:mm")}` }))} empty="No activity yet." /> : null}
              </CardBody>
            </Card>
          ) : !team.data ? <EmptyState title="Choose a person or a team" description="Profiles, tasks, and leave sit with the people already in the workspace." /> : null}
        </div>
      </div>

      <Drawer open={creating || editing} title={creating ? "New team" : "Edit team"} onClose={() => { setCreating(false); setEditing(false); }}>
        <form className="space-y-4" onSubmit={(event) => { event.preventDefault(); void saveTeamMutation.mutateAsync(); }}>
          {message ? <p className="text-sm text-danger">{message}</p> : null}
          <Field label="Name">{({ id }) => <Input id={id} value={teamDraft.name} onChange={(event) => setTeamDraft({ ...teamDraft, name: event.target.value })} required />}</Field>
          <Field label="Description">{({ id }) => <Textarea id={id} value={teamDraft.description} onChange={(event) => setTeamDraft({ ...teamDraft, description: event.target.value })} />}</Field>
          <div className="flex gap-2">
            <Button type="submit" disabled={saveTeamMutation.isPending}>{creating ? "Create team" : "Save team"}</Button>
            {!creating && teamId ? <Button type="button" variant="danger" onClick={() => void deleteTeam(teamId).then(() => { setParams({}); setCreating(false); setEditing(false); return refresh(); })}>Delete</Button> : null}
          </div>
        </form>
      </Drawer>
    </div>
  );
}

function draftFrom(profile: MemberProfile): ProfileDraft {
  return {
    name: profile.name,
    email: profile.email,
    phone: profile.phone ?? "",
    photoUrl: profile.photoUrl ?? "",
    designation: profile.designation ?? "",
    department: profile.department ?? "",
    joinedOn: profile.joinedOn ?? "",
    managerId: profile.managerId ?? "",
    status: profile.status,
    notes: profile.notes ?? "",
    skills: profile.skills,
    technologies: profile.technologies,
  };
}

function toWrite(draft: ProfileDraft): MemberWrite {
  return {
    name: draft.name,
    email: draft.email,
    phone: draft.phone || null,
    photoUrl: draft.photoUrl || null,
    designation: draft.designation || null,
    department: draft.department || null,
    joinedOn: draft.joinedOn || null,
    managerId: draft.managerId || null,
    status: draft.status,
    notes: draft.notes || null,
    skills: draft.skills,
    technologies: draft.technologies,
  };
}

function Avatar({ name, photoUrl, large = false }: { name: string; photoUrl: string | null; large?: boolean }) {
  const size = large ? "h-14 w-14 text-base" : "h-8 w-8 text-xs";
  if (photoUrl) {
    return <img src={photoUrl} alt="" className={`${size} rounded-full object-cover`} />;
  }

  const initials = name.split(" ").map((part) => part[0]).slice(0, 2).join("").toUpperCase();
  return <span className={`${size} inline-flex items-center justify-center rounded-full bg-accent-soft font-medium text-accent`}>{initials}</span>;
}

function NameList({ label, values, draft, onDraft, onAdd, onRemove, disabled }: { label: string; values: string[]; draft: string; onDraft: (value: string) => void; onAdd: () => void; onRemove: (name: string) => void; disabled: boolean }) {
  return (
    <div className="space-y-2">
      <p className="text-sm font-medium text-ink">{label}</p>
      <div className="flex flex-wrap gap-2">
        {values.length === 0 ? <span className="text-sm text-muted">None yet.</span> : values.map((name) => (
          <button key={name} type="button" className="rounded-md bg-surface-muted px-2 py-1 text-xs text-ink" onClick={() => onRemove(name)} disabled={disabled}>{name}</button>
        ))}
      </div>
      {disabled ? null : (
        <div className="flex gap-2">
          <Input value={draft} onChange={(event) => onDraft(event.target.value)} aria-label={`Add ${label.toLowerCase()}`} />
          <Button type="button" variant="secondary" onClick={onAdd}>Add</Button>
        </div>
      )}
    </div>
  );
}

function SimpleList({ rows, empty }: { rows: { id: string; title: string; meta: string; href?: string }[]; empty: string }) {
  if (rows.length === 0) {
    return <p className="text-sm text-muted">{empty}</p>;
  }

  return (
    <ul className="divide-y divide-line">
      {rows.map((row) => (
        <li key={row.id} className="py-2">
          {row.href ? <Link to={row.href} className="text-sm font-medium text-ink hover:underline">{row.title}</Link> : <p className="text-sm font-medium text-ink">{row.title}</p>}
          <p className="text-xs text-muted">{row.meta}</p>
        </li>
      ))}
    </ul>
  );
}
