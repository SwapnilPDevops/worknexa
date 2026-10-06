import { z } from "zod";

export const userSessionSchema = z.object({
  id: z.string(),
  email: z.string(),
  displayName: z.string(),
  roles: z.array(z.string()),
  permissions: z.array(z.string()),
});

export type UserSession = z.infer<typeof userSessionSchema>;

export const loginResponseSchema = z.object({
  accessToken: z.string(),
  expiresAt: z.string(),
  user: userSessionSchema,
});

export function readData<T>(value: unknown, schema: z.ZodType<T>) {
  return z.object({ data: schema }).parse(value).data;
}
