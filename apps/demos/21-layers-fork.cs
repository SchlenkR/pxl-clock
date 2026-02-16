// Example 21: Fork Layer (Copy Content)
// Fork creates a layer with a copy of the current canvas

#:package Pxl@0.0.46

using Pxl.Ui.CSharp;
using SkiaSharp;

var scene = PxlApp.CreateScene(ctx =>
{
    // Draw some content
    ctx.DrawBackground(Colors.DarkGray);
    ctx.DrawCircle(12, 12, 8, colorFill: Colors.Blue);
    ctx.DrawTextMono4x5("PXL", 5, 10, Colors.White);

    // Fork creates a copy of the current state
    var forked = ctx.Fork();

    // Modify the forked layer (invert-like effect)
    var pixels = forked.Pixels;
    foreach (var cell in pixels.Cells)
    {
        // Shift colors (swap RGB channels)
        var c = cell.Color;
        pixels[cell.X, cell.Y] = Color.FromRgb(c.G, c.B, c.R);
    }

    // Apply with difference blend for interesting effect
    forked.Apply(SKBlendMode.Difference);
});

await PxlApp.SimulateAndSendToDevice(scene);
