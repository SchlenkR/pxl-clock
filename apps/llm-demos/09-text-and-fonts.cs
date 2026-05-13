// ---
// app: LlmDemo09TextAndFonts
// displayName: Text and Fonts
// author: Cumin & Potato
// description: All built-in fonts, MeasureText for centering, text colored with a Paint
// ---

// INTENT: The 24x24 canvas is too small for arbitrary fonts — only the bundled
// pixel fonts read well. Convention:
//   `ctx.DrawTextVar*` / `ctx.DrawTextMono*`  — convenience overloads, one per font
//   `ctx.DrawText(text, x, y, font: Fonts.X)`  — generic, takes a FontInfo
//
// All text APIs default to `isAntialias: false` for crisp pixels (overriding
// the AA-by-default that shape APIs use). Don't fight that — pixel fonts look
// bad anti-aliased.
//
// Coordinates: x, y is the TOP-LEFT of the text bounding box.

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (RasterSurface ctx) =>
{
    ctx.DrawBackground(Colors.Black);
    var t = ctx.Elapsed.TotalSeconds;

    // ===== Font sampler: each row uses a different built-in font =====
    // Sizes are approximate cell footprints, monospaced (Mono*) or
    // proportional (Var*).
    ctx.DrawTextVar3x5("Var3x5",   1, 0,  color: Colors.Gray);
    ctx.DrawTextMono3x5("Mono3x5", 1, 6,  color: Colors.Gray);
    ctx.DrawTextVar4x5("Var4x5",   1, 12, color: Colors.White);  // recommended default

    // ===== Centering with MeasureText =====
    // To horizontally center, measure the text width, then x = (24 - width) / 2.
    // There's a `MeasureText(...)` for the generic API and a per-font variant
    // (MeasureTextVar4x5 etc.) — pick the one that matches your draw call.
    var label = ctx.Now.ToString("HH:mm");          // "15:42"
    var w = ctx.MeasureTextMono4x5(label);
    var x = (ctx.Width - w) / 2;                    // ctx.Width == 24
    ctx.DrawTextMono4x5(label, x, 18, color: Colors.Cyan);

    // ===== Text coloured with a Paint =====
    // The `color` parameter is `Paint?` — Color converts implicitly, but you can
    // also pass any `Paints.*` factory result, e.g. a horizontal gradient.
    var hue = (t * 0.3) % 1.0;
    var grad = Paints.HorizontalGradient(24,
        Color.FromHsl(hue, 1.0, 0.5),
        Color.FromHsl((hue + 0.33) % 1.0, 1.0, 0.5));

    // Bottom row: gradient-coloured marquee. Use ctx.CycleNo for a discrete
    // shift effect (1 char ≈ 5 px in Mono4x5).
    var msg = "PXL";
    ctx.DrawTextVar10x10(msg, 2, 14, color: grad);
};
