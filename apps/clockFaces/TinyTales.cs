// ---
// app: TinyTales
// displayName: Tiny Tales
// author: Claude
// description: Colour field with a running wave and a seconds arc; every full minute one of eleven short stories plays and ends with the time
// appType: ClockFace
// ---

// Ten short stories, one per minute, never the same one twice in a row. Each ends with
// the time filling the screen and then collapsing into the small readout.
//
// With the cast - cat, crate, bird, cheese, mouse:
//   1 Delivery   the cheese drops in, the mouse gets there first, the cat lands too late
//   2 Chase      mouse and cheese race past, the cat brakes in the middle and finds nothing
//   3 Trap       a trap is set, the light comes on, the crate catches the wrong animal
//   4 Window     three shelves run as conveyor belts, each its own chase
//   5 Tumble     cat and cheese tumble through in throwing arcs
//   6 Catalogue  the cast as catalogue pages, cheese and mouse on the last two
//
// Without the cast, other mechanics:
//   7 FlipBoard  a split-flap display steps through, staggered per row
//   8 Fold       the field creases like paper and opens up white
//   9 Spotlight  a cone searches the field, finds the mouse first, then the time
//  10 Measure    dimension lines drive in and snap onto the outline of the digits

#:package Pxl@*

using Pxl.Ui.CSharp;

var baseTone = Param.Color(Color.FromRgbByte(255, 121, 26), label: "Base colour");

var shadowStrength = Param.Float(0.40, min: 0.0, max: 1.0, label: "Drop shadow");
var secondsRadius = Param.Float(12.0, min: 5.0, max: 16.0, label: "Seconds radius");
var secondsOpacity = Param.Float(0.95, min: 0.0, max: 1.0, label: "Seconds ring");

// All times in seconds; each act runs its own schedule, stretched to fit the duration.
var actDuration = Param.Float(4.0, min: 3.0, max: 15.0, label: "Act duration");
var calmDuration = Param.Float(2.0, min: 1.5, max: 20.0, label: "Calm");
var holdBigTime = Param.Float(0.8, min: 0.0, max: 6.0, label: "Hold time");
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
var actCatalogue = Param.Bool(true, label: "6 Catalogue");
var actFlipBoard = Param.Bool(true, label: "7 Flip board");
var actFold = Param.Bool(true, label: "8 Fold");
var actSpotlight = Param.Bool(true, label: "9 Spotlight");
var actMeasure = Param.Bool(true, label: "10 Measure");

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
    ['k'] = Color.FromRgbByte(38, 42, 54),
    ['g'] = Color.FromRgbByte(104, 112, 130),
    ['w'] = Color.FromRgbByte(238, 242, 248),
    ['a'] = Color.FromRgbByte(255, 214, 86),
    ['s'] = Color.FromRgbByte(255, 176, 48),
    ['c'] = Color.FromRgbByte(255, 216, 96),
    ['d'] = Color.FromRgbByte(196, 134, 28),
};

// Index 0..4: cat, crate, bird, cheese, mouse.
var cast = new[]
{
    PixelSprite.FromRows(new[]
    {
        "k......k.k.",
        "kk.....kkk.",
        ".k....kkkkk",
        ".kkkkkkkkak",
        ".kkkkkkkkkk",
        ".kggggkkk..",
        "..k.k..k.k.",
    }, fur),
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
    PixelSprite.FromRows(new[]
    {
        "....dddd",
        "..ddcccd",
        "ddckcccd",
        "dcccccdd",
        "dcckcccd",
        "dddddddd",
    }, fur),
    PixelSprite.FromRows(new[]
    {
        "...kk....",
        "..kkkk...",
        "g.kkkkk..",
        "gkkkkkkw.",
        "gkkkkkak.",
        "..kkkkk..",
        "..k..k...",
    }, fur),
};

var rimTone = Color.FromRgbByte(255, 246, 226);

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

var scene = (RasterSurface ctx) =>
{
    var t = ctx.Elapsed.TotalSeconds;
    var now = ctx.Now;
    SetTones();

    var stamp = $"{now:HHmm}{baseTone.R:F2}{baseTone.G:F2}{baseTone.B:F2}";
    bigTime.Ensure($"whole{stamp}{shadowStrength:F2}", s => SmallTime(s, now, accent, timeShadow));
    bigTop.Ensure($"top{stamp}{shadowStrength:F2}", s => SmallTime(s, now, accent, timeShadow, 1));
    bigBottom.Ensure($"bottom{stamp}{shadowStrength:F2}", s => SmallTime(s, now, accent, timeShadow, 2));

    timeOnPaper.Ensure($"{stamp}{shadowStrength:F2}", s => SmallTime(s, now, accent, timeShadow));
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
            case 5: ActCatalogue(ctx, progress); break;
            case 6: ActFlipBoard(ctx, progress); break;
            case 7: ActFold(ctx, progress); break;
            case 8: ActSpotlight(ctx, progress); break;
            default: ActMeasure(ctx, progress); break;
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

    var total = actDuration + holdBigTime;

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
    if (second < hold + holdBigTime) return point;
    return (second - holdBigTime) / actDuration;
}

// The moment (0..1) at which the big time stands complete, just before it dissolves.
double HoldPoint(int act) => act switch
{
    0 => 3.18 / 4.70,
    1 => 2.70 / 3.90,
    2 => 2.72 / 4.30,
    3 => 2.56 / 3.76,
    4 => 2.25 / 3.85,
    5 => 2.95 / 4.10,
    6 => 1.87 / 4.45,
    7 => 2.74 / 3.62,
    8 => 2.65 / 4.10,
    _ => 2.42 / 4.30,
};

