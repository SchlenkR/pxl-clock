// ---
// app: LlmDemo12SkiaShaderEscapeHatch
// displayName: SkiaSharp Shader Escape Hatch
// author: Cumin & Potato
// description: Drop down to SkiaSharp when Pxl.Paints isn't enough — Color/SKColor implicit conversion, Paints.Shader(SKShader)
// ---

// INTENT: The Pxl renderer is built on SkiaSharp, and the wrapper exposes
// just enough Skia for an escape hatch when you need a shader the `Paints`
// factory doesn't cover (composed shaders, runtime SKSL, image shaders,
// custom tile modes, etc.).
//
// Three things bridge the layers:
//   • Color   ↔ SKColor       — implicit conversion both ways (no cast needed)
//   • BlendMode ↔ SKBlendMode — explicit `.ToSkia()` / `BlendMode.FromSkia(...)`
//   • Paints.Shader(SKShader) — wraps any SKShader in a Pxl `Paint`
//
// `using SkiaSharp;` is allowed (not in the PXL1021 forbidden list).

#:package Pxl@*

using Pxl.Ui.CSharp;
using SkiaSharp;            // ← required for SK* types

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);
    var t = ctx.Elapsed.TotalSeconds;

    // ===== 1. Implicit Color ↔ SKColor =====
    // Anywhere that takes SKColor, you can pass a Color (and vice versa).
    SKColor[] stops = new SKColor[] { Colors.Red, Colors.Blue, Colors.Lime };

    // ===== 2. SkiaSharp shader as a Paint =====
    // Build a Skia shader directly — gives access to options the Pxl Paints
    // factory doesn't expose (here: TileMode.Mirror for a reflected gradient).
    var mirrored = SKShader.CreateLinearGradient(
        new SKPoint(0, 0),
        new SKPoint(8, 0),                // 8-pixel period
        stops,
        SKShaderTileMode.Mirror);          // bounces back and forth across the canvas
    ctx.DrawRectXyWh(0, 0, 24, 8, colorFill: Paints.Shader(mirrored));

    // ===== 3. Composing two Skia shaders =====
    // SKShader.CreateCompose lets you blend shaders directly at the Skia layer.
    // Here: a slow-rotating sweep blended over a vertical fade.
    var fade = SKShader.CreateLinearGradient(
        new SKPoint(0, 8), new SKPoint(0, 16),
        new SKColor[] { Colors.Black, Colors.DarkBlue },
        SKShaderTileMode.Clamp);

    var spin = SKShader.CreateSweepGradient(
        new SKPoint(12, 12),
        new SKColor[] { Colors.Magenta, Colors.Cyan, Colors.Magenta });

    var composed = SKShader.CreateCompose(fade, spin, SKBlendMode.Plus);
    ctx.DrawRectXyWh(0, 8, 24, 8, colorFill: Paints.Shader(composed));

    // ===== 4. BlendMode bridge =====
    // The Pxl `BlendMode` enum mirrors `SKBlendMode` exactly — every Pxl
    // value has a Skia counterpart and vice versa. Convert when you need to
    // mix Pxl and Skia code:
    SKBlendMode skMode = BlendMode.Plus.ToSkia();          // Pxl → Skia
    BlendMode pxMode = BlendMode.FromSkia(SKBlendMode.Lighten);  // Skia → Pxl

    // Use the converted Pxl mode on a layer's Apply (just to show round-tripping).
    var glow = ctx.NewLayer(clearColor: Colors.Transparent);
    glow.DrawCircle(12, 20, 4 + Math.Sin(t * 3) * 1.5, colorFill: Colors.Yellow.WithAlpha(0.8));
    glow.Apply(pxMode);

    // ===== 5. The same trick with SKTypeface =====
    // `Fonts.Var4x5.Typeface` is an `SKTypeface` — feed it into any Skia API
    // that needs a typeface (custom text shaping, complex layouts, etc.).
    //   SKTypeface tf = Fonts.Var4x5.Typeface;
};
