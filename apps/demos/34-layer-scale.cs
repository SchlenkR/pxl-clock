// ---
// app: Demo34LayerScale
// displayName: Layer Scaling
// author: Cumin & Potato
// ---
// Example 34: Layer Scaling
// Draw small, scale up — great for zoom effects

#:package Pxl@*

using Pxl.Ui.CSharp;

var pulse = Animate.EaseInOut(2, 1, 3, repeat: Repeat.PingPong);

var scene = (RasterSurface ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    // Draw a tiny smiley at original size (centered at 0,0)
    var layer = ctx.NewLayer(clearColor: Colors.Transparent);
    layer.DrawCircle(12, 12, 4, colorFill: Colors.Yellow);
    layer.DrawPoint(10, 11, Colors.Black);
    layer.DrawPoint(14, 11, Colors.Black);
    layer.DrawPoint(11, 14, Colors.Black);
    layer.DrawPoint(12, 14, Colors.Black);
    layer.DrawPoint(13, 14, Colors.Black);

    // Scale up from center
    var s = pulse.Eval(ctx);
    layer.Translate(-12, -12);   // move center to origin
    layer.Scale(s);              // scale
    layer.Translate(12, 12);     // move back

    layer.Apply(BlendMode.SourceOver, Interpolation.NearestNeighbor);
};