// Indices of the ticked acts, in the order of the header comment.
List<int> EnabledActs()
{
    var flags = new[]
    {
        actDelivery, actChase, actTrap, actWindow, actTumble, actCatalogue,
        actFlipBoard, actFold, actSpotlight, actMeasure,
    };
    var enabled = new List<int>();
    for (var i = 0; i < flags.Length; i++)
        if (flags[i]) enabled.Add(i);
    return enabled;
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
        ctx.DrawSurface(fieldWithTime);
        var jolt = Shake.At(s - catOn, amplitude: 1.6, duration: 0.24, frequency: 40.0);
        var grab = Easings.EaseInCubic(MathH.Clamp01((s - grabOff) / (grabOn - grabOff)));

        var drop = Easings.EaseOutBounce(MathH.Clamp01(s / cheeseOn));
        var kx = MathH.Lerp(8.0, -18.0, grab);
        if (s < cheeseOn) DeliveryFigureSoft(ctx, 3, 8.0, MathH.Lerp(-14.0, 18.0, drop) - 4.0, 0.22);
        Figure(ctx, 3, kx + jolt.X, MathH.Lerp(-14.0, 18.0, drop) + jolt.Y);

        if (s > mouseOff)
        {
            var u = Easings.EaseOutBack(MathH.Clamp01((s - mouseOff) / (mouseOn - mouseOff)));
            var x = MathH.Lerp(26.0, 16.0, u);
            if (s < mouseOn) DeliveryFigureSoft(ctx, 4, x + 4.0, 17.0, 0.22);
            Figure(ctx, 4, MathH.Lerp(x, -10.0, grab) + jolt.X, 17.0 + jolt.Y, flipX: true);
        }

        if (s > catOff)
        {
            var u = Easings.EaseOutBounce(MathH.Clamp01((s - catOff) / (catOn - catOff)));
            var y = MathH.Lerp(-10.0, 17.0, u);
            if (s < catOn) DeliveryFigureSoft(ctx, 0, 7.0, y - 4.0, 0.22);
            Figure(ctx, 0, 7.0 + jolt.X, y + jolt.Y, flipX: true);
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
        ctx.DrawSurface(bigTime.Surface, x: jolt.X, y: MathH.Lerp(1.6, 0.0, g) + jolt.Y);
        DeliveryDust(ctx, 12.0, 20.0, 20.0, s - stampOn, accentLight);
        DeliveryFlash(ctx, 1.0 - (s - stampOn) / 0.11);
        return;
    }

    if (s < dissolveOn)
    {
        var m = (s - holdOn) * 1.12;
        ctx.DrawSurface(paperPlain);
        ctx.DrawSurface(bigTime.Surface, alpha: MathH.Clamp01(1.0 - m / 0.36));
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
        ctx.DrawSurface(fieldWithTime);
        ChaseConvoy(ctx, s, tMouse, dMouse, tCheese, dCheese, tCat, dCat);
        return;
    }

    if (s < tDissolve)
    {
        var edge = MathH.Lerp(24.0, 0.0, Easings.EaseInOutCubic(MathH.Clamp01((s - tSheet) / dSheet)));
        if (edge > 0.5)
        {
            ctx.DrawSurface(fieldWithTime);
            ChaseRunner(ctx, 0, Math.Min(7.0, 7.0 - (16.0 - edge) * 1.7), 12.0, MathH.Clamp01((16.0 - edge) / 9.0));
            ChaseSheetEdge(ctx, edge);
            ctx.DrawSurface(paperPlain, x: edge);
        }
        else
        {
            ctx.DrawSurface(paperPlain);
        }

        if (s < tBig) return;

        var e1 = Easings.EaseOutBack(MathH.Clamp01((s - tBig) / dBig));
        var e2 = Easings.EaseOutBack(MathH.Clamp01((s - tBig - stagger) / dBig));
        var x1 = MathH.Lerp(27.0, 0.0, e1);
        var x2 = MathH.Lerp(27.0, 0.0, e2);
        ctx.DrawSurface(bigTop.Surface, x: x1 + 5.0, alpha: MathH.Clamp01(1.0 - e1) * 0.35);
        ctx.DrawSurface(bigBottom.Surface, x: x2 + 5.0, alpha: MathH.Clamp01(1.0 - e2) * 0.35);
        ctx.DrawSurface(bigTop.Surface, x: x1);
        ctx.DrawSurface(bigBottom.Surface, x: x2);
        return;
    }

    if (s < tEnd)
    {
        var m = (s - tDissolve) * 1.6;
        ctx.DrawSurface(paperPlain);
        ctx.DrawSurface(bigTime.Surface, alpha: MathH.Clamp01(1.0 - m / 0.30));
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
        ChaseRunner(ctx, 4, MathH.Lerp(26.0, -14.0, u), 3, 1.0);
    }

    if (s >= tCheese && s < tCheese + dCheese)
    {
        var u = (s - tCheese) / dCheese;
        ChaseRunner(ctx, 3, MathH.Lerp(26.0, -12.0, u), 18, 1.0);
    }

    if (s < tCat) return;

    var v = MathH.Clamp01((s - tCat) / dCat);
    var x = MathH.Lerp(26.0, 7.0, Easings.EaseOutBack(v));
    var after = s - tCat - dCat;
    var bounce = after > 0.0 ? Math.Sin(after * 24.0) * Math.Exp(-after * 8.0) * 1.4 : 0.0;
    ChaseRunner(ctx, 0, x, 12.0 + bounce, (1.0 - v) * (1.0 - v));
}

