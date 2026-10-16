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
    <div className="w-full flex flex-col gap-4">
      <div>
        <h1 className="text-3xl sm:text-[36px] font-semibold text-[#111827]">Settings</h1>
        <p className="mt-1.5 text-sm leading-[20px] text-[#4b5563]">Appearance for this browser. Account settings arrive with sign-in.</p>
      </div>
      <Card>
        <CardHeader title="Theme" description="Light, dark, or match the system." />
        <CardBody className="grid gap-2.5">
          {options.map((option) => (
            <label key={option.id} className="flex cursor-pointer items-start gap-3 rounded-[8px] border border-[#d1ccc2] bg-[#ebe8e2] px-3.5 py-3 has-checked:border-[#111827] has-checked:bg-[#fed7aa]/35 transition-colors">
              <input
                type="radio"
                name="theme"
                className="mt-1 accent-[#111827]"
                checked={preference === option.id}
                onChange={() => setPreference(option.id)}
              />
              <span>
                <span className="block text-sm font-medium text-[#111827]">{option.label}</span>
                <span className="block text-xs text-[#4b5563]">{option.detail}</span>
              </span>
            </label>
          ))}
        </CardBody>
      </Card>
      <IntegrationsPanel />
      <Card>
        <CardHeader title="Design system" description="Reusable components used by the shell." />
        <CardBody>
          <Link to="/foundations" className="text-sm font-medium text-[#111827] underline decoration-[#fed7aa] underline-offset-4 hover:decoration-[#111827]">
            Open the component library
          </Link>
        </CardBody>
      </Card>
    </div>
  );
}
