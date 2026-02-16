// Colour Wheel Random
// Color wheel with random hue seeds that change each minute
// Design: Urs Enzler

#:package Pxl@0.0.44

using Pxl.Ui.CSharp;

var rand = Random.Shared;
var previousSeed = rand.NextDouble() * 360;
var currentSeed = rand.NextDouble() * 360;
var nextSeed = rand.NextDouble() * 360;
Color[]? pixels = null;
var lastSecond = -1;
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

var backgroundColor = Color.FromHsv360(195, 0.9, 0.2).WithAlpha(0.4);

var scene = PxlApp.CreateScene(ctx =>
{
    var now = ctx.Now;
    var second = now.Second;
    var minute = now.Minute;

    // On minute change: shift seeds
    if (minute != lastMinute)
    {
        lastMinute = minute;
        previousSeed = currentSeed;
        currentSeed = nextSeed;
        nextSeed = rand.NextDouble() * 360;
    }

    // On second change: recalculate pixels
    if (second != lastSecond)
    {
        lastSecond = second;

        var (previousAngle, previousDirection) = GetShorterArc(previousSeed, currentSeed);
        var (currentAngle, currentDirection) = GetShorterArc(currentSeed, nextSeed);
        var previousAnglePerStep = previousAngle / 60.0;
        var currentAnglePerStep = currentAngle / 60.0;

        Color GetColor(int s, int step)
        {
            if (s <= second)
            {
                var value = currentSeed + currentAnglePerStep * s * currentDirection;
                return Color.FromHsv360(
                    value > 0 ? value : 360.0 + value,
                    1.0 - step * 0.15,
                    1.0 - (double)(second - s) / 100.0 - step * 0.05);
            }
            else
            {
                var value = previousSeed + previousAnglePerStep * s * previousDirection;
                return Color.FromHsv360(
                    value > 0 ? value : 360.0 + value,
                    1.0 - step * 0.15,
                    1.0 - (double)((second - s + 60) % 60) / 100.0 - step * 0.05);
            }
        }

        pixels = new Color[576];
        for (var i = 0; i < 576; i++)
            pixels[i] = backgroundColor;

        void Set(int x, int y, Color color) => pixels[x + y * 24] = color;

        for (var s = 0; s <= 6; s++)
        {
            Set(12 + s, 4, GetColor(s, 0));
            Set(12 + s, 3, GetColor(s, 1));
            Set(12 + s, 2, GetColor(s, 2));
            Set(12 + s, 1, GetColor(s, 3));
            Set(12 + s, 0, GetColor(s, 4));
        }

        // s=7: corner top-right diagonal + fills
        {
            var s =7;
            Set(19, 4, GetColor(s, 0));
            Set(20, 3, GetColor(s, 1));
            Set(21, 2, GetColor(s, 2));
            Set(22, 1, GetColor(s, 3));
            Set(23, 0, GetColor(s, 4));

            Set(19, 3, GetColor(s, 1));
            Set(19, 2, GetColor(s, 2));
            Set(19, 1, GetColor(s, 3));
            Set(19, 0, GetColor(s, 4));

            Set(20, 2, GetColor(s, 2));
            Set(20, 1, GetColor(s, 3));
            Set(20, 0, GetColor(s, 4));

            Set(21, 1, GetColor(s, 3));
            Set(21, 0, GetColor(s, 4));

            Set(22, 0, GetColor(s, 4));
        }

        // s=8: corner top-right fills
        {
            var s =8;
            Set(20, 4, GetColor(s, 1));
            Set(21, 4, GetColor(s, 2));
            Set(22, 4, GetColor(s, 3));
            Set(23, 4, GetColor(s, 4));
            Set(21, 3, GetColor(s, 2));
            Set(22, 3, GetColor(s, 3));
            Set(23, 3, GetColor(s, 4));
            Set(22, 2, GetColor(s, 3));
            Set(23, 2, GetColor(s, 4));
            Set(23, 1, GetColor(s, 4));
        }

        for (var s = 8; s <= 21; s++)
        {
            Set(19, s - 3, GetColor(s, 0));
            Set(20, s - 3, GetColor(s, 1));
            Set(21, s - 3, GetColor(s, 2));
            Set(22, s - 3, GetColor(s, 3));
            Set(23, s - 3, GetColor(s, 4));
        }

        // s=22: corner bottom-right diagonal + fills
        {
            var s =22;
            Set(19, 19, GetColor(s, 0));
            Set(20, 20, GetColor(s, 1));
            Set(21, 21, GetColor(s, 2));
            Set(22, 22, GetColor(s, 3));
            Set(23, 23, GetColor(s, 4));
            Set(20, 19, GetColor(s, 1));
            Set(21, 19, GetColor(s, 2));
            Set(22, 19, GetColor(s, 3));
            Set(23, 19, GetColor(s, 4));
            Set(21, 20, GetColor(s, 2));
            Set(22, 20, GetColor(s, 3));
            Set(23, 20, GetColor(s, 4));
            Set(22, 21, GetColor(s, 3));
            Set(23, 21, GetColor(s, 4));
            Set(23, 22, GetColor(s, 4));
        }

        // s=23: corner bottom-right fills
        {
            var s =23;
            Set(19, 20, GetColor(s, 1));
            Set(19, 21, GetColor(s, 2));
            Set(19, 22, GetColor(s, 3));
            Set(19, 23, GetColor(s, 4));
            Set(20, 21, GetColor(s, 2));
            Set(20, 22, GetColor(s, 3));
            Set(20, 23, GetColor(s, 4));
            Set(21, 22, GetColor(s, 3));
            Set(21, 23, GetColor(s, 4));
            Set(22, 23, GetColor(s, 4));
        }

        for (var s = 23; s <= 36; s++)
        {
            Set(41 - s, 19, GetColor(s, 0));
            Set(41 - s, 20, GetColor(s, 1));
            Set(41 - s, 21, GetColor(s, 2));
            Set(41 - s, 22, GetColor(s, 3));
            Set(41 - s, 23, GetColor(s, 4));
        }

        // s=37: corner bottom-left diagonal + fills
        {
            var s =37;
            Set(4, 19, GetColor(s, 0));
            Set(3, 20, GetColor(s, 1));
            Set(2, 21, GetColor(s, 2));
            Set(1, 22, GetColor(s, 3));
            Set(0, 23, GetColor(s, 4));
            Set(4, 20, GetColor(s, 1));
            Set(4, 21, GetColor(s, 2));
            Set(4, 22, GetColor(s, 3));
            Set(4, 23, GetColor(s, 4));
            Set(3, 21, GetColor(s, 2));
            Set(3, 22, GetColor(s, 3));
            Set(3, 23, GetColor(s, 4));
            Set(2, 22, GetColor(s, 3));
            Set(2, 23, GetColor(s, 4));
            Set(1, 23, GetColor(s, 4));
        }

        // s=38: corner bottom-left fills
        {
            var s =38;
            Set(3, 19, GetColor(s, 1));
            Set(2, 19, GetColor(s, 2));
            Set(1, 19, GetColor(s, 3));
            Set(0, 19, GetColor(s, 4));
            Set(2, 20, GetColor(s, 2));
            Set(1, 20, GetColor(s, 3));
            Set(0, 20, GetColor(s, 4));
            Set(1, 21, GetColor(s, 3));
            Set(0, 21, GetColor(s, 4));
            Set(0, 22, GetColor(s, 4));
        }

        for (var s = 38; s <= 51; s++)
        {
            Set(4, 56 - s, GetColor(s, 0));
            Set(3, 56 - s, GetColor(s, 1));
            Set(2, 56 - s, GetColor(s, 2));
            Set(1, 56 - s, GetColor(s, 3));
            Set(0, 56 - s, GetColor(s, 4));
        }

        // s=52: corner top-left diagonal + fills
        {
            var s =52;
            Set(4, 4, GetColor(s, 0));
            Set(3, 3, GetColor(s, 1));
            Set(2, 2, GetColor(s, 2));
            Set(1, 1, GetColor(s, 3));
            Set(0, 0, GetColor(s, 4));
            Set(3, 4, GetColor(s, 1));
            Set(2, 4, GetColor(s, 2));
            Set(1, 4, GetColor(s, 3));
            Set(0, 4, GetColor(s, 4));
            Set(2, 3, GetColor(s, 2));
            Set(1, 3, GetColor(s, 3));
            Set(0, 3, GetColor(s, 4));
            Set(1, 2, GetColor(s, 3));
            Set(0, 2, GetColor(s, 4));
            Set(0, 1, GetColor(s, 4));
        }

        // s=53: corner top-left fills
        {
            var s =53;
            Set(4, 3, GetColor(s, 1));
            Set(4, 2, GetColor(s, 2));
            Set(4, 1, GetColor(s, 3));
            Set(4, 0, GetColor(s, 4));
            Set(3, 2, GetColor(s, 2));
            Set(3, 1, GetColor(s, 3));
            Set(3, 0, GetColor(s, 4));
            Set(2, 1, GetColor(s, 3));
            Set(2, 0, GetColor(s, 4));
            Set(1, 0, GetColor(s, 4));
        }

        for (var s = 53; s <= 59; s++)
        {
            Set(s - 48, 4, GetColor(s, 0));
            Set(s - 48, 3, GetColor(s, 1));
            Set(s - 48, 2, GetColor(s, 2));
            Set(s - 48, 1, GetColor(s, 3));
            Set(s - 48, 0, GetColor(s, 4));
        }
    }

    if (pixels != null)
        ctx.SetPixels(pixels, BlendMode.Source);

    // Time overlay
    ctx.DrawTextMono4x5($"{now:HH}", 6, 6, color: Colors.White);
    ctx.DrawTextMono4x5($"{now:mm}", 9, 13, color: Colors.White);
});

await PxlApp.SimulateAndSendToDevice(scene);
