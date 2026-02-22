// ---
// app: ColorDiagonalWithFullEdges
// displayName: Color Diagonal With Full Edges
// appType: ClockFace
// author: "Nora & Urs Enzler"
// description: Diagonal lines with corner fills that change based on time
// ---

#:package Pxl@0.0.46

using Pxl.Ui.CSharp;

// Generate 30 HSV colors
var colors = new Color[30];
for (var i = 0; i < 30; i++)
    colors[i] = Color.FromHsv360(i * 12.0, 0.7, 0.9);

// Build line endpoint arrays
var lines1 = new (int x1, int y1, int x2, int y2)[30];
var lines2 = new (int x1, int y1, int x2, int y2)[30];
{
    var idx = 0;
    for (var i = 15; i >= 0; i--) lines1[idx++] = (i, 0, 24, 24 - i);
    for (var i = 1; i <= 14; i++) lines1[idx++] = (0, i, 24 - i, 24);
    idx = 0;
    for (var i = 9; i <= 24; i++) lines2[idx++] = (i, 0, 0, i);
    for (var i = 1; i <= 14; i++) lines2[idx++] = (i, 24, 24, i);
}

// Corner line arrays
var cornersTopLeft = new (int, int, int, int)[9];
for (var i = 0; i <= 8; i++) cornersTopLeft[i] = (i, 0, 0, i);

var cornersTopRight = new (int, int, int, int)[9];
for (var i = 0; i <= 8; i++) cornersTopRight[i] = (24 - i, 0, 24, i);

var cornersBottomLeft = new (int, int, int, int)[10];
for (var i = 0; i <= 9; i++) cornersBottomLeft[i] = (0, 24 - i, i, 24);

var cornersBottomRight = new (int, int, int, int)[10];
for (var i = 0; i <= 9; i++) cornersBottomRight[i] = (24 - i, 24, 24, 24 - i);

var scene = PxlApp.CreateScene(ctx =>
{
    var now = ctx.Now;
    var sec = now.Second;
    var min = now.Minute;
    var even = min % 2 == 0;

    // Main diagonal lines
    var lMin = sec <= 30 ? 0 : sec - 30;
    var lMax = Math.Min(sec, 29);
    var lines = even ? lines1 : lines2;

    for (var l = lMin; l <= lMax; l++)
    {
        var (x1, y1, x2, y2) = lines[l % 30];
        ctx.DrawLine(x1, y1, x2, y2, color: colors[l % 30]);
    }

    // Corner fills (depend on direction and half of minute)
    var corners1 = (even, sec < 30) switch
    {
        (true, true) => cornersTopRight,
        (false, true) => cornersTopLeft,
        _ => null
    };
    var corners2 = (even, sec >= 30) switch
    {
        (true, true) => cornersBottomLeft,
        (false, true) => cornersBottomRight,
        _ => null
    };

    if (corners1 != null)
        for (var i = 0; i < corners1.Length; i++)
        {
            var color = Color.FromHsv360(0, 0.7, Math.Max(0, 0.7 - (8 - i) * 0.1));
            var (x1, y1, x2, y2) = corners1[i];
            ctx.DrawLine(x1, y1, x2, y2, color: color);
        }

    if (corners2 != null)
        for (var i = 0; i < corners2.Length; i++)
        {
            var color = Color.FromHsv360(0, 0.7, Math.Max(0, 0.7 - (8 - i) * 0.1));
            var (x1, y1, x2, y2) = corners2[i];
            ctx.DrawLine(x1, y1, x2, y2, color: color);
        }

    // Diffuser + time
    ctx.DrawRectXyWh(0, 7, 24, 9, colorFill: Color.FromArgbByte(80, 0, 0, 0), isAntialias: true);
    ctx.DrawRectXyWh(0, 8, 24, 7, colorFill: Color.FromArgbByte(80, 0, 0, 0), isAntialias: true);
    ctx.DrawTextVar4x5($"{now:HH}:{now:mm}", 1, 9, color: Colors.White);
});

await PxlApp.Run(scene);
