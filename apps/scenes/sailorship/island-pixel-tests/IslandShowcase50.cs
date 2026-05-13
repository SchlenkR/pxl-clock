// ---
// app: IslandShowcase50
// displayName: Island Showcase 50
// author: Ronald Schlenker
// description: Fifty further island design drafts beyond the original 20. Themes span Asia, Caribbean, Arctic, mythic, industrial wreckage and natural curios. Cycles one per second by default. Set selectedIsland 1..50 at the top to lock one design.
// appType: Scene
// duration: 60
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

// ============================================================================
// SELECTOR
// ============================================================================
// Set to 1..50 to lock that island design. Set to null to cycle one per second.
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
// NEW PALETTE COLORS (Asian, Caribbean, Exotic, Arctic, Fantasy, Industrial,
// Wreckage, Natural)
// ============================================================================

// --- East Asian ---
var cToriiRed    = Color.FromRgb(0.84, 0.20, 0.12);
var cToriiRedHi  = Color.FromRgb(0.96, 0.40, 0.22);
var cPagodaRoof  = Color.FromRgb(0.14, 0.18, 0.24);
var cBambooHi    = Color.FromRgb(0.82, 0.90, 0.42);
var cBambooMid   = Color.FromRgb(0.58, 0.74, 0.30);
var cBambooDark  = Color.FromRgb(0.34, 0.48, 0.18);

// --- Caribbean / Tropical ---
var cCoralPink   = Color.FromRgb(0.98, 0.60, 0.54);
var cCoralBright = Color.FromRgb(0.94, 0.78, 0.62);
var cCoralDark   = Color.FromRgb(0.65, 0.28, 0.25);
var cMangroveGn  = Color.FromRgb(0.28, 0.52, 0.28);
var cMangroveDk  = Color.FromRgb(0.10, 0.30, 0.16);
var cSeaGrape    = Color.FromRgb(0.40, 0.68, 0.34);
var cSeaGrapeDk  = Color.FromRgb(0.18, 0.42, 0.20);
var cGrapeBerry  = Color.FromRgb(0.42, 0.18, 0.48);

// --- Tropical Exotic ---
var cBaobabHi    = Color.FromRgb(0.78, 0.62, 0.44);
var cBaobabMid   = Color.FromRgb(0.56, 0.40, 0.28);
var cBaobabDark  = Color.FromRgb(0.34, 0.22, 0.16);
var cMoaiHi      = Color.FromRgb(0.62, 0.56, 0.52);
var cMoaiMid     = Color.FromRgb(0.38, 0.32, 0.30);
var cMoaiDark    = Color.FromRgb(0.18, 0.14, 0.12);
var cTikiWood    = Color.FromRgb(0.54, 0.32, 0.18);
var cTikiThatch  = Color.FromRgb(0.72, 0.60, 0.30);

// --- Arctic / Ice ---
var cIceHi       = Color.FromRgb(0.94, 0.98, 0.99);
var cIceMid      = Color.FromRgb(0.72, 0.86, 0.94);
var cIceShadow   = Color.FromRgb(0.46, 0.66, 0.82);
var cIceDeep     = Color.FromRgb(0.20, 0.40, 0.58);
var cSealDark    = Color.FromRgb(0.12, 0.14, 0.18);

// --- Fantasy / Mythic ---
var cCrystalHi   = Color.FromRgb(0.82, 0.96, 1.00);
var cCrystalMid  = Color.FromRgb(0.50, 0.82, 0.94);
var cCrystalGlow = Color.FromRgb(0.30, 0.94, 0.94);
var cAtlantisSt  = Color.FromRgb(0.62, 0.66, 0.58);
var cSeaweedGn   = Color.FromRgb(0.22, 0.54, 0.26);
var cSeaweedDk   = Color.FromRgb(0.10, 0.32, 0.16);
var cBoneHi      = Color.FromRgb(0.90, 0.86, 0.76);
var cBoneMid     = Color.FromRgb(0.68, 0.62, 0.52);
var cBoneDark    = Color.FromRgb(0.40, 0.34, 0.28);
var cDragonEye   = Color.FromRgb(0.96, 0.24, 0.10);
var cHutWood     = Color.FromRgb(0.38, 0.24, 0.16);
var cHutRoof     = Color.FromRgb(0.28, 0.16, 0.10);
var cWitchLight  = Color.FromRgb(0.72, 0.90, 0.30);
var cMushroomCap = Color.FromRgb(0.90, 0.44, 0.34);
var cMushroomDk  = Color.FromRgb(0.54, 0.22, 0.18);
var cMushroomSp  = Color.FromRgb(0.98, 0.88, 0.78);
var cStemBase    = Color.FromRgb(0.88, 0.82, 0.72);

// --- Industrial / Modern ---
var cRustHi      = Color.FromRgb(0.82, 0.42, 0.18);
var cRustMid     = Color.FromRgb(0.56, 0.26, 0.10);
var cRustDark    = Color.FromRgb(0.30, 0.14, 0.06);
var cContRed     = Color.FromRgb(0.78, 0.22, 0.14);
var cContBlue    = Color.FromRgb(0.22, 0.36, 0.64);
var cContYellow  = Color.FromRgb(0.88, 0.68, 0.18);
var cContGreen   = Color.FromRgb(0.22, 0.52, 0.30);
var cConcreteHi  = Color.FromRgb(0.78, 0.78, 0.76);
var cConcreteMid = Color.FromRgb(0.58, 0.58, 0.56);
var cConcreteDk  = Color.FromRgb(0.34, 0.34, 0.32);
var cTurbineWh   = Color.FromRgb(0.94, 0.96, 0.98);
var cTurbineGy   = Color.FromRgb(0.62, 0.64, 0.68);

// --- Wreckage / Decay ---
var cWreckSail   = Color.FromRgb(0.74, 0.70, 0.62);
var cPlaneBody   = Color.FromRgb(0.88, 0.84, 0.72);
var cPlaneWing   = Color.FromRgb(0.64, 0.60, 0.56);
var cPlaneStrip  = Color.FromRgb(0.28, 0.32, 0.56);

// --- Natural Curios ---
var cBasaltHi    = Color.FromRgb(0.48, 0.44, 0.42);
var cBasaltMid   = Color.FromRgb(0.30, 0.28, 0.28);
var cBasaltDark  = Color.FromRgb(0.14, 0.12, 0.12);
var cSulfurYel   = Color.FromRgb(0.98, 0.88, 0.22);
var cSulfurGlow  = Color.FromRgb(1.00, 0.95, 0.40);
var cSulfurDk    = Color.FromRgb(0.72, 0.60, 0.14);
var cMudHi       = Color.FromRgb(0.42, 0.34, 0.24);
var cMudMid      = Color.FromRgb(0.28, 0.22, 0.16);
var cMudDark     = Color.FromRgb(0.14, 0.10, 0.08);
var cBubbleMud   = Color.FromRgb(0.52, 0.42, 0.30);

