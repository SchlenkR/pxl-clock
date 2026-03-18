---
name: step-by-step-reel
description: Generates a Remotion-based tutorial reel video from an existing step-by-step tutorial — split-screen layout with code + animation, outro with product photos, collage ending.
user-invocable: true
---

# Step-by-Step Reel (Remotion Video)

Takes an existing step-by-step tutorial (created by the `step-by-step` skill) and its short animations (created by the `step-by-step-short` skill) and generates a professional tutorial reel video using the **Reel Maker** tool.

The Reel Maker is a Remotion-based (React) video tool at `tools/reel-maker/`. It produces split-screen videos: text + syntax-highlighted C# code on one side, animated GIF on the other. Both vertical (1080x1920) and horizontal (1920x1080) formats.

## ⚠️ IMPORTANT: The Reel Maker is a fixed tool

The Reel Maker at `tools/reel-maker/` is a **finished, tested tool**. Do NOT modify any of its source files:
- `src/SplitSlideComponent.tsx` — split-screen slide layout
- `src/OutroSlideComponent.tsx` — white outro with PXL logo + CTA
- `src/CollageSlideComponent.tsx` — product photo collage grid
- `src/CodeHighlight.tsx` — C# syntax highlighting (VS Code Dark+ colors)
- `src/Reel.tsx` — slide router
- `src/Root.tsx` — Remotion compositions
- `src/types.ts` — TypeScript interfaces
- `src/layout.ts` — layout parameters (font sizes, colors, spacing)
- `render.mjs` — CLI render script

Your job is to **generate the `reel-config.json`** and **run the render command**. Nothing else.

## Prerequisites

### Required input

Both skills must have been run already:

1. **`step-by-step`** — the full tutorial with `step-*.cs`, `step-*.gif`, and `README.md`
2. **`step-by-step-short`** — the short animations `short-*.gif` and `README-Short.md`

The input folder must exist:
```
tutorials/<ClockfaceName>/
```

...and must contain `short-*.gif` files and `README-Short.md`. If these don't exist, tell the user which skill to run first.

### Required tools

| Tool | Purpose | Check |
|------|---------|-------|
| `node` | Run the Remotion render script | `node --version` (v18+) |
| `npm` | Install dependencies (if needed) | `npm --version` |

If `tools/reel-maker/node_modules/` doesn't exist, run:
```bash
cd tools/reel-maker && npm install
```

## Input

The user provides a clockface name:
```
/step-by-step-reel ColourWheelDynamic
```

## Task

### 1. Read the existing tutorial

Read `tutorials/<ClockfaceName>/README-Short.md` to extract:
- The number of steps
- The title of each step
- The bullet-point description of each step

Also read the full `README.md` to extract code snippets for each step. The code in the reel should be **stripped down** — only the interesting/new parts, no boilerplate (`#:package`, `using`, the outer `var scene` lambda). Focus on the 5-15 most important lines per step.

Verify that all `short-*.gif` files exist.

### 2. Generate `reel-config.json`

Create `tutorials/<ClockfaceName>/reel-config.json` following this exact schema:

```json
{
  "fps": 30,
  "background": "#0e0e12",
  "logoPath": "pxl-logo.svg",
  "logoPathDark": "pxl-logo-black.svg",
  "brandTagline": "Programming...",
  "clockfaceName": "<Clockface Display Name>",
  "footerLeft": "github.com/SchlenkR/pxl-clock",
  "footerRight": "pxlclock.com",
  "outroPhotos": [
    "outro-photos/Pxl_clock_1.jpg",
    "outro-photos/Pxl_clock_2.jpg",
    "outro-photos/Pxl_clock_3.jpg",
    "outro-photos/Pxl_clock_5.jpg",
    "outro-photos/Pxl_clock_10.jpg",
    "outro-photos/Pxl_clock_11.jpg",
    "outro-photos/Pxl_clock_12.jpg",
    "outro-photos/Pxl_clock_15.jpg",
    "outro-photos/Pxl_clock_20.jpg",
    "outro-photos/Pxl_clock_25.jpg",
    "outro-photos/Pxl_clock_35.jpg",
    "outro-photos/Pxl_clock_40.jpg",
    "outro-photos/Pxl_clock_45.jpg",
    "outro-photos/Pxl_clock_55.jpg",
    "outro-photos/Pxl_clock_60.jpg",
    "outro-photos/Pxl_clock_65.jpg",
    "outro-photos/Pxl_clock_75.jpg",
    "outro-photos/Pxl_clock_80.jpg",
    "outro-photos/Pxl_clock_85.jpg",
    "outro-photos/Pxl_clock_90.jpg"
  ],
  "slides": [
    ...
  ]
}
```

The `outroPhotos` array is always the same — these are pre-installed product photos in `tools/reel-maker/public/outro-photos/`.

### Slide structure

