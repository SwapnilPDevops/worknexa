type ProgressProps = {
  value: number;
  label: string;
};

export function Progress({ value, label }: ProgressProps) {
  const bounded = Math.min(100, Math.max(0, value));
  return (
    <div>
      <div className="mb-1.5 flex items-center justify-between text-xs text-muted">
        <span>{label}</span>
        <span>{bounded}%</span>
      </div>
      <div className="h-1.5 overflow-hidden rounded-full bg-surface-muted" role="progressbar" aria-valuenow={bounded} aria-valuemin={0} aria-valuemax={100} aria-label={label}>
        <div className="h-full rounded-full bg-accent transition-[width] duration-300" style={{ width: `${bounded}%` }} />
      </div>
    </div>
  );
}
