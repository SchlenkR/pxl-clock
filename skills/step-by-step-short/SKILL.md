---
name: step-by-step-short
description: Creates a compact social-media-friendly version of an existing step-by-step tutorial — short animations (3s), bullet-point captions, no code. Designed for Instagram Reels / YouTube Shorts.
user-invocable: true
---

# Step-by-Step Short (Social Media Version)

Takes an existing step-by-step tutorial (created by the `step-by-step` skill) and produces a compact version optimized for social media: short animations, punchy captions, no source code. The output is ready to be assembled into an Instagram Reel or YouTube Short.

## Prerequisites

### Required input

This skill requires that the `step-by-step` skill has already been run for the given clockface. The input folder must exist:

```
apps/stepByStep/<ClockfaceName>/
```

…and must contain `step-*.cs` files and a `README.md`. If the folder doesn't exist, tell the user to run the `step-by-step` skill first.

### Required tools

| Tool | Purpose | Check command | Install (macOS) |
|------|---------|---------------|-----------------|
| `Pxl.Render` | Render .cs scripts to animated images | `cd /Users/ronald/repos/github.pxl/pxl-software && dotnet tool run Pxl.Render` | Already in repo (run `dotnet tool restore` if needed) |
| `ffmpeg` | Merge step animations into one video | `ffmpeg -version` | `brew install ffmpeg` |

If a tool is missing, tell the user which one and how to install it before proceeding.

## Input

The user provides a clockface name:
```
/step-by-step-short ColourRain
```

Read the existing tutorial from `apps/stepByStep/<ClockfaceName>/README.md` to understand the steps.

## Task

### 1. Read the existing tutorial

