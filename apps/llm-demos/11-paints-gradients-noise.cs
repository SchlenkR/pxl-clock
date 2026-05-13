// ---
// app: LlmDemo11PaintsGradientsNoise
// displayName: Paints, Gradients, and Noise
// author: Cumin & Potato
// description: Linear / Radial / Sweep / Perlin paints, and how to animate them
// ---

// INTENT: Anywhere a draw method takes a `Paint?` (colorFill, colorStroke,
// background) you can pass a procedural paint instead of a single Color.
// The `Paints` factory has the full menu. None of these animate by themselves
// — to make them move, REBUILD them per frame using time-driven parameters.

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (RasterSurface ctx) =>
{
    ctx.DrawBackground(Colors.Black);
    var t = ctx.Elapsed.TotalSeconds;

    // ===== Linear gradient =====
    // Two ways to build one:
    //   LinearGradient((x1, y1), (x2, y2), colors[])  — explicit endpoints
    //   HorizontalGradient(width, c1, c2)             — convenience left→right
    //   VerticalGradient(height, c1, c2)              — convenience top→bottom
    var sky = Paints.VerticalGradient(8, Colors.DeepSkyBlue, Colors.Black);
    ctx.DrawRectXyWh(0, 0, 24, 8, colorFill: sky);

    // ===== Radial gradient =====
    // RadialGradient(centerTuple, radius, colors[])
    var sun = Paints.RadialGradient((20, 4), 5,
        new[] { Colors.White, Colors.Yellow.WithAlpha(0.8), Colors.TransparentBlack });
    ctx.DrawRectXyWh(0, 0, 24, 8, colorFill: sun);   // overlay sun on sky

    // ===== Sweep (angular) gradient =====
    // SweepGradient(centerTuple, colors[]) — like a colour wheel, useful for
    // radial menus, spinners, hue pickers.
    var wheel = Paints.SweepGradient((12, 16),
        new[] {
            Colors.Red, Colors.Yellow, Colors.Lime,
            Colors.Cyan, Colors.Blue, Colors.Magenta, Colors.Red,  // close the loop
        });
    ctx.DrawCircle(12, 16, 5, colorFill: wheel);

    // ===== Perlin noise =====
    // PerlinNoiseFractal(baseFreqX, baseFreqY, octaves, seed) — STATIC field,
    // doesn't move on its own.
    //
    // To animate, drive the SEED from time. Cast to int — fractional seeds
    // would be re-quantised internally. ctx.CycleNo also works for purely
    // frame-driven noise (no wall-time dependence).
    var noiseSeed = (int)(t * 100);
    var clouds = Paints.PerlinNoiseFractal(0.18, 0.18, 4, noiseSeed);
    ctx.DrawRectXyWh(0, 12, 8, 4, colorFill: clouds);

    // PerlinNoiseTurbulence — same idea, different texture (sharper, more
    // chaotic — better for fire/smoke than for soft clouds).
    var fire = Paints.PerlinNoiseTurbulence(0.25, 0.25, 3, noiseSeed);
    ctx.DrawRectXyWh(8, 12, 8, 4, colorFill: fire);

    // ===== Animating a gradient =====
    // The whole point of "Paints don't animate themselves": you control the
    // animation by REBUILDING the paint with shifted parameters each frame.
    var hueShift = (t * 0.3) % 1.0;
    var movingBand = Paints.HorizontalGradient(24,
        Color.FromHsl(hueShift, 1.0, 0.5),
        Color.FromHsl((hueShift + 0.5) % 1.0, 1.0, 0.5));
    ctx.DrawRectXyWh(0, 20, 24, 4, colorFill: movingBand);
};