// --- Additional thatch / texture ---
var cThatchHi    = Color.FromRgb(0.82, 0.70, 0.38);
var cThatchMid   = Color.FromRgb(0.62, 0.50, 0.26);
var cThatchDark  = Color.FromRgb(0.40, 0.30, 0.16);
var cBeachUmbr   = Color.FromRgb(0.94, 0.30, 0.24);
var cBeachUmbrHi = Color.FromRgb(1.00, 0.56, 0.32);
var cStoneRed    = Color.FromRgb(0.72, 0.28, 0.18);

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

    // Granite mapper (Nordic rock: H/hi, b/base, B/mid, m/mid, d/dark, o/outline)
    Color? GraniteMap(char ch) => ch switch
    {
        'H' => cGraniteHi,
        'b' => cGraniteBase,
        'B' => cGraniteMid,
        'm' => cGraniteMid,
        'd' => cGraniteDark,
        'o' => cGraniteDark,
        _   => (Color?)null,
    };

    // White stone mapper (Mediterranean: H/hi, S/bright, M/mid, D/dark, E/edge)
    Color? StoneMap(char ch) => ch switch
    {
        'H' => cStoneHi,
        'S' => cStoneBright,
        'M' => cStoneMid,
        'D' => cStoneDark,
        'E' => cStoneEdge,
        _   => (Color?)null,
    };

    // Snow mapper (H/hi, s/mid, m/shadow, d/granite-mid, o/granite-dark)
    Color? SnowMap(char ch) => ch switch
    {
        'H' => cSnowHi,
        's' => cSnowMid,
        'm' => cSnowShadow,
        'M' => cSnowShadow,
        'd' => cGraniteMid,
        'o' => cGraniteDark,
        _   => (Color?)null,
    };

    // Lava mapper (H/hi, b/rock, d/dark, G/glow, c/core, o/outline)
    Color? LavaMap(char ch) => ch switch
    {
        'H' => cLavaRockHi,
        'b' => cLavaRock,
        'd' => cLavaRockDk,
        'G' => cLavaGlow,
        'c' => cLavaCore,
        'o' => cSandWet,
        _   => (Color?)null,
    };

    // Bamboo mapper (H/hi, b/mid, d/dark)
    Color? BambooMap(char ch) => ch switch
    {
        'H' => cBambooHi,
        'b' => cBambooMid,
        'd' => cBambooDark,
        _   => (Color?)null,
    };

    // Ice mapper (H/hi, s/mid, m/shadow, d/deep)
    Color? IceMap(char ch) => ch switch
    {
        'H' => cIceHi,
        's' => cIceMid,
        'm' => cIceShadow,
        'd' => cIceDeep,
        'o' => cIceDeep,
        _   => (Color?)null,
    };

    // Coral mapper (H/hi, b/bright, d/dark, o/outline)
    Color? CoralMap(char ch) => ch switch
    {
        'H' => cCoralPink,
        'b' => cCoralBright,
        'd' => cCoralDark,
        'o' => cCoralDark,
        _   => (Color?)null,
    };

    // Basalt mapper (H/hi, b/mid, d/dark, o/dark outline)
    Color? BasaltMap(char ch) => ch switch
    {
        'H' => cBasaltHi,
        'b' => cBasaltMid,
        'd' => cBasaltDark,
        'o' => cBasaltDark,
        _   => (Color?)null,
    };

    // Moai stone mapper (H/hi, b/mid, d/dark)
    Color? MoaiMap(char ch) => ch switch
    {
        'H' => cMoaiHi,
        'b' => cMoaiMid,
        'd' => cMoaiDark,
        _   => (Color?)null,
    };

    // Concrete mapper (H/hi, b/mid, d/dark)
    Color? ConcreteMap(char ch) => ch switch
    {
        'H' => cConcreteHi,
        'b' => cConcreteMid,
        'd' => cConcreteDk,
        _   => (Color?)null,
    };

    // Common foam ring helper
    void FoamRing(int[] xsTop, int yTop, int[] xsBot, int yBot)
    {
        foreach (var fx in xsTop) Put(fx, yTop, cWaterFoam);
        foreach (var fx in xsBot) Put(fx, yBot, cWaterFoam);
    }

    // Trunk helper (3-px cylinder shading for palm trunks)
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

    // Generic column helper (3-px cylinder with custom palette, e.g. for pagoda/bamboo/basalt)
    void Cylinder(int col, int yTop, int yBot, Color cHi, Color cBase, Color cShad, Color? cAccent = null)
    {
        for (var sy = yTop; sy <= yBot; sy++)
        {
            Put(col - 1, sy, cHi);
            Put(col,     sy, cBase);
            Put(col + 1, sy, cShad);
        }
        if (cAccent.HasValue)
        {
            Put(col + 2, yBot - 1, cAccent.Value);
            Put(col + 2, yBot,     cAccent.Value);
        }
    }

    // ========================================================================
    // 50 ISLAND DESIGNS
    // ========================================================================

    // ========================================================================
    // GROUP A: EAST ASIAN (01–05)
    // ========================================================================

    // ---- 01: Japanese torii gate on granite rock ---------------------------
    void Island01()
    {
        // Granite base (elliptical, 14x6)
        Sprite(new (string, int)[]
        {
            ("         oHHHHo         ", 15),
            ("       oHbbbbbbBo       ", 16),
            ("     oHbbbbbbbbBBo      ", 17),
            ("     oHbbbbbbbbBBo      ", 18),
            ("      oommmmmmddo       ", 19),
            ("        oooooo          ", 20),
        }, GraniteMap);
        // Snow/moss cap on granite
        Sprite(new (string, int)[]
        {
            ("         hsss           ", 14),
            ("        hsssss          ", 15),
        }, ch => ch switch
        {
            'h' => cSnowHi,
            's' => cSnowMid,
            _   => (Color?)null,
        });
        // Vermilion torii: two pillars (cylinder shaded) + curved top beam
        Cylinder(9,  8, 15, cToriiRedHi, cToriiRed, cRoofDark);
        Cylinder(14, 8, 15, cToriiRedHi, cToriiRed, cRoofDark);
        // Top beam (curved lintel)
        for (var sx = 8; sx <= 15; sx++) Put(sx, 7, cToriiRedHi);
        for (var sx = 7; sx <= 16; sx++) Put(sx, 8, cToriiRed);
        // Small pine left
        Put(5, 13, cPineHi); Put(4, 14, cPineDark); Put(5, 14, cPineHi); Put(6, 14, cPineDark);
        Put(5, 15, cPineTrunk);
        // Cast shadow of torii on rock (right side)
        Put(15, 14, cGraniteDark); Put(16, 14, cGraniteDark); Put(15, 15, cGraniteDark);
        FoamRing(new[] { 5, 9, 13, 17 }, 21, new[] { 7, 11, 15 }, 22);
    }

    // ---- 02: Five-story pagoda on forested hill ----------------------------
    void Island02()
    {
        // Stone+earth hill (elliptical)
        Sprite(new (string, int)[]
        {
            ("       oHHsssssMo       ", 14),
            ("     oHHsssssssssMMo    ", 15),
            ("    oHssssssssssssMMo   ", 16),
            ("    oHsssmmmmmmddMMo    ", 17),
            ("     ommmmmmmddddo      ", 18),
            ("       oooooo           ", 19),
        }, SandMap);
        Sprite(new (string, int)[]
        {
            ("        ghggGk          ", 12),
            ("       ghgggGGGk        ", 13),
            ("        ggGGGk          ", 14),
        }, GrassMap);
        foreach (var rx in new[] { 8, 9, 10, 11, 12, 13, 14, 15 }) Put(rx, 15, cGoldRim);
        // Pagoda: central body (3px wide column) with tiered eaves
        // Tier 5 (top)
        Put(11, 4, cPagodaRoof); Put(12, 4, cPagodaRoof);
        // Tower body
        for (var sy = 5; sy <= 13; sy++)
        {
            Put(11, sy, cStoneHi);
            Put(12, sy, cStoneBright);
        }
        Put(11, 14, cStoneMid);
        // Eaves at levels
        foreach (var ey in new[] { 6, 8, 10, 12 })
        {
            for (var sx = 9; sx <= 14; sx++) Put(sx, ey, cPagodaRoof);
            Put(9, ey, cStoneHi); Put(14, ey, cStoneMid);
        }
        // Base platform
        for (var sx = 9; sx <= 14; sx++) { Put(sx, 14, cStoneBright); Put(sx, 13, cStoneHi); }
        // Small pines flanking
        Put(6, 10, cPineHi); Put(5, 11, cPineDark); Put(6, 11, cPineHi); Put(7, 11, cPineDark); Put(6, 12, cPineTrunk);
        Put(17, 9, cPineHi); Put(16, 10, cPineDark); Put(17, 10, cPineHi); Put(18, 10, cPineDark); Put(17, 11, cPineTrunk);
        // Cast shadow from pagoda rightward
        Put(15, 14, cSandDark); Put(16, 14, cSandDark); Put(15, 15, cSandDark);
        FoamRing(new[] { 4, 8, 12, 16, 20 }, 20, new[] { 6, 10, 14, 18 }, 21);
    }

    // ---- 03: Chinese karst pillar (Halong Bay style) -----------------------
    void Island03()
    {
        // Tall vertical limestone wedge, narrow at top, widens at base
        // Widths: 4, 6, 8, 10, 12, 14, 14, 12, 10, 8 (peak at water)
        Sprite(new (string, int)[]
        {
            ("           HH            ",  4),
            ("          HSSH           ",  5),
            ("         HSSSSH          ",  6),
            ("        HSSSSSSH         ",  7),
            ("       HSSSSSSSSH        ",  8),
            ("      HSSSSSSSSSSH       ",  9),
            ("     HSSSSSSSSSSSSH      ", 10),
            ("    HSSSSSSSSSSSSSSH     ", 11),
            ("    HSSSSSSSSSSSSSSH     ", 12),
            ("   HSSSSSMMMMSSSSSSH     ", 13),
            ("   HSSSSMMMMMMMSSSH      ", 14),
            ("    HMMMMMMMMMMMMH       ", 15),
            ("     EDDDDDDDDDE         ", 16),
            ("      EEEEEEEE           ", 17),
        }, StoneMap);
        // Green cap (moss/vegetation on top)
        Sprite(new (string, int)[]
        {
            ("          gggg           ",  3),
            ("         gGGGk           ",  4),
            ("          GGk            ",  5),
        }, GrassMap);
        // Cast shadow on right side of pillar (self-shadow)
        Put(17, 10, cStoneDark); Put(18, 11, cStoneDark); Put(17, 12, cStoneDark);
        FoamRing(new[] { 5, 9, 13, 17 }, 18, new[] { 7, 11, 15 }, 19);
    }

    // ---- 04: Bamboo grove islet (low sand, bamboo cluster) -----------------
    void Island04()
    {
        // Low, broad sand ellipse (~20x5)
        Sprite(new (string, int)[]
        {
            ("       oHHHHHHHHHo       ", 16),
            ("    oHHsssssssssssMMo    ", 17),
            ("  oHHsssmmmmmmmdddMMo    ", 18),
            ("    ommmmmmmddddo        ", 19),
            ("       ooooooo           ", 20),
        }, SandMap);
        // Tiny grass patch
        Sprite(new (string, int)[]
        {
            ("         gggg           ", 15),
            ("        ggGk            ", 16),
        }, GrassMap);
        foreach (var rx in new[] { 10, 11, 12, 13 }) Put(rx, 17, cGoldRim);
        // 4 bamboo stalks, each 2px wide (not 3 — bamboo is thinner), with nodes
        void BambooStalk(int col, int topY, int botY)
        {
            for (var sy = topY; sy <= botY; sy++)
            {
                var isNode = (sy % 3 == 0);
                Put(col, sy, isNode ? cBambooMid : cBambooHi);
                Put(col + 1, sy, isNode ? cBambooDark : cBambooMid);
            }
        }
        BambooStalk(8,  7, 16);
        BambooStalk(10, 6, 16);
        BambooStalk(12, 5, 16);
        BambooStalk(14, 8, 16);
        // Bamboo leaves (small fronds at tops)
        foreach (var (cx, cy) in new[] { (7, 6), (9, 5), (11, 4), (13, 7) })
        {
            Put(cx - 1, cy, cBambooHi); Put(cx, cy, cBambooHi); Put(cx + 1, cy, cBambooMid);
            Put(cx, cy - 1, cBambooHi);
        }
        // Cast shadows of bamboo stalks (rightward)
        foreach (var x in new[] { 11, 13, 15, 17 }) Put(x, 17, cSandDark);
        FoamRing(new[] { 3, 7, 11, 15, 19 }, 21, new[] { 5, 9, 13, 17 }, 22);
    }

    // ---- 05: Miniature Shinto shrine on stone steps ------------------------
    void Island05()
    {
        // Stone base (elliptical)
        Sprite(new (string, int)[]
        {
            ("        oHHsssMo         ", 16),
            ("      oHHsssssssMMo      ", 17),
            ("     oHsssmmmmmddMMo     ", 18),
            ("      ommmmmddddo        ", 19),
            ("        oooooo           ", 20),
        }, SandMap);
        // Stone steps (layered on the hill)
        Sprite(new (string, int)[]
        {
            ("         SSSS           ", 13),
            ("        SSSSSS          ", 14),
            ("       SSSSSSSS         ", 15),
        }, ch => ch switch
        {
            'S' => cStoneBright,
            _   => (Color?)null,
        });
        // Small shrine body (honden)
        for (var sy = 9; sy <= 12; sy++)
        for (var sx = 9; sx <= 13; sx++)
            Put(sx, sy, sy == 9 ? cStoneHi : cStoneBright);
        // Curved roof
        Put(8, 8, cPagodaRoof); Put(9, 7, cPagodaRoof); Put(10, 7, cPagodaRoof); Put(11, 7, cPagodaRoof); Put(12, 7, cPagodaRoof); Put(13, 8, cPagodaRoof);
        Put(8, 9, cStoneEdge);
        // Mini torii in front
        Put(9,  12, cToriiRed); Put(10, 12, cToriiRed);
        Put(9,  13, cToriiRedHi); Put(10, 13, cToriiRed);
        Put(9,  11, cToriiRedHi);
        // Cast shadow
        Put(14, 15, cSandDark);
        FoamRing(new[] { 5, 9, 13, 17 }, 21, new[] { 7, 11, 15 }, 22);
    }

    // ========================================================================
    // GROUP B: CARIBBEAN / TROPICAL EXPANDED (06–10)
    // ========================================================================

    // ---- 06: Caribbean cay with mangrove fringe ----------------------------
    void Island06()
    {
        // Sand cay ellipse (16x6)
        Sprite(new (string, int)[]
        {
            ("         oHHHHo          ", 15),
            ("      oHssssssssMo       ", 16),
            ("    oHHssssssssssMMo     ", 17),
            ("    oHsssmmmmmmmddMo     ", 18),
            ("     ommmmmddddo         ", 19),
            ("       oooooo            ", 20),
        }, SandMap);
        // Grass cap
        Sprite(new (string, int)[]
        {
            ("         gggg            ", 14),
            ("        ggGGk            ", 15),
        }, GrassMap);
        foreach (var rx in new[] { 9, 10, 11, 12, 13, 14 }) Put(rx, 16, cGoldRim);
        // Mangrove roots at water edge (dark vertical lines dipping into water)
        void Mangrove(int cx, int rootTop)
        {
            Put(cx, rootTop,     cMangroveGn);
            Put(cx - 1, rootTop + 1, cMangroveDk); Put(cx, rootTop + 1, cMangroveGn); Put(cx + 1, rootTop + 1, cMangroveDk);
            Put(cx, rootTop + 2, cMangroveGn);
            // Root hanging into water
            Put(cx, rootTop + 3, cMangroveDk);
            Put(cx, rootTop + 4, cMangroveDk);
        }
        Mangrove(6,  17);
        Mangrove(9,  17);
        Mangrove(14, 17);
        Mangrove(17, 16);
        // Single small palm
        Trunk(12, 8, 14);
        Sprite(new (string, int)[]
        {
            ("         vvVL            ",  4),
            ("        vvCvVL           ",  5),
            ("         vvvL            ",  6),
        }, CrownMap);
        FoamRing(new[] { 3, 7, 12, 16 }, 21, new[] { 5, 9, 14, 18 }, 22);
    }

    // ---- 07: Coral reef atoll (ring with pink/orange coral) ----------------
    void Island07()
    {
        // Coral ring (elliptical ring, hollow center showing lagoon)
        Sprite(new (string, int)[]
        {
            ("      oHHHbbbbHHo        ", 15),
            ("    oHHbbbLLLLbbHHo      ", 16),
            ("   oHHbbLLLLLLLLbbHHo    ", 17),
            ("   oHbbbLLLLLLLLLbbHo    ", 18),
            ("   oHHbbLLLLLLLLbbHHo    ", 19),
            ("    oHHHbbbbbbHHHo       ", 20),
        }, ch => ch switch
        {
            'H' => cCoralPink,
            'b' => cCoralBright,
            'L' => cLagoon,
            'o' => cCoralDark,
            _   => (Color?)null,
        });
        // Tiny sand islet on right side of ring
        Sprite(new (string, int)[]
        {
            ("                oo       ", 17),
            ("               oHsMo     ", 18),
            ("                ooo      ", 19),
        }, SandMap);
        // Gold rim accent on coral edges
        foreach (var rx in new[] { 7, 8, 14, 15 }) Put(rx, 17, cGoldRim);
        // Foam outside ring
        FoamRing(new[] { 4, 8, 12, 16, 20 }, 21, new[] { 6, 11, 15, 19 }, 22);
        // Inner foam where lagoon meets ring
        Put(9, 17, cWaterFoam); Put(14, 18, cWaterFoam);
    }

    // ---- 08: Sea grape islet (broad low sand, sea grape canopy) ------------
    void Island08()
    {
        // Wide flat sandbar (~20x5)
        Sprite(new (string, int)[]
        {
            ("       oHHHHHHHHHHo      ", 16),
            ("    oHHssssssssssssMMo   ", 17),
            ("  oHHsssmmmmmmmmdddMMo   ", 18),
            ("    ommmmmmmddddo        ", 19),
            ("       oooooo            ", 20),
        }, SandMap);
        // Sea grape canopy (broad low green dome)
        Sprite(new (string, int)[]
        {
            ("       eGGGGe            ", 14),
            ("      eGGGGGGGe          ", 15),
            ("     eGGGGGGGGGGe        ", 16),
        }, ch => ch switch
        {
            'e' => cSeaGrapeDk,
            'G' => cSeaGrape,
            _   => (Color?)null,
        });
        // Purple berry dots
        Put(9, 15, cGrapeBerry); Put(13, 15, cGrapeBerry); Put(11, 16, cGrapeBerry);
        // Trunk (short, cylinder shaded)
        Cylinder(11, 13, 16, cWoodHi, cWoodMid, cWoodDk);
        // Gold rim transition
        foreach (var rx in new[] { 7, 8, 9, 15, 16 }) Put(rx, 17, cGoldRim);
        FoamRing(new[] { 3, 7, 11, 15, 19 }, 21, new[] { 5, 9, 13, 17 }, 22);
    }

    // ---- 09: Hurricane-ravaged island (eroded sand, broken palm) -----------
    void Island09()
    {
        // Eroded, asymmetric sand — chunk missing on left, debris right
        Sprite(new (string, int)[]
        {
            ("        oHHHHHo          ", 15),
            ("     oHHssssssssMMMo     ", 16),
            ("   oHHssssssssssssMMo    ", 17),
            ("     oHHmmmmmdddMMo      ", 18),
            ("        ommmddoo         ", 19),
            ("         ooooo           ", 20),
        }, SandMap);
        // Broken palm — trunk snapped mid-height, leaning right
        for (var sy = 8; sy <= 12; sy++)
        {
            Put(10 + (sy - 8), sy, cTrunkHi);
            Put(11 + (sy - 8), sy, cTrunkBase);
            Put(12 + (sy - 8), sy, cTrunkMid);
        }
        // Snapped top (jagged)
        Put(14, 12, cTrunkDark); Put(15, 12, cTrunkMid);
        Put(15, 11, cTrunkHi);
        // Fallen palm frond on sand
        Put(6, 17, cFrondHi); Put(7, 17, cFrondMid); Put(8, 17, cFrondDark);
        // Driftwood debris
        Put(18, 17, cWoodMid); Put(19, 17, cWoodDk);
        Put(4, 16, cWoodMid);
        // Gold rim fragment
        Put(13, 17, cGoldRim);
        FoamRing(new[] { 3, 8, 13, 18 }, 21, new[] { 5, 10, 15 }, 22);
    }

    // ---- 10: Sandy cay with beach umbrella ---------------------------------
    void Island10()
    {
        // Clean sand ellipse (~16x7)
        Sprite(new (string, int)[]
        {
            ("         oHHHHo          ", 14),
            ("       oHssssssMo        ", 15),
            ("     oHsssssssssMMo      ", 16),
            ("    oHsssssmmmmmMMMo     ", 17),
            ("     oHmmmmmmdddMMo      ", 18),
            ("       ommmmddddo        ", 19),
            ("         oooooo          ", 20),
        }, SandMap);
        // Beach umbrella (striped canopy)
        Put(10, 12, cBeachUmbrHi); Put(11, 12, cBeachUmbr); Put(12, 12, cBeachUmbrHi); Put(13, 12, cBeachUmbr);
        Put(10, 11, cBeachUmbr); Put(11, 11, cBeachUmbrHi); Put(12, 11, cBeachUmbr); Put(13, 11, cBeachUmbrHi);
        // Umbrella pole (thin, shadow trailing right)
        Put(12, 13, cWoodMid); Put(12, 14, cWoodMid); Put(12, 15, cWoodDk);
        // Small towel on sand
        Put(8, 16, cMedDome); Put(9, 16, cMedDomeHi); Put(10, 16, cMedDome);
        // Cast shadow of umbrella (rightward)
        Put(14, 15, cSandDark); Put(15, 16, cSandDark);
        FoamRing(new[] { 5, 9, 13, 17 }, 21, new[] { 7, 11, 15 }, 22);
    }

    // ========================================================================
    // GROUP C: MEDITERRANEAN DEEPER (11–15)
    // ========================================================================

    // ---- 11: Roman aqueduct ruin (arched bridge across island) --------------
    void Island11()
    {
        // Stone hill base
        Sprite(new (string, int)[]
        {
            ("     EHSSSSSSSSSSME      ", 14),
            ("   EHSSSSSSSSSSSSSME     ", 15),
            ("  EHSSSSSSSSSSSSSSME     ", 16),
            ("  EHSSSMMMMMMMMMMSME     ", 17),
            ("   ESMMMMMMMDDDDDME      ", 18),
            ("    EEEEEEEEEEEE         ", 19),
        }, StoneMap);
        // Aqueduct arches: two tiers spanning left-center-right
        // Lower tier arches (row 12-13)
        for (var sy = 12; sy <= 13; sy++)
        {
            foreach (var cx in new[] { 5, 8, 11, 14, 17 })
            {
                Put(cx, sy, sy == 12 ? cStoneHi : cStoneBright);
                if (sy == 13) { Put(cx, sy, cStoneMid); }
            }
        }
        // Arch openings (dark spaces between columns)
        foreach (var ax in new[] { 6, 7, 9, 10, 12, 13, 15, 16 })
            Put(ax, 13, cStoneEdge);
        // Upper tier (row 9-10)
        for (var cx = 8; cx <= 14; cx++) Put(cx, 9, cStoneHi);
        for (var cx = 7; cx <= 15; cx++) Put(cx, 10, cStoneBright);
        // Water channel on top
        for (var cx = 6; cx <= 16; cx++) Put(cx, 8, cStoneHi);
        // Broken section (right side missing)
        // Cypress on hill
        Put(5, 10, cOliveDark); Put(5, 11, cOliveHi); Put(5, 12, cOliveHi); Put(5, 13, cOliveTrunk);
        Put(18, 11, cOliveDark); Put(18, 12, cOliveHi); Put(18, 13, cOliveTrunk);
        // Cast shadow from aqueduct
        Put(17, 15, cStoneDark); Put(18, 16, cStoneDark);
        FoamRing(new[] { 4, 8, 12, 16, 20 }, 20, new[] { 6, 10, 14, 18 }, 21);
    }

    // ---- 12: Cliff fishing village (stacked white boxes, colored boats) ----
    void Island12()
    {
        // Cliff base (steep limestone, tall left side grading down right)
        Sprite(new (string, int)[]
        {
            ("    EHSSSSSSSSSSME       ", 13),
            ("   EHSSSSSSSSSSSSME      ", 14),
            ("  EHSSSSSSSSSSSSSSME     ", 15),
            ("  EHSSSMMMMMMMSSSSME     ", 16),
            ("   ESMMMMMDDDDDSSME      ", 17),
            ("    EMEEDDDDDDDDME       ", 18),
            ("      EEEEEEEEE          ", 19),
        }, StoneMap);
        // Stacked white houses (cube clusters on the cliff)
        void WhiteCube(int lx, int ty, int w, int h)
        {
            for (var sy = ty; sy < ty + h; sy++)
            for (var sx = lx; sx < lx + w; sx++)
                Put(sx, sy, sy == ty ? cStoneHi : cStoneBright);
            for (var sx = lx; sx < lx + w; sx++)
                Put(sx, ty + h - 1, sx == lx ? cStoneEdge : cStoneMid);
        }
        WhiteCube(6,  9, 3, 3);
        WhiteCube(10, 8, 3, 4);
        WhiteCube(14, 10, 3, 3);
        WhiteCube(17, 11, 3, 2);
        // Tiny colored roofs
        Put(7, 9, cMedRoof); Put(11, 8, cMedRoof);
        // Small boats in water below
        Put(4, 18, cBoatHull); Put(5, 18, cBoatHull);
        Put(19, 17, cBoatHull); Put(20, 17, cBoatHull);
        // Boat masts (tiny)
        Put(5, 17, cStoneHi);
        Put(19, 16, cStoneHi);
        FoamRing(new[] { 4, 9, 14, 19 }, 20, new[] { 6, 11, 16 }, 21);
    }

    // ---- 13: Abandoned monastery on cliff (chapel, bell tower, ruins) ------
    void Island13()
    {
        // Stone cliff
        Sprite(new (string, int)[]
        {
            ("      EHSSSSSSSSME       ", 14),
            ("    EHSSSSSSSSSSSSME     ", 15),
            ("   EHSSSSSSSMMMSSSSME    ", 16),
            ("   EHSSMMMMMMDDDSSME     ", 17),
            ("    EMMMMDDDDDDDDME      ", 18),
            ("      EEEEEEEEEE         ", 19),
        }, StoneMap);
        // Chapel body (central)
        for (var sy = 9; sy <= 13; sy++)
        for (var sx = 9; sx <= 12; sx++) Put(sx, sy, sx == 9 ? cStoneEdge : cStoneBright);
        Put(9, 9, cStoneHi); Put(10, 9, cStoneHi); Put(11, 9, cStoneHi); Put(12, 9, cStoneHi);
        // Gable roof
        Put(9, 8, cMedRoof); Put(10, 7, cMedRoofHi); Put(11, 7, cMedRoofHi); Put(12, 8, cMedRoof);
        // Bell tower (left, tall)
        for (var sy = 5; sy <= 13; sy++)
        {
            Put(7, sy, cStoneHi);
            Put(8, sy, cStoneBright);
        }
        Put(7, 5, cStoneHi); Put(8, 5, cStoneHi);
        // Bell opening
        Put(8, 7, cStoneEdge);
        // Ruined wall section (right)
        Put(15, 11, cStoneBright); Put(15, 12, cStoneMid); Put(15, 13, cStoneMid);
        Put(16, 12, cStoneBright); Put(16, 13, cStoneMid);
        // Broken arch
        Put(14, 10, cStoneHi); Put(15, 10, cStoneHi);
        // Cypress
        Put(4, 12, cOliveDark); Put(4, 11, cOliveHi); Put(4, 10, cOliveHi);
        Put(19, 13, cOliveDark); Put(19, 12, cOliveHi);
        // Cast shadow
        Put(13, 15, cStoneDark); Put(9, 14, cStoneDark);
        FoamRing(new[] { 4, 9, 14, 19 }, 20, new[] { 6, 11, 16 }, 21);
    }

    // ---- 14: Cypress grove on hill -----------------------------------------
    void Island14()
    {
        // Gentle stone hill
        Sprite(new (string, int)[]
        {
            ("      EHSSSSSSSSME       ", 15),
            ("    EHSSSSSSSSSSSSME     ", 16),
            ("   EHSSSSSMMMMMSSSME     ", 17),
            ("    ESMMMMMMDDDDDME      ", 18),
            ("      EEEEEEEEEE         ", 19),
        }, StoneMap);
        // Cypress trees (tall thin dark spires, 2px wide)
        void Cypress(int cx, int ty)
        {
            Put(cx, ty,     cOliveHi);
            Put(cx, ty + 1, cOliveHi); Put(cx + 1, ty + 1, cOliveDark);
            Put(cx, ty + 2, cOliveHi); Put(cx + 1, ty + 2, cOliveDark);
            Put(cx, ty + 3, cOliveHi); Put(cx + 1, ty + 3, cOliveDark);
            Put(cx, ty + 4, cOliveTrunk);
        }
        Cypress(8,  9);
        Cypress(12, 7);
        Cypress(15, 8);
        Cypress(6,  10);
        Cypress(18, 10);
        // Cast shadows (trees cast rightward)
        Put(14, 16, cStoneDark); Put(17, 16, cStoneDark); Put(10, 17, cStoneDark);
        FoamRing(new[] { 4, 9, 14, 19 }, 20, new[] { 6, 11, 16 }, 21);
    }

    // ---- 15: Marble quarry coast (stepped white quarry face, crane) --------
    void Island15()
    {
        // Stepped quarry face (white, geometric, descending to water)
        Sprite(new (string, int)[]
        {
            ("       EHHHHHHHHE        ", 12),
            ("      EHWWWWWWWWWE       ", 13),
            ("     EHWWWWWWWWWWWE      ", 14),
            ("      EHWWWWWWWWWE       ", 15),
            ("       EHHHHHHHHE        ", 16),
            ("        EEWWEE           ", 17),
            ("         EEEE            ", 18),
        }, ch => ch switch
        {
            'H' => cStoneHi,
            'W' => cStoneBright,
            'E' => cStoneEdge,
            _   => (Color?)null,
        });
        // Quarry crane (small boom structure on top)
        Put(11, 9, cStoneDark); Put(12, 9, cStoneDark);   // boom
        Put(12, 10, cStoneDark); Put(12, 11, cStoneDark);  // mast
        // Cut block on platform
        Put(9, 13, cStoneHi); Put(10, 13, cStoneHi); Put(9, 14, cStoneBright); Put(10, 14, cStoneBright);
        // Marble vein accents (blue-grey streaks)
        Put(10, 14, cStoneMid);
        FoamRing(new[] { 4, 9, 14, 19 }, 19, new[] { 6, 11, 16 }, 20);
    }

    // ========================================================================
    // GROUP D: NORDIC DEEPER (16–20)
    // ========================================================================

    // ---- 16: Stave church on rock -------------------------------------------
    void Island16()
    {
        // Granite base
        Sprite(new (string, int)[]
        {
            ("       oHHHHHHo          ", 15),
            ("     oHbbbbbbBBo         ", 16),
            ("    oHbbbbbbbbBBo        ", 17),
            ("    oHmmmmmmmmBBo        ", 18),
            ("     oommmddddo          ", 19),
            ("       oooooo            ", 20),
        }, GraniteMap);
        // Stave church: dark wood, multi-tiered roof
        // Base
        for (var sx = 9; sx <= 14; sx++) { Put(sx, 13, cRoofDark); Put(sx, 14, cRoofDark); }
        // Walls (dark wood)
        for (var sy = 10; sy <= 12; sy++)
        for (var sx = 9; sx <= 14; sx++) Put(sx, sy, cWoodMid);
        Put(9, 12, cWoodDk);
        // Tier 1 roof (broad)
        for (var sx = 8; sx <= 15; sx++) Put(sx, 10, cRoofDark);
        // Tier 2 roof (narrower, higher)
        for (var sx = 10; sx <= 13; sx++) Put(sx, 7, cRoofDark);
        for (var sx = 10; sx <= 13; sx++) Put(sx, 8, cRoofDark);
        for (var sx = 10; sx <= 13; sx++) Put(sx, 9, cRoofDark);
        // Dragon-head ridge (tiny peak)
        Put(11, 6, cWoodDk); Put(12, 6, cWoodDk);
        Put(11, 5, cWoodMid);
        // Small pine left
        Put(5, 13, cPineHi); Put(4, 14, cPineDark); Put(5, 14, cPineHi); Put(6, 14, cPineDark);
        Put(5, 15, cPineTrunk);
        FoamRing(new[] { 4, 8, 13, 17 }, 21, new[] { 6, 10, 15 }, 22);
    }

    // ---- 17: Ice floe with seal ---------------------------------------------
    void Island17()
    {
        // Flat ice floe (~18x4)
        Sprite(new (string, int)[]
        {
            ("       oHHHHHHHoo        ", 16),
            ("    oHHssssssssssso      ", 17),
            ("  oHHsssssssssssssso     ", 18),
            ("    oossssssssssoo       ", 19),
            ("       ooooooo           ", 20),
        }, IceMap);
        // Small seal on ice (dark blob shape)
        Put(11, 15, cSealDark);
        Put(10, 16, cSealDark); Put(11, 16, cSealDark); Put(12, 16, cSealDark);
        // Seal head
        Put(10, 14, cSealDark); Put(11, 14, cSealDark);
        // Ice reflection highlight
        Put(13, 17, cIceHi);
        // Gold rim substitute: bright white edge
        foreach (var rx in new[] { 7, 8, 16, 17 }) Put(rx, 18, cSnowHi);
        FoamRing(new[] { 4, 9, 14, 19 }, 21, new[] { 6, 11, 16 }, 22);
    }

    // ---- 18: Runestone islet (standing carved stone on granite) ------------
    void Island18()
    {
        // Granite base ellipse
        Sprite(new (string, int)[]
        {
            ("        oHHHHo           ", 15),
            ("      oHbbbbbbBo         ", 16),
            ("     oHbbbbbbbbBBo       ", 17),
            ("     oHmmmmmmmmBBo       ", 18),
            ("       oomddddo          ", 19),
            ("         ooo             ", 20),
        }, GraniteMap);
        // Runestone (standing stone, 3px wide, irregular top)
        Put(11, 8, cStoneMid); Put(12, 8, cStoneBright);
        Put(11, 9, cStoneBright); Put(12, 9, cStoneHi);
        Put(11, 10, cStoneBright); Put(12, 10, cStoneHi);
        Put(11, 11, cStoneBright); Put(12, 11, cStoneHi);
        Put(11, 12, cStoneBright); Put(12, 12, cStoneMid);
        Put(11, 13, cStoneMid); Put(12, 13, cStoneMid);
        Put(11, 14, cStoneDark);
        // Carved rune marks (dark spots)
        Put(12, 9, cStoneEdge); Put(12, 11, cStoneEdge);
        // Cast shadow of runestone
        Put(14, 16, cGraniteDark); Put(15, 17, cGraniteDark);
        // Small pine
        Put(16, 13, cPineHi); Put(15, 14, cPineDark); Put(16, 14, cPineHi); Put(17, 14, cPineDark);
        FoamRing(new[] { 5, 9, 13, 17 }, 21, new[] { 7, 11, 15 }, 22);
    }

    // ---- 19: Sauna cabin on lake rock ---------------------------------------
    void Island19()
    {
        // Granite ledge
        Sprite(new (string, int)[]
        {
            ("      oHHHHHHHo          ", 15),
            ("    oHbbbbbbbbBBo        ", 16),
            ("    oHbbbbbbbbBBo        ", 17),
            ("     oommmmmmddo         ", 18),
            ("       oooooo            ", 19),
        }, GraniteMap);
        // Sauna cabin (small log cube)
        for (var sy = 11; sy <= 14; sy++)
        for (var sx = 8; sx <= 13; sx++) Put(sx, sy, (sx + sy) % 3 == 0 ? cWoodHi : cWoodMid);
        // Dark gable roof
        Put(7, 10, cRoofDark); Put(8, 9, cRoofDark); Put(9, 9, cRoofDark); Put(10, 9, cRoofDark); Put(11, 9, cRoofDark); Put(12, 9, cRoofDark); Put(13, 10, cRoofDark);
        // Chimney
        Put(10, 7, cGraniteBase); Put(10, 8, cGraniteMid);
        // Smoke wisps
        Put(10, 5, cSmoke); Put(10, 6, cSmoke);
        Put(9, 5, cSmokeDark); Put(11, 6, cSmokeDark);
        // Small pier extending into water
        for (var sx = 14; sx <= 18; sx++) Put(sx, 16, cWoodMid);
        Put(15, 17, cWoodDk); Put(17, 17, cWoodDk);
        // Door
        Put(10, 14, cWoodDk);
        // Window glow
        Put(12, 13, cLightGlow);
        FoamRing(new[] { 4, 9, 14, 19 }, 20, new[] { 6, 11, 16 }, 21);
    }

    // ---- 20: Viking longship hull beached -----------------------------------
    void Island20()
    {
        // Pebble/gravel beach (dark granite oval)
        Sprite(new (string, int)[]
        {
            ("       oHHHHHHo          ", 16),
            ("     oHbbbbbbbbBBo       ", 17),
            ("     oHbbbbbbbbBBo       ", 18),
            ("      oommmmmddo         ", 19),
            ("        ooooo            ", 20),
        }, GraniteMap);
        // Longship hull (curved dark wood form, resting on beach)
        // Hull curve: sweeping left-to-right
        Put(6,  15, cWoodDk); Put(7, 14, cWoodMid); Put(8, 14, cWoodHi);
        Put(9,  13, cWoodHi); Put(10, 13, cWoodHi); Put(11, 13, cWoodMid); Put(12, 13, cWoodMid);
        Put(13, 14, cWoodMid); Put(14, 14, cWoodDk); Put(15, 15, cWoodDk);
        // Hull bottom edge (dark)
        Put(8, 15, cWoodDk); Put(9, 14, cWoodDk); Put(10, 14, cWoodDk); Put(11, 14, cWoodDk); Put(12, 14, cWoodDk); Put(13, 15, cWoodDk);
        // Dragon head prow (left)
        Put(5, 14, cWoodHi); Put(5, 13, cWoodHi); Put(4, 13, cWoodMid);
        // Stern post (right)
        Put(16, 14, cWoodMid); Put(16, 13, cWoodHi);
        // Single mast stump
        Put(11, 10, cWoodMid); Put(11, 11, cWoodHi); Put(11, 12, cWoodMid);
        // Shield dots on hull side
        Put(8, 13, cLightRed); Put(10, 13, cLightWhite); Put(12, 13, cWoodYellow);
        // Cast shadow
        Put(15, 17, cGraniteDark); Put(16, 17, cGraniteDark);
        FoamRing(new[] { 4, 8, 13, 17 }, 21, new[] { 6, 10, 15 }, 22);
    }

    // ========================================================================
    // GROUP E: TROPICAL EXOTIC (21–25)
    // ========================================================================

    // ---- 21: African baobab tree on savanna islet --------------------------
    void Island21()
    {
        // Dry savanna sand (warm ochre ellipse)
        Sprite(new (string, int)[]
        {
            ("         oHHHHo           ", 14),
            ("       oHssssssMo         ", 15),
            ("     oHsssssssssMMo       ", 16),
            ("    oHsssmmmmmmmMMMo      ", 17),
            ("     oHmmmmmdddMMo        ", 18),
            ("       oomdddo            ", 19),
            ("         ooo              ", 20),
        }, SandMap);
        // Dry grass (savanna tone — use olive)
        Sprite(new (string, int)[]
        {
            ("          gg             ", 13),
            ("         gGk             ", 14),
        }, ch => ch switch
        {
            'g' => cOliveHi,
            'G' => cOliveDark,
            'k' => cOliveDark,
            _   => (Color?)null,
        });
        foreach (var rx in new[] { 8, 9, 14, 15 }) Put(rx, 15, cGoldRim);
        // Baobab trunk (massive, 4-5 px wide at base, 3 at top)
        for (var sy = 5; sy <= 14; sy++)
        {
            var w = sy <= 8 ? 3 : 5;
            var baseX = 10;
            Put(baseX, sy, cBaobabHi);
            Put(baseX + 1, sy, cBaobabMid);
            Put(baseX + 2, sy, cBaobabMid);
            if (w >= 4) Put(baseX + 3, sy, cBaobabDark);
            if (w >= 5) Put(baseX - 1, sy, cBaobabHi);
        }
        // Baobab branches (sparse, crown-like at top)
        Put(9, 4, cBaobabMid); Put(10, 4, cBaobabMid); Put(11, 4, cBaobabMid);
        Put(8, 3, cWoodDk); Put(12, 3, cWoodDk);
        Put(9, 2, cOliveDark); Put(10, 2, cOliveHi); Put(11, 2, cOliveDark);
        // Cast shadow
        Put(16, 17, cSandDark); Put(17, 17, cSandDark); Put(16, 18, cSandDark);
        FoamRing(new[] { 5, 9, 13, 17 }, 21, new[] { 7, 11, 15 }, 22);
    }

    // ---- 22: Baobab avenue islet (two smaller baobabs, savanna) ------------
    void Island22()
    {
        // Wide low sand (~20x5)
        Sprite(new (string, int)[]
        {
            ("       oHHHHHHHHHHo       ", 16),
            ("    oHHssssssssssssMMo    ", 17),
            ("   oHsssmmmmmmmmdddMMo    ", 18),
            ("     ommmmmmmdddo         ", 19),
            ("        oooooo            ", 20),
        }, SandMap);
        // Two smaller baobabs
        void SmallBaobab(int cx, int ty)
        {
            var bot = 16;
            for (var sy = ty; sy <= bot; sy++)
            {
                Put(cx, sy, cBaobabHi);
                Put(cx + 1, sy, cBaobabMid);
                Put(cx + 2, sy, cBaobabDark);
            }
            Put(cx - 1, ty, cBaobabMid);
            Put(cx + 1, ty - 1, cOliveDark); Put(cx, ty - 1, cOliveHi);
        }
        SmallBaobab(6,  10);
        SmallBaobab(14, 11);
        // Cast shadows
        Put(10, 18, cSandDark); Put(18, 18, cSandDark);
        FoamRing(new[] { 3, 7, 11, 15, 19 }, 21, new[] { 5, 9, 13, 17 }, 22);
    }

    // ---- 23: Easter Island moai (giant stone head on rock) -----------------
    void Island23()
    {
        // Rocky base ellipse
        Sprite(new (string, int)[]
        {
            ("         oooo            ", 16),
            ("       ooHHHHoo          ", 17),
            ("     ooHHbbbbHHoo        ", 18),
            ("    oHHbbbbbbHHoo        ", 19),
            ("     ooHHHHHHoo          ", 20),
        }, ch => ch switch
        {
            'H' => cMoaiHi,
            'b' => cMoaiMid,
            'o' => cMoaiDark,
            _   => (Color?)null,
        });
        // Moai head (massive rectangular stone, 7px wide x 8 tall)
        // Ears (side protrusions)
        Put(7, 8, cMoaiMid); Put(16, 8, cMoaiMid);
        Put(7, 9, cMoaiHi); Put(16, 9, cMoaiHi);
        Put(7, 10, cMoaiMid); Put(16, 10, cMoaiMid);
        // Main head block
        for (var sy = 8; sy <= 14; sy++)
        for (var sx = 8; sx <= 15; sx++)
        {
            var shade = cMoaiMid;
            if (sx <= 9) shade = cMoaiHi;
            if (sx >= 14) shade = cMoaiDark;
            if (sy == 8 && sx >= 10 && sx <= 13) shade = cMoaiHi;
            Put(sx, sy, shade);
        }
        // Eye sockets
        Put(10, 10, cMoaiDark); Put(13, 10, cMoaiDark);
        // Nose ridge
        Put(11, 11, cMoaiHi); Put(12, 11, cMoaiHi);
        Put(11, 12, cMoaiHi); Put(12, 12, cMoaiMid);
        // Cast shadow
        Put(17, 17, cMoaiDark); Put(18, 17, cMoaiDark);
        FoamRing(new[] { 4, 8, 13, 17 }, 21, new[] { 6, 10, 15 }, 22);
    }

    // ---- 24: Polynesian tiki (carved post + thatched hut on sand) ----------
    void Island24()
    {
        // Sand ellipse
        Sprite(new (string, int)[]
        {
            ("         oHHHHo           ", 15),
            ("       oHssssssMo         ", 16),
            ("     oHsssssssssMMo       ", 17),
            ("    oHsssmmmmmdddMo       ", 18),
            ("      ommmmddddo          ", 19),
            ("        oooooo            ", 20),
        }, SandMap);
        // Thatched hut (triangular roof shape)
        Sprite(new (string, int)[]
        {
            ("         tttt            ", 11),
            ("        tttttt           ", 12),
            ("       tttttttt          ", 13),
        }, ch => ch switch
        {
            't' => cThatchHi,
            _   => (Color?)null,
        });
        // Hut body (dark)
        for (var sx = 9; sx <= 13; sx++) { Put(sx, 14, cTikiWood); Put(sx, 15, cTikiWood); }
        // Tiki carved post (left of hut, 2px wide)
        Put(7, 8, cTikiWood); Put(8, 8, cTikiWood);
        Put(7, 9, cTikiWood); Put(8, 9, cTikiWood);
        // Tiki face carving (light notches)
        Put(7, 10, cTikiThatch); Put(8, 10, cTikiThatch);
        Put(7, 11, cTikiThatch);
        Put(7, 12, cTikiWood); Put(8, 12, cTikiWood);
        Put(7, 13, cTikiWood); Put(8, 13, cTikiWood);
        Put(7, 14, cTikiWood);
        // Small palm on right
        Trunk(15, 9, 15);
        Sprite(new (string, int)[]
        {
            ("         vvVL            ",  5),
            ("        vvCvVL           ",  6),
            ("         vvvL            ",  7),
        }, CrownMap);
        // Gold rim
        foreach (var rx in new[] { 9, 10, 14 }) Put(rx, 16, cGoldRim);
        FoamRing(new[] { 5, 9, 13, 17 }, 21, new[] { 7, 11, 15 }, 22);
    }

    // ---- 25: Giant palm / Madagascar traveler's palm islet -----------------
    void Island25()
    {
        // Sand ellipse (~16x6)
        Sprite(new (string, int)[]
        {
            ("         oHHHo            ", 15),
            ("       oHssssMo           ", 16),
            ("     oHsssssssMMo         ", 17),
            ("    oHsssmmmmmddMo        ", 18),
            ("      ommmmddddo          ", 19),
            ("        ooooo             ", 20),
        }, SandMap);
        // Tall palm with characteristic fan fronds
        Trunk(11, 3, 14);
        // Fan fronds (radiating from top)
        Sprite(new (string, int)[]
        {
            ("       vvVvvVL           ",  1),
            ("      vvVvvvVVL          ",  2),
            ("     vvvCvvvvVVLL        ",  3),
            ("      vvVvvvVVL          ",  4),
        }, CrownMap);
        // Small lemur dot in branches (tiny grey/brown blob)
        Put(12, 2, cMoaiMid); Put(13, 2, cMoaiDark);
        // Gold rim
        foreach (var rx in new[] { 9, 10, 11, 12, 13 }) Put(rx, 16, cGoldRim);
        FoamRing(new[] { 5, 9, 13, 17 }, 21, new[] { 7, 11, 15 }, 22);
    }

    // ========================================================================
    // GROUP F: ARCTIC / ANTARCTIC (26–30)
    // ========================================================================

    // ---- 26: Penguin colony rock -------------------------------------------
    void Island26()
    {
        // Dark rock base (irregular, wide)
        Sprite(new (string, int)[]
        {
            ("       oHHHHHHHo         ", 16),
            ("     oHbbbbbbbbBBo       ", 17),
            ("    oHbbbbbbbbbbBBo      ", 18),
            ("     oommmmmmdddo        ", 19),
            ("       ooooooo           ", 20),
        }, GraniteMap);
        // Snow patches on rock
        Sprite(new (string, int)[]
        {
            ("       Hsss             ", 15),
            ("      Hsssss            ", 16),
        }, SnowMap);
        // Penguins: black back, white belly, 3px tall
        void Penguin(int cx, int cy)
        {
            Put(cx, cy, cSealDark);             // head
            Put(cx - 1, cy + 1, cSealDark); Put(cx, cy + 1, cSnowHi); Put(cx + 1, cy + 1, cSealDark);
            Put(cx, cy + 2, cSnowHi);            // belly white
        }
        Penguin(8,  14);
        Penguin(12, 14);
        Penguin(10, 15);
        Penguin(14, 15);
        Penguin(6,  15);
        // Gold rim (bright snow edge)
        foreach (var rx in new[] { 5, 12 }) Put(rx, 18, cIceHi);
        FoamRing(new[] { 4, 9, 14, 19 }, 21, new[] { 6, 11, 16 }, 22);
    }

    // ---- 27: Iceberg with arch tunnel --------------------------------------
    void Island27()
    {
        // Iceberg (irregular, tall ice form)
        Sprite(new (string, int)[]
        {
            ("         HHHH             ",  4),
            ("       HssssssH           ",  5),
            ("      HsssssssssH         ",  6),
            ("     HsssssssssssH        ",  7),
            ("     HsssssssssssH        ",  8),
            ("    HsssssssssssssH       ",  9),
            ("    HssssssWWsssssH       ", 10),
            ("   HsssssWWWWsssssH       ", 11),
            ("   HssssssWWssssssH       ", 12),
            ("   HssssssssssssssH       ", 13),
            ("    HsssssssssssH         ", 14),
            ("     HsssssssssH          ", 15),
            ("      HsssssssH           ", 16),
            ("       HHHHHHH            ", 17),
        }, ch => ch switch
        {
            'H' => cIceHi,
            's' => cIceMid,
            'W' => cIceDeep,
            _   => (Color?)null,
        });
        // Tunnel opening (dark showing water through)
        // W pixels already in rows 10-12 create the arch tunnel
        // Highlight on arch top
        Put(10, 9, cIceHi); Put(16, 9, cIceHi);
        Put(11, 10, cIceHi); Put(17, 10, cIceHi);
        // Gold rim substitute (bright white edge)
        foreach (var rx in new[] { 8, 18 }) Put(rx, 10, cSnowHi);
        FoamRing(new[] { 4, 9, 14, 19 }, 18, new[] { 6, 11, 16 }, 19);
    }

    // ---- 28: Ice cave island (dome of ice with dark cave mouth) ------------
    void Island28()
    {
        // Ice dome (wide, with dark cave opening)
        Sprite(new (string, int)[]
        {
            ("       HHHHHHHHHH         ", 12),
            ("     HHssssssssssHH       ", 13),
            ("    HssssssWWssssssH      ", 14),
            ("   HssssWWWWWWssssssH     ", 15),
            ("   HsssWWWWWWWWsssssH     ", 16),
            ("    HssssWWWWssssssH      ", 17),
            ("     HHHssssssssHHH       ", 18),
            ("        HHHHHHHH          ", 19),
            ("          oooo            ", 20),
        }, ch => ch switch
        {
            'H' => cIceHi,
            's' => cIceMid,
            'W' => cIceDeep,
            'o' => cIceDeep,
            _   => (Color?)null,
        });
        FoamRing(new[] { 4, 9, 14, 19 }, 21, new[] { 6, 11, 16 }, 22);
    }

    // ---- 29: Frozen shipwreck mast (mast poking from ice/snow) -------------
    void Island29()
    {
        // Ice/snow mound (frozen ship buried within)
        Sprite(new (string, int)[]
        {
            ("       oHHssMo            ", 16),
            ("     oHHssssssMMo         ", 17),
            ("    oHHssssssssssMo       ", 18),
            ("    oHsssmmmmmmMMo        ", 19),
            ("     oommmddddo           ", 20),
            ("       ooooo              ", 21),
        }, IceMap);
        // Ship mast (tall dark wood, 3px cylinder shading, tilted slightly right)
        for (var sy = 5; sy <= 16; sy++)
        {
            var sx = 9 + (sy - 5) / 5; // slight rightward lean
            if (sx >= 12) sx = 12;
            Put(sx, sy, cWoodHi);
            Put(sx + 1, sy, cWoodMid);
        }
        // Broken crows nest / rigging fragments
        Put(11, 6, cWoodDk); Put(12, 6, cWoodDk);
        Put(12, 7, cWoodMid);
        // Torn sail fragment (tattered white/grey)
        Put(12, 8, cWreckSail); Put(13, 8, cWreckSail);
        Put(12, 9, cWreckSail);
        // Broken yardarm
        Put(10, 9, cWoodMid); Put(13, 10, cWoodDk);
        // Gold rim (bright ice edge)
        foreach (var rx in new[] { 6, 15 }) Put(rx, 19, cIceHi);
        FoamRing(new[] { 4, 9, 14, 19 }, 22, new[] { 6, 11, 16 }, 23);
    }

    // ---- 30: Glacier calving face (tall vertical ice wall) -----------------
    void Island30()
    {
        // Tall narrow ice wall (vertical wedge, widens at base)
        Sprite(new (string, int)[]
        {
            ("          HH             ",  3),
            ("         HssH            ",  4),
            ("        HssssH           ",  5),
            ("        HssssH           ",  6),
            ("       HssssssH          ",  7),
            ("       HssssssH          ",  8),
            ("      HssssssssH         ",  9),
            ("      HssssssssH         ", 10),
            ("     HssssssssssH        ", 11),
            ("     HsssmssssssH        ", 12),
            ("    HsssmmsssssssH       ", 13),
            ("    HssmmmmsssssssH      ", 14),
            ("   HsssmmmmssssssssH     ", 15),
            ("   HssmmmmmmsssssssH     ", 16),
            ("    HmmmmmmmmsssssH      ", 17),
            ("     HHHHHHHHHHHH        ", 18),
        }, ch => ch switch
        {
            'H' => cIceHi,
            's' => cIceMid,
            'm' => cIceShadow,
            _   => (Color?)null,
        });
        // Blue ice streak (deep glacial blue)
        Put(14, 12, cIceDeep); Put(14, 13, cIceDeep); Put(13, 14, cIceDeep);
        FoamRing(new[] { 4, 9, 14, 19 }, 19, new[] { 6, 11, 16 }, 20);
    }

    // ========================================================================
    // GROUP G: MYTHIC / FANTASY (31–35)
    // ========================================================================

    // ---- 31: Floating crystal isle (crystal spire on floating rock) --------
    void Island31()
    {
        // Floating rock base (narrow, chunky)
        Sprite(new (string, int)[]
        {
            ("        oHHHHo            ", 14),
            ("      oHbbbbBBo           ", 15),
            ("     oHbbbbbbBBo          ", 16),
            ("      oHbbbbBBo           ", 17),
            ("        ooooo             ", 18),
        }, GraniteMap);
        // Crystal spire (tall glowing cyan structure, 3px wide)
        for (var sy = 3; sy <= 13; sy++)
        {
            Put(10, sy, cCrystalHi);
            Put(11, sy, cCrystalMid);
            Put(12, sy, cCrystalHi);
        }
        // Crystal tip (narrowing)
        Put(11, 1, cCrystalGlow);
        Put(10, 2, cCrystalHi); Put(11, 2, cCrystalGlow); Put(12, 2, cCrystalHi);
        // Glow aura around crystal
        Put(11, 0, cCrystalGlow);
        Put(10, 1, cCrystalGlow); Put(12, 1, cCrystalGlow);
        // Floating rock shadow (below, disconnected)
        Put(9, 20, cGraniteDark); Put(10, 20, cGraniteDark); Put(11, 20, cGraniteDark); Put(12, 20, cGraniteDark);
        FoamRing(new[] { 4, 9, 14, 19 }, 19, new[] { 6, 11, 16 }, 20);
    }

    // ---- 32: Sunken Atlantis spire (stone spire just above water) ----------
    void Island32()
    {
        // Low stone structure barely breaking surface (~16x4 top)
        Sprite(new (string, int)[]
        {
            ("     oHHHssssssHHHo      ", 16),
            ("   oHHsssssssssssHHo     ", 17),
            ("   oHHsssmmmmmmssHHo     ", 18),
            ("    oHHmmmmmmddHHo       ", 19),
            ("      oooooooo           ", 20),
        }, ch => ch switch
        {
            'H' => cAtlantisSt,
            's' => cStoneBright,
            'm' => cStoneMid,
            'd' => cStoneDark,
            'o' => cStoneEdge,
            _   => (Color?)null,
        });
        // Central spire (pointing up)
        Put(11, 10, cStoneHi); Put(12, 10, cStoneHi);
        Put(11, 11, cStoneBright); Put(12, 11, cStoneBright);
        Put(11, 12, cStoneBright); Put(12, 12, cStoneMid);
        Put(11, 13, cStoneMid); Put(12, 13, cStoneMid);
        Put(11, 14, cStoneDark);
        // Seaweed draped on spire
        Put(10, 13, cSeaweedGn); Put(13, 13, cSeaweedGn);
        Put(10, 14, cSeaweedDk); Put(12, 14, cSeaweedGn);
        // Submerged stone shadow (hint of more structure below)
        Put(8, 18, cStoneDark); Put(14, 18, cStoneDark);
        Put(9, 19, cStoneDark); Put(13, 19, cStoneDark);
        // Gold rim substitute (bright edge)
        foreach (var rx in new[] { 6, 17 }) Put(rx, 17, cStoneHi);
        FoamRing(new[] { 4, 9, 14, 19 }, 21, new[] { 6, 11, 16 }, 22);
    }

    // ---- 33: Dragon skull rock formation ------------------------------------
    void Island33()
    {
        // Dark bone/rock skull shape (organic, asymmetric)
        Sprite(new (string, int)[]
        {
            ("       HHHH               ", 12),
            ("     HHbbbbHH             ", 13),
            ("    HbbbbbbbbHH           ", 14),
            ("   HbbbbHHbbbbHH          ", 15),
            ("  HbbbbHHHHbbbbHH         ", 16),
            ("  HbbbbHHHHbbbbHH         ", 17),
            ("  HbbbbHHbbbbbbHH         ", 18),
            ("   HbbbbbbbbbbHH          ", 19),
            ("    HHHHHHHHHH            ", 20),
        }, ch => ch switch
        {
            'H' => cBoneHi,
            'b' => cBoneMid,
            _   => (Color?)null,
        });
        // Eye socket (dark hollow)
        Put(8, 14, cBoneDark); Put(9, 14, cBoneDark);
        Put(8, 15, cBoneDark); Put(9, 15, cBoneDark);
        // Glowing eye within socket
        Put(9, 15, cDragonEye);
        // Teeth (lower jaw — bone spikes)
        Put(7, 17, cBoneHi); Put(11, 17, cBoneHi);
        Put(6, 18, cBoneHi); Put(12, 18, cBoneHi);
        // Horn protrusions (top)
        Put(6, 11, cBoneMid); Put(14, 11, cBoneMid);
        Put(5, 10, cBoneMid);
        // Outline edge
        Put(3, 16, cBoneDark); Put(20, 15, cBoneDark);
        FoamRing(new[] { 3, 8, 13, 18 }, 21, new[] { 5, 10, 15 }, 22);
    }

    // ---- 34: Witch's hut on stilts over water ------------------------------
    void Island34()
    {
        // Small sand/mud patch
        Sprite(new (string, int)[]
        {
            ("        oHHHHo            ", 17),
            ("      oHbbbbBBo           ", 18),
            ("       oobboo             ", 19),
        }, ch => ch switch
        {
            'H' => cMudHi,
            'b' => cMudMid,
            'B' => cMudDark,
            'o' => cMudDark,
            _   => (Color?)null,
        });
        // Stilts (4 thin dark posts going down into water)
        for (var sy = 12; sy <= 17; sy++)
        {
            Put(8, sy, cHutWood);
            Put(11, sy, cHutWood);
            Put(13, sy, cHutWood);
            Put(16, sy, cHutWood);
        }
        // Stilt water reflections (dark hints)
        Put(8, 18, cWoodDk); Put(11, 18, cWoodDk); Put(13, 18, cWoodDk); Put(16, 18, cWoodDk);
        // Hut body (crooked 7x4 box)
        for (var sy = 8; sy <= 11; sy++)
        for (var sx = 8; sx <= 15; sx++) Put(sx, sy, cHutWood);
        // Crooked roof (asymmetric)
        Put(7, 7, cHutRoof); Put(8, 6, cHutRoof); Put(9, 6, cHutRoof); Put(10, 5, cHutRoof); Put(11, 5, cHutRoof); Put(12, 5, cHutRoof); Put(13, 6, cHutRoof); Put(14, 7, cHutRoof);
        // Window glow (greenish)
        Put(10, 9, cWitchLight); Put(11, 9, cWitchLight);
        // Door
        Put(13, 10, cWoodDk); Put(13, 11, cWoodDk);
        // Chimney
        Put(9, 5, cGraniteBase); Put(9, 4, cGraniteBase);
        // Smoke puff
        Put(9, 3, cSmoke);
        // Small lantern hanging from corner
        Put(15, 7, cLightGlow);
        FoamRing(new[] { 4, 9, 14, 19 }, 20, new[] { 6, 11, 16 }, 21);
    }

    // ---- 35: Giant mushroom island ------------------------------------------
    void Island35()
    {
        // Dark soil/moss base (wide low ellipse)
        Sprite(new (string, int)[]
        {
            ("       oHHHHHHo           ", 17),
            ("     oHbbbbbbbbBBo        ", 18),
            ("     oHbbbbbbbbBBo        ", 19),
            ("      oommmmddo           ", 20),
            ("        oooo              ", 21),
        }, ch => ch switch
        {
            'H' => cWoodMid,
            'b' => cWoodDk,
            'B' => cWoodDk,
            'm' => cWoodDk,
            'd' => cWoodDk,
            'o' => cWoodDk,
            _   => (Color?)null,
        });
        // Mushroom stem (thick cylinder, 4px wide)
        for (var sy = 9; sy <= 17; sy++)
        {
            Put(9, sy, cStemBase);
            Put(10, sy, cStemBase);
            Put(11, sy, cMushroomSp);
            Put(12, sy, cStemBase);
        }
        Put(13, 15, cWoodDk); Put(13, 16, cWoodDk);  // stem shadow accent
        // Mushroom cap (dome)
        Sprite(new (string, int)[]
        {
            ("       RRRRRR             ",  5),
            ("      RRRRRRRR            ",  6),
            ("     RRRRRRRRRRR           ",  7),
            ("    RRRRRWWWRRRRR          ",  8),
            ("     RRRRWWWRRRR           ",  9),
        }, ch => ch switch
        {
            'R' => cMushroomCap,
            'W' => cMushroomSp,
            _   => (Color?)null,
        });
        // Small mushrooms on ground
        Put(5, 16, cMushroomCap); Put(5, 17, cStemBase);
        Put(17, 17, cMushroomCap); Put(17, 18, cStemBase);
        // Gold rim on cap edge
        foreach (var rx in new[] { 8, 9, 15, 16 }) Put(rx, 10, cGoldRim);
        FoamRing(new[] { 4, 9, 14, 19 }, 22, new[] { 6, 11, 16 }, 23);
    }

    // ========================================================================
    // GROUP H: INDUSTRIAL / MODERN (36–40)
    // ========================================================================

    // ---- 36: Rusted oil rig stub -------------------------------------------
    void Island36()
    {
        // Oil-stained water/shadow base (reflection block)
        Put(9, 18, cRustDark); Put(10, 18, cRustDark); Put(11, 18, cRustDark); Put(12, 18, cRustDark); Put(13, 18, cRustDark); Put(14, 18, cRustDark);
        // Platform (horizontal deck)
        for (var sx = 7; sx <= 16; sx++) { Put(sx, 13, cRustHi); Put(sx, 14, cRustMid); }
        // Support legs (4 corners, 1px, dark)
        for (var sy = 15; sy <= 17; sy++)
        {
            Put(7, sy, cRustDark);
            Put(10, sy, cRustDark);
            Put(13, sy, cRustDark);
            Put(16, sy, cRustDark);
        }
        // Cross braces
        Put(8, 16, cRustMid); Put(9, 15, cRustMid);
        Put(15, 16, cRustMid); Put(14, 15, cRustMid);
        // Derrick (central structure, 3px cylinder)
        for (var sy = 5; sy <= 12; sy++)
        {
            Put(10, sy, cRustHi);
            Put(11, sy, cRustMid);
            Put(12, sy, cRustDark);
        }
        // Top platform
        Put(9, 5, cRustHi); Put(10, 4, cRustHi); Put(11, 4, cRustHi); Put(12, 4, cRustHi); Put(13, 5, cRustHi);
        // Small flame/vent on top
        Put(11, 3, cLavaGlow);
        // Equipment boxes
        Put(8, 13, cContBlue); Put(15, 13, cContRed);
        // Gold rim substitute (bright rust edge)
        foreach (var rx in new[] { 7, 16 }) Put(rx, 14, cRustHi);
        FoamRing(new[] { 5, 10, 15, 20 }, 19, new[] { 7, 12, 17 }, 20);
    }

    // ---- 37: Container port micro ------------------------------------------
    void Island37()
    {
        // Concrete quay (wide flat surface)
        Sprite(new (string, int)[]
        {
            ("    oHHHHHHHHHHHHHHo     ", 15),
            ("   oHbbbbbbbbbbbbbbBo    ", 16),
            ("   oHbbbbbbbbbbbbbbBo    ", 17),
            ("   oHbbbbbbbbbbbbbbBo    ", 18),
            ("    oobbbbbbbbbboo       ", 19),
            ("       ooooooo           ", 20),
        }, ConcreteMap);
        // Stacked containers (colored blocks, 4-wide x 2-tall each)
        void Container(int lx, int ty, Color c)
        {
            for (var sy = ty; sy < ty + 3; sy++)
            for (var sx = lx; sx < lx + 4; sx++)
                Put(sx, sy, sy == ty ? c : c);
            // Dark edge on bottom and right
            for (var sx = lx; sx < lx + 4; sx++) Put(sx, ty + 2, Color.FromRgb(c.R * 0.6, c.G * 0.6, c.B * 0.6));
        }
        Container(4,  10, cContRed);
        Container(9,  8,  cContBlue);
        Container(9,  11, cContYellow);
        Container(14, 9,  cContGreen);
        Container(14, 12, cContRed);
        // Tiny crane (stick boom)
        Put(18, 10, cRustDark); Put(18, 11, cRustDark);
        Put(17, 10, cRustMid); Put(19, 10, cRustMid);
        Put(17, 9, cConcreteDk);
        // Mooring bollard
        Put(5, 18, cConcreteDk); Put(20, 18, cConcreteDk);
        // Gold rim
        foreach (var rx in new[] { 5, 18 }) Put(rx, 17, cGoldRim);
        FoamRing(new[] { 3, 8, 13, 18 }, 21, new[] { 5, 10, 15, 20 }, 22);
    }

    // ---- 38: Abandoned lighthouse base (crumbled bottom, no tower) ---------
    void Island38()
    {
        // Rocky base (irregular dark rocks)
        Sprite(new (string, int)[]
        {
            ("      oHHHHHHHo          ", 15),
            ("    oHbbbbbbbbBBo         ", 16),
            ("   oHbbbbbbbbbbBBo        ", 17),
            ("   oHbbbbbbbbbbBBo        ", 18),
            ("    oommmmddo            ", 19),
            ("      oooo               ", 20),
        }, GraniteMap);
        // Crumbled lighthouse base (broken cylindrical stump, 4px wide)
        for (var sy = 10; sy <= 14; sy++)
        for (var sx = 9; sx <= 12; sx++) Put(sx, sy, cLightWhite);
        Put(9, 14, cGraniteMid); Put(12, 14, cGraniteMid);
        // Broken top (irregular)
        Put(9, 9, cGraniteMid); Put(10, 8, cLightWhite); Put(11, 9, cLightWhite); Put(12, 8, cGraniteMid);
        // Fallen debris pieces
        Put(7, 16, cLightWhite); Put(8, 16, cLightWhite);
        Put(14, 15, cLightWhite);
        Put(15, 16, cGraniteMid);
        // Weeds growing through cracks
        Put(10, 15, cOliveHi);
        Put(7, 15, cOliveDark);
        // Cast shadow
        Put(14, 16, cGraniteDark); Put(15, 16, cGraniteDark);
        FoamRing(new[] { 4, 9, 14, 19 }, 21, new[] { 6, 11, 16 }, 22);
    }

    // ---- 39: Decommissioned submarine pen (concrete bunker, dark opening) --
    void Island39()
    {
        // Rocky cliff with concrete bunker built in
        Sprite(new (string, int)[]
        {
            ("   oHHHHHHHHHHHHHHo      ", 14),
            ("  oHbbbbbbbbbbbbbbBo     ", 15),
            ("  oHbbbbWWWWbbbbbbBo     ", 16),
            ("  oHbbbbWWWWbbbbbbBo     ", 17),
            ("  oHbbbbWWWWbbbbbbBo     ", 18),
            ("   oobbbbbbbbbboo        ", 19),
            ("      ooooooo            ", 20),
        }, ch => ch switch
        {
            'H' => cConcreteHi,
            'b' => cConcreteMid,
            'B' => cConcreteDk,
            'W' => cGraniteDark,
            'o' => cConcreteDk,
            _   => (Color?)null,
        });
        // Pen opening (dark interior showing water)
        // Already in sprite via 'W' characters
        // Water inside pen opening
        Put(8, 18, cWaterFoam);
        // Concrete reinforcements
        Put(7, 14, cConcreteHi); Put(17, 14, cConcreteHi);
        // Rusted door/gate remnant
        Put(5, 16, cRustMid); Put(5, 17, cRustDark);
        // Seaweed on concrete
        Put(18, 16, cSeaweedGn); Put(19, 17, cSeaweedGn);
        FoamRing(new[] { 3, 8, 13, 18 }, 21, new[] { 5, 10, 15 }, 22);
    }

    // ---- 40: Offshore wind turbine mini-platform ---------------------------
    void Island40()
    {
        // Small concrete platform base (strip at water)
        Sprite(new (string, int)[]
        {
            ("       oHHHHHHo           ", 17),
            ("      oHbbbbbbBo          ", 18),
            ("       oobbbbo            ", 19),
        }, ConcreteMap);
        // Turbine tower (tall white cylinder, 3px wide)
        for (var sy = 4; sy <= 17; sy++)
        {
            Put(10, sy, cTurbineWh);
            Put(11, sy, cTurbineGy);
            Put(12, sy, cTurbineWh);
        }
        // Nacelle (top box)
        Put(9, 3, cTurbineGy); Put(10, 3, cTurbineGy); Put(11, 3, cTurbineGy); Put(12, 3, cTurbineGy);
        // Three blades (diagonal lines)
        // Blade 1 (pointing up-left)
        Put(8, 2, cTurbineWh); Put(7, 1, cTurbineWh);
        // Blade 2 (pointing right)
        Put(13, 3, cTurbineWh); Put(14, 3, cTurbineWh); Put(15, 3, cTurbineWh);
        // Blade 3 (pointing down-right)
        Put(13, 4, cTurbineWh); Put(14, 5, cTurbineGy);
        // Warning light on nacelle
        Put(11, 2, cLightRed);
        // Platform access ladder (dark vertical)
        Put(13, 15, cConcreteDk); Put(13, 16, cConcreteDk);
        // Gold rim substitute
        foreach (var rx in new[] { 6, 17 }) Put(rx, 19, cTurbineWh);
        FoamRing(new[] { 5, 10, 15, 20 }, 20, new[] { 7, 12, 17 }, 21);
    }

    // ========================================================================
    // GROUP I: WRECKAGE / DECAY (41–45)
    // ========================================================================

    // ---- 41: Old shipwreck tilted on reef -----------------------------------
    void Island41()
    {
        // Reef rock base (irregular)
        Sprite(new (string, int)[]
        {
            ("     oHHsssssMo           ", 16),
            ("    oHsssssssssMMo        ", 17),
            ("   oHsssssmmmmmddMo       ", 18),
            ("    ommmmmmddddo          ", 19),
            ("      oooooo              ", 20),
        }, SandMap);
        // Shipwreck hull (wooden, tilted left)
        // Hull body (left-leaning, 3-4px tall strip)
        for (var sy = 13; sy <= 15; sy++)
        for (var sx = 6; sx <= 16; sx++)
        {
            var shade = cWoodMid;
            if (sx <= 8) shade = cWoodHi;
            if (sx >= 14) shade = cWoodDk;
            Put(sx, sy, shade);
        }
        // Higher stern (right side)
        Put(15, 12, cWoodHi); Put(16, 12, cWoodMid);
        Put(15, 11, cWoodHi);
        // Broken bow (left, smashed)
        Put(5, 14, cWoodMid); Put(4, 14, cWoodDk);
        // Broken masts (two stumps)
        Put(9, 9, cWoodDk); Put(9, 10, cWoodMid); Put(9, 11, cWoodHi);
        Put(13, 9, cWoodDk); Put(13, 10, cWoodMid); Put(13, 11, cWoodHi);
        // Torn sail fragment on rear mast
        Put(12, 8, cWreckSail); Put(13, 7, cWreckSail); Put(14, 8, cWreckSail);
        // Hull planking highlights
        Put(10, 14, cWoodHi); Put(12, 14, cWoodHi);
        // Gold rim along sand edge
        foreach (var rx in new[] { 4, 19 }) Put(rx, 19, cGoldRim);
        FoamRing(new[] { 3, 8, 13, 18 }, 21, new[] { 5, 10, 15, 20 }, 22);
    }

    // ---- 42: Pirate careened hull (galleon tilted on sand) -----------------
    void Island42()
    {
        // Sand beach (wide, asymmetrical — sloping left up, right flat)
        Sprite(new (string, int)[]
        {
            ("    oHHHHH               ", 16),
            ("   oHsssssssMo           ", 17),
            ("  oHssssssssssMMo        ", 18),
            ("  oHsssmmmmmmmddMo       ", 19),
            ("   ommmmddddo            ", 20),
            ("     ooooo               ", 21),
        }, SandMap);
        // Careened galleon hull (curved, tilting to show bottom)
        // Hull planking from top to waterline
        for (var sy = 13; sy <= 17; sy++)
        for (var sx = 5; sx <= 14; sx++)
        {
            var shade = cWoodMid;
            if (sy >= 16) shade = cWoodDk;
            if (sx >= 12) shade = cWoodDk;
            Put(sx, sy, shade);
        }
        // Hull bottom copper/red (careened = bottom visible)
        for (var sx = 5; sx <= 12; sx++) { Put(sx, 17, cWoodRed); Put(sx, 18, cWoodRed); }
        // Stern castle (high back)
        for (var sy = 9; sy <= 12; sy++)
        for (var sx = 10; sx <= 14; sx++) Put(sx, sy, cWoodHi);
        Put(10, 8, cWoodHi); Put(11, 8, cWoodHi); Put(12, 8, cWoodHi);
        // Cannon ports (dark squares)
        Put(7, 15, cWoodDk); Put(11, 15, cWoodDk);
        Put(7, 16, cWoodDk); Put(11, 16, cWoodDk);
        // Bowsprit (forward pointing stick)
        Put(3, 15, cWoodMid); Put(4, 15, cWoodHi);
        // Broken mast
        Put(8, 9, cWoodDk); Put(8, 10, cWoodMid); Put(8, 11, cWoodHi); Put(8, 12, cWoodMid);
        // Fallen yardarm on sand
        Put(15, 18, cWoodMid); Put(16, 18, cWoodDk); Put(17, 18, cWoodMid);
        // Gold rim
        foreach (var rx in new[] { 3, 19 }) Put(rx, 20, cGoldRim);
        FoamRing(new[] { 2, 8, 14, 19 }, 22, new[] { 4, 10, 16 }, 23);
    }

    // ---- 43: Crashed bush plane on beach -----------------------------------
    void Island43()
    {
        // Wide flat sand beach
        Sprite(new (string, int)[]
        {
            ("     oHHHHHHHHHHo        ", 16),
            ("   oHHssssssssssMMo      ", 17),
            ("  oHsssmmmmmmmmdddMMo    ", 18),
            ("   ommmmmmmmddddo        ", 19),
            ("      oooooo             ", 20),
        }, SandMap);
        // Small plane fuselage (5 long x 2 tall, tilted)
        for (var sy = 14; sy <= 15; sy++)
        for (var sx = 7; sx <= 13; sx++) Put(sx, sy, cPlaneBody);
        // Cockpit window (front)
        Put(7, 14, cPlaneWing);
        // Tail (vertical)
        Put(12, 13, cPlaneStrip); Put(13, 12, cPlaneStrip); Put(13, 13, cPlaneStrip);
        // Broken wing (left, half buried in sand)
        Put(4, 15, cPlaneWing); Put(5, 15, cPlaneWing); Put(6, 15, cPlaneWing);
        Put(4, 16, cPlaneWing); Put(5, 16, cPlaneWing);
        // Broken wing stub (right)
        Put(14, 14, cPlaneWing); Put(15, 14, cPlaneWing);
        // Propeller (bent)
        Put(6, 14, cPlaneWing); Put(6, 13, cPlaneWing);
        // Debris scattered
        Put(16, 17, cPlaneBody); Put(17, 17, cPlaneWing);
        // Gold rim
        foreach (var rx in new[] { 6, 18 }) Put(rx, 19, cGoldRim);
        FoamRing(new[] { 3, 8, 13, 18 }, 21, new[] { 5, 10, 15 }, 22);
    }

    // ---- 44: Half-sunken plane wing (large wing floating) ------------------
    void Island44()
    {
        // No true island base — just the wing structure at water level
        // Sparse sand debris near wing
        Put(10, 20, cSandMid); Put(11, 20, cSandDark);
        // Large wing section (8 wide, floating, partially submerged)
        Sprite(new (string, int)[]
        {
            ("   WWWWWWWW             ", 15),
            ("   WWWWWWWW             ", 16),
            ("    WWWWWW              ", 17),
            ("     WWWW               ", 18),
        }, ch => ch switch
        {
            'W' => cPlaneWing,
            _   => (Color?)null,
        });
        // Wing stripes
        Put(6, 15, cPlaneStrip); Put(10, 15, cPlaneStrip);
        Put(7, 16, cPlaneStrip); Put(9, 16, cPlaneStrip);
        // Engine pod (cylindrical, attached under wing)
        Put(7, 17, cPlaneBody); Put(8, 17, cPlaneBody);
        Put(7, 18, cPlaneWing); Put(8, 18, cPlaneWing);
        // Metallic reflection glint
        Put(5, 16, cIceHi);
        // Oil slick / shadow
        Put(8, 19, cRustDark); Put(9, 19, cRustDark);
        FoamRing(new[] { 4, 9, 14, 19 }, 19, new[] { 6, 11, 16 }, 20);
    }

    // ---- 45: Abandoned wooden pier ruin ------------------------------------
    void Island45()
    {
        // Small sand/pebble strip
        Sprite(new (string, int)[]
        {
            ("       oHHHHo             ", 17),
            ("     oHbbbbBBo            ", 18),
            ("      oobboo              ", 19),
        }, ch => ch switch
        {
            'H' => cMudHi,
            'b' => cMudMid,
            'B' => cMudDark,
            'o' => cMudDark,
            _   => (Color?)null,
        });
        // Pier posts (vertical, staggered, some leaning)
        void PierPost(int cx, int top, int bot, bool leanRight)
        {
            for (var sy = top; sy <= bot; sy++)
            {
                var sx = cx + (leanRight ? (sy - top) / 3 : 0);
                Put(sx, sy, cWoodMid);
            }
        }
        PierPost(6,  13, 17, false);
        PierPost(9,  12, 17, true);
        PierPost(12, 14, 17, false);
        PierPost(15, 11, 17, false);
        PierPost(18, 13, 17, true);
        // Broken planks (horizontal, some gaps)
        for (var sx = 5; sx <= 13; sx++) Put(sx, 13, cWoodHi);
        Put(8, 13, cWoodDk);  // gap
        Put(11, 13, cWoodDk); // gap
        for (var sx = 5; sx <= 14; sx++) Put(sx, 12, cWoodMid);
        Put(7, 12, cWoodDk); Put(10, 12, cWoodDk); // gaps
        // Missing section (right side: only posts, no planks)
        // Splintered plank end
        Put(14, 12, cWoodDk);
        // Seagull on post (tiny white dot)
        Put(18, 10, cSnowHi);
        FoamRing(new[] { 4, 9, 14, 19 }, 20, new[] { 6, 11, 16 }, 21);
    }

    // ========================================================================
    // GROUP J: NATURAL CURIOS (46–50)
    // ========================================================================

    // ---- 46: Sea arch rock formation ---------------------------------------
    void Island46()
    {
        // Rock base with arch opening
        Sprite(new (string, int)[]
        {
            ("      HHHH        HHHH    ", 13),
            ("    HbbbbHH      HHbbbbH  ", 14),
            ("   HbbbbbbHH    HHbbbbbbH ", 15),
            ("   HbbbbbbHHWWWWHbbbbbbH ", 16),
            ("   HbbbbbbHHWWWWHbbbbbbH ", 17),
            ("    HbbbbbbHHWWWHbbbbH   ", 18),
            ("     HbbbbbbHHHHbbbbH    ", 19),
            ("      HHHHHHHHHHHHH      ", 20),
        }, ch => ch switch
        {
            'H' => cBasaltHi,
            'b' => cBasaltMid,
            'W' => cIceDeep,
            _   => (Color?)null,
        });
        // Water showing through arch (W chars in sprite)
        // Arch top highlight
        Put(11, 15, cBasaltHi); Put(12, 15, cBasaltHi);
        Put(11, 16, cBasaltHi); Put(12, 16, cBasaltHi);
        // Arch opening water glint
        Put(12, 18, cWaterFoam);
        // Gold rim
        foreach (var rx in new[] { 3, 20 }) Put(rx, 14, cGoldRim);
        FoamRing(new[] { 3, 8, 13, 18 }, 21, new[] { 5, 10, 15, 20 }, 22);
    }

    // ---- 47: Blowhole cliff (spray shooting up) ----------------------------
    void Island47()
    {
        // Vertical dark cliff (wedge shape, tall)
        Sprite(new (string, int)[]
        {
            ("         HHHH            ",  8),
            ("        HHbbHH           ",  9),
            ("       HHbbbbHH          ", 10),
            ("       HbbbbbbHH         ", 11),
            ("      HbbbbbbbbHH        ", 12),
            ("      HbbbbbbbbHH        ", 13),
            ("     HbbbbbbbbbbHH       ", 14),
            ("     HbbbbbbbbbbHH       ", 15),
            ("    HbbbbbbbbbbbbHH      ", 16),
            ("    HbbbbbbbbbbbbHH      ", 17),
            ("   HbbbbbbbbbbbbbbHH     ", 18),
            ("   HddddddddddddddHH     ", 19),
            ("     oooooooooooo        ", 20),
        }, ch => ch switch
        {
            'H' => cBasaltHi,
            'b' => cBasaltMid,
            'd' => cBasaltDark,
            'o' => cBasaltDark,
            _   => (Color?)null,
        });
        // Blowhole opening (dark crack at cliff top)
        Put(11, 8, cBasaltDark); Put(12, 8, cBasaltDark);
        // Water spray shooting up (white/cyan burst)
        Put(11, 5, cWaterFoam); Put(12, 5, cWaterFoam);
        Put(10, 6, cWaterFoam); Put(11, 6, cWaterFoam); Put(12, 6, cWaterFoam); Put(13, 6, cWaterFoam);
        Put(10, 7, cWaterFoam); Put(11, 7, cWaterFoam); Put(12, 7, cWaterFoam); Put(13, 7, cWaterFoam);
        // Spray droplets
        Put(9, 5, cIceHi); Put(14, 5, cIceHi);
        Put(8, 6, cIceMid); Put(15, 6, cIceMid);
        // Gold rim
        foreach (var rx in new[] { 6, 17 }) Put(rx, 10, cGoldRim);
        FoamRing(new[] { 3, 8, 13, 18 }, 21, new[] { 5, 10, 15, 20 }, 22);
    }

    // ---- 48: Giant's Causeway basalt columns (hexagonal stepped) -----------
    void Island48()
    {
        // Stepped hexagonal columns (dark basalt, geometric)
        // Layer 1 (top, narrow)
        Sprite(new (string, int)[]
        {
            ("       HHHH               ", 13),
            ("      HbbbbH              ", 14),
            ("      HbbbbH              ", 15),
        }, BasaltMap);
        // Layer 2 (wider)
        Sprite(new (string, int)[]
        {
            ("     HHHHHHHH             ", 15),
            ("    HbbbbbbbbH            ", 16),
            ("    HbbbbbbbbH            ", 17),
        }, BasaltMap);
        // Layer 3 (widest)
        Sprite(new (string, int)[]
        {
            ("   HHHHHHHHHHHH           ", 17),
            ("  HbbbbbbbbbbbbH          ", 18),
            ("  HbbbbbbbbbbbbH          ", 19),
            ("   HHHHHHHHHHHH           ", 20),
        }, BasaltMap);
        // Column joint lines (dark vertical divisions)
        foreach (var (x, topY, botY) in new[] { (7, 16, 17), (9, 16, 17), (13, 16, 17), (15, 16, 17) })
            for (var sy = topY; sy <= botY; sy++) Put(x, sy, cBasaltDark);
        // Highlight on top edges
        Put(7, 13, cBasaltHi); Put(10, 13, cBasaltHi);
        Put(5, 17, cBasaltHi); Put(17, 17, cBasaltHi);
        // Gold rim
        foreach (var rx in new[] { 4, 18 }) Put(rx, 19, cGoldRim);
        FoamRing(new[] { 4, 9, 14, 19 }, 21, new[] { 6, 11, 16 }, 22);
    }

    // ---- 49: Thermal vent / fumarole (rock cone with steam, sulfur) --------
    void Island49()
    {
        // Dark volcanic rock base
        Sprite(new (string, int)[]
        {
            ("        oHHHHo            ", 16),
            ("      oHbbbbbbBo          ", 17),
            ("     oHbbbbbbbbBo         ", 18),
            ("     oHbbbbbbbbBo         ", 19),
            ("      oobbbboo            ", 20),
            ("        oooo              ", 21),
        }, GraniteMap);
        // Sulfur deposits (yellow patches on rock)
        Put(8, 17, cSulfurYel); Put(9, 17, cSulfurDk);
        Put(14, 18, cSulfurYel); Put(15, 18, cSulfurDk);
        Put(11, 19, cSulfurGlow);
        // Vent cone (small cone at center top)
        Sprite(new (string, int)[]
        {
            ("          cc             ", 13),
            ("         HddH            ", 14),
            ("        HddddH           ", 15),
        }, ch => ch switch
        {
            'c' => cLavaCore,
            'H' => cLavaRockHi,
            'd' => cLavaRockDk,
            _   => (Color?)null,
        });
        // Steam/smoke rising
        Put(11, 10, cSmokeDark); Put(12, 10, cSmokeDark);
        Put(10, 9, cSmoke); Put(11, 9, cSmoke); Put(12, 9, cSmoke);
        Put(10, 8, cSmoke); Put(12, 8, cSmoke);
        Put(11, 7, cSmoke);
        // Glow from vent
        Put(11, 12, cLavaGlow); Put(12, 12, cLavaGlow);
        // Gold rim
        foreach (var rx in new[] { 7, 16 }) Put(rx, 20, cGoldRim);
        FoamRing(new[] { 5, 10, 15, 20 }, 22, new[] { 7, 12, 17 }, 23);
    }

    // ---- 50: Mud volcano cone (small cone, bubbling mud) --------------------
    void Island50()
    {
        // Mud cone base (low, wide ellipse)
        Sprite(new (string, int)[]
        {
            ("       ooHHHHoo          ", 17),
            ("     ooHHbbbbHHoo        ", 18),
            ("    oHHbbbbbbbbHHo       ", 19),
            ("    oHHbbbbbbbbHHo       ", 20),
            ("     ooHHHHHHHHoo        ", 21),
        }, ch => ch switch
        {
            'H' => cMudHi,
            'b' => cMudMid,
            'o' => cMudDark,
            _   => (Color?)null,
        });
        // Crater at top (dark pool)
        Sprite(new (string, int)[]
        {
            ("         dddd            ", 15),
            ("        dWBBd            ", 16),
        }, ch => ch switch
        {
            'd' => cMudDark,
            'W' => cMudMid,
            'B' => cBubbleMud,
            _   => (Color?)null,
        });
        // Bubbles in mud pool
        Put(9, 15, cBubbleMud); Put(12, 15, cBubbleMud);
        Put(10, 16, cMudHi);    // bubble highlight
        // Mud drip on side
        Put(8, 18, cMudDark); Put(14, 18, cMudDark);
        Put(7, 19, cMudDark);
        // Small steam puff
        Put(10, 14, cSmoke);
        // Gold rim
        foreach (var rx in new[] { 6, 16 }) Put(rx, 20, cGoldRim);
        FoamRing(new[] { 4, 9, 14, 19 }, 22, new[] { 6, 11, 16 }, 23);
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
        Island21, Island22, Island23, Island24, Island25,
        Island26, Island27, Island28, Island29, Island30,
        Island31, Island32, Island33, Island34, Island35,
        Island36, Island37, Island38, Island39, Island40,
        Island41, Island42, Island43, Island44, Island45,
        Island46, Island47, Island48, Island49, Island50,
    };

    int idx;
    if (selectedIsland.HasValue && selectedIsland.Value >= 1 && selectedIsland.Value <= 50)
    {
        idx = selectedIsland.Value - 1;
    }
    else
    {
        var t = ctx.Elapsed.TotalSeconds;
        idx = ((int)Math.Floor(t)) % 50;
        if (idx < 0) idx += 50;
        // 2-row cycle indicator: top row = idx % 24, second row = idx / 24
        Put(idx % 24, 0, cWaterFoam);
        Put(idx / 24, 1, cWaterFoam);
    }

    actions[idx]();
};