// ---
// app: Acropolis02Perspective
// displayName: Acropolis 02 Perspective
// author: Ronald Schlenker
// description: Acropolis in three-quarter perspective on a long limestone hill that scrolls horizontally across the canvas. Front facade has a triangular pediment and 4 columns; back facade is offset 4 cols right and 2 rows up, with a roof slope and side colonnade between them giving real 3D depth. Sprite is 60 px wide and scrolls in from left to right, looping. Cypress and ruined column on the left flank, foreground granite boulders intrude into the temple to lock it into the island.
// appType: Scene
// duration: 60
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

const int canvasW = 24;
const int canvasH = 24;
const int spriteW = 60;
const double scrollDuration = 30.0;

// Perspective depth: back layer is shifted right and up
const int DX = 4;
const int DY = -2;

// ---- PALETTE --------------------------------------------------------------
var cSkyTop      = Color.FromRgb(0.55, 0.74, 0.96);
var cSkyHorizon  = Color.FromRgb(1.00, 0.90, 0.74);
var cWaterBright = Color.FromRgb(0.22, 0.58, 0.68);
var cWaterDeep   = Color.FromRgb(0.04, 0.22, 0.38);
var cWaterFoam   = Color.FromRgb(0.96, 0.99, 1.00);

var cStoneHi     = Color.FromRgb(1.00, 0.98, 0.92);
var cStoneBright = Color.FromRgb(0.92, 0.86, 0.74);
var cStoneMid    = Color.FromRgb(0.70, 0.62, 0.50);
var cStoneDark   = Color.FromRgb(0.40, 0.32, 0.24);
var cStoneEdge   = Color.FromRgb(0.22, 0.16, 0.12);
var cStoneShade  = Color.FromRgb(0.55, 0.46, 0.36);   // back-layer tone
var cStoneCast   = Color.FromRgb(0.62, 0.52, 0.38);

var cRockHi      = Color.FromRgb(0.62, 0.58, 0.54);
var cRockBase    = Color.FromRgb(0.42, 0.38, 0.36);
var cRockMid     = Color.FromRgb(0.24, 0.20, 0.20);
var cRockDark    = Color.FromRgb(0.10, 0.08, 0.08);

var cCypressHi   = Color.FromRgb(0.30, 0.50, 0.30);
var cCypressDk   = Color.FromRgb(0.10, 0.26, 0.16);
var cTrunk       = Color.FromRgb(0.34, 0.24, 0.16);
var cOliveHi     = Color.FromRgb(0.55, 0.68, 0.28);
var cOliveDk     = Color.FromRgb(0.30, 0.42, 0.18);
var cGoldRim     = Color.FromRgb(0.85, 0.62, 0.22);

// ---- TEMPLE GEOMETRY ------------------------------------------------------
// Front: pediment apex at (34, 3), 5 rows tall, base 11 wide (cols 29-39).
// All other temple rows are derived.
const int FAX = 34;            // front apex x
const int FAY = 3;             // front apex y
const int PED_H = 5;           // pediment rows (apex to base)
const int PED_HW = 5;          // pediment half-width at base (slope 1 col/row)

int FBL = FAX - PED_HW;        // 29 front pediment-base left
int FBR = FAX + PED_HW;        // 39 front pediment-base right
int FBY = FAY + PED_H;         // 8  front pediment-base row
int FAL = FBL - 1;             // 28 architrave left (1 px overhang)
int FAR = FBR + 1;             // 40 architrave right
int FA1Y = FBY + 1;            // 9  architrave bright row
int FA2Y = FBY + 2;            // 10 architrave shadow row
int FCY  = FBY + 3;            // 11 cap row
int FCT  = FBY + 4;            // 12 column body top
int FCB  = FBY + 6;            // 14 column body bottom (3 rows)
int FBSY = FBY + 7;            // 15 base row
int FSTY = FBY + 8;            // 16 stylobate (= hill top)

var frontColLefts = new[] { 29, 32, 35, 38 };
var backColLefts  = new[] { 29 + DX, 32 + DX, 35 + DX, 38 + DX };  // 33,36,39,42

