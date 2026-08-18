// ---
// app: TinyTales
// displayName: Tiny Tales
// author: Claude
// description: Colour field with a running wave and a seconds arc; every full minute one of eleven short stories plays and ends with the time
// appType: ClockFace
// ---

// Eight short stories, one per minute, never the same one twice in a row. Each ends with
// the time filling the screen and then collapsing into the small readout.
//
// With the cast - cat, crate, bird, cheese, mouse:
//   1 Delivery   the cheese drops in, the mouse gets there first, the cat lands too late
//   2 Chase      mouse and cheese race past, the cat brakes in the middle and finds nothing
//   3 Trap       a trap is set, the light comes on, the crate catches the wrong animal
//   4 Window     three shelves run as conveyor belts, each its own chase
//   5 Tumble     cat and cheese tumble through in throwing arcs
//
// Without the cast, other mechanics:
//   6 FlipBoard  a split-flap display steps through, staggered per row
//   7 Fold       the field creases like paper and opens up white
//   8 Spotlight  a cone searches the field and finds one of the cast, then the time

#:package Pxl@*

using Pxl.Ui.CSharp;

var baseTone = Param.Color(Color.FromRgbByte(255, 121, 26), label: "Base colour");

var shadowStrength = Param.Float(0.40, min: 0.0, max: 1.0, label: "Drop shadow");
var secondsRadius = Param.Float(12.0, min: 5.0, max: 16.0, label: "Seconds radius");
var secondsOpacity = Param.Float(0.95, min: 0.0, max: 1.0, label: "Seconds ring");

// All times in seconds; each act runs its own schedule, stretched to fit the duration.
var actDuration = Param.Float(7.0, min: 3.0, max: 15.0, label: "Act duration");
var calmDuration = Param.Float(2.0, min: 1.5, max: 20.0, label: "Calm");
var holdTime = Param.Float(1.2, min: 0.0, max: 6.0, label: "Hold time");
var waveDuration = Param.Float(2.2, min: 1.5, max: 5.0, label: "Wave duration");
var previewInterval = Param.Float(9.0, min: 3.0, max: 30.0, label: "Preview interval");

var everyNMinutes = Param.Int(1, min: 1, max: 15, label: "Play every N minutes");
var preview = Param.Bool(false, label: "Preview");

// Only the ticked acts take part in the draw.
var actDelivery = Param.Bool(true, label: "1 Delivery");
var actChase = Param.Bool(true, label: "2 Chase");
var actTrap = Param.Bool(true, label: "3 Trap");
var actWindow = Param.Bool(true, label: "4 Window");
var actTumble = Param.Bool(true, label: "5 Tumble");
var actFlipBoard = Param.Bool(true, label: "6 Flip board");
var actFold = Param.Bool(true, label: "7 Fold");
var actSpotlight = Param.Bool(true, label: "8 Spotlight");

// Paper white is the counterpart of the field and stays out of the colour family.
var paper = Color.FromRgbByte(255, 253, 248);

// Derived per frame: the knob is live, and file scope would only ever be computed once.
var baseHue = 0.0;
var baseSaturation = 1.0;
var light = paper;
var deep = paper;
var accent = paper;
var accentLight = paper;
var edgeTone = paper;
var deepDark = paper;
var timeShadow = paper;

Color Tone(double l, double sat = 1.0, double dh = 0.0) =>
    Color.FromHsl((baseHue + dh + 1.0) % 1.0, MathH.Clamp01(baseSaturation * sat), MathH.Clamp01(l));

void SetTones()
{
    var (h, s, _) = ColorOps.ToHsl(baseTone);
    baseHue = h;
    baseSaturation = s;
    light = Tone(0.60, 1.00, 0.040);
    deep = Tone(0.42, 0.78, -0.043);
    accent = Tone(0.46, 0.85, -0.003);
    accentLight = Tone(0.59, 0.88, 0.014);
    edgeTone = Tone(0.32, 0.85, -0.030);
    deepDark = Tone(0.20, 0.88, -0.035);
    timeShadow = Tone(0.60, 0.42, 0.010);
}

CyclePicker actPicker = null;

var sideA = new RasterSurface(24, 24);
var sideB = new RasterSurface(24, 24);
var fieldWithTime = new RasterSurface(24, 24);
var fieldPlain = new RasterSurface(24, 24);
var paperPlain = new RasterSurface(24, 24);
var paperWithTime = new RasterSurface(24, 24);

var bigTime = new BakedSurface(24, 24);
var bigTop = new BakedSurface(24, 24);
var bigBottom = new BakedSurface(24, 24);
var timeOnPaper = new BakedSurface(24, 24);
var timeOnField = new BakedSurface(24, 24);


// Neutral on purpose - dark silhouettes read on any base colour. Only the cheese is yellow.
var fur = new Dictionary<char, Color>
{
    ['k'] = Color.FromRgbByte(34, 36, 46),
    ['d'] = Color.FromRgbByte(74, 78, 92),
    ['g'] = Color.FromRgbByte(104, 112, 130),
    ['h'] = Color.FromRgbByte(176, 183, 198),
    ['w'] = Color.FromRgbByte(240, 244, 250),
    ['p'] = Color.FromRgbByte(226, 152, 156),
    ['a'] = Color.FromRgbByte(255, 214, 86),
    ['s'] = Color.FromRgbByte(255, 176, 48),
    ['c'] = Color.FromRgbByte(255, 216, 96),
    ['y'] = Color.FromRgbByte(196, 134, 28),
};

// Index 0..5: cat sitting, crate, bird, cheese, mouse, cat crouching.
var cast = new[]
{
    // 0 - Katze sitzend
    PixelSprite.FromRows(new[]
    {
        ".kk...kk.....",
        ".kdk.kdk..kk.",
        ".kdddddddk.kk",
        ".kdadddadk.kd",
        ".kdddddddk.kd",
        ".kddwpwddk.kd",
        "..kdddddk..kd",
        "..kdddddk.kd.",
        ".kdddddddkkd.",
        ".kddwwwddddk.",
        ".kddwwwdddk..",
        ".kdddddddk...",
        ".kdk..kdk....",
    }, fur),
    // 1 - Kiste
    PixelSprite.FromRows(new[]
    {
        "wwwwwwwww",
        "wkkkkkkkw",
        "wkgggggkw",
        "wkgggggkw",
        "wkkkkkkkw",
        "wkgggggkw",
        "wkgggggkw",
        "wkkkkkkkw",
        "wkgggggkw",
        "wkgggggkw",
        "wwwwwwwww",
    }, fur),
    // 2 - Vogel
    PixelSprite.FromRows(new[]
    {
        "...kk....",
        "..kkkk...",
        "..kkkk...",
        "ss.kakk..",
        "..kkkkk..",
        ".kwwkkkk.",
        ".kwwkkkk.",
        ".kwwkkkk.",
        "..kkkkkk.",
        "...kkkkkk",
        "....kk.kk",
    }, fur),
    // 3 - Kaese
    PixelSprite.FromRows(new[]
    {
        "....yyyy",
        "..yyccck",
        "yykccckc",
        "ycccccyy",
        "yckccccy",
        "yyyyyyyy",
    }, fur),
    // 4 - Maus, stehend
    PixelSprite.FromRows(new[]
    {
        "...kkk.....",
        "..khphk....",
        ".khhhhkkk.k",
        "khhahhhhhhk",
        "khhhhhhhhkk",
        ".khhhhhhhk.",
        "..kkhhhkk..",
        "...k.k.k...",
    }, fur),
    // 5 - Katze geduckt, kurz vor dem Absprung
    PixelSprite.FromRows(new[]
    {
        "kk.kk..........",
        "kdkdk..........",
        "kddddk.......kk",
        "kddadkk.....kk.",
        "kdddwddkk..kk..",
        ".kdddddddkkkk..",
        ".kddddddddddk..",
        "..kdddddddddk..",
        "..kdddddddddk..",
        "..kddk...kddk..",
        "..kkk.....kkk..",
    }, fur),
};

// Galopp in drei Phasen und Trippeln in zwei: nur die Beine wechseln, Rumpf und Kopf
// bleiben stehen, sonst flackert die Figur.
var catRun = new SpriteAnimation(1.0,
    PixelSprite.FromRows(new[]
    {
        ".kk.kk.......kk",
        ".kdkdk......kk.",
        "kdddddk....kk..",
        "kdadadkkkkkk...",
        "kdwpwdddddddk..",
        ".kdddddddddddk.",
        "..kddddddddddkk",
        "...kdk....kdk.k",
        "..k..kk..k.kk..",
        "..kk....kk.....",
    }, fur),
    PixelSprite.FromRows(new[]
    {
        ".kk.kk.......kk",
        ".kdkdk......kk.",
        "kdddddk....kk..",
        "kdadadkkkkkk...",
        "kdwpwdddddddk..",
        ".kdddddddddddk.",
        "..kddddddddddkk",
        "....kdkkkkdk...",
        "....kdk..kdk...",
        "....kkk..kkk...",
    }, fur),
    PixelSprite.FromRows(new[]
    {
        ".kk.kk.......kk",
        ".kdkdk......kk.",
        "kdddddk....kk..",
        "kdadadkkkkkk...",
        "kdwpwdddddddk..",
        ".kdddddddddddk.",
        "..kddddddddddkk",
        ".kkdk.....kdkk.",
        "kk.k......k..kk",
        "..kk...........",
    }, fur));

