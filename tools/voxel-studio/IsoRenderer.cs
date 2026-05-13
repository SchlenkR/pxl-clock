using System;
using System.Collections.Generic;

namespace VoxelStudio;

// World-space directional light + ambient. Light direction is anchored in
// the world, so rotating the camera reveals lit/shadowed sides correctly.
public sealed class LightingPreset
{
    public string Name { get; init; } = "";
    public double Ambient { get; init; } = 0.5;
    public double LightX { get; init; }
    public double LightY { get; init; } = 1.0;
    public double LightZ { get; init; }
    // Colour multiplier applied to the DIRECT (lit) contribution. Defaults
    // to neutral white. Use values <1 on a channel to suppress it (cool
    // light) or >1 to push it (warm/saturated light). Shadows stay neutral
    // because they're driven by Ambient only.
    public double TintR { get; init; } = 1.0;
    public double TintG { get; init; } = 1.0;
    public double TintB { get; init; } = 1.0;
    public override string ToString() => Name;
}

public static class LightingPresets
{
    // ASYMMETRIC X/Z light components so visible vertical faces at yaw=45°
    // get distinctly different brightnesses → 3D cylindrical look.
    public static readonly LightingPreset[] All = new[]
    {
        new LightingPreset { Name = "Studio L",  Ambient = 0.45, LightX = -0.75, LightY = 0.55, LightZ = -0.20 },
        new LightingPreset { Name = "Studio R",  Ambient = 0.45, LightX =  0.75, LightY = 0.55, LightZ = -0.20 },
        new LightingPreset { Name = "Noon",      Ambient = 0.40, LightX = -0.10, LightY = 0.95, LightZ = -0.05 },
        new LightingPreset { Name = "Golden",    Ambient = 0.45, LightX = -0.85, LightY = 0.30, LightZ = -0.30 },
        new LightingPreset { Name = "Overcast",  Ambient = 0.72, LightX = -0.50, LightY = 0.75, LightZ = -0.20 },
        new LightingPreset { Name = "Softbox",   Ambient = 0.60, LightX = -0.55, LightY = 0.65, LightZ = -0.30 },
        new LightingPreset { Name = "Drama L",   Ambient = 0.25, LightX = -0.90, LightY = 0.30, LightZ = -0.25 },
        new LightingPreset { Name = "Drama R",   Ambient = 0.25, LightX =  0.90, LightY = 0.30, LightZ = -0.25 },
        new LightingPreset { Name = "Twilight",  Ambient = 0.20, LightX = -0.55, LightY = 0.40, LightZ = -0.35 },
        new LightingPreset { Name = "Backlit",   Ambient = 0.35, LightX =  0.30, LightY = 0.55, LightZ =  0.75 },
        new LightingPreset { Name = "Front",     Ambient = 0.30, LightX = -0.20, LightY = 0.40, LightZ = -0.90 },
        // ---- New tinted presets (warm vs cool light colour) ----
        // Sunrise: warm orange light from low east, slightly cool ambient
        new LightingPreset { Name = "Sunrise",   Ambient = 0.45, LightX =  0.85, LightY = 0.20, LightZ = -0.15,
                             TintR = 1.15, TintG = 0.95, TintB = 0.75 },
        // Sunset: golden from low west, warm but not blown out
        new LightingPreset { Name = "Sunset",    Ambient = 0.40, LightX = -0.85, LightY = 0.20, LightZ =  0.10,
                             TintR = 1.20, TintG = 0.90, TintB = 0.65 },
        // Tropical Noon: bright warm-ish overhead, deep saturated shadows
        new LightingPreset { Name = "Tropical",  Ambient = 0.55, LightX = -0.20, LightY = 0.90, LightZ = -0.20,
                             TintR = 1.08, TintG = 1.04, TintB = 0.96 },
        // Lagoon: cool blue-green soft daylight (overcast over shallow water)
        new LightingPreset { Name = "Lagoon",    Ambient = 0.65, LightX = -0.40, LightY = 0.65, LightZ = -0.35,
                             TintR = 0.88, TintG = 1.00, TintB = 1.08 },
        // Magic Hour: pinkish low warm light, dim ambient
        new LightingPreset { Name = "Magic Hr",  Ambient = 0.35, LightX = -0.60, LightY = 0.25, LightZ = -0.55,
                             TintR = 1.18, TintG = 0.92, TintB = 0.98 },
        // Moonlit: dim cold light from high, deep cool shadows
        new LightingPreset { Name = "Moonlit",   Ambient = 0.22, LightX = -0.30, LightY = 0.80, LightZ = -0.30,
                             TintR = 0.70, TintG = 0.85, TintB = 1.18 },
    };
}