// Figure with speed streaks, ghost image and drop shadow, travelling left.
void ChaseRunner(RasterSurface ctx, int nr, double x, double y, double tempo)
{
    var right = x + FigureWidth(nr);
    var height = FigureHeight(nr);

    for (var r = -1; r <= height; r++)
    {
        if (Scatter.Value(nr * 13 + r, 5.0) < 0.42) continue;
        var length = 5.0 + Scatter.Value(r, nr * 3.0) * 14.0;
        ChaseStreak(ctx, right + 1.0, right + 1.0 + length * tempo, (int)Math.Round(y) + r, 0.95 * tempo);
    }

    ChaseGhost(ctx, nr, x + 3.0 * tempo, y, 0.30 * tempo);
    ChaseShadow(ctx, nr, x, y);
    Figure(ctx, nr, x, y, flipX: true);
}

void ChaseShadow(RasterSurface ctx, int nr, double x, double y)
{
    var helper = ctx.CreateSurface(24, 24);
    FigureRaw(helper, nr, x + 1.0, y + 1.0, flipX: true);
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
    FigureRaw(helper, nr, x, y, flipX: true);
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
    var s = p * 4.3;

    if (s < 1.86)
    {
        TrapScene(ctx, s);
        if (s > 1.46) TrapSweep(ctx, Easings.EaseInOutSine((s - 1.46) / 0.36), paperPlain);
        return;
    }

    if (s < 2.72)
    {
        ctx.DrawSurface(paperPlain);
        var top = TrapIn(s, 1.86, 2.22);
        var bottom = TrapIn(s, 2.08, 2.46);
        if (top > 0.0) ctx.DrawSurface(bigTop.Surface, y: MathH.Lerp(-15.0, 0.0, Easings.EaseOutBack(top)));
        if (bottom > 0.0) ctx.DrawSurface(bigBottom.Surface, x: MathH.Lerp(-26.0, 0.0, Easings.EaseOutBack(bottom)));
        return;
    }

    if (s < 3.80)
    {
        var m = s - 2.72;
        ctx.DrawSurface(paperPlain);
        ctx.DrawSurface(bigTime.Surface, alpha: MathH.Clamp01(1.0 - m / 0.45));
        return;
    }

    ctx.DrawSurface(paperWithTime);
    TrapSweep(ctx, Easings.EaseInOutSine((s - 3.80) / 0.44), fieldWithTime);
}

// The cat sneaks under the hanging crate to get the bait, and the crate comes down on it.
void TrapScene(RasterSurface ctx, double s)
{
    var cheese = TrapIn(s, 0.18, 0.52);
    var crate = TrapIn(s, 0.38, 0.78);
    var cat = TrapIn(s, 0.58, 0.98);
    var mouse = TrapIn(s, 0.98, 1.16);
    var drop = Easings.EaseInCubic(TrapIn(s, 1.20, 1.34));

    ctx.DrawSurface(fieldWithTime);
    TrapStage(ctx, TrapIn(s, 0.02, 0.30));
    TrapShadow(ctx, 0, 7, cheese * cheese);
    TrapShadow(ctx, 14, 23, cat * cat);

    if (cheese > 0.0) TrapPiece(ctx, 3, MathH.Lerp(-10.0, 0.0, Easings.EaseOutBack(cheese)), 12);
    if (cat > 0.0) TrapPiece(ctx, 0, MathH.Lerp(30.0, 13.0, Easings.EaseOutBack(cat)), 11, flipX: true);
    if (mouse > 0.0) TrapPiece(ctx, 4, MathH.Lerp(-11.0, 7.0, Easings.EaseOutBack(mouse)), 11);
    if (crate > 0.0)
    {
        var hang = MathH.Lerp(-13.0, -4.0, Easings.EaseOutBack(crate));
        var jolt = Shake.At(s - 1.34, amplitude: 1.4, duration: 0.20, frequency: 42.0);
        TrapPiece(ctx, 1, 14.0 + (drop >= 1.0 ? jolt.X : 0.0), MathH.Lerp(hang, 7.0, drop));
    }

    TrapLight(ctx, s - 1.00);
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
    const double dWindowOpen = 0.62;
    const double dWindowBelt = 1.10;
    const double dWindowClose = 0.24;
    const double dWindowBig = 0.60;
    const double dWindowDissolve = 0.80;
    const double dWindowBack = 0.40;

    var tOpen = dWindowOpen;
    var tBelt = tOpen + dWindowBelt;
    var tClose = tBelt + dWindowClose;
    var tBig = tClose + dWindowBig;
    var tDissolve = tBig + dWindowDissolve;
    var s = p * (tDissolve + dWindowBack);

    if (s < tOpen) WindowOpen(ctx, s);
    else if (s < tClose) WindowBelt(ctx, s, s - tBelt);
    else if (s < tBig) WindowTime(ctx, s - tClose);
    else if (s < tDissolve) WindowDissolve(ctx, s - tBig, dWindowDissolve);
    else WindowBack(ctx, s - tDissolve);
}

