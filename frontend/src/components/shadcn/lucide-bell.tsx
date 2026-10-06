import { Bell } from "lucide-react";

type LucideIconProps = {
  size?: number;
};

export function LucideBell({ size = 16 }: LucideIconProps) {
  return <Bell size={size} aria-hidden="true" />;
}
