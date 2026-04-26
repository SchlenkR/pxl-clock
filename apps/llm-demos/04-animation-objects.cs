// ---
// app: LlmDemo04AnimationObjects
// displayName: Animation Objects
// author: Cumin & Potato
// description: The Animate factory — Linear, EaseInOut, PingPong, ToggleValues. State outside, .Eval inside.
// ---

// INTENT: For most parameter-over-time animation, the `Animate` factory is
// cleaner than raw `Math.Sin(ctx.Elapsed...)`: easing curves are predefined,
// repeat behaviour is declarative, and the `.Eval(ctx)` result is cached per
// frame so calling it multiple times is free.
//
// Crucial pattern: animation OBJECTS are state — declare them OUTSIDE the
// scene lambda so they survive across frames. If you `new` them inside, you'd
// reset them every frame and they'd never advance.

#:package Pxl@*

using Pxl.Ui.CSharp;

// === STATE (outside scene — persists across frames) ===

// Linear motion, 2 second cycle, ping-pongs between x=2 and x=20.
var bouncerX = Animate.EaseInOut(2.0, 2, 20, repeat: Repeat.PingPong);

// Pulse the radius with sine easing — feels organic for "breathing" effects.
var breath = Animate.EaseInOutSine(1.5, 1.5, 4.0, repeat: Repeat.PingPong);

// Discrete cycle through a colour palette — each value held for 0.5s.
var colorCycle = Animate.ToggleValues(0.5,
    Colors.Red, Colors.Orange, Colors.Yellow,
    Colors.Lime, Colors.Cyan, Colors.Magenta);

// One-shot fade-in (Repeat.Once is the default; included here for clarity).
var fadeIn = Animate.EaseOut(3.0, 0.0, 1.0, repeat: Repeat.Once);

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    // .Eval(ctx) returns the current value, caching it per frame.
    // Safe to call multiple times — second call is free.
    var x = bouncerX.Eval(ctx);
    var r = breath.Eval(ctx);
    var col = colorCycle.Eval(ctx);
    var alpha = fadeIn.Eval(ctx);

    // Draw a breathing, ping-ponging, colour-cycling circle that fades in.
    ctx.DrawCircle(x, 12, r, colorFill: col.WithAlpha(alpha));

    // Inspect properties without calling Eval again — the API caches them.
    // bouncerX.Value      → current double
    // bouncerX.ValueInt   → current int (rounded)
    // bouncerX.IsRunning  → bool
    // bouncerX.IsAtEnd    → bool (only meaningful for Repeat.Once)

    // Programmatic control if you need to gate an animation:
    // bouncerX.Pause(); bouncerX.Resume(); bouncerX.Restart();

    // Show progress of the one-shot fade as a thin status bar.
    ctx.DrawRectXyWh(0, 22, alpha * 24, 2, colorFill: Colors.White);
};