var mouseRun = new SpriteAnimation(1.0,
    PixelSprite.FromRows(new[]
    {
        "...kkk.....",
        "..khphk....",
        ".khhhhkkk.k",
        "khhahhhhhhk",
        "khhhhhhhhkk",
        ".khhhhhhhk.",
        "..kkhhhkk..",
        "...k.k.k...",
    }, fur),
    PixelSprite.FromRows(new[]
    {
        "...kkk.....",
        "..khphk....",
        ".khhhhkkk.k",
        "khhahhhhhhk",
        "khhhhhhhhkk",
        ".khhhhhhhk.",
        "..kkhhhkk..",
        "..k..k..k..",
    }, fur));

var rimTone = Color.FromRgbByte(255, 246, 226);
var frameTime = 0.0;
var spotPick = 0;
PixelAssembly flyIn = null;

int FigureWidth(int nr) => cast[nr].Width;

int FigureHeight(int nr) => cast[nr].Height;

// Figure with a bright rim along its top edge, so it stands out however dark the field is.
void Figure(RasterSurface ctx, int nr, double x0, double y0, bool flipX = false)
{
    var sprite = cast[nr];
    var x = (int)Math.Round(x0);
    var y = (int)Math.Round(y0);
    var rim = ctx.CreateSurface(24, 24);
    sprite.Draw(rim, x, y - 1, flipX: flipX);
    for (var py = 0; py < 24; py++)
    for (var px = 0; px < 24; px++)
        if (rim[px, py].A > 0.0) ctx[px, py] = ColorOps.Lerp(ctx[px, py], rimTone, 0.45);
    sprite.Draw(ctx, x, y, flipX: flipX);
}

// Without the rim - for shadows, ghosts and anything processed further.
void FigureRaw(RasterSurface ctx, int nr, double x0, double y0, bool flipX = false) =>
    cast[nr].Draw(ctx, (int)Math.Round(x0), (int)Math.Round(y0), flipX: flipX);

// The same, but cat and mouse move their legs while they travel.
void Running(RasterSurface ctx, int nr, double x0, double y0, bool flipX = false)
{
    var sprite = Stride(nr, x0);
    var x = (int)Math.Round(x0);
    var y = (int)Math.Round(y0);
    var rim = ctx.CreateSurface(24, 24);
    sprite.Draw(rim, x, y - 1, flipX: flipX);
    for (var py = 0; py < 24; py++)
    for (var px = 0; px < 24; px++)
        if (rim[px, py].A > 0.0) ctx[px, py] = ColorOps.Lerp(ctx[px, py], rimTone, 0.45);
    sprite.Draw(ctx, x, y, flipX: flipX);
}

void RunningRaw(RasterSurface ctx, int nr, double x0, double y0, bool flipX = false) =>
    Stride(nr, x0).Draw(ctx, (int)Math.Round(x0), (int)Math.Round(y0), flipX: flipX);

// Ein Schritt alle paar Pixel Weg - dadurch stehen die Beine still, sobald die Figur steht.
PixelSprite Stride(int nr, double x) => nr switch
{
    0 => catRun.At(-x / 3.0),
    4 => mouseRun.At(-x / 2.0),
    _ => cast[nr],
};

int RunWidth(int nr) => nr == 0 ? 15 : nr == 4 ? 11 : cast[nr].Width;

int RunHeight(int nr) => nr == 0 ? 10 : nr == 4 ? 8 : cast[nr].Height;

var scene = (RasterSurface ctx) =>
{
    var t = ctx.Elapsed.TotalSeconds;
    var now = ctx.Now;
    frameTime = t;
    spotPick = (int)((now.Hour * 60L + now.Minute) % 3);
    SetTones();

    var stamp = $"{now:HHmm}{baseTone.R:F2}{baseTone.G:F2}{baseTone.B:F2}";
    bigTime.Ensure($"whole{stamp}{shadowStrength:F2}", s => SmallTime(s, now, accent, timeShadow));
    bigTop.Ensure($"top{stamp}{shadowStrength:F2}", s => SmallTime(s, now, accent, timeShadow, 1));
    bigBottom.Ensure($"bottom{stamp}{shadowStrength:F2}", s => SmallTime(s, now, accent, timeShadow, 2));

    var timeChanged = timeOnPaper.Ensure($"{stamp}{shadowStrength:F2}", s => SmallTime(s, now, accent, timeShadow));
    if (timeChanged || flyIn is null)
    {
        var targets = PixelTargets.From(timeOnPaper.Surface, alphaThreshold: 0.4);
        flyIn = new PixelAssembly(targets)
        {
            Duration = 0.62,
            Spread = 15.0,
            Gravity = 0.0,
            FlightColor = accentLight,
            DelayOf = i => Scatter.Value(targets[i].X, targets[i].Y) * 0.34,
        };
    }
    timeOnField.Ensure($"{stamp}{shadowStrength:F2}", s => SmallTime(s, now, Colors.White, deepDark));

    Ground(t, now);

    paperPlain.DrawBackground(paper);
    paperWithTime.DrawBackground(paper);
    paperWithTime.DrawSurface(timeOnPaper.Surface);

    var (playing, progress, which) = PickAct(t, now);
    if (playing)
    {
        switch (which)
        {
            case 0: ActDelivery(ctx, progress); break;
            case 1: ActChase(ctx, progress); break;
            case 2: ActTrap(ctx, progress); break;
            case 3: ActWindow(ctx, progress); break;
            case 4: ActTumble(ctx, progress); break;
            case 5: ActFlipBoard(ctx, progress); break;
            case 6: ActFold(ctx, progress); break;
            default: ActSpotlight(ctx, progress); break;
        }
        return;
    }

    ctx.DrawSurface(fieldWithTime);
};

// ================================ Building blocks ================================

// An act starts at second 0 of the minute, so it ends on the time that just began.
(bool Playing, double Progress, int Which) PickAct(double t, DateTime now)
{
    var enabled = EnabledActs();
    if (enabled.Count == 0) return (false, 0.0, 0);
    if (actPicker is null || actPicker.Count != enabled.Count)
        actPicker = new CyclePicker(enabled.Count, avoidRepeat: true, seed: 20260801);

    var total = actDuration + holdTime;

    if (preview)
    {
        var cycle = Math.Max(total + 0.5, previewInterval);
        var number = (long)Math.Floor(t / cycle);
        var local = t - number * cycle;
        actPicker.Update(number);
        var which = enabled[actPicker.Current];
        return local < total ? (true, WithHold(local, which), which) : (false, 0.0, which);
    }

    var minuteNo = now.Hour * 60L + now.Minute;
    actPicker.Update(minuteNo);
    var w = enabled[actPicker.Current];
    if (now.Minute % everyNMinutes != 0) return (false, 0.0, w);

    var second = now.Second + now.Millisecond / 1000.0;
    return second < total ? (true, WithHold(second, w), w) : (false, 0.0, w);
}

// Progress freezes while the big time is held; the rest of the act is unaffected.
double WithHold(double second, int act)
{
    var point = HoldPoint(act);
    var hold = point * actDuration;
    if (second < hold) return second / actDuration;
    if (second < hold + holdTime) return point;
    return (second - holdTime) / actDuration;
}

// The moment (0..1) at which the big time stands complete, just before it dissolves.
double HoldPoint(int act) => act switch
{
    0 => 3.18 / 4.70,
    1 => 2.70 / 3.90,
    2 => 4.10 / 5.90,
    3 => 5.50 / 6.80,
    4 => 2.25 / 3.85,
    5 => 1.87 / 4.45,
    6 => 2.74 / 3.62,
    _ => 2.65 / 4.10,
};

// Indices of the ticked acts, in the order of the header comment.
List<int> EnabledActs()
{
    var flags = new[]
    {
        actDelivery, actChase, actTrap, actWindow, actTumble,
        actFlipBoard, actFold, actSpotlight,
    };
    var enabled = new List<int>();
    for (var i = 0; i < flags.Length; i++)
        if (flags[i]) enabled.Add(i);
    return enabled;
}

// The digits fly in pixel by pixel on the white sheet - carries the end of every act.
void TimeFlyIn(RasterSurface ctx, double seconds)
{
    ctx.DrawSurface(paperPlain);
    flyIn.Draw(ctx, seconds);
}

