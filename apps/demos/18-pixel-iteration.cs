// ---
// app: Demo18PixelIteration
// displayName: Iterating Over Pixels
// author: Cumin & Potato
// ---
// Example 18: Iterating Over Pixels
// Use the Cells property to iterate with coordinates

#:package Pxl@0.0.50

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    var time = ctx.Elapsed.TotalSeconds;
    var pixels = ctx.Pixels;

    // Create a plasma-like effect by iterating over all cells
    foreach (var cell in pixels.Cells)
    {
        // Calculate color based on position and time
        var value = 
            Math.Sin(cell.X * 0.5 + time)
            + Math.Sin(cell.Y * 0.5 + time)
            + Math.Sin((cell.X + cell.Y) * 0.3 + time)
            + Math.Sin(Math.Sqrt(cell.X * cell.X + cell.Y * cell.Y) * 0.3);

        // Normalize to 0-1 range
        value = (value + 4) / 8;

        // Convert to color using HSL
        var color = Color.FromHsl(value, 1.0, 0.5);

        pixels[cell.X, cell.Y] = color;
    }
};

