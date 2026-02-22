// ---
// app: Demo06LinearGradient
// displayName: Linear Gradients
// author: Cumin & Potato
// ---
// Example 06: Linear Gradients
// Create smooth color transitions with linear gradients

#:package Pxl@0.0.51

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    // Horizontal gradient (left to right)
    ctx.DrawRectXyWh(
        0, 0, 24, 8,
        colorFill: Paints.LinearGradient(
            start: (0, 0),
            end: (24, 0),
            colors: [Colors.Red, Colors.Yellow]
        ));

    // Vertical gradient (top to bottom)
    ctx.DrawRectXyWh(
        0, 9, 24, 7,
        colorFill: Paints.LinearGradient(
            start: (0, 9),
            end: (0, 16),
            colors: [Colors.Blue, Colors.Cyan]
        ));

    // Multi-color gradient (rainbow)
    ctx.DrawRectXyWh(
        0, 17, 24, 7,
        colorFill: Paints.LinearGradient(
            start: (0, 17),
            end: (24, 17),
            colors: [Colors.Red, Colors.Orange, Colors.Yellow, 
                     Colors.Green, Colors.Blue, Colors.Purple]
        ));
};

