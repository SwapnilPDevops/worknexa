import { useEffect, useState } from "react";
import { z } from "zod";
import { useAuth } from "../auth/auth-context.tsx";
import { Badge } from "../components/design-system/badge.tsx";
import { Button } from "../components/design-system/button.tsx";
import { Card, CardBody, CardHeader } from "../components/design-system/card.tsx";
import { Field, Input } from "../components/design-system/field.tsx";
import { useZodForm } from "../hooks/use-zod-form.ts";
import { ApiError } from "../services/api-client.ts";

const profileSchema = z.object({
  displayName: z.string().trim().min(1, "Enter a display name.").max(200),
});

const passwordSchema = z
  .object({
    currentPassword: z.string().min(1, "Enter your current password."),
    newPassword: z.string().min(8, "Use at least 8 characters.").regex(/[A-Za-z]/, "Include a letter.").regex(/[0-9]/, "Include a number."),
  })
  .refine((values) => values.currentPassword !== values.newPassword, {
    path: ["newPassword"],
    message: "Choose a different password.",
  });

export function ProfilePage() {
  const { user, updateProfile, changePassword } = useAuth();
  const [profileMessage, setProfileMessage] = useState<string | null>(null);
  const [passwordMessage, setPasswordMessage] = useState<string | null>(null);
  const [profileError, setProfileError] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const profileForm = useZodForm(profileSchema, { defaultValues: { displayName: user?.displayName ?? "" } });
  const passwordForm = useZodForm(passwordSchema, { defaultValues: { currentPassword: "", newPassword: "" } });

  useEffect(() => {
    document.title = "Profile · Worknexa";
  }, []);

  if (!user) {
    return null;
  }

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4">
      <div>
        <h1 className="text-xl font-semibold tracking-tight">Profile</h1>
        <p className="mt-1 text-sm text-muted">{user.email}</p>
        <div className="mt-3 flex flex-wrap gap-1.5">
          {user.roles.map((role) => (
            <Badge key={role} tone="accent">
              {role}
            </Badge>
          ))}
        </div>
      </div>
      <Card>
        <CardHeader title="Display name" description="This is how you appear in Worknexa." />
        <CardBody>
          <form
            className="space-y-4"
            noValidate
            onSubmit={profileForm.handleSubmit(async (values) => {
              setProfileError(null);
              setProfileMessage(null);
              try {
                await updateProfile(values.displayName);
                setProfileMessage("Display name saved.");
              } catch (caught) {
                setProfileError(caught instanceof ApiError ? caught.message : "Could not save your profile.");
              }
            })}
          >
            <Field label="Display name" error={profileForm.formState.errors.displayName?.message}>
              {({ id, describedBy }) => <Input id={id} aria-describedby={describedBy} {...profileForm.register("displayName")} />}
            </Field>
            {profileMessage ? <p role="status" className="text-sm text-muted">{profileMessage}</p> : null}
            {profileError ? <p role="alert" className="text-sm text-danger">{profileError}</p> : null}
            <Button type="submit" disabled={profileForm.formState.isSubmitting}>
              Save profile
            </Button>
          </form>
        </CardBody>
      </Card>
      <Card>
        <CardHeader title="Password" description="Use at least 8 characters, including a letter and a number." />
        <CardBody>
          <form
            className="space-y-4"
            noValidate
            onSubmit={passwordForm.handleSubmit(async (values) => {
              setPasswordError(null);
              setPasswordMessage(null);
              try {
                await changePassword(values.currentPassword, values.newPassword);
                passwordForm.reset();
                setPasswordMessage("Password updated.");
              } catch (caught) {
                setPasswordError(caught instanceof ApiError ? caught.message : "Could not change your password.");
              }
            })}
          >
            <Field label="Current password" error={passwordForm.formState.errors.currentPassword?.message}>
              {({ id, describedBy }) => (
                <Input id={id} type="password" autoComplete="current-password" aria-describedby={describedBy} {...passwordForm.register("currentPassword")} />
              )}
            </Field>
            <Field label="New password" error={passwordForm.formState.errors.newPassword?.message}>
              {({ id, describedBy }) => (
                <Input id={id} type="password" autoComplete="new-password" aria-describedby={describedBy} {...passwordForm.register("newPassword")} />
              )}
            </Field>
            {passwordMessage ? <p role="status" className="text-sm text-muted">{passwordMessage}</p> : null}
            {passwordError ? <p role="alert" className="text-sm text-danger">{passwordError}</p> : null}
            <Button type="submit" disabled={passwordForm.formState.isSubmitting}>
              Change password
            </Button>
          </form>
        </CardBody>
      </Card>
    </div>
  );
}