// Builds fieldWithTime: gradient field with running wave, small time and seconds arc.
void Ground(double t, DateTime now)
{
    Side(sideA, light, deep);
    Side(sideB, deep, light);

    var calm = calmDuration;
    var wave = waveDuration;
    var cycle = calm + wave;
    var number = (int)Math.Floor(t / cycle);
    var resting = number % 2 == 0 ? sideA : sideB;
    var incoming = number % 2 == 0 ? sideB : sideA;
    var local = t - number * cycle;

    if (local < calm)
        CopyInto(fieldWithTime, resting);
    else
        Transitions.Iris(fieldWithTime, resting, incoming, Easings.EaseOutCubic((local - calm) / wave), softness: 0.5);

    CopyInto(fieldPlain, fieldWithTime);
    SecondsArc(fieldPlain, now);
    fieldWithTime.DrawSurface(timeOnField.Surface);
    SecondsArc(fieldWithTime, now);
}

void Side(RasterSurface s, Color from, Color to)
{
    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
        s[x, y] = ColorOps.Lerp(from, to, (x + y) / 46.0);
}

void CopyInto(RasterSurface target, RasterSurface source)
{
    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
        target[x, y] = source[x, y];
}

void SecondsArc(RasterSurface s, DateTime now)
{
    var share = (now.Second + now.Millisecond / 1000.0) / 60.0;
    if (share <= 0.0) return;
    var circumference = Math.PI * 2.0 * secondsRadius;

    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
    {
        var dx = x + 0.5 - 12.0;
        var dy = y + 0.5 - 12.0;
        var cover = Falloff.Ring(Math.Sqrt(dx * dx + dy * dy), secondsRadius, 1.1);
        if (cover <= 0.004) continue;
        var wo = Math.Atan2(dy, dx) + Math.PI / 2.0;
        if (wo < 0.0) wo += Math.PI * 2.0;
        var hier = wo / (Math.PI * 2.0);
        if (hier > share) continue;
        var head = MathH.Clamp01((share - hier) * circumference * 2.0);
        s[x, y] = ColorOps.Lerp(s[x, y], Colors.White, cover * head * secondsOpacity);
    }
}

// part 0 draws the whole readout, 1 only the hours, 2 only the minutes with the colon.
void SmallTime(RasterSurface s, DateTime now, Color color, Color shadow, int part = 0)
{
    ClearSurface(s);
    var sch = shadow.WithAlpha(shadowStrength);
    if (part != 2)
    {
        s.DrawTextAdafruitClassic($"{now:HH}", 4, 5, sch);
        s.DrawTextAdafruitClassic($"{now:HH}", 3, 4, color);
    }
    if (part == 1) return;
    s.DrawTextAdafruitClassic($"{now:mm}", 10, 14, sch);
    s[7, 16] = sch;
    s[7, 18] = sch;
    s.DrawTextAdafruitClassic($"{now:mm}", 9, 13, color);
    s[6, 15] = color;
    s[6, 17] = color;
}

void Glyph(RasterSurface s, string[] rows, int x0, int y0, Color top, Color bottom)
{
    for (var r = 0; r < rows.Length; r++)
    for (var c = 0; c < rows[r].Length; c++)
    {
        if (rows[r][c] != '#') continue;
        var x = x0 + c;
        var y = y0 + r;
        if (x < 0 || x >= 24 || y < 0 || y >= 24) continue;
        s[x, y] = ColorOps.Lerp(top, bottom, r / (double)(rows.Length - 1));
    }
}

void ClearSurface(RasterSurface s)
{
    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
        s[x, y] = Colors.TransparentBlack;
}

// ---------------------------------------------------------------- act: delivery

// The stamp: a white sheet twice the screen height, the big time in its lower half.
RasterSurface DeliveryStampSheet(RasterSurface ctx)
{
    var white = paperPlain[0, 0];
    var s = ctx.CreateSurface(24, 48);
    for (var y = 0; y < 48; y++)
    for (var x = 0; x < 24; x++)
        s[x, y] = white;
    s.DrawSurface(bigTime.Surface, y: 24);
    return s;
}

void DeliveryFigureSoft(RasterSurface ctx, int nr, double x, double y, double alpha)
{
    var s = ctx.CreateSurface(24, 24);
    Figure(s, nr, x, y);
    ctx.DrawSurface(s, alpha: alpha);
}

void DeliveryDust(RasterSurface ctx, double x0, double y0, double width, double age, Color color)
{
    const double life = 0.30;
    if (age < 0.0 || age >= life) return;
    var u = age / life;
    for (var i = 0; i < 10; i++)
    {
        var side = i % 2 == 0 ? -1.0 : 1.0;
        var r = (i * 37 % 11) / 11.0;
        var x = x0 + side * (width * 0.5 + u * (2.5 + r * 5.0));
        var y = y0 - u * (5.0 + r * 5.0) + u * u * 7.0;
        var px = (int)Math.Round(x);
        var py = (int)Math.Round(y);
        if (px < 0 || px > 23 || py < 0 || py > 23) continue;
        ctx[px, py] = ColorOps.Lerp(ctx[px, py], color, (1.0 - u) * 0.95);
    }
}

void DeliveryFlash(RasterSurface ctx, double k)
{
    if (k <= 0.0) return;
    var a = MathH.Clamp01(k);
    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
        ctx[x, y] = ColorOps.Lerp(ctx[x, y], Colors.White, a);
}

// The mouse takes the cheese away and the cat lands where it used to be.
void ActDelivery(RasterSurface ctx, double p)
{
    const double deliveryTotal = 4.70;
    const double cheeseOn = 0.55;
    const double mouseOff = 0.55;
    const double mouseOn = 1.10;
    const double grabOff = 1.30;
    const double grabOn = 1.85;
    const double catOff = 1.60;
    const double catOn = 2.05;
    const double stampOff = 2.30;
    const double stampOn = 2.60;
    const double settleOn = 2.82;
    const double holdOn = 3.18;
    const double dissolveOn = 4.25;

    var s = p * deliveryTotal;

    if (s < stampOn)
    {
        ctx.DrawSurface(fieldPlain);
        var jolt = Shake.At(s - catOn, amplitude: 1.6, duration: 0.24, frequency: 40.0);
        var grab = Easings.EaseInCubic(MathH.Clamp01((s - grabOff) / (grabOn - grabOff)));

        var drop = Easings.EaseOutBounce(MathH.Clamp01(s / cheeseOn));
        var kx = MathH.Lerp(8.0, -18.0, grab);
        if (s < cheeseOn) DeliveryFigureSoft(ctx, 3, 8.0, MathH.Lerp(-14.0, 18.0, drop) - 4.0, 0.22);
        Figure(ctx, 3, kx + jolt.X, MathH.Lerp(-14.0, 18.0, drop) + jolt.Y);

        if (s > mouseOff)
        {
            var u = Easings.EaseOutBack(MathH.Clamp01((s - mouseOff) / (mouseOn - mouseOff)));
            var x = MathH.Lerp(26.0, 15.0, u);
            if (s < mouseOn) DeliveryFigureSoft(ctx, 4, x + 4.0, 16.0, 0.22);
            Running(ctx, 4, MathH.Lerp(x, -12.0, grab) + jolt.X, 16.0 + jolt.Y);
        }

        if (s > catOff)
        {
            var u = Easings.EaseOutBounce(MathH.Clamp01((s - catOff) / (catOn - catOff)));
            var y = MathH.Lerp(-14.0, 11.0, u);
            if (s < catOn) DeliveryFigureSoft(ctx, 0, 6.0, y - 4.0, 0.22);
            Figure(ctx, 0, 6.0 + jolt.X, y + jolt.Y, flipX: true);
        }

        DeliveryDust(ctx, 11.5, 23.0, 9.0, s - cheeseOn, Colors.White);
        DeliveryDust(ctx, 12.5, 22.0, 12.0, s - catOn, Colors.White);

        if (s >= stampOff)
        {
            var f = Easings.EaseIn(MathH.Clamp01((s - stampOff) / (stampOn - stampOff)));
            var y = MathH.Lerp(-48.0, -22.4, f);
            var sheet = DeliveryStampSheet(ctx);
            ctx.DrawSurface(sheet, y: y + 3.0, alpha: 0.35);
            ctx.DrawSurface(sheet, y: y);
        }
        return;
    }

    if (s < holdOn)
    {
        var g = Easings.EaseOutBack(MathH.Clamp01((s - stampOn) / (settleOn - stampOn)));
        var jolt = Shake.At(s - stampOn, amplitude: 1.5, duration: 0.20, frequency: 44.0);
        ctx.DrawSurface(paperPlain);
        flyIn.Draw(ctx, s - stampOn);
        DeliveryDust(ctx, 12.0, 20.0, 20.0, s - stampOn, accentLight);
        DeliveryFlash(ctx, 1.0 - (s - stampOn) / 0.11);
        return;
    }

    if (s < dissolveOn)
    {
        ctx.DrawSurface(paperWithTime);
        return;
    }

    var e = Easings.EaseOutCubic(MathH.Clamp01((s - dissolveOn) / (deliveryTotal - dissolveOn)));
    Transitions.Iris(ctx, paperWithTime, fieldWithTime, e, softness: 0.5);
}

