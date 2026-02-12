// Example 15: Display Time
// Show the current time on screen

#:package Pxl@0.0.42

using Pxl.Ui.CSharp;

var scene = PxlApp.CreateScene(ctx =>
{
    ctx.DrawBackground(Colors.DarkBlue);

    // Get current time from context
    var now = ctx.Now;

    // Format and display hours and minutes
    var time = now.ToString("HH:mm");
    ctx.DrawTextMono4x5(time, 0, 2, Colors.White);

    // Display seconds below
    var seconds = now.Second.ToString("00");
    ctx.DrawTextMono4x5(seconds, 8, 10, Colors.Yellow);

    // Blinking colon effect
    if (now.Millisecond < 500)
    {
        ctx.DrawPoint(12, 18, Colors.White, strokeWidth: 2);
        ctx.DrawPoint(12, 21, Colors.White, strokeWidth: 2);
    }
});

await PxlApp.Simulate(scene);
