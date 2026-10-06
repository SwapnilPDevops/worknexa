/** Returns the link only when it is an absolute http(s) URL, so stored or synced data cannot inject javascript: or data: links. */
export function safeHref(value: string | null | undefined): string | undefined {
  if (!value) {
    return undefined;
  }

  try {
    const url = new URL(value.trim());
    return url.protocol === "https:" || url.protocol === "http:" ? url.href : undefined;
  } catch {
    return undefined;
  }
}
