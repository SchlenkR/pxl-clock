// ---
// app: Demo40ClassesClockDigits
// displayName: Clock Digits (Classes)
// author: Cumin & Potato
// ---
// Example 40: Using classes — a clock with animated digit tiles

#:package Pxl@0.0.61

using Pxl.Ui.CSharp;

var digits = new DigitTile[4];
for (var i = 0; i < 4; i++)
    digits[i] = new DigitTile(x: 1 + i * 6, y: 9);

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Color.FromRgb(0.05, 0.0, 0.1));

    var now = ctx.Now;
    int[] values = [now.Hour / 10, now.Hour % 10, now.Minute / 10, now.Minute % 10];

    for (var i = 0; i < 4; i++)
        digits[i].DrawAnimated(ctx, values[i]);

    // Blinking colon
    if (now.Millisecond < 500)
    {
        ctx.DrawPoint(12, 11, Colors.White);
        ctx.DrawPoint(12, 14, Colors.White);
    }

    // Date bar at bottom
    var dateStr = now.ToString("dd.MM");
    ctx.DrawTextMono4x5(dateStr, 0, 19, Colors.Gray);
};

class DigitTile(int x, int y)
{
    private int lastValue = -1;
    private double flash = 0;

    public void DrawAnimated(DrawingContext ctx, int value)
    {
        if (value != lastValue)
        {
            flash = 1.0;
            lastValue = value;
        }

        // Flash fades out
        if (flash > 0) flash = Math.Max(0, flash - 0.02);

        var brightness = 0.6 + flash * 0.4;
        var color = Color.FromHsl360(200 + flash * 60, 80, brightness * 50);

        ctx.DrawTextMono4x5(value.ToString(), x, y, color);
    }
}
