// Example 12: Time-Based Animation
// Use ctx.Elapsed for smooth, frame-rate independent animation

#:package Pxl@0.0.46

using Pxl.Ui.CSharp;

var scene = PxlApp.CreateScene(ctx =>
{
    ctx.DrawBackground(Colors.Black);

    // Use elapsed time for smooth sine wave animation
    var time = ctx.Elapsed.TotalSeconds;

    // Oscillate X position using sine
    var x = 12 + Math.Sin(time * 2) * 8;

    // Oscillate Y position using cosine (creates circular motion)
    var y = 12 + Math.Cos(time * 2) * 8;

    // Draw a trail effect with multiple circles
    for (var i = 0; i < 5; i++)
    {
        var t = time - i * 0.1;
        var trailX = 12 + Math.Sin(t * 2) * 8;
        var trailY = 12 + Math.Cos(t * 2) * 8;
        var alpha = 1.0 - i * 0.2;
        ctx.DrawCircle(trailX, trailY, 2, 
            colorFill: Color.FromRgba(1, 1, 1, alpha));
    }
});

await PxlApp.SimulateAndSendToDevice(scene);
