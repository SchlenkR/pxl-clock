// ---
// app: Mythen
// displayName: Mythen
// appType: ClockFace
// author: Urs Enzler
// description: Mountain silhouette with dynamic sky gradient
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

var mythen = Image.LoadSingleImage("assets/mythen.png");

var followClock = Param.Bool(true, label: "Sky follows the clock");
var timeOfDay = Param.Float(12.0, min: 0.0, max: 24.0, label: "Time of day",
    description: "Which hour the sky shows while it does not follow the clock");

// Sky keyframes over the day: hour, top/bottom hue, saturation, value.
// Hues may exceed 360 so interpolation takes the short way around the wheel.
var skyKeys = new (double Hour, double HTop, double HBot, double STop, double SBot, double VTop, double VBot)[]
{
    (5.5,  200, 200, 0.30, 0.50, 0.00, 0.40),   // night
    (7.0,  216, 375, 0.42, 0.17, 0.76, 0.83),   // dawn
    (8.5,  216, 213, 0.22, 0.27, 0.76, 0.83),   // morning
    (12.0, 216, 200, 0.51, 0.00, 0.78, 0.85),   // day
    (17.5, 216, 200, 0.51, 0.00, 0.78, 0.85),   // late afternoon
    (19.0, 216, 375, 0.42, 0.17, 0.78, 0.83),   // dusk
    (22.5, 200, 200, 0.30, 0.50, 0.00, 0.40),   // night again
};

static double Lerp(double start, double end, double k) =>
    start + (end - start) * k;

(double, double, double, double, double, double) SkyAt(double hour)
{
    var h = ((hour % 24.0) + 24.0) % 24.0;
    for (var i = 0; i < skyKeys.Length; i++)
    {
        var a = skyKeys[i];
        var b = skyKeys[(i + 1) % skyKeys.Length];
        var span = (b.Hour - a.Hour + 24.0) % 24.0;
        var into = (h - a.Hour + 24.0) % 24.0;
        if (into < span)
        {
            var k = into / span;
            return (
                Lerp(a.HTop, b.HTop, k), Lerp(a.HBot, b.HBot, k),
                Lerp(a.STop, b.STop, k), Lerp(a.SBot, b.SBot, k),
                Lerp(a.VTop, b.VTop, k), Lerp(a.VBot, b.VBot, k));
        }
    }
    var last = skyKeys[^1];
    return (last.HTop, last.HBot, last.STop, last.SBot, last.VTop, last.VBot);
}

var scene = (RasterSurface ctx) =>
{
    var now = ctx.Now;
    // Only the sky can be set by hand - the readout always shows the real time.
    var hour = followClock ? now.TimeOfDay.TotalHours : timeOfDay;
    var (hTop, hBot, sTop, sBot, vTop, vBot) = SkyAt(hour);

    // Draw sky gradient (horizontal lines)
    for (var l = 0; l < 20; l++)
    {
        var k = (l + 1) / 20.0;
        var color = Color.FromHsv360(
            Lerp(hTop, hBot, k) % 360,
            Lerp(sTop, sBot, k),
            Lerp(vTop, vBot, k));
        ctx.DrawLine(0, l, 24, l, color: color);
    }

    // Mountain silhouette
    ctx.DrawImage(mythen, 0, 0);

    // Centered time
    ctx.DrawTextVar4x5($"{now:HH}:{now:mm}", 1, 9, color: Colors.White);
};
