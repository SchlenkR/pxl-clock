using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace VoxelStudio;

public readonly record struct Voxel(int X, int Y, int Z, int MaterialIndex);

public readonly record struct Material(string Name, uint Top, uint Front, uint Side);

public sealed class VoxModel
{
    public List<Material> Materials { get; } = new();
    public List<Voxel> Voxels { get; } = new();

    public int IndexOfMaterial(string name)
    {
        for (var i = 0; i < Materials.Count; i++)
            if (Materials[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }
}

public sealed class VoxParseException : Exception
{
    public int LineNumber { get; }
    public VoxParseException(int lineNumber, string message) : base($"line {lineNumber}: {message}")
    {
        LineNumber = lineNumber;
    }
}

public static class VoxFormat
{
    public static VoxModel Parse(string source)
    {
        var model = new VoxModel();
        var reader = new StringReader(source);
        var lineNo = 0;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Tokenize, then drop any tokens from the first comment marker on.
            // A token that begins with '#' is treated as the start of a comment
            // UNLESS it looks exactly like a #RRGGBB hex colour (so material
            // colour values are preserved).
            var allTokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            var dataCount = 0;
            foreach (var tok in allTokens)
            {
                if (tok.Length > 0 && tok[0] == '#' && !IsHexColorToken(tok)) break;
                dataCount++;
            }
            if (dataCount == 0) continue;
            var tokens = new string[dataCount];
            Array.Copy(allTokens, tokens, dataCount);

            switch (tokens[0].ToLowerInvariant())
            {
                case "material":
                    ParseMaterial(model, tokens, lineNo);
                    break;
                case "box":
                    ParseBox(model, tokens, lineNo);
                    break;
                case "v":
                case "voxel":
                    ParseVoxel(model, tokens, lineNo);
                    break;
                default:
                    throw new VoxParseException(lineNo, $"unknown directive '{tokens[0]}'");
            }
        }
        DedupeVoxels(model);
        return model;
    }

    // Two voxels at the same (x,y,z) cause unstable depth-sort flicker in the
    // renderer (the "coconut hidden in foliage" pattern). Resolve by letting
    // the LAST definition win — matches the intent of patterns like:
    //   box 5 10 5 9 10 5 frond   # row of leaves
    //   v   5 10 5 coco           # coconut OVERRIDES leaf at (5,10,5)
    static void DedupeVoxels(VoxModel model)
    {
        var seen = new Dictionary<(int, int, int), int>(model.Voxels.Count);
        for (var i = 0; i < model.Voxels.Count; i++)
        {
            var v = model.Voxels[i];
            seen[(v.X, v.Y, v.Z)] = i;
        }
        if (seen.Count == model.Voxels.Count) return;   // no duplicates

        var keep = new bool[model.Voxels.Count];
        foreach (var idx in seen.Values) keep[idx] = true;

        var compacted = new List<Voxel>(seen.Count);
        for (var i = 0; i < model.Voxels.Count; i++)
            if (keep[i]) compacted.Add(model.Voxels[i]);

        model.Voxels.Clear();
        model.Voxels.AddRange(compacted);
    }

    static void ParseMaterial(VoxModel model, string[] tokens, int lineNo)
    {
        // material NAME top=#RRGGBB front=#RRGGBB side=#RRGGBB
        if (tokens.Length < 5)
            throw new VoxParseException(lineNo, "expected: material NAME top=#... front=#... side=#...");
        var name = tokens[1];
        uint? top = null, front = null, side = null;
        for (var i = 2; i < tokens.Length; i++)
        {
            var kv = tokens[i].Split('=', 2);
            if (kv.Length != 2)
                throw new VoxParseException(lineNo, $"expected key=value, got '{tokens[i]}'");
            var color = ParseHexColor(kv[1], lineNo);
            switch (kv[0].ToLowerInvariant())
            {
                case "top":   top = color;   break;
                case "front": front = color; break;
                case "side":  side = color;  break;
                default: throw new VoxParseException(lineNo, $"unknown material key '{kv[0]}'");
            }
        }
        if (top is null || front is null || side is null)
            throw new VoxParseException(lineNo, "material requires top, front, and side colors");
        model.Materials.Add(new Material(name, top.Value, front.Value, side.Value));
    }

    static void ParseBox(VoxModel model, string[] tokens, int lineNo)
    {
        // box X1 Y1 Z1 X2 Y2 Z2 MAT
        if (tokens.Length != 8)
            throw new VoxParseException(lineNo, "expected: box X1 Y1 Z1 X2 Y2 Z2 MATNAME");
        var x1 = ParseInt(tokens[1], lineNo);
        var y1 = ParseInt(tokens[2], lineNo);
        var z1 = ParseInt(tokens[3], lineNo);
        var x2 = ParseInt(tokens[4], lineNo);
        var y2 = ParseInt(tokens[5], lineNo);
        var z2 = ParseInt(tokens[6], lineNo);
        var matName = tokens[7];
        var matIdx = model.IndexOfMaterial(matName);
        if (matIdx < 0) throw new VoxParseException(lineNo, $"unknown material '{matName}'");
        var (xMin, xMax) = (Math.Min(x1, x2), Math.Max(x1, x2));
        var (yMin, yMax) = (Math.Min(y1, y2), Math.Max(y1, y2));
        var (zMin, zMax) = (Math.Min(z1, z2), Math.Max(z1, z2));
        for (var x = xMin; x <= xMax; x++)
        for (var y = yMin; y <= yMax; y++)
        for (var z = zMin; z <= zMax; z++)
            model.Voxels.Add(new Voxel(x, y, z, matIdx));
    }

    static void ParseVoxel(VoxModel model, string[] tokens, int lineNo)
    {
        // v X Y Z MAT
        if (tokens.Length != 5)
            throw new VoxParseException(lineNo, "expected: v X Y Z MATNAME");
        var x = ParseInt(tokens[1], lineNo);
        var y = ParseInt(tokens[2], lineNo);
        var z = ParseInt(tokens[3], lineNo);
        var matName = tokens[4];
        var matIdx = model.IndexOfMaterial(matName);
        if (matIdx < 0) throw new VoxParseException(lineNo, $"unknown material '{matName}'");
        model.Voxels.Add(new Voxel(x, y, z, matIdx));
    }

    static int ParseInt(string s, int lineNo)
    {
        if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            throw new VoxParseException(lineNo, $"expected integer, got '{s}'");
        return v;
    }

    static bool IsHexColorToken(string s)
    {
        if (s.Length != 7 || s[0] != '#') return false;
        for (var i = 1; i < 7; i++)
        {
            var c = s[i];
            var ok = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!ok) return false;
        }
        return true;
    }

    static uint ParseHexColor(string s, int lineNo)
    {
        var t = s.StartsWith("#") ? s.Substring(1) : s;
        if (t.Length != 6 || !uint.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            throw new VoxParseException(lineNo, $"expected #RRGGBB color, got '{s}'");
        return 0xFF000000u | rgb;  // pack as ARGB with full alpha
    }
}
