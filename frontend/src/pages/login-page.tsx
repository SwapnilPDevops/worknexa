import { useEffect, useState } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { z } from "zod";
import { useAuth } from "../auth/auth-context.tsx";
import { Button } from "../components/design-system/button.tsx";
import { Field, Input } from "../components/design-system/field.tsx";
import { LoadingState } from "../components/design-system/states.tsx";
import { useZodForm } from "../hooks/use-zod-form.ts";
import { ApiError } from "../services/api-client.ts";

const schema = z.object({
  email: z.string().trim().email("Enter a valid email."),
  password: z.string().min(1, "Enter your password."),
});

export function LoginPage() {
  const { status, user, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [error, setError] = useState<string | null>(null);
  const form = useZodForm(schema, { defaultValues: { email: "", password: "" } });

  useEffect(() => {
    document.title = "Sign in · Worknexa";
  }, []);
  const from = (location.state as { from?: string } | null)?.from;

  if (status === "loading") {
    return (
      <main className="mx-auto max-w-md px-6 py-16">
        <LoadingState label="Checking your session" />
      </main>
    );
  }

  if (user) {
    return <Navigate to={from && from !== "/login" ? from : "/"} replace />;
  }

  return (
    <main className="flex min-h-dvh items-center justify-center bg-canvas px-4 py-10 text-ink">
      <div className="w-full max-w-md rounded-xl border border-line bg-surface p-6 shadow-[var(--wx-shadow-rest)]">
        <div className="mb-6 flex items-center gap-3">
          <img src="/brand/worknexa-mark.svg" alt="" width={32} height={32} className="size-8" />
          <div>
            <h1 className="text-lg font-semibold tracking-tight">Worknexa</h1>
            <p className="text-xs text-muted">Your Work. One Intelligent Place.</p>
          </div>
        </div>
        <form
          className="space-y-4"
          noValidate
          onSubmit={form.handleSubmit(async (values) => {
            setError(null);
            try {
              await login(values.email, values.password);
              navigate(from && from !== "/login" ? from : "/", { replace: true });
            } catch (caught) {
              setError(caught instanceof ApiError ? caught.message : "Sign-in failed.");
            }
          })}
        >
          <Field label="Email" error={form.formState.errors.email?.message}>
            {({ id, describedBy }) => (
              <Input id={id} type="email" autoComplete="username" aria-describedby={describedBy} {...form.register("email")} />
            )}
          </Field>
          <Field label="Password" error={form.formState.errors.password?.message}>
            {({ id, describedBy }) => (
              <Input id={id} type="password" autoComplete="current-password" aria-describedby={describedBy} {...form.register("password")} />
            )}
          </Field>
          {error ? (
            <p role="alert" className="rounded-lg bg-danger-soft px-3 py-2 text-sm text-danger">
              {error}
            </p>
          ) : null}
          <Button type="submit" className="w-full" disabled={form.formState.isSubmitting}>
            {form.formState.isSubmitting ? "Signing in" : "Sign in"}
          </Button>
        </form>
      </div>
    </main>
  );
}