// Classic voxel renderer: each voxel is a real world-aligned cube. The 3
// visible world-faces are projected as filled convex quads in screen space
// (orthographic). Per-face brightness is Lambert against a world-fixed sun
// + ambient. Per-voxel AO and hard-shadow ray modulate the final colour.
//
// Camera: orbit (yaw + pitch), no roll. Y is world-up.
public static class IsoRenderer
{
    const int FACE_PX = 0;   // +X (east face of voxel)
    const int FACE_NX = 1;   // -X
    const int FACE_PZ = 2;   // +Z (back)
    const int FACE_NZ = 3;   // -Z (front)
    const int FACE_PY = 4;   // +Y (top)
    const int FACE_NY = 5;   // -Y (bottom)

    static readonly (double x, double y, double z)[] FaceNormals = new[]
    {
        ( 1.0,  0.0,  0.0),
        (-1.0,  0.0,  0.0),
        ( 0.0,  0.0,  1.0),
        ( 0.0,  0.0, -1.0),
        ( 0.0,  1.0,  0.0),
        ( 0.0, -1.0,  0.0),
    };

    static readonly (int dx, int dy, int dz)[] FaceNeighbourOffsets = new[]
    {
        ( 1,  0,  0),
        (-1,  0,  0),
        ( 0,  0,  1),
        ( 0,  0, -1),
        ( 0,  1,  0),
        ( 0, -1,  0),
    };

    // For each (face, vertex) → 3 AO neighbour offsets (side1, side2, corner).
    // Voxel order matches ProjectFaceVertices vertex order.
    // Index: face*12 + vertex*3 + neighbour_idx (0=side1, 1=side2, 2=corner)
    static readonly (int dx, int dy, int dz)[] AONeighbours = new (int, int, int)[]
    {
        // FACE_PX (face 0): outer side is +X, tangents are Y and Z
        ( 1,-1, 0), ( 1, 0,-1), ( 1,-1,-1),   // v0 (sign -Y, -Z)
        ( 1, 1, 0), ( 1, 0,-1), ( 1, 1,-1),   // v1 (sign +Y, -Z)
        ( 1, 1, 0), ( 1, 0, 1), ( 1, 1, 1),   // v2 (sign +Y, +Z)
        ( 1,-1, 0), ( 1, 0, 1), ( 1,-1, 1),   // v3 (sign -Y, +Z)

        // FACE_NX (face 1): outer side is -X
        (-1,-1, 0), (-1, 0, 1), (-1,-1, 1),   // v0
        (-1, 1, 0), (-1, 0, 1), (-1, 1, 1),   // v1
        (-1, 1, 0), (-1, 0,-1), (-1, 1,-1),   // v2
        (-1,-1, 0), (-1, 0,-1), (-1,-1,-1),   // v3

        // FACE_PZ (face 2): outer side is +Z
        ( 1, 0, 1), ( 0,-1, 1), ( 1,-1, 1),   // v0
        ( 1, 0, 1), ( 0, 1, 1), ( 1, 1, 1),   // v1
        (-1, 0, 1), ( 0, 1, 1), (-1, 1, 1),   // v2
        (-1, 0, 1), ( 0,-1, 1), (-1,-1, 1),   // v3

        // FACE_NZ (face 3): outer side is -Z
        (-1, 0,-1), ( 0,-1,-1), (-1,-1,-1),   // v0
        (-1, 0,-1), ( 0, 1,-1), (-1, 1,-1),   // v1
        ( 1, 0,-1), ( 0, 1,-1), ( 1, 1,-1),   // v2
        ( 1, 0,-1), ( 0,-1,-1), ( 1,-1,-1),   // v3

        // FACE_PY (face 4): outer side is +Y (top)
        (-1, 1, 0), ( 0, 1,-1), (-1, 1,-1),   // v0
        (-1, 1, 0), ( 0, 1, 1), (-1, 1, 1),   // v1
        ( 1, 1, 0), ( 0, 1, 1), ( 1, 1, 1),   // v2
        ( 1, 1, 0), ( 0, 1,-1), ( 1, 1,-1),   // v3

        // FACE_NY (face 5): outer side is -Y (bottom)
        (-1,-1, 0), ( 0,-1, 1), (-1,-1, 1),   // v0
        (-1,-1, 0), ( 0,-1,-1), (-1,-1,-1),   // v1
        ( 1,-1, 0), ( 0,-1,-1), ( 1,-1,-1),   // v2
        ( 1,-1, 0), ( 0,-1, 1), ( 1,-1, 1),   // v3
    };

