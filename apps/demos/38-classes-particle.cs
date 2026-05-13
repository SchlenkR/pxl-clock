// ---
// app: Demo38ClassesParticle
// displayName: Particle System (Classes)
// author: Cumin & Potato
// ---
// Example 38: Using classes — a simple particle system

#:package Pxl@*

using Pxl.Ui.CSharp;

var random = new Random(42);
var particles = new Particle[20];
for (var i = 0; i < particles.Length; i++)
    particles[i] = Particle.CreateRandom(random);

var scene = (RasterSurface ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    foreach (var p in particles)
    {
        p.Update();
        p.Draw(ctx);
        if (p.Y > 24) p.Reset(random);
    }
};

class Particle
{
    public double X { get; set; }
    public double Y { get; set; }
    public double SpeedY { get; set; }
    public double Hue { get; set; }

    public void Update()
    {
        Y += SpeedY;
        X += Math.Sin(Y * 0.3) * 0.2;
    }

    public void Draw(RasterSurface ctx)
    {
        var fade = 1.0 - (Y / 24.0);
        ctx.DrawPoint(X, Y, Color.FromHsl360(Hue, 100, 50 * fade));
    }

    public void Reset(Random rng)
    {
        X = rng.NextDouble() * 24;
        Y = 0;
        SpeedY = 0.05 + rng.NextDouble() * 0.15;
        Hue = rng.NextDouble() * 360;
    }

    public static Particle CreateRandom(Random rng)
    {
        var p = new Particle();
        p.X = rng.NextDouble() * 24;
        p.Y = rng.NextDouble() * 24;
        p.SpeedY = 0.05 + rng.NextDouble() * 0.15;
        p.Hue = rng.NextDouble() * 360;
        return p;
    }
}
