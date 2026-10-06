export const chartColors = {
  accent: "var(--wx-accent)",
  signal: "var(--wx-signal)",
  warning: "var(--wx-warning)",
  danger: "var(--wx-danger)",
  muted: "var(--wx-muted)",
} as const;

export const axisTick = { fill: "var(--wx-muted)", fontSize: 12 };

export const axisLine = { stroke: "var(--wx-line)" };

export const gridStroke = "var(--wx-line)";

export const tooltipStyle = {
  cursor: { fill: "var(--wx-surface-muted)" },
  contentStyle: {
    background: "var(--wx-surface)",
    border: "1px solid var(--wx-line)",
    borderRadius: 8,
    boxShadow: "var(--wx-shadow-rest)",
    fontSize: 12,
  },
  labelStyle: { color: "var(--wx-ink)", fontWeight: 600, marginBottom: 4 },
  itemStyle: { color: "var(--wx-ink)", padding: 0 },
};

export const reducedMotion = typeof window !== "undefined" && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
