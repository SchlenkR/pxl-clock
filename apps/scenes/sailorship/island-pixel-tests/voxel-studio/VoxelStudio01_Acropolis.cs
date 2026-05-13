// ---
// app: VoxelStudio01Acropolis
// displayName: Voxel Studio 01 Acropolis
// author: Ronald Schlenker
// description: Self-contained iso voxel renderer rebuilt from the SailboatOpenSea3D voxel system. True orthographic isometric projection (2:1, no perspective). The acropolis voxel model rotates continuously around the Y axis so all four sides become visible. Each voxel is a 2x2 pixel block with top face brighter than front face. Painter's algorithm sorts back-to-front. Faint floor grid below the model gives spatial reference. Single-file - safe to edit the voxel list inline to experiment with new shapes.
// appType: Scene
// duration: 60
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

const int canvasW = 24;
const int canvasH = 24;

// ============================================================================
// PALETTE
// ============================================================================
var cBgTop     = Color.FromRgb(0.06, 0.08, 0.14);    // studio dark top
var cBgBot     = Color.FromRgb(0.14, 0.16, 0.22);    // studio dark bottom
var cFloor     = Color.FromRgb(0.22, 0.24, 0.32);    // floor diamond grid

// Materials: top face (lit), front/side face (shadow)
var matTop = new[]
{
    Color.FromRgb(1.00, 0.96, 0.82),   // 0 sandstone top  (warm cream)
    Color.FromRgb(0.94, 0.92, 0.96),   // 1 marble top     (cool white)
    Color.FromRgb(0.55, 0.85, 0.30),   // 2 grass top
    Color.FromRgb(0.95, 0.78, 0.22),   // 3 gold accent top
};
var matFront = new[]
{
    Color.FromRgb(0.62, 0.52, 0.32),
    Color.FromRgb(0.55, 0.55, 0.62),
    Color.FromRgb(0.28, 0.50, 0.18),
    Color.FromRgb(0.62, 0.45, 0.10),
};

// ============================================================================
// VOXEL MODEL — small Acropolis. Edit this list to experiment with shapes.
// (x, y, z, mat) where y=up, x=right, z=back. Model fits roughly in 5x8x5.
// ============================================================================
var voxels = new List<(int x, int y, int z, int mat)>();

// ---- Stylobate (5x1x5 base at y=0) — sandstone
for (int x = 0; x < 5; x++)
for (int z = 0; z < 5; z++)
    voxels.Add((x, 0, z, 0));

// ---- Stepped second tier (3x1x3 centered at y=1)
for (int x = 1; x < 4; x++)
for (int z = 1; z < 4; z++)
    voxels.Add((x, 1, z, 0));

// ---- 8 columns (4 corners + 4 mid-edges) at y=2..4 — marble
var cols = new[]
{
    (0, 0), (4, 0), (0, 4), (4, 4),    // corners
    (2, 0), (2, 4), (0, 2), (4, 2),    // mid-edges
};
foreach (var (cx, cz) in cols)
    for (int sy = 2; sy <= 4; sy++)
        voxels.Add((cx, sy, cz, 1));

// ---- Architrave (5x1x5 at y=5) — sandstone
for (int x = 0; x < 5; x++)
for (int z = 0; z < 5; z++)
    voxels.Add((x, 5, z, 0));

// ---- Pediment / stepped roof
for (int x = 1; x < 4; x++)
for (int z = 1; z < 4; z++)
    voxels.Add((x, 6, z, 0));
voxels.Add((2, 7, 2, 3));  // gold finial

// ---- Precompute occluder set for top-exposure test
var voxelSet = new HashSet<(int, int, int)>();
foreach (var v in voxels) voxelSet.Add((v.x, v.y, v.z));

// Model center for rotation (geometric center of the X/Z extent)
const double cmX = 2.0;
const double cmZ = 2.0;

