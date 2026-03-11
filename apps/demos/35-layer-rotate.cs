// ---
// app: Demo35LayerRotate
// displayName: Layer Rotation
// author: Cumin & Potato
// ---
// Example 35: Layer Rotation
// Spin a shape around the display center

#:package Pxl@0.0.58

using Pxl.Ui.CSharp;

var angle = Animate.Linear(4, 0, 360, repeat: Repeat.Loop);

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    var layer = ctx.NewLayer(clearColor: Colors.Transparent);

    // Draw a cross pattern
    layer.DrawRectXyWh(10, 2, 4, 20, colorFill: Colors.Cyan);
    layer.DrawRectXyWh(2, 10, 20, 4, colorFill: Colors.Cyan);

    // Rotate around center
    layer.Rotate(angle.Eval(ctx));

    layer.Apply(BlendMode.SourceOver);
};