// ---------------------------------------------------------------- act: chase

// Mouse, dropped cheese, then the cat braking in the middle; the sheet sweeps it away.
void ActChase(RasterSurface ctx, double p)
{
    var s = p * 3.9;

    var tMouse = 0.18;
    var dMouse = 0.46;
    var tCheese = 0.44;
    var dCheese = 0.46;
    var tCat = 0.78;
    var dCat = 0.58;
    var tSheet = 1.50;
    var dSheet = 0.46;
    var tBig = 1.80;
    var dBig = 0.40;
    var stagger = 0.12;
    var tDissolve = 2.70;
    var tEnd = 3.48;
    var dEnd = 0.42;

    if (s < tSheet)
    {
        ctx.DrawSurface(fieldPlain);
        ChaseConvoy(ctx, s, tMouse, dMouse, tCheese, dCheese, tCat, dCat);
        return;
    }

    if (s < tDissolve)
    {
        var edge = MathH.Lerp(24.0, 0.0, Easings.EaseInOutCubic(MathH.Clamp01((s - tSheet) / dSheet)));
        if (edge > 0.5)
        {
            ctx.DrawSurface(fieldPlain);
            ChaseRunner(ctx, 0, Math.Min(5.0, 5.0 - (16.0 - edge) * 1.7), 11.0, MathH.Clamp01((16.0 - edge) / 9.0));
            ChaseSheetEdge(ctx, edge);
            ctx.DrawSurface(paperPlain, x: edge);
        }
        else
        {
            ctx.DrawSurface(paperPlain);
        }

        if (s < tBig) return;

        flyIn.Draw(ctx, s - tBig);
        return;
    }

    if (s < tEnd)
    {
        ctx.DrawSurface(paperWithTime);
        return;
    }

    var u = Easings.EaseInOutSine(MathH.Clamp01((s - tEnd) / dEnd));
    Transitions.Wipe(ctx, paperWithTime, fieldWithTime, u, angleDegrees: 180.0, softness: 1.4);
}

void ChaseConvoy(RasterSurface ctx, double s, double tMouse, double dMouse, double tCheese, double dCheese, double tCat, double dCat)
{
    if (s >= tMouse && s < tMouse + dMouse)
    {
        var u = (s - tMouse) / dMouse;
        ChaseRunner(ctx, 4, MathH.Lerp(26.0, -13.0, u), 2, 1.0);
    }

    if (s >= tCheese && s < tCheese + dCheese)
    {
        var u = (s - tCheese) / dCheese;
        ChaseRunner(ctx, 3, MathH.Lerp(26.0, -10.0, u), 18, 1.0);
    }

    if (s < tCat) return;

    var v = MathH.Clamp01((s - tCat) / dCat);
    var x = MathH.Lerp(26.0, 7.0, Easings.EaseOutBack(v));
    var after = s - tCat - dCat;
    var bounce = after > 0.0 ? Math.Sin(after * 24.0) * Math.Exp(-after * 8.0) * 1.4 : 0.0;
    ChaseRunner(ctx, 0, x, 11.0 + bounce, (1.0 - v) * (1.0 - v));
}

// Figure with speed streaks, ghost image and drop shadow, travelling left.
void ChaseRunner(RasterSurface ctx, int nr, double x, double y, double tempo)
{
    var right = x + RunWidth(nr);
    var height = RunHeight(nr);

    for (var r = -1; r <= height; r++)
    {
        if (Scatter.Value(nr * 13 + r, 5.0) < 0.42) continue;
        var length = 5.0 + Scatter.Value(r, nr * 3.0) * 14.0;
        ChaseStreak(ctx, right + 1.0, right + 1.0 + length * tempo, (int)Math.Round(y) + r, 0.95 * tempo);
    }

    ChaseGhost(ctx, nr, x + 3.0 * tempo, y, 0.30 * tempo);
    ChaseShadow(ctx, nr, x, y);
    Running(ctx, nr, x, y);
}

void ChaseShadow(RasterSurface ctx, int nr, double x, double y)
{
    var helper = ctx.CreateSurface(24, 24);
    RunningRaw(helper, nr, x + 1.0, y + 1.0);
    var dark = deepDark;
    for (var py = 0; py < 24; py++)
    for (var px = 0; px < 24; px++)
        if (helper[px, py].A > 0.0) ctx[px, py] = ColorOps.Lerp(ctx[px, py], dark, 0.55);
}

// Narrow shadow ahead of the incoming sheet.
void ChaseSheetEdge(RasterSurface ctx, double edge)
{
    for (var d = 1; d <= 2; d++)
    {
        var x = (int)Math.Round(edge) - d;
        if (x < 0 || x >= 24) continue;
        for (var y = 0; y < 24; y++)
            ctx[x, y] = ColorOps.Lerp(ctx[x, y], Tone(0.25, 0.85, -0.030), d == 1 ? 0.45 : 0.20);
    }
}

void ChaseGhost(RasterSurface ctx, int nr, double x, double y, double alpha)
{
    if (alpha <= 0.02) return;
    var helper = ctx.CreateSurface(24, 24);
    RunningRaw(helper, nr, x, y);
    ctx.DrawSurface(helper, alpha: alpha);
}

void ChaseStreak(RasterSurface ctx, double from, double to, int y, double strength)
{
    if (y < 0 || y >= 24 || strength <= 0.02) return;
    var a = (int)Math.Round(from);
    var b = (int)Math.Round(to);
    for (var x = a; x <= b; x++)
    {
        if (x < 0 || x >= 24) continue;
        var f = (x - a) / Math.Max(1.0, b - a);
        ctx[x, y] = ColorOps.Lerp(ctx[x, y], Tone(0.84, 1.00, 0.030), strength * (1.0 - f * f));
    }
}

// ---------------------------------------------------------------- act: trap

void ActTrap(RasterSurface ctx, double p)
{
    var s = p * 5.9;

    if (s < 3.30)
    {
        TrapScene(ctx, s);
        if (s > 2.90) TrapSweep(ctx, Easings.EaseInOutSine((s - 2.90) / 0.36), paperPlain);
        return;
    }

    if (s < 4.20)
    {
        TimeFlyIn(ctx, s - 3.30);
        return;
    }

    if (s < 5.20)
    {
        ctx.DrawSurface(paperWithTime);
        return;
    }

    ctx.DrawSurface(paperWithTime);
    TrapSweep(ctx, Easings.EaseInOutSine((s - 5.20) / 0.44), fieldWithTime);
}

// The cat sneaks under the hanging crate to get the bait, and the crate comes down on it.
// Die Falle haengt ueber dem Koeder. Die Maus schnappt ihn sich und ist schon wieder
// draussen, als die Falle zuschlaegt - auf nichts. Die Katze kommt zu spaet.
void TrapScene(RasterSurface ctx, double s)
{
    var cheese = TrapIn(s, 0.20, 0.55);
    var crate = TrapIn(s, 0.35, 0.75);
    var mouseIn = TrapIn(s, 0.85, 1.30);
    var grab = Easings.EaseInCubic(TrapIn(s, 1.50, 1.85));
    var drop = Easings.EaseInCubic(TrapIn(s, 1.86, 1.98));
    var catIn = TrapIn(s, 2.10, 2.55);

    ctx.DrawSurface(fieldPlain);
    TrapStage(ctx, TrapIn(s, 0.02, 0.30));
    TrapShadow(ctx, 1, 8, cheese * cheese);

    // Der Koeder und die Maus verlassen die Buehne gemeinsam nach links.
    if (cheese > 0.0)
        TrapPiece(ctx, 3, MathH.Lerp(MathH.Lerp(30.0, 2.0, Easings.EaseOutBack(cheese)), -14.0, grab), 12);

    if (mouseIn > 0.0)
    {
        var walk = MathH.Lerp(26.0, 1.0, Easings.EaseOutCubic(mouseIn));
        Running(ctx, 4, MathH.Lerp(walk, -13.0, grab), 10);
    }

    // Das Seil haelt die Falle, bis sie faellt.
    if (crate > 0.0 && drop < 1.0)
        for (var y = 0; y < 4; y++)
            ctx[5, y] = edgeTone;

    if (crate > 0.0)
    {
        var hang = MathH.Lerp(-11.0, -3.0, Easings.EaseOutBack(crate));
        var jolt = Shake.At(s - 1.98, amplitude: 1.6, duration: 0.22, frequency: 42.0);
        TrapPiece(ctx, 1, 1.0 + (drop >= 1.0 ? jolt.X : 0.0), MathH.Lerp(hang, 7.0, drop));
    }

    // Die Katze rennt an die leere Falle, setzt sich davor und aergert sich.
    if (catIn > 0.0 && catIn < 1.0)
        Running(ctx, 0, MathH.Lerp(28.0, 11.0, Easings.EaseOutCubic(catIn)), 8);
    else if (catIn >= 1.0)
    {
        var annoyed = Math.Sin((s - 2.55) * 24.0) * Math.Exp(-(s - 2.55) * 1.8) * 1.2;
        TrapPiece(ctx, 0, 11.0 + annoyed, 5);
    }

    TrapLight(ctx, s - 1.30);
}

