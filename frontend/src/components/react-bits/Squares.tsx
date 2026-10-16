import { useEffect, useRef, type CSSProperties } from "react";

interface SquaresProps {
  direction?: "diagonal" | "up" | "right" | "down" | "left";
  speed?: number;
  borderColor?: string;
  squareSize?: number;
  hoverFillColor?: string;
  className?: string;
  style?: CSSProperties;
}

export function Squares({
  direction = "right",
  speed = 0.5,
  borderColor = "rgba(17, 24, 39, 0.08)",
  squareSize = 40,
  hoverFillColor = "rgba(254, 215, 170, 0.25)",
  className,
  style,
}: SquaresProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const gridOffset = useRef({ x: 0, y: 0 });
  const hoveredSquare = useRef<{ x: number; y: number } | null>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    let animationFrameId: number;

    const resizeCanvas = () => {
      canvas.width = canvas.offsetWidth;
      canvas.height = canvas.offsetHeight;
    };

    window.addEventListener("resize", resizeCanvas);
    resizeCanvas();

    const draw = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);

      const startX = Math.floor(gridOffset.current.x / squareSize) * squareSize;
      const startY = Math.floor(gridOffset.current.y / squareSize) * squareSize;

      for (let x = startX; x < canvas.width + squareSize; x += squareSize) {
        for (let y = startY; y < canvas.height + squareSize; y += squareSize) {
          const squareX = x - (gridOffset.current.x % squareSize);
          const squareY = y - (gridOffset.current.y % squareSize);

          if (
            hoveredSquare.current &&
            hoveredSquare.current.x >= squareX &&
            hoveredSquare.current.x < squareX + squareSize &&
            hoveredSquare.current.y >= squareY &&
            hoveredSquare.current.y < squareY + squareSize
          ) {
            ctx.fillStyle = hoverFillColor;
            ctx.fillRect(squareX, squareY, squareSize, squareSize);
          }

          ctx.strokeStyle = borderColor;
          ctx.lineWidth = 1;
          ctx.strokeRect(squareX, squareY, squareSize, squareSize);
        }
      }

      switch (direction) {
        case "right":
          gridOffset.current.x -= speed;
          break;
        case "left":
          gridOffset.current.x += speed;
          break;
        case "up":
          gridOffset.current.y += speed;
          break;
        case "down":
          gridOffset.current.y -= speed;
          break;
        case "diagonal":
          gridOffset.current.x -= speed;
          gridOffset.current.y -= speed;
          break;
      }

      animationFrameId = requestAnimationFrame(draw);
    };

    draw();

    return () => {
      window.removeEventListener("resize", resizeCanvas);
      cancelAnimationFrame(animationFrameId);
    };
  }, [direction, speed, borderColor, hoverFillColor, squareSize]);

  const handleMouseMove = (event: React.MouseEvent<HTMLCanvasElement>) => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const rect = canvas.getBoundingClientRect();
    hoveredSquare.current = {
      x: event.clientX - rect.left,
      y: event.clientY - rect.top,
    };
  };

  const handleMouseLeave = () => {
    hoveredSquare.current = null;
  };

  return (
    <canvas
      ref={canvasRef}
      onMouseMove={handleMouseMove}
      onMouseLeave={handleMouseLeave}
      className={className}
      style={{ width: "100%", height: "100%", ...style }}
    />
  );
}
