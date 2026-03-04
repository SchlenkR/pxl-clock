// ---
// app: Mythen
// displayName: Mythen
// appType: ClockFace
// author: Urs Enzler
// description: Mountain silhouette with dynamic sky gradient
// ---

#:package Pxl@0.0.57

using Pxl.Ui.CSharp;

var mythen = Image.LoadSingleImage("assets/mythen.png");

// Sky gradient interpolation
static double Lerp(double start, double end, double step, double steps) =>
    start + (end - start) * (step / steps);

var scene = (DrawingContext ctx) =>
{
    var now = ctx.Now;
    var hour = now.Hour;

    // Sky colors change based on hour
    var (hTop, hBot, sTop, sBot, vTop, vBot) = hour switch
    {
        < 7  => (200.0, 200.0, 0.3, 0.5, 0.0, 0.4),
        < 8  => (216.0, 375.0, 0.42, 0.17, 0.76, 0.83),
        < 9  => (216.0, 213.0, 0.22, 0.27, 0.76, 0.83),
        < 18 => (216.0, 200.0, 0.51, 0.0, 0.78, 0.85),
        < 22 => (216.0, 375.0, 0.42, 0.17,
                 Lerp(0.78, 0.0, hour - 18, 4), Lerp(0.83, 0.4, hour - 18, 4)),
        _    => (200.0, 200.0, 0.3, 0.5, 0.0, 0.4)
    };

    // Draw sky gradient (horizontal lines)
    for (var l = 0; l < 20; l++)
    {
        var step = l + 1;
        var color = Color.FromHsv360(
            Lerp(hTop, hBot, step, 20) % 360,
            Lerp(sTop, sBot, step, 20),
            Lerp(vTop, vBot, step, 20));
        ctx.DrawLine(0, l, 24, l, color: color);
    }

    // Mountain silhouette
    ctx.DrawImage(mythen, 0, 0);

    // Centered time
    ctx.DrawTextVar4x5($"{now:HH}:{now:mm}", 1, 9, color: Colors.White);
};

