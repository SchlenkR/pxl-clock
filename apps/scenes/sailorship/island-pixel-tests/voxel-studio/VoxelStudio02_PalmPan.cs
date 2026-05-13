// ---
// app: VoxelStudio02PalmPan
// displayName: Voxel Studio 02 Palm Pan
// author: Ronald Schlenker
// description: Standalone voxel palm island, static iso shot. Tweak yawDeg / pitchDeg for camera angle and offsetX / offsetZ (world-space, voxel units) to shift the model in 3D before projection. The .vox model is embedded as a text literal at the top of the file and parsed inline (material/box/v directives). The renderer is a self-contained iso projector with painter's algorithm — no shared lib needed. Sky+water gradient background.
// appType: Scene
// duration: 60
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

const int W = 24;
const int H = 24;

// ============================================================================
// TUNE THESE — camera + composition
//   yawDeg, pitchDeg     → camera angle (yaw around Y, pitch tilt)
//   offsetX, offsetZ     → CAMERA-SPACE shift in voxel units (applied AFTER
//                           yaw, BEFORE pitch). +X = horizontal-right in the
//                           projected image. +Z = "deeper into the scene"
//                           (model wanders backward / upward, at offsetZ=0
//                           the model bottom sits exactly on the horizon).
//   scale                → pixels per voxel
// The model is anchored so that its lowest voxel (y = minY) sits on the
// sky/water horizon at screen-Y = horizonY. offsetZ then pushes the island
// "into" the scene from there.
// ============================================================================
const double yawDeg   = 25.0;
const double pitchDeg = 22.0;
const double offsetX  = 0.0;        // camera-X shift (voxels)
const double offsetZ  = 0.0;        // camera-Z shift (voxels, +back/up from horizon)
const double scale    = 1.0;        // pixels per voxel

// ============================================================================
// EMBEDDED .VOX MODEL — currently tiny-palm-isle.vox (14 × 6 × 14)
// Swap this string with the contents of any .vox file from
// tools/voxel-studio/Models/ to render a different scene.
// ============================================================================
const string VoxSource = """
material sand    top=#FFE5B0  front=#A88B52  side=#7E6740
material wetsand top=#E8C880  front=#9C7E48  side=#6A5630
material trunk   top=#854E2C  front=#4F2E14  side=#3A2210
material trunkdk top=#5A3418  front=#36200E  side=#23150A
material frondlt top=#7AE040  front=#3D8B22  side=#225E15
material frond   top=#3D8B22  front=#225E15  side=#163E0E
material coco    top=#6B3818  front=#3D2210  side=#251608

box 2 0 1   12 0 4   sand
box 3 0 0   11 0 5   sand
box 1 0 2   13 0 3   sand
box 0 0 2   0 0 3    wetsand
box 14 0 2  14 0 3   wetsand

box 5 1 1   10 1 4   sand
box 6 1 0   9 1 5    sand
box 6 2 1   9 2 4    sand
v 7 3 2   sand
v 7 3 3   sand
v 8 3 2   sand
v 8 3 3   sand

box 7 4 2   8 10 3   trunk
v 8 5 3   trunkdk
v 8 7 3   trunkdk
v 8 9 3   trunkdk

box 4 11 2   11 11 3   frond
box 7 11 0   8 11 5    frond
box 5 11 1   10 11 4   frond
box 6 11 1   9 11 4    frondlt

box 5 12 1   10 12 4   frondlt
box 6 12 0   9 12 5    frondlt
box 7 12 2   8 12 3    frondlt

box 7 13 2   8 13 3    frondlt

v 3 11 2   frond
v 3 11 3   frond
v 12 11 2  frond
v 12 11 3  frond
v 7 11 6   frond
v 8 11 6   frond

v 5 11 2  coco
v 10 11 3 coco
""";

