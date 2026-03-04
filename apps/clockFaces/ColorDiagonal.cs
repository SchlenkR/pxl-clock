// ---
// app: ColorDiagonal
// displayName: Color Diagonal
// appType: ClockFace
// author: "Nora & Urs Enzler"
// description: Animated diagonal lines that alternate direction each minute
// ---

#:package Pxl@0.0.57

using Pxl.Ui.CSharp;

var colors = new Color[]
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

// Build line endpoint arrays (two diagonal directions)
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

var scene = (DrawingContext ctx) =>
{
    var now = ctx.Now;
    var sec = now.Second;

    // Draw diagonal lines (direction alternates each minute)
    var lMin = sec <= 30 ? 0 : sec - 30;
    var lMax = Math.Min(sec, 29);
    var lines = now.Minute % 2 == 0 ? lines1 : lines2;

    for (var l = lMin; l <= lMax; l++)
    {
        var (x1, y1, x2, y2) = lines[l % 30];
        ctx.DrawLine(x1, y1, x2, y2, color: colors[l % 30]);
    }

    // Diffuser overlay for text readability
    ctx.DrawRectXyWh(0, 7, 24, 9, colorFill: Color.FromArgbByte(80, 0, 0, 0), isAntialias: true);
    ctx.DrawRectXyWh(0, 8, 24, 7, colorFill: Color.FromArgbByte(80, 0, 0, 0), isAntialias: true);

    // Centered time
    ctx.DrawTextVar4x5($"{now:HH}:{now:mm}", 1, 9, color: Colors.White);
};

