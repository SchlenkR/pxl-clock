// ---
// app: Demo37LayerInterpolation
// displayName: Interpolation Comparison
// author: Cumin & Potato
// ---
// Example 37: Interpolation Comparison
// Compare NearestNeighbor (sharp) vs Linear (smooth) side by side

#:package Pxl@0.0.64

using Pxl.Ui.CSharp;

var zoom = Animate.EaseInOut(3, 1, 4, repeat: Repeat.PingPong);

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    var s = zoom.Eval(ctx);

    // Left: NearestNeighbor (sharp pixels)
    var left = ctx.NewLayer(clearColor: Colors.Transparent);
    left.DrawCircle(6, 12, 3, colorFill: Colors.Red);
    left.DrawPoint(5, 11, Colors.White);
    left.Translate(-6, -12);
    left.Scale(s);
    left.Translate(6, 12);
    left.Apply(BlendMode.SourceOver, Interpolation.NearestNeighbor);

    // Right: Linear (smooth)
    var right = ctx.NewLayer(clearColor: Colors.Transparent);
    right.DrawCircle(18, 12, 3, colorFill: Colors.Red);
    right.DrawPoint(17, 11, Colors.White);
    right.Translate(-18, -12);
    right.Scale(s);
    right.Translate(18, 12);
    right.Apply(BlendMode.SourceOver, Interpolation.Linear);

    // Divider line
    ctx.DrawLine(12, 0, 12, 24, Colors.DimGray);

    // Labels
    ctx.DrawText("NN", 1, 1, color: Colors.Gray, fontSize: 5);
    ctx.DrawText("LI", 14, 1, color: Colors.Gray, fontSize: 5);
};
