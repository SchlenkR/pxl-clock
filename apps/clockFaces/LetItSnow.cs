// Let It Snow
// Snow particles falling around time digits displayed as ice
// Design: Urs Enzler

#:package Pxl@0.0.45

using Pxl.Ui.CSharp;

// Digit patterns (4x5 pixels)
var digits = new string[][]
{
    [ " XX ", "X XX", "XX X", "X  X", " XX " ],
    [ "  X ", " XX ", "  X ", "  X ", "  X " ],
    [ " XX ", "X  X", "  X ", " X  ", "XXXX" ],
    [ "XXX ", "   X", " XX ", "   X", "XXX " ],
    [ "X  X", "X  X", "XXXX", "   X", "   X" ],
    [ "XXXX", "X   ", "XXX ", "   X", "XXX " ],
    [ " XX ", "X   ", "XXX ", "X  X", " XX " ],
    [ "XXXX", "   X", "  X ", "  X ", "  X " ],
    [ " XX ", "X  X", " XX ", "X  X", " XX " ],
    [ " XX ", "X  X", " XXX", "   X", " XX " ],
};

const int Empty = 0, Falling = 1, Lying = 2, Ice = 3;

static void DrawDigit(string[] digit, int[] world, int x, int y)
{
    for (var r = 0; r < 5; r++)
        for (var c = 0; c < 4; c++)
            if (digit[r][c] == 'X')
                world[(r + y) * 24 + c + x] = Ice;
}

int[] CreateWorld(DateTime now)
{
    var world = new int[576];
    var (h1, h2) = (now.Hour / 10, now.Hour % 10);
    var (m1, m2) = (now.Minute / 10, now.Minute % 10);

    DrawDigit(digits[h1], world, 1, 19);
    DrawDigit(digits[h2], world, 6, 19);
    DrawDigit(digits[m1], world, 13, 19);
    DrawDigit(digits[m2], world, 18, 19);

    world[20 * 24 + 11] = Ice;
    world[22 * 24 + 11] = Ice;

    return world;
}

int[] NextGeneration(int[] world)
{
    var next = new int[576];
    var lastCreated = -2;

    for (var i = 0; i < 576; i++)
    {
        var above = i >= 24 ? world[i - 24] : -1;
        var below = i + 24 < 576 ? world[i + 24] : -1;
        var leftHill = i >= 49 ? world[i - 49] : -1;
        var rightHill = i >= 47 ? world[i - 47] : -1;
        var cur = world[i];

        if (cur == Ice) { next[i] = Ice; continue; }
        if (cur == Lying) { next[i] = Lying; continue; }

        // Top row: random snowflake creation
        if (i < 24 && cur == Empty)
        {
            if (Random.Shared.Next(10) > 8 && i > lastCreated + 1)
            {
                lastCreated = i;
                next[i] = Falling;
            }
            continue;
        }

        // Falling logic
        if (cur == Falling || above == Falling)
        {
            if (cur == Falling)
            {
                if (below == Empty) { next[i] = Empty; }
                else if (below == Lying || below == Ice || below == -1) { next[i] = Lying; }
                else next[i] = Empty;
            }
            else if (above == Falling && cur == Empty)
            {
                if (below == Empty) next[i] = Falling;
                else if (below == Lying || below == Ice || below == -1) next[i] = Lying;
                else next[i] = Falling;
            }
            continue;
        }

        // Hill building
        if (cur == Empty && (below == Lying || below == Ice || below == -1))
        {
            if (leftHill == Lying || rightHill == Lying)
            {
                next[i] = Falling;
                continue;
            }
        }

        if (above == Lying) { next[i] = Lying; continue; }
    }

    return next;
}

var snowflakeColor = Colors.White;
var emptyColor = Color.FromHsv360(200, 0.8, 0.1);
var iceColor = Color.FromHsv360(222, 0.6, 0.8);

// State
var world = CreateWorld(DateTime.Now);
var lastMinute = -1;
var lastHalfSec = -1;

var scene = PxlApp.CreateScene(ctx =>
{
    var now = ctx.Now;
    var halfSec = now.Millisecond / 500;

    if (now.Minute != lastMinute)
    {
        lastMinute = now.Minute;
        world = CreateWorld(now);
        lastHalfSec = halfSec;
    }
    else if (halfSec != lastHalfSec)
    {
        lastHalfSec = halfSec;
        world = NextGeneration(world);
    }

    // Render
    var pixels = new Color[576];
    for (var i = 0; i < 576; i++)
    {
        pixels[i] = world[i] switch
        {
            Falling => snowflakeColor,
            Lying => Color.FromHsv360(220, 0.1 + GetSnowHeight(world, i % 24, i / 24) * 0.025, 1.0),
            Ice => iceColor,
            _ => emptyColor
        };
    }
    ctx.SetPixels(pixels, BlendMode.Source);
});

static int GetSnowHeight(int[] world, int col, int row)
{
    var h = 0;
    for (var r = 0; r <= row; r++)
        if (world[r * 24 + col] is Lying or Ice) h++;
    return h;
}

await PxlApp.SimulateAndSendToDevice(scene);
