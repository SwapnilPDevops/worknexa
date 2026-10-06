import { format, parseISO } from "date-fns";

export function formatTimestamp(value: string): string {
  return format(parseISO(value), "dd MMM yyyy");
}
