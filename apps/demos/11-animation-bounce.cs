// ---
// app: Demo11AnimationBounce
// displayName: Bouncing Animation
// author: Cumin & Potato
// ---
// Example 11: Bouncing Animation
// A ball that bounces back and forth

#:package Pxl@0.0.58

using Pxl.Ui.CSharp;

double x = 12;
double dx = 0.3;  // Velocity

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.DarkBlue);

    // Draw the bouncing ball
    ctx.DrawCircle(x, 12, 4, colorFill: Colors.Yellow);

    // Update position
    x += dx;

    // Bounce off walls
    if (x >= ctx.Width - 4 || x <= 4)
        dx = -dx;
};

