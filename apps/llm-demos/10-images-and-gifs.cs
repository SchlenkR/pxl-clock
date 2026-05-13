// ---
// app: LlmDemo10ImagesAndGifs
// displayName: Images and Animated GIFs
// author: Cumin & Potato
// description: LoadSingleImage / LoadAnimatedGif, transforms (Resize, Crop), draw, repeat behaviour
// ---

// INTENT: Image assets are loaded ONCE outside the scene (state — happens at
// compile time, embedded into the DLL), then drawn each frame. Transformations
// (Resize / Crop / CropRegion) return NEW immutable instances — they don't
// mutate the original.
//
// This demo would normally need an `assets/` folder next to the file. Instead
// we synthesise an image programmatically so the demo is self-contained and
// renders in the simulator without external files. The patterns shown for
// drawing/transforming are identical to using a real loaded image.
//
// To use a real asset:
//   var img = Image.LoadSingleImage("assets/logo.png");      // path is RELATIVE to this .cs
//   var gif = Image.LoadAnimatedGif("assets/sprite.gif");
//
// IMPORTANT: the path argument MUST be a string literal — no variables,
// no concatenation, no interpolation. The compiler embeds the file at build
// time using the literal path.

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (RasterSurface ctx) =>
{
    ctx.DrawBackground(Color.FromRgb(0.04, 0.04, 0.08));
    var t = ctx.Elapsed.TotalSeconds;

    // === USAGE PATTERN with a REAL image (commented out — needs an asset) ===
    //
    // // Load (outside scene, but shown here inline for documentation)
    // var img = Image.LoadSingleImage("assets/logo.png");
    //
    // // Transform — each call returns a NEW SingleImage; chain freely.
    // var thumb = img.Resize(16, 16, useAntiAlias: false);   // pixel-art ⇒ no AA
    // var inner = img.CropRegion(x: 4, y: 4, width: 16, height: 16);
    // var trim  = img.Crop(left: 2, top: 2, right: 2, bottom: 2);
    //
    // // Draw at (x, y) — top-left of the image
    // ctx.DrawImage(thumb, 4, 4);
    //
    // // === Animated GIFs ===
    // var gif = Image.LoadAnimatedGif("assets/sprite.gif");
    // var slowed = gif.WithFrameDuration(200);     // override → 200ms / frame
    // ctx.DrawImage(gif, x: 0, y: 0, repeat: true); // auto-advances via ctx.Elapsed
    //
    // // GIF properties:
    // //   gif.FrameCount, gif.Width, gif.Height, gif.TotalDuration
    //
    // // Sprite maps from a single sheet:
    // var sheet  = Image.LoadSingleImage("assets/walk-cycle.png");
    // var sprites = sheet.ToSpriteMap(cellWidth: 8, cellHeight: 8, frameDurationMs: 100);
    // var walk    = sprites.CreateAnimation((0, 0), (0, 1), (0, 2), (0, 3));
    // ctx.DrawImage(walk, 8, 8);

    // === SELF-CONTAINED FALLBACK: a procedurally drawn "image" ===
    // Demonstrates the same DRAW idea: drift a pre-computed-looking shape
    // across the screen at a moving (x, y).
    var x = 4 + (Math.Sin(t) + 1) * 6;     // 4 .. 16
    var y = 4 + (Math.Cos(t) + 1) * 6;     // 4 .. 16

    // Tile a small motif (3x3) — pretend each `pixels[...] = ...` block came
    // from `ctx.DrawImage(img, x, y)`.
    var pattern = new[]
    {
        Colors.TransparentBlack, Colors.Yellow, Colors.TransparentBlack,
        Colors.Yellow,           Colors.Red,    Colors.Yellow,
        Colors.TransparentBlack, Colors.Yellow, Colors.TransparentBlack,
    };
    for (int dy = 0; dy < 3; dy++)
        for (int dx = 0; dx < 3; dx++)
        {
            var c = pattern[dy * 3 + dx];
            if (c.A > 0) ctx.SetPixel((int)x + dx, (int)y + dy, c);
        }
};
