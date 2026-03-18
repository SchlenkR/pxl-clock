import React from "react";
import { Sequence, useVideoConfig } from "remotion";
import type { ReelConfig } from "./types";
import { SplitSlideComponent } from "./SplitSlideComponent";
import { OutroSlideComponent } from "./OutroSlideComponent";
import { CollageSlideComponent } from "./CollageSlideComponent";

export const Reel: React.FC<{ config: ReelConfig }> = ({ config }) => {
  const { fps } = useVideoConfig();

  let currentFrame = 0;

  return (
    <>
      {config.slides.map((slide, i) => {
        const durationInFrames = Math.round(slide.durationInSeconds * fps);
        const from = currentFrame;
        currentFrame += durationInFrames;

        return (
          <Sequence key={i} from={from} durationInFrames={durationInFrames}>
            {slide.collage ? (
              <CollageSlideComponent slide={slide} config={config} />
            ) : slide.outro ? (
              <OutroSlideComponent slide={slide} config={config} />
            ) : (
              <SplitSlideComponent slide={slide} config={config} />
            )}
          </Sequence>
        );
      })}
    </>
  );
};

export const calculateTotalDuration = (config: ReelConfig): number => {
  return config.slides.reduce(
    (sum, slide) => sum + Math.round(slide.durationInSeconds * config.fps),
    0
  );
};