void WindowOpen(RasterSurface ctx, double s)
{
    ctx.DrawSurface(fieldWithTime);
    for (var i = 0; i < 3; i++)
    {
        var local = s - i * 0.09;
        var blind = Easings.EaseOutCubic(MathH.Clamp01(local / 0.30));
        var rows = (int)Math.Round(blind * 8.0);
        if (rows <= 0) continue;
        for (var r = 0; r < rows; r++)
        for (var x = 0; x < 24; x++)
            ctx[x, i * 8 + r] = paper;
        WindowFigure(ctx, i, WindowTravel(i, s), Math.Min(rows, 7));
        WindowShelf(ctx, i, Easings.EaseOutCubic(MathH.Clamp01((local - 0.30) / 0.16)));
    }
}

void WindowBelt(RasterSurface ctx, double tTravel, double tShut)
{
    ctx.DrawSurface(paperPlain);
    for (var i = 0; i < 3; i++)
    {
        WindowShelf(ctx, i, 1.0);
        WindowFigure(ctx, i, WindowTravel(i, tTravel), 7);
    }

    if (tShut <= 0.0) return;
    for (var i = 0; i < 3; i++)
    {
        var u = MathH.Clamp01((tShut - i * 0.04) / 0.16);
        var covered = (int)Math.Ceiling(u * 4.0);
        for (var k = 0; k < covered; k++)
        for (var x = 0; x < 24; x++)
        {
            ctx[x, i * 8 + k] = paper;
            ctx[x, i * 8 + 7 - k] = paper;
        }
    }
}

void WindowTime(RasterSurface ctx, double s)
{
    ctx.DrawSurface(paperPlain);
    var top = Easings.EaseOutBack(MathH.Clamp01(s / 0.30));
    var bottom = Easings.EaseOutBack(MathH.Clamp01((s - 0.13) / 0.30));
    ctx.DrawSurface(bigTop.Surface, x: MathH.Lerp(27.0, 0.0, top));
    ctx.DrawSurface(bigBottom.Surface, x: MathH.Lerp(-27.0, 0.0, bottom));
}

void WindowDissolve(RasterSurface ctx, double s, double duration)
{
    ctx.DrawSurface(paperPlain);
    ctx.DrawSurface(bigTop.Surface, alpha: MathH.Clamp01(1.0 - s / 0.34));
    ctx.DrawSurface(bigBottom.Surface);
    ctx.DrawSurface(timeOnPaper.Surface, alpha: MathH.Clamp01((s - duration + 0.22) / 0.22));
}

