import React from "react";
import { AbsoluteFill, interpolate, staticFile, useCurrentFrame } from "remotion";
import { Gif } from "@remotion/gif";
import type { AnimationSlide } from "./types";

export const AnimationSlideComponent: React.FC<{
  slide: AnimationSlide;
  background: string;
}> = ({ slide, background }) => {
  const frame = useCurrentFrame();

  const scale = interpolate(frame, [0, 6], [0.9, 1], {
    extrapolateRight: "clamp",
  });

  const opacity = interpolate(frame, [0, 4], [0, 1], {
    extrapolateRight: "clamp",
  });

  // gifPath is just a filename — served from public/ via staticFile()
  const gifSrc = staticFile(slide.gifPath);

  return (
    <AbsoluteFill
      style={{
        backgroundColor: background,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        opacity,
      }}
    >
      <div
        style={{
          transform: `scale(${scale})`,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          width: "80%",
          height: "80%",
        }}
      >
        <Gif
          src={gifSrc}
          fit="contain"
          style={{
            width: "100%",
            height: "100%",
          }}
        />
      </div>
    </AbsoluteFill>
  );
};