    // AO per vertex: 1.0 = no occlusion, AO_FLOOR = fully occluded.
    // If both edge neighbours are solid the corner is fully blocked
    // (Minecraft rule — diagonal can't help when both sides are walls).
    const double AO_STRENGTH = 0.55;
    const double AO_FLOOR = 1.0 - AO_STRENGTH;
    static double ComputeVertexAO(bool s1, bool s2, bool corner)
    {
        if (s1 && s2) return AO_FLOOR;
        var n = (s1 ? 1 : 0) + (s2 ? 1 : 0) + (corner ? 1 : 0);
        return 1.0 - (n / 3.0) * AO_STRENGTH;
    }

    public static void Render(
        VoxModel model,
        double yawRad,
        double pitchRad,
        LightingPreset light,
        uint[] pixels,
        int width,
        int height,
        double scale,
        uint bgColor = 0xFF101418,
        bool clearBackground = true,
        bool showSunMarker = false,
        bool cameraLockedLight = false,
        double lightIntensity = 1.0,
        double panOffset = 0.0,
        double panScreenX = 0.0,
        double panScreenY = 0.0)
    {
        if (clearBackground)
            for (var i = 0; i < pixels.Length; i++) pixels[i] = bgColor;
        if (model.Voxels.Count == 0) return;

        // Bounds (for centering)
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;
        foreach (var v in model.Voxels)
        {
            if (v.X < minX) minX = v.X; if (v.X > maxX) maxX = v.X;
            if (v.Y < minY) minY = v.Y; if (v.Y > maxY) maxY = v.Y;
            if (v.Z < minZ) minZ = v.Z; if (v.Z > maxZ) maxZ = v.Z;
        }
        // Center on the mid of the cube spans (each voxel occupies a unit cube
        // from (vx, vy, vz) to (vx+1, vy+1, vz+1), so centroid is +0.5 offset).
        var cmX = (minX + maxX + 1) * 0.5;
        var cmY = (minY + maxY + 1) * 0.5;
        var cmZ = (minZ + maxZ + 1) * 0.5;

        // Pan along the model's LONG horizontal axis. panOffset ∈ [-1, 1].
        // -1 = scroll all the way to one end of the model, +1 = the other end.
        var xExtent = (maxX - minX) + 1;
        var zExtent = (maxZ - minZ) + 1;
        if (xExtent >= zExtent) cmX += panOffset * xExtent * 0.5;
        else                    cmZ += panOffset * zExtent * 0.5;

        var voxSet = new HashSet<(int, int, int)>();
        foreach (var v in model.Voxels) voxSet.Add((v.X, v.Y, v.Z));

        // Normalize light direction
        var lLen = Math.Sqrt(
            light.LightX * light.LightX +
            light.LightY * light.LightY +
            light.LightZ * light.LightZ);
        if (lLen < 1e-9) lLen = 1.0;
        var lxN = light.LightX / lLen;
        var lyN = light.LightY / lLen;
        var lzN = light.LightZ / lLen;

        var cosY = Math.Cos(yawRad);
        var sinY = Math.Sin(yawRad);
        var cosP = Math.Cos(pitchRad);
        var sinP = Math.Sin(pitchRad);

        // If camera-locked: treat the preset's direction as CAMERA-space and
        // inverse-rotate it into world space, so that as the camera spins,
        // the light spins with it (= constant lighting from the viewer's POV).
        double lx, ly, lz;
        if (cameraLockedLight)
        {
            var lyP = lyN * cosP + lzN * sinP;
            var lzP = -lyN * sinP + lzN * cosP;
            lx = lxN * cosY + lzP * sinY;
            lz = -lxN * sinY + lzP * cosY;
            ly = lyP;
        }
        else
        {
            lx = lxN; ly = lyN; lz = lzN;
        }

        // Hard shadow per voxel (world-space, camera-independent).
        // Returns 1.0 if lit, 0.0 if a voxel blocks the ray to the sun.
        var voxShadow = new double[model.Voxels.Count];
        ComputeShadow(model.Voxels, voxSet, lx, ly, lz, voxShadow);

        // Per-material × per-face base colour (raw, unshaded).
        // Shading is applied per-pixel: brightness = (ambient + direct) × AO.
        var matCount = model.Materials.Count;
        var faceBaseColor = new uint[matCount * 6];
        for (var m = 0; m < matCount; m++)
        {
            var mat = model.Materials[m];
            for (var f = 0; f < 6; f++)
            {
                faceBaseColor[m * 6 + f] = f switch
                {
                    FACE_PY => mat.Top,
                    FACE_NY => mat.Front,
                    _       => mat.Side,
                };
            }
        }

        // Lambert dot product per world-face (independent of voxel position)
        var faceDot = new double[6];
        for (var f = 0; f < 6; f++)
        {
            var (nx, ny, nz) = FaceNormals[f];
            var d = nx * lx + ny * ly + nz * lz;
            faceDot[f] = d > 0 ? d : 0;
        }

        // panScreenX/Y is a free 2D screen-space pan in pixels (e.g. middle-
        // mouse drag). It shifts the projected centroid; nothing else changes,
        // so the model is just translated on screen.
        var centerX = width  / 2.0 + panScreenX;
        var centerY = height / 2.0 + panScreenY;

        // Determine which of the 6 world-faces are facing the camera.
        // Project each face normal through yaw+pitch; visible if its post-
        // rotation z (= camera-z) is negative (= pointing toward viewer).
        var faceVisible = new bool[6];
        for (var f = 0; f < 6; f++)
        {
            var (nx, ny, nz) = FaceNormals[f];
            var rx = nx * cosY - nz * sinY;
            var rzYaw = nx * sinY + nz * cosY;
            var ry = ny;
            var rz2 = ry * sinP + rzYaw * cosP;
            faceVisible[f] = rz2 < -1e-6;
        }

        // FACE-LEVEL painter sort: build (voxIdx, faceIdx, depth) for every
        // visible non-occluded face, then sort by face-CENTER depth. This is
        // strictly more correct than voxel-center sort and fixes the
        // "single pixel changes color when rotating slightly" Z-fighting.
        var faceItems = new List<(int voxIdx, int faceIdx, double depth)>(model.Voxels.Count * 3);
        for (var i = 0; i < model.Voxels.Count; i++)
        {
            var v = model.Voxels[i];
            var dx = v.X + 0.5 - cmX;
            var dy = v.Y + 0.5 - cmY;
            var dz = v.Z + 0.5 - cmZ;
            for (var f = 0; f < 6; f++)
            {
                if (!faceVisible[f]) continue;
                var off = FaceNeighbourOffsets[f];
                if (voxSet.Contains((v.X + off.dx, v.Y + off.dy, v.Z + off.dz))) continue;

                var (nx, ny, nz) = FaceNormals[f];
                // Face center = voxel center + 0.5 × outward normal
                var fdx = dx + nx * 0.5;
                var fdy = dy + ny * 0.5;
                var fdz = dz + nz * 0.5;
                var rzYaw = fdx * sinY + fdz * cosY;
                var rz2 = fdy * sinP + rzYaw * cosP;
                // Stable tiebreak: identical depth → deterministic order
                var depth = rz2 * 1_000_000.0
                          - v.Y * 1_000.0
                          - v.X * 10.0
                          - v.Z
                          - f * 0.001;
                faceItems.Add((i, f, depth));
            }
        }
        faceItems.Sort((a, b) => b.depth.CompareTo(a.depth));

        // Reuse buffers for face vertex projection + per-vertex AO
        Span<double> vx = stackalloc double[4];
        Span<double> vy = stackalloc double[4];
        Span<double> vAO = stackalloc double[4];

        foreach (var item in faceItems)
        {
            var v = model.Voxels[item.voxIdx];
            var f = item.faceIdx;

            ProjectFaceVertices(v, f, cmX, cmY, cmZ,
                                cosY, sinY, cosP, sinP,
                                scale, centerX, centerY, vx, vy);

            // Smooth AO: per face vertex, look at 3 outer-side neighbours
            var aoBase = f * 12;
            for (var i = 0; i < 4; i++)
            {
                var s1Off = AONeighbours[aoBase + i * 3];
                var s2Off = AONeighbours[aoBase + i * 3 + 1];
                var cOff  = AONeighbours[aoBase + i * 3 + 2];
                var s1 = voxSet.Contains((v.X + s1Off.dx, v.Y + s1Off.dy, v.Z + s1Off.dz));
                var s2 = voxSet.Contains((v.X + s2Off.dx, v.Y + s2Off.dy, v.Z + s2Off.dz));
                var c  = voxSet.Contains((v.X + cOff.dx,  v.Y + cOff.dy,  v.Z + cOff.dz));
                vAO[i] = ComputeVertexAO(s1, s2, c);
            }

            var baseColor = faceBaseColor[v.MaterialIndex * 6 + f];
            // Standard additive lighting:
            //   brightness = (ambient + direct) × AO
            //   direct     = max(0, dot(N, L)) × sunIntensity × shadow
            // Ambient is uniform per-face. Shadow only blocks direct light.
            var directContrib = faceDot[f] * lightIntensity * voxShadow[item.voxIdx];
            FillConvexQuadAO(pixels, width, height, vx, vy, vAO,
                             baseColor, light.Ambient, directContrib,
                             light.TintR, light.TintG, light.TintB);
        }

        // Sun marker — visual proof that the light is anchored in WORLD space.
        // Draws a yellow disc at the world position the light comes FROM, so
        // when you rotate the camera the sun visibly stays put in the world.
        if (showSunMarker)
        {
            var sunDist = 8.0;
            var swx = lx * sunDist;   // light direction is "toward light" → sun lives there
            var swy = ly * sunDist;
            var swz = lz * sunDist;
            var rx = swx * cosY - swz * sinY;
            var rzYaw = swx * sinY + swz * cosY;
            var ry = swy;
            var ry2 = ry * cosP - rzYaw * sinP;
            var sxC = (int)Math.Round(rx * scale + centerX);
            var syC = (int)Math.Round(-ry2 * scale + centerY);
            var radius = Math.Max(3, (int)Math.Round(scale / 2));
            DrawDisc(pixels, width, height, sxC, syC, radius, 0xFFFFD040, 0xFFFFFFB0);
        }
    }