void WindowBack(RasterSurface ctx, double s)
{
    for (var i = 0; i < 3; i++)
    {
        var u = Easings.EaseOutCubic(MathH.Clamp01((s - (2 - i) * 0.06) / 0.24));
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

// Shelf board of level i, u = how far it has slid in.
void WindowShelf(RasterSurface ctx, int i, double u)
{
    if (u <= 0.0) return;
    var span = Math.Min(24, (int)Math.Round(u * 24.0));
    for (var k = 0; k < span; k++)
        ctx[i % 2 == 0 ? 23 - k : k, i * 8 + 7] = accent;
}

// Own speed, own direction, and the belt nearly stops in the middle of the screen.
double WindowTravel(int i, double t)
{
    var delay = i == 0 ? 0.00 : i == 1 ? 0.18 : 0.38;
    var duration = i == 0 ? 1.98 : i == 1 ? 1.86 : 1.72;
    var v = MathH.Clamp01((t - delay) / duration);
    var eased = v + 0.5 * Math.Sin(v * Math.PI * 2.0) / (Math.PI * 2.0);
    var width = FigureWidth(WindowCast(i));
    return i % 2 == 0 ? 24.0 - eased * (24.0 + width) : -width + eased * (24.0 + width);
}

// Top to bottom: cat, mouse, cheese - the food chain as three conveyor belts.
int WindowCast(int i) => i switch { 0 => 0, 1 => 4, _ => 3 };

void WindowFigure(RasterSurface ctx, int i, double x, int visible)
{
    var nr = WindowCast(i);
    var top = i * 8;
    var helper = ctx.CreateSurface(24, 24);
    Figure(helper, nr, x, top + 7 - FigureHeight(nr), flipX: i % 2 == 0);
    for (var py = top; py < Math.Min(24, top + visible); py++)
    for (var px = 0; px < 24; px++)
        if (helper[px, py].A > 0.0) ctx[px, py] = helper[px, py];
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
        TumbleEdge(ctx, fieldWithTime, paperPlain, cx2 - 5.0);

        var u1 = MathH.Clamp01((s - catStart) / catSpan);
        if (u1 < 1.0)
            TumbleSpin(ctx, 0, MathH.Lerp(-5.5, 21.0, u1), TumbleArc(u1, 20.0, 6.0, 0.45), TumbleQuarter(u1, 0.38, 0.50, 0.88));

        if (s >= cheeseStart && u2 < 1.0)
            TumbleSpin(ctx, 3, cx2, TumbleArc(u2, 15.0, 8.0, 0.5), TumbleQuarter(u2, 0.42, 0.55));

        if (s >= bottomStart)
        {
            var uU = MathH.Clamp01((s - bottomStart) / bottomSpan);
            var jolt = TumbleJolt(s - (topStart + topSpan * 0.6));
            ctx.DrawSurface(bigBottom.Surface, y: (TumbleDrop(uU) - 1.0) * 21.0 + jolt);
        }

        if (s >= topStart)
        {
            var uO = MathH.Clamp01((s - topStart) / topSpan);
            ctx.DrawSurface(bigTop.Surface, y: (TumbleDrop(uO) - 1.0) * 15.0);
        }
    }
    else if (s < backStart)
    {
        var m = s - dissolveStart;
        ctx.DrawSurface(paperPlain);
        ctx.DrawSurface(bigTime.Surface, alpha: MathH.Clamp01(1.0 - m / 0.22));
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

// ---------------------------------------------------------------- act: catalogue

void ActCatalogue(RasterSurface ctx, double p)
{
    const double catalogueTotal = 4.1;
    const double catalogueOpen = 0.34;
    const double catalogueBreath = 0.13;
    const double catalogueTurn = 0.34;
    const double catalogueHoldBig = 0.34;
    const double catalogueFly = 0.55;
    const double catalogueHoldSmall = 0.15;

    var s = p * catalogueTotal;
    var paging = 0.0;
    for (var i = 0; i < CataloguePageCount(); i++) paging += CatalogueSlot(i);

    var tPages = catalogueOpen + paging;
    var tBreath = tPages + catalogueBreath;
    var tTurn = tBreath + catalogueTurn;
    var tHold = tTurn + catalogueHoldBig;
    var tFly = tHold + catalogueFly;
    var tSmall = tFly + catalogueHoldSmall;

    if (s < catalogueOpen)
    {
        var u = Easings.EaseOutCubic(s / catalogueOpen);
        CatalogueTurn(ctx, fieldWithTime, CataloguePage(ctx, 0), u, toLeft: true);
    }
    else if (s < tPages)
    {
        var m = s - catalogueOpen;
        var nr = 0;
        var start = 0.0;
        while (nr < CataloguePageCount() - 1 && m >= start + CatalogueSlot(nr))
        {
            start += CatalogueSlot(nr);
            nr++;
        }
        var slot = CatalogueSlot(nr);
        var local = m - start;
        var stamp = slot * 0.72;
        var content = CatalogueContent(ctx, nr);
        if (local < stamp)
        {
            ctx.DrawBackground(paper);
            ctx.DrawSurface(content);
        }
        else
        {
            CatalogueSweep(ctx, content, (local - stamp) / (slot - stamp));
        }
    }
    else if (s < tBreath)
    {
        ctx.DrawSurface(paperPlain);
    }
    else if (s < tTurn)
    {
        var u = Easings.EaseOutCubic((s - tBreath) / catalogueTurn);
        CatalogueTurn(ctx, paperPlain, CatalogueTimePage(ctx, 3.0), u, toLeft: true);
    }
    else if (s < tHold)
    {
        var u = Easings.EaseOutBack(MathH.Clamp01((s - tTurn) / 0.26));
        ctx.DrawSurface(paperPlain);
        ctx.DrawSurface(bigTime.Surface, y: MathH.Lerp(3.0, 0.0, u));
    }
    else if (s < tFly)
    {
        var m = (s - tHold) / catalogueFly * 1.08;
        ctx.DrawSurface(paperPlain);
        ctx.DrawSurface(bigTime.Surface, alpha: MathH.Clamp01(1.0 - m / 0.5));
    }
    else if (s < tSmall)
    {
        ctx.DrawSurface(paperWithTime);
    }
    else
    {
        var u = Easings.EaseOutCubic(MathH.Clamp01((s - tSmall) / Math.Max(0.001, catalogueTotal - tSmall)));
        CatalogueTurn(ctx, paperWithTime, fieldWithTime, u, toLeft: false);
    }
}

int CataloguePageCount() => 9;

double CatalogueSlot(int i) => Math.Max(0.11, 0.36 * Math.Pow(0.84, i));

int CatalogueFigureNo(int i) => new[] { 2, 0, 1, 3, 4, 0, 2, 3, 4 }[i];

int CataloguePageNo(int i) => new[] { 4, 7, 11, 17, 25, 34, 46, 61, 79 }[i];

// One catalogue page without background: figure, floor shadow, two text bars, page number.
RasterSurface CatalogueContent(RasterSurface ctx, int i)
{
    var content = ctx.CreateSurface(24, 24);
    var nr = CatalogueFigureNo(i);
    var w = FigureWidth(nr);
    var h = FigureHeight(nr);
    var x0 = (int)Math.Round((24 - w) / 2.0);
    var y0 = 11 - (int)Math.Round(h / 2.0);

    var box = accentLight;
    var floor = Tone(0.46, 0.85, 0.000);
    for (var y = 5; y <= 17; y++)
    for (var x = 2; x <= 21; x++)
        content[x, y] = box;
    for (var x = x0 + 1; x < x0 + w - 1; x++)
        if (x >= 2 && x <= 21 && y0 + h <= 17) content[x, y0 + h] = floor;

    Figure(content, nr, x0, y0);

    CatalogueBar(content, 3, 19, 8 + (i % 4) * 2, Tone(0.64, 0.06, 0.000));
    CatalogueBar(content, 3, 21, 5 + (i % 3) * 2, Tone(0.78, 0.05, 0.000));
    content.DrawTextVar3x5(CataloguePageNo(i).ToString(), 16, 0, accent);
    return content;
}

void CatalogueBar(RasterSurface s, int x0, int y, int length, Color c)
{
    for (var x = x0; x < x0 + length; x++)
        if (x >= 0 && x < 24 && y >= 0 && y < 24) s[x, y] = c;
}

RasterSurface CataloguePage(RasterSurface ctx, int i)
{
    var side = ctx.CreateSurface(24, 24);
    side.DrawBackground(paper);
    side.DrawSurface(CatalogueContent(ctx, i));
    return side;
}

RasterSurface CatalogueTimePage(RasterSurface ctx, double dy)
{
    var side = ctx.CreateSurface(24, 24);
    side.DrawBackground(paper);
    side.DrawSurface(bigTime.Surface, y: dy);
    return side;
}

// Turning page: a hard crease travels across, preceded by the shadow of the lifted sheet.
void CatalogueTurn(RasterSurface ctx, RasterSurface older, RasterSurface newer, double u, bool toLeft)
{
    var k = MathH.Clamp01(u);
    var edge = toLeft ? 24.0 * (1.0 - k) : 24.0 * k;
    var view = MathH.Clamp01(Math.Min(k, 1.0 - k) * 8.0);
    var crease = Tone(0.25, 0.50, 0.000);

    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
    {
        var d = x + 0.5 - edge;
        var isNewer = toLeft ? d > 0.0 : d < 0.0;
        var ab = Math.Abs(d);
        var c = isNewer ? newer[x, y] : older[x, y];
        var core = MathH.Clamp01(1.3 - ab);
        var dark = isNewer
            ? Math.Max(0.80 * core, 0.45 * MathH.Clamp01(1.0 - ab / 4.0))
            : 0.40 * MathH.Clamp01(1.0 - ab / 1.5);
        ctx[x, y] = ColorOps.Lerp(c, crease, dark * view);
    }
}

// In between: the discarded page snaps out to the left, edge on.
void CatalogueSweep(RasterSurface ctx, RasterSurface content, double u)
{
    var k = MathH.Clamp01(u);
    ctx.DrawBackground(paper);
    var width = MathH.Lerp(0.62, 0.26, k) * 24.0;
    var x = MathH.Lerp(3.0, -3.0, k);
    ctx.DrawSurfaceRegion(content, 0, 0, 24, 24, destX: x, destY: 0, destWidth: width, destHeight: 24, alpha: 0.85);
    var kx = (int)Math.Round(x + width);
    if (kx >= 0 && kx < 24)
        for (var y = 0; y < 24; y++) ctx[kx, y] = ColorOps.Lerp(ctx[kx, y], accentLight, 0.55);
}

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

    if (s < tFirst) FlipRound(ctx, s, fieldWithTime, bigSheet, flipStepsOne, flipStagger, 1);
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
    SpotStage(ctx);
    SpotPaint(ctx, Easings.EaseOutCubic(MathH.Clamp01(s / 0.14)), -34.0, spotNarrow, 1.1, 34.0, SpotSpark(s - 0.17), 0.0, 0.0);
}

void SpotSearch(RasterSurface ctx, double s)
{
    SpotStage(ctx);
    SpotPaint(ctx, 1.0, SpotSearchAngle(s), spotNarrow, 1.1, 34.0, 1.0, 0.0, 0.0);
}

void SpotFind(RasterSurface ctx, double s, double duration)
{
    var u = MathH.Clamp01(s / duration);
    var swing = MathH.Lerp(-27.0, 24.0, Easings.EaseInOutSine(u)) * (1.0 - Easings.EaseInCubic(MathH.Clamp01((u - 0.5) / 0.5)));
    var opening = Easings.EaseInCubic(MathH.Clamp01((u - 0.45) / 0.55));
    SpotStage(ctx, MathH.Lerp(0.0, -12.0, Easings.EaseInCubic(MathH.Clamp01((u - 0.18) / 0.34))));
    SpotPaint(ctx, 1.0, swing, MathH.Lerp(spotNarrow, 0.36, opening), MathH.Lerp(1.1, 1.8, opening), 34.0, 1.0, 0.0, 0.0);
}

void SpotFlood(RasterSurface ctx, double s)
{
    SpotStage(ctx, -99.0);
    SpotPaint(ctx, 1.0, 0.0, 0.36, 1.8, 34.0, 1.0, MathH.Clamp01(s / 0.06), Envelope.Bell(MathH.Clamp01(s / 0.18)) * 0.40);
}

void SpotDissolve(RasterSurface ctx, double s, double duration)
{
    ctx.DrawSurface(paperPlain);
    ctx.DrawSurface(bigTop.Surface, alpha: MathH.Clamp01(1.0 - s / 0.26));
    ctx.DrawSurface(bigBottom.Surface);
    ctx.DrawSurface(timeOnPaper.Surface, alpha: MathH.Clamp01((s - duration + 0.30) / 0.20));
}

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

// The cone finds the mouse before the time; she scurries off once it moves on.
void SpotStage(RasterSurface ctx, double mouseX = 0.0)
{
    ctx.DrawSurface(paperPlain);
    ctx.DrawSurface(bigTime.Surface);
    if (mouseX > -10.0) Figure(ctx, 4, mouseX, 17, flipX: true);
}

double SpotSearchAngle(double s)
{
    if (s < 0.34) return MathH.Lerp(-34.0, 27.0, Easings.EaseInOutCubic(s / 0.34));
    if (s < 0.42) return 27.0;
    if (s < 0.70) return MathH.Lerp(27.0, -9.0, Easings.EaseInOutCubic((s - 0.42) / 0.28));
    if (s < 0.76) return -9.0;
    return MathH.Lerp(-9.0, -27.0, Easings.EaseInOutCubic(MathH.Clamp01((s - 0.76) / 0.26)));
}

double SpotSpark(double s)
{
    if (s < 0.0) return 0.0;
    if (s < 0.07) return 0.9;
    if (s < 0.13) return 0.0;
    return MathH.Clamp01(0.4 + (s - 0.13) / 0.09);
}

// ---------------------------------------------------------------- act: measure

// Guides and dimension arrows measure the screen and snap onto the outline of the digits.
void ActMeasure(RasterSurface ctx, double p)
{
    var s = p * 4.3;
    var pen = Tone(0.67, 0.80, 0.010);
    var dim = accent;
    var ink = edgeTone;

    if (s < 0.34)
    {
        ctx.DrawSurface(fieldWithTime);
        MeasureSweep(ctx, paperPlain, Easings.EaseInOutSine(s / 0.34), true);
        return;
    }

    if (s >= 3.85)
    {
        ctx.DrawSurface(paperWithTime);
        MeasureSweep(ctx, fieldWithTime, Easings.EaseInOutSine((s - 3.85) / 0.45), false);
        return;
    }

    ctx.DrawSurface(paperPlain);

    if (s >= 2.42)
    {
        var m = s - 2.42;
        var away = MeasureIn(s, 2.42, 2.76);
        if (away < 1.0)
        {
            var off = MathH.Lerp(0.0, 7.0, Easings.EaseInCubic(away));
            MeasureFrame(ctx, MeasureRound(-off), MeasureRound(2.0 - off), MeasureRound(23.0 + off), MeasureRound(20.0 + off), pen, 1.0, 1.0 - away);
        }
        if (s < 3.52)
        {
            ctx.DrawSurface(bigTime.Surface, alpha: MathH.Clamp01(1.0 - m / 0.42));
        }
        else ctx.DrawSurface(paperWithTime);
        return;
    }

    var z = s < 1.06 ? 0.0 : MeasureSnap((s - 1.06) / 0.42);
    var air = s < 1.86 ? 0.0 : s < 1.94 ? 2.0 : 1.0;
    var lx = MathH.Lerp(-2.0, 1.0, z) - air;
    var rx = MathH.Lerp(25.0, 22.0, z) + air;
    var oy = MathH.Lerp(-2.0, 0.0, z) - air;
    var uy = MathH.Lerp(25.0, 23.0, z) + air;
    var x0 = Math.Max(0, MeasureRound(lx));
    var x1 = Math.Min(23, MeasureRound(rx));
    var y0 = Math.Max(0, MeasureRound(oy));
    var y1 = Math.Min(23, MeasureRound(uy));

    var snapping = (s > 1.14 && s < 1.26) || (s > 1.86 && s < 1.96);
    MeasureFrame(ctx, x0, y0, x1, y1, snapping ? ink : pen, MeasureIn(s, 0.32, 0.62), 1.0);
    MeasureCorners(ctx, x0, y0, x1, y1, ink, MeasureIn(s, 1.04, 1.10) * (1.0 - MeasureIn(s, 1.26, 1.48)));

    var fade = 1.0 - MeasureIn(s, 1.34, 1.48);
    if (fade > 0.0)
    {
        var eb = Easings.EaseOutBack(MeasureIn(s, 0.48, 0.78));
        if (eb > 0.0) MeasureArrowH(ctx, 11, MathH.Lerp(11.5, lx, eb), MathH.Lerp(11.5, rx, eb), dim, fade);
        var eh = Easings.EaseOutBack(MeasureIn(s, 0.56, 0.86));
        if (eh > 0.0) MeasureArrowV(ctx, 11, MathH.Lerp(11.5, oy, eh), MathH.Lerp(11.5, uy, eh), dim, fade);

        var ab = Easings.EaseOutBack(MeasureIn(s, 0.78, 0.90));
        if (ab > 0.0) MeasureNumber(ctx, x1 - x0 + 1, 3, 5 + MeasureRound(2.0 - ab * 2.0), dim, fade * MathH.Clamp01(ab * 2.0));
        var ah = Easings.EaseOutBack(MeasureIn(s, 0.86, 0.98));
        if (ah > 0.0) MeasureNumber(ctx, y1 - y0 + 1, 14, 13 + MeasureRound(2.0 - ah * 2.0), dim, fade * MathH.Clamp01(ah * 2.0));
    }

    if (s > 1.50)
        MeasureReveal(ctx, s < 1.86 ? MathH.Lerp(0.5, 23.5, Easings.EaseInOutSine((s - 1.50) / 0.36)) : 30.0, ink);
}

// Four corner marks that flash the moment things snap into place.
void MeasureCorners(RasterSurface ctx, int x0, int y0, int x1, int y1, Color c, double a)
{
    if (a <= 0.0) return;
    for (var i = 1; i <= 2; i++)
    {
        MeasureBlend(ctx, x0 + i, y0, c, a);
        MeasureBlend(ctx, x0, y0 + i, c, a);
        MeasureBlend(ctx, x1 - i, y0, c, a);
        MeasureBlend(ctx, x1, y0 + i, c, a);
        MeasureBlend(ctx, x0 + i, y1, c, a);
        MeasureBlend(ctx, x0, y1 - i, c, a);
        MeasureBlend(ctx, x1 - i, y1, c, a);
        MeasureBlend(ctx, x1, y1 - i, c, a);
    }
}

// Paper slides in behind a hard measuring edge, left to right or back.
void MeasureSweep(RasterSurface ctx, RasterSurface target, double u, bool toRight)
{
    var edge = MathH.Lerp(-2.0, 26.0, MathH.Clamp01(u));
    var blade = edgeTone;
    var stroke = Tone(0.67, 0.80, 0.010);
    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
    {
        var d = toRight ? x + 0.5 : 23.5 - x;
        if (d < edge - 2.0) ctx[x, y] = target[x, y];
        else if (d < edge - 1.0) ctx[x, y] = stroke;
        else if (d < edge) ctx[x, y] = blade;
    }
}

// Rectangle outline drawn as one continuous line starting at the top left corner.
void MeasureFrame(RasterSurface ctx, int x0, int y0, int x1, int y1, Color c, double e, double a)
{
    var w = x1 - x0;
    var h = y1 - y0;
    var n = 2 * (w + h);
    var to = (int)Math.Round(MathH.Clamp01(e) * n);
    for (var i = 0; i < to; i++)
    {
        int x;
        int y;
        if (i < w) { x = x0 + i; y = y0; }
        else if (i < w + h) { x = x1; y = y0 + (i - w); }
        else if (i < 2 * w + h) { x = x1 - (i - w - h); y = y1; }
        else { x = x0; y = y1 - (i - 2 * w - h); }
        MeasureBlend(ctx, x, y, c, a);
    }
}

void MeasureArrowH(RasterSurface ctx, int y, double from, double to, Color c, double a)
{
    var x0 = MeasureRound(from);
    var x1 = MeasureRound(to);
    for (var x = x0; x <= x1; x++) MeasureBlend(ctx, x, y, c, a);
    MeasureBlend(ctx, x0 + 1, y - 1, c, a);
    MeasureBlend(ctx, x0 + 1, y + 1, c, a);
    MeasureBlend(ctx, x1 - 1, y - 1, c, a);
    MeasureBlend(ctx, x1 - 1, y + 1, c, a);
}

void MeasureArrowV(RasterSurface ctx, int x, double from, double to, Color c, double a)
{
    var y0 = MeasureRound(from);
    var y1 = MeasureRound(to);
    for (var y = y0; y <= y1; y++) MeasureBlend(ctx, x, y, c, a);
    MeasureBlend(ctx, x - 1, y0 + 1, c, a);
    MeasureBlend(ctx, x + 1, y0 + 1, c, a);
    MeasureBlend(ctx, x - 1, y1 - 1, c, a);
    MeasureBlend(ctx, x + 1, y1 - 1, c, a);
}

// Two-digit dimension number in 3x5 digits.
void MeasureNumber(RasterSurface ctx, int value, int x0, int y0, Color c, double a)
{
    var digits = new[]
    {
        new[] { "###", "#.#", "#.#", "#.#", "###" },
        new[] { ".#.", "##.", ".#.", ".#.", "###" },
        new[] { "###", "..#", "###", "#..", "###" },
        new[] { "###", "..#", "###", "..#", "###" },
        new[] { "#.#", "#.#", "###", "..#", "..#" },
        new[] { "###", "#..", "###", "..#", "###" },
        new[] { "###", "#..", "###", "#.#", "###" },
        new[] { "###", "..#", "..#", "..#", "..#" },
        new[] { "###", "#.#", "###", "#.#", "###" },
        new[] { "###", "#.#", "###", "..#", "###" },
    };
    var text = Math.Clamp(value, 0, 99).ToString("00");
    for (var i = 0; i < text.Length; i++)
    {
        var glyph = digits[text[i] - '0'];
        for (var r = 0; r < 5; r++)
        for (var col = 0; col < 3; col++)
            if (glyph[r][col] == '#') MeasureBlend(ctx, x0 + i * 4 + col, y0 + r, c, a);
    }
}

// A dark scan line uncovers the big time row by row.
void MeasureReveal(RasterSurface ctx, double scan, Color c)
{
    for (var y = 1; y <= 22; y++)
    {
        if (y > scan) continue;
        for (var x = 0; x < 24; x++)
        {
            var pixel = bigTime.Surface[x, y];
            if (pixel.A <= 0.0) continue;
            ctx[x, y] = ColorOps.Lerp(ctx[x, y], pixel, pixel.A);
        }
    }
    var row = MeasureRound(scan);
    if (row < 1 || row > 22) return;
    for (var x = 1; x <= 22; x++) MeasureBlend(ctx, x, row, c, 0.85);
}

void MeasureBlend(RasterSurface ctx, int x, int y, Color c, double a)
{
    if (x < 0 || x > 23 || y < 0 || y > 23 || a <= 0.0) return;
    ctx[x, y] = ColorOps.Lerp(ctx[x, y], c, MathH.Clamp01(a));
}

double MeasureIn(double s, double from, double to) => MathH.Clamp01((s - from) / (to - from));

int MeasureRound(double v) => (int)Math.Round(v);

// Fast run-up with two decaying overshoots - the snap.
double MeasureSnap(double u)
{
    if (u >= 1.0) return 1.0;
    var run = 1.0 - Math.Exp(-7.0 * u);
    return run + Math.Sin(u * Math.PI * 3.0) * 0.18 * Math.Exp(-4.5 * u);
}
