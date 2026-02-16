// Example 07: Radial Gradients
// Create circular color transitions

#:package Pxl@0.0.43

using Pxl.Ui.CSharp;

var scene = PxlApp.CreateScene(ctx =>
{
    ctx.DrawBackground(Colors.Black);

    // Simple radial gradient in a circle
    ctx.DrawCircle(
        7, 7, 6,
        colorFill: Paints.RadialGradient(
            center: (7, 7),
            radius: 6,
            colors: [Colors.White, Colors.Blue]
        ));

    // Radial gradient with off-center highlight (3D effect)
    ctx.DrawCircle(
        18, 7, 5,
        colorFill: Paints.RadialGradient(
            center: (16, 5),  // Offset center for highlight
            radius: 7,
            colors: [Colors.White, Colors.Red, Colors.DarkRed]
        ));

    // Radial gradient in a rectangle (sunset effect)
    ctx.DrawRectXyWh(
        2, 14, 20, 8,
        colorFill: Paints.RadialGradient(
            center: (12, 22),  // Center at bottom
            radius: 15,
            colors: [Colors.Yellow, Colors.Orange, Colors.Red, Colors.Purple]
        ));
});

await PxlApp.SimulateAndSendToDevice(scene);
