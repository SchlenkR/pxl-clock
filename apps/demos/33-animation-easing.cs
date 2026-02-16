// Example 33: Easing Animations
// Compare different easing functions side by side

#:package Pxl@0.0.44

using Pxl.Ui.CSharp;

// Create looping animations with different easings (3 seconds each)
var linear = Anim.Linear(3, 0, 28, repeat: Repeat.Loop);
var easeIn = Anim.EaseIn(3, 0, 28, repeat: Repeat.Loop);
var easeOut = Anim.EaseOut(3, 0, 28, repeat: Repeat.Loop);
var easeInOut = Anim.EaseInOut(3, 0, 28, repeat: Repeat.Loop);

// Toggle the label color every 0.8 seconds
var labelColor = Anim.ToggleValues(0.8, Colors.White, Colors.Gray);

var scene = PxlApp.CreateScene(ctx =>
{
    ctx.DrawBackground(Colors.Black);

    // Each dot shows the same animation with a different easing
    ctx.DrawCircle(linear.Eval(ctx), 3, 2, colorFill: Colors.Red);
    ctx.DrawCircle(easeIn.Eval(ctx), 9, 2, colorFill: Colors.Green);
    ctx.DrawCircle(easeOut.Eval(ctx), 15, 2, colorFill: Colors.Blue);
    ctx.DrawCircle(easeInOut.Eval(ctx), 21, 2, colorFill: Colors.Yellow);

    // Use the toggled color
    ctx.DrawText("ease", 0, 23, color: labelColor.Eval(ctx), fontSize: 5);
});

await PxlApp.SimulateAndSendToDevice(scene);