    static void DrawDisc(uint[] pixels, int w, int h, int cx, int cy, int radius, uint outer, uint inner)
    {
        var rOuter = radius * radius;
        var rInner = (radius - 1) * (radius - 1);
        for (var dy = -radius; dy <= radius; dy++)
        for (var dx = -radius; dx <= radius; dx++)
        {
            var d2 = dx * dx + dy * dy;
            if (d2 > rOuter) continue;
            var x = cx + dx;
            var y = cy + dy;
            if (x < 0 || x >= w || y < 0 || y >= h) continue;
            pixels[y * w + x] = d2 <= rInner ? inner : outer;
        }
    }

    // Project the 4 corners of one voxel-face into screen space.
    static void ProjectFaceVertices(
        Voxel v, int face,
        double cmX, double cmY, double cmZ,
        double cosY, double sinY, double cosP, double sinP,
        double scale, double centerX, double centerY,
        Span<double> outX, Span<double> outY)
    {
        // CCW vertex order in voxel-local coords (0..1 cube)
        Span<double> cornerX = stackalloc double[4];
        Span<double> cornerY = stackalloc double[4];
        Span<double> cornerZ = stackalloc double[4];
        switch (face)
        {
            case FACE_PX:
                cornerX[0]=1; cornerY[0]=0; cornerZ[0]=0;
                cornerX[1]=1; cornerY[1]=1; cornerZ[1]=0;
                cornerX[2]=1; cornerY[2]=1; cornerZ[2]=1;
                cornerX[3]=1; cornerY[3]=0; cornerZ[3]=1;
                break;
            case FACE_NX:
                cornerX[0]=0; cornerY[0]=0; cornerZ[0]=1;
                cornerX[1]=0; cornerY[1]=1; cornerZ[1]=1;
                cornerX[2]=0; cornerY[2]=1; cornerZ[2]=0;
                cornerX[3]=0; cornerY[3]=0; cornerZ[3]=0;
                break;
            case FACE_PZ:
                cornerX[0]=1; cornerY[0]=0; cornerZ[0]=1;
                cornerX[1]=1; cornerY[1]=1; cornerZ[1]=1;
                cornerX[2]=0; cornerY[2]=1; cornerZ[2]=1;
                cornerX[3]=0; cornerY[3]=0; cornerZ[3]=1;
                break;
            case FACE_NZ:
                cornerX[0]=0; cornerY[0]=0; cornerZ[0]=0;
                cornerX[1]=0; cornerY[1]=1; cornerZ[1]=0;
                cornerX[2]=1; cornerY[2]=1; cornerZ[2]=0;
                cornerX[3]=1; cornerY[3]=0; cornerZ[3]=0;
                break;
            case FACE_PY:
                cornerX[0]=0; cornerY[0]=1; cornerZ[0]=0;
                cornerX[1]=0; cornerY[1]=1; cornerZ[1]=1;
                cornerX[2]=1; cornerY[2]=1; cornerZ[2]=1;
                cornerX[3]=1; cornerY[3]=1; cornerZ[3]=0;
                break;
            case FACE_NY:
                cornerX[0]=0; cornerY[0]=0; cornerZ[0]=1;
                cornerX[1]=0; cornerY[1]=0; cornerZ[1]=0;
                cornerX[2]=1; cornerY[2]=0; cornerZ[2]=0;
                cornerX[3]=1; cornerY[3]=0; cornerZ[3]=1;
                break;
        }
        for (var i = 0; i < 4; i++)
        {
            var wx = v.X + cornerX[i];
            var wy = v.Y + cornerY[i];
            var wz = v.Z + cornerZ[i];
            var dxv = wx - cmX;
            var dyv = wy - cmY;
            var dzv = wz - cmZ;
            var rx = dxv * cosY - dzv * sinY;
            var rzYaw = dxv * sinY + dzv * cosY;
            var ry = dyv;
            var ry2 = ry * cosP - rzYaw * sinP;
            outX[i] = rx * scale + centerX;
            outY[i] = -ry2 * scale + centerY;
        }
    }

