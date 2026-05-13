// ---
// app: Volcano01Erupting
// displayName: Volcano 01 Erupting
// author: Ronald Schlenker
// description: First refined active volcano. Animated rising smoke plume drifting in the wind, parabolic lava sparks ejected from the crater, pulsing crater glow, static lava flow streaming down the right slope into a steaming contact line at the waterline. Asymmetric cone with full DKC gradient, scorched-earth base, vegetation clinging to the cooler left slope, foreground granite boulders that intrude into the cone for embedded depth, and a warm lava reflection in the water on the right.
// appType: Scene
// duration: 30
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

const int canvasW = 24;
const int canvasH = 24;

// ---- PALETTE (dusky / volcanic) -------------------------------------------
var cSkyTop      = Color.FromRgb(0.32, 0.28, 0.42);   // dusky purple
var cSkyHorizon  = Color.FromRgb(0.78, 0.55, 0.42);   // ash-warm horizon
var cWaterBright = Color.FromRgb(0.18, 0.42, 0.54);
var cWaterDeep   = Color.FromRgb(0.04, 0.16, 0.28);
var cWaterFoam   = Color.FromRgb(0.95, 0.98, 1.00);
var cLavaReflect = Color.FromRgb(0.85, 0.45, 0.20);   // warm reflection in water

// Volcanic rock (5-shade asymmetric)
var cRockHi      = Color.FromRgb(0.48, 0.40, 0.36);   // bright lit (upper-left)
var cRockBase    = Color.FromRgb(0.30, 0.24, 0.22);
var cRockMid     = Color.FromRgb(0.20, 0.16, 0.16);
var cRockDark    = Color.FromRgb(0.10, 0.08, 0.08);
var cRockEdge    = Color.FromRgb(0.04, 0.03, 0.03);

// Scorched / ashy ground
var cAshHi       = Color.FromRgb(0.42, 0.30, 0.22);
var cAshDk       = Color.FromRgb(0.22, 0.16, 0.12);

// Lava
var cLavaCore    = Color.FromRgb(1.00, 0.95, 0.55);
var cLavaGlow    = Color.FromRgb(1.00, 0.55, 0.10);
var cLavaDark    = Color.FromRgb(0.78, 0.22, 0.06);
var cLavaCrust   = Color.FromRgb(0.42, 0.12, 0.05);

// Smoke
var cSmokeHot    = Color.FromRgb(0.55, 0.40, 0.36);   // dark plume base
var cSmokeMid    = Color.FromRgb(0.62, 0.55, 0.55);
var cSmokeLight  = Color.FromRgb(0.82, 0.78, 0.78);   // dispersing top

// Vegetation (sparse, dark green clinging to slope)
var cMossHi      = Color.FromRgb(0.32, 0.50, 0.20);
var cMossDk      = Color.FromRgb(0.16, 0.28, 0.12);

// Foreground boulders (granite, contrasts with volcanic rock)
var cBoulderHi   = Color.FromRgb(0.62, 0.58, 0.54);
var cBoulderMid  = Color.FromRgb(0.30, 0.28, 0.28);
var cBoulderDark = Color.FromRgb(0.10, 0.08, 0.08);

// Steam (where lava meets water)
var cSteamHi     = Color.FromRgb(0.95, 0.95, 0.95);
var cSteamMid    = Color.FromRgb(0.74, 0.74, 0.78);

