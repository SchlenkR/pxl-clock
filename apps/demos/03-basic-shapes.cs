// ---
// app: Demo03BasicShapes
// displayName: Basic Shapes
// author: Cumin & Potato
// ---
// Example 03: Basic Shapes
// Draw rectangles, circles, and lines

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    // Draw a filled rectangle (x, y, width, height)
    ctx.DrawRectXyWh(2, 2, 8, 6, colorFill: Colors.Red);

    // Draw a filled circle (centerX, centerY, radius)
    ctx.DrawCircle(18, 5, 4, colorFill: Colors.Green);

    // Draw a line (x1, y1, x2, y2)
    ctx.DrawLine(2, 14, 22, 20, color: Colors.Blue);
};

