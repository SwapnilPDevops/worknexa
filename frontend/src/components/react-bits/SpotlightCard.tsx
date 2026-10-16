import { useRef, useState, type MouseEvent, type ReactNode, type HTMLAttributes } from "react";
import { cn } from "../../utils/cn.ts";

interface SpotlightCardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  spotlightColor?: string;
  spotlightSize?: number;
  className?: string;
}

export function SpotlightCard({
  children,
  spotlightColor = "rgba(254, 215, 170, 0.22)",
  spotlightSize = 350,
  className,
  ...props
}: SpotlightCardProps) {
  const divRef = useRef<HTMLDivElement>(null);
  const [position, setPosition] = useState({ x: 0, y: 0 });
  const [opacity, setOpacity] = useState(0);

  const handleMouseMove = (e: MouseEvent<HTMLDivElement>) => {
    if (!divRef.current) return;
    const rect = divRef.current.getBoundingClientRect();
    setPosition({ x: e.clientX - rect.left, y: e.clientY - rect.top });
  };

  const handleMouseEnter = () => {
    setOpacity(1);
  };

  const handleMouseLeave = () => {
    setOpacity(0);
  };

  return (
    <div
      ref={divRef}
      onMouseMove={handleMouseMove}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
      className={cn(
        "relative overflow-hidden rounded-[8px] border border-[#D1CCC2] bg-[#DFDCD6] transition-[border-color,box-shadow] duration-200 hover:border-[#111827]/25",
        className,
      )}
      {...props}
    >
      <div
        className="pointer-events-none absolute -inset-px transition-opacity duration-300"
        style={{
          opacity,
          background: `radial-gradient(${spotlightSize}px circle at ${position.x}px ${position.y}px, ${spotlightColor}, transparent 80%)`,
        }}
        aria-hidden="true"
      />
      <div className="relative z-10">{children}</div>
    </div>
  );
}