// ============================================================================
// .VOX PARSER — handles `material name top=#... front=#... side=#...`,
// `box X1 Y1 Z1   X2 Y2 Z2   matname`, and `v X Y Z matname`.
// Trims `# comment` lines / inline trailing comments.
// ============================================================================
Color HexToColor(string hex)
{
    var h = hex.TrimStart('#');
    var r = Convert.ToInt32(h.Substring(0, 2), 16) / 255.0;
    var g = Convert.ToInt32(h.Substring(2, 2), 16) / 255.0;
    var b = Convert.ToInt32(h.Substring(4, 2), 16) / 255.0;
    return Color.FromRgb(r, g, b);
}

var matIndex = new Dictionary<string, int>();
var matTop   = new List<Color>();
var matFront = new List<Color>();
var matSide  = new List<Color>();
var voxels   = new List<(int x, int y, int z, int mat)>();

foreach (var rawLine in VoxSource.Split('\n'))
{
    var line = rawLine;
    // strip inline `#` comments (but keep `#RRGGBB` colour tokens — those are
    // never preceded by whitespace, so we only cut comment-`#` after a space)
    var spaceHash = line.IndexOf(" #");
    if (spaceHash >= 0) line = line.Substring(0, spaceHash);
    line = line.Trim();
    if (line.Length == 0 || line.StartsWith("#")) continue;

    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0) continue;

    if (parts[0] == "material" && parts.Length >= 3)
    {
        var name = parts[1];
        Color top = default, front = default, side = default;
        bool hasTop = false, hasFront = false, hasSide = false;
        for (int i = 2; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.StartsWith("top="))   { top   = HexToColor(p.Substring(4)); hasTop   = true; }
            else if (p.StartsWith("front=")) { front = HexToColor(p.Substring(6)); hasFront = true; }
            else if (p.StartsWith("side="))  { side  = HexToColor(p.Substring(5)); hasSide  = true; }
        }
        if (!hasFront && hasTop)  front = top;
        if (!hasSide  && hasFront) side = front;
        if (!hasSide  && hasTop)   side = top;
        matIndex[name] = matTop.Count;
        matTop.Add(top);
        matFront.Add(front);
        matSide.Add(side);
    }
    else if (parts[0] == "box" && parts.Length >= 8)
    {
        int x1 = int.Parse(parts[1]), y1 = int.Parse(parts[2]), z1 = int.Parse(parts[3]);
        int x2 = int.Parse(parts[4]), y2 = int.Parse(parts[5]), z2 = int.Parse(parts[6]);
        var matIdx = matIndex[parts[7]];
        for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
        for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
        for (int z = Math.Min(z1, z2); z <= Math.Max(z1, z2); z++)
            voxels.Add((x, y, z, matIdx));
    }
    else if (parts[0] == "v" && parts.Length >= 5)
    {
        int x = int.Parse(parts[1]), y = int.Parse(parts[2]), z = int.Parse(parts[3]);
        var matIdx = matIndex[parts[4]];
        voxels.Add((x, y, z, matIdx));
    }
}

// ============================================================================
// PRECOMPUTE: model bounds + occluder set (for top-face exposure check)
// ============================================================================
int minX = int.MaxValue, maxX = int.MinValue;
int minY = int.MaxValue, maxY = int.MinValue;
int minZ = int.MaxValue, maxZ = int.MinValue;
foreach (var v in voxels)
{
    if (v.x < minX) minX = v.x; if (v.x > maxX) maxX = v.x;
    if (v.y < minY) minY = v.y; if (v.y > maxY) maxY = v.y;
    if (v.z < minZ) minZ = v.z; if (v.z > maxZ) maxZ = v.z;
}
double cmX = (minX + maxX + 1) * 0.5;
double cmY = (minY + maxY + 1) * 0.5;
double cmZ = (minZ + maxZ + 1) * 0.5;

var voxelSet = new HashSet<(int, int, int)>();
foreach (var v in voxels) voxelSet.Add((v.x, v.y, v.z));

// Sky+water palette
var cSkyTop      = Color.FromRgb(0.62, 0.88, 1.00);
var cSkyHorizon  = Color.FromRgb(0.85, 0.95, 1.00);
var cWaterBright = Color.FromRgb(0.30, 0.78, 0.74);
var cWaterDeep   = Color.FromRgb(0.10, 0.50, 0.55);
const int horizonY = 9;             // sky above this row, water below

