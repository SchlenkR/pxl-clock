// ---
// app: LlmDemo06PerPixelWrite
// displayName: Per-Pixel Write
// author: Cumin & Potato
// description: SetPixel / GetPixel / Pixels[x,y] / Pixels.Cells / SetPixels bulk write
// ---

// INTENT: For per-pixel computation, prefer direct buffer access over
// `DrawPoint` — it skips the Paint pipeline. There are several access
// patterns; this demo shows when each is appropriate.
//
// 1. Single pixel:        ctx.SetPixel(x, y, color)        — quick reads/writes
// 2. Property-style:      ctx.Pixels[x, y] = color         — same as above
// 3. Linear index:        ctx.Pixels[index] = color        — when you have y*W+x
// 4. Iterate every cell:  foreach (var c in ctx.Pixels.Cells)  — full sweeps
// 5. Bulk overwrite:      ctx.SetPixels(buffer, BlendMode)  — when you build the
//                          whole frame in an array first (faster than 576 calls)

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    var t = ctx.Elapsed.TotalSeconds;

    // === Pattern 1+4: full-canvas effect via Pixels.Cells ===
    // .Cells gives you an iterator yielding (X, Y, Color) for every pixel.
    // Use it for radial / position-dependent effects.
    foreach (var cell in ctx.Pixels.Cells)
    {
        var dx = cell.X - 12;
        var dy = cell.Y - 12;
        var dist = Math.Sqrt(dx * dx + dy * dy);

        // Radial colour ripple driven by elapsed time.
        var hue = ((dist - t * 4) / 24.0) % 1.0;
        if (hue < 0) hue += 1;

        // Direct write via the indexer is identical to SetPixel.
        ctx.Pixels[cell.X, cell.Y] = Color.FromHsl(hue, 1.0, 0.5);
    }

    // === Pattern 2: single pixel via SetPixel ===
    // Convenient one-liner. Coordinates are doubles → cast to int implicitly.
    ctx.SetPixel(12, 12, Colors.White);

    // === Pattern 3: linear index when you have an array index ===
    // Layout is row-major: index = y * 24 + x.
    // This sets the four corners using only the linear form to demonstrate.
    int W = 24;
    ctx.Pixels[0]                   = Colors.Yellow;            // (0, 0)
    ctx.Pixels[W - 1]               = Colors.Yellow;            // (23, 0)
    ctx.Pixels[(W - 1) * W]         = Colors.Yellow;            // (0, 23)
    ctx.Pixels[(W - 1) * W + W - 1] = Colors.Yellow;            // (23, 23)

    // === Pattern 5: bulk write — pre-build a buffer, blit it in one call ===
    // (Commented out so the radial effect above stays visible; uncomment to
    // see the buffer pattern overwriting everything.)
    //
    // Color[] buffer = new Color[24 * 24];
    // for (int y = 0; y < 24; y++)
    //     for (int x = 0; x < 24; x++)
    //         buffer[y * 24 + x] = (x + y) % 2 == 0 ? Colors.White : Colors.Black;
    // ctx.SetPixels(buffer, BlendMode.Source);
    //
    // BlendMode.Source REPLACES the destination (ignores anything underneath).
    // Use BlendMode.SourceOver for normal alpha compositing.

    // Reading back: GetPixel for single, Pixels[x,y] for property style.
    // var underCenter = ctx.GetPixel(12, 12);
};
