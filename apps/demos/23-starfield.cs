// ---
// app: Demo23Starfield
// displayName: Starfield Effect
// author: Cumin & Potato
// ---
// Example 23: Starfield Effect
// Create a simple animated starfield

#:package Pxl@0.0.58

using Pxl.Ui.CSharp;

// Generate random star positions (persistent state)
var random = new Random(42);
var stars = new (double x, double y, double speed, double brightness)[30];
for (var i = 0; i < stars.Length; i++)
{
    stars[i] = (
        random.NextDouble() * 24,
        random.NextDouble() * 24,
        0.1 + random.NextDouble() * 0.3,
        0.3 + random.NextDouble() * 0.7
    );
}

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    // Draw and animate each star
    for (var i = 0; i < stars.Length; i++)
    {
        var (x, y, speed, brightness) = stars[i];

        // Draw star with varying brightness
        var color = Color.FromRgb(brightness, brightness, brightness);
        ctx.DrawPoint(x, y, color);

        // Move star
        stars[i].x += speed;

        // Wrap around
        if (stars[i].x > 24)
        {
            stars[i].x = 0;
            stars[i].y = random.NextDouble() * 24;
        }
    }
};

