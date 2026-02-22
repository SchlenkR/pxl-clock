// ---
// app: Demo22SceneComposition
// displayName: Multiple Shapes - Scene Composition
// author: Cumin & Potato
// ---
// Example 22: Multiple Shapes - Scene Composition
// Combine everything to create a complete scene

#:package Pxl@0.0.46

using Pxl.Ui.CSharp;

var scene = PxlApp.CreateScene(ctx =>
{
    var time = ctx.Elapsed.TotalSeconds;

    // Sky gradient background
    ctx.DrawRectXyWh(
        0, 0, 24, 16,
        colorFill: Paints.LinearGradient(
            (0, 0), (0, 16),
            [Colors.DarkBlue, Colors.Blue, Colors.Orange]
        ));

    // Ground
    ctx.DrawRectXyWh(0, 16, 24, 8, colorFill: Colors.DarkGreen);

    // Sun with animated position (setting)
    var sunY = 8 + Math.Sin(time * 0.2) * 4;
    ctx.DrawCircle(
        20, sunY, 3,
        colorFill: Paints.RadialGradient(
            (19, sunY - 1), 4,
            [Colors.Yellow, Colors.Orange]
        ));

    // Simple house
    ctx.DrawRectXyWh(3, 12, 8, 8, colorFill: Colors.Brown);  // Body
    ctx.DrawRectXyWh(5, 15, 2, 3, colorFill: Colors.Yellow); // Window
    ctx.DrawRectXyWh(9, 17, 2, 3, colorFill: Colors.DarkRed);// Door

    // Roof (triangle using arcs)
    ctx.DrawLine(2, 12, 7, 7, Colors.DarkRed, strokeWidth: 1);
    ctx.DrawLine(7, 7, 12, 12, Colors.DarkRed, strokeWidth: 1);
});

await PxlApp.Run(scene);
