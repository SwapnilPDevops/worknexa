const prefix = "worknexa.draft.";

export function readDraft<T>(key: string): T | null {
  try {
    const raw = localStorage.getItem(prefix + key);
    if (!raw) {
      return null;
    }

    const parsed = JSON.parse(raw) as { value?: T };
    return parsed.value ?? null;
  } catch {
    return null;
  }
}

export function writeDraft<T>(key: string, value: T) {
  localStorage.setItem(prefix + key, JSON.stringify({ savedAt: Date.now(), value }));
}

export function clearDraft(key: string) {
  localStorage.removeItem(prefix + key);
}

/** Drafts can hold private note text, so they must not outlive the session on a shared browser. */
export function clearAllDrafts() {
  try {
    const keys = Array.from({ length: localStorage.length }, (_, index) => localStorage.key(index));
    keys.filter((key): key is string => key?.startsWith(prefix) ?? false).forEach((key) => localStorage.removeItem(key));
  } catch {
    // Storage unavailable: nothing was persisted.
  }
}
