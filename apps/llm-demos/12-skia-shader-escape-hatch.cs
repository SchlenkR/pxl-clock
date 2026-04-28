// ---
// app: LlmDemo12SkiaShaderEscapeHatch
// displayName: SkiaSharp Shader Escape Hatch
// author: Cumin & Potato
// description: Drop down to SkiaSharp via Pxl.Ui.CSharp.Advanced when no Pxl primitive does what you need
// ---

// INTENT: The Pxl renderer is built on SkiaSharp, but the everyday Pxl API
// (Color, BlendMode, Paints, Typeface, ...) hides Skia entirely. When you need
// a Skia construct that Pxl doesn't expose (composed shaders, runtime SKSL,
// custom blend modes, exotic tile modes), opt in via the
// `Pxl.Ui.CSharp.Advanced` namespace — that namespace itself signals "I'm
// coupling to SkiaSharp now".
//
// What's available there:
//   • SkiaInterop.ToSkia()/ToPxl() — convert Color, BlendMode, Typeface
//   • AdvancedPaints.Shader(SKShader) — wrap any SKShader as a Pxl Paint

#:package Pxl@*

using Pxl.Ui.CSharp;
using Pxl.Ui.CSharp.Advanced;   // ← opt in to Skia-coupled helpers
using SkiaSharp;                 // ← required for SK* types

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);
    var t = ctx.Elapsed.TotalSeconds;

    // ===== 1. Color → SKColor via SkiaInterop =====
    // No more implicit conversion — call .ToSkia() explicitly.
    SKColor[] stops = new[] { Colors.Red.ToSkia(), Colors.Blue.ToSkia(), Colors.Lime.ToSkia() };

    // ===== 2. SkiaSharp shader as a Paint via AdvancedPaints.Shader =====
    // Build a Skia shader directly when the Pxl Paints factory doesn't
    // expose what you need (here: a mirrored tile mode that bounces back).
    var mirrored = SKShader.CreateLinearGradient(
        new SKPoint(0, 0),
        new SKPoint(8, 0),                // 8-pixel period
        stops,
        SKShaderTileMode.Mirror);
    ctx.DrawRectXyWh(0, 0, 24, 8, colorFill: AdvancedPaints.Shader(mirrored));

    // ===== 3. Composing two Skia shaders =====
    // SKShader.CreateCompose blends shaders at the Skia layer — a slow-
    // rotating sweep blended over a vertical fade.
    var fade = SKShader.CreateLinearGradient(
        new SKPoint(0, 8), new SKPoint(0, 16),
        new[] { Colors.Black.ToSkia(), Colors.DarkBlue.ToSkia() },
        SKShaderTileMode.Clamp);

    var spin = SKShader.CreateSweepGradient(
        new SKPoint(12, 12),
        new[] { Colors.Magenta.ToSkia(), Colors.Cyan.ToSkia(), Colors.Magenta.ToSkia() });

    var composed = SKShader.CreateCompose(fade, spin, SKBlendMode.Plus);
    ctx.DrawRectXyWh(0, 8, 24, 8, colorFill: AdvancedPaints.Shader(composed));

    // ===== 4. BlendMode bridge via SkiaInterop =====
    // Pxl `BlendMode` and Skia `SKBlendMode` map 1:1 — convert either way.
    SKBlendMode skMode = BlendMode.Plus.ToSkia();
    BlendMode   pxMode = SKBlendMode.Lighten.ToPxl();

    // Use the converted Pxl mode on a layer's Apply (just to show round-tripping).
    var glow = ctx.NewLayer(clearColor: Colors.Transparent);
    glow.DrawCircle(12, 20, 4 + Math.Sin(t * 3) * 1.5, colorFill: Colors.Yellow.WithAlpha(0.8));
    glow.Apply(pxMode);

    // ===== 5. The same trick for typefaces =====
    // `Fonts.Var4x5.Typeface.ToSkia()` returns an `SKTypeface` for any Skia
    // API that needs one (custom text shaping, complex layouts, etc.).
    //   SKTypeface tf = Fonts.Var4x5.Typeface.ToSkia();
};
