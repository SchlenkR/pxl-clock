// ---
// app: IslandShowcase20
// displayName: Island Showcase 20
// author: Ronald Schlenker
// description: Twenty island design drafts across three themes (South Pacific, Mediterranean, Nordic). Cycles one per second by default. Set selectedIsland 1..20 at the top to lock a specific design. Includes far-distance miniatures and shapes beyond simple ellipses (atolls, double-ellipses, vertical cliff wedges).
// appType: Scene
// duration: 30
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

// ============================================================================
// SELECTOR
// ============================================================================
// Set to 1..20 to lock that island design. Set to null to cycle one per second.
int? selectedIsland = null;

const int canvasW = 24;
const int canvasH = 24;

// ============================================================================
// SHARED PALETTE
// ============================================================================
// Sky + water (shared by all designs)
var cSkyTop      = Color.FromRgb(0.62, 0.88, 1.00);
var cSkyHorizon  = Color.FromRgb(0.85, 0.95, 1.00);
var cWaterBright = Color.FromRgb(0.30, 0.78, 0.74);
var cWaterDeep   = Color.FromRgb(0.10, 0.50, 0.55);
var cWaterFoam   = Color.FromRgb(0.95, 0.99, 1.00);
var cLagoon      = Color.FromRgb(0.55, 0.92, 0.92);

// Sand (5-shade)
var cSandHi      = Color.FromRgb(1.00, 0.94, 0.72);
var cSandBright  = Color.FromRgb(0.96, 0.84, 0.55);
var cSandMid     = Color.FromRgb(0.78, 0.62, 0.32);
var cSandDark    = Color.FromRgb(0.50, 0.36, 0.20);
var cSandWet     = Color.FromRgb(0.32, 0.22, 0.14);

// Grass (4-shade) + golden rim
var cGrassHi     = Color.FromRgb(0.62, 0.92, 0.36);
var cGrassBright = Color.FromRgb(0.42, 0.78, 0.28);
var cGrassMid    = Color.FromRgb(0.26, 0.58, 0.22);
var cGrassDark   = Color.FromRgb(0.14, 0.34, 0.16);
var cGoldRim     = Color.FromRgb(0.85, 0.62, 0.22);

// Palm
var cTrunkHi     = Color.FromRgb(0.65, 0.42, 0.22);
var cTrunkBase   = Color.FromRgb(0.46, 0.28, 0.14);
var cTrunkMid    = Color.FromRgb(0.30, 0.18, 0.08);
var cTrunkDark   = Color.FromRgb(0.16, 0.10, 0.05);
var cFrondHi     = Color.FromRgb(0.62, 0.92, 0.36);
var cFrondMid    = Color.FromRgb(0.36, 0.74, 0.30);
var cFrondDark   = Color.FromRgb(0.20, 0.48, 0.20);
var cCoco        = Color.FromRgb(0.42, 0.24, 0.10);

// Volcanic rock + glow
var cLavaRockHi  = Color.FromRgb(0.45, 0.38, 0.34);
var cLavaRock    = Color.FromRgb(0.28, 0.22, 0.20);
var cLavaRockDk  = Color.FromRgb(0.14, 0.10, 0.08);
var cLavaGlow    = Color.FromRgb(1.00, 0.55, 0.10);
var cLavaCore    = Color.FromRgb(1.00, 0.85, 0.40);
var cSmoke       = Color.FromRgb(0.70, 0.70, 0.74);
var cSmokeDark   = Color.FromRgb(0.45, 0.45, 0.50);

// Driftwood
var cWoodHi      = Color.FromRgb(0.72, 0.58, 0.42);
var cWoodMid     = Color.FromRgb(0.50, 0.38, 0.26);
var cWoodDk      = Color.FromRgb(0.28, 0.20, 0.14);

// Mediterranean white limestone (5-shade)
var cStoneHi     = Color.FromRgb(1.00, 0.98, 0.92);
var cStoneBright = Color.FromRgb(0.92, 0.88, 0.78);
var cStoneMid    = Color.FromRgb(0.72, 0.68, 0.60);
var cStoneDark   = Color.FromRgb(0.42, 0.38, 0.32);
var cStoneEdge   = Color.FromRgb(0.22, 0.18, 0.14);

// Mediterranean accents
var cMedDome     = Color.FromRgb(0.18, 0.46, 0.78);
var cMedDomeHi   = Color.FromRgb(0.42, 0.68, 0.92);
var cMedRoof     = Color.FromRgb(0.72, 0.30, 0.20);
var cMedRoofHi   = Color.FromRgb(0.92, 0.50, 0.28);
var cOliveHi     = Color.FromRgb(0.55, 0.68, 0.28);
var cOliveDark   = Color.FromRgb(0.30, 0.42, 0.18);
var cOliveTrunk  = Color.FromRgb(0.42, 0.34, 0.22);
var cBoatHull    = Color.FromRgb(0.92, 0.45, 0.18);

