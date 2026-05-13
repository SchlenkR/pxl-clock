using System;
using System.Globalization;
using System.IO;
using SkiaSharp;
using VoxelStudio;

namespace VoxRender;

// Headless PNG renderer for .vox models. Wraps the same IsoRenderer the
// VoxelStudio GUI uses, then composites it on the sky+water gradient (or
// transparent) and writes a PNG.
//
// Usage:
//   dotnet run --project tools/vox-render -- INPUT.vox OUTPUT.png [opts]
// Options:
//   --size N          Render N×N (default 24, the PXL clock's native size)
//   --scale F         px-per-voxel (default 1.0 for 24px, 32.0 for studio)
//   --yaw DEG         Camera yaw    (default 344)
//   --pitch DEG       Camera pitch  (default 334)
//   --sun-yaw DEG     Sun yaw       (default 250)
//   --sun-pitch DEG   Sun pitch     (default 32)
//   --sun-pct N       Sun intensity 0..500 (default 0)
//   --ambient N       Ambient floor 0..2.5 (default 0.98)
//   --pan F           Pan offset -1..1 (default 0)
//   --bg both|sky|water|none  (default water for 24px, none for big)
//   --preset NAME     Lighting preset name (default "Studio R")
//   --led             LED-matrix style preview (cell+gap+shading)
//
// Examples:
//   # the same 24×24 the LED matrix shows
//   dotnet run --project tools/vox-render -- Models/palm-island-15.vox out.png
//   # the same view the studio shows (big iso, no background)
//   dotnet run --project tools/vox-render -- foo.vox big.png --size 600 --scale 32 --bg none

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length < 2 || args[0] is "-h" or "--help")
        {
            PrintHelp();
            return args.Length == 0 ? 1 : 0;
        }

        var input = args[0];
        var output = args[1];

        // Defaults matched to VoxelStudio's Pxl preview
        int size = 24;
        double scale = 1.0;
        double yaw = 344, pitch = 334;
        double sunYaw = 250, sunPitch = 32;
        double sunPct = 0;
        double ambient = 0.98;
        double pan = 0;
        string bg = "water";
        string presetName = "Studio R";
        bool led = false;

        for (var i = 2; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i]
                : throw new ArgumentException($"missing value after {args[i]}");
            switch (args[i])
            {
                case "--size":      size = int.Parse(Next()); break;
                case "--scale":     scale = ParseD(Next()); break;
                case "--yaw":       yaw = ParseD(Next()); break;
                case "--pitch":     pitch = ParseD(Next()); break;
                case "--sun-yaw":   sunYaw = ParseD(Next()); break;
                case "--sun-pitch": sunPitch = ParseD(Next()); break;
                case "--sun-pct":   sunPct = ParseD(Next()); break;
                case "--ambient":   ambient = ParseD(Next()); break;
                case "--pan":       pan = ParseD(Next()); break;
                case "--bg":        bg = Next(); break;
                case "--preset":    presetName = Next(); break;
                case "--led":       led = true; break;
                default:
                    Console.Error.WriteLine($"unknown option: {args[i]}");
                    return 2;
            }
        }

        // Auto-pick scale and bg defaults when --size differs from 24
        if (size != 24)
        {
            if (Math.Abs(scale - 1.0) < 1e-9) scale = 32.0;
            if (bg == "water") bg = "none";
        }

        var src = File.ReadAllText(input);
        var model = VoxFormat.Parse(src);

        // Pick a preset (mainly for ambient-default; we override the sun
        // direction with --sun-yaw/--sun-pitch like the GUI's drag does).
        LightingPreset basePreset = LightingPresets.All[1];   // Studio R
        for (var i = 0; i < LightingPresets.All.Length; i++)
        {
            if (LightingPresets.All[i].Name.Equals(presetName, StringComparison.OrdinalIgnoreCase))
                { basePreset = LightingPresets.All[i]; break; }
        }
        var (lx, ly, lz) = SunDir(sunYaw, sunPitch);
        var light = new LightingPreset
        {
            Name = basePreset.Name,
            Ambient = ambient,
            LightX = lx,
            LightY = ly,
            LightZ = lz,
        };

        var pixels = new uint[size * size];
        FillBackground(pixels, size, bg);

        var yawRad = yaw * Math.PI / 180.0;
        var pitchRad = pitch * Math.PI / 180.0;
        IsoRenderer.Render(model, yawRad, pitchRad, light, pixels, size, size, scale,
            clearBackground: bg == "none",
            lightIntensity: sunPct / 100.0,
            panOffset: pan);

        if (led)
            pixels = RenderLed(pixels, size, out size);

        SavePng(output, pixels, size, size);
        Console.WriteLine($"wrote {output} ({size}×{size}, model={Path.GetFileName(input)})");
        return 0;
    }

    static double ParseD(string s) => double.Parse(s, CultureInfo.InvariantCulture);

    static (double lx, double ly, double lz) SunDir(double yawDeg, double pitchDeg)
    {
        var yawR = yawDeg * Math.PI / 180.0;
        var pitR = pitchDeg * Math.PI / 180.0;
        var cp = Math.Cos(pitR);
        return (-Math.Sin(yawR) * cp, Math.Sin(pitR), -Math.Cos(yawR) * cp);
    }

    // Mirror VoxelStudio.MainWindow.FillPxlSkyAndWater + the LED preview,
    // so headless renders match what the GUI shows.
    static readonly uint cSkyTop      = MakeArgb(0.62, 0.88, 1.00);
    static readonly uint cSkyHorizon  = MakeArgb(0.85, 0.95, 1.00);
    static readonly uint cWaterBright = MakeArgb(0.30, 0.78, 0.74);
    static readonly uint cWaterDeep   = MakeArgb(0.10, 0.50, 0.55);
    const int HorizonY24 = 8;

    static uint MakeArgb(double r, double g, double b)
        => 0xFF000000u
         | ((uint)Math.Clamp(r * 255.0, 0, 255) << 16)
         | ((uint)Math.Clamp(g * 255.0, 0, 255) << 8)
         |  (uint)Math.Clamp(b * 255.0, 0, 255);

    static void FillBackground(uint[] pixels, int size, string bg)
    {
        if (bg == "none")
        {
            for (var i = 0; i < pixels.Length; i++) pixels[i] = 0xFF101418u;
            return;
        }
        // Horizon position scales with image height (so larger renders still
        // place the horizon ~1/3 from the top, matching the 24px layout).
        var horizon = Math.Max(1, (int)Math.Round(size * (HorizonY24 / 24.0)));
        for (var y = 0; y < size; y++)
        {
            uint c;
            switch (bg)
            {
                case "sky":
                    c = LerpArgb(cSkyTop, cSkyHorizon, y / (double)(size - 1));
                    break;
                case "water":
                    if (size <= 24)
                        c = LerpArgb(cWaterBright, cWaterDeep, y / (double)(size - 1));
                    else
                    {
                        // Large images: full vertical water gradient too
                        c = LerpArgb(cWaterBright, cWaterDeep, y / (double)(size - 1));
                    }
                    break;
                default: // both
                    if (y < horizon)
                        c = LerpArgb(cSkyTop, cSkyHorizon, y / (double)(horizon - 1));
                    else
                        c = LerpArgb(cWaterBright, cWaterDeep,
                            (y - horizon) / (double)(size - 1 - horizon));
                    break;
            }
            for (var x = 0; x < size; x++) pixels[y * size + x] = c;
        }
    }

    static uint LerpArgb(uint a, uint b, double t)
    {
        if (t < 0) t = 0; else if (t > 1) t = 1;
        var ar = (a >> 16) & 0xFF; var ag = (a >> 8) & 0xFF; var ab = a & 0xFF;
        var br = (b >> 16) & 0xFF; var bg = (b >> 8) & 0xFF; var bb = b & 0xFF;
        var nr = (uint)(ar + (br - (double)ar) * t);
        var ng = (uint)(ag + (bg - (double)ag) * t);
        var nb = (uint)(ab + (bb - (double)ab) * t);
        return 0xFF000000u | (nr << 16) | (ng << 8) | nb;
    }

    // LED-matrix style preview: every source pixel becomes one cell with a
    // 1px black gap and 3D shading (mirrors VoxelStudio's RenderLedPreview).
    const int LedCell = 14;
    const int LedGap  = 1;
    static uint[] RenderLed(uint[] src, int srcSize, out int outSize)
    {
        outSize = srcSize * LedCell;
        var dst = new uint[outSize * outSize];
        const uint pcb = 0xFF050608;
        for (var i = 0; i < dst.Length; i++) dst[i] = pcb;
        var inner = LedCell - 2 * LedGap;
        for (var cy = 0; cy < srcSize; cy++)
        for (var cx = 0; cx < srcSize; cx++)
        {
            var color = src[cy * srcSize + cx];
            var x0 = cx * LedCell + LedGap;
            var y0 = cy * LedCell + LedGap;
            var hi   = Scale(color, 1.35);
            var mid  = Scale(color, 1.10);
            var low  = Scale(color, 0.65);
            var dark = Scale(color, 0.40);
            for (var dy = 0; dy < inner; dy++)
            for (var dx = 0; dx < inner; dx++)
            {
                uint c;
                if (dx == 0 && dy == 0) c = hi;
                else if (dx == 0 || dy == 0) c = mid;
                else if (dx == inner - 1 && dy == inner - 1) c = dark;
                else if (dx == inner - 1 || dy == inner - 1) c = low;
                else c = color;
                dst[(y0 + dy) * outSize + (x0 + dx)] = c;
            }
        }
        return dst;
    }

    static uint Scale(uint c, double f)
    {
        var a = (c >> 24) & 0xFFu;
        var r = (uint)Math.Min(255, Math.Max(0, ((c >> 16) & 0xFF) * f));
        var g = (uint)Math.Min(255, Math.Max(0, ((c >> 8)  & 0xFF) * f));
        var b = (uint)Math.Min(255, Math.Max(0, ( c        & 0xFF) * f));
        return (a << 24) | (r << 16) | (g << 8) | b;
    }

    // BGRA → RGBA → PNG via SkiaSharp
    static void SavePng(string path, uint[] pixels, int width, int height)
    {
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var bmp = new SKBitmap(info);
        unsafe
        {
            fixed (uint* p = pixels)
                bmp.InstallPixels(info, (IntPtr)p, width * 4);
            using var img = SKImage.FromBitmap(bmp);
            using var data = img.Encode(SKEncodedImageFormat.Png, 100);
            using var fs = File.Create(path);
            data.SaveTo(fs);
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine("vox-render — headless .vox → PNG");
        Console.WriteLine();
        Console.WriteLine("Usage: vox-render INPUT.vox OUTPUT.png [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --size N          render N×N pixels (default 24)");
        Console.WriteLine("  --scale F         px per voxel (default 1.0 at size 24, 32 otherwise)");
        Console.WriteLine("  --yaw DEG         camera yaw    (default 344)");
        Console.WriteLine("  --pitch DEG       camera pitch  (default 334)");
        Console.WriteLine("  --sun-yaw DEG     sun yaw       (default 250)");
        Console.WriteLine("  --sun-pitch DEG   sun pitch     (default 32)");
        Console.WriteLine("  --sun-pct N       sun intensity 0..500 (default 0)");
        Console.WriteLine("  --ambient N       ambient floor 0..2.5 (default 0.98)");
        Console.WriteLine("  --pan F           pan offset -1..1 (default 0)");
        Console.WriteLine("  --bg both|sky|water|none  (default water at 24px, none at larger)");
        Console.WriteLine("  --preset NAME     lighting preset (default 'Studio R')");
        Console.WriteLine("  --led             LED-matrix style preview");
    }
}
