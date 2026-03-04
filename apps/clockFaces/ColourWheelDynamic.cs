// ---
// app: ColourWheelDynamic
// displayName: Colour Wheel Dynamic
// appType: ClockFace
// author: Urs Enzler
// description: Dynamic HSV color wheel where all positions are always visible
// ---

#:package Pxl@0.0.57

using Pxl.Ui.CSharp;

var bgColor = Color.FromHsv360(195, 0.9, 0.2).WithAlpha(0.4);

// State: pixel buffer recalculated when second changes
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

        // Reset pixels to background
        for (var i = 0; i < 576; i++)
            pixels[i] = bgColor;

        Color GetColor(int s, int step)
        {
            double h, sat, v;
            if (s <= second)
            {
                h = (double)((minute * 60) + s);
                sat = 1.0 - (step * 0.15);
                v = 1.0 - (double)(second - s) / 100.0 - step * 0.05;
            }
            else
            {
                h = (double)((((minute - 1 + 60) % 60) * 60) + s + 1);
                sat = 1.0 - (step * 0.15);
                v = 1.0 - (double)((second - s + 60) % 60) / 100.0 - step * 0.05;
            }

            // Clamp values
            h = ((h % 360) + 360) % 360;
            sat = Math.Clamp(sat, 0, 1);
            v = Math.Clamp(v, 0, 1);

            return Color.FromHsv360(h, sat, v);
        }

        void Set(int x, int y, Color c)
        {
            if (x >= 0 && x < 24 && y >= 0 && y < 24)
                pixels[x + y * 24] = c;
        }

        // s = 0..6: top edge, middle section
        for (var s = 0; s <= 6; s++)
        {
            Set(12 + s, 4, GetColor(s, 0));
            Set(12 + s, 3, GetColor(s, 1));
            Set(12 + s, 2, GetColor(s, 2));
            Set(12 + s, 1, GetColor(s, 3));
            Set(12 + s, 0, GetColor(s, 4));
        }

        // s = 7: top-right corner diagonal + fills
        {
            var s =7;
            Set(19, 4, GetColor(s, 0));
            Set(20, 3, GetColor(s, 1));
            Set(21, 2, GetColor(s, 2));
            Set(22, 1, GetColor(s, 3));
            Set(23, 0, GetColor(s, 4));
            Set(19, 3, GetColor(s, 1)); Set(19, 2, GetColor(s, 2)); Set(19, 1, GetColor(s, 3)); Set(19, 0, GetColor(s, 4));
            Set(20, 2, GetColor(s, 2)); Set(20, 1, GetColor(s, 3)); Set(20, 0, GetColor(s, 4));
            Set(21, 1, GetColor(s, 3)); Set(21, 0, GetColor(s, 4));
            Set(22, 0, GetColor(s, 4));
        }

        // s = 8: top-right corner fills
        {
            var s =8;
            Set(20, 4, GetColor(s, 1)); Set(21, 4, GetColor(s, 2)); Set(22, 4, GetColor(s, 3)); Set(23, 4, GetColor(s, 4));
            Set(21, 3, GetColor(s, 2)); Set(22, 3, GetColor(s, 3)); Set(23, 3, GetColor(s, 4));
            Set(22, 2, GetColor(s, 3)); Set(23, 2, GetColor(s, 4));
            Set(23, 1, GetColor(s, 4));
        }

        // s = 8..21: right edge
        for (var s = 8; s <= 21; s++)
        {
            Set(19, s - 3, GetColor(s, 0)); Set(20, s - 3, GetColor(s, 1)); Set(21, s - 3, GetColor(s, 2)); Set(22, s - 3, GetColor(s, 3)); Set(23, s - 3, GetColor(s, 4));
        }

        // s = 22: bottom-right corner diagonal + fills
        {
            var s =22;
            Set(19, 19, GetColor(s, 0)); Set(20, 20, GetColor(s, 1)); Set(21, 21, GetColor(s, 2)); Set(22, 22, GetColor(s, 3)); Set(23, 23, GetColor(s, 4));
            Set(20, 19, GetColor(s, 1)); Set(21, 19, GetColor(s, 2)); Set(22, 19, GetColor(s, 3)); Set(23, 19, GetColor(s, 4));
            Set(21, 20, GetColor(s, 2)); Set(22, 20, GetColor(s, 3)); Set(23, 20, GetColor(s, 4));
            Set(22, 21, GetColor(s, 3)); Set(23, 21, GetColor(s, 4));
            Set(23, 22, GetColor(s, 4));
        }

        // s = 23: bottom-right corner fills
        {
            var s =23;
            Set(19, 20, GetColor(s, 1)); Set(19, 21, GetColor(s, 2)); Set(19, 22, GetColor(s, 3)); Set(19, 23, GetColor(s, 4));
            Set(20, 21, GetColor(s, 2)); Set(20, 22, GetColor(s, 3)); Set(20, 23, GetColor(s, 4));
            Set(21, 22, GetColor(s, 3)); Set(21, 23, GetColor(s, 4));
            Set(22, 23, GetColor(s, 4));
        }

        // s = 23..36: bottom edge
        for (var s = 23; s <= 36; s++)
        {
            Set(41 - s, 19, GetColor(s, 0)); Set(41 - s, 20, GetColor(s, 1)); Set(41 - s, 21, GetColor(s, 2)); Set(41 - s, 22, GetColor(s, 3)); Set(41 - s, 23, GetColor(s, 4));
        }

        // s = 37: bottom-left corner diagonal + fills
        {
            var s =37;
            Set(4, 19, GetColor(s, 0)); Set(3, 20, GetColor(s, 1)); Set(2, 21, GetColor(s, 2)); Set(1, 22, GetColor(s, 3)); Set(0, 23, GetColor(s, 4));
            Set(4, 20, GetColor(s, 1)); Set(4, 21, GetColor(s, 2)); Set(4, 22, GetColor(s, 3)); Set(4, 23, GetColor(s, 4));
            Set(3, 21, GetColor(s, 2)); Set(3, 22, GetColor(s, 3)); Set(3, 23, GetColor(s, 4));
            Set(2, 22, GetColor(s, 3)); Set(2, 23, GetColor(s, 4));
            Set(1, 23, GetColor(s, 4));
        }

        // s = 38: bottom-left corner fills
        {
            var s =38;
            Set(3, 19, GetColor(s, 1)); Set(2, 19, GetColor(s, 2)); Set(1, 19, GetColor(s, 3)); Set(0, 19, GetColor(s, 4));
            Set(2, 20, GetColor(s, 2)); Set(1, 20, GetColor(s, 3)); Set(0, 20, GetColor(s, 4));
            Set(1, 21, GetColor(s, 3)); Set(0, 21, GetColor(s, 4));
            Set(0, 22, GetColor(s, 4));
        }

        // s = 38..51: left edge
        for (var s = 38; s <= 51; s++)
        {
            Set(4, 56 - s, GetColor(s, 0)); Set(3, 56 - s, GetColor(s, 1)); Set(2, 56 - s, GetColor(s, 2)); Set(1, 56 - s, GetColor(s, 3)); Set(0, 56 - s, GetColor(s, 4));
        }

        // s = 52: top-left corner diagonal + fills
        {
            var s =52;
            Set(4, 4, GetColor(s, 0)); Set(3, 3, GetColor(s, 1)); Set(2, 2, GetColor(s, 2)); Set(1, 1, GetColor(s, 3)); Set(0, 0, GetColor(s, 4));
            Set(3, 4, GetColor(s, 1)); Set(2, 4, GetColor(s, 2)); Set(1, 4, GetColor(s, 3)); Set(0, 4, GetColor(s, 4));
            Set(2, 3, GetColor(s, 2)); Set(1, 3, GetColor(s, 3)); Set(0, 3, GetColor(s, 4));
            Set(1, 2, GetColor(s, 3)); Set(0, 2, GetColor(s, 4));
            Set(0, 1, GetColor(s, 4));
        }

        // s = 53: top-left corner fills
        {
            var s =53;
            Set(4, 3, GetColor(s, 1)); Set(4, 2, GetColor(s, 2)); Set(4, 1, GetColor(s, 3)); Set(4, 0, GetColor(s, 4));
            Set(3, 2, GetColor(s, 2)); Set(3, 1, GetColor(s, 3)); Set(3, 0, GetColor(s, 4));
            Set(2, 1, GetColor(s, 3)); Set(2, 0, GetColor(s, 4));
            Set(1, 0, GetColor(s, 4));
        }

        // s = 53..59: top edge, left section
        for (var s = 53; s <= 59; s++)
        {
            Set(s - 48, 4, GetColor(s, 0)); Set(s - 48, 3, GetColor(s, 1)); Set(s - 48, 2, GetColor(s, 2)); Set(s - 48, 1, GetColor(s, 3)); Set(s - 48, 0, GetColor(s, 4));
        }
    }

    ctx.SetPixels(pixels, BlendMode.Source);

    // Time display
    ctx.DrawTextMono4x5($"{now:HH}", 6, 6, color: Colors.White);
    ctx.DrawTextMono4x5($"{now:mm}", 9, 13, color: Colors.White);
};

