const apiUrl = import.meta.env.VITE_API_URL ?? "";
const legacyTokenKey = "worknexa.access-token";

// The token stays in memory so scripts cannot read it from storage; reloads restore the session from the HttpOnly cookie.
let accessToken: string | null = null;

try {
  sessionStorage.removeItem(legacyTokenKey);
} catch {
  // Storage can be unavailable (privacy mode); nothing to clean up then.
}

export function setAccessToken(token: string | null) {
  accessToken = token;
}

export class ApiError extends Error {
  readonly status: number;

  readonly code: string;

  constructor(status: number, message: string, code = "UNEXPECTED_ERROR") {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
  }
}

export async function getJson(path: string): Promise<unknown> {
  const response = await request(path);
  if (!response.ok) {
    throw await toError(response);
  }

  return response.json() as Promise<unknown>;
}

export async function sendJson(path: string, method: string, body?: unknown): Promise<unknown> {
  const response = await request(path, {
    method,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!response.ok) {
    throw await toError(response);
  }

  if (response.status === 204) {
    return null;
  }

  return response.json() as Promise<unknown>;
}

export async function sendForm(path: string, form: FormData): Promise<unknown> {
  const response = await request(path, { method: "POST", body: form });
  if (!response.ok) {
    throw await toError(response);
  }

  return response.json() as Promise<unknown>;
}

export async function getBlob(path: string): Promise<Blob> {
  const response = await request(path);
  if (!response.ok) {
    throw await toError(response);
  }

  return response.blob();
}

async function request(path: string, init?: RequestInit) {
  const headers = new Headers(init?.headers);
  headers.set("X-Worknexa-Request", "1");
  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }
  if (typeof init?.body === "string" && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  return fetch(`${apiUrl}${path}`, {
    ...init,
    headers,
    credentials: "include",
  });
}

async function toError(response: Response) {
  const payload = await response.json().catch(() => null) as { code?: string; message?: string } | null;
  return new ApiError(response.status, payload?.message ?? `Request failed with status ${response.status}`, payload?.code);
}