double TrapIn(double s, double from, double to) => MathH.Clamp01((s - from) / (to - from));

// Fades out time and seconds arc and puts up the empty stage with its floor line.
void TrapStage(RasterSurface ctx, double a)
{
    if (a <= 0.0) return;
    var up = light;
    var down = deep;
    var line = edgeTone;
    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
    {
        var ground = ColorOps.Lerp(up, down, (x + y) / 46.0);
        if (y == 18) ground = ColorOps.Lerp(ground, line, 0.6);
        else if (y > 18) ground = ColorOps.Lerp(ground, line, 0.22);
        ctx[x, y] = ColorOps.Lerp(ctx[x, y], ground, a);
    }
}

// Figure with an offset drop shadow, so the pieces stand in front of each other.
void TrapPiece(RasterSurface ctx, int nr, double x, double y, bool flipX = false)
{
    var stamp = ctx.CreateSurface(24, 24);
    FigureRaw(stamp, nr, x + 1.0, y + 1.0, flipX: flipX);
    var shadowColor = deepDark;
    for (var yy = 0; yy < 24; yy++)
    for (var xx = 0; xx < 24; xx++)
        if (stamp[xx, yy].A > 0.0) ctx[xx, yy] = ColorOps.Lerp(ctx[xx, yy], shadowColor, 0.55);
    Figure(ctx, nr, x, y, flipX: flipX);
}

void TrapShadow(RasterSurface ctx, int x0, int x1, double a)
{
    if (a <= 0.0) return;
    var tone = deepDark;
    for (var x = Math.Max(0, x0); x <= Math.Min(23, x1); x++)
        ctx[x, 18] = ColorOps.Lerp(ctx[x, 18], tone, a * 0.6);
}

void TrapLight(RasterSurface ctx, double k)
{
    if (k <= 0.0) return;
    var strength = TrapFlicker(k);
    if (strength <= 0.01) return;
    var lightColor = Color.FromRgb(1.00, 0.97, 0.88);
    var core = Color.FromRgbByte(255, 246, 206);
    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
    {
        var dx = x + 0.5 - 2.0;
        var dy = y + 0.5 - 10.0;
        var d = Math.Sqrt(dx * dx + dy * dy);
        if (d < 2.6)
            ctx[x, y] = ColorOps.Lerp(ctx[x, y], core, (1.0 - d / 2.6) * 0.7 * MathH.Clamp01(strength));
        if (dy <= 0.0) continue;
        var cone = MathH.Clamp01((dy / Math.Max(d, 0.001) - 0.40) / 0.5);
        var g = MathH.Clamp01(1.0 - d / 13.0) * cone;
        if (g > 0.01) ctx[x, y] = ctx[x, y].LitBy(lightColor, 1.0 + g * g * 0.55 * strength);
    }
}

double TrapFlicker(double k)
{
    if (k < 0.08) return 1.4;
    if (k < 0.14) return 0.0;
    if (k < 0.22) return 1.6;
    return 1.0 + 0.6 * Math.Exp(-(k - 0.22) * 12.0);
}

// Slanted sweep with a dark leading edge that swaps the picture for target.
void TrapSweep(RasterSurface ctx, double u, RasterSurface target)
{
    var band = edgeTone;
    var edge = MathH.Lerp(-3.5, 34.0, MathH.Clamp01(u));
    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
    {
        var d = x + 0.5 + (y + 0.5) * 0.30;
        if (d < edge) ctx[x, y] = target[x, y];
        else if (d < edge + 1.7) ctx[x, y] = band;
    }
}

// ---------------------------------------------------------------- act: window

void ActWindow(RasterSurface ctx, double p)
{
    const double raceEnd = 4.30;
    const double sweepEnd = 4.66;
    const double flyEnd = 5.50;
    const double holdEnd = 6.30;
    var s = p * 6.80;

    if (s < raceEnd)
    {
        WindowRace(ctx, s);
        return;
    }

    if (s < sweepEnd)
    {
        WindowRace(ctx, raceEnd);
        WindowShut(ctx, (s - raceEnd) / (sweepEnd - raceEnd));
        return;
    }

    if (s < flyEnd)
    {
        TimeFlyIn(ctx, s - sweepEnd);
        return;
    }

    if (s < holdEnd)
    {
        ctx.DrawSurface(paperWithTime);
        return;
    }

    WindowBack(ctx, (s - holdEnd) / (6.80 - holdEnd));
}

// Ein Rennen ueber drei Etagen: die Maus im Zickzack nach unten, die Katze hinterher.
// Auf der letzten Etage bleibt die Maus stehen und lugt - da stehen auch ihre Beine
// still - und ist wieder weg, sobald die Katze bei ihr ankommt.
void WindowRace(RasterSurface ctx, double s)
{
    ctx.DrawSurface(paperPlain);
    for (var i = 0; i < 3; i++)
        WindowShelf(ctx, i, Easings.EaseOutCubic(MathH.Clamp01((s - i * 0.10) / 0.28)));

    WindowRunner(ctx, 4, 0, s, 0.10, 1.15, false);
    WindowRunner(ctx, 4, 1, s, 1.15, 2.20, true);
    WindowRunner(ctx, 0, 1, s, 1.48, 2.53, true);

    // Untere Etage: die Maus haelt in der Mitte an, bis die Katze fast da ist.
    if (s > 2.20)
    {
        var arrive = MathH.Clamp01((s - 2.20) / 0.65);
        var flee = MathH.Clamp01((s - 3.35) / 0.55);
        var x = MathH.Lerp(MathH.Lerp(25.0, 11.0, Easings.EaseOutCubic(arrive)), -13.0, Easings.EaseInCubic(flee));
        Running(ctx, 4, x, 16);
    }

    if (s > 2.62)
    {
        var chase = MathH.Clamp01((s - 2.62) / 1.05);
        var x = MathH.Lerp(27.0, 13.0, Easings.EaseOutCubic(chase));
        Running(ctx, 0, x, 14);
    }
}

// Eine Figur laeuft einmal quer durch ihre Etage.
void WindowRunner(RasterSurface ctx, int nr, int level, double s, double from, double to, bool toRight)
{
    if (s < from || s > to) return;
    var u = Easings.EaseInOutSine(MathH.Clamp01((s - from) / (to - from)));
    var width = RunWidth(nr);
    var x = toRight ? MathH.Lerp(-width - 2.0, 26.0, u) : MathH.Lerp(26.0, -width - 2.0, u);
    Running(ctx, nr, x, level * 8 + 8 - RunHeight(nr), flipX: toRight);
}

// Die Etagen klappen von aussen zu.
void WindowShut(RasterSurface ctx, double u)
{
    for (var i = 0; i < 3; i++)
    {
        var v = MathH.Clamp01((u - i * 0.08) / 0.5);
        var covered = (int)Math.Ceiling(v * 4.0);
        for (var k = 0; k < covered; k++)
        for (var x = 0; x < 24; x++)
        {
            ctx[x, i * 8 + k] = paper;
            ctx[x, i * 8 + 7 - k] = paper;
        }
    }
}

void WindowBack(RasterSurface ctx, double s)
{
    for (var i = 0; i < 3; i++)
    {
        var u = Easings.EaseOutCubic(MathH.Clamp01((s - (2 - i) * 0.12) / 0.5));
        var edge = u * 25.0;
        for (var r = 0; r < 8; r++)
        {
            var y = i * 8 + r;
            for (var x = 0; x < 24; x++)
            {
                var free = i % 2 == 0 ? x >= 24 - edge : x < edge;
                ctx[x, y] = free ? fieldWithTime[x, y] : paperWithTime[x, y];
            }
        }
    }
}

// Regalbrett der Etage i.
void WindowShelf(RasterSurface ctx, int i, double u)
{
    if (u <= 0.0) return;
    var span = Math.Min(24, (int)Math.Round(u * 24.0));
    for (var k = 0; k < span; k++)
        ctx[i % 2 == 0 ? 23 - k : k, i * 8 + 7] = accent;
}

// ---------------------------------------------------------------- act: tumble

