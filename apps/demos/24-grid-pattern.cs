// ---
// app: Demo24GridPattern
// displayName: Drawing Grid Pattern
// author: Cumin & Potato
// ---
// Example 24: Drawing Grid Pattern
// Use loops to create repetitive patterns

#:package Pxl@0.0.54

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    var time = ctx.Elapsed.TotalSeconds;

    ctx.DrawBackground(Colors.Black);

    // Draw a grid of animated circles
    for (var row = 0; row < 4; row++)
    {
        for (var col = 0; col < 4; col++)
        {
            var x = 3 + col * 6;
            var y = 3 + row * 6;

            // Animate radius based on position and time
            var phase = (row + col) * 0.5;
            var radius = 1.5 + Math.Sin(time * 2 + phase) * 0.5;

            // Animate color hue based on position
            var hue = ((row * 4 + col) / 16.0 + time * 0.1) % 1.0;
            var color = Color.FromHsl(hue, 1.0, 0.5);

            ctx.DrawCircle(x, y, radius, colorFill: color);
        }
    }
};

