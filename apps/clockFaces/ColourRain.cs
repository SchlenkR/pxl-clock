// ---
// app: ColourRain
// displayName: Colour Rain
// appType: ClockFace
// author: "Nico & Urs Enzler"
// description: Colorful rain lines flowing down with time overlay
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

var colours = Param.Choice("rainbow", ["rainbow", "single"], label: "Colours");
var rainColor = Param.Color(Color.FromHsv360(200, 0.8, 1.0), label: "Rain colour");
var dropLength = Param.Int(4, min: 2, max: 12, label: "Drop length");

var offsets = new[] { 10, 4, 17, 7, 12, 1, 13, 19, 9, 14, 1, 7, 18, 9, 5, 17, 8, 4, 9, 19, 2, 6, 13, 17 };

var scene = (RasterSurface ctx) =>
{
    var now = ctx.Now;
    var step = now.Second % 24;

    // Rain lines
    for (var i = 0; i < 24; i++)
    {
        var y = (step + offsets[i]) % 24;
        var color = colours == "rainbow" ? Color.FromHsv360(i * 15.0, 0.8, 1.0) : rainColor;
        ctx.DrawLine(i, y, i, y + dropLength - 1,
            color: color.WithAlpha(0.6),
            isAntialias: false);
    }

    // Time
    ctx.DrawTextVar10x10($"{now.Hour}", 0, 1,
        color: Color.FromHsv360(220, 0.5, 1.0).WithAlpha(0.7));
    ctx.DrawTextVar10x10($"{now.Minute:D2}", 2, 10,
        color: Color.FromHsv360(20, 0.5, 1.0).WithAlpha(0.7));
    ctx.DrawTextVar4x5($"{now.Second:D2}", 15, 19,
        color: Color.FromHsv360(100, 0.5, 1.0).WithAlpha(0.7));
};

