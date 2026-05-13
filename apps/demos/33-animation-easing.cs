// ---
// app: Demo33AnimationEasing
// displayName: Easing Animations
// author: Cumin & Potato
// ---
// Example 33: Easing Animations
// Compare different easing functions side by side

#:package Pxl@*

using Pxl.Ui.CSharp;

// Create looping animations with different easings (3 seconds each)
var linear = Animate.Linear(3, 0, 28, repeat: Repeat.Loop);
var easeIn = Animate.EaseIn(3, 0, 28, repeat: Repeat.Loop);
var easeOut = Animate.EaseOut(3, 0, 28, repeat: Repeat.Loop);
var easeInOut = Animate.EaseInOut(3, 0, 28, repeat: Repeat.Loop);

// Toggle the label color every 0.8 seconds
var labelColor = Animate.ToggleValues(0.8, Colors.White, Colors.Gray);

var scene = (RasterSurface ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    // Each dot shows the same animation with a different easing
    ctx.DrawCircle(linear.Eval(ctx), 3, 2, colorFill: Colors.Red);
    ctx.DrawCircle(easeIn.Eval(ctx), 9, 2, colorFill: Colors.Green);
    ctx.DrawCircle(easeOut.Eval(ctx), 15, 2, colorFill: Colors.Blue);
    ctx.DrawCircle(easeInOut.Eval(ctx), 21, 2, colorFill: Colors.Yellow);

    // Use the toggled color
    ctx.DrawText("ease", 0, 23, color: labelColor.Eval(ctx), fontSize: 5);
};