// ============================================================================
// SCENE
// ============================================================================
var scene = (RasterSurface ctx) =>
{
    var t = ctx.Elapsed.TotalSeconds;

    // Scroll: sprite enters from left edge, moves right, loops
    var totalDistance = spriteW + canvasW;
    var phase = (t % scrollDuration) / scrollDuration;
    var sox = -spriteW + (int)Math.Round(phase * totalDistance);

    void Put(int x, int y, Color c)
    {
        if (x >= 0 && x < canvasW && y >= 0 && y < canvasH) ctx.SetPixel(x, y, c);
    }

    // Sprite-x to screen-x conversion (handles scroll)
    void Pix(int spriteX, int y, Color c) => Put(sox + spriteX, y, c);

    // ---- LAYER 0: sky + water gradient (full canvas, no scroll)
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

    void SpriteRows(int baseX, int baseY, string[] rows, Func<char, Color?> map)
    {
        for (var ry = 0; ry < rows.Length; ry++)
        {
            var row = rows[ry];
            for (var rx = 0; rx < row.Length; rx++)
            {
                var ch = row[rx];
                if (ch == ' ' || ch == '.') continue;
                var c = map(ch);
                if (c.HasValue) Pix(baseX + rx, baseY + ry, c.Value);
            }
        }
    }

    // Triangular pediment: apex at (apexX, apexY), grows downward by `height`,
    // half-width at base = `halfW`. Slope = halfW/height col/row.
    void Pediment(int apexX, int apexY, int height, int halfW, Color edge, Color fill)
    {
        for (var dy = 0; dy <= height; dy++)
        {
            var y = apexY + dy;
            var w = (dy * halfW + height / 2) / height;  // rounded
            if (w == 0) Pix(apexX, y, edge);
            else
            {
                Pix(apexX - w, y, edge);
                Pix(apexX + w, y, edge);
                for (var x = apexX - w + 1; x < apexX + w; x++) Pix(x, y, fill);
            }
        }
    }

    void Column(int leftX, int topY, int botY, Color hi, Color shade)
    {
        for (var y = topY; y <= botY; y++)
        {
            Pix(leftX,     y, hi);
            Pix(leftX + 1, y, shade);
        }
    }

    void RowFill(int y, int leftX, int rightX, Color c)
    {
        for (var x = leftX; x <= rightX; x++) Pix(x, y, c);
    }

    // ========================================================================
    // LAYER 1: LONG LIMESTONE HILL (~50 wide, top at row 16 = stylobate)
    // ========================================================================
    SpriteRows(0, 16, new[]
    {
        ".....................oHHHHHHHHHHHHHHHHHHHHHHHo.............",  // y=16 hill top
        "..................oHHHHsssssssssssssssssssssssMMo..........",  // y=17
        "...............oHHHsssssssssssssssssssssssssssMMMMo........",  // y=18
        "............oHHsssssssssssssmmmmmmmmmmmmMMMMMMMMMMMMo......",  // y=19
        "..........oHsssssssmmmmmmmmmmmmmmddddddddMMMMMMMMMMMMo.....",  // y=20
        "........oommmmmmmmmmmmmmddddddddddddddddMMMMMMMMMMMMo......",  // y=21
        "........oooooooooooooooooooooooooooooooooooooooooo.........",  // y=22 base outline
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
    // LAYER 2: VEGETATION + small ruin LEFT of temple (gives the long hill life)
    // ========================================================================

    // Olive bushes far left
    void Olive(int cx, int cy)
    {
        Pix(cx,     cy,     cOliveHi);
        Pix(cx - 1, cy,     cOliveDk);
        Pix(cx + 1, cy,     cOliveDk);
        Pix(cx,     cy - 1, cOliveHi);
    }
    Olive( 9, 15);
    Olive(13, 14);
    Olive(16, 15);

    // Ruined column stub on the left (around sprite col 20)
    Pix(20, 13, cStoneHi);
    Pix(20, 14, cStoneBright);
    Pix(20, 15, cStoneMid);
    Pix(19, 16, cStoneEdge);
    Pix(20, 16, cStoneCast);
    Pix(21, 16, cStoneEdge);

    // Cypress just left of temple (sprite col 24)
    Pix(24, 8, cCypressDk);
    for (var sy = 9; sy <= 14; sy++)
    {
        Pix(24, sy, cCypressHi);
        Pix(25, sy, cCypressDk);
    }
    Pix(24, 15, cTrunk);

    // ========================================================================
    // LAYER 3: BACK TEMPLE (drawn first, parts visible through gaps + right)
    // Offset by (DX, DY) = (+4, -2) from front geometry.
    // ========================================================================
    var BAX = FAX + DX;        // 38
    var BAY = FAY + DY;        // 1
    var BBL = FBL + DX;        // 33
    var BBR = FBR + DX;        // 43
    var BBY = FBY + DY;        // 6
    var BAL = FAL + DX;        // 32
    var BAR = FAR + DX;        // 44
    var BA1Y = FA1Y + DY;      // 7
    var BA2Y = FA2Y + DY;      // 8
    var BCY  = FCY + DY;       // 9
    var BCT  = FCT + DY;       // 10
    var BCB  = FCB + DY;       // 12
    var BBSY = FBSY + DY;      // 13

    // Back pediment (darker shade, dark outline)
    Pediment(BAX, BAY, PED_H, PED_HW, cStoneEdge, cStoneShade);

    // Back architrave (2 rows)
    RowFill(BA1Y, BAL, BAR, cStoneShade);
    RowFill(BA2Y, BAL, BAR, cStoneDark);
    Pix(BAL, BA1Y, cStoneEdge); Pix(BAR, BA1Y, cStoneEdge);
    Pix(BAL, BA2Y, cStoneEdge); Pix(BAR, BA2Y, cStoneEdge);

    // Back caps (continuous abacus)
    RowFill(BCY, BAL, BAR, cStoneShade);

    // Back columns
    foreach (var leftX in backColLefts)
        Column(leftX, BCT, BCB, cStoneShade, cStoneDark);

    // Back bases
    RowFill(BBSY, BAL, BAR, cStoneShade);

    // ========================================================================
    // LAYER 4: ROOF SLOPE (right-side parallelogram between front+back pediment)
    // Hand-mapped pixel-by-pixel — slopes don't all land on integer grid, so
    // visible coverage is computed from the cleaner edges.
    //   y=2:  cols 36-39
    //   y=3:  cols 35-40 (front apex at 34 covers col 34)
    //   y=4:  cols 36-41
    //   y=5:  cols 37-42
    //   y=6:  cols 38-43
    //   y=7:  cols 39-41
    // Top edge (ridge) brighter, fill mid, right edge darker.
    // ========================================================================
    Pix(36, 2, cStoneBright);
    RowFill(2, 37, 39, cStoneMid);

    Pix(35, 3, cStoneBright);
    RowFill(3, 36, 39, cStoneMid);
    Pix(40, 3, cStoneEdge);

    Pix(36, 4, cStoneBright);
    RowFill(4, 37, 40, cStoneMid);
    Pix(41, 4, cStoneEdge);

    Pix(37, 5, cStoneBright);
    RowFill(5, 38, 41, cStoneMid);
    Pix(42, 5, cStoneEdge);

    Pix(38, 6, cStoneBright);
    RowFill(6, 39, 42, cStoneMid);
    Pix(43, 6, cStoneEdge);

    RowFill(7, 39, 41, cStoneDark);  // eave

    // ========================================================================
    // LAYER 5: SIDE WALL (right-side parallelogram, between architrave + bases)
    // Covers cols 40-44 vertically; back columns will be overdrawn next.
    // ========================================================================
    RowFill(9,  42, 44, cStoneMid);
    RowFill(10, 40, 44, cStoneMid);
    RowFill(11, 40, 44, cStoneMid);
    RowFill(12, 40, 44, cStoneMid);
    RowFill(13, 40, 44, cStoneMid);
    RowFill(14, 40, 42, cStoneMid);

    // Right back-edge (darker outline)
    for (var y = 9; y <= 13; y++) Pix(44, y, cStoneEdge);
    Pix(43, 13, cStoneEdge);
    Pix(42, 14, cStoneEdge);

    // Side-stylobate face (between back stylobate row 14 and front row 16)
    RowFill(15, 41, 45, cStoneShade);
    Pix(45, 15, cStoneEdge);

    // ========================================================================
    // LAYER 6: BACK COLUMNS RE-DRAW (so they show through the side wall)
    // Only the rightmost back column (cols 42-43) is meaningfully visible
    // beyond the front facade; redraw it on top of the side wall.
    // ========================================================================
    Column(42, BCT, BCB, cStoneShade, cStoneDark);

    // ========================================================================
    // LAYER 7: FRONT TEMPLE (drawn LAST in temple stack — covers back/wall)
    // ========================================================================

    // Front pediment (bright fill, dark outline)
    Pediment(FAX, FAY, PED_H, PED_HW, cStoneEdge, cStoneHi);

    // Front architrave (bright + shadow band, edge outline at sides)
    RowFill(FA1Y, FAL, FAR, cStoneBright);
    RowFill(FA2Y, FAL, FAR, cStoneMid);
    Pix(FAL, FA1Y, cStoneEdge); Pix(FAR, FA1Y, cStoneEdge);
    Pix(FAL, FA2Y, cStoneEdge); Pix(FAR, FA2Y, cStoneEdge);
    // Ruined notch
    Pix(34, FA2Y, cStoneEdge);

    // Front cap row (continuous abacus, bracket accents)
    RowFill(FCY, FAL, FAR, cStoneHi);
    foreach (var bx in new[] { 29, 32, 35, 38 }) Pix(bx + 1, FCY, cStoneBright);

    // Front column bodies
    foreach (var leftX in frontColLefts)
        Column(leftX, FCT, FCB, cStoneHi, cStoneMid);
    // One column with a chipped lower-right (character)
    Pix(36, FCB, cStoneEdge);

    // Front bases
    RowFill(FBSY, FAL, FAR, cStoneHi);
    foreach (var bx in new[] { 29, 32, 35, 38 }) Pix(bx + 1, FBSY, cStoneBright);

    // ========================================================================
    // LAYER 8: GOLDRIM accents at temple-hill seam, cast shadows of columns
    // ========================================================================
    foreach (var rx in new[] { 28, 31, 34, 37, 40 }) Pix(rx, FSTY, cGoldRim);
    foreach (var leftX in frontColLefts) Pix(leftX + 2, FSTY, cStoneCast);

    // ========================================================================
    // LAYER 9: FOREGROUND BOULDERS (embedding temple into the island)
    // ========================================================================
    // Boulder LEFT (sprite cols 4-9, in front of hill)
    SpriteRows(4, 18, new[]
    {
        " HRR ",
        "HRRrM",
        "HRrMM",
        "ooooo",
    }, ch => ch switch
    {
        'H' => cRockHi, 'R' => cRockBase, 'r' => cRockBase,
        'M' => cRockMid, 'o' => cRockDark, _ => (Color?)null,
    });

    // Boulder INTRUDING into temple right-base (sprite cols 39-43, rows 13-17)
    SpriteRows(39, 13, new[]
    {
        " HR  ",
        "HRRM ",
        "HRrMM",
        "RrrMM",
        "ooooo",
    }, ch => ch switch
    {
        'H' => cRockHi, 'R' => cRockBase, 'r' => cRockBase,
        'M' => cRockMid, 'o' => cRockDark, _ => (Color?)null,
    });

    // Center foreground boulder
    SpriteRows(28, 19, new[]
    {
        " HRR ",
        "HRrMM",
        "ooooo",
    }, ch => ch switch
    {
        'H' => cRockHi, 'R' => cRockBase, 'r' => cRockBase,
        'M' => cRockMid, 'o' => cRockDark, _ => (Color?)null,
    });

    // Boulder far right (sprite cols 49-53)
    SpriteRows(49, 18, new[]
    {
        "HRR  ",
        "RRrM ",
        "RrMM ",
        "ooooo",
    }, ch => ch switch
    {
        'H' => cRockHi, 'R' => cRockBase, 'r' => cRockBase,
        'M' => cRockMid, 'o' => cRockDark, _ => (Color?)null,
    });

    // ========================================================================
    // LAYER 10: FOAM RING along the waterline (sprite-relative)
    // ========================================================================
    foreach (var fx in new[] {  3,  8, 13, 19, 25, 31, 37, 43, 49, 54 })
        Pix(fx, 22, cWaterFoam);
    foreach (var fx in new[] {  6, 11, 16, 22, 28, 34, 40, 46, 52 })
        Pix(fx, 23, cWaterFoam);
};
