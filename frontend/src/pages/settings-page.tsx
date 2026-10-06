import { useEffect } from "react";
import { Link } from "react-router-dom";
import { useTheme, type ThemePreference } from "../app/theme.tsx";
import { Card, CardBody, CardHeader } from "../components/design-system/card.tsx";
import { IntegrationsPanel } from "../features/integrations/integrations-panel.tsx";

const options: { id: ThemePreference; label: string; detail: string }[] = [
  { id: "light", label: "Light", detail: "Bright surfaces for daytime work." },
  { id: "dark", label: "Dark", detail: "Dim surfaces for low light." },
  { id: "system", label: "System", detail: "Follow this device." },
];

export function SettingsPage() {
  const { preference, setPreference } = useTheme();

  useEffect(() => {
    document.title = "Settings · Worknexa";
  }, []);

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4">
      <div>
        <h1 className="text-xl font-semibold tracking-tight">Settings</h1>
        <p className="mt-1 text-sm text-muted">Appearance for this browser. Account settings arrive with sign-in.</p>
      </div>
      <Card>
        <CardHeader title="Theme" description="Light, dark, or match the system." />
        <CardBody className="grid gap-2">
          {options.map((option) => (
            <label key={option.id} className="flex cursor-pointer items-start gap-3 rounded-lg border border-line px-3 py-3 has-checked:border-accent has-checked:bg-accent-soft">
              <input
                type="radio"
                name="theme"
                className="mt-1"
                checked={preference === option.id}
                onChange={() => setPreference(option.id)}
              />
              <span>
                <span className="block text-sm font-medium">{option.label}</span>
                <span className="block text-xs text-muted">{option.detail}</span>
              </span>
            </label>
          ))}
        </CardBody>
      </Card>
      <IntegrationsPanel />
      <Card>
        <CardHeader title="Design system" description="Reusable components used by the shell." />
        <CardBody>
          <Link to="/foundations" className="text-sm font-medium text-accent">
            Open the component library
          </Link>
        </CardBody>
      </Card>
    </div>
  );
}
