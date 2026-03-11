// ---
// app: Demo13AnimationColors
// displayName: Color Animation
// author: Cumin & Potato
// ---
// Example 13: Color Animation
// Animate colors using HSL for smooth hue transitions

#:package Pxl@0.0.58

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    var time = ctx.Elapsed.TotalSeconds;

    // Animate hue through the color spectrum (0.0 to 1.0)
    var hue = (time * 0.1) % 1.0;

    // Create color from animated hue
    var animatedColor = Color.FromHsl(hue, 1.0, 0.5);

    // Fill background with animated color
    ctx.DrawBackground(animatedColor);

    // Draw some shapes with complementary colors
    var complementaryHue = (hue + 0.5) % 1.0;
    var complementary = Color.FromHsl(complementaryHue, 1.0, 0.5);

    ctx.DrawCircle(12, 12, 8, colorFill: complementary);
};

