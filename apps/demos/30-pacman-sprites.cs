// ---
// app: Demo32PacmanSprites
// displayName: Pacman Sprites
// author: Cumin & Potato
// ---
// Example 32: Pacman Sprites
// Load a sprite sheet, create animations, and draw animated sprites

#:package Pxl@0.0.46

using Pxl.Ui.CSharp;

// Load sprite sheet and create a 16x16 sprite map (80ms per frame)
var sprites = Image.LoadSingleImage("assets/pacman_sprite.png")
    .Crop(left: 456, top: 0, right: 0, bottom: 0)
    .ToSpriteMap(cellWidth: 16, cellHeight: 16, frameDurationMs: 80);

// Create character animations from sprite cells (row, col)
var pacmanRight = sprites.CreateAnimation((0, 0), (0, 1), (0, 2), (0, 1));
var ghostRed = sprites.CreateAnimation((4, 0), (4, 1));
var ghostPink = sprites.CreateAnimation((5, 0), (5, 1));

var scene = PxlApp.CreateScene(ctx =>
{
    ctx.DrawBackground(Colors.Black);

    // Use CycleNo for position animation (Elapsed can be negative in some simulators)
    var x = ctx.CycleNo * 0.5 % (ctx.Width + 48) - 16;

    ctx.DrawImage(pacmanRight, x, 8);
    ctx.DrawImage(ghostRed, x - 20, 8);
    ctx.DrawImage(ghostPink, x - 38, 8);
});

await PxlApp.Run(scene);
