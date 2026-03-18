import React from "react";
import { Composition } from "remotion";
import { Reel, calculateTotalDuration } from "./Reel";
import type { ReelConfig } from "./types";

const defaultConfig: ReelConfig = {
  fps: 30,
  logoPath: "pxl-logo.svg",
  brandTagline: "Programming...",
  clockfaceName: "Preview",
  slides: [
    {
      title: "Step 1 — Preview",
      description: "This is a preview slide",
      gifPath: "placeholder.gif",
      durationInSeconds: 3,
    },
  ],
  background: "#0e0e12",
};

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Composition
        id="ReelVertical"
        component={Reel}
        durationInFrames={calculateTotalDuration(defaultConfig)}
        fps={defaultConfig.fps}
        width={1080}
        height={1920}
        defaultProps={{ config: defaultConfig }}
      />
      <Composition
        id="ReelHorizontal"
        component={Reel}
        durationInFrames={calculateTotalDuration(defaultConfig)}
        fps={defaultConfig.fps}
        width={1920}
        height={1080}
        defaultProps={{ config: defaultConfig }}
      />
    </>
  );
};