// ============================================================================
// SCENE
// ============================================================================
var scene = (RasterSurface ctx) =>
{
    var t = ctx.Elapsed.TotalSeconds;

    // Continuous Y-axis rotation. Full turn per `rotPeriod` seconds.
    var rotPeriod = 14.0;
    var angle = t * (Math.PI * 2.0 / rotPeriod);
    var cosA = Math.Cos(angle);
    var sinA = Math.Sin(angle);

    // ---- Background gradient (vertical)
    for (int y = 0; y < canvasH; y++)
    {
        var f = y / (double)(canvasH - 1);
        var c = new Color(
            cBgTop.R + (cBgBot.R - cBgTop.R) * f,
            cBgTop.G + (cBgBot.G - cBgTop.G) * f,
            cBgTop.B + (cBgBot.B - cBgTop.B) * f);
        for (int x = 0; x < canvasW; x++)
            ctx.SetPixel(x, y, c);
    }

    void Put(int x, int y, Color c)
    {
        if (x >= 0 && x < canvasW && y >= 0 && y < canvasH) ctx.SetPixel(x, y, c);
    }

    // ---- Iso projection parameters
    // 2:1 isometric: each voxel = 2x2 px on screen. X step = (+2, +1).
    // Z step = (-2, +1). Y step = (0, -2).
    const int voxStrideX = 2;   // horizontal pixels per X or Z voxel-step
    const int voxStrideY = 1;   // vertical pixels per X or Z voxel-step
    const int voxStrideUp = 2;  // vertical pixels per Y voxel-step

    int screenCX = canvasW / 2;       // 12
    int screenCY = canvasH - 6;       // 18: model anchored low so peak room above

    // ---- Floor grid (faint iso diamond at y=-1, just below stylobate)
    // Diamond extent matches the stylobate footprint plus 1 voxel border
    void DrawFloorDot(double dx, double dz)
    {
        var rx = dx * cosA - dz * sinA;
        var rz = dx * sinA + dz * cosA;
        var sxF = (rx - rz) * voxStrideX + screenCX;
        var syF = (rx + rz) * voxStrideY + 1 * voxStrideUp + screenCY;  // y=-1 plane
        Put((int)Math.Round(sxF),     (int)Math.Round(syF), cFloor);
        Put((int)Math.Round(sxF) + 1, (int)Math.Round(syF), cFloor);
    }
    for (int gx = -1; gx <= 5; gx++)
    for (int gz = -1; gz <= 5; gz++)
    {
        // Only draw the perimeter of the floor plane for clarity
        if (gx != -1 && gx != 5 && gz != -1 && gz != 5) continue;
        DrawFloorDot(gx - cmX, gz - cmZ);
    }

    // ---- Project all voxels (rotate + iso-project + record depth)
    var projected = new List<(double sxF, double syF, int mat, bool topExp, double depth)>();
    foreach (var v in voxels)
    {
        var dx = v.x - cmX;
        var dz = v.z - cmZ;
        // Rotate around Y axis
        var rx = dx * cosA - dz * sinA;
        var rz = dx * sinA + dz * cosA;
        var ry = (double)v.y;
        // Iso project
        var sxF = (rx - rz) * voxStrideX + screenCX;
        var syF = (rx + rz) * voxStrideY - ry * voxStrideUp + screenCY;
        // Top-exposed test (using ORIGINAL voxel coords — rotation doesn't change neighbours)
        var topExp = !voxelSet.Contains((v.x, v.y + 1, v.z));
        // Painter depth: voxels further from the camera drawn first.
        // After rotation, depth into the scene = (rx + rz) for SE-iso-camera.
        // Higher (rx+rz) = further away = drawn first.
        // Tie-break by Y so lower voxels draw before upper at same depth.
        var depth = (rx + rz) * 100.0 - ry;
        projected.Add((sxF, syF, v.mat, topExp, depth));
    }

    // Sort back-to-front
    projected.Sort((a, b) => b.depth.CompareTo(a.depth));

    // ---- Render each voxel as a 2x2 px block (top row = top face, bot row = front)
    foreach (var (sxF, syF, mat, topExp, _) in projected)
    {
        int sx = (int)Math.Round(sxF);
        int sy = (int)Math.Round(syF);
        var topC = topExp ? matTop[mat] : matFront[mat];
        var botC = matFront[mat];
        Put(sx,     sy,     topC);
        Put(sx + 1, sy,     topC);
        Put(sx,     sy + 1, botC);
        Put(sx + 1, sy + 1, botC);
    }
};
