// ---
// app: NicosDog
// displayName: Nicos Dog
// appType: ClockFace
// author: Urs Enzler
// description: Pixel art dog scrolling across the screen with time overlay
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

var frame1 = new[]
{
    "                                   yyyy           g                                 ",
    "                               yyyyyyyyyyyy    ggggg                                ",
    "                                   yyyy       gggggggg                              ",
    "                               yyyyyyyyyyyy     ggg ggg                             ",
    "                                    y y          gg  g                              ",
    "                                    y y                                             ",
    "                                    y y                                             ",
    "                                    y y                                             ",
    "                                                                                    ",
    "                                            e e                                     ",
    "                                          e eeee                                    ",
    "                                         eeeeeee    lll lll                         ",
    "                                          eeeee     lfl lfl                         ",
    "     _               ee  e               eeeeeeee   lfl lfl        _                ",
    "    ___      _      eeeeeeee              e be e    ddddddd       ___      _        ",
    "   _____    ___      eeeeee                 bb      dcdddcd      _____    ___       ",
    "  ______________    eeeeeeee                bb      ddddddd     ______________      ",
    "_________________    e be e                 bb      ddcccdd    ________________     ",
    "___________________    bb                   bb       ddddd    __________________    ",
    "_____________________  bb                   bb       ddddd   _____________________  ",
    "_______________________bb           ________tdddddddddddddd_________________________",
    "_______________________bb__________________ttdddddddddddddd_________________________",
    "_______________________bb__________________tbdd_dd____dd_dd_________________________",
    "_____________________________________________dd_dd____dd_dd_________________________",
};

var frame2 = new[]
{
    "                               yyyyyyyyyyyy      gg                                 ",
    "                                   yyyy         ggggg                               ",
    "                               yyyyyyyyyyyy    ggggggg                              ",
    "                                   yyyy         ggg ggg                             ",
    "                                   y y          gg  gg                              ",
    "                                   y y                                              ",
    "                                   y y                                              ",
    "                                   y y                                              ",
    "                                                                                    ",
    "                                            e e                                     ",
    "                                          e eeee                                    ",
    "                                         eeeeeee    lll lll                         ",
    "                                          eeeee     lfl lfl                         ",
    "     _               ee  e               eeeeeeee   lfl lfl        _                ",
    "    ___      _      eeeeeeee              e be e    ddddddd       ___      _        ",
    "   _____    ___      eeeeee                 bb      dcdddcd      _____    ___       ",
    "  ______________    eeeeeeee                bb      ddddddd     ______________      ",
    "_________________    e be e                 bb      ddcccdd    ________________     ",
    "___________________    bb                   bb       ddddd    __________________    ",
    "_____________________  bb                   bb       ddddd   _____________________  ",
    "_______________________bb           ________tdddddddddddddd_________________________",
    "_______________________bb__________________ttdddddddddddddd_________________________",
    "_______________________bb_________________ttbdd_dd____dd_dd_________________________",
    "______________________________________________dd_dd____dd_dd________________________",
};

static Color CharToColor(char c) => c switch
{
    'b' => Colors.Brown,
    'y' => Colors.Yellow,
    'd' => Colors.SaddleBrown,
    'e' => Colors.Green,
    'g' => Colors.Gray,
    't' => Colors.RosyBrown,
    'l' => Colors.SaddleBrown,
    'f' => Colors.SandyBrown,
    'c' => Colors.Bisque,
    '_' => Colors.Beige,
    _ => Colors.Blue
};

// State
Color[]? cachedPixels = null;
var lastSecond = -1;

var scene = (RasterSurface ctx) =>
{
    var now = ctx.Now;
    var sec = now.Second;

    if (sec != lastSecond || cachedPixels == null)
    {
        lastSecond = sec;
        var frame = sec % 2 == 0 ? frame1 : frame2;
        var pixels = new Color[576];

        for (var row = 0; row < 24; row++)
        {
            var line = frame[row];
            var start = line.Length - 1 - sec - 24;
            if (start < 0) start = 0;
            var slice = line.Substring(start, Math.Min(24, line.Length - start));

            for (var col = 0; col < Math.Min(24, slice.Length); col++)
                pixels[row * 24 + col] = CharToColor(slice[col]);
        }

        cachedPixels = pixels;
    }

    ctx.SetPixels(cachedPixels, BlendMode.Source);

    // Diffuser + time
    ctx.DrawRectXyWh(0, 2, 24, 9, colorFill: Color.FromArgbByte(80, 0, 0, 0), isAntialias: true);
    ctx.DrawRectXyWh(0, 3, 24, 7, colorFill: Color.FromArgbByte(80, 0, 0, 0), isAntialias: true);
    ctx.DrawTextVar4x5($"{now:HH}:{now:mm}", 1, 4, color: Colors.White);
};