Color LerpColor(Color a, Color b, double t)
{
    if (t < 0) t = 0; else if (t > 1) t = 1;
    return new Color(
        a.R + (b.R - a.R) * t,
        a.G + (b.G - a.G) * t,
        a.B + (b.B - a.B) * t);
}

// ============================================================================
// SCENE
// ============================================================================
var scene = (RasterSurface ctx) =>
{
    var t = ctx.Elapsed.TotalSeconds;
    _ = t;  // time isn't used (static shot) — kept for easy re-introduction of animation
    var yawRad = yawDeg * Math.PI / 180.0;

    // ---- Background gradient (sky on top, water on bottom)
    for (int y = 0; y < H; y++)
    {
        Color c;
        if (y < horizonY)
        {
            var f = horizonY <= 1 ? 0.0 : y / (double)(horizonY - 1);
            c = LerpColor(cSkyTop, cSkyHorizon, f);
        }
        else
        {
            var f = (H - 1 - horizonY) <= 0 ? 0.0 : (y - horizonY) / (double)(H - 1 - horizonY);
            c = LerpColor(cWaterBright, cWaterDeep, f);
        }
        for (int x = 0; x < W; x++) ctx.SetPixel(x, y, c);
    }

    // ---- Iso projection
    var pitchRad = pitchDeg * Math.PI / 180.0;
    var cosY = Math.Cos(yawRad);
    var sinY = Math.Sin(yawRad);
    var cosP = Math.Cos(pitchRad);
    var sinP = Math.Sin(pitchRad);

    // Anchor: at offsetZ=0 the model's bottom voxel sits on the BOTTOM
    // edge of the frame ("island in the foreground"). Increasing offsetZ
    // pushes it backward through the horizon toward the upper edge.
    //   bottom-voxel dy = (minY + 0.5) - cmY   (negative half-height)
    //   bottom-voxel ryProj (no offset) = dy * cosP
    //   syF = -ryProj * scale + screenCY → set syF = (H-1) → solve for screenCY
    var anchorDy = (minY + 0.5) - cmY;
    var anchorRy = anchorDy * cosP;
    var screenCX = W * 0.5;
    var screenCY = (H - 1) + anchorRy * scale;

    // Project all voxels (yaw → camera-space offset → pitch → orthographic)
    var projected = new List<(double sxF, double syF, int matIdx, bool topExp, double depth)>(voxels.Count);
    foreach (var v in voxels)
    {
        var dx = v.x + 0.5 - cmX;
        var dy = v.y + 0.5 - cmY;
        var dz = v.z + 0.5 - cmZ;
        // Yaw around Y axis
        var rxYaw = dx * cosY - dz * sinY;
        var rzYaw = dx * sinY + dz * cosY;
        // Camera-space offset (applied between yaw and pitch so it's
        // independent of yaw — +X stays "right in image", +Z stays "into
        // the scene", which becomes vertical in the image after pitch).
        rxYaw += offsetX;
        rzYaw += offsetZ;
        // Pitch around X axis
        var ryProj =  dy * cosP + rzYaw * sinP;
        var rzProj = -dy * sinP + rzYaw * cosP;
        // Orthographic project (Y-up on screen → invert)
        var sxF = rxYaw   * scale + screenCX;
        var syF = -ryProj * scale + screenCY;
        var topExp = !voxelSet.Contains((v.x, v.y + 1, v.z));
        projected.Add((sxF, syF, v.mat, topExp, rzProj));
    }
    // Painter sort: back-to-front (highest rzProj = farthest)
    projected.Sort((a, b) => b.depth.CompareTo(a.depth));

    // ---- Draw 1 pixel per voxel (top-exposed → top colour, else front)
    foreach (var (sxF, syF, matIdx, topExp, _) in projected)
    {
        int sx = (int)Math.Round(sxF);
        int sy = (int)Math.Round(syF);
        if (sx < 0 || sx >= W || sy < 0 || sy >= H) continue;
        var c = topExp ? matTop[matIdx] : matFront[matIdx];
        ctx.SetPixel(sx, sy, c);
    }
};