    // Scanline fill with per-vertex AO interpolation + additive lighting.
    //   brightness = (ambient + direct) × pixelAO
    // Ambient illuminates the face uniformly. Direct (Lambert × shadow) adds
    // on top. AO darkens both. Brightness > 1 lerps toward white (HDR).
    static void FillConvexQuadAO(uint[] pixels, int w, int h,
                                 ReadOnlySpan<double> vx, ReadOnlySpan<double> vy,
                                 ReadOnlySpan<double> vAO,
                                 uint baseColor, double ambient, double directContrib,
                                 double tintR, double tintG, double tintB)
    {
        double minY = double.MaxValue, maxY = double.MinValue;
        for (var i = 0; i < 4; i++)
        {
            if (vy[i] < minY) minY = vy[i];
            if (vy[i] > maxY) maxY = vy[i];
        }
        var y0 = Math.Max(0, (int)Math.Floor(minY));
        var y1 = Math.Min(h - 1, (int)Math.Ceiling(maxY));
        // Per-channel face brightness: ambient is neutral, direct gets the
        // light tint so warm sunset light makes lit faces orange but
        // shadowed faces stay neutral (or whatever the ambient evokes).
        var faceLightR = ambient + directContrib * tintR;
        var faceLightG = ambient + directContrib * tintG;
        var faceLightB = ambient + directContrib * tintB;
        // Skip per-channel work when the tint is neutral (hot-path fast path).
        var neutral = Math.Abs(tintR - 1) < 1e-6
                   && Math.Abs(tintG - 1) < 1e-6
                   && Math.Abs(tintB - 1) < 1e-6;
        var faceLight = ambient + directContrib;

        for (var y = y0; y <= y1; y++)
        {
            var ys = y + 0.5;
            double leftX = double.PositiveInfinity, rightX = double.NegativeInfinity;
            double leftAO = 0, rightAO = 0;
            for (var i = 0; i < 4; i++)
            {
                var x1 = vx[i];
                var y1f = vy[i];
                var ao1 = vAO[i];
                var ni = (i + 1) & 3;
                var x2 = vx[ni];
                var y2f = vy[ni];
                var ao2 = vAO[ni];
                if ((y1f <= ys && y2f > ys) || (y2f <= ys && y1f > ys))
                {
                    var t = (ys - y1f) / (y2f - y1f);
                    var x  = x1  + (x2  - x1)  * t;
                    var ao = ao1 + (ao2 - ao1) * t;
                    if (x < leftX)  { leftX  = x; leftAO  = ao; }
                    if (x > rightX) { rightX = x; rightAO = ao; }
                }
            }
            if (leftX > rightX) continue;
            // Pixel-center-inside-polygon rule: pixel x is "in" if its centre
            // (x+0.5) lies in [leftX, rightX]. This guarantees adjacent
            // polygons (sharing an edge) tile without gaps.
            var sx0 = Math.Max(0, (int)Math.Ceiling(leftX - 0.5));
            var sx1 = Math.Min(w - 1, (int)Math.Floor(rightX - 0.5));
            var widthSpan = rightX - leftX;
            for (var x = sx0; x <= sx1; x++)
            {
                var t = widthSpan < 1e-6 ? 0 : (x + 0.5 - leftX) / widthSpan;
                var pixelAO = leftAO + (rightAO - leftAO) * t;
                if (neutral)
                {
                    var brightness = faceLight * pixelAO;
                    pixels[y * w + x] = ApplyBrightness(baseColor, brightness);
                }
                else
                {
                    pixels[y * w + x] = ApplyTintedShading(
                        baseColor,
                        faceLightR * pixelAO,
                        faceLightG * pixelAO,
                        faceLightB * pixelAO);
                }
            }
        }
    }

