// ---
// app: Worm
// displayName: Worm
// appType: ClockFace
// author: Urs Enzler
// description: Bouncing color worm trail with time overlay
// ---

#:package Pxl@0.0.45

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

// State
var rand = Random.Shared;
var vX = rand.NextDouble() * 1.9;
var vY = Math.Sqrt(4.0 - vX * vX);
var points = new List<(double X, double Y)> { (rand.NextDouble() * 24, rand.NextDouble() * 24) };
var lastTick = -1;
var lastMinute = -1;

var scene = PxlApp.CreateScene(ctx =>
{
    var now = ctx.Now;
    var tick = now.Millisecond / 100;

    // Reset worm on minute change
    if (now.Minute != lastMinute)
    {
        lastMinute = now.Minute;
        vX = rand.NextDouble() * 1.9;
        vY = Math.Sqrt(4.0 - vX * vX);
        points.Clear();
        points.Add((rand.NextDouble() * 24, rand.NextDouble() * 24));
        lastTick = tick;
    }
    else if (tick != lastTick)
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
    }

    // Draw worm segments (oldest first so newest is on top)
    var segCount = Math.Min(60, points.Count);
    for (var i = 0; i < segCount - 1; i++)
    {
        var a = points[segCount - 1 - i];
        var b = points[segCount - 2 - i];
        var baseColor = wormColors[(points.Count + i) % wormColors.Length];
        var c = baseColor.WithAlpha(Math.Max(0, (255.0 - i) / 255.0));
        ctx.DrawLine((int)a.X, (int)a.Y, (int)b.X, (int)b.Y,
            color: c, strokeWidth: 2, isAntialias: true);
    }

    // Diffuser overlay for text readability
    ctx.DrawRectXyWh(0, 7, 24, 9, colorFill: Color.FromArgbByte(80, 0, 0, 0), isAntialias: true);
    ctx.DrawRectXyWh(0, 8, 24, 7, colorFill: Color.FromArgbByte(80, 0, 0, 0), isAntialias: true);

    // Centered time
    ctx.DrawTextVar4x5($"{now:HH}:{now:mm}", 1, 9, color: Colors.White);
});

await PxlApp.SimulateAndSendToDevice(scene);
