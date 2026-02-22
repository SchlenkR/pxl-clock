// ---
// app: Demo16Arcs
// displayName: Arcs and Pie Charts
// author: Cumin & Potato
// ---
// Example 16: Arcs and Pie Charts
// Draw arcs for progress indicators and pie charts

#:package Pxl@0.0.50

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    var time = ctx.Elapsed.TotalSeconds;

    // Animated progress arc
    var progress = (time % 5) / 5;  // 0 to 1 over 5 seconds
    var sweepAngle = progress * 360;

    // Draw background circle
    ctx.DrawCircle(8, 8, 6, colorFill: Colors.DarkGray);

    // Draw progress arc
    ctx.DrawArcCenter(
        8, 8, 6,
        startAngle: -90,  // Start at top
        sweepAngle: sweepAngle,
        colorFill: Colors.Lime);

    // Simple pie chart
    ctx.DrawArcCenter(18, 16, 5, -90, 120, colorFill: Colors.Red);
    ctx.DrawArcCenter(18, 16, 5, 30, 90, colorFill: Colors.Blue);
    ctx.DrawArcCenter(18, 16, 5, 120, 150, colorFill: Colors.Yellow);
};

