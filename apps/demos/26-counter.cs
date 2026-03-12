// ---
// app: Demo26Counter
// displayName: Counter Display
// author: Cumin & Potato
// ---
// Example 26: Counter Display
// A simple counter using state variables

#:package Pxl@0.0.61

using Pxl.Ui.CSharp;

int counter = 0;
double lastSecond = -1;

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    // Increment counter every second
    var currentSecond = Math.Floor(ctx.Elapsed.TotalSeconds);
    if (currentSecond > lastSecond)
    {
        counter++;
        lastSecond = currentSecond;
    }

    // Display counter
    ctx.DrawTextMono4x5("COUNT", 2, 2, Colors.Gray);

    // Large number display
    var countStr = counter.ToString();
    // Center the text
    var textWidth = countStr.Length * 5;
    var x = (24 - textWidth) / 2;
    ctx.DrawTextMono4x5(countStr, x, 12, Colors.Lime);

    // Progress bar for next increment
    var progress = ctx.Elapsed.TotalSeconds % 1.0;
    ctx.DrawRectXyWh(2, 20, 20 * progress, 2, colorFill: Colors.DarkGreen);
    ctx.DrawRectXyWh(2, 20, 20, 2, colorStroke: Colors.Green);
};

