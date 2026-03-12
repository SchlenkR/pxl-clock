// ---
// app: Demo17PixelAccess
// displayName: Direct Pixel Access
// author: Cumin & Potato
// ---
// Example 17: Direct Pixel Access
// Read and write individual pixels

#:package Pxl@0.0.61

using Pxl.Ui.CSharp;

var random = new Random(42);

var scene = (DrawingContext ctx) =>
{
    // Draw some background content first
    ctx.DrawBackground(Colors.DarkBlue);
    ctx.DrawCircle(12, 12, 8, colorFill: Colors.Yellow);

    // Access pixels directly
    var pixels = ctx.Pixels;

    // Add random noise to each pixel
    for (var y = 0; y < pixels.Height; y++)
    {
        for (var x = 0; x < pixels.Width; x++)
        {
            // Read current pixel
            var current = pixels[x, y];

            // Add small random variation (noise in 0-1 range)
            var noise = (random.NextDouble() - 0.5) * 0.15;
            var noisy = Color.FromRgb(
                Math.Clamp(current.R + noise, 0, 1),
                Math.Clamp(current.G + noise, 0, 1),
                Math.Clamp(current.B + noise, 0, 1)
            );

            // Write modified pixel back
            pixels[x, y] = noisy;
        }
    }
};

