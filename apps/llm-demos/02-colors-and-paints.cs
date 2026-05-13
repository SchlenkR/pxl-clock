// ---
// app: LlmDemo02ColorsAndPaints
// displayName: Colors and Paints
// author: Cumin & Potato
// description: Color factories (RGB / HSL / byte), implicit Color → Paint conversion, gradients
// ---

// INTENT: Colors are a frequent source of LLM hallucination ("Color.FromHex",
// "Color.Lerp", etc). This demo enumerates the actual factory methods, the
// implicit Color→Paint conversion that lets you pass `Colors.Red` wherever a
// `Paint` is expected, and the `Paints.*` factories for gradients.

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (RasterSurface ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    // ===== Color factory methods =====
    // All Color values are immutable structs — every "modifier" returns a NEW Color.

    // Doubles 0.0 .. 1.0 (preferred — matches the rest of the API)
    var rgb     = Color.FromRgb(0.9, 0.2, 0.4);
    var rgba    = Color.FromRgba(0.9, 0.2, 0.4, 0.5);   // 50% alpha
    var hsl     = Color.FromHsl(0.6, 0.8, 0.5);          // hue/sat/light, all 0..1
    var hsl360  = Color.FromHsl360(220, 0.8, 0.5);       // hue 0..360 — often more readable
    var hsv360  = Color.FromHsv360(120, 0.7, 0.9);

    // Bytes 0 .. 255 (handy when copying RGB values from a designer/tool)
    var byteCol = Color.FromRgbByte(255, 100, 0);

    // Modifiers — chain to derive variants
    var dim     = Colors.Cyan.WithAlpha(0.3);            // half-transparent cyan

    // Use them. Color → Paint is implicit, no cast needed.
    ctx.DrawRectXyWh(1, 1, 4, 4, colorFill: rgb);
    ctx.DrawRectXyWh(6, 1, 4, 4, colorFill: rgba);       // partial alpha over black
    ctx.DrawRectXyWh(11, 1, 4, 4, colorFill: hsl360);
    ctx.DrawRectXyWh(16, 1, 4, 4, colorFill: hsv360);
    ctx.DrawRectXyWh(1, 6, 4, 4, colorFill: byteCol);
    ctx.DrawRectXyWh(6, 6, 4, 4, colorFill: dim);

    // ===== Predefined colors =====
    // 140+ named colors live on the static `Colors` class (CSS / X11 names).
    // Common ones: Black, White, Red, Green, Blue, Yellow, Cyan, Magenta,
    // Orange, Purple, Pink, Lime, Gold, Silver, Gray + dark variants.
    ctx.DrawRectXyWh(11, 6, 4, 4, colorFill: Colors.DodgerBlue);
    ctx.DrawRectXyWh(16, 6, 4, 4, colorFill: Colors.Crimson);

    // ===== Paints (gradients) =====
    // `Paint?` parameters accept either a `Color` (implicit) or a `Paint` from
    // the `Paints` factory. Use Paints for gradients, shaders, image patterns.
    var horiz = Paints.HorizontalGradient(24, Colors.Red, Colors.Blue);
    ctx.DrawRectXyWh(0, 12, 24, 4, colorFill: horiz);

    var radial = Paints.RadialGradient((12, 20), 6, new[] { Colors.White, Colors.Black });
    ctx.DrawCircle(12, 20, 4, colorFill: radial);
};
