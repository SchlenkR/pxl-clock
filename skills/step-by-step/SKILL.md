---
name: step-by-step
description: Decomposes a PXL Clock clockface into progressive learning steps, creating a .cs script and animated GIF for each step — from simplest building block to the finished clockface.
user-invocable: true
---

# Step-by-Step Clockface Builder

You are given an existing PXL Clock clockface. Your task is to decompose it into a progressive sequence of learning steps — from the simplest building block to the complete clockface. For every step, you create a C# script (`.cs`) and render an animated GIF preview.

## Context: What is a PXL Clock Clockface?

The PXL Clock is a 24x24 RGB-LED pixel display. Clockfaces are C# scripts that render animations on this 24x24 canvas. Each script defines a `scene` lambda that runs every frame (~40 fps):

```csharp
#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);
    ctx.DrawTextMono4x5($"{ctx.Now:HH:mm}", 2, 10, color: Colors.White);
};
```

Key facts:
- Canvas is 24x24 pixels (576 total)
- `#:package Pxl@*` imports the Pxl NuGet package (required in every script)
- `var scene = (DrawingContext ctx) => { ... };` is the entry point — called every frame
- Variables declared **before** the scene lambda persist across frames (state)
- `ctx.Now` gives the current time, `ctx.Elapsed` the time since start

Existing clockfaces live in `apps/clockFaces/`. The full API reference is in `llms.txt` at the repository root.

## Input

The user provides a clockface name. Read the source from:
```
apps/clockFaces/<Name>.cs
```

If the file doesn't exist, list available clockfaces from that folder and ask the user to pick one.

## Task

### 1. Analyze the clockface

Read the source code carefully. Identify the independent concepts it uses:
- Background rendering
- Color creation (solid, HSV, gradients)
- Shape drawing (lines, circles, rectangles, arcs)
- Text rendering (time display, different fonts)
- Pixel-level access (SetPixels, GetPixel)
- Animation (time-based movement, sin/cos, easing)
- State management (variables persisting across frames)
- Algorithms (cellular automata, particle systems, physics)
- Layers & blend modes
- Helper functions / classes

### 2. Plan the steps

Design **5 to 12 steps** that progressively build toward the final clockface. Each step must:

- **Be self-contained** — it compiles and renders as a standalone animation
- **Add exactly one new concept** to the previous step (or at most two closely related ones)
- **Be visually interesting** on its own — not just a blank screen with one pixel
- **Build logically** toward the final result

**Step planning guidelines:**

1. **Step 01** — The simplest visual element from the clockface. Examples: a colored background, a single shape, a static color.
2. **Middle steps** — Add complexity one layer at a time: introduce motion, add time-based behavior, add more visual elements, introduce color variation, add state.
3. **Final step** — The complete clockface. Must match the original source (you may clean up formatting but the logic must be equivalent).

**Good decomposition example (for a rain + clock clockface):**
1. Black background with one colored vertical line
2. Multiple colored lines at different x-positions
3. Lines move downward based on time (animation)
4. Each line gets a unique HSV color
5. Add offset array for staggered rain movement
6. Display the hour using large text
7. Add minutes and seconds display
8. Final: complete clockface with rain + all time elements

**Bad decomposition (avoid this):**
1. Empty scene (black screen) — boring, teaches nothing
2. Just an import statement — that's not a step

### 3. Create the files

Write all output into:
```
tutorials/<ClockfaceName>/
```

For each step, create a `.cs` file:

```csharp
// Step N: <Short title>
// <One-line description of what this step adds>

#:package Pxl@*

using Pxl.Ui.CSharp;

// ... code for this step ...

var scene = (DrawingContext ctx) =>
{
    // ... rendering code ...
};
```

**Conventions:**
- Always use `#:package Pxl@*` (NOT `#:project`)
- The `var scene` lambda is the only entry point
- State variables go **before** the scene lambda
- Keep the code clean and well-commented — these are learning materials

### 4. Render the final clockface first

Before rendering the individual steps, render the **original clockface** as a 2-minute animation. This will be shown at the top of the tutorial as the goal.

```bash
cd /Users/ronald/repos/github.pxl/pxl-software
dotnet tool run Pxl.Render <absolute-path-to-original-clockface.cs> \
  -f gif -s 15 -d 120 -m clock --fps 20 --gap 0.1 \
  -o <output-dir>/final.gif
```

### 5. Render each step as animated WebP

Use the `Pxl.Render` dotnet tool to render each step. Do **not** install it — use it ad-hoc from the existing tool manifest in the pxl-software repo:

```bash
cd /Users/ronald/repos/github.pxl/pxl-software
dotnet tool run Pxl.Render <absolute-path-to-step.cs> \
  -f gif -s 15 -d 10 -m clock --fps 20 --gap 0.1 \
  -o <absolute-path-to-step.gif>
```

