// ---
// app: Demo36LayerCombinedTransforms
// displayName: Combined Layer Transforms
// author: Cumin & Potato
// ---
// Example 36: Combined Layer Transforms
// Scale, rotate, and translate together for a spinning orbiting square

#:package Pxl@*

using Pxl.Ui.CSharp;

var orbit = Animate.Linear(3, 0, 360, repeat: Repeat.Loop);
var spin = Animate.Linear(1.5, 0, 360, repeat: Repeat.Loop);
var size = Animate.EaseInOut(2, 0.8, 1.5, repeat: Repeat.PingPong);

var scene = (RasterSurface ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    // Static center dot
    ctx.DrawCircle(12, 12, 2, colorFill: Colors.DimGray);

    // Orbiting, spinning, pulsing square
    var layer = ctx.NewLayer(clearColor: Colors.Transparent);
    layer.DrawRectXyWh(9, 3, 6, 6, colorFill: Colors.Orange);

    // 1. Move square center to origin
    layer.Translate(-12, -6);
    // 2. Scale around origin
    layer.Scale(size.Eval(ctx));
    // 3. Move back
    layer.Translate(12, 6);
    // 4. Spin the square around its own center
    layer.Rotate(spin.Eval(ctx), 12, 6);
    // 5. Orbit around display center
    layer.Rotate(orbit.Eval(ctx));

    layer.Apply(BlendMode.SourceOver);
};
