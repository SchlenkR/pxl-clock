// ---
// app: Acropolis03DeepIsland
// displayName: Acropolis 03 Deep Island
// author: Ronald Schlenker
// description: Small acropolis sitting at the back of a deep perspectivally-tilted island. The island top is a trapezoidal grass-and-dirt plateau that runs from a narrow back edge (row 10) to a wide front edge (row 14), giving real depth in screen-Z. A limestone cliff face drops from the front edge to an organic waterline with bays and spits. Olive grove, cypress and a ruined column stub live on the plateau; foreground boulders intrude into the cliff. Sprite 60 px wide, scrolls in from left.
// appType: Scene
// duration: 60
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

const int canvasW = 24;
const int canvasH = 24;
const int spriteW = 60;
const double scrollDuration = 30.0;

// Subtle perspective offset for the small back-acropolis (depth to back face)
const int DX = 3;
const int DY = -2;

// ---- PALETTE --------------------------------------------------------------
var cSkyTop      = Color.FromRgb(0.55, 0.74, 0.96);
var cSkyHorizon  = Color.FromRgb(1.00, 0.90, 0.74);
var cWaterBright = Color.FromRgb(0.22, 0.58, 0.68);
var cWaterDeep   = Color.FromRgb(0.04, 0.22, 0.38);
var cWaterFoam   = Color.FromRgb(0.96, 0.99, 1.00);

// White limestone (cliff)
var cStoneHi     = Color.FromRgb(1.00, 0.98, 0.92);
var cStoneBright = Color.FromRgb(0.92, 0.86, 0.74);
var cStoneMid    = Color.FromRgb(0.70, 0.62, 0.50);
var cStoneDark   = Color.FromRgb(0.40, 0.32, 0.24);
var cStoneEdge   = Color.FromRgb(0.22, 0.16, 0.12);
var cStoneShade  = Color.FromRgb(0.55, 0.46, 0.36);
var cStoneCast   = Color.FromRgb(0.62, 0.52, 0.38);

// Grass + earth on the plateau
var cGrassHi     = Color.FromRgb(0.62, 0.82, 0.34);
var cGrassBright = Color.FromRgb(0.46, 0.72, 0.28);
var cGrassMid    = Color.FromRgb(0.30, 0.54, 0.22);
var cGrassDark   = Color.FromRgb(0.18, 0.36, 0.16);
var cEarthHi     = Color.FromRgb(0.78, 0.62, 0.38);
var cEarthMid    = Color.FromRgb(0.55, 0.42, 0.26);

// Granite boulders (foreground)
var cRockHi      = Color.FromRgb(0.62, 0.58, 0.54);
var cRockBase    = Color.FromRgb(0.42, 0.38, 0.36);
var cRockMid     = Color.FromRgb(0.24, 0.20, 0.20);
var cRockDark    = Color.FromRgb(0.10, 0.08, 0.08);

// Vegetation
var cCypressHi   = Color.FromRgb(0.30, 0.50, 0.30);
var cCypressDk   = Color.FromRgb(0.10, 0.26, 0.16);
var cTrunk       = Color.FromRgb(0.34, 0.24, 0.16);
var cOliveHi     = Color.FromRgb(0.55, 0.68, 0.28);
var cOliveDk     = Color.FromRgb(0.30, 0.42, 0.18);

var cGoldRim     = Color.FromRgb(0.85, 0.62, 0.22);

// ---- SMALL TEMPLE GEOMETRY ------------------------------------------------
// 3-column mini-acropolis. Sits on the BACK of the plateau.
const int FAX = 33;            // front apex x
const int FAY = 2;             // front apex y (sits high since temple is far back)
const int PED_H = 3;           // pediment 3 rows
const int PED_HW = 3;          // pediment half-width

int FBL = FAX - PED_HW;        // 30
int FBR = FAX + PED_HW;        // 36
int FBY = FAY + PED_H;         // 5  pediment base
int FAL = FBL - 1;             // 29 architrave left
int FAR = FBR + 1;             // 37 architrave right
int FA1Y = FBY + 1;            // 6
int FA2Y = FBY + 2;            // 7
int FCY  = FBY + 3;            // 8  cap row
int FCT  = FBY + 4;            // 9  column body top
int FCB  = FBY + 5;            // 10 column body bottom (2 rows)
int FBSY = FBY + 6;            // 11 bases (sits on plateau)

// 3 columns 2-wide, 1-px gaps. cols 30-31, 33-34, 36-37 (gaps at 32, 35)
var frontColLefts = new[] { 30, 33, 36 };
var backColLefts  = new[] { 30 + DX, 33 + DX, 36 + DX };

