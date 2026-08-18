// ---
// app: AroundTheClock
// displayName: Around The Clock
// appType: ClockFace
// author: "Nico & Urs Enzler"
// description: Three concentric pixel paths show seconds, minutes and hours
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

var style = Param.Choice("classic", ["classic", "rainbow"], label: "Style");
var tint = Param.Color(Color.FromHsv360(190, 1.0, 1.0), label: "Tint",
    description: "Base colour of the rings in classic style");
var backgroundColor = Param.Color(Color.FromHsv360(195, 0.9, 0.2), label: "Background");
var showDigits = Param.Bool(true, label: "Show digits");

var scene = (RasterSurface ctx) =>
{
    var now = ctx.Now;
    ctx.DrawBackground(backgroundColor);

    // The two inner rings use the tint shifted a bit further around the wheel
    var minuteTint = tint.ShiftHue(10.0 / 360.0);
    var hourTint = tint.ShiftHue(20.0 / 360.0);

    if (showDigits)
    {
        // Centered time (no leading zero for hour, like the original)
        var timeText = $"{now.Hour}:{now:mm}";
        var textWidth = ctx.MeasureTextVar4x5(timeText);
        var marginLeft = (ctx.Width - textWidth) / 2.0;
        var marginTop = (ctx.Height - Fonts.Var4x5.DefaultHeight - 1) / 2.0;
        ctx.DrawTextVar4x5(timeText, marginLeft, marginTop, color: Colors.White);
    }

    // Seconds (outermost ring)
    for (var s = 0; s < now.Second; s++)
    {
        var delta = (double)(now.Second - s);
        var color = style == "rainbow"
            ? Color.FromHsv360((140.0 + 5.0 * s) % 360, 1.0, 0.7 * (59.0 - delta) / 60.0 + 0.3)
            : tint.LitBy(Colors.White, 0.8 * (59.0 - delta) / 60.0 + 0.2);
        var (x, y) = s switch
        {
            <= 21 => (s + 1, 1),
            <= 25 => (22, s - 20),
            <= 29 => (22, s - 9),
            <= 51 => (52 - s, 21),
            <= 55 => (1, 72 - s),
            _ => (1, 61 - s)
        };
        ctx.DrawPoint(x, y, color: color, isAntialias: false);
    }

    // Minutes (middle ring)
    for (var m = 0; m < now.Minute; m++)
    {
        var delta = (double)(now.Minute - m);
        var color = style == "rainbow"
            ? Color.FromHsv360((240.0 + 5.0 * m) % 360, 1.0, 0.7 * (59.0 - delta) / 60.0 + 0.3)
            : minuteTint.LitBy(Colors.White, 0.8 * (59.0 - delta) / 60.0 + 0.2);
        var (x, y) = m switch
        {
            <= 19 => (m + 2, 2),
            <= 24 => (21, m - 17),
            <= 29 => (21, m - 10),
            <= 49 => (51 - m, 20),
            <= 54 => (2, 69 - m),
            _ => (2, 62 - m)
        };
        ctx.DrawPoint(x, y, color: color, isAntialias: false);
    }

    // Hours (innermost ring)
    for (var h = 0; h < now.Hour; h++)
    {
        var delta = (double)(now.Hour - h);
        var color = style == "rainbow"
            ? Color.FromHsv360((40.0 + 15.0 * h) % 360, 1.0, 0.8 * (59.0 - delta) / 60.0 + 0.2)
            : hourTint.LitBy(Colors.White, 0.8 * (23.0 - delta) / 24.0 + 0.2);
        var (x, y) = h switch
        {
            <= 9 => (h + 7, 6),
            10 => (16, h - 3),
            11 => (16, h + 4),
            <= 21 => (28 - h, 16),
            22 => (7, 37 - h),
            _ => (7, 30 - h)
        };
        ctx.DrawPoint(x, y, color: color, isAntialias: false);
    }
};
