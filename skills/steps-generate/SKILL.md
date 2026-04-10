---
name: steps-generate
description: Decomposes a PXL Clock clockface into progressive learning steps. Creates a C# script, an explainer script, and description files per step. Does NOT render GIFs.
argument-hint: ClockfaceName
disable-model-invocation: true
user-invocable: true
---

# Steps Generate

Decomposes an existing PXL Clock clockface into a progressive sequence of learning steps. Produces per step: a compilable `.cs` file, a display-only `-explainer.cs`, and a `.short.md` for the video overlay.

**This skill does NOT render GIFs.** That is a separate step.

**All output text (descriptions, code comments) must be in English.**

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

The full API reference is in `llms.txt` at the `pxl-clock` repo root.

## Variables

```
CLOCKFACE_NAME = $ARGUMENTS
SOURCE_PATH    = <provided via prompt>
OUTPUT_DIR     = <provided via prompt>
REPO           = /Users/ronald/repos/github.pxl/pxl-tutorial-maker
PXL_CLOCK_REPO = /Users/ronald/repos/github.pxl/pxl-clock
```

## Task

### 1. Read the clockface

Read the source file at `SOURCE_PATH`. Also read `$PXL_CLOCK_REPO/llms.txt` for the full API reference.

### 2. Analyze concepts and dependencies

Identify the concepts used in the clockface and **sort them by dependency** — which concept requires which other concept to make sense visually?

Common concepts (not exhaustive):
- Background rendering
- Color creation (solid, HSV, gradients)
- Shape drawing (lines, circles, rectangles, arcs)
- Text rendering (time display, different fonts)
- Pixel-level access
- Animation (time-based movement, sin/cos, easing)
- State management (variables persisting across frames)
- Algorithms (cellular automata, particle systems, etc.)
- Layers & blend modes
- Helper functions / classes

**The dependency order directly determines the step order.** A concept that depends on another must come after it. Example: "rainbow colors along the body" depends on "the body exists" — so the body shape must come first, colors second.

### 3. Plan the steps

Design a progression from simplest building block to complete clockface. If the prompt specifies a **minimum step count**, create at least that many steps (more is fine if the clockface warrants it). If no minimum is given (or it is -1), decide the right number yourself based on the complexity of the clockface — typically 5–12 steps.

Each step must:
- Be **self-contained** — compiles and renders as a standalone script
- Be **visually interesting** on its own
- Build **logically** toward the final result

**Step planning guidelines:**
1. **Step 01** — The simplest visible element on a black background. Must show something recognizable: a shape, a line, a dot, text. **Never** just a background color — that's a black/colored screen with nothing on it, boring and teaches nothing.
2. **Middle steps** — Add complexity one layer at a time: motion, time-based behavior, more elements, color variation, state. **Each step must clearly transform or extend the previous step's visual output.** The viewer should look at step N and immediately see "ah, that's step N-1 but now with X added/changed."
3. **Final step** — The complete clockface. Must match the original source exactly (formatting may be cleaned up, logic must be equivalent).

**The one-change rule for linear steps:**

When a step builds on the previous one (pattern 1 below), it must change **exactly one essential thing**. Not two, not three — one. What counts as "one essential thing" requires judgment: adding a `DrawLine` call is one thing; adding `DrawLine` + changing the background color + introducing a new variable is three things. Before writing a linear step, ask yourself: "Can I explain what changed in a single sentence without 'and'?" If not, split it into multiple steps.

This rule does NOT apply to combination steps (pattern 2 below) — when you merge previously isolated concepts, naturally many things change at once. That's fine because each concept was already understood individually.

**Two valid step patterns:**

1. **Linear build-up** — Step N extends step N-1 directly. The viewer sees everything from before, plus **one** new thing. Example: step 3 had a static line → step 4 makes it move. That's one change.
2. **Isolate, then combine** — When a new concept is complex enough, **step back** and show it on its own first. Use a simple, minimal scene (e.g. just a background + the new concept) so the viewer can focus on understanding it without distraction. Then in a later step, combine it with what was built before: "We take the rainbow colors from step 2 and apply them to the moving body from step 3." In the combination step, many things change — that's expected and OK.

**Pattern 2 is preferred when it makes learning easier.** Don't keep piling changes onto an increasingly complex scene if a concept deserves its own spotlight. Stepping back to a clean slate for one step, then combining, is a powerful teaching tool — it shows the concept in isolation, then proves it works in context.

Both patterns can be mixed freely. The key is: **every step must either build on the previous one (changing one thing) OR introduce a concept in isolation that will be visibly combined later.** The viewer must always understand how this step fits into the bigger picture.

**Avoid:**
- Empty/black screen or just-a-background-color as step 01
- Steps that look identical to the previous one (invisible changes)
- Linear steps that change more than one essential thing — split them
- Orphan concepts — if you introduce something in isolation, it MUST be combined with other concepts in a later step
- A concept that never connects back to the rest of the clockface

### 4. Create the step C# files

Write all files into `OUTPUT_DIR/`.

For each step, create `step-NN.cs`:

```csharp
// ---
// app: <ClockfaceName>Step<NN>
// displayName: <Clockface Name> - Step <N>
// author: Tutorial
// ---
// Step N: <Short title>
// <One-line description of what this step introduces>

#:package Pxl@*

using Pxl.Ui.CSharp;

// State variables (if any) go here, before the scene lambda

var scene = (DrawingContext ctx) =>
{
    // rendering code
};
```

