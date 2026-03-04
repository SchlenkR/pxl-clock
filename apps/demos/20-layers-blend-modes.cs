// ---
// app: Demo20LayersBlendModes
// displayName: Layers with Blend Modes
// author: Cumin & Potato
// ---
// Example 20: Layers with Blend Modes
// Different blend modes create different visual effects

#:package Pxl@0.0.57

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    // Draw colorful background
    ctx.DrawRectXyWh(0, 0, 12, 24, colorFill: Colors.Red);
    ctx.DrawRectXyWh(12, 0, 12, 24, colorFill: Colors.Blue);

    // Create layer with shapes
    var layer = ctx.NewLayer(clearColor: Colors.Transparent);
    layer.DrawCircle(12, 8, 6, colorFill: Colors.Yellow);
    layer.DrawCircle(12, 16, 6, colorFill: Colors.Cyan);

    // Try different blend modes:
    // - SourceOver: Normal alpha blending (default)
    // - Multiply: Darkens (good for shadows)
    // - Screen: Lightens (good for glow)
    // - Difference: Inverts colors

    layer.Apply(BlendMode.Screen);  // Change this to experiment!
};

