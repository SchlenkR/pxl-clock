// ---
// app: Demo09TextWithGradients
// displayName: Text with Gradients
// author: Cumin & Potato
// ---
// Example 09: Text with Gradients
// Combine text with gradient paints

#:package Pxl@0.0.61

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    // Text with horizontal gradient
    ctx.DrawTextMono4x5(
        "RAINBOW", 0, 0,
        Paints.LinearGradient(
            (0, 0), (24, 0),
            [Colors.Red, Colors.Yellow, Colors.Green, Colors.Cyan, Colors.Blue]
        ));

    // Text with vertical gradient
    ctx.DrawTextMono4x5(
        "SUNSET", 0, 8,
        Paints.LinearGradient(
            (0, 8), (0, 13),
            [Colors.Yellow, Colors.Orange, Colors.Red]
        ));

    // Text with radial gradient
    ctx.DrawTextMono4x5(
        "GLOW", 4, 16,
        Paints.RadialGradient(
            (12, 19), 10,
            [Colors.White, Colors.Purple]
        ));
};