**Conventions:**
- **Every script MUST start with a YAML frontmatter block** (`// ---` ... `// ---`). Without it, `Pxl.Render` will refuse to compile. The `app` field must be a valid C# identifier (no spaces/hyphens).
- Always use `#:package Pxl@*` (NOT `#:project`)
- The `var scene` lambda is the only entry point
- State variables go **before** the scene lambda
- Keep code clean and well-commented — these are learning materials
- Zero-pad step numbers: `step-01.cs`, `step-02.cs`, ..., `step-12.cs`

### 4b. Create the explainer C# files

For each step, also create `step-NN-explainer.cs`. This is a **display-only** version of the code shown in the video overlay. It highlights only the essential/new parts introduced in this step. Everything else (boilerplate, unchanged code from previous steps) is replaced with `// ...` comments.

The explainer file is **not** compilable — it exists purely for visual presentation in the reel.

**Rules:**
- Strip the YAML frontmatter block (`// ---` ... `// ---`)
- Strip `#:package` and `using` directives
- Strip the `var scene = (DrawingContext ctx) => {` wrapper and closing `};` — show only the body
- Replace unchanged/boilerplate sections with a single `// ...` line
- Keep the new/changed code exactly as in the full `.cs` file
- If there are state variables (before the scene lambda) that are new in this step, include them at the top
- Use `// ...` (with a space) as the ellipsis marker — not `//...` or `...`
- Keep the code compact but readable — the viewer sees this on screen for ~4 seconds. Aim for **max 20 lines**. If the new code is longer, show only the most important parts.
- **Max 50 characters per line.** The code is displayed in a monospace font on a vertical (9:16) video — lines longer than 50 characters get cut off. If a line is too long, break it using standard C# line-continuation style (e.g. break after `(`, `,`, or an operator, indent the continuation by 4 spaces). Shorten variable names in the explainer if needed — it's display-only, not compilable.

**Example 1 — everything is new** (step 3 adds HSV color cycling):

Explainer `step-03-explainer.cs`:
```csharp
var hue = (float)(ctx.Now.TimeOfDay.TotalSeconds * 30 % 360);
var color = Color.FromHsv(hue, 1f, 1f);
ctx.DrawLine(0, 12, 23, 12, color);
```

**Example 2 — change in the middle of existing code** (step 5 adds bounce logic inside an existing movement block):

Explainer `step-05-explainer.cs`:
```csharp
// ...
if (x < 0 || x >= 24) dx = -dx;
if (y < 0 || y >= 24) dy = -dy;
x += dx;
y += dy;
// ...
```

Use `// ...` at top and/or bottom to show that surrounding code exists but is unchanged.

### 5. Write descriptions for each step

For each step, create a description file alongside the `.cs` file.

**`step-NN.short.md`** — 3 sentences / short paragraphs (blank line between each). This text appears as a typewriter overlay in the reel video. It must cover **two things**:

1. **What happens visually** — what the viewer sees on the display, how it differs from the previous step
2. **How it's done technically** — which API, technique, or concept makes it work (e.g. "HSV color cycling", "sin-based oscillation", "a queue that tracks positions")

Separate each sentence into its own paragraph for readability in the video overlay. Always reference the previous step to make the progression clear (except step 1, which has no predecessor).

**Writing style:** Direct, conversational, practical. Use short punchy sentences. Name the concrete thing — "HSV cycling", "a Queue<Point>", "sin-based oscillation" — not vague abstractions. The tone is a developer explaining to another developer what's cool about this step.

**Inline code formatting:** Wrap source code identifiers, API calls, mathematical expressions, and color values in Markdown backticks (`` ` ``). This includes: method names (`DrawLine`), types (`Color`, `List<Point>`), variables (`vX`, `segCount`), numeric constants (`0.5f`), math formulas (`sqrt(4 - vX²)`), and color references (`Color.FromRgbByte(255, 0, 0)`). Do NOT backtick-wrap plain English words or general concepts — only things that are literally code, math, or color values.

**DON'T:**
> This step adds beautiful color effects to create a stunning visual experience on the display.

> We enhance the animation with smooth transitions and elegant movement patterns.

(Vague, marketing-speak, no technical substance, no connection to previous step.)

**DO — step 1** (no predecessor):
> A horizontal red line cuts across the center of a black canvas.
>
> Just one call — `DrawLine` — but it's the backbone the worm will grow from.

**DO — step 4** (building on step 3):
> The solid red worm from step 3 explodes into a flowing rainbow — each segment a different color.
>
> HSV color cycling via `Color.FromHsv` assigns each body part a unique hue based on its index.
>
> The palette cycles through 30 hues, rippling along the body as it moves.

### 6. Verify

After writing all files, confirm:
- All `step-NN.cs`, `step-NN-explainer.cs`, `step-NN.short.md` files exist in `OUTPUT_DIR/`
- The final step's code is equivalent to the original clockface

Report: number of steps created, one-line summary of each step's title.

## Output Structure

```
OUTPUT_DIR/
├── step-01.cs
├── step-01-explainer.cs
├── step-01.short.md
├── step-02.cs
├── step-02-explainer.cs
├── step-02.short.md
├── ...
├── step-NN.cs               ← final step = complete clockface
├── step-NN-explainer.cs
└── step-NN.short.md
```

## Error Handling

- **Compilation can't be verified here** — downstream GIF render will catch issues.
- **Clockface not found:** List available files from the apps directory and ask the user to pick one.
- **API questions:** Consult `$PXL_CLOCK_REPO/llms.txt` for the complete `Pxl.Ui.CSharp` API.
