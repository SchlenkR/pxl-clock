// ---
// app: LlmDemo05StateAcrossFrames
// displayName: State Across Frames
// author: Cumin & Potato
// description: Persistent state via List<T> + class with mutable fields. The ValueTuple immutability gotcha.
// ---

// INTENT: Two questions LLMs get wrong constantly:
//   1. "How do I keep state between frames?"  → declare outside `scene`, mutate inside.
//   2. "Why doesn't `arr[i].X += 1` compile?" → `(int X, int Y)` is a ValueTuple,
//                                                an immutable struct. You either
//                                                rebuild the tuple or use a class.
//
// This demo shows the canonical particle-system pattern with a CLASS for
// mutable particle state — robust, idiomatic, and sidesteps the tuple trap.

#:package Pxl@*

using Pxl.Ui.CSharp;

// === STATE: a list of particles, mutated in-place each frame ===

class Particle
{
    public double X, Y;
    public double VY;       // vertical velocity
    public Color Color;
}

var rng = new Random(42);   // seeded for repeatability — change if you want variety
var particles = new List<Particle>();

// Spawn 30 falling particles at random horizontal positions.
for (int i = 0; i < 30; i++)
    particles.Add(new Particle
    {
        X = rng.NextDouble() * 24,
        Y = rng.NextDouble() * 24,
        VY = 2.0 + rng.NextDouble() * 4.0,
        Color = Color.FromHsl(rng.NextDouble(), 0.8, 0.6),
    });

// === SCENE: read state, mutate state, draw state ===

var scene = (RasterSurface ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    // dt = seconds since last frame ≈ 1/Fps. Multiply velocities by it so motion
    // looks the same regardless of FPS.
    var dt = 1.0 / Math.Max(1, ctx.Fps);

    foreach (var p in particles)
    {
        // Mutating a class field is fine — no copy semantics to worry about.
        p.Y += p.VY * dt;

        // Wrap around the bottom edge so the rain loops forever.
        if (p.Y >= 24)
        {
            p.Y = 0;
            p.X = rng.NextDouble() * 24;
        }

        // Draw. SetPixel is the right tool here — one pixel per particle, no
        // need for the heavier shape pipeline.
        ctx.SetPixel((int)p.X, (int)p.Y, p.Color);
    }

    // ===== ANTI-PATTERN — DO NOT DO THIS =====
    //
    // var tuples = new List<(double X, double Y)>();
    // tuples.Add((1, 2));
    // tuples[0].X += 1;            //  ← won't compile: ValueTuple is immutable
    //
    // Workarounds:
    //   tuples[0] = (tuples[0].X + 1, tuples[0].Y);   // rebuild the tuple
    //   // OR use a class (as `Particle` above) when you mutate often.
};