// ============================================================================
var scene = (RasterSurface ctx) =>
{
    var t = ctx.Elapsed.TotalSeconds;

    // Scroll: sprite enters from left, moves right, loops
    var totalDistance = spriteW + canvasW;
    var phase = (t % scrollDuration) / scrollDuration;
    var sox = -spriteW + (int)Math.Round(phase * totalDistance);

    void Put(int x, int y, Color c)
    {
        if (x >= 0 && x < canvasW && y >= 0 && y < canvasH) ctx.SetPixel(x, y, c);
    }

    void Pix(int spriteX, int y, Color c) => Put(sox + spriteX, y, c);

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

    void Pediment(int apexX, int apexY, int height, int halfW, Color edge, Color fill)
    {
        for (var dy = 0; dy <= height; dy++)
        {
            var y = apexY + dy;
            var w = (dy * halfW + height / 2) / height;
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
    // LAYER 1: ISLAND TOP (perspective trapezoid plateau, grass + earth)
    //   row 10: back edge (narrow)
    //   row 14: front edge (widest)
    // The trapezoid shape itself signals depth — back is further away.
    // ========================================================================
    SpriteRows(0, 10, new[]
    {
        // y=10 back edge of plateau (narrow), grass with outline
        ".........................oHHggggggggggGGGGo................",
        // y=11
        "......................oHHggggggggggggggGGGGGGo.............",
        // y=12 mid plateau, broader
        "..................oHHsgggggggggggggggggggGGGGGGGo..........",
        // y=13 broader still
        "..............oHHssgggggggggggggggggggggggGGGGGGGGGo.......",
        // y=14 widest = front-top edge of island, transitions to cliff
        "........oHHssssgggggggggggggggggggggggggggGGGGGGGGGGGGGo...",
    }, ch => ch switch
    {
        'H' => cGrassHi,
        's' => cGrassBright,
        'g' => cGrassBright,
        'G' => cGrassMid,
        'o' => cGrassDark,
        _   => (Color?)null,
    });

    // Earth-colored path winding from cliff-front to acropolis (suggests scale)
    Pix(28, 14, cEarthMid);
    Pix(29, 13, cEarthMid);
    Pix(30, 12, cEarthHi);
    Pix(31, 11, cEarthMid);
    Pix(32, 11, cEarthHi);

    // ========================================================================
    // LAYER 2: CLIFF FRONT (limestone, drops from row 14 to organic waterline)
    // Slightly wider than the plateau front edge so the cliff visually "wraps"
    // around the plateau (looks more solid).
    // ========================================================================
    SpriteRows(0, 15, new[]
    {
        // y=15 cliff just below plateau front
        "........oHHHHsssssssssssssssssssssssssssssssssssMMMMMo.....",
        // y=16
        "........oHsssssssmmmmmmmmmmmmmmmmmmmmmmmmmmMMMMMMMMMMMo....",
        // y=17 lower cliff, more shadow
        ".........ommmmmmmmmmddddddddddddddddddddddMMMMMMMMMMMo.....",
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

    // Organic waterline at y=18 — bays and spits, NOT a flat row
    SpriteRows(0, 18, new[]
    {
        // bays (gaps) and spits (extensions) along the waterline
        "..........oooo  ooooooo  oooooooooooo   ooooooo  ooooo.....",
    }, ch => ch switch
    {
        'o' => cStoneEdge,
        _   => (Color?)null,
    });

    // ========================================================================
    // LAYER 3: GOLDRIM at plateau-cliff seam (warm sun-hit accent line)
    // ========================================================================
    foreach (var rx in new[] { 14, 22, 30, 38, 46, 50 })
        Pix(rx, 14, cGoldRim);

    // ========================================================================
    // LAYER 4: VEGETATION on the plateau
    // ========================================================================

    // Cypress just left of acropolis (sprite col 27)
    Pix(27, 6, cCypressDk);
    for (var sy = 7; sy <= 10; sy++)
    {
        Pix(27, sy, cCypressHi);
        Pix(28, sy, cCypressDk);
    }
    Pix(27, 11, cTrunk);

    // Cypress right of acropolis (sprite col 39)
    Pix(39, 7, cCypressDk);
    for (var sy = 8; sy <= 10; sy++)
    {
        Pix(39, sy, cCypressHi);
        Pix(40, sy, cCypressDk);
    }
    Pix(39, 11, cTrunk);

    // Olive grove on mid-plateau (cluster of small bushes)
    void Olive(int cx, int cy)
    {
        Pix(cx,     cy,     cOliveHi);
        Pix(cx - 1, cy,     cOliveDk);
        Pix(cx + 1, cy,     cOliveDk);
        Pix(cx,     cy - 1, cOliveHi);
    }
    Olive(20, 13);
    Olive(23, 14);
    Olive(17, 14);
    Olive(45, 13);
    Olive(48, 14);

    // Ruined column stub on the front-mid plateau
    Pix(13, 12, cStoneHi);
    Pix(13, 13, cStoneBright);
    Pix(13, 14, cStoneMid);
    Pix(12, 14, cStoneEdge);
    Pix(14, 14, cStoneCast);

    // ========================================================================
    // LAYER 5: BACK-FACADE (subtle 3D depth for the small acropolis)
    // Just enough to show the temple has depth (back pediment apex peeks out,
    // small side wall on the right).
    // ========================================================================
    var BAX = FAX + DX; var BAY = FAY + DY;
    var BAL = FAL + DX; var BAR = FAR + DX;
    var BA1Y = FA1Y + DY; var BA2Y = FA2Y + DY;
    var BCY = FCY + DY;
    var BCT = FCT + DY; var BCB = FCB + DY;
    var BBSY = FBSY + DY;

    // Back pediment outline (just outline, no fill — keep mini)
    Pediment(BAX, BAY, PED_H, PED_HW, cStoneEdge, cStoneShade);

    // Back architrave + caps + bases (compact)
    RowFill(BA1Y, BAL, BAR, cStoneShade);
    RowFill(BA2Y, BAL, BAR, cStoneDark);
    RowFill(BCY,  BAL, BAR, cStoneShade);
    RowFill(BBSY, BAL, BAR, cStoneShade);
    foreach (var leftX in backColLefts)
        Column(leftX, BCT, BCB, cStoneShade, cStoneDark);

    // Roof slope (small, cols visible to right of front pediment)
    Pix(35, 1, cStoneBright);
    Pix(34, 2, cStoneBright); Pix(35, 2, cStoneMid); Pix(36, 2, cStoneMid);
    Pix(35, 3, cStoneBright); Pix(36, 3, cStoneMid); Pix(37, 3, cStoneMid); Pix(38, 3, cStoneEdge);
    Pix(36, 4, cStoneBright); Pix(37, 4, cStoneMid); Pix(38, 4, cStoneMid); Pix(39, 4, cStoneEdge);
    Pix(37, 5, cStoneMid); Pix(38, 5, cStoneEdge);

    // Side wall right
    RowFill(7, 38, 40, cStoneMid);
    RowFill(8, 38, 40, cStoneMid);
    RowFill(9, 38, 40, cStoneMid);
    RowFill(10, 38, 40, cStoneMid);
    RowFill(11, 38, 39, cStoneMid);
    Pix(40, 7, cStoneEdge); Pix(40, 8, cStoneEdge); Pix(40, 9, cStoneEdge); Pix(40, 10, cStoneEdge);

    // ========================================================================
    // LAYER 6: FRONT acropolis (drawn last in temple stack)
    // ========================================================================
    Pediment(FAX, FAY, PED_H, PED_HW, cStoneEdge, cStoneHi);

    RowFill(FA1Y, FAL, FAR, cStoneBright);
    RowFill(FA2Y, FAL, FAR, cStoneMid);
    Pix(FAL, FA1Y, cStoneEdge); Pix(FAR, FA1Y, cStoneEdge);
    Pix(FAL, FA2Y, cStoneEdge); Pix(FAR, FA2Y, cStoneEdge);

    RowFill(FCY, FAL, FAR, cStoneHi);

    foreach (var leftX in frontColLefts)
        Column(leftX, FCT, FCB, cStoneHi, cStoneMid);

    RowFill(FBSY, FAL, FAR, cStoneHi);

    // Cast shadows of columns on the plateau directly below the bases
    foreach (var leftX in frontColLefts) Pix(leftX + 2, FBSY, cStoneCast);

    // ========================================================================
    // LAYER 7: FOREGROUND BOULDERS at cliff-base (embedding into the island)
    // ========================================================================
    SpriteRows(8, 17, new[]
    {
        " HRR ",
        "HRrMM",
        "ooooo",
    }, ch => ch switch
    {
        'H' => cRockHi, 'R' => cRockBase, 'r' => cRockBase,
        'M' => cRockMid, 'o' => cRockDark, _ => (Color?)null,
    });

    SpriteRows(20, 18, new[]
    {
        "HRRRR",
        "RrrMM",
        "ooooo",
    }, ch => ch switch
    {
        'H' => cRockHi, 'R' => cRockBase, 'r' => cRockBase,
        'M' => cRockMid, 'o' => cRockDark, _ => (Color?)null,
    });

    SpriteRows(36, 17, new[]
    {
        "  HRR ",
        " HRRrM",
        "HRrrMM",
        "ooooo ",
    }, ch => ch switch
    {
        'H' => cRockHi, 'R' => cRockBase, 'r' => cRockBase,
        'M' => cRockMid, 'o' => cRockDark, _ => (Color?)null,
    });

    SpriteRows(50, 18, new[]
    {
        "HRR  ",
        "RrMM ",
        "ooooo",
    }, ch => ch switch
    {
        'H' => cRockHi, 'R' => cRockBase, 'r' => cRockBase,
        'M' => cRockMid, 'o' => cRockDark, _ => (Color?)null,
    });

    // ========================================================================
    // LAYER 8: FOAM RING tracking the organic waterline
    // ========================================================================
    foreach (var fx in new[] {  6, 11, 16, 22, 28, 34, 40, 46, 52, 56 })
        Pix(fx, 19, cWaterFoam);
    foreach (var fx in new[] {  9, 14, 19, 25, 31, 37, 43, 49, 54 })
        Pix(fx, 20, cWaterFoam);
    foreach (var fx in new[] { 12, 18, 27, 35, 42, 50 })
        Pix(fx, 21, cWaterFoam);
};
