// ---
// app: LlmDemo03TimeAndLoops
// displayName: Time and Loops
// author: Cumin & Potato
// description: Driving motion from ctx.Elapsed (Sin/Cos), ctx.Now for clocks, ctx.CycleNo for periodic state
// ---

// INTENT: Every animated pixogram needs to react to time. The runtime exposes
// three time sources on `DrawingContext` — pick the right one for the job:
//
//   ctx.Elapsed    TimeSpan since the scene started — for smooth analogue motion
//                  (Sin/Cos based on TotalSeconds).
//   ctx.Now        Current local DateTime — for clock faces / wall-time things.
//   ctx.CycleNo    Frame counter (long, ~40 per second) — for "every Nth frame"
//                  triggers and for state changes that should be deterministic
//                  per frame (independent of wall time).
//
// This demo shows all three driving motion in one scene. No `Animate` objects —
// raw time math, useful when the motion is one-off and you don't need easing
// helpers. (For canned easings, see the next demo.)

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    // --- ctx.Elapsed: smooth oscillation ---
    // Pattern: Sin(t * speed) maps [-1..1]; remap to your range with `* amp + offset`.
    var t = ctx.Elapsed.TotalSeconds;
    var x = 12 + Math.Sin(t * 2.0) * 8;        // bob between 4 and 20, period ≈ π s
    ctx.DrawCircle(x, 6, 2, colorFill: Colors.Cyan);

    // --- ctx.Now: clock-face content ---
    // `ctx.Now.Second` is 0..59. Drive a sweep hand from it.
    var sec = ctx.Now.Second + ctx.Now.Millisecond / 1000.0;  // smooth across the second
    var angle = sec / 60.0 * Math.PI * 2 - Math.PI / 2;       // 0s = pointing up
    var hx = 12 + Math.Cos(angle) * 9;
    var hy = 12 + Math.Sin(angle) * 9;
    ctx.DrawLine(12, 12, hx, hy, color: Colors.White, isAntialias: false);

    // --- ctx.CycleNo: discrete per-frame triggers ---
    // Blink every 20 frames (roughly twice per second at 40 fps).
    if (ctx.CycleNo % 20 < 10)
        ctx.DrawCircle(20, 20, 2, colorFill: Colors.Red);
    else
        ctx.DrawCircle(20, 20, 2, colorStroke: Colors.Red, strokeWidth: 1);

    // --- Combined: rainbow background driven by elapsed time ---
    // Every pixel gets a hue derived from its position + scroll offset.
    var scrollHue = (t * 0.2) % 1.0;            // full rotation every 5 seconds
    for (int y = 18; y < 24; y++)
        for (int px = 0; px < 24; px++)
        {
            var hue = ((px + y) / 48.0 + scrollHue) % 1.0;
            ctx.SetPixel(px, y, Color.FromHsl(hue, 1.0, 0.5));
        }
};
