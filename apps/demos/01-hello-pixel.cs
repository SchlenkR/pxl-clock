// ---
// app: Demo01HelloPixel
// displayName: Hello Pixel
// author: Cumin & Potato
// ---
// Example 01: Hello Pixel
// The simplest possible example - draw a single pixel

#:package Pxl@0.0.46

using Pxl.Ui.CSharp;

var scene = PxlApp.CreateScene(ctx =>
{
    // Draw a single white pixel at position (12, 12)
    ctx.SetPixel(12, 12, Colors.White);
});

await PxlApp.SimulateAndSendToDevice(scene);
