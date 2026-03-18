/**
 * Layout parameters for the reel slides.
 * Tweak these values to adjust sizes, spacing, and fonts.
 *
 * "v" = vertical (1080x1920), "h" = horizontal (1920x1080)
 */

export const LAYOUT = {
  // ── Fonts ──
  fontFamily:
    '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif',
  monoFamily:
    '"SF Mono", "Fira Code", "Cascadia Code", Menlo, Monaco, "Courier New", monospace',

  // ── Header ──
  header: {
    logoHeight: { v: 80, h: 56 },
    gap: { v: 24, h: 18 },
    paddingTop: { v: 36, h: 28 },
    taglineFontSize: { v: 40, h: 28 },
    clockfaceNameFontSize: { v: 36, h: 24 },
    taglineColor: "#666666",
    clockfaceNameColor: "#999999",
  },

  // ── Separator ──
  separator: {
    marginTop: { v: 16, h: 12 },
    color: "#333333",
  },

  // ── Content area ──
  content: {
    titleFontSize: { v: 56, h: 44 },
    titleColor: "#ffffff",
    descriptionFontSize: { v: 40, h: 30 },
    descriptionColor: "#bbbbbb",
    paddingTopWithCode: { v: 24, h: 16 },
    paddingTopNoCode: { v: 10, h: 10 },
  },

  // ── Code block ──
  code: {
    fontSize: { v: 26, h: 20 },
    lineHeight: 1.5,
    padding: { v: "24px 28px", h: "16px 20px" },
    backgroundColor: "#1a1a28",
    borderColor: "#2a2a3e",
    borderRadius: 12,
  },

  // ── Footer ──
  footer: {
    leftFontSize: { v: 30, h: 22 },
    rightFontSize: { v: 32, h: 24 },
    leftColor: "#555555",
    rightColor: "#666666",
    paddingBottom: { v: 32, h: 22 },
    paddingTop: { v: 18, h: 12 },
    borderColor: "#2a2a2a",
  },

  // ── GIF half ──
  gif: {
    sizePct: "94%",
  },

  // ── Outro ──
  outro: {
    logoHeightPct: { v: 0.15, h: 0.25 },
    logoMarginBottom: { v: 40, h: 30 },
    lineFontSizes: {
      // index 0, 1, 2+
      v: [48, 40, 34],
      h: [38, 32, 26],
    },
    lineColors: ["#111111", "#444444", "#888888"],
    lineWeights: [700, 500, 400] as number[],
    lineSpacing: { v: 16, h: 12 },
    overlayOpacity: 0.75,
    padding: { v: "80px 60px", h: "40px 80px" },
  },

  // ── Side padding (shared) ──
  sidePadding: { v: 50, h: 40 },
} as const;

/** Helper: pick vertical or horizontal value */
export function vh<T>(isVertical: boolean, pair: { v: T; h: T }): T {
  return isVertical ? pair.v : pair.h;
}
