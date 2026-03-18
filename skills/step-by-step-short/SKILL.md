---
name: step-by-step-short
description: Creates short 3-second GIF animations and a compact social-media-friendly README from an existing step-by-step tutorial. No video rendering — that's done by step-by-step-reel.
user-invocable: true
---

# Step-by-Step Short (Social Media Version)

Takes an existing step-by-step tutorial (created by the `step-by-step` skill) and produces short animations and a compact README optimized for social media: 3-second GIFs, punchy captions, no source code.

**This skill does NOT create videos.** Video rendering is handled by the `step-by-step-reel` skill, which takes the output of this skill and feeds it into the Remotion-based Reel Maker.

## Prerequisites

### Required input

This skill requires that the `step-by-step` skill has already been run for the given clockface. The input folder must exist:

```
tutorials/<ClockfaceName>/
```

…and must contain `step-*.cs` files and a `README.md`. If the folder doesn't exist, tell the user to run the `step-by-step` skill first.

### Required tools

| Tool | Purpose | Check command |
|------|---------|---------------|
| `Pxl.Render` | Render .cs scripts to animated GIFs | `cd /Users/ronald/repos/github.pxl/pxl-software && dotnet tool run Pxl.Render` |

If the tool is not available, run `dotnet tool restore` in the pxl-software repo directory.

**Note:** `ffmpeg` is NOT needed. This skill does not create videos.

## Input

The user provides a clockface name:
```
/step-by-step-short ColourRain
```

Read the existing tutorial from `tutorials/<ClockfaceName>/README.md` to understand the steps.

## Task

### 1. Read the existing tutorial

Read `tutorials/<ClockfaceName>/README.md`. Extract:
- The number of steps
- The title of each step
- The key idea of each step (you'll condense this further)

Also list all `step-*.cs` files to confirm they exist.

### 2. Re-render all steps as 3-second animations

Render each step as a **3-second animated GIF** — short enough for social media pacing.

```bash
cd /Users/ronald/repos/github.pxl/pxl-software
dotnet tool run Pxl.Render <absolute-path-to-step-NN.cs> \
  -f gif -s 15 -d 3 -m clock --fps 20 --gap 0.1 \
  -o <output-dir>/short-NN.gif
```

Also render the final clockface (from the original source in `tutorials/<ClockfaceName>/<Name>.cs`) as a **10-second** clip:

```bash
cd /Users/ronald/repos/github.pxl/pxl-software
dotnet tool run Pxl.Render <absolute-path-to-clockface.cs> \
  -f gif -s 15 -d 10 -m clock --fps 20 --gap 0.1 \
  -o <output-dir>/short-final.gif
```

**Render parameters:**

| | Steps | Final |
|---|---|---|
| `-f` | gif | gif |
| `-s` | 15 (→ 360x360px) | 15 (→ 360x360px) |
| `-d` | **3** (3 seconds) | **10** (10 seconds) |
| `-m` | clock | clock |
| `--fps` | 20 | 20 |
| `--gap` | 0.1 | 0.1 |

### 3. Write README-Short.md

Create `README-Short.md` in the same output folder. This is the **social media script** — ultra-compact, designed to be readable on a phone screen.

**Rules:**
- **No source code.** Zero. Not even snippets.
- **Bullet points only** — 1 to 3 bullets per step, max ~15 words each
- **Describe the idea, not the implementation.** Say "Add falling rain" not "Use a for-loop with DrawLine"
- **Start with the goal** — show the finished clockface, one sentence what it is
- **End with the result** — the final animation with a short wrap-up

**Template:**

```markdown
# <Clockface Name> — Step by Step

![Final Result](short-final.gif)

**<One sentence: what this clockface does.>**

---

### Step 1: <Title>
- <What we start with — the simplest visual element>

![Step 1](short-01.gif)

### Step 2: <Title>
- <What changes — one key idea>
- <What to notice>

![Step 2](short-02.gif)

...

### Done!
- <One-line celebration or summary>

![Complete](short-final.gif)
```

**Example:**

```markdown
# Colour Rain — Step by Step

![Final Result](short-final.gif)

**Colorful rain lines falling across the display with a time overlay.**

---

### Step 1: A single line
- One vertical line on a black background

![Step 1](short-01.gif)

### Step 2: Fill the display
- 24 lines, one per column

![Step 2](short-02.gif)

### Step 3: Make it rain
- Lines scroll downward every second

![Step 3](short-03.gif)

### Step 4: Add color
- Each column gets a unique hue from the HSV spectrum

![Step 4](short-04.gif)

### Step 5: Stagger the drops
- Random offsets so lines don't fall in sync

![Step 5](short-05.gif)

### Step 6: Show the time
- Hour, minutes, and seconds overlaid on the rain

![Step 6](short-06.gif)

### Done!
- From one line to a full clockface in 6 steps

![Complete](short-final.gif)
```

### 4. Verify

Check that:
- All `short-*.gif` files exist and are animated (not static)
- `README-Short.md` is concise — each step should fit on a phone screen without scrolling

Report to the user: number of steps, and remind them to run `step-by-step-reel` next to generate the video.

## Output Structure

All output goes into the **same folder** as the existing tutorial:

```
tutorials/<ClockfaceName>/
├── README.md              # (existing) Full tutorial
├── README-Short.md        # NEW: Social media script
├── final.gif              # (existing) Full 2-min animation
├── short-01.gif           # NEW: 3-second step animations
├── short-02.gif
├── ...
├── short-final.gif        # NEW: 10-second final animation
├── step-01.cs             # (existing)
├── step-01.gif            # (existing)
├── ...
└── step-NN.cs             # (existing)
```

## What's next?

After this skill is done, run `step-by-step-reel` to generate the tutorial video from the short GIFs and README-Short.md content.

## Error Handling

- **Input folder missing:** Tell the user to run the `step-by-step` skill first.
- **Pxl.Render not available:** Run `dotnet tool restore` in `/Users/ronald/repos/github.pxl/pxl-software`.
- **GIF rendering fails:** Fix the .cs file and retry (up to 3 attempts). If the .cs files from the original tutorial don't compile, something is broken upstream — tell the user.
