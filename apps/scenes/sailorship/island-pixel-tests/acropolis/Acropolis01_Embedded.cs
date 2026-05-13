// ---
// app: Acropolis01Embedded
// displayName: Acropolis 01 Embedded
// author: Ronald Schlenker
// description: First refined Acropolis. Temple is visually embedded into the limestone hill via foreground granite boulders that overlap the stylobate front and intrude into the rightmost column area. Cypress on the left flank breaks the temple-rock seam. White-limestone hill carries the full DKC gradient. Pediment is a proper triangle. Two-pixel columns with H/D cylinder shading and cast shadows that fall onto the stylobate.
// appType: Scene
// duration: 10
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

const int canvasW = 24;
const int canvasH = 24;

// ---- PALETTE (warm Mediterranean) -----------------------------------------
var cSkyTop      = Color.FromRgb(0.55, 0.74, 0.96);
var cSkyHorizon  = Color.FromRgb(1.00, 0.90, 0.74);
var cWaterBright = Color.FromRgb(0.22, 0.58, 0.68);
var cWaterDeep   = Color.FromRgb(0.04, 0.22, 0.38);
var cWaterFoam   = Color.FromRgb(0.96, 0.99, 1.00);

// White limestone (5-shade) — temple AND hill use this
var cStoneHi     = Color.FromRgb(1.00, 0.98, 0.92);
var cStoneBright = Color.FromRgb(0.92, 0.86, 0.74);
var cStoneMid    = Color.FromRgb(0.70, 0.62, 0.50);
var cStoneDark   = Color.FromRgb(0.40, 0.32, 0.24);
var cStoneEdge   = Color.FromRgb(0.22, 0.16, 0.12);
var cStoneCast   = Color.FromRgb(0.62, 0.52, 0.38);   // cast-shadow tone

// Granite foreground boulders (4-shade) — separate material for contrast
var cRockHi      = Color.FromRgb(0.62, 0.58, 0.54);
var cRockBase    = Color.FromRgb(0.42, 0.38, 0.36);
var cRockMid     = Color.FromRgb(0.24, 0.20, 0.20);
var cRockDark    = Color.FromRgb(0.10, 0.08, 0.08);

// Vegetation
var cCypressHi   = Color.FromRgb(0.30, 0.50, 0.30);
var cCypressDk   = Color.FromRgb(0.10, 0.26, 0.16);
var cTrunk       = Color.FromRgb(0.34, 0.24, 0.16);

// Goldrim — warm seam between temple and hill
var cGoldRim     = Color.FromRgb(0.85, 0.62, 0.22);