    // Per-voxel hard shadow ray toward the world-space light.
    // Result: 1.0 = fully lit, 0.0 = blocked (in shadow). The ambient term
    // (added separately at fill time) provides the floor for shadowed pixels.
    static void ComputeShadow(
        List<Voxel> voxels,
        HashSet<(int, int, int)> voxSet,
        double lx, double ly, double lz,
        double[] result)
    {
        for (var i = 0; i < voxels.Count; i++)
        {
            var v = voxels[i];
            var rx = v.X + 0.5 + lx * 0.55;
            var ry = v.Y + 0.5 + ly * 0.55;
            var rz = v.Z + 0.5 + lz * 0.55;
            var inShadow = false;
            for (var step = 0; step < 18; step++)
            {
                var ix = (int)Math.Floor(rx);
                var iy = (int)Math.Floor(ry);
                var iz = (int)Math.Floor(rz);
                if ((ix != v.X || iy != v.Y || iz != v.Z) &&
                    voxSet.Contains((ix, iy, iz)))
                {
                    inShadow = true;
                    break;
                }
                rx += lx; ry += ly; rz += lz;
            }
            result[i] = inShadow ? 0.0 : 1.0;
        }
    }

    // Brightness < 1 → multiplicative darken (color * brightness).
    // Brightness > 1 → lerp toward WHITE so over-exposed faces actually
    // saturate to white instead of just boosting the strongest channel.
    // brightness = 2.0 → full white; >2.0 stays white.
    static uint ApplyBrightness(uint color, double brightness)
    {
        if (brightness >= 0.999 && brightness <= 1.001) return color;
        var a = (color >> 24) & 0xFF;
        var r = (color >> 16) & 0xFF;
        var g = (color >> 8) & 0xFF;
        var b = color & 0xFF;
        if (brightness <= 1.0)
        {
            var nr = ClampByte(r * brightness);
            var ng = ClampByte(g * brightness);
            var nb = ClampByte(b * brightness);
            return (a << 24) | (nr << 16) | (ng << 8) | nb;
        }
        else
        {
            var t = brightness - 1.0;
            if (t > 1.0) t = 1.0;
            var nr = ClampByte(r + t * (255.0 - r));
            var ng = ClampByte(g + t * (255.0 - g));
            var nb = ClampByte(b + t * (255.0 - b));
            return (a << 24) | (nr << 16) | (ng << 8) | nb;
        }
    }

    static uint ClampByte(double v)
    {
        if (v < 0) return 0;
        if (v > 255) return 255;
        return (uint)v;
    }

    // Per-channel version of ApplyBrightness for tinted lights.
    // Each channel of the base colour gets its own brightness factor, so
    // warm sunset light (R=1.4, G=0.85, B=0.55) colours up the lit faces
    // independently. Highlights (>1) lerp toward white per channel just
    // like ApplyBrightness.
    static uint ApplyTintedShading(uint color, double br, double bg, double bb)
    {
        var a = (color >> 24) & 0xFF;
        var r = (color >> 16) & 0xFF;
        var g = (color >> 8) & 0xFF;
        var b = color & 0xFF;
        return (a << 24) | (ChannelShade(r, br) << 16) | (ChannelShade(g, bg) << 8) | ChannelShade(b, bb);
    }

    static uint ChannelShade(uint c, double brightness)
    {
        if (brightness <= 1.0) return ClampByte(c * brightness);
        var t = Math.Min(1.0, brightness - 1.0);
        return ClampByte(c + t * (255.0 - c));
    }
}
