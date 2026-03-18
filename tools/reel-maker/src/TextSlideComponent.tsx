import React from "react";
import {
  AbsoluteFill,
  interpolate,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import type { TextSlide } from "./types";

export const TextSlideComponent: React.FC<{
  slide: TextSlide;
  accentColor: string;
  background: string;
}> = ({ slide, accentColor, background }) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  // Fade in over 5 frames
  const opacity = interpolate(frame, [0, 5], [0, 1], {
    extrapolateRight: "clamp",
  });

  // Slide up slightly on enter
  const translateY = interpolate(frame, [0, 8], [20, 0], {
    extrapolateRight: "clamp",
  });

  return (
    <AbsoluteFill
      style={{
        backgroundColor: background,
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        padding: "60px 80px",
        opacity,
        transform: `translateY(${translateY}px)`,
      }}
    >
      {slide.lines.map((line, i) => (
        <div
          key={i}
          style={{
            fontSize: line.fontSize ?? 64,
            color: line.color ?? "#ffffff",
            fontFamily:
              '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif',
            fontWeight: i === 0 ? 800 : 500,
            textAlign: "center",
            lineHeight: 1.3,
            marginBottom: i < slide.lines.length - 1 ? 16 : 0,
          }}
        >
          {line.text}
        </div>
      ))}
    </AbsoluteFill>
  );
};
