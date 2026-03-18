import React from "react";
import {
  AbsoluteFill,
  Img,
  interpolate,
  staticFile,
  useCurrentFrame,
  useVideoConfig,
  random,
} from "remotion";
import type { ReelConfig, Slide } from "./types";
import { LAYOUT } from "./layout";

const L = LAYOUT;

interface PhotoTile {
  src: string;
  x: number;
  y: number;
  w: number;
  h: number;
  rotation: number;
  delay: number; // frame delay before appearing
}

function buildTiles(
  photos: string[],
  width: number,
  height: number,
  totalFrames: number
): PhotoTile[] {
  const v = height > width;
  // Grid: vertical = 3 cols, horizontal = 4 cols
  const cols = v ? 3 : 4;
  const rows = v ? 5 : 3;
  const count = cols * rows;

  // Shuffle photos deterministically
  const shuffled = photos
    .map((p, i) => ({ p, r: random(`collage-${i}`) }))
    .sort((a, b) => a.r - b.r)
    .map((x) => x.p);

  // Tile size with small overlap
  const tileW = (width / cols) * 1.08;
  const tileH = (height / rows) * 1.08;
  const gapX = width / cols;
  const gapY = height / rows;

  const tiles: PhotoTile[] = [];
  for (let row = 0; row < rows; row++) {
    for (let col = 0; col < cols; col++) {
      const idx = row * cols + col;
      const photo = shuffled[idx % shuffled.length];

      // Slight random offset for organic feel
      const offsetX = (random(`ox-${idx}`) - 0.5) * 10;
      const offsetY = (random(`oy-${idx}`) - 0.5) * 10;
      const rotation = (random(`rot-${idx}`) - 0.5) * 6; // -3 to +3 degrees

      // Stagger: each tile appears a few frames after the previous
      const staggerPerTile = Math.min(3, totalFrames / (count + 4));
      const delay = Math.round(idx * staggerPerTile);

      tiles.push({
        src: staticFile(photo),
        x: col * gapX - (tileW - gapX) / 2 + offsetX,
        y: row * gapY - (tileH - gapY) / 2 + offsetY,
        w: tileW,
        h: tileH,
        rotation,
        delay,
      });
    }
  }

  return tiles;
}

export const CollageSlideComponent: React.FC<{
  slide: Slide;
  config: ReelConfig;
}> = ({ slide, config }) => {
  const frame = useCurrentFrame();
  const { width, height, durationInFrames } = useVideoConfig();

  const photos = config.outroPhotos ?? [];
  if (photos.length === 0) return <AbsoluteFill style={{ backgroundColor: "#ffffff" }} />;

  const slideDuration = Math.round(slide.durationInSeconds * (config.fps ?? 30));
  const tiles = buildTiles(photos, width, height, slideDuration);

  return (
    <AbsoluteFill style={{ backgroundColor: "#ffffff", overflow: "hidden" }}>
      {tiles.map((tile, i) => {
        const localFrame = frame - tile.delay;

        // Each tile: scale up + fade in
        const opacity = interpolate(localFrame, [0, 6], [0, 1], {
          extrapolateLeft: "clamp",
          extrapolateRight: "clamp",
        });
        const scale = interpolate(localFrame, [0, 8], [1.15, 1.0], {
          extrapolateLeft: "clamp",
          extrapolateRight: "clamp",
        });

        return (
          <div
            key={i}
            style={{
              position: "absolute",
              left: tile.x,
              top: tile.y,
              width: tile.w,
              height: tile.h,
              opacity,
              transform: `rotate(${tile.rotation}deg) scale(${scale})`,
              borderRadius: 8,
              overflow: "hidden",
              boxShadow: "0 4px 20px rgba(0,0,0,0.25)",
            }}
          >
            <Img
              src={tile.src}
              style={{
                width: "100%",
                height: "100%",
                objectFit: "cover",
              }}
            />
          </div>
        );
      })}

      {/* Subtle dark vignette overlay at the edges */}
      <AbsoluteFill
        style={{
          background:
            "radial-gradient(ellipse at center, transparent 50%, rgba(0,0,0,0.15) 100%)",
          pointerEvents: "none",
        }}
      />
    </AbsoluteFill>
  );
};
