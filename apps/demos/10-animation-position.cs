// Example 10: Simple Position Animation
// Animate a shape's position using a variable outside the scene

#:package Pxl@0.0.42

using Pxl.Ui.CSharp;

// State variable - lives outside the scene, persists between frames
double x = 0;

var scene = PxlApp.CreateScene(ctx =>
{
    ctx.DrawBackground(Colors.Black);

    // Draw a ball at the animated position
    ctx.DrawCircle(x, 12, 3, colorFill: Colors.Red);

    // Update position for next frame
    x += 0.2;

    // Wrap around when off screen
    if (x > ctx.Width + 3)
        x = -3;
});

await PxlApp.Simulate(scene);
