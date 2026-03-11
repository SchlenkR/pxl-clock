// ---
// app: ColorDiamond
// displayName: Color Diamond
// appType: ClockFace
// author: "Nico & Urs Enzler"
// description: Nested colored rectangles with a seconds hand around the border
// ---

#:package Pxl@0.0.58

using Pxl.Ui.CSharp;

// Seconds hand positions around the diamond border (0-59)
var handPos = new (int x, int y)[]
{
    (11,0),(13,0),(14,1),(15,1),(16,2),(17,2),(18,3),(19,3),(20,4),(21,5),
    (21,6),(22,7),(22,8),(23,9),(23,10),(23,11),(23,13),(22,14),(22,15),(21,16),
    (21,17),(20,18),(19,19),(18,19),(17,20),(16,21),(15,22),(14,22),(13,23),(12,23),
    (11,23),(10,23),(9,23),(8,22),(7,22),(6,21),(5,20),(4,19),(3,19),(2,18),
    (1,17),(1,16),(0,15),(0,14),(0,13),(0,11),(0,9),(1,8),(1,7),(2,6),
    (2,5),(2,4),(3,4),(3,3),(4,3),(5,2),(6,2),(7,1),(8,1),(9,0)
};

var scene = (DrawingContext ctx) =>
{
    var now = ctx.Now;
    var hour = now.Hour;
    var sec = now.Second;

    // Saturation and value shift with time of day
    var sat = hour <= 15 ? 0.3 + 0.7 * hour / 15.0 : 0.3 + 0.7 * (23.0 - hour) / 9.0;
    var val = hour <= 15 ? 0.5 + 0.5 * hour / 15.0 : 0.5 + 0.5 * (23.0 - hour) / 9.0;
    var seed = (double)(hour * now.Minute * 20);

    // Draw nested rectangles (up to 5 passes based on seconds)
    for (var s = 0; s <= Math.Min(sec, 11); s++)
        ctx.DrawRectXyWh(s, s, 23 - 2 * s, 23 - 2 * s,
            colorStroke: Color.FromHsv360((seed + s * 10) % 360, sat, val));
    for (var s = 12; s <= Math.Min(sec, 23); s++)
        ctx.DrawRectXyWh(s, s, 23 - 2 * s, 23 - 2 * s,
            colorStroke: Color.FromHsv360((seed + s * 10) % 360, sat, val));
    for (var s = 24; s <= Math.Min(sec, 35); s++)
    {
        var r = s % 24;
        ctx.DrawRectXyWh(r, r, 23 - 2 * r, 23 - 2 * r,
            colorStroke: Color.FromHsv360((seed + (r + 24.0) * 10) % 360, sat, val));
    }
    for (var s = 36; s <= Math.Min(sec, 47); s++)
    {
        var r = s % 24;
        ctx.DrawRectXyWh(r, r, 23 - 2 * r, 23 - 2 * r,
            colorStroke: Color.FromHsv360((seed + (r + 24.0) * 10) % 360, sat, val));
    }
    for (var s = 48; s <= Math.Min(sec, 59); s++)
    {
        var r = s % 24;
        ctx.DrawRectXyWh(r, r, 23 - 2 * r, 23 - 2 * r,
            colorStroke: Color.FromHsv360((seed + (r + 48.0) * 10) % 360, sat, val));
    }

    // Seconds hand (large dot on the border)
    var (hx, hy) = handPos[sec];
    ctx.DrawPoint(hx, hy, color: Colors.Black, strokeWidth: 3, isAntialias: true);

    // Centered time
    ctx.DrawTextVar4x5($"{now:HH}:{now:mm}", 1, 9, color: Colors.White);
};

