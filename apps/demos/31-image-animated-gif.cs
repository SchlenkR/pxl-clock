// Example 31: Animated GIF
// Load and draw an animated GIF with automatic frame cycling

#:package Pxl@0.0.45

using Pxl.Ui.CSharp;

// Load an animated GIF and resize to fit the display
var animation = Image.LoadAnimatedGif("assets/mario.gif")
    .Resize(32, 24);

var scene = PxlApp.CreateScene(ctx =>
{
    ctx.DrawBackground(Colors.Black);

    // The frame is automatically selected based on elapsed time
    ctx.DrawImage(animation, 0, 0, repeat: true);
});

await PxlApp.SimulateAndSendToDevice(scene);