Read `apps/stepByStep/<ClockfaceName>/README.md`. Extract:
- The number of steps
- The title of each step
- The key idea of each step (you'll condense this further)

Also list all `step-*.cs` files to confirm they exist.

### 2. Re-render all steps as 3-second animations

Render each step as a **3-second animated WebP** — short enough for social media pacing.

```bash
cd /Users/ronald/repos/github.pxl/pxl-software
dotnet tool run Pxl.Render <absolute-path-to-step-NN.cs> \
  -f gif -s 15 -d 3 -m clock --fps 20 --gap 0.1 \
  -o <output-dir>/short-NN.gif
```

Also render the final clockface (from the original source in `apps/clockFaces/<Name>.cs`) as a **10-second** clip:

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

### 4. Create merged videos with text slides

Create two videos that alternate between **text slides** and **animation clips**: text → GIF → text → GIF → ... The clock animation is centered on a dark background.

Produce **two formats:**
- **Horizontal** (1920x1080, 16:9) — for YouTube, Twitter, LinkedIn
- **Vertical** (1080x1920, 9:16) — for Instagram Reels, YouTube Shorts, TikTok

#### Video structure

The video follows this sequence:

1. **Title slide** (3 seconds) — clockface name + one-sentence description
2. **Goal animation** — `short-final.gif` (10 seconds), centered
3. For each step:
   - **Text slide** (3 seconds) — step number + title + bullet points from README-Short.md
   - **Step animation** — `short-NN.gif` (3 seconds), centered
4. **Closing slide** (3 seconds) — "Done!" + summary line
5. **Final animation** — `short-final.gif` again (10 seconds), centered

#### Step 4a: Create text slide images

Use `ffmpeg` to generate text slides as still images. Each slide is white text on a dark background (#111111).

**For each text slide**, create a PNG using ffmpeg's `lavfi` input:

```bash
DIR="apps/stepByStep/<ClockfaceName>"

# Title slide (horizontal)
ffmpeg -y -f lavfi -i "color=c=0x111111:s=1920x1080:d=1" -frames:v 1 \
  -vf "drawtext=text='<Clockface Name>':fontsize=64:fontcolor=white:x=(w-text_w)/2:y=(h-text_h)/2-50, \
       drawtext=text='<one-sentence description>':fontsize=32:fontcolor=0xAAAAAA:x=(w-text_w)/2:y=(h-text_h)/2+30" \
  "$DIR/slide-title-h.png"

# Title slide (vertical)
ffmpeg -y -f lavfi -i "color=c=0x111111:s=1080x1920:d=1" -frames:v 1 \
  -vf "drawtext=text='<Clockface Name>':fontsize=56:fontcolor=white:x=(w-text_w)/2:y=(h-text_h)/2-50, \
       drawtext=text='<one-sentence description>':fontsize=28:fontcolor=0xAAAAAA:x=(w-text_w)/2:y=(h-text_h)/2+30" \
  "$DIR/slide-title-v.png"
```

Repeat for each step slide and the closing slide. For step slides, show:
- Line 1: "Step N" (large)
- Line 2: Step title (medium)
- Line 3-5: Bullet points from README-Short.md (smaller, gray)

**Tip for multi-line text:** Use multiple `drawtext` filters chained with commas, each with a different `y` offset.

**Font:** ffmpeg uses system fonts. On macOS, a good default is `fontfile=/System/Library/Fonts/Helvetica.ttc`. If the font is not found, omit `fontfile` and ffmpeg will use its default.

#### Step 4b: Create video segments

For each segment, create a short MP4 clip:

**Text slides** (still image → 3-second video):
```bash
ffmpeg -y -loop 1 -i "$DIR/slide-title-h.png" -t 3 \
  -c:v libx264 -pix_fmt yuv420p -r 20 "$DIR/seg-00-title-h.mp4"
```

**Animation clips** (GIF → centered on dark background):
```bash
# Horizontal: 360x360 GIF centered on 1920x1080 dark background
ffmpeg -y -ignore_loop 0 -i "$DIR/short-01.gif" \
  -c:v libx264 -pix_fmt yuv420p -r 20 \
  -vf "scale=360:360,pad=1920:1080:(1920-360)/2:(1080-360)/2:color=0x111111" \
  "$DIR/seg-01-anim-h.mp4"

# Vertical: 360x360 GIF centered on 1080x1920 dark background
ffmpeg -y -ignore_loop 0 -i "$DIR/short-01.gif" \
  -c:v libx264 -pix_fmt yuv420p -r 20 \
  -vf "scale=360:360,pad=1080:1920:(1080-360)/2:(1920-360)/2:color=0x111111" \
  "$DIR/seg-01-anim-v.mp4"
```

**Important:** Use `-ignore_loop 0` when reading GIFs so ffmpeg reads all frames, not just the first.

Repeat for all steps and the final animation.

#### Step 4c: Create concat lists and merge

```bash
# Horizontal
ls "$DIR"/seg-*-h.mp4 | sort > "$DIR/concat-h.txt"
sed -i '' "s|^|file '|; s|$|'|" "$DIR/concat-h.txt"
ffmpeg -y -f concat -safe 0 -i "$DIR/concat-h.txt" -c copy "$DIR/video-horizontal.mp4"

# Vertical
ls "$DIR"/seg-*-v.mp4 | sort > "$DIR/concat-v.txt"
sed -i '' "s|^|file '|; s|$|'|" "$DIR/concat-v.txt"
ffmpeg -y -f concat -safe 0 -i "$DIR/concat-v.txt" -c copy "$DIR/video-vertical.mp4"
```

#### Step 4d: Clean up temp files

```bash
rm "$DIR"/seg-*.mp4 "$DIR"/slide-*.png "$DIR"/concat-*.txt
```

#### Naming convention for segments

Use zero-padded names so they sort correctly:
```
seg-00-title-h.mp4      # Title text slide
seg-01-goal-h.mp4       # Goal animation (short-final.gif)
seg-02-text-h.mp4       # Step 1 text slide
seg-03-anim-h.mp4       # Step 1 animation
seg-04-text-h.mp4       # Step 2 text slide
seg-05-anim-h.mp4       # Step 2 animation
...
seg-NN-close-h.mp4      # Closing text slide
seg-NN+1-final-h.mp4    # Final animation again
```

### 5. Verify

Check that:
- All `short-*.gif` files exist and are animated (not static)
- `README-Short.md` is concise — each step should fit on a phone screen without scrolling
- `video-horizontal.mp4` plays correctly: text → animation → text → animation → ...
- `video-vertical.mp4` plays correctly and the GIF is nicely centered
- The clock animation is centered and surrounded by dark background in both formats

Report to the user: number of steps, total video duration, and file sizes.

## Output Structure

All output goes into the **same folder** as the existing tutorial:

```
apps/stepByStep/<ClockfaceName>/
├── README.md              # (existing) Full tutorial
├── README-Short.md        # NEW: Social media script
├── final.gif              # (existing) Full 2-min animation
├── short-01.gif           # NEW: 3-second step animations
├── short-02.gif
├── ...
├── short-final.gif        # NEW: 10-second final animation
├── video-horizontal.mp4   # NEW: 16:9 video (text + animations)
├── video-vertical.mp4     # NEW: 9:16 video (text + animations)
├── step-01.cs             # (existing)
├── step-01.gif            # (existing)
├── ...
└── step-NN.cs             # (existing)
```

## Error Handling

- **Input folder missing:** Tell the user to run the `step-by-step` skill first.
- **ffmpeg not installed:** Skip the video merge step. Tell the user: `brew install ffmpeg` and re-run.
- **Pxl.Render not available:** Run `dotnet tool restore` in `/Users/ronald/repos/github.pxl/pxl-software`.
- **GIF rendering fails:** Fix the .cs file and retry (up to 3 attempts). If the .cs files from the original tutorial don't compile, something is broken upstream — tell the user.
