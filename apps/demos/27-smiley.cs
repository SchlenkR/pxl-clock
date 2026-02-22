// ---
// app: Demo27Smiley
// displayName: Smiley Face
// author: Cumin & Potato
// ---
// Example 27: Smiley Face
// Put it all together - gradients, shapes, and composition

#:package Pxl@0.0.46

using Pxl.Ui.CSharp;

var scene = PxlApp.CreateScene(ctx =>
{
    // Background with radial gradient
    ctx.DrawRectXyWh(
        0, 0, 24, 24,
        colorFill: Paints.RadialGradient(
            (12, 12), 17,
            [Colors.LightBlue, Colors.DarkBlue]
        ));

    // Face with gradient (yellow to orange)
    ctx.DrawCircle(
        12, 12, 10,
        colorFill: Paints.RadialGradient(
            (10, 10), 12,
            [Colors.Yellow, Colors.Gold, Colors.Orange]
        ));

    // Face outline
    ctx.DrawCircle(
        12, 12, 10,
        colorStroke: Colors.DarkOrange,
        strokeWidth: 1);

    // Eyes with gradient (white to blue to black)
    ctx.DrawCircle(
        9, 10, 1.5,
        colorFill: Paints.RadialGradient(
            (9, 10), 1.5,
            [Colors.White, Colors.Blue, Colors.Black]
        ));

    ctx.DrawCircle(
        15, 10, 1.5,
        colorFill: Paints.RadialGradient(
            (15, 10), 1.5,
            [Colors.White, Colors.Blue, Colors.Black]
        ));

    // Smile
    ctx.DrawRectXyWh(
        8, 15, 8, 2,
        colorFill: Paints.LinearGradient(
            (8, 15), (8, 17),
            [Colors.Red, Colors.DarkRed]
        ));

    // Rosy cheeks (fading to transparent)
    ctx.DrawCircle(
        7, 13, 1.5,
        colorFill: Paints.RadialGradient(
            (7, 13), 1.5,
            [Colors.Pink, Color.FromRgba(255, 192, 203, 0)]
        ));

    ctx.DrawCircle(
        17, 13, 1.5,
        colorFill: Paints.RadialGradient(
            (17, 13), 1.5,
            [Colors.Pink, Color.FromRgba(255, 192, 203, 0)]
        ));
});

await PxlApp.SimulateAndSendToDevice(scene);
