import React from "react";
import {
  AbsoluteFill,
  Img,
  interpolate,
  staticFile,
  useCurrentFrame,
  useVideoConfig,
} from "remotion";
import { Gif } from "@remotion/gif";
import type { ReelConfig, Slide } from "./types";
import { CodeHighlight } from "./CodeHighlight";
import { LAYOUT, vh } from "./layout";

const L = LAYOUT;

export const SplitSlideComponent: React.FC<{
  slide: Slide;
  config: ReelConfig;
}> = ({ slide, config }) => {
  const frame = useCurrentFrame();
  const { width, height } = useVideoConfig();
  const background = config.background ?? "#0e0e12";
  const isVertical = height > width;
  const v = isVertical;

  const instant = slide.skipEntrance ?? false;
  const contentOpacity = instant ? 1 : interpolate(frame, [0, 6], [0, 1], {
    extrapolateRight: "clamp",
  });
  const contentSlide = instant ? 0 : interpolate(frame, [0, 8], [10, 0], {
    extrapolateRight: "clamp",
  });
  const gifScale = instant ? 1 : interpolate(frame, [0, 8], [0.94, 1], {
    extrapolateRight: "clamp",
  });
  const gifOpacity = instant ? 1 : interpolate(frame, [0, 5], [0, 1], {
    extrapolateRight: "clamp",
  });

  const gifSrc = staticFile(slide.gifPath);
  const logoSrc = config.logoPath ? staticFile(config.logoPath) : null;

  // 50/50 split
  const textStyle: React.CSSProperties = v
    ? { top: 0, left: 0, width, height: height / 2 }
    : { top: 0, left: 0, width: width / 2, height };

  const gifStyle: React.CSSProperties = v
    ? { top: height / 2, left: 0, width, height: height / 2 }
    : { top: 0, left: width / 2, width: width / 2, height };

  const pad = vh(v, L.sidePadding);

  return (
    <AbsoluteFill style={{ backgroundColor: background }}>
      {/* ─── TEXT HALF ─── */}
      <div
        style={{
          position: "absolute",
          ...textStyle,
          display: "flex",
          flexDirection: "column",
        }}
      >
        {/* ── Header row: Logo left, tagline + name right ── */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            padding: `${vh(v, L.header.paddingTop)}px ${pad}px 0`,
            gap: vh(v, L.header.gap),
          }}
        >
          {logoSrc && (
            <Img
              src={logoSrc}
              style={{
                height: vh(v, L.header.logoHeight),
                objectFit: "contain",
                flexShrink: 0,
              }}
            />
          )}
          <div style={{ display: "flex", flexDirection: "column", gap: 2 }}>
            {config.brandTagline && (
              <span
                style={{
                  fontFamily: L.fontFamily,
                  fontSize: vh(v, L.header.taglineFontSize),
                  fontWeight: 400,
                  color: L.header.taglineColor,
                }}
              >
                {config.brandTagline}
              </span>
            )}
            {config.clockfaceName && (
              <span
                style={{
                  fontFamily: L.fontFamily,
                  fontSize: vh(v, L.header.clockfaceNameFontSize),
                  fontWeight: 400,
                  color: L.header.clockfaceNameColor,
                  fontStyle: "italic",
                }}
              >
                &ldquo;{config.clockfaceName}&rdquo;
              </span>
            )}
          </div>
        </div>

        {/* ── Separator ── */}
        <div
          style={{
            height: 1,
            backgroundColor: L.separator.color,
            margin: `${vh(v, L.separator.marginTop)}px ${pad}px 0`,
          }}
        />

        {/* ── Step content (animated) ── */}
        <div
          style={{
            flex: 1,
            display: "flex",
            flexDirection: "column",
            justifyContent: slide.code ? "flex-start" : "center",
            padding: `${vh(v, slide.code ? L.content.paddingTopWithCode : L.content.paddingTopNoCode)}px ${pad}px 0`,
            opacity: contentOpacity,
            transform: `translateY(${contentSlide}px)`,
            overflow: "hidden",
          }}
        >
          {/* Title */}
          <div
            style={{
              fontFamily: L.fontFamily,
              fontSize: vh(v, L.content.titleFontSize),
              fontWeight: 700,
              color: L.content.titleColor,
              textAlign: "left",
              lineHeight: 1.3,
              marginBottom: slide.description ? 14 : 0,
            }}
          >
            {slide.title}
          </div>

          {/* Description */}
          {slide.description && (
            <div
              style={{
                fontFamily: L.fontFamily,
                fontSize: vh(v, L.content.descriptionFontSize),
                fontWeight: 400,
                color: L.content.descriptionColor,
                textAlign: "left",
                lineHeight: 1.4,
                marginBottom: slide.code ? 24 : 0,
              }}
            >
              {slide.description}
            </div>
          )}

          {/* Code block */}
          {slide.code && (
            <div
              style={{
                flex: 1,
                display: "flex",
                minHeight: 0,
              }}
            >
              <pre
                style={{
                  fontFamily: L.monoFamily,
                  fontSize: vh(v, L.code.fontSize),
                  lineHeight: L.code.lineHeight,
                  margin: 0,
                  padding: vh(v, L.code.padding),
                  backgroundColor: L.code.backgroundColor,
                  borderRadius: L.code.borderRadius,
                  border: `1px solid ${L.code.borderColor}`,
                  width: "100%",
                  whiteSpace: "pre-wrap",
                  wordBreak: "break-word",
                  overflow: "hidden",
                }}
              >
                <CodeHighlight
                  code={slide.code}
                  fontSize={vh(v, L.code.fontSize)}
                  lineHeight={L.code.lineHeight}
                />
              </pre>
            </div>
          )}
        </div>

        {/* ── Footer ── */}
        {(config.footerLeft || config.footerRight) && (
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              margin: `0 ${pad}px`,
              borderTop: `1px solid ${L.footer.borderColor}`,
              padding: `${vh(v, L.footer.paddingTop)}px 0 ${vh(v, L.footer.paddingBottom)}px`,
            }}
          >
            <span
              style={{
                fontFamily: L.monoFamily,
                fontSize: vh(v, L.footer.leftFontSize),
                color: L.footer.leftColor,
              }}
            >
              {config.footerLeft ?? ""}
            </span>
            <span
              style={{
                fontFamily: L.fontFamily,
                fontSize: vh(v, L.footer.rightFontSize),
                fontWeight: 600,
                color: L.footer.rightColor,
              }}
            >
              {config.footerRight ?? ""}
            </span>
          </div>
        )}
      </div>

      {/* ─── GIF HALF ─── */}
      <div
        style={{
          position: "absolute",
          ...gifStyle,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          opacity: gifOpacity,
        }}
      >
        <div
          style={{
            transform: `scale(${gifScale})`,
            width: L.gif.sizePct,
            height: L.gif.sizePct,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <Gif
            src={gifSrc}
            fit="contain"
            style={{ width: "100%", height: "100%" }}
          />
        </div>
      </div>
    </AbsoluteFill>
  );
};
