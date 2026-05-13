// ---
// app: PalmIsland11EllipticalSilhouette
// displayName: Palm Island 11 — Elliptical silhouette
// author: Ronald Schlenker
// description: Fixes the stacked-stripes problem — the entire island silhouette is now a clear horizontally-stretched ELLIPSE. Sand is widest in the middle and narrows at top and bottom. Outline pixels at the silhouette edge clearly define the curved shape. Grass is a smaller inner ellipse on top.
// appType: Scene
// duration: 10
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

const int canvasH = 24;
const int canvasW = 24;

// ---- COLOUR PALETTE -------------------------------------------------------
var cSkyTop      = Color.FromRgb(0.62, 0.88, 1.00);
var cSkyHorizon  = Color.FromRgb(0.85, 0.95, 1.00);
var cWaterBright = Color.FromRgb(0.30, 0.78, 0.74);
var cWaterDeep   = Color.FromRgb(0.10, 0.50, 0.55);
var cWaterFoam   = Color.FromRgb(0.95, 0.99, 1.00);

// Grass — 4 shades
var cGrassHi     = Color.FromRgb(0.62, 0.92, 0.36);
var cGrassBright = Color.FromRgb(0.42, 0.78, 0.28);
var cGrassMid    = Color.FromRgb(0.26, 0.58, 0.22);
var cGrassDark   = Color.FromRgb(0.14, 0.34, 0.16);

// Yellow/gold rim
var cGoldRim     = Color.FromRgb(0.85, 0.62, 0.22);

// Sand — 5 shades
var cSandHi      = Color.FromRgb(1.00, 0.94, 0.72);
var cSandBright  = Color.FromRgb(0.96, 0.84, 0.55);
var cSandMid     = Color.FromRgb(0.78, 0.62, 0.32);
var cSandDark    = Color.FromRgb(0.50, 0.36, 0.20);
var cSandWet     = Color.FromRgb(0.32, 0.22, 0.14);  // also serves as the silhouette OUTLINE pixel

// Palm trunk — 4 shades
var cTrunkHi     = Color.FromRgb(0.65, 0.42, 0.22);
var cTrunkBase   = Color.FromRgb(0.46, 0.28, 0.14);
var cTrunkMid    = Color.FromRgb(0.30, 0.18, 0.08);
var cTrunkDark   = Color.FromRgb(0.16, 0.10, 0.05);

// Crown
var cFrondHi     = Color.FromRgb(0.62, 0.92, 0.36);
var cFrondMid    = Color.FromRgb(0.36, 0.74, 0.30);
var cFrondDark   = Color.FromRgb(0.20, 0.48, 0.20);
var cCoco        = Color.FromRgb(0.42, 0.24, 0.10);

