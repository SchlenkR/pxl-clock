// ---
// app: ColourWheel
// displayName: Colour Wheel
// appType: ClockFace
// author: Urs Enzler
// description: HSV colour wheel around the border - classic, dynamic, or with random hues
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

var style = Param.Choice("classic", ["classic", "dynamic", "random"], label: "Style",
    description: "classic fills up over the minute, dynamic keeps the whole wheel lit, random drifts between random hues");
var backgroundColor = Param.Color(Color.FromHsv360(195, 0.9, 0.2).WithAlpha(0.4), label: "Background");

// Random style: hue seeds shift every minute, the wheel walks between them
var rand = Random.Shared;
var previousSeed = rand.NextDouble() * 360;
var currentSeed = rand.NextDouble() * 360;
var nextSeed = rand.NextDouble() * 360;
var lastMinute = -1;

static (double angle, int dir) GetShorterArc(double a, double b)
{
    a %= 360; b %= 360;
    var diff = Math.Abs(b - a);
    if (diff > 180)
    {
        diff = 360 - diff;
        return Math.Abs((a + diff) % 360 - b) < Math.Abs((b + diff) % 360 - a)
            ? (diff, 1) : (diff, -1);
    }
    return Math.Abs((a + diff) % 360 - b) < Math.Abs((b + diff) % 360 - a)
        ? (diff, 1) : (diff, -1);
}

var scene = (RasterSurface ctx) =>
{
    var now = ctx.Now;
    var second = now.Second;
    var minute = now.Minute;

    if (minute != lastMinute)
    {
        lastMinute = minute;
        previousSeed = currentSeed;
        currentSeed = nextSeed;
        nextSeed = rand.NextDouble() * 360;
    }

    var (previousAngle, previousDirection) = GetShorterArc(previousSeed, currentSeed);
    var (currentAngle, currentDirection) = GetShorterArc(currentSeed, nextSeed);

    var pixels = new Color[576];
    for (var i = 0; i < 576; i++)
        pixels[i] = backgroundColor;

    void Set(int x, int y, Color c) => pixels[x + y * 24] = c;

    bool Visible(int s) => style != "classic" || s <= second;

    Color GetColor(int s, int step)
    {
        var isCurrent = s <= second;
        var age = isCurrent ? second - s : (second - s + 60) % 60;
        var hue = style switch
        {
            "classic" => minute * 60.0 + s,
            "dynamic" => isCurrent ? minute * 60.0 + s : ((minute + 59) % 60) * 60.0 + s + 1,
            _ => isCurrent
                ? currentSeed + currentAngle / 60.0 * s * currentDirection
                : previousSeed + previousAngle / 60.0 * s * previousDirection,
        };
        var val = 1.0 - age / 100.0 - (style == "classic" ? 0.0 : step * 0.05);
        return Color.FromHsv360(
            ((hue % 360.0) + 360.0) % 360.0,
            Math.Clamp(1.0 - step * 0.15, 0.0, 1.0),
            Math.Clamp(val, 0.0, 1.0));
    }

    // One 5-pixel ray per second along an edge; step 0 is the innermost pixel.
    void Edge(int s, int x, int y, int inX, int inY)
    {
        if (!Visible(s))
            return;
        for (var i = 0; i <= 4; i++)
            Set(x + inX * i, y + inY * i, GetColor(s, i));
    }

    // A 5x5 corner block split into two triangular halves; the half containing
    // the diagonal belongs to sDiag, the other to sFill. step = max(dx, dy).
    void Corner(int sDiag, int sFill, int baseX, int baseY, int dirX, int dirY, bool diagTakesY)
    {
        for (var dx = 0; dx <= 4; dx++)
        for (var dy = 0; dy <= 4; dy++)
        {
            var onDiagSide = diagTakesY ? dy >= dx : dx >= dy;
            var s = onDiagSide ? sDiag : sFill;
            if (!Visible(s))
                continue;
            Set(baseX + dirX * dx, baseY + dirY * dy, GetColor(s, Math.Max(dx, dy)));
        }
    }

    for (var s = 0; s <= 6; s++) Edge(s, 12 + s, 4, 0, -1);        // top edge, going right
    Corner(7, 8, 19, 4, 1, -1, diagTakesY: true);                  // top-right corner
    for (var s = 8; s <= 21; s++) Edge(s, 19, s - 3, 1, 0);        // right edge, going down
    Corner(22, 23, 19, 19, 1, 1, diagTakesY: false);               // bottom-right corner
    for (var s = 23; s <= 36; s++) Edge(s, 41 - s, 19, 0, 1);      // bottom edge, going left
    Corner(37, 38, 4, 19, -1, 1, diagTakesY: true);                // bottom-left corner
    for (var s = 38; s <= 51; s++) Edge(s, 4, 56 - s, -1, 0);      // left edge, going up
    Corner(52, 53, 4, 4, -1, -1, diagTakesY: false);               // top-left corner
    for (var s = 53; s <= 59; s++) Edge(s, s - 48, 4, 0, -1);      // top edge, continuing right

    ctx.SetPixels(pixels, BlendMode.Source);

    ctx.DrawTextMono4x5($"{now:HH}", 6, 6, color: Colors.White);
    ctx.DrawTextMono4x5($"{now:mm}", 9, 13, color: Colors.White);
};