// Nordic granite (4-shade)
var cGraniteHi   = Color.FromRgb(0.66, 0.62, 0.62);
var cGraniteBase = Color.FromRgb(0.46, 0.42, 0.42);
var cGraniteMid  = Color.FromRgb(0.30, 0.28, 0.30);
var cGraniteDark = Color.FromRgb(0.16, 0.14, 0.16);

// Nordic vegetation + structures
var cPineHi      = Color.FromRgb(0.18, 0.42, 0.24);
var cPineDark    = Color.FromRgb(0.06, 0.22, 0.14);
var cPineTrunk   = Color.FromRgb(0.34, 0.22, 0.14);
var cLightWhite  = Color.FromRgb(0.94, 0.94, 0.92);
var cLightRed    = Color.FromRgb(0.84, 0.18, 0.16);
var cLightGlow   = Color.FromRgb(1.00, 0.94, 0.55);
var cWoodRed     = Color.FromRgb(0.66, 0.18, 0.16);
var cWoodYellow  = Color.FromRgb(0.92, 0.78, 0.32);
var cWoodBlue    = Color.FromRgb(0.22, 0.34, 0.62);
var cWoodWhite   = Color.FromRgb(0.92, 0.90, 0.84);
var cRoofDark    = Color.FromRgb(0.22, 0.18, 0.16);
var cSnowHi      = Color.FromRgb(0.98, 0.98, 1.00);
var cSnowMid     = Color.FromRgb(0.78, 0.84, 0.92);
var cSnowShadow  = Color.FromRgb(0.55, 0.62, 0.74);