// ============================================================================
// SCENE
// ============================================================================
var scene = (RasterSurface ctx) =>
{
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
    // LAYER 1: LIMESTONE HILL (full DKC ellipse, top row IS the stylobate)
    // The hill's top is what the temple sits on. The full elliptical silhouette
    // gives the island its plasticity — varying row widths, outline pixels.
    // ========================================================================
    Sprite(new (string, int)[]
    {
        ("       oHHHHHHHHHHHHHo  ", 14),  // top: smooth stylobate band
        ("    oHHsssssssssssssssMo", 15),  // widening, second step
        ("   oHsssssssssssssssssMo", 16),
        ("   oHssssmmmmmmmmmMMMMMo", 17),  // widest, mid tone takes over right
        ("   oHmmmmmmmmmmmddMMMMMo", 18),
        ("    ommmmmmmmddddddMMMo ", 19),
        ("      ooooooooooooooo   ", 20),  // narrow bottom, all outline
    }, ch => ch switch
    {
        'H' => cStoneHi,
        's' => cStoneBright,
        'm' => cStoneMid,
        'M' => cStoneMid,
        'd' => cStoneDark,
        'o' => cStoneEdge,
        _   => (Color?)null,
    });

    // ========================================================================
    // LAYER 2: CYPRESS on the left hill flank (sits on hill, breaks edge)
    // Tall narrow dark green tree partially obscuring the architrave's left.
    // ========================================================================
    void Cypress(int cx, int topY, int height)
    {
        Put(cx, topY, cCypressDk);
        for (var sy = topY + 1; sy < topY + height; sy++)
        {
            Put(cx,     sy, cCypressHi);
            Put(cx + 1, sy, cCypressDk);
        }
        Put(cx, topY + height, cTrunk);
    }
    Cypress(4, 8, 6);  // left, top y=8, base y=14 — overlaps the architrave height range

    // ========================================================================
    // LAYER 3: TEMPLE
    // Pediment (proper triangle) → architrave → capitals → columns → bases
    // Columns at cols 9-10, 12-13, 15-16, 18-19 (4 columns, 2-wide, 1-px gaps)
    // ========================================================================

    // Pediment (apex y=2, base y=6) — outline 'e', fill 'P'
    Sprite(new (string, int)[]
    {
        ("            ePPe        ",  2),  // apex 4 wide
        ("           ePPPPe       ",  3),  // 6 wide
        ("          ePPPPPPe      ",  4),  // 8 wide
        ("         ePPPPPPPPe     ",  5),  // 10 wide
        ("        ePPPPPPPPPPe    ",  6),  // 12 wide (base, x=8-19)
    }, ch => ch switch
    {
        'e' => cStoneEdge,
        'P' => cStoneHi,
        _   => (Color?)null,
    });

    // Architrave (entablature) — slight overhang both sides, 2 rows
    for (var sx = 7; sx <= 20; sx++) Put(sx, 7, cStoneBright);   // top: bright band
    for (var sx = 7; sx <= 20; sx++) Put(sx, 8, cStoneMid);      // bottom: shadow band
    Put(7, 7, cStoneEdge); Put(20, 7, cStoneEdge);
    Put(7, 8, cStoneEdge); Put(20, 8, cStoneEdge);
    // Ruined notch in the architrave
    Put(14, 8, cStoneEdge);
    Put(15, 8, cStoneEdge);

    // Capitals (continuous abacus) — bright caps with darker brackets between
    var capRow = "        MHHMHHMHHMHHM   ";
    for (var sx = 0; sx < capRow.Length; sx++)
    {
        var ch = capRow[sx];
        if (ch == 'H') Put(sx, 9, cStoneHi);
        else if (ch == 'M') Put(sx, 9, cStoneBright);
    }

    // Column bodies (rows 10-12) — H = highlight (left), D = shadow (right)
    foreach (var leftX in new[] { 9, 12, 15, 18 })
    {
        for (var sy = 10; sy <= 12; sy++)
        {
            Put(leftX,     sy, cStoneHi);
            Put(leftX + 1, sy, cStoneMid);
        }
    }
    // Ruined column 3 — base chunk missing (creates character)
    Put(15, 12, cStoneEdge);

    // Bases (continuous, same pattern as caps)
    for (var sx = 0; sx < capRow.Length; sx++)
    {
        var ch = capRow[sx];
        if (ch == 'H') Put(sx, 13, cStoneHi);
        else if (ch == 'M') Put(sx, 13, cStoneBright);
    }

    // ========================================================================
    // LAYER 4: GOLDRIM at temple-hill seam (warm accent, not full row)
    // Sparse pixels mark the warm sun-hit transition between marble & hill top.
    // ========================================================================
    foreach (var rx in new[] { 8, 11, 14, 17, 20 })
        Put(rx, 14, cGoldRim);

    // ========================================================================
    // LAYER 5: CAST SHADOWS of columns onto the stylobate (sun upper-left)
    // Each column casts a 1-2 px shadow to the right on the hill top.
    // ========================================================================
    foreach (var leftX in new[] { 9, 12, 15, 18 })
    {
        Put(leftX + 2, 14, cStoneCast);  // shadow on stylobate
    }

    // ========================================================================
    // LAYER 6: FOREGROUND GRANITE BOULDERS (the embedding move)
    // Drawn LAST so they overwrite the temple base and hill front. The right
    // boulder INTRUDES into the rightmost column's base — that's the visual
    // proof that the temple is IN the island, not on it.
    // ========================================================================
    // Left boulder — sits in front of the hill flank
    Sprite(new (string, int)[]
    {
        ("  HRR                   ", 17),
        (" HRRrM                  ", 18),
        ("HRRRrMM                 ", 19),
        (" oooooo                 ", 20),
    }, ch => ch switch
    {
        'H' => cRockHi,
        'R' => cRockBase,
        'r' => cRockBase,
        'M' => cRockMid,
        'o' => cRockDark,
        _   => (Color?)null,
    });

    // Right boulder — INTRUDES into the temple zone (peaks at row 12)
    // Covers col 4's right side (col 19) at rows 12-13 → column visibly leans
    // into the rock.
    Sprite(new (string, int)[]
    {
        ("                   HR   ", 12),  // peak intrudes into column body
        ("                  HRRM  ", 13),  // covers col 4 base right
        ("                 HRRRMM ", 14),  // covers stylobate right
        ("                HRRRRMMM", 15),
        ("                HRRrMMMM", 16),
        ("                 oooooo ", 17),
    }, ch => ch switch
    {
        'H' => cRockHi,
        'R' => cRockBase,
        'r' => cRockBase,
        'M' => cRockMid,
        'o' => cRockDark,
        _   => (Color?)null,
    });

    // Center foreground boulder — small, bites up to row 16
    Sprite(new (string, int)[]
    {
        ("           HRRR         ", 18),
        ("          HRRRrMM       ", 19),
        ("           ooooo        ", 20),
    }, ch => ch switch
    {
        'H' => cRockHi,
        'R' => cRockBase,
        'r' => cRockBase,
        'M' => cRockMid,
        'o' => cRockDark,
        _   => (Color?)null,
    });

    // ========================================================================
    // LAYER 7: FOAM RING at the waterline contact
    // ========================================================================
    foreach (var fx in new[] { 1, 6, 10, 15, 19, 22 }) Put(fx, 21, cWaterFoam);
    foreach (var fx in new[] { 3, 8, 13, 17, 21 })     Put(fx, 22, cWaterFoam);
};
