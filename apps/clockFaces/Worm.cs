// ---
// app: Worm
// displayName: Worm
// appType: ClockFace
// author: Urs Enzler
// description: Bouncing color worm trail with time overlay
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

var wormColors = new Color[]
{
    Color.FromRgbByte(230, 69, 69),   Color.FromRgbByte(230, 101, 69),
    Color.FromRgbByte(230, 133, 69),  Color.FromRgbByte(230, 165, 69),
    Color.FromRgbByte(230, 197, 69),  Color.FromRgbByte(230, 230, 69),
    Color.FromRgbByte(197, 230, 69),  Color.FromRgbByte(165, 230, 69),
    Color.FromRgbByte(133, 230, 69),  Color.FromRgbByte(101, 230, 69),
    Color.FromRgbByte(69, 230, 69),   Color.FromRgbByte(69, 230, 101),
    Color.FromRgbByte(69, 230, 133),  Color.FromRgbByte(69, 230, 165),
    Color.FromRgbByte(69, 230, 197),  Color.FromRgbByte(69, 230, 230),
    Color.FromRgbByte(69, 197, 230),  Color.FromRgbByte(69, 165, 230),
    Color.FromRgbByte(69, 133, 230),  Color.FromRgbByte(69, 101, 230),
    Color.FromRgbByte(69, 69, 230),   Color.FromRgbByte(101, 69, 230),
    Color.FromRgbByte(133, 69, 230),  Color.FromRgbByte(165, 69, 230),
    Color.FromRgbByte(197, 69, 230),  Color.FromRgbByte(230, 69, 230),
    Color.FromRgbByte(230, 69, 197),  Color.FromRgbByte(230, 69, 165),
    Color.FromRgbByte(230, 69, 133),  Color.FromRgbByte(230, 69, 101)
};

var wormLength = Param.Int(60, min: 10, max: 200, label: "Worm length");
var speed = Param.Float(1.0, min: 0.25, max: 4.0, label: "Speed");
var endless = Param.Bool(false, label: "Endless",
    description: "Keep the worm going instead of restarting every minute");
var colours = Param.Choice("rainbow", ["rainbow", "single"], label: "Colours");
var wormColor = Param.Color(Color.FromRgbByte(69, 197, 230), label: "Worm colour");
var backdrop = Param.Float(0.3, min: 0.0, max: 1.0, label: "Text backdrop",
    description: "Darkening behind the time for readability");

// State
var rand = Random.Shared;
var vX = rand.NextDouble() * 1.9;
var vY = Math.Sqrt(4.0 - vX * vX);
var points = new List<(double X, double Y)> { (rand.NextDouble() * 24, rand.NextDouble() * 24) };
var colorPhase = 0;
var lastTick = -1L;
var lastMinute = -1;

var scene = (RasterSurface ctx) =>
{
    var now = ctx.Now;
    var tick = (long)(ctx.Elapsed.TotalSeconds * 10.0 * speed);

    if (now.Minute != lastMinute)
    {
        lastMinute = now.Minute;
        if (!endless)
        {
            vX = rand.NextDouble() * 1.9;
            vY = Math.Sqrt(4.0 - vX * vX);
            points.Clear();
            points.Add((rand.NextDouble() * 24, rand.NextDouble() * 24));
            colorPhase = 0;
            lastTick = tick;
        }
    }
    if (tick != lastTick)
    {
        lastTick = tick;
        var (hx, hy) = points[0];
        var xc = hx + vX;
        var yc = hy + vY;

        var nx = hx;
        var ny = hy;
        if (xc < 0 || xc >= ctx.Width) vX = -vX; else nx = xc;
        if (yc < 0 || yc >= ctx.Height) vY = -vY; else ny = yc;

        points.Insert(0, (nx, ny));
        colorPhase++;
        if (points.Count > 200)
            points.RemoveAt(points.Count - 1);
    }

    // Draw worm segments (oldest first so newest is on top)
    var segCount = Math.Min(wormLength, points.Count);
    for (var i = 0; i < segCount - 1; i++)
    {
        var a = points[segCount - 1 - i];
        var b = points[segCount - 2 - i];
        var baseColor = colours == "rainbow"
            ? wormColors[(colorPhase + i) % wormColors.Length]
            : wormColor;
        var c = baseColor.WithAlpha(Math.Max(0, (255.0 - i) / 255.0));
        ctx.DrawLine((int)a.X, (int)a.Y, (int)b.X, (int)b.Y,
            color: c, strokeWidth: 2, isAntialias: true);
    }

    // Diffuser overlay for text readability
    var shade = Color.FromArgbByte((byte)Math.Round(backdrop * 255), 0, 0, 0);
    ctx.DrawRectXyWh(0, 7, 24, 9, colorFill: shade, isAntialias: true);
    ctx.DrawRectXyWh(0, 8, 24, 7, colorFill: shade, isAntialias: true);

    // Centered time
    ctx.DrawTextVar4x5($"{now:HH}:{now:mm}", 1, 9, color: Colors.White);
};
