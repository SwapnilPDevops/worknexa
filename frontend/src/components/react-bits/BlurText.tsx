import { useEffect, useState } from "react";

interface BlurTextProps {
  text: string;
  delay?: number;
  className?: string;
  animateBy?: "words" | "letters";
  direction?: "top" | "bottom";
}

export function BlurText({
  text = "",
  delay = 50,
  className = "",
  animateBy = "words",
  direction = "top",
}: BlurTextProps) {
  const elements = animateBy === "words" ? text.split(" ") : text.split("");
  const [inView, setInView] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => {
      setInView(true);
    }, 50);
    return () => clearTimeout(timer);
  }, []);

  return (
    <span className={`inline-flex flex-wrap ${className}`}>
      {elements.map((segment, index) => (
        <span
          key={index}
          style={{
            transition: `all 0.5s ease-out ${index * delay}ms`,
            filter: inView ? "blur(0px)" : "blur(8px)",
            opacity: inView ? 1 : 0,
            transform: inView
              ? "translate3d(0, 0, 0)"
              : `translate3d(0, ${direction === "top" ? "-12px" : "12px"}, 0)`,
          }}
          className="inline-block"
        >
          {segment}
          {animateBy === "words" && index < elements.length - 1 ? "\u00A0" : ""}
        </span>
      ))}
    </span>
  );
}
