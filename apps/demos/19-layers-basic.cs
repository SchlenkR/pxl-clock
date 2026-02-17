// ---
// app: Demo19LayersBasic
// displayName: Layers - Basic
// author: PXL
// ---
// Example 19: Layers - Basic
// Create a separate layer and apply it back

#:package Pxl@0.0.46

using Pxl.Ui.CSharp;
using SkiaSharp;

var scene = PxlApp.CreateScene(ctx =>
{
    // Draw main background
    ctx.DrawBackground(Colors.DarkBlue);
    ctx.DrawCircle(8, 12, 6, colorFill: Colors.Red);

    // Create a new layer with transparent background
    var layer = ctx.NewLayer(clearColor: Colors.TransparentBlack);

    // Draw on the layer
    layer.DrawCircle(16, 12, 6, colorFill: Colors.Yellow);

    // Apply the layer back to the main context
    // SourceOver blends using alpha
    layer.Apply(SKBlendMode.SrcOver);
});

await PxlApp.SimulateAndSendToDevice(scene);
