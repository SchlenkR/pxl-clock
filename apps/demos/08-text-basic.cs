// ---
// app: Demo08TextBasic
// displayName: Basic Text
// author: Cumin & Potato
// ---
// Example 08: Basic Text
// Draw text with different fonts

#:package Pxl@0.0.46

using Pxl.Ui.CSharp;

var scene = PxlApp.CreateScene(ctx =>
{
    ctx.DrawBackground(Colors.DarkBlue);

    // Monospace 4x5 font (good for numbers and fixed-width text)
    ctx.DrawTextMono4x5("HELLO", 0, 0, Colors.White);

    // Variable width 4x5 font (more compact)
    ctx.DrawTextVar4x5("WORLD", 0, 7, Colors.Yellow);

    // Smaller 3x5 fonts
    ctx.DrawTextMono3x5("TINY", 0, 14, Colors.Cyan);
    ctx.DrawTextVar3x5("TEXT", 0, 20, Colors.Lime);
});

await PxlApp.Run(scene);