If `dotnet tool run` says the tool is not available, run `dotnet tool restore` once first (restores from the existing manifest — does not install anything new).

**Render parameters:**

| | Steps | Final |
|---|---|---|
| `-f` | **gif** | **gif** |
| `-s` | 15 (→ 360x360px) | 15 (→ 360x360px) |
| `-d` | **10** (10 seconds) | **120** (2 minutes) |
| `-m` | clock | clock |
| `--fps` | 20 | 20 |
| `--gap` | 0.1 | 0.1 |

We use **animated GIF** format. The files are larger than WebP but universally compatible — every tool, browser, and social media platform supports them, and `ffmpeg` can read them directly without conversion.

**IMPORTANT — Rendering checklist:**
- Every step must be a **10-second animated WebP** (200 frames at 20fps). The final must be **2 minutes** (2400 frames). If a render produces a static image or very short animation, check that `-d` and `--fps 20` are both present.
- The `--gap 0.1` parameter controls the spacing between LED pixels. The default (0.05) makes the gap nearly invisible, while larger values like 0.2 make the gap too dominant. `0.1` gives about 1.5px spacing at scale 15 — a subtle but visible gap between the rounded LED pixels.
- Do **not** change `-s 15` — 360x360px is a good balance between quality and file size.

If rendering fails (compilation error), fix the `.cs` file and retry (up to 3 attempts per step).

### 6. Write README.md — the tutorial

Create a `README.md` in the output folder. This is **not** just a list of steps — it is a **narrative tutorial** that reads like a guide. Write it as if you're explaining to a beginner: "Here's what we're building. Let's figure out how to get there, one step at a time."

**Structure:**

1. **Start with the goal.** Show the finished clockface GIF (`final.gif`) and briefly describe what it does.
2. **For each step**, include:
   - A short explanatory text (2-5 sentences or bullet points) explaining the concept/technique
   - The **full C# source code** for this step (as a fenced code block)
   - The rendered GIF
3. **Use a conversational, teaching tone.** Not dry documentation — more like a workshop walkthrough.
4. **Bullet points are welcome** — use them to highlight key concepts, new API calls, or what to look for in the GIF.

**Template:**

```markdown
# <Clockface Name> — Step by Step

Original clockface by <author>.

## The Goal

This is what we're building:

![Final Result](final.gif)

<2-3 sentences describing the clockface: what it shows, what makes it visually interesting, what techniques it uses.>

Let's build this from scratch, one step at a time.

---

## Step 1: <Title>

<Explanatory text: what we're doing here, what concept we introduce. Written in first person plural ("we") as if guiding the reader. Can use bullet points to highlight key concepts.>

` ` `csharp
// the full C# code for this step
` ` `

![Step 1](step-01.gif)

## Step 2: <Title>

<Explanatory text: what's new, why we need it, how it builds on the previous step. Point out what changed visually.>

- Key point about the new concept
- What to notice in the animation

` ` `csharp
// the full C# code for this step
` ` `

![Step 2](step-02.gif)

...

## Step N: The Complete Clockface

<Wrap-up: what we've built, how all the pieces come together.>

` ` `csharp
// the complete clockface code
` ` `

![Complete](step-NN.gif)
```

**Example tone (for a rain clockface):**

> ## Step 3: Make the rain move
>
> Static lines aren't very rainy. We want them to fall downward over time.
>
> - We use `ctx.Now.Second` to get the current second
> - Each line's Y position shifts by `step = second % 24`
> - This gives us a simple scrolling animation — the lines "fall" every second
>
> ```csharp
> var scene = (DrawingContext ctx) =>
> {
>     var step = ctx.Now.Second % 24;
>     for (var i = 0; i < 24; i++)
>         ctx.DrawLine(i, step, i, step + 3, color: Colors.Blue);
> };
> ```
>
> Watch how the lines now scroll down the display.

### 7. Verify

After all steps are rendered, review the GIF sequence to confirm:
- Each step is visually distinct from the previous one
- The progression makes sense (simple → complex)
- The final step matches the original clockface

Report to the user: how many steps were created, with a one-line summary of each.

## Output Structure

```
tutorials/<ClockfaceName>/
├── final.gif           # The complete clockface (rendered from original)
├── step-01.cs
├── step-01.gif
├── step-02.cs
├── step-02.gif
├── ...
├── step-NN.cs          # Final step = complete clockface
├── step-NN.gif
└── README.md           # Narrative tutorial with embedded GIFs
```

## Error Handling

- **Compilation errors:** Fix the script and re-render. Common issues: missing semicolons, wrong method signatures, wrong types. Consult `llms.txt` in the repository root.
- **Render tool not available:** Run `dotnet tool restore` in the pxl-software repo directory.
- **Clockface not found:** List available clockfaces from `apps/clockFaces/` and ask the user.
