// ---
// app: ColourWheel
// displayName: Colour Wheel
// appType: ClockFace
// author: Urs Enzler
// description: HSV color wheel cycling through the full spectrum
// ---

#:package Pxl@0.0.54

using Pxl.Ui.CSharp;

var backgroundColor = Color.FromHsv360(195, 0.9, 0.2).WithAlpha(0.4);

Color GetColor(int minute, int second, int s, int step)
{
    var h = (minute * 60 + s) % 360;
    var sat = 1.0 - step * 0.15;
    var val = 1.0 - (double)(second - s) / 100.0;
    return Color.FromHsv360(h, sat, val);
}

void Set(Color[] px, int x, int y, Color c) => px[x + y * 24] = c;

Color[] CalculatePixels(int minute, int second)
{
    var pixels = new Color[576];
    for (var i = 0; i < 576; i++)
        pixels[i] = backgroundColor;

    // Seconds 0-6: top edge, going right
    for (var s = 0; s <= Math.Min(6, second); s++)
    {
        Set(pixels, 12 + s, 4, GetColor(minute, second, s, 0));
        Set(pixels, 12 + s, 3, GetColor(minute, second, s, 1));
        Set(pixels, 12 + s, 2, GetColor(minute, second, s, 2));
        Set(pixels, 12 + s, 1, GetColor(minute, second, s, 3));
        Set(pixels, 12 + s, 0, GetColor(minute, second, s, 4));
    }

    // Second 7: top-right corner diagonal
    for (var s = 7; s <= Math.Min(7, second); s++)
    {
        Set(pixels, 19, 4, GetColor(minute, second, s, 0));
        Set(pixels, 20, 3, GetColor(minute, second, s, 1));
        Set(pixels, 21, 2, GetColor(minute, second, s, 2));
        Set(pixels, 22, 1, GetColor(minute, second, s, 3));
        Set(pixels, 23, 0, GetColor(minute, second, s, 4));
        Set(pixels, 19, 3, GetColor(minute, second, s, 1));
        Set(pixels, 19, 2, GetColor(minute, second, s, 2));
        Set(pixels, 19, 1, GetColor(minute, second, s, 3));
        Set(pixels, 19, 0, GetColor(minute, second, s, 4));
        Set(pixels, 20, 2, GetColor(minute, second, s, 2));
        Set(pixels, 20, 1, GetColor(minute, second, s, 3));
        Set(pixels, 20, 0, GetColor(minute, second, s, 4));
        Set(pixels, 21, 1, GetColor(minute, second, s, 3));
        Set(pixels, 21, 0, GetColor(minute, second, s, 4));
        Set(pixels, 22, 0, GetColor(minute, second, s, 4));
    }

    // Second 8: right of top-right corner fills
    for (var s = 8; s <= Math.Min(8, second); s++)
    {
        Set(pixels, 20, 4, GetColor(minute, second, s, 1));
        Set(pixels, 21, 4, GetColor(minute, second, s, 2));
        Set(pixels, 22, 4, GetColor(minute, second, s, 3));
        Set(pixels, 23, 4, GetColor(minute, second, s, 4));
        Set(pixels, 21, 3, GetColor(minute, second, s, 2));
        Set(pixels, 22, 3, GetColor(minute, second, s, 3));
        Set(pixels, 23, 3, GetColor(minute, second, s, 4));
        Set(pixels, 22, 2, GetColor(minute, second, s, 3));
        Set(pixels, 23, 2, GetColor(minute, second, s, 4));
        Set(pixels, 23, 1, GetColor(minute, second, s, 4));
    }

    // Seconds 8-21: right edge, going down
    for (var s = 8; s <= Math.Min(21, second); s++)
    {
        Set(pixels, 19, s - 3, GetColor(minute, second, s, 0));
        Set(pixels, 20, s - 3, GetColor(minute, second, s, 1));
        Set(pixels, 21, s - 3, GetColor(minute, second, s, 2));
        Set(pixels, 22, s - 3, GetColor(minute, second, s, 3));
        Set(pixels, 23, s - 3, GetColor(minute, second, s, 4));
    }

    // Second 22: bottom-right corner diagonal
    for (var s = 22; s <= Math.Min(22, second); s++)
    {
        Set(pixels, 19, 19, GetColor(minute, second, s, 0));
        Set(pixels, 20, 20, GetColor(minute, second, s, 1));
        Set(pixels, 21, 21, GetColor(minute, second, s, 2));
        Set(pixels, 22, 22, GetColor(minute, second, s, 3));
        Set(pixels, 23, 23, GetColor(minute, second, s, 4));
        Set(pixels, 20, 19, GetColor(minute, second, s, 1));
        Set(pixels, 21, 19, GetColor(minute, second, s, 2));
        Set(pixels, 22, 19, GetColor(minute, second, s, 3));
        Set(pixels, 23, 19, GetColor(minute, second, s, 4));
        Set(pixels, 21, 20, GetColor(minute, second, s, 2));
        Set(pixels, 22, 20, GetColor(minute, second, s, 3));
        Set(pixels, 23, 20, GetColor(minute, second, s, 4));
        Set(pixels, 22, 21, GetColor(minute, second, s, 3));
        Set(pixels, 23, 21, GetColor(minute, second, s, 4));
        Set(pixels, 23, 22, GetColor(minute, second, s, 4));
    }

    // Second 23: bottom of bottom-right corner fills
    for (var s = 23; s <= Math.Min(23, second); s++)
    {
        Set(pixels, 19, 20, GetColor(minute, second, s, 1));
        Set(pixels, 19, 21, GetColor(minute, second, s, 2));
        Set(pixels, 19, 22, GetColor(minute, second, s, 3));
        Set(pixels, 19, 23, GetColor(minute, second, s, 4));
        Set(pixels, 20, 21, GetColor(minute, second, s, 2));
        Set(pixels, 20, 22, GetColor(minute, second, s, 3));
        Set(pixels, 20, 23, GetColor(minute, second, s, 4));
        Set(pixels, 21, 22, GetColor(minute, second, s, 3));
        Set(pixels, 21, 23, GetColor(minute, second, s, 4));
        Set(pixels, 22, 23, GetColor(minute, second, s, 4));
    }

    // Seconds 23-36: bottom edge, going left
    for (var s = 23; s <= Math.Min(36, second); s++)
    {
        Set(pixels, 41 - s, 19, GetColor(minute, second, s, 0));
        Set(pixels, 41 - s, 20, GetColor(minute, second, s, 1));
        Set(pixels, 41 - s, 21, GetColor(minute, second, s, 2));
        Set(pixels, 41 - s, 22, GetColor(minute, second, s, 3));
        Set(pixels, 41 - s, 23, GetColor(minute, second, s, 4));
    }

    // Second 37: bottom-left corner diagonal
    for (var s = 37; s <= Math.Min(37, second); s++)
    {
        Set(pixels, 4, 19, GetColor(minute, second, s, 0));
        Set(pixels, 3, 20, GetColor(minute, second, s, 1));
        Set(pixels, 2, 21, GetColor(minute, second, s, 2));
        Set(pixels, 1, 22, GetColor(minute, second, s, 3));
        Set(pixels, 0, 23, GetColor(minute, second, s, 4));
        Set(pixels, 4, 20, GetColor(minute, second, s, 1));
        Set(pixels, 4, 21, GetColor(minute, second, s, 2));
        Set(pixels, 4, 22, GetColor(minute, second, s, 3));
        Set(pixels, 4, 23, GetColor(minute, second, s, 4));
        Set(pixels, 3, 21, GetColor(minute, second, s, 2));
        Set(pixels, 3, 22, GetColor(minute, second, s, 3));
        Set(pixels, 3, 23, GetColor(minute, second, s, 4));
        Set(pixels, 2, 22, GetColor(minute, second, s, 3));
        Set(pixels, 2, 23, GetColor(minute, second, s, 4));
        Set(pixels, 1, 23, GetColor(minute, second, s, 4));
    }

    // Second 38: left of bottom-left corner fills
    for (var s = 38; s <= Math.Min(38, second); s++)
    {
        Set(pixels, 3, 19, GetColor(minute, second, s, 1));
        Set(pixels, 2, 19, GetColor(minute, second, s, 2));
        Set(pixels, 1, 19, GetColor(minute, second, s, 3));
        Set(pixels, 0, 19, GetColor(minute, second, s, 4));
        Set(pixels, 2, 20, GetColor(minute, second, s, 2));
        Set(pixels, 1, 20, GetColor(minute, second, s, 3));
        Set(pixels, 0, 20, GetColor(minute, second, s, 4));
        Set(pixels, 1, 21, GetColor(minute, second, s, 3));
        Set(pixels, 0, 21, GetColor(minute, second, s, 4));
        Set(pixels, 0, 22, GetColor(minute, second, s, 4));
    }

    // Seconds 38-51: left edge, going up
    for (var s = 38; s <= Math.Min(51, second); s++)
    {
        Set(pixels, 4, 56 - s, GetColor(minute, second, s, 0));
        Set(pixels, 3, 56 - s, GetColor(minute, second, s, 1));
        Set(pixels, 2, 56 - s, GetColor(minute, second, s, 2));
        Set(pixels, 1, 56 - s, GetColor(minute, second, s, 3));
        Set(pixels, 0, 56 - s, GetColor(minute, second, s, 4));
    }

    // Second 52: top-left corner diagonal
    for (var s = 52; s <= Math.Min(52, second); s++)
    {
        Set(pixels, 4, 4, GetColor(minute, second, s, 0));
        Set(pixels, 3, 3, GetColor(minute, second, s, 1));
        Set(pixels, 2, 2, GetColor(minute, second, s, 2));
        Set(pixels, 1, 1, GetColor(minute, second, s, 3));
        Set(pixels, 0, 0, GetColor(minute, second, s, 4));
        Set(pixels, 3, 4, GetColor(minute, second, s, 1));
        Set(pixels, 2, 4, GetColor(minute, second, s, 2));
        Set(pixels, 1, 4, GetColor(minute, second, s, 3));
        Set(pixels, 0, 4, GetColor(minute, second, s, 4));
        Set(pixels, 2, 3, GetColor(minute, second, s, 2));
        Set(pixels, 1, 3, GetColor(minute, second, s, 3));
        Set(pixels, 0, 3, GetColor(minute, second, s, 4));
        Set(pixels, 1, 2, GetColor(minute, second, s, 3));
        Set(pixels, 0, 2, GetColor(minute, second, s, 4));
        Set(pixels, 0, 1, GetColor(minute, second, s, 4));
    }

    // Second 53: top of top-left corner fills
    for (var s = 53; s <= Math.Min(53, second); s++)
    {
        Set(pixels, 4, 3, GetColor(minute, second, s, 1));
        Set(pixels, 4, 2, GetColor(minute, second, s, 2));
        Set(pixels, 4, 1, GetColor(minute, second, s, 3));
        Set(pixels, 4, 0, GetColor(minute, second, s, 4));
        Set(pixels, 3, 2, GetColor(minute, second, s, 2));
        Set(pixels, 3, 1, GetColor(minute, second, s, 3));
        Set(pixels, 3, 0, GetColor(minute, second, s, 4));
        Set(pixels, 2, 1, GetColor(minute, second, s, 3));
        Set(pixels, 2, 0, GetColor(minute, second, s, 4));
        Set(pixels, 1, 0, GetColor(minute, second, s, 4));
    }

    // Seconds 53-59: top edge, continuing right
    for (var s = 53; s <= Math.Min(59, second); s++)
    {
        Set(pixels, s - 48, 4, GetColor(minute, second, s, 0));
        Set(pixels, s - 48, 3, GetColor(minute, second, s, 1));
        Set(pixels, s - 48, 2, GetColor(minute, second, s, 2));
        Set(pixels, s - 48, 1, GetColor(minute, second, s, 3));
        Set(pixels, s - 48, 0, GetColor(minute, second, s, 4));
    }

    return pixels;
}

// State
var pixels = new Color[576];
var lastSecond = -1;

var scene = (DrawingContext ctx) =>
{
    var now = ctx.Now;
    var second = now.Second;
    var minute = now.Minute;

    if (second != lastSecond)
    {
        lastSecond = second;
        pixels = CalculatePixels(minute, second);
    }

    ctx.SetPixels(pixels, BlendMode.Source);

    ctx.DrawTextMono4x5($"{now:HH}", 6, 6, color: Colors.White);
    ctx.DrawTextMono4x5($"{now:mm}", 9, 13, color: Colors.White);
};

