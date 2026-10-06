import { cn } from "../../utils/cn.ts";

type AvatarProps = {
  name: string;
  src?: string;
  size?: "sm" | "md";
};

function initials(name: string) {
  return name
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");
}

export function Avatar({ name, src, size = "md" }: AvatarProps) {
  const dimension = size === "sm" ? "size-7 text-[11px]" : "size-8 text-xs";
  if (src) {
    return <img src={src} alt="" className={cn("rounded-full object-cover", dimension)} />;
  }
  return (
    <span className={cn("inline-flex items-center justify-center rounded-full bg-accent-soft font-semibold text-accent", dimension)} aria-hidden="true">
      {initials(name)}
    </span>
  );
}

export function AvatarGroup({ names }: { names: string[] }) {
  return (
    <div className="flex -space-x-2" aria-label={names.join(", ")}>
      {names.map((name) => (
        <span key={name} className="rounded-full ring-2 ring-surface">
          <Avatar name={name} size="sm" />
        </span>
      ))}
    </div>
  );
}
