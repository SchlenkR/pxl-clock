// ---
// app: Demo05Colors
// displayName: Working with Colors
// author: Cumin & Potato
// ---
// Example 05: Working with Colors
// Different ways to create and use colors

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (RasterSurface ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    // Using predefined colors
    ctx.DrawRectXyWh(1, 1, 5, 5, colorFill: Colors.Red);
    ctx.DrawRectXyWh(7, 1, 5, 5, colorFill: Colors.Lime);
    ctx.DrawRectXyWh(13, 1, 5, 5, colorFill: Colors.Blue);

    // Create color from RGB doubles (0.0-1.0)
    var customColor1 = Color.FromRgb(1.0, 0.5, 0.0); // Orange
    ctx.DrawRectXyWh(1, 8, 5, 5, colorFill: customColor1);

    // Another way with RGB
    var customColor2 = Color.FromRgb(0.5, 0.0, 0.5); // Purple
    ctx.DrawRectXyWh(7, 8, 5, 5, colorFill: customColor2);

    // Create color with alpha (transparency)
    var semiTransparent = Color.FromRgba(1.0, 1.0, 0.0, 0.5); // 50% yellow
    ctx.DrawRectXyWh(10, 6, 8, 8, colorFill: semiTransparent);

    // Create color from HSL (hue, saturation, lightness)
    var hslColor = Color.FromHsl(0.6, 1.0, 0.5); // Cyan-ish
    ctx.DrawRectXyWh(1, 15, 5, 5, colorFill: hslColor);
};