// ============================================================================
// SCENE
// ============================================================================
var scene = (RasterSurface ctx) =>
{
    var t = ctx.Elapsed.TotalSeconds;

    void Put(int x, int y, Color c)
    {
        if (x >= 0 && x < canvasW && y >= 0 && y < canvasH) ctx.SetPixel(x, y, c);
    }

    // ---- LAYER 0: sky + water gradient
    var horizonY = 8;
    for (var y = 0; y < canvasH; y++)
    for (var x = 0; x < canvasW; x++)
    {
        Color c;
        if (y < horizonY)
        {
            var f = y / (double)(horizonY - 1);
            c = new Color(
                cSkyTop.R + (cSkyHorizon.R - cSkyTop.R) * f,
                cSkyTop.G + (cSkyHorizon.G - cSkyTop.G) * f,
                cSkyTop.B + (cSkyHorizon.B - cSkyTop.B) * f);
        }
        else
        {
            var f = (y - horizonY) / (double)(canvasH - 1 - horizonY);
            c = new Color(
                cWaterBright.R + (cWaterDeep.R - cWaterBright.R) * f,
                cWaterBright.G + (cWaterDeep.G - cWaterBright.G) * f,
                cWaterBright.B + (cWaterDeep.B - cWaterBright.B) * f);
        }
        ctx.SetPixel(x, y, c);
    }

    void Sprite((string row, int y)[] rows, Func<char, Color?> map)
    {
        foreach (var (row, sy) in rows)
        for (var sx = 0; sx < row.Length; sx++)
        {
            var ch = row[sx];
            if (ch == ' ' || ch == '.') continue;
            var c = map(ch);
            if (c.HasValue) Put(sx, sy, c.Value);
        }
    }

    // ========================================================================
    // LAYER 1: VOLCANO CONE (asymmetric, full DKC gradient)
    // Sun is upper-left → highlights on the left flank, shadows on right.
    // Crater opening at top-center (cols 11-13). Cone base spans about cols
    // 1-22 at y=20 (wide foot). Total height ~12 rows.
    //
    // Letters:
    //   H  bright highlight (upper-left flank)
    //   b  base rock
    //   M  mid shadow
    //   d  deep shadow (right flank)
    //   o  outline at waterline
    //   .  transparent
    // ========================================================================
    var craterX = 12;
    var craterY = 9;

    Sprite(new (string, int)[]
    {
        ("           ddd          ",  9),  // crater rim (3 wide)
        ("          dHbbMd        ", 10),
        ("         dHHbbbMMd      ", 11),
        ("        dHHbbbbMMMd     ", 12),
        ("       dHHbbbbbbMMMd    ", 13),
        ("      dHHbbbbbbbbMMMd   ", 14),
        ("     dHHbbbbbbbbbbMMMd  ", 15),
        ("    dHHbbbbbbbbbbbbMMMd ", 16),
        ("   dHHbbbbbbbbbbbbbbMMMd", 17),
        ("  dHHbbbbbbbbbbbbbbbbMMd", 18),
        (" oodbbbbbbbbbbbbbbbbbbdo", 19),
        ("  ooooooooooooooooooooo ", 20),
    }, ch => ch switch
    {
        'H' => cRockHi,
        'b' => cRockBase,
        'M' => cRockMid,
        'd' => cRockDark,
        'o' => cRockEdge,
        _   => (Color?)null,
    });

    // ========================================================================
    // LAYER 2: LAVA FLOW down the RIGHT slope (static, glowing)
    // Bright vein from crater rim down to waterline, with cooler crust on edges.
    // ========================================================================
    Sprite(new (string, int)[]
    {
        ("              CL        ", 11),
        ("               CL       ", 12),
        ("                CL      ", 13),
        ("                CCL     ", 14),
        ("                 CCL    ", 15),
        ("                 CCCL   ", 16),
        ("                  CCCL  ", 17),
        ("                  CCCcL ", 18),
        ("                  cCccL ", 19),
    }, ch => ch switch
    {
        'C' => cLavaCore,
        'L' => cLavaGlow,
        'c' => cLavaDark,
        _   => (Color?)null,
    });

    // ========================================================================
    // LAYER 3: SCORCHED EARTH band at base of the cone
    // ========================================================================
    foreach (var sx in new[] { 3, 5, 8, 14, 18, 21 }) Put(sx, 19, cAshHi);
    foreach (var sx in new[] { 4, 7, 12, 16, 20 })     Put(sx, 18, cAshDk);

    // ========================================================================
    // LAYER 4: MOSS / sparse vegetation on the cooler LEFT slope
    // ========================================================================
    Put(5, 17, cMossHi);
    Put(4, 18, cMossDk);
    Put(6, 18, cMossHi);
    Put(3, 19, cMossDk);
    Put(7, 19, cMossDk);

    // ========================================================================
    // LAYER 5: FOREGROUND BOULDERS (embedding move) — granite, in front of
    // the cone base. Drawn AFTER the cone so they visibly overlap its silhouette.
    // ========================================================================
    Sprite(new (string, int)[]
    {
        ("HRR                     ", 18),
        ("RRrM                    ", 19),
        ("ooooo                   ", 20),
    }, ch => ch switch
    {
        'H' => cBoulderHi,
        'R' => cBoulderMid,
        'r' => cBoulderMid,
        'M' => cBoulderDark,
        'o' => cBoulderDark,
        _   => (Color?)null,
    });
    Sprite(new (string, int)[]
    {
        ("                    HRR ", 18),
        ("                   HRRrM", 19),
        ("                   ooooo", 20),
    }, ch => ch switch
    {
        'H' => cBoulderHi,
        'R' => cBoulderMid,
        'r' => cBoulderMid,
        'M' => cBoulderDark,
        'o' => cBoulderDark,
        _   => (Color?)null,
    });

    // ========================================================================
    // LAYER 6: LAVA REFLECTION on the water (under the lava-meets-sea point)
    // Warm orange tint a few pixels below the contact, fading with distance.
    // ========================================================================
    foreach (var sx in new[] { 18, 19, 20, 21 }) Put(sx, 20, cLavaReflect);
    Put(20, 21, cLavaReflect);
    Put(19, 22, cLavaReflect);

    // ========================================================================
    // LAYER 7: STEAM where lava meets water (animated puffs)
    // ========================================================================
    {
        var contactX = 20;
        var contactY = 19;
        for (var i = 0; i < 3; i++)
        {
            var phase = i * 0.6;
            var lifetime = 2.0;
            var age = (t + phase) % lifetime;
            var rise = age * 1.4;
            var drift = Math.Sin(age * 2.5 + i * 1.7) * 1.3;
            var x = contactX + (int)Math.Round(drift);
            var y = contactY - (int)Math.Round(rise);
            if (y >= 0 && y < contactY)
            {
                var fade = age / lifetime;
                if (fade < 0.4) Put(x, y, cSteamMid);
                else if (fade < 0.85) Put(x, y, cSteamHi);
            }
        }
    }

    // ========================================================================
    // LAYER 8: PULSING CRATER GLOW
    // The crater opening pulses between core-yellow and glow-orange.
    // ========================================================================
    {
        var pulse = 0.5 + 0.5 * Math.Sin(t * 2.4);  // 0..1
        // Inner core (always brightest)
        Put(craterX,     craterY,     cLavaCore);
        // Surrounding glow that brightens with pulse
        if (pulse > 0.3)
        {
            Put(craterX - 1, craterY,     cLavaGlow);
            Put(craterX + 1, craterY,     cLavaGlow);
        }
        if (pulse > 0.6)
        {
            Put(craterX,     craterY - 1, cLavaGlow);   // upward flare
            Put(craterX - 1, craterY - 1, cLavaDark);
            Put(craterX + 1, craterY - 1, cLavaDark);
        }
        if (pulse > 0.85)
        {
            Put(craterX,     craterY - 2, cLavaCore);   // bright tip
        }
    }

    // ========================================================================
    // LAYER 9: ANIMATED LAVA SPARKS (parabolic arcs out of the crater)
    // Each spark has its own phase, launches up-and-out, falls back.
    // ========================================================================
    {
        var sparkCount = 6;
        for (var i = 0; i < sparkCount; i++)
        {
            var phase = i * 0.37;                        // offset per spark
            var lifetime = 1.4;
            var age = (t + phase) % lifetime;
            var dirSeed = i * 1.7 + 0.3;
            var vx = Math.Sin(dirSeed) * 2.6;            // horizontal velocity
            var vy0 = -4.5 - (i % 2) * 0.6;              // upward initial velocity
            var g = 5.5;                                  // gravity
            var px = craterX + vx * age;
            var py = craterY + vy0 * age + 0.5 * g * age * age;
            var x = (int)Math.Round(px);
            var y = (int)Math.Round(py);
            if (y < craterY && y >= 0 && x >= 0 && x < canvasW)
            {
                var lifeF = age / lifetime;
                Color c = lifeF < 0.35 ? cLavaCore
                        : lifeF < 0.75 ? cLavaGlow
                        :                cLavaDark;
                Put(x, y, c);
            }
        }
    }

    // ========================================================================
    // LAYER 10: ANIMATED SMOKE PLUME (rises from crater, drifts, fades up)
    // ========================================================================
    {
        var smokeCount = 8;
        for (var i = 0; i < smokeCount; i++)
        {
            var phase = i * 0.55;
            var lifetime = 5.5;
            var age = (t + phase) % lifetime;
            var rise = age * 1.5;
            var drift = Math.Sin(age * 1.1 + i * 0.9) * 2.2 + age * 0.4;  // wind-drift right
            var x = craterX + (int)Math.Round(drift);
            var y = (craterY - 1) - (int)Math.Round(rise);
            if (y >= 0 && y < craterY && x >= 0 && x < canvasW)
            {
                var fade = age / lifetime;   // 0 → 1
                Color c;
                if (fade < 0.25) c = cSmokeHot;
                else if (fade < 0.55) c = cSmokeMid;
                else if (fade < 0.85) c = cSmokeLight;
                else continue;  // dispersed, skip
                Put(x, y, c);
            }
        }
    }

    // ========================================================================
    // LAYER 11: FOAM RING at waterline
    // ========================================================================
    foreach (var fx in new[] { 1, 5, 10, 14, 17 })   Put(fx, 21, cWaterFoam);
    foreach (var fx in new[] { 3, 8, 12, 16 })       Put(fx, 22, cWaterFoam);
};
