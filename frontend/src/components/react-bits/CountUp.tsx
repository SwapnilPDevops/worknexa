import { useEffect, useRef, useState } from "react";

interface CountUpProps {
  to: number;
  from?: number;
  direction?: "up" | "down";
  delay?: number;
  duration?: number;
  className?: string;
  prefix?: string;
  suffix?: string;
  decimals?: number;
}

export function CountUp({
  to,
  from = 0,
  direction = "up",
  delay = 0,
  duration = 2,
  className = "",
  prefix = "",
  suffix = "",
  decimals = 0,
}: CountUpProps) {
  const [value, setValue] = useState(from);
  const startTimeRef = useRef<number | null>(null);
  const frameRef = useRef<number | null>(null);

  useEffect(() => {
    const startValue = direction === "down" ? to : from;
    const endValue = direction === "down" ? from : to;

    const timeout = setTimeout(() => {
      const step = (timestamp: number) => {
        if (!startTimeRef.current) startTimeRef.current = timestamp;
        const progress = Math.min((timestamp - startTimeRef.current) / (duration * 1000), 1);
        // easeOutExpo
        const ease = progress === 1 ? 1 : 1 - Math.pow(2, -10 * progress);
        const current = startValue + (endValue - startValue) * ease;
        setValue(current);

        if (progress < 1) {
          frameRef.current = requestAnimationFrame(step);
        }
      };

      frameRef.current = requestAnimationFrame(step);
    }, delay * 1000);

    return () => {
      clearTimeout(timeout);
      if (frameRef.current) cancelAnimationFrame(frameRef.current);
    };
  }, [from, to, direction, delay, duration]);

  const formatted = decimals > 0 ? value.toFixed(decimals) : Math.round(value).toString();

  return (
    <span className={className}>
      {prefix}
      {formatted}
      {suffix}
    </span>
  );
}
