// ---
// app: Demo19LayersBasic
// displayName: Layers - Basic
// author: Cumin & Potato
// ---
// Example 19: Layers - Basic
// Create a separate layer and apply it back

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    // Draw main background
    ctx.DrawBackground(Colors.DarkBlue);
    ctx.DrawCircle(8, 12, 6, colorFill: Colors.Red);

    // Create a new layer with transparent background
    var layer = ctx.NewLayer(clearColor: Colors.Transparent);

    // Draw on the layer
    layer.DrawCircle(16, 12, 6, colorFill: Colors.Yellow);

    // Apply the layer back to the main context
    // SourceOver blends using alpha (default)
    layer.Apply(BlendMode.SourceOver);
};

