// ---
// app: Demo04FillAndStroke
// displayName: Fill and Stroke
// author: Cumin & Potato
// ---
// Example 04: Fill and Stroke
// Shapes can have both fill and stroke (outline)

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (RasterSurface ctx) =>
{
    ctx.DrawBackground(Colors.DarkGray);

    // Rectangle with both fill and stroke
    ctx.DrawRectXyWh(
        2, 2, 10, 8,
        colorFill: Colors.Blue,
        colorStroke: Colors.White,
        strokeWidth: 1);

    // Circle with only stroke (no fill)
    ctx.DrawCircle(
        18, 6, 4,
        colorFill: null,
        colorStroke: Colors.Yellow,
        strokeWidth: 1);

    // Circle with both fill and stroke
    ctx.DrawCircle(
        12, 17, 5,
        colorFill: Colors.Purple,
        colorStroke: Colors.Pink,
        strokeWidth: 1);
};

