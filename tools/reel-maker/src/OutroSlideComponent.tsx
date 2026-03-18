import React from "react";
import {
  AbsoluteFill,
  Img,
  interpolate,
  staticFile,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import type { ReelConfig, Slide } from "./types";
import { LAYOUT, vh } from "./layout";

const L = LAYOUT;

export const OutroSlideComponent: React.FC<{
  slide: Slide;
  config: ReelConfig;
}> = ({ slide, config }) => {
  const frame = useCurrentFrame();
  const { width, height } = useVideoConfig();
  const v = height > width;

  const bgProgress = interpolate(frame, [0, 15], [0, 1], {
    extrapolateRight: "clamp",
  });

  const bg = config.background ?? "#0e0e12";

  const contentOpacity = interpolate(frame, [8, 18], [0, 1], {
    extrapolateRight: "clamp",
  });
  const contentScale = interpolate(frame, [8, 20], [0.95, 1], {
    extrapolateRight: "clamp",
  });

  const logoSrc = config.logoPathDark
    ? staticFile(config.logoPathDark)
    : null;

  return (
    <AbsoluteFill>
      {/* Dark background (fading out) */}
      <AbsoluteFill
        style={{
          backgroundColor: bg,
          opacity: 1 - bgProgress,
        }}
      />
      {/* White background (fading in) */}
      <AbsoluteFill
        style={{
          backgroundColor: "#ffffff",
          opacity: bgProgress,
        }}
      />

      {/* Content */}
      <AbsoluteFill
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          opacity: contentOpacity,
          transform: `scale(${contentScale})`,
          padding: vh(v, L.outro.padding),
        }}
      >
        {logoSrc && (
          <Img
            src={logoSrc}
            style={{
              height: height * vh(v, L.outro.logoHeightPct),
              objectFit: "contain",
              marginBottom: vh(v, L.outro.logoMarginBottom),
            }}
          />
        )}

        {slide.outroLines?.map((line, i) => {
          const sizes = vh(v, L.outro.lineFontSizes);
          return (
            <div
              key={i}
              style={{
                fontFamily: L.fontFamily,
                fontSize: sizes[Math.min(i, sizes.length - 1)],
                fontWeight: L.outro.lineWeights[Math.min(i, L.outro.lineWeights.length - 1)],
                color: L.outro.lineColors[Math.min(i, L.outro.lineColors.length - 1)],
                textAlign: "center",
                lineHeight: 1.4,
                marginBottom: i < slide.outroLines!.length - 1
                  ? vh(v, L.outro.lineSpacing)
                  : 0,
              }}
            >
              {line}
            </div>
          );
        })}
      </AbsoluteFill>
    </AbsoluteFill>
  );
};
