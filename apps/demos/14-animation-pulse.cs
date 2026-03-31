// ---
// app: Demo14AnimationPulse
// displayName: Pulsing Animation
// author: Cumin & Potato
// ---
// Example 14: Pulsing Animation
// Animate size and opacity for a pulsing effect

#:package Pxl@0.0.64

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    var time = ctx.Elapsed.TotalSeconds;

    // Create a pulsing effect using sine
    var pulse = (Math.Sin(time * 3) + 1) / 2;  // 0 to 1

    // Animate radius
    var radius = 3 + pulse * 5;

    // Animate color intensity
    var color = Color.FromRgb(pulse, 0, 1 - pulse);

    // Draw the pulsing circle
    ctx.DrawCircle(12, 12, radius, colorFill: color);

    // Draw outer glow rings
    for (var i = 1; i <= 3; i++)
    {
        var alpha = (1 - pulse) * 0.3 / i;
        ctx.DrawCircle(
            12, 12, radius + i * 2,
            colorFill: null,
            colorStroke: Color.FromRgba(1, 1, 1, alpha),
            strokeWidth: 1);
    }
};