// ============================================================================
// SCENE
// ============================================================================
var scene = (RasterSurface ctx) =>
{
    void Put(int x, int y, Color c)
    {
        if (x >= 0 && x < canvasW && y >= 0 && y < canvasH) ctx.SetPixel(x, y, c);
    }

    // ---- LAYER 0: sky + water gradient (shared by all designs)
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

    // Sprite renderer: each row is a string with one char per pixel.
    // ' ' (space) and '.' are skipped (transparent). yOffset shifts whole sprite.
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

    // Common sand mapper for tropical islands (H/s/m/M/d/o convention)
    Color? SandMap(char ch) => ch switch
    {
        'H' => cSandHi,
        's' => cSandBright,
        'm' => cSandMid,
        'M' => cSandMid,
        'd' => cSandDark,
        'o' => cSandWet,
        _   => (Color?)null,
    };

    // Common grass mapper
    Color? GrassMap(char ch) => ch switch
    {
        'h' => cGrassHi,
        'g' => cGrassBright,
        'G' => cGrassMid,
        'k' => cGrassDark,
        _   => (Color?)null,
    };

    // Common palm crown mapper
    Color? CrownMap(char ch) => ch switch
    {
        'v' => cFrondHi,
        'V' => cFrondMid,
        'L' => cFrondDark,
        'C' => cCoco,
        _   => (Color?)null,
    };

    // Common foam ring helper
    void FoamRing(int[] xsTop, int yTop, int[] xsBot, int yBot)
    {
        foreach (var fx in xsTop) Put(fx, yTop, cWaterFoam);
        foreach (var fx in xsBot) Put(fx, yBot, cWaterFoam);
    }

    // Trunk helper (3-px cylinder shading)
    void Trunk(int col, int yTop, int yBot)
    {
        for (var sy = yTop; sy <= yBot; sy++)
        {
            Put(col - 1, sy, cTrunkHi);
            Put(col,     sy, cTrunkBase);
            Put(col + 1, sy, cTrunkMid);
        }
        Put(col + 2, yBot - 1, cTrunkDark);
        Put(col + 2, yBot,     cTrunkDark);
    }

    // ========================================================================
    // 20 ISLAND DESIGNS
    // ========================================================================

    // ---- 01: South - Classic palm isle (mid-size, single palm) -------------
    void Island01()
    {
        Sprite(new (string, int)[]
        {
            ("         oHHHo          ", 14),
            ("       oHsssssMo        ", 15),
            ("     oHsssssssMMMo      ", 16),
            ("    oHsssssmmmMMMMo     ", 17),
            ("     oHmmmmmmddMMo      ", 18),
            ("      ommmddddo         ", 19),
            ("        oooooo          ", 20),
        }, SandMap);
        Sprite(new (string, int)[]
        {
            ("         gggg           ", 12),
            ("        ghggGk          ", 13),
            ("         ggGk           ", 14),
        }, GrassMap);
        foreach (var rx in new[] { 9, 10, 11, 12, 13 }) Put(rx, 14, cGoldRim);
        Trunk(12, 7, 13);
        Sprite(new (string, int)[]
        {
            ("        vvvVL           ",  3),
            ("       vvCvvvVL         ",  4),
            ("      vvvvvvvVVL        ",  5),
            ("       vvvvvVL          ",  6),
        }, CrownMap);
        FoamRing(new[] { 8, 11, 14, 17 }, 21, new[] { 9, 13, 16 }, 22);
    }

    // ---- 02: South - Atoll with central lagoon -----------------------------
    void Island02()
    {
        // Outer sand ring
        Sprite(new (string, int)[]
        {
            ("       oHsssssMo        ", 14),
            ("     oHsssLLLsssMo      ", 15),
            ("    oHssLLLLLLLssMo     ", 16),
            ("    oHssLLLLLLLssMo     ", 17),
            ("    oHmmLLLLLLLmmMo     ", 18),
            ("     oHmmLLLLLmmMo      ", 19),
            ("       ommmmmmmo        ", 20),
        }, ch => ch switch
        {
            'L' => cLagoon,
            _   => SandMap(ch),
        });
        // Foam outside, hint of foam where lagoon meets ring
        FoamRing(new[] { 5, 9, 14, 18 }, 21, new[] { 7, 11, 16 }, 22);
        Put(9,  16, cWaterFoam);
        Put(14, 17, cWaterFoam);
    }

    // ---- 03: South - Wide flat sandbar with twin palms ---------------------
    void Island03()
    {
        Sprite(new (string, int)[]
        {
            ("       oHHHHHHHHo       ", 15),
            ("   oHHsssssssssssssMMo  ", 16),
            (" oHHsssssssssssssssMMMMo", 17),
            (" oHsssssmmmmmmmmmMMMMMMo", 18),
            ("   ommmmmmmmmmddddddo   ", 19),
            ("        oooooooo        ", 20),
        }, SandMap);
        Sprite(new (string, int)[]
        {
            ("      ggg     gggg      ", 14),
            ("    ghgggk   ghgggGk    ", 15),
            ("     ggGk    ggGGk      ", 16),
        }, GrassMap);
        // Two short palms
        Trunk(7,  9, 14);
        Trunk(16, 9, 14);
        Sprite(new (string, int)[]
        {
            ("     vvVL     vvVL      ",  5),
            ("    vvCvVL   vvCvVL     ",  6),
            ("     vvvL     vvvL      ",  7),
        }, CrownMap);
        FoamRing(new[] { 3, 7, 11, 15, 19 }, 21, new[] { 5, 9, 13, 17 }, 22);
    }

    // ---- 04: South - Volcanic island with lava + smoke ---------------------
    void Island04()
    {
        // Smoke wisps
        Sprite(new (string, int)[]
        {
            ("           ss           ",  1),
            ("          sSSs          ",  2),
            ("          ssSs          ",  3),
            ("           ss           ",  4),
        }, ch => ch switch
        {
            's' => cSmoke,
            'S' => cSmokeDark,
            _   => (Color?)null,
        });
        // Volcano cone with glow at crater
        Sprite(new (string, int)[]
        {
            ("          GcG           ",  6),
            ("         dHGGdd         ",  7),
            ("        dHbbGGdd        ",  8),
            ("       dHbbbbbbdd       ",  9),
            ("      dHbbbbbbbbdd      ", 10),
            ("     dHbbbbbbbbbbdd     ", 11),
            ("    dHbbbbbbbbbbbbdd    ", 12),
            ("   dHHbbbbbbbbbbbbHdd   ", 13),
            ("  dHHbbbbbbbbbbbbbbHHdd ", 14),
            (" dHbbbbbbbbbbbbbbbbbbHHd", 15),
            (" oodbbbbbbbbbbbbbbbbboo ", 16),
            ("   ooooooooooooooooo    ", 17),
        }, ch => ch switch
        {
            'H' => cLavaRockHi,
            'b' => cLavaRock,
            'd' => cLavaRockDk,
            'G' => cLavaGlow,
            'c' => cLavaCore,
            'o' => cSandWet,
            _   => (Color?)null,
        });
        FoamRing(new[] { 2, 6, 11, 16, 21 }, 18, new[] { 4, 9, 14, 19 }, 19);
    }

    // ---- 05: South - Long driftwood spit (narrow elongated sandbar) --------
    void Island05()
    {
        Sprite(new (string, int)[]
        {
            (" oHHHHsssssssssssMMMMMo ", 17),
            ("  oHsssssmmmmmmmMMMMMo  ", 18),
            ("   ooooooooooooooooo    ", 19),
        }, SandMap);
        // Driftwood log lying on sand
        Sprite(new (string, int)[]
        {
            ("      W W W             ", 16),
            ("      WwwwwwwwD         ", 17),
        }, ch => ch switch
        {
            'W' => cWoodHi,
            'w' => cWoodMid,
            'D' => cWoodDk,
            _   => (Color?)null,
        });
        FoamRing(new[] { 1, 6, 11, 16, 22 }, 20, new[] { 3, 8, 13, 18 }, 21);
    }

    // ---- 06: South - Tiny far-distance palm islet --------------------------
    void Island06()
    {
        // Sits HIGH near horizon to read as "far away"
        Sprite(new (string, int)[]
        {
            ("           v            ",  9),
            ("          vCv           ", 10),
            ("           v            ", 11),
        }, CrownMap);
        Put(11, 11, cTrunkHi);
        Put(11, 12, cTrunkBase);
        Sprite(new (string, int)[]
        {
            ("          ooo           ", 13),
            ("         oHsMo          ", 14),
            ("          ooo           ", 15),
        }, SandMap);
        Put(8,  16, cWaterFoam);
        Put(13, 16, cWaterFoam);
    }

    // ---- 07: South - Offset double-ellipse (peanut-shape twin islet) -------
    void Island07()
    {
        // Left ellipse around col 6, right (smaller) around col 16
        Sprite(new (string, int)[]
        {
            ("    oHHHo               ", 15),
            ("  oHssssssMo    oHHHo   ", 16),
            (" oHsssssssssMooHsssMo   ", 17),
            (" oHsssmmmmddMMosmmmMo   ", 18),
            ("  ommmmmddddo  ommmo    ", 19),
            ("    ooooooo     ooo     ", 20),
        }, SandMap);
        // Tiny grass on the bigger left mound
        Sprite(new (string, int)[]
        {
            ("    gggg                ", 14),
            ("    ggGk                ", 15),
        }, GrassMap);
        FoamRing(new[] { 1, 5, 9, 14, 19 }, 21, new[] { 3, 7, 12, 17 }, 22);
    }

    // ---- 08: Med - Akropolis temple on white-stone hill --------------------
    void Island08()
    {
        // Pediment + entablature (very top)
        Sprite(new (string, int)[]
        {
            ("        SSSSSSSS        ",  6),
            ("       SHHHHHHHHS       ",  7),
            ("       SHHHHHHHHS       ",  8),
            ("       SHHHHHHHHS       ",  9),
        }, ch => ch switch
        {
            'H' => cStoneHi,
            'S' => cStoneBright,
            _   => (Color?)null,
        });
        // Columns (5 of them, 1px wide, with capitals)
        for (var sy = 10; sy <= 14; sy++)
        {
            foreach (var cx in new[] { 8, 10, 12, 14, 16 })
                Put(cx, sy, sy == 10 ? cStoneHi : cStoneBright);
        }
        // Stylobate (base step)
        for (var sx = 7; sx <= 17; sx++) Put(sx, 15, cStoneMid);
        // Rocky white-stone hill base
        Sprite(new (string, int)[]
        {
            ("    EHSSSSSSSSSSSSME    ", 16),
            ("   EHSSSSSSSSSSSSSSME   ", 17),
            ("   ESSSSSMMMMMMMMSSME   ", 18),
            ("    EMMMMMMDDDDMMME     ", 19),
            ("     EEEEEEEEEEEE       ", 20),
        }, ch => ch switch
        {
            'H' => cStoneHi,
            'S' => cStoneBright,
            'M' => cStoneMid,
            'D' => cStoneDark,
            'E' => cStoneEdge,
            _   => (Color?)null,
        });
        FoamRing(new[] { 4, 8, 12, 16, 20 }, 21, new[] { 6, 11, 15, 19 }, 22);
    }

    // ---- 09: Med - Santorini cluster (white houses + blue domes) -----------
    void Island09()
    {
        // Cliff base (white limestone)
        Sprite(new (string, int)[]
        {
            ("     EHSSSSSSSSSSME     ", 14),
            ("    EHSSSSSSSSSSSSME    ", 15),
            ("   EHSSSSSSSMMMMMMMME   ", 16),
            ("   ESSSSMMMMMMMDDDDME   ", 17),
            ("    EMMMMMMDDDDDDDME    ", 18),
            ("     EEEEEEEEEEEE       ", 19),
        }, ch => ch switch
        {
            'H' => cStoneHi,
            'S' => cStoneBright,
            'M' => cStoneMid,
            'D' => cStoneDark,
            'E' => cStoneEdge,
            _   => (Color?)null,
        });
        // Two blue domes
        Put(8,  10, cMedDomeHi); Put(9, 10, cMedDome);
        Put(7,  11, cMedDomeHi); Put(8, 11, cMedDome); Put(9, 11, cMedDome); Put(10, 11, cMedDome);
        Put(15, 9, cMedDomeHi); Put(16, 9, cMedDome);
        Put(14, 10, cMedDomeHi); Put(15, 10, cMedDome); Put(16, 10, cMedDome); Put(17, 10, cMedDome);
        // White cube houses under the domes + standalone cube on right
        for (var sx = 7; sx <= 10; sx++) { Put(sx, 12, cStoneHi); Put(sx, 13, cStoneBright); }
        Put(7, 13, cStoneEdge);
        for (var sx = 14; sx <= 17; sx++) { Put(sx, 11, cStoneHi); Put(sx, 12, cStoneBright); Put(sx, 13, cStoneBright); }
        // Standalone little house left
        Put(4, 12, cStoneHi); Put(5, 12, cStoneHi);
        Put(4, 13, cStoneBright); Put(5, 13, cStoneBright);
        Put(4, 13, cStoneEdge);
        FoamRing(new[] { 4, 8, 12, 16, 20 }, 20, new[] { 6, 11, 15, 19 }, 21);
    }

    // ---- 10: Med - Lone whitewashed chapel with bell tower -----------------
    void Island10()
    {
        // Base hill (limestone, smaller)
        Sprite(new (string, int)[]
        {
            ("       oHsssssssMo      ", 15),
            ("     oHHsssssssssMMo    ", 16),
            ("     oHmmmmmmmmddMMo    ", 17),
            ("      ommmmmmddddo      ", 18),
            ("        ooooooo         ", 19),
        }, SandMap);
        // Chapel body (white cube)
        for (var sy = 10; sy <= 13; sy++)
        for (var sx = 9; sx <= 14; sx++) Put(sx, sy, sx == 9 ? cStoneEdge : cStoneBright);
        // Top edge highlight
        for (var sx = 9; sx <= 14; sx++) Put(sx, 10, cStoneHi);
        // Bell tower (small turret on left side)
        Put(10, 6, cStoneHi);
        Put(10, 7, cStoneBright);
        Put(10, 8, cStoneBright);
        Put(10, 9, cStoneBright);
        Put(11, 7, cStoneBright);
        Put(11, 8, cStoneBright);
        Put(11, 9, cStoneBright);
        Put(11, 6, cStoneBright);
        // Cross atop bell tower
        Put(10, 5, cStoneHi);
        Put(11, 5, cStoneHi);
        // Blue door
        Put(11, 12, cMedDome); Put(11, 13, cMedDome);
        Put(12, 12, cMedDome); Put(12, 13, cMedDome);
        FoamRing(new[] { 5, 9, 13, 17 }, 20, new[] { 7, 12, 16 }, 21);
    }

    // ---- 11: Med - Olive hillside (terraced) -------------------------------
    void Island11()
    {
        // Stone+earth hill
        Sprite(new (string, int)[]
        {
            ("        EHSSSSSSME      ", 13),
            ("      EHSSSSSSSSSSME    ", 14),
            ("     EHSSSSSSSSSSSSME   ", 15),
            ("    EHSSSSSSMMMMMMMSME  ", 16),
            ("    ESSSMMMMMMMDDDDSME  ", 17),
            ("     EMMMMMMDDDDDDDME   ", 18),
            ("       EEEEEEEEEEE      ", 19),
        }, ch => ch switch
        {
            'H' => cStoneHi,
            'S' => cStoneBright,
            'M' => cStoneMid,
            'D' => cStoneDark,
            'E' => cStoneEdge,
            _   => (Color?)null,
        });
        // Olive trees (small clumps on hill)
        void Olive(int cx, int cy)
        {
            Put(cx,     cy,     cOliveHi);
            Put(cx - 1, cy,     cOliveDark);
            Put(cx + 1, cy,     cOliveDark);
            Put(cx,     cy - 1, cOliveHi);
            Put(cx,     cy + 1, cOliveTrunk);
        }
        Olive(9,  12);
        Olive(13, 11);
        Olive(17, 12);
        Olive(11, 14);
        FoamRing(new[] { 4, 8, 12, 16, 20 }, 20, new[] { 6, 10, 14, 18 }, 21);
    }

    // ---- 12: Med - Ruined columns (broken pillars on grass) ----------------
    void Island12()
    {
        // Grass-topped stone hill
        Sprite(new (string, int)[]
        {
            ("       oHsssssssMo      ", 15),
            ("     oHsssssssssssMo    ", 16),
            ("    oHsssmmmmmmmmMMMo   ", 17),
            ("     ommmmmddddddMMo    ", 18),
            ("       oooooooooo       ", 19),
        }, SandMap);
        Sprite(new (string, int)[]
        {
            ("         ggggg          ", 13),
            ("        ghgggGk         ", 14),
            ("         gGGGk          ", 15),
        }, GrassMap);
        // Standing column (left, 3 tall)
        Put(9,  9,  cStoneHi);
        Put(9,  10, cStoneBright);
        Put(9,  11, cStoneBright);
        Put(9,  12, cStoneMid);
        Put(8,  8,  cStoneHi); Put(9, 8, cStoneHi); Put(10, 8, cStoneHi);
        // Fallen column (lying on grass)
        Put(11, 14, cStoneHi); Put(12, 14, cStoneBright); Put(13, 14, cStoneBright); Put(14, 14, cStoneMid);
        // Broken stub (right)
        Put(16, 12, cStoneBright);
        Put(16, 13, cStoneMid);
        FoamRing(new[] { 4, 8, 12, 16, 20 }, 20, new[] { 6, 10, 14, 18 }, 21);
    }

    // ---- 13: Med - Small harbor with stone mole + boat ---------------------
    void Island13()
    {
        // Curved stone harbor wall (open to upper-right) embracing small water
        Sprite(new (string, int)[]
        {
            ("       EHHHHHHHE        ", 13),
            ("     EHSSSSSSSSSE       ", 14),
            ("    EHSS      SSSE      ", 15),
            ("    EHSS      SSSE      ", 16),
            ("    EHSS    SSSSSE      ", 17),
            ("    EHSSSSSSSSSSE       ", 18),
            ("     EMMMMMMMMME        ", 19),
        }, ch => ch switch
        {
            'H' => cStoneHi,
            'S' => cStoneBright,
            'M' => cStoneMid,
            'E' => cStoneEdge,
            _   => (Color?)null,
        });
        // Inner harbor water (lagoon tone)
        for (var sx = 8; sx <= 13; sx++)
        {
            Put(sx, 15, cLagoon);
            Put(sx, 16, cLagoon);
        }
        for (var sx = 8; sx <= 11; sx++) Put(sx, 17, cLagoon);
        // Small orange-hulled boat in harbor
        Put(10, 16, cBoatHull); Put(11, 16, cBoatHull); Put(12, 16, cBoatHull);
        Put(11, 15, cStoneHi);  // tiny mast/sail flash
        FoamRing(new[] { 4, 8, 16, 20 }, 20, new[] { 6, 14, 18 }, 21);
    }

    // ---- 14: Med - Lone pillar on rocky outcrop (small/distant) -----------
    void Island14()
    {
        // Small rocky base
        Sprite(new (string, int)[]
        {
            ("        oHHHHo          ", 16),
            ("      oHHsssssMMo       ", 17),
            ("      oHmmmmmmMMo       ", 18),
            ("        oooooo          ", 19),
        }, ch => ch switch
        {
            'H' => cGraniteHi,
            's' => cGraniteBase,
            'm' => cGraniteMid,
            'M' => cGraniteMid,
            'o' => cGraniteDark,
            _   => (Color?)null,
        });
        // Single white pillar
        Put(11, 10, cStoneHi); Put(12, 10, cStoneHi);
        Put(11, 11, cStoneBright);
        Put(11, 12, cStoneBright);
        Put(11, 13, cStoneBright);
        Put(11, 14, cStoneBright);
        Put(11, 15, cStoneMid);
        Put(10, 15, cStoneHi); Put(12, 15, cStoneMid);
        FoamRing(new[] { 5, 9, 13, 17 }, 20, new[] { 7, 11, 15 }, 21);
    }

    // ---- 15: Nordic - Striped lighthouse on granite cliff ------------------
    void Island15()
    {
        // Granite base
        Sprite(new (string, int)[]
        {
            ("      oHHHHHHHo         ", 13),
            ("    oHHbbbbbbbBBo       ", 14),
            ("   oHbbbbbbbbbbBBo      ", 15),
            ("   oHbbbbbbbbbbBBo      ", 16),
            ("    oHmmmmmmmmBBo       ", 17),
            ("     oommmmmddo         ", 18),
            ("       oooooo           ", 19),
        }, ch => ch switch
        {
            'H' => cGraniteHi,
            'b' => cGraniteBase,
            'B' => cGraniteMid,
            'm' => cGraniteMid,
            'd' => cGraniteDark,
            'o' => cGraniteDark,
            _   => (Color?)null,
        });
        // Lighthouse tower (stripes red/white) center col 11-12
        Put(11, 4, cLightGlow); Put(12, 4, cLightGlow);  // glow halo
        Put(11, 5, cLightGlow); Put(12, 5, cLightGlow);  // light room
        Put(10, 6, cLightWhite); Put(11, 6, cLightWhite); Put(12, 6, cLightWhite); Put(13, 6, cLightWhite);  // cap
        // Stripes (alternating)
        for (var sy = 7; sy <= 12; sy++)
        {
            var stripe = (sy - 7) % 2 == 0 ? cLightRed : cLightWhite;
            Put(11, sy, stripe);
            Put(12, sy, stripe);
        }
        // Wider base
        Put(10, 13, cLightWhite); Put(11, 13, cLightWhite); Put(12, 13, cLightWhite); Put(13, 13, cLightWhite);
        FoamRing(new[] { 4, 8, 12, 16, 20 }, 20, new[] { 6, 11, 15, 19 }, 21);
    }

    // ---- 16: Nordic - Skerry with colorful wooden houses -------------------
    void Island16()
    {
        // Granite skerry
        Sprite(new (string, int)[]
        {
            ("    oHHHHHHHHHHHHo      ", 14),
            ("  oHHbbbbbbbbbbbBBo     ", 15),
            ("  oHbbbbbbbbbbbbBBo     ", 16),
            ("   oHmmmmmmmmmmBBo      ", 17),
            ("    oommmmmdddBo        ", 18),
            ("      oooooooo          ", 19),
        }, ch => ch switch
        {
            'H' => cGraniteHi,
            'b' => cGraniteBase,
            'B' => cGraniteMid,
            'm' => cGraniteMid,
            'd' => cGraniteDark,
            'o' => cGraniteDark,
            _   => (Color?)null,
        });
        // Three small wooden houses (red, yellow, blue)
        // Red house
        Put(5, 11, cRoofDark); Put(6, 11, cRoofDark); Put(7, 11, cRoofDark);
        Put(5, 12, cWoodRed); Put(6, 12, cWoodRed); Put(7, 12, cWoodRed);
        Put(5, 13, cWoodRed); Put(6, 13, cWoodRed); Put(7, 13, cWoodRed);
        // Yellow house (taller)
        Put(10, 10, cRoofDark); Put(11, 10, cRoofDark); Put(12, 10, cRoofDark);
        Put(10, 11, cWoodYellow); Put(11, 11, cWoodYellow); Put(12, 11, cWoodYellow);
        Put(10, 12, cWoodYellow); Put(11, 12, cWoodYellow); Put(12, 12, cWoodYellow);
        Put(10, 13, cWoodYellow); Put(11, 13, cWoodYellow); Put(12, 13, cWoodYellow);
        // Blue house
        Put(15, 11, cRoofDark); Put(16, 11, cRoofDark); Put(17, 11, cRoofDark);
        Put(15, 12, cWoodBlue); Put(16, 12, cWoodBlue); Put(17, 12, cWoodBlue);
        Put(15, 13, cWoodBlue); Put(16, 13, cWoodBlue); Put(17, 13, cWoodBlue);
        // Tiny door highlights
        Put(11, 12, cWoodWhite);
        FoamRing(new[] { 3, 7, 11, 15, 19 }, 20, new[] { 5, 9, 13, 17 }, 21);
    }

    // ---- 17: Nordic - Granite rock with dark pines -------------------------
    void Island17()
    {
        Sprite(new (string, int)[]
        {
            ("       oHHHHHHo         ", 14),
            ("     oHbbbbbbbBBo       ", 15),
            ("    oHbbbbbbbbbBBo      ", 16),
            ("    oHmmmmmmmmmBBo      ", 17),
            ("     oommmmddddo        ", 18),
            ("       oooooo           ", 19),
        }, ch => ch switch
        {
            'H' => cGraniteHi,
            'b' => cGraniteBase,
            'B' => cGraniteMid,
            'm' => cGraniteMid,
            'd' => cGraniteDark,
            'o' => cGraniteDark,
            _   => (Color?)null,
        });
        // Three dark pines (triangle silhouettes)
        void Pine(int cx, int topY)
        {
            Put(cx, topY,     cPineHi);
            Put(cx - 1, topY + 1, cPineDark); Put(cx, topY + 1, cPineHi); Put(cx + 1, topY + 1, cPineDark);
            Put(cx - 1, topY + 2, cPineDark); Put(cx, topY + 2, cPineHi); Put(cx + 1, topY + 2, cPineDark);
            Put(cx, topY + 3, cPineTrunk);
        }
        Pine(8,  10);
        Pine(12, 9);
        Pine(16, 11);
        FoamRing(new[] { 4, 8, 12, 16, 20 }, 20, new[] { 6, 10, 14, 18 }, 21);
    }

    // ---- 18: Nordic - Vertical fjord cliff (wedge, NOT an ellipse) ---------
    void Island18()
    {
        // Tall narrow rock wedge: wide at base, sharp peak at top.
        // Completely different silhouette from the elliptical type.
        Sprite(new (string, int)[]
        {
            ("           HH           ",  3),
            ("          HHbb          ",  4),
            ("          HbbB          ",  5),
            ("         HHbbBB         ",  6),
            ("         HbbbBB         ",  7),
            ("        HHbbbbBB        ",  8),
            ("        HbbbbbBB        ",  9),
            ("       HHbbbbbbBB       ", 10),
            ("       HbbbbbbbBB       ", 11),
            ("      HHbbbbbbbbBB      ", 12),
            ("      HbbbbbbbbbBB      ", 13),
            ("     HHbbbbbbbbbbBB     ", 14),
            ("    HHbbbbbbbbbbbbBB    ", 15),
            ("   HHbbbbbbbbbbbbbbBB   ", 16),
            ("  HHbbbbbbbbbbbbbbbbBB  ", 17),
            (" oodbbbbbbbbbbbbbbbboo  ", 18),
            ("   ooooooooooooooooo    ", 19),
        }, ch => ch switch
        {
            'H' => cGraniteHi,
            'b' => cGraniteBase,
            'B' => cGraniteMid,
            'd' => cGraniteDark,
            'o' => cGraniteDark,
            _   => (Color?)null,
        });
        // A single tiny pine clinging to the side
        Put(15, 13, cPineHi); Put(16, 14, cPineDark);
        FoamRing(new[] { 2, 7, 12, 17, 21 }, 20, new[] { 4, 9, 14, 19 }, 21);
    }

    // ---- 19: Nordic - Red boathouse on stilts ------------------------------
    void Island19()
    {
        // Small granite ledge
        Sprite(new (string, int)[]
        {
            ("    oHHHHHHHHHo         ", 16),
            ("  oHbbbbbbbbbBBo        ", 17),
            ("  oommmmmmmddBo         ", 18),
            ("    oooooooo            ", 19),
        }, ch => ch switch
        {
            'H' => cGraniteHi,
            'b' => cGraniteBase,
            'B' => cGraniteMid,
            'm' => cGraniteMid,
            'd' => cGraniteDark,
            'o' => cGraniteDark,
            _   => (Color?)null,
        });
        // Falu-red boathouse (gable roof) on the rock
        // Roof
        Put(9,  8,  cRoofDark);
        Put(8,  9,  cRoofDark); Put(9,  9,  cRoofDark); Put(10, 9,  cRoofDark);
        Put(7, 10, cRoofDark); Put(8, 10, cRoofDark); Put(9, 10, cRoofDark); Put(10, 10, cRoofDark); Put(11, 10, cRoofDark);
        // Walls (red)
        for (var sy = 11; sy <= 14; sy++)
        for (var sx = 7;  sx <= 11; sx++)
            Put(sx, sy, sx == 7 ? cRoofDark : cWoodRed);
        // White door + window
        Put(9,  13, cWoodWhite);
        Put(9,  14, cWoodWhite);
        Put(11, 12, cWoodWhite);
        // Pier extending right into water
        for (var sx = 12; sx <= 17; sx++) Put(sx, 16, cWoodMid);
        Put(13, 17, cWoodDk); Put(15, 17, cWoodDk); Put(17, 17, cWoodDk);  // stilts
        FoamRing(new[] { 3, 8, 13, 17, 21 }, 20, new[] { 5, 10, 15, 19 }, 21);
    }

    // ---- 20: Nordic - Snow / ice island ------------------------------------
    void Island20()
    {
        // Snow-covered mound with dark rocks poking through
        Sprite(new (string, int)[]
        {
            ("        oHHHHo          ", 13),
            ("      oHsssssssMo       ", 14),
            ("    oHsssssssssssMMo    ", 15),
            ("   oHHssssssssssssMMMo  ", 16),
            ("   oHsssssmmmmmmMMMMo   ", 17),
            ("    oHmmmmmmmmddMMo     ", 18),
            ("      ommmddddo         ", 19),
            ("        oooooo          ", 20),
        }, ch => ch switch
        {
            'H' => cSnowHi,
            's' => cSnowMid,
            'm' => cSnowShadow,
            'M' => cSnowShadow,
            'd' => cGraniteMid,
            'o' => cGraniteDark,
            _   => (Color?)null,
        });
        // Dark rocks poking through the snow
        Put(8,  16, cGraniteDark); Put(9, 16, cGraniteMid);
        Put(15, 15, cGraniteDark); Put(16, 15, cGraniteMid); Put(15, 16, cGraniteMid);
        // A single dark pine (stunted)
        Put(12, 11, cPineHi);
        Put(11, 12, cPineDark); Put(12, 12, cPineHi); Put(13, 12, cPineDark);
        Put(12, 13, cPineTrunk);
        FoamRing(new[] { 4, 8, 12, 16, 20 }, 21, new[] { 6, 10, 14, 18 }, 22);
    }

    // ========================================================================
    // DISPATCH — pick island by selector or by elapsed-time second
    // ========================================================================
    var actions = new Action[]
    {
        Island01, Island02, Island03, Island04, Island05,
        Island06, Island07, Island08, Island09, Island10,
        Island11, Island12, Island13, Island14, Island15,
        Island16, Island17, Island18, Island19, Island20,
    };

    int idx;
    if (selectedIsland.HasValue && selectedIsland.Value >= 1 && selectedIsland.Value <= 20)
    {
        idx = selectedIsland.Value - 1;
    }
    else
    {
        var t = ctx.Elapsed.TotalSeconds;
        idx = ((int)Math.Floor(t)) % 20;
        if (idx < 0) idx += 20;
        // Tiny indicator dot at the top edge so cycle viewers can identify
        // which design is on screen (column = current index)
        Put(idx, 0, cWaterFoam);
    }

    actions[idx]();
};
