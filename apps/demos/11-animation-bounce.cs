// Example 11: Bouncing Animation
// A ball that bounces back and forth

#:package Pxl@0.0.44

using Pxl.Ui.CSharp;

double x = 12;
double dx = 0.3;  // Velocity

var scene = PxlApp.CreateScene(ctx =>
{
    ctx.DrawBackground(Colors.DarkBlue);

    // Draw the bouncing ball
    ctx.DrawCircle(x, 12, 4, colorFill: Colors.Yellow);

    // Update position
    x += dx;

    // Bounce off walls
    if (x >= ctx.Width - 4 || x <= 4)
        dx = -dx;
});

await PxlApp.SimulateAndSendToDevice(scene);
