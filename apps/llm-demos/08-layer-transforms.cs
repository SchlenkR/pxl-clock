// ---
// app: LlmDemo08LayerTransforms
// displayName: Layer Transforms
// author: Cumin & Potato
// description: Translate, Scale, Rotate (around center vs specific point), and Interpolation
// ---

// INTENT: To rotate, scale, or translate a group of draws as a unit, draw them
// onto a layer and apply transforms to the layer. Transforms ACCUMULATE on the
// layer — they're not pushed/popped like a matrix stack — and are applied
// together when you call `.Apply(...)`.
//
// For pixel art, almost always pass `Interpolation.NearestNeighbor` to Apply
// (this is the default) — `Interpolation.Linear` introduces blurry tweens
// between source and destination pixels.

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (RasterSurface ctx) =>
{
    ctx.DrawBackground(Colors.Black);
    var t = ctx.Elapsed.TotalSeconds;

    // ===== Rotate around layer center =====
    // We draw a small "+" cross on a layer, then spin it. With a single argument
    // Rotate(degrees) rotates around the LAYER CENTER (12, 12 for 24x24).
    var spinner = ctx.NewLayer(clearColor: Colors.Transparent);
    spinner.DrawLine(12, 9, 12, 15, color: Colors.Cyan);
    spinner.DrawLine(9, 12, 15, 12, color: Colors.Cyan);
    spinner.Rotate(t * 90);   // 90 deg / second
    spinner.Apply();

    // ===== Rotate around a specific point =====
    // Rotate(degrees, x, y) rotates around (x, y). Useful for orbiting one
    // element around another.
    var orbit = ctx.NewLayer(clearColor: Colors.Transparent);
    orbit.DrawCircle(20, 6, 1.5, colorFill: Colors.Yellow);   // small body offset from center
    orbit.Rotate(t * 60, 12, 6);                              // orbit around (12, 6)
    orbit.Apply();

    // ===== Scale =====
    // Scale(s)        — uniform scale around (0,0)
    // Scale(sx, sy)   — non-uniform scale
    // The scale origin is the layer's (0,0) — translate first to scale around
    // a different point.
    var scaler = ctx.NewLayer(clearColor: Colors.Transparent);
    scaler.DrawRectXyWh(0, 0, 4, 4, colorFill: Colors.Magenta);
    var s = 1 + (Math.Sin(t * 2) + 1);   // 1 .. 3
    scaler.Translate(4, 18);             // place
    scaler.Scale(s, 1);                  // grow horizontally only
    scaler.Apply(interpolation: Interpolation.NearestNeighbor);

    // ===== Combined: translate + rotate + scale =====
    // Order matters — transforms compose in order they're called. The
    // intuitive read is "right-to-left" (innermost first), like a matrix stack.
    // Below: scale FIRST, then rotate, then translate to final position.
    var combo = ctx.NewLayer(clearColor: Colors.Transparent);
    combo.DrawRectXyXy(-2, -1, 2, 1, colorFill: Colors.Lime);  // 4x2 bar centered on (0,0)
    combo.Scale(1.5, 0.7);
    combo.Rotate(t * 120);
    combo.Translate(18, 19);
    combo.Apply();
};
