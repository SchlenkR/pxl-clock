// ---
// app: AroundTheClock
// displayName: Around The Clock
// appType: ClockFace
// author: Urs Enzler
// description: Three concentric pixel paths show seconds, minutes and hours
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    var now = ctx.Now;
    ctx.DrawBackground(Color.FromHsv360(195, 0.9, 0.2));

    // Centered time (no leading zero for hour, like the original)
    var timeText = $"{now.Hour}:{now:mm}";
    var textWidth = ctx.MeasureTextVar4x5(timeText);
    var marginLeft = (ctx.Width - textWidth) / 2.0;
    var marginTop = (ctx.Height - Fonts.Var4x5.DefaultHeight - 1) / 2.0;
    ctx.DrawTextVar4x5(timeText, marginLeft, marginTop, color: Colors.White);

    // Seconds (outermost ring)
    for (var s = 0; s < now.Second; s++)
    {
        var delta = (double)(now.Second - s);
        var v = 0.8 * (59.0 - delta) / 60.0 + 0.2;
        var color = Color.FromHsv360(190, 1.0, v);
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
        var v = 0.8 * (59.0 - delta) / 60.0 + 0.2;
        var color = Color.FromHsv360(200, 1.0, v);
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
        var v = 0.8 * (23.0 - delta) / 24.0 + 0.2;
        var color = Color.FromHsv360(210, 1.0, v);
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

