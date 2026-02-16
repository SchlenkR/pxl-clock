// Example 02: Background and Point
// Fill the background and draw a point with DrawPoint

#:package Pxl@0.0.43

using Pxl.Ui.CSharp;

var scene = PxlApp.CreateScene(ctx =>
{
    // Fill the entire canvas with a background color
    ctx.DrawBackground(Colors.DarkBlue);

    // Draw a point (can have a stroke width for larger points)
    ctx.DrawPoint(12, 12, Colors.Yellow);
});

await PxlApp.SimulateAndSendToDevice(scene);
