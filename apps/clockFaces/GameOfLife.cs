// ---
// app: GameOfLife
// displayName: Game Of Life
// appType: ClockFace
// author: Urs Enzler
// description: Conway's Game of Life seeded with the current time digits
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

// Digit patterns (4x5 pixels each)
var digits = new string[][]
{
    [ " XX ", "X XX", "XX X", "X  X", " XX " ],  // 0
    [ "  X ", " XX ", "  X ", "  X ", "  X " ],  // 1
    [ " XX ", "X  X", "  X ", " X  ", "XXXX" ],  // 2
    [ "XXX ", "   X", " XX ", "   X", "XXX " ],  // 3
    [ "X  X", "X  X", "XXXX", "   X", "   X" ],  // 4
    [ "XXXX", "X   ", "XXX ", "   X", "XXX " ],  // 5
    [ " XX ", "X   ", "XXX ", "X  X", " XX " ],  // 6
    [ "XXXX", "   X", "  X ", "  X ", "  X " ],  // 7
    [ " XX ", "X  X", " XX ", "X  X", " XX " ],  // 8
    [ " XX ", "X  X", " XXX", "   X", " XX " ],  // 9
};

var aliveColor = Param.Color(Color.FromHsv360(0, 0.8, 0.6), label: "Cells");
var emptyColor = Param.Color(Color.FromHsv360(200, 0.6, 0.2), label: "Background");
var speed = Param.Float(2.0, min: 0.5, max: 8.0, label: "Speed", description: "Generations per second");
var reseedMinutes = Param.Int(1, min: 1, max: 10, label: "Reseed every N minutes",
    description: "How long the evolution may run before the time digits are seeded again");
var timeOverlay = Param.Bool(true, label: "Time overlay");

void DrawDigit(string[] digit, bool[] world, int x, int y)
{
    for (var r = 0; r < 5; r++)
        for (var c = 0; c < 4; c++)
            if (digit[r][c] == 'X')
                world[(r + y) * 24 + c + x] = true;
}

bool[] CreateWorld(DateTime now)
{
    var world = new bool[576];
    var (h1, h2) = (now.Hour / 10, now.Hour % 10);
    var (m1, m2) = (now.Minute / 10, now.Minute % 10);
    var (d1, d2) = (now.Day / 10, now.Day % 10);
    var (mo1, mo2) = (now.Month / 10, now.Month % 10);

    DrawDigit(digits[h1], world, 1, 5);
    DrawDigit(digits[h2], world, 6, 5);
    DrawDigit(digits[m1], world, 13, 5);
    DrawDigit(digits[m2], world, 18, 5);
    DrawDigit(digits[d1], world, 1, 13);
    DrawDigit(digits[d2], world, 6, 13);
    DrawDigit(digits[mo1], world, 13, 13);
    DrawDigit(digits[mo2], world, 18, 13);

    // Colons and dots
    world[6 * 24 + 11] = true;
    world[8 * 24 + 11] = true;
    world[17 * 24 + 11] = true;
    world[17 * 24 + 23] = true;

    return world;
}

bool[] NextGeneration(bool[] world)
{
    var next = new bool[576];
    for (var i = 0; i < 576; i++)
    {
        var (x, y) = (i % 24, i / 24);
        var alive = 0;
        for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                var (nx, ny) = (x + dx, y + dy);
                if (nx >= 0 && nx < 24 && ny >= 0 && ny < 24 && world[ny * 24 + nx])
                    alive++;
            }
        next[i] = world[i] ? (alive == 2 || alive == 3) : (alive == 3);
    }
    return next;
}

// State
var world = CreateWorld(DateTime.Now);
var lastMinute = -1;
var lastStep = -1L;

var scene = (RasterSurface ctx) =>
{
    var now = ctx.Now;
    var step = (long)(ctx.Elapsed.TotalSeconds * speed);

    if (now.Minute != lastMinute)
    {
        lastMinute = now.Minute;
        if (now.Minute % reseedMinutes == 0)
        {
            world = CreateWorld(now);
            lastStep = step;
        }
    }
    if (step != lastStep)
    {
        lastStep = step;
        world = NextGeneration(world);
    }

    // Render world
    var pixels = new Color[576];
    for (var i = 0; i < 576; i++)
        pixels[i] = world[i] ? aliveColor : emptyColor;
    ctx.SetPixels(pixels, BlendMode.Source);

    if (timeOverlay)
    {
        ctx.DrawTextVar4x5($"{now:HH}:{now:mm}", 1, 5,
            color: Colors.White.WithAlpha(0.7));
        ctx.DrawTextVar4x5($"{now:dd}.{now:MM}.", 1, 13,
            color: Colors.White.WithAlpha(0.7));
    }
};