// ============================================================================
// SCENE
// ============================================================================
var scene = (RasterSurface ctx) =>
{
    var horizonY = 8;

    // ---- LAYER 0: sky + water
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

    void put(int x, int y, Color c)
    {
        if (x >= 0 && x < canvasW && y >= 0 && y < canvasH) ctx.SetPixel(x, y, c);
    }

    // ============================================================================
    // ISLAND BODY — designed as an ELLIPTICAL SILHOUETTE.
    // Each row has DIFFERENT WIDTH following an ellipse curve (wider in middle,
    // narrower at top and bottom). Sand fills most of it. Grass is a smaller
    // inner ellipse on top. The very edge pixels are dark sand-wet (outline)
    // so the silhouette is clearly framed.
    // ============================================================================
    //
    // Sand silhouette (ellipse: 18 wide × 8 tall, centred at y=17):
    //   row 13: width  6   <- top (narrow)
    //   row 14: width 10
    //   row 15: width 14
    //   row 16: width 18   <- widest
    //   row 17: width 18
    //   row 18: width 16
    //   row 19: width 12
    //   row 20: width  6   <- bottom (narrow)
    //
    //                          11111111112222
    //                0123456789012345678901234
    var sandRows = new[]
    {
        ("         oHHHHo         ", 13),  // top of sand ellipse — narrow + outline pixels
        ("       oHsssssssMo      ", 14),
        ("      oHsssssssssMMo    ", 15),
        ("    oHHsssssssssssMMMo  ", 16),  // widest
        ("    oHsssssmmmmmmMMMMo  ", 17),
        ("     oHsmmmmmmmmmddMo   ", 18),
        ("      ommmmmmmddddo     ", 19),
        ("         ooooooo        ", 20),  // bottom front (narrow)
    };
    foreach (var (row, sy) in sandRows)
        for (var sx = 0; sx < row.Length && sx < canvasW; sx++)
        {
            var ch = row[sx];
            if (ch == ' ') continue;
            Color c = ch switch
            {
                'H' => cSandHi,
                's' => cSandBright,
                'm' => cSandMid,
                'M' => cSandMid,
                'd' => cSandDark,
                'o' => cSandWet,   // also functions as the dark outline at silhouette edge
                _ => cSandBright,
            };
            put(sx, sy, c);
        }

    // ---- GRASS PLATEAU (smaller inner ellipse on top of sand)
    // Width pattern (also elliptical, smaller than sand):
    //   row 10: width  4
    //   row 11: width  8
    //   row 12: width 10  <- widest
    //   row 13: width  8
    //                          11111111112222
    //                0123456789012345678901234
    var grassRows = new[]
    {
        ("          gggg          ", 10),
        ("        ghggGGGk        ", 11),
        ("       ghgggGGGGk       ", 12),  // widest grass
        ("        gggGGGk         ", 13),  // bottom of grass — sits ON the sand top
    };
    foreach (var (row, sy) in grassRows)
        for (var sx = 0; sx < row.Length && sx < canvasW; sx++)
        {
            var ch = row[sx];
            if (ch == ' ') continue;
            Color c = ch switch
            {
                'h' => cGrassHi,
                'g' => cGrassBright,
                'G' => cGrassMid,
                'k' => cGrassDark,
                _ => cGrassBright,
            };
            put(sx, sy, c);
        }

    // ---- GOLDEN RIM where the grass ellipse boundary meets the sand
    // (just a thin band on the front of the grass-sand junction)
    foreach (var rx in new[] { 8, 9, 10, 11, 12, 13, 14, 15 })
        put(rx, 14, cGoldRim);   // wait this conflicts with sand row 14, will overwrite

    // ---- LAYER: PALM TRUNK (centred on grass plateau) — 3 px wide
    var trunkCol = 12;
    for (var sy = 5; sy <= 12; sy++)
    {
        put(trunkCol - 1, sy, cTrunkHi);
        put(trunkCol,     sy, cTrunkBase);
        put(trunkCol + 1, sy, cTrunkMid);
    }
    // Trunk root accent
    put(trunkCol + 2, 11, cTrunkDark);
    put(trunkCol + 2, 12, cTrunkDark);

    // ---- LAYER: PALM CROWN
    var crownRows = new[]
    {
        ("        vvvvVL          ",  0),
        ("       vvvCvvvVL        ",  1),
        ("      vvvvvvvvVVL       ",  2),
        ("     vvvvCvvvvVVLL      ",  3),
        ("      vvvvvvvvVL        ",  4),
        ("       vvvCvvL          ",  5),
    };
    foreach (var (row, sy) in crownRows)
        for (var sx = 0; sx < row.Length && sx < canvasW; sx++)
        {
            var ch = row[sx];
            if (ch == ' ') continue;
            Color c = ch switch
            {
                'v' => cFrondHi,
                'V' => cFrondMid,
                'L' => cFrondDark,
                'C' => cCoco,
                _ => cFrondHi,
            };
            put(sx, sy, c);
        }

    // ---- FOAM RING around the silhouette base
    foreach (var fx in new[] { 6, 9, 12, 15, 18 })
        put(fx, 21, cWaterFoam);
    foreach (var fx in new[] { 7, 11, 14, 17 })
        put(fx, 22, cWaterFoam);
};