void ActTumble(RasterSurface ctx, double p)
{
    const double tumbleTotal = 3.85;
    const double catStart = 0.00;
    const double catSpan = 0.85;
    const double cheeseStart = 0.50;
    const double cheeseSpan = 0.70;
    const double bottomStart = 0.88;
    const double bottomSpan = 0.52;
    const double topStart = 1.06;
    const double topSpan = 0.52;
    const double dissolveStart = 2.25;
    const double dissolveSpeed = 1.50;
    const double backStart = 3.02;
    const double backSpan = 0.68;

    var s = p * tumbleTotal;

    if (s < dissolveStart)
    {
        var u2 = MathH.Clamp01((s - cheeseStart) / cheeseSpan);
        var cx2 = MathH.Lerp(31.0, -9.0, u2);
        TumbleEdge(ctx, fieldPlain, paperPlain, cx2 - 5.0);

        var u1 = MathH.Clamp01((s - catStart) / catSpan);
        if (u1 < 1.0)
            TumbleSpin(ctx, 0, MathH.Lerp(-5.5, 21.0, u1), TumbleArc(u1, 20.0, 6.0, 0.45), TumbleQuarter(u1, 0.38, 0.50, 0.88));

        if (s >= cheeseStart && u2 < 1.0)
            TumbleSpin(ctx, 3, cx2, TumbleArc(u2, 15.0, 8.0, 0.5), TumbleQuarter(u2, 0.42, 0.55));

        if (s >= bottomStart)
        {
            var uU = MathH.Clamp01((s - bottomStart) / bottomSpan);
            var jolt = TumbleJolt(s - (topStart + topSpan * 0.6));
            flyIn.Draw(ctx, s - bottomStart);
        }

        if (s >= topStart)
        {
            var uO = MathH.Clamp01((s - topStart) / topSpan);

        }
    }
    else if (s < backStart)
    {
        ctx.DrawSurface(paperWithTime);
    }
    else if (s < backStart + backSpan)
    {
        var u4 = MathH.Clamp01((s - backStart) / backSpan);
        var cx4 = MathH.Lerp(-8.0, 31.0, u4);
        TumbleEdge(ctx, fieldWithTime, paperWithTime, cx4 + 5.5);
        if (u4 < 1.0)
            TumbleSpin(ctx, 0, cx4, TumbleArc(u4, 17.0, 8.0, 0.5), TumbleQuarter(u4, 0.55, 0.75));
    }
    else
    {
        ctx.DrawSurface(fieldWithTime);
    }
}

// Vertical wipe: one surface left of the edge, the other right of it.
void TumbleEdge(RasterSurface ctx, RasterSurface left, RasterSurface right, double edge)
{
    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
        ctx[x, y] = ColorOps.Lerp(right[x, y], left[x, y], MathH.Clamp01(edge - x));
}

// Figure rotated about its centre by quarter turn q.
void TumbleSpin(RasterSurface ctx, int nr, double cx, double cy, int q)
{
    var w = FigureWidth(nr);
    var h = FigureHeight(nr);
    var source = ctx.CreateSurface(24, 24);
    FigureRaw(source, nr, 0, 0);

    var x0 = (int)Math.Round(cx - (q % 2 == 0 ? w : h) / 2.0);
    var y0 = (int)Math.Round(cy - (q % 2 == 0 ? h : w) / 2.0);

    for (var r = 0; r < h; r++)
    for (var c = 0; c < w; c++)
    {
        var color = source[c, r];
        if (color.A <= 0.0) continue;
        var dx = q switch { 0 => c, 1 => h - 1 - r, 2 => w - 1 - c, _ => r };
        var dy = q switch { 0 => r, 1 => c, 2 => h - 1 - r, _ => w - 1 - c };
        var x = x0 + dx;
        var y = y0 + dy;
        if (x >= 0 && x < 24 && y >= 0 && y < 24) ctx[x, y] = color;
    }
}

// Lazy tumbling: at each mark the piece turns another quarter.
int TumbleQuarter(double u, params double[] marks)
{
    var k = 0;
    foreach (var m in marks) if (u >= m) k++;
    return k % 4;
}

// Throwing arc: starts at y0, apex of height peak at time uPeak.
double TumbleArc(double u, double y0, double peak, double uPeak)
{
    var a = (y0 - peak) / (uPeak * uPeak);
    var d = u - uPeak;
    return peak + a * d * d;
}

// Free fall until impact, then two small bounces. 0 = up, 1 = in place.
double TumbleDrop(double u)
{
    const double impact = 0.6;
    if (u < impact)
    {
        var k = u / impact;
        return k * k;
    }
    var r = (u - impact) / (1.0 - impact);
    return 1.0 - Math.Abs(Math.Sin(r * Math.PI * 2.0)) * 0.22 * (1.0 - r) * (1.0 - r);
}

double TumbleJolt(double dt) => dt < 0.0 || dt > 0.16 ? 0.0 : Math.Sin(dt / 0.16 * Math.PI) * 1.4;

// ---------------------------------------------------------------- act: flip board

const double flipTotal = 4.45;
const int flipCount = 6;
const int flipHeight = 4;
const double flipStep = 0.17;
const double flipStagger = 0.075;
const double flipHoldBig = 0.70;
const double flipHoldSmall = 0.50;
const double flipBounce = 0.12;
const double flipFinal = 0.20;
const double flipFinalStagger = 0.06;
const int flipStepsOne = 3;
const int flipStepsTwo = 4;

// Split-flap display: rows step through staggered, first to the big time, then to the small one.
void ActFlipBoard(RasterSurface ctx, double p)
{
    var s = p * flipTotal;
    var bigSheet = ctx.CreateSurface(24, 24);
    bigSheet.DrawSurface(paperPlain);
    bigSheet.DrawSurface(bigTime.Surface);

    var tFirst = FlipDuration(flipStepsOne, flipStagger);
    var tBig = tFirst + flipHoldBig;
    var tSecond = tBig + FlipDuration(flipStepsTwo, flipStagger);
    var tSmall = tSecond + flipHoldSmall;

    if (s < tFirst) FlipRound(ctx, s, fieldPlain, bigSheet, flipStepsOne, flipStagger, 1);
    else if (s < tBig) ctx.DrawSurface(bigSheet);
    else if (s < tSecond) FlipRound(ctx, s - tBig, bigSheet, paperWithTime, flipStepsTwo, flipStagger, 2);
    else if (s < tSmall) ctx.DrawSurface(paperWithTime);
    else FlipRound(ctx, s - tSmall, paperWithTime, fieldWithTime, 1, flipFinalStagger, 3);
}

double FlipDuration(int steps, double stagger) =>
    (flipCount - 1) * stagger + (steps + FlipSpread(steps)) * (steps > 1 ? flipStep : flipFinal) + flipBounce;

int FlipSpread(int steps) => steps > 1 ? 1 : 0;

void FlipRound(RasterSurface ctx, double u, RasterSurface older, RasterSurface target, int steps, double stagger, int lane)
{
    var step = steps > 1 ? flipStep : flipFinal;
    for (var i = 0; i < flipCount; i++)
    {
        var mine = steps + (FlipRandom(lane * 101 + i * 17) < 0.45 ? FlipSpread(steps) : 0);
        var local = u - i * stagger;
        if (local <= 0.0)
        {
            FlipStrip(ctx, i, older, 1.0);
            continue;
        }
        if (local >= mine * step)
        {
            var since = local - mine * step;
            var bounce = since < flipBounce ? 1.0 - 0.28 * Math.Sin(since / flipBounce * Math.PI) : 1.0;
            FlipStrip(ctx, i, target, bounce);
            continue;
        }
        var j = (int)(local / step);
        var f = local / step - j;
        var source = f < 0.5
            ? (j == 0 ? older : FlipBetween(ctx, i, lane * 11 + j))
            : (j == mine - 1 ? target : FlipBetween(ctx, i, lane * 11 + j + 1));
        FlipStrip(ctx, i, source, Math.Pow(Math.Abs(Math.Cos(f * Math.PI)), 0.55));
    }
}

// One row: both halves squash towards the edges, the dark gap opening between them.
void FlipStrip(RasterSurface ctx, int i, RasterSurface source, double k)
{
    var y0 = i * flipHeight;
    var half = flipHeight / 2.0;
    var cover = (int)Math.Round(flipHeight * MathH.Clamp01(k));
    var top = (cover + 1) / 2;
    var bottom = cover / 2;
    var housing = Tone(0.14, 0.65, -0.020);

    for (var r = 0; r < flipHeight; r++)
    {
        var row = -1;
        var edge = false;
        if (r < top)
        {
            row = (int)((r + 0.5) / top * half);
            edge = r == top - 1;
        }
        else if (r >= flipHeight - bottom)
        {
            row = (int)(half + (r - (flipHeight - bottom) + 0.5) / bottom * half);
            edge = r == flipHeight - bottom;
        }
        var sourceRow = y0 + Math.Min(flipHeight - 1, Math.Max(0, row));
        var sheen = edge && cover < flipHeight ? 0.35 : 0.0;
        for (var x = 0; x < 24; x++)
            ctx[x, y0 + r] = row < 0 ? housing : ColorOps.Lerp(source[x, sourceRow], Colors.White, sheen);
    }
}

// In-between flap: the roll runs through big digits first, then through clock readings.
RasterSurface FlipBetween(RasterSurface ctx, int i, int j)
{
    var sheet = ctx.CreateSurface(24, 24);
    sheet.DrawSurface(paperPlain);
    var stamp = ctx.CreateSurface(24, 24);
    var hour = (int)(FlipRandom(i * 41 + j * 17) * 24.0);
    var min = (int)(FlipRandom(i * 29 + j * 23) * 60.0);
    SmallTime(stamp, new DateTime(2000, 1, 1, hour, min, 0), accent, timeShadow);
    sheet.DrawSurface(stamp);
    return sheet;
}

double FlipRandom(int n)
{
    var h = (uint)n * 2654435761u;
    h ^= h >> 15;
    h *= 2246822519u;
    h ^= h >> 13;
    return h % 10007u / 10007.0;
}

// ---------------------------------------------------------------- act: fold

const double foldScoreTime = 0.32;
const double foldBendTime = 0.26;
const double foldCloseTime = 0.44;
const double foldHoldTime = 0.10;
const double foldOpenTime = 0.56;
const double foldBigTime = 1.06;
const double foldBackTime = 0.62;
const double foldTotal = 3.62;

void ActFold(RasterSurface ctx, double p)
{
    var tScore = foldScoreTime;
    var tBend = tScore + foldBendTime;
    var tShut = tBend + foldCloseTime;
    var tHold = tShut + foldHoldTime;
    var tOpen2 = tHold + foldOpenTime;
    var tBig = tOpen2 + foldBigTime;
    var tBack = tBig + foldBackTime;
    var s = p * foldTotal;

    if (s < tScore)
    {
        CopyInto(ctx, fieldWithTime);
        FoldScore(ctx, Easings.EaseOutCubic(s / foldScoreTime), true, 1.0);
        return;
    }
    if (s < tBend)
    {
        var u = (s - tScore) / foldBendTime;
        FoldFlap(ctx, FoldScored(), paperPlain, 0.62 * Easings.EaseOutBack(u), true);
        return;
    }
    if (s < tShut)
    {
        var u = (s - tBend) / foldCloseTime;
        FoldFlap(ctx, FoldScored(), paperPlain, MathH.Lerp(0.62, Math.PI, Math.Pow(u, 1.55)), true);
        return;
    }
    if (s < tHold)
    {
        var u = (s - tShut) / foldHoldTime;
        CopyInto(ctx, paperPlain);
        FoldImpact(ctx, 1.0 - u);
        FoldMark(ctx, u);
        return;
    }
    if (s < tOpen2)
    {
        var u = (s - tHold) / foldOpenTime;
        FoldFlap(ctx, FoldSheet(true), FoldSheet(false), Math.PI * (1.0 - Easings.EaseOut(u)), true);
        return;
    }
    if (s < tBig)
    {
        var u = (s - tOpen2) / foldBigTime;
        CopyInto(ctx, FoldSheet(true));
        FoldSheen(ctx, u);
        FoldScore(ctx, Easings.EaseOutCubic(MathH.Clamp01((u - 0.62) / 0.30)), false, 1.0);
        return;
    }
    if (s < tBack)
    {
        var u = (s - tBig) / foldBackTime;
        var sheet = FoldSheet(true);
        FoldScore(sheet, 1.0, false, 1.0);
        FoldFlap(ctx, sheet, fieldWithTime, Math.PI * Math.Pow(u, 1.5), false);
        return;
    }

    CopyInto(ctx, fieldWithTime);
}

// Folds the half beyond the crease: angle 0 shows the source, PI the target.
void FoldFlap(RasterSurface ctx, RasterSurface source, RasterSurface target, double angle, bool horizontal)
{
    var co = Math.Cos(angle);
    var si = Math.Abs(Math.Sin(angle));
    var tone = FoldTone();
    var limit = 12.0 * co;
    var length = 2.5 + 5.5 * si;

    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
    {
        var d = (horizontal ? y : x) + 0.5 - 12.0;
        var u = Math.Abs(co) < 0.001 ? 99.0 : d / co;
        if (u > 0.0 && u < 12.0)
        {
            var front = co > 0.0;
            var q = front ? (int)(12.0 + u) : (int)(12.0 - u);
            var side = front ? source : target;
            var raw = horizontal ? side[x, q] : side[q, y];
            ctx[x, y] = ColorOps.Lerp(raw, tone, (front ? 0.50 : 0.32) * si);
        }
        else
        {
            var raw = d > 0.0 ? target[x, y] : source[x, y];
            var cast = MathH.Clamp01(1.0 - Math.Abs(d - limit) / length);
            ctx[x, y] = ColorOps.Lerp(raw, tone, (d > 0.0 ? 0.62 : 0.34) * si * cast);
        }
    }

    FoldCrease(ctx, 12.0 + limit, co > 0.0, si, horizontal);
}

// The travelling crease: bright paper edge with the contact shadow behind it.
void FoldCrease(RasterSurface ctx, double creasePos, bool front, double si, bool horizontal)
{
    if (si <= 0.01) return;
    var bright = Tone(0.93, 1.00, 0.030);
    var tone = FoldTone();
    var edgeRow = front ? (int)Math.Ceiling(creasePos - 0.5) - 1 : (int)Math.Floor(creasePos - 0.5) + 1;
    var shadowRow = front ? edgeRow + 1 : edgeRow - 1;

    for (var i = 0; i < 24; i++)
    {
        if (edgeRow >= 0 && edgeRow < 24)
        {
            if (horizontal) ctx[i, edgeRow] = ColorOps.Lerp(ctx[i, edgeRow], bright, 0.70 * si);
            else ctx[edgeRow, i] = ColorOps.Lerp(ctx[edgeRow, i], bright, 0.70 * si);
        }
        if (shadowRow >= 0 && shadowRow < 24)
        {
            if (horizontal) ctx[i, shadowRow] = ColorOps.Lerp(ctx[i, shadowRow], tone, 0.55 * si);
            else ctx[shadowRow, i] = ColorOps.Lerp(ctx[shadowRow, i], tone, 0.55 * si);
        }
    }
}

void FoldScore(RasterSurface ctx, double run, bool horizontal, double strength)
{
    if (run <= 0.0) return;
    var tone = FoldTone();
    var far = run * 26.0;

    for (var i = 0; i < 24; i++)
    {
        var covered = MathH.Clamp01(far - i) * strength;
        if (covered <= 0.0) continue;
        var head = MathH.Clamp01(1.0 - (far - i) / 3.0);
        var top = (0.62 + 0.38 * head) * covered;
        var middle = (0.52 + 0.30 * head) * covered;
        var trail = 0.22 * covered;
        if (horizontal)
        {
            ctx[i, 11] = ColorOps.Lerp(ctx[i, 11], Colors.White, top);
            ctx[i, 12] = ColorOps.Lerp(ctx[i, 12], tone, middle);
            ctx[i, 13] = ColorOps.Lerp(ctx[i, 13], tone, trail);
        }
        else
        {
            ctx[11, i] = ColorOps.Lerp(ctx[11, i], Colors.White, top);
            ctx[12, i] = ColorOps.Lerp(ctx[12, i], tone, middle);
            ctx[13, i] = ColorOps.Lerp(ctx[13, i], tone, trail);
        }
    }
}

RasterSurface FoldScored()
{
    var sheet = new RasterSurface(24, 24);
    CopyInto(sheet, fieldWithTime);
    FoldScore(sheet, 1.0, true, 1.0);
    return sheet;
}

RasterSurface FoldSheet(bool withBig)
{
    var sheet = new RasterSurface(24, 24);
    sheet.DrawBackground(paper);
    if (withBig) sheet.DrawSurface(bigTime.Surface);
    FoldMark(sheet, 1.0);
    return sheet;
}

void FoldMark(RasterSurface ctx, double strength)
{
    var tone = FoldTone();
    for (var i = 0; i < 24; i++)
    {
        ctx[i, 11] = ColorOps.Lerp(ctx[i, 11], Colors.White, 0.30 * strength);
        ctx[i, 12] = ColorOps.Lerp(ctx[i, 12], tone, 0.18 * strength);
    }
}

void FoldImpact(RasterSurface ctx, double strength)
{
    var tone = FoldTone();
    for (var x = 0; x < 24; x++)
    for (var y = 0; y < 4; y++)
        ctx[x, y] = ColorOps.Lerp(ctx[x, y], tone, strength * 0.42 * (1.0 - y / 4.0));
}

void FoldSheen(RasterSurface ctx, double u)
{
    var tone = FoldTone();
    var middle = MathH.Lerp(-9.0, 36.0, Easings.EaseInOutSine(MathH.Clamp01(u / 0.5)));
    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
    {
        var dd = Math.Abs(x + y * 0.35 - middle);
        if (dd > 5.0) continue;
        var k = 1.0 - dd / 5.0;
        ctx[x, y] = ColorOps.Lerp(ctx[x, y], tone, 0.18 * k * k);
    }
}

Color FoldTone() => Tone(0.23, 0.42, -0.010);

// ---------------------------------------------------------------- act: spotlight

const double spotNarrow = 0.20;
const double spotPivotX = 12.0;
const double spotPivotY = -4.0;

void ActSpotlight(RasterSurface ctx, double p)
{
    const double dSpotIgnite = 0.40;
    const double dSpotSearch = 1.02;
    const double dSpotFind = 0.68;
    const double dSpotFlood = 0.62;
    const double dSpotDissolve = 0.80;
    const double dSpotClose = 0.58;

    var tIgnite = dSpotIgnite;
    var tSearch = tIgnite + dSpotSearch;
    var tFound = tSearch + dSpotFind;
    var tFlood = tFound + dSpotFlood;
    var tDissolve = tFlood + dSpotDissolve;
    var s = p * (tDissolve + dSpotClose);

    if (s < tIgnite) SpotIgnite(ctx, s);
    else if (s < tSearch) SpotSearch(ctx, s - tIgnite);
    else if (s < tFound) SpotFind(ctx, s - tSearch, dSpotFind);
    else if (s < tFlood) SpotFlood(ctx, s - tFound);
    else if (s < tDissolve) SpotDissolve(ctx, s - tFlood, dSpotDissolve);
    else SpotClose(ctx, s - tDissolve, dSpotClose);
}

void SpotIgnite(RasterSurface ctx, double s)
{
    SpotStage(ctx, 0.0);
    SpotPaint(ctx, Easings.EaseOutCubic(MathH.Clamp01(s / 0.14)), SpotSweepStart(), spotNarrow, 1.1, 34.0, SpotSpark(s - 0.17), 0.0, 0.0);
}

void SpotSearch(RasterSurface ctx, double s)
{
    SpotStage(ctx, 0.0);
    SpotPaint(ctx, 1.0, SpotSearchAngle(s), spotNarrow, 1.1, 34.0, 1.0, 0.0, 0.0);
}

void SpotFind(RasterSurface ctx, double s, double duration)
{
    var u = MathH.Clamp01(s / duration);
    var opening = Easings.EaseInCubic(MathH.Clamp01((u - 0.55) / 0.45));

    // Nummer 0 verliert die Maus und sucht ihr hinterher, die anderen bleiben auf dem Fund.
    var swing = spotPick == 0
        ? MathH.Lerp(SpotTargetAngle(), 22.0, Easings.EaseInOutSine(MathH.Clamp01((u - 0.45) / 0.55))) * (1.0 - Easings.EaseInCubic(MathH.Clamp01((u - 0.7) / 0.3)))
        : SpotTargetAngle() * (1.0 - Easings.EaseInCubic(MathH.Clamp01((u - 0.6) / 0.4)));

    SpotStage(ctx, u);
    SpotPaint(ctx, 1.0, swing, MathH.Lerp(spotNarrow, 0.36, opening), MathH.Lerp(1.1, 1.8, opening), 34.0, 1.0, 0.0, 0.0);
}

void SpotFlood(RasterSurface ctx, double s)
{
    SpotStage(ctx, 1.0);
    SpotPaint(ctx, 1.0, 0.0, 0.36, 1.8, 34.0, 1.0, MathH.Clamp01(s / 0.06), Envelope.Bell(MathH.Clamp01(s / 0.18)) * 0.40);
}

void SpotDissolve(RasterSurface ctx, double s, double duration) => TimeFlyIn(ctx, s);

void SpotClose(RasterSurface ctx, double s, double duration)
{
    var u = MathH.Clamp01(s / duration);
    var tight = Easings.EaseInOutSine(MathH.Clamp01(u / 0.62));
    ctx.DrawSurface(paperWithTime);
    SpotPaint(ctx, 0.0, MathH.Lerp(0.0, -30.0, Easings.EaseInCubic(u)), MathH.Lerp(0.80, 0.05, tight), MathH.Lerp(5.5, 0.5, tight), 42.0, MathH.Clamp01(1.0 - (u - 0.70) / 0.30), MathH.Clamp01(1.0 - u / 0.22), 0.0);
}

// Whatever is in ctx is the lit stage; outside the cone the darkened ground remains.
void SpotPaint(RasterSurface ctx, double dimm, double angleDegrees, double halfSlope, double baseHalf, double reach, double strength, double flood, double over)
{
    var w = angleDegrees * Math.PI / 180.0;
    var sw = Math.Sin(w);
    var cw = Math.Cos(w);
    var night = Tone(0.04, 0.70, -0.020);

    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
    {
        var dx = x + 0.5 - spotPivotX;
        var dy = y + 0.5 - spotPivotY;
        var depth = dx * sw + dy * cw;
        var across = dx * cw - dy * sw;
        var half = baseHalf + Math.Max(0.0, depth) * halfSlope;
        var rim = MathH.Clamp01((half - Math.Abs(across)) / 1.4);
        var core = MathH.Clamp01(1.0 - Math.Abs(across) / Math.Max(0.8, half));
        var tail = MathH.Clamp01((reach - depth) / 4.0);
        var cone = depth <= 0.0 ? 0.0 : Math.Min(rim, 0.72 + 0.28 * core) * tail * strength;
        var lit = MathH.Clamp01(Math.Max(cone, flood));
        var color = ColorOps.Lerp(ColorOps.Lerp(fieldWithTime[x, y], night, dimm), ctx[x, y], lit);
        ctx[x, y] = over > 0.0 ? ColorOps.Lerp(color, Colors.White, over * lit) : color;
    }
}

// Drei Nummern, die der Kegel auffuehren kann - welche, entscheidet die Minute.
// 0: die Maus erstarrt im Licht und ist weg, sobald der Kegel zuckt.
// 1: die Katze sitzt im Licht und laesst sich nicht stoeren, bis sie gemuetlich abgeht.
// 2: der Kegel findet den Kaese, und die Maus klaut ihn ihm vor der Nase weg.
void SpotStage(RasterSurface ctx, double u)
{
    ctx.DrawSurface(paperPlain);

    if (spotPick == 0)
    {
        var flee = Easings.EaseInCubic(MathH.Clamp01((u - 0.42) / 0.38));
        if (flee < 1.0) Running(ctx, 4, MathH.Lerp(1.0, -13.0, flee), 15);
        return;
    }

    if (spotPick == 1)
    {
        var leave = Easings.EaseInCubic(MathH.Clamp01((u - 0.62) / 0.38));
        if (leave <= 0.0) Figure(ctx, 0, 6, 10);
        else if (leave < 1.0) Running(ctx, 0, MathH.Lerp(6.0, -17.0, leave), 13);
        return;
    }

    var steal = Easings.EaseInCubic(MathH.Clamp01((u - 0.62) / 0.38));
    if (steal < 1.0) Figure(ctx, 3, MathH.Lerp(13.0, -11.0, steal), 17);
    var comes = MathH.Clamp01((u - 0.2) / 0.42);
    if (comes > 0.0) Running(ctx, 4, MathH.Lerp(26.0, 2.0, Easings.EaseOutCubic(comes)) - steal * 15.0, 16);
}

// Wohin der Kegel am Ende der Suche zeigt, und aus welcher Ecke er losfaehrt.
double SpotTargetAngle() => spotPick switch { 0 => -27.0, 1 => -9.0, _ => 2.0 };

double SpotSweepStart() => spotPick == 2 ? 34.0 : -34.0;

// Jede Nummer sucht anders: eine fahrig hin und her, eine in ruhigen Zuegen, eine
// startet auf der falschen Seite und muss ganz herum.
double SpotSearchAngle(double s)
{
    var target = SpotTargetAngle();
    if (spotPick == 1)
    {
        if (s < 0.46) return MathH.Lerp(-34.0, 24.0, Easings.EaseInOutSine(s / 0.46));
        if (s < 0.58) return 24.0;
        return MathH.Lerp(24.0, target, Easings.EaseInOutCubic(MathH.Clamp01((s - 0.58) / 0.44)));
    }

    if (spotPick == 2)
    {
        if (s < 0.30) return MathH.Lerp(34.0, 12.0, Easings.EaseInOutCubic(s / 0.30));
        if (s < 0.40) return 12.0;
        if (s < 0.62) return MathH.Lerp(12.0, -30.0, Easings.EaseInOutCubic((s - 0.40) / 0.22));
        if (s < 0.70) return -30.0;
        return MathH.Lerp(-30.0, target, Easings.EaseInOutCubic(MathH.Clamp01((s - 0.70) / 0.32)));
    }

    if (s < 0.34) return MathH.Lerp(-34.0, 27.0, Easings.EaseInOutCubic(s / 0.34));
    if (s < 0.42) return 27.0;
    if (s < 0.70) return MathH.Lerp(27.0, -9.0, Easings.EaseInOutCubic((s - 0.42) / 0.28));
    if (s < 0.76) return -9.0;
    return MathH.Lerp(-9.0, target, Easings.EaseInOutCubic(MathH.Clamp01((s - 0.76) / 0.26)));
}

double SpotSpark(double s)
{
    if (s < 0.0) return 0.0;
    if (s < 0.07) return 0.9;
    if (s < 0.13) return 0.0;
    return MathH.Clamp01(0.4 + (s - 0.13) / 0.09);
}