The slides array must follow this exact sequence:

#### a) Intro slide (first)
Shows the finished clockface with a catchy one-line description. Must have `skipEntrance: true` so it's visible from frame 0.

```json
{
  "title": "<One-line description of what this clockface does>",
  "gifPath": "short-final.gif",
  "durationInSeconds": 5,
  "skipEntrance": true
}
```

#### b) Step slides (one per tutorial step)
Each step has a title, short description, stripped C# code, and the animation GIF.

```json
{
  "title": "Step N — <Title>",
  "description": "<1-2 line description from README-Short.md>",
  "code": "<stripped C# code — only the interesting parts, 5-15 lines>",
  "gifPath": "short-NN.gif",
  "durationInSeconds": 3
}
```

**Code stripping rules:**
- Remove `#:package Pxl@*`
- Remove `using Pxl.Ui.CSharp;`
- Remove the outer `var scene = (DrawingContext ctx) => { ... };` wrapper — show only the body
- Keep only the code that's NEW or CHANGED in this step
- Use `\n` for line breaks in the JSON string
- If the code is too long (>15 lines), show only the most important fragment and add `// ...` to indicate omission
- Preserve proper indentation

#### c) "Done!" slide
```json
{
  "title": "Done!",
  "description": "From <starting point> to <finished result>\nin N steps",
  "gifPath": "short-final.gif",
  "durationInSeconds": 5
}
```

#### d) Outro slide
```json
{
  "title": "",
  "outro": true,
  "outroLines": [
    "www.pxlclock.com",
    "Get your own PXL Clock",
    "Use code RONALD for €25 off"
  ],
  "durationInSeconds": 3
}
```

#### e) Collage slide (last)
```json
{
  "title": "",
  "collage": true,
  "durationInSeconds": 4
}
```

### 3. Render the video

Run the render command from the reel-maker directory:

```bash
cd tools/reel-maker
node render.mjs ../../tutorials/<ClockfaceName>/reel-config.json --format both
```

This produces two files in the clockface directory:
- `<ClockfaceName>-vertical.mp4` (1080x1920)
- `<ClockfaceName>-horizontal.mp4` (1920x1080)

**Note:** The render script automatically copies GIF files to the reel-maker's `public/` directory for Remotion to serve, and cleans them up after rendering.

### 4. Verify

Check that:
- Both MP4 files exist and have reasonable file sizes (typically 5-10 MB each)
- The render completed without errors
- Report file sizes and total video duration to the user

## Slide type reference

| Property | Type | Description |
|----------|------|-------------|
| `title` | string | Slide title (supports `\n` for line breaks) |
| `description` | string? | Subtitle/description text |
| `code` | string? | C# code snippet (syntax-highlighted automatically) |
| `gifPath` | string? | Path to animated GIF (relative to config file) |
| `durationInSeconds` | number | How long this slide is shown |
| `skipEntrance` | boolean? | If true, no fade-in animation (use for first slide) |
| `outro` | boolean? | If true, renders as white outro card with PXL logo |
| `outroLines` | string[]? | CTA text lines for the outro slide |
| `collage` | boolean? | If true, renders product photo collage grid |

## Config reference

| Property | Type | Description |
|----------|------|-------------|
| `fps` | number | Frames per second (always 30) |
| `background` | string? | Dark background color (default: `#0e0e12`) |
| `logoPath` | string? | Light PXL logo SVG for dark slides |
| `logoPathDark` | string? | Dark PXL logo SVG for white outro |
| `brandTagline` | string? | Tagline next to logo (e.g. "Programming...") |
| `clockfaceName` | string? | Clockface name shown in header + used for output filenames |
| `footerLeft` | string? | Footer left text (GitHub link) |
| `footerRight` | string? | Footer right text (website) |
| `outroPhotos` | string[]? | Product photo paths for collage slide |

## Output Structure

```
tutorials/<ClockfaceName>/
├── reel-config.json              # NEW: Remotion slide config
├── <ClockfaceName>-vertical.mp4  # NEW: 9:16 video for Reels/Shorts
├── <ClockfaceName>-horizontal.mp4 # NEW: 16:9 video for YouTube
├── README.md                     # (existing from step-by-step)
├── README-Short.md               # (existing from step-by-step-short)
├── short-*.gif                   # (existing from step-by-step-short)
├── step-*.cs                     # (existing from step-by-step)
└── step-*.gif                    # (existing from step-by-step)
```

## Error Handling

- **Missing short GIFs:** Tell the user to run `step-by-step-short` first.
- **Missing node_modules:** Run `cd tools/reel-maker && npm install`.
- **Render fails with "source image cannot be decoded":** Product photos in `public/outro-photos/` may be too large. Resize with `sips -Z 800 *.jpg` in that directory.
- **Render fails with other errors:** Check that `reel-config.json` is valid JSON and all `gifPath` references exist.
