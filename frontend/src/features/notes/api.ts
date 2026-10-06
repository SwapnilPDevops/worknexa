import { z } from "zod";
import { readData } from "../../auth/session.ts";
import { getJson, sendJson } from "../../services/api-client.ts";

const namedRefSchema = z.object({ id: z.string(), name: z.string() });

export const noteSchema = z.object({
  id: z.string(),
  title: z.string(),
  content: z.string(),
  category: z.string().nullable(),
  isFavorite: z.boolean(),
  isPinned: z.boolean(),
  projectId: z.string().nullable(),
  projectName: z.string().nullable(),
  taskId: z.string().nullable(),
  taskTitle: z.string().nullable(),
  meetingId: z.string().nullable(),
  meetingTitle: z.string().nullable(),
  learningTopicId: z.string().nullable(),
  learningTopicTitle: z.string().nullable(),
  tags: z.array(z.string()),
  createdAt: z.string(),
  updatedAt: z.string(),
});

export type NoteItem = z.infer<typeof noteSchema>;

export const noteOptionsSchema = z.object({
  projects: z.array(namedRefSchema),
  tasks: z.array(namedRefSchema),
  meetings: z.array(namedRefSchema),
  learningTopics: z.array(namedRefSchema),
  tags: z.array(z.string()),
});

export type NoteOptions = z.infer<typeof noteOptionsSchema>;

export type NoteWrite = {
  title: string;
  content: string;
  category: string | null;
  isFavorite: boolean;
  isPinned: boolean;
  projectId: string | null;
  taskId: string | null;
  meetingId: string | null;
  learningTopicId: string | null;
  tags: string[];
};

export async function fetchNoteOptions() {
  return readData(await getJson("/api/notes/options"), noteOptionsSchema);
}

export async function fetchNotes(query: string, filter?: { learningTopicId?: string; learningOnly?: boolean; favorites?: boolean }) {
  const params = new URLSearchParams();
  if (query) {
    params.set("q", query);
  }

  if (filter?.learningTopicId) {
    params.set("learningTopicId", filter.learningTopicId);
  }

  if (filter?.learningOnly) {
    params.set("learningOnly", "true");
  }

  if (filter?.favorites) {
    params.set("favorites", "true");
  }

  return readData(await getJson(`/api/notes?${params}`), z.array(noteSchema));
}

export async function fetchNote(id: string) {
  return readData(await getJson(`/api/notes/${id}`), noteSchema);
}

export async function saveNote(note: NoteWrite, id?: string) {
  const path = id ? `/api/notes/${id}` : "/api/notes";
  return readData(await sendJson(path, id ? "PUT" : "POST", note), noteSchema);
}

export async function deleteNote(id: string) {
  await sendJson(`/api/notes/${id}`, "DELETE");
}
