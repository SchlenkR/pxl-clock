// ---
// app: Demo38ElapsedCounter
// displayName: Elapsed Counter
// author: Cumin & Potato
// ---
// Example 38: Elapsed Counter
// Displays elapsed seconds and tenths of a second

#:package Pxl@0.0.58

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    var totalSeconds = ctx.Elapsed.TotalSeconds;
    var wholeSeconds = (int)totalSeconds;
    var tenths = (int)((totalSeconds - wholeSeconds) * 10);

    // Display seconds
    var secText = wholeSeconds.ToString();
    ctx.DrawTextVar4x5(secText, 1, 9, Colors.White);

    // Display decimal point
    var secWidth = ctx.MeasureTextVar4x5(secText);
    var dotX = 1 + secWidth + 1;
    ctx.DrawPoint(dotX, 14, Colors.Gray);

    // Display tenths
    ctx.DrawTextVar4x5(tenths.ToString(), dotX + 2, 9, Colors.Yellow);
};
