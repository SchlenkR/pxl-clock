// ---
// app: LlmDemo12NativeRasterPaints
// displayName: Native Raster Paints
// author: Cumin & Potato
// description: Uses only the managed Pxl paint API after the Skia escape hatch was removed
// ---

// INTENT: The renderer is now pure managed rasterization. Advanced Skia escape
// hatches were intentionally removed, so custom looks should be composed from
// Pxl paints, layers, transforms, pixels, and blend modes.

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (RasterSurface ctx) =>
{
    ctx.DrawBackground(Colors.Black);
    var t = ctx.Elapsed.TotalSeconds;

    var mirrored = Paints.LinearGradient(
        (0, 0),
        (8, 0),
        new[] { Colors.Red, Colors.Blue, Colors.Lime },
        tileMode: TileMode.Mirror);
    ctx.DrawRectXyWh(0, 0, 24, 8, colorFill: mirrored);

    var fade = Paints.VerticalGradient(8, Colors.Black, Colors.DarkBlue);
    ctx.DrawRectXyWh(0, 8, 24, 8, colorFill: fade);

    var wheel = Paints.SweepGradient(
        (12, 12),
        new[] { Colors.Magenta, Colors.Cyan, Colors.Yellow, Colors.Magenta },
        localTransform: Transform2D.Rotate(t * 80, 12, 12));
    var glow = ctx.NewLayer(clearColor: Colors.Transparent);
    glow.DrawCircle(12, 12, 7, colorFill: wheel);
    glow.Apply(BlendMode.Plus);

    var pulse = 4 + Math.Sin(t * 3) * 1.5;
    var light = ctx.NewLayer(clearColor: Colors.Transparent);
    light.DrawCircle(12, 20, pulse, colorFill: Colors.Yellow.WithAlpha(0.8));
    light.Apply(BlendMode.Lighten);
};