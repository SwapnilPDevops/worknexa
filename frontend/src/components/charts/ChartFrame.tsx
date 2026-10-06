import type { ReactNode } from "react";
import { ResponsiveContainer } from "recharts";

type ChartFrameProps = {
  children: ReactNode;
  height?: number;
};

export function ChartFrame({ children, height = 256 }: ChartFrameProps) {
  return (
    <div className="w-full" style={{ height }}>
      <ResponsiveContainer width="100%" height="100%">
        {children}
      </ResponsiveContainer>
    </div>
  );
}
