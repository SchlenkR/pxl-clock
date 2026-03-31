// ---
// app: Demo25WaveAnimation
// displayName: Wave Animation
// author: Cumin & Potato
// ---
// Example 25: Wave Animation
// Create an animated sine wave

#:package Pxl@0.0.64

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    var time = ctx.Elapsed.TotalSeconds;

    ctx.DrawBackground(Colors.DarkBlue);

    // Draw multiple sine waves
    for (var wave = 0; wave < 3; wave++)
    {
        // Different color for each wave
        var color = wave switch
        {
            0 => Colors.Red,
            1 => Colors.Lime,
            _ => Colors.Cyan
        };

        // Draw wave as connected points
        var prevY = 0.0;
        for (var x = 0; x < 24; x++)
        {
            // Calculate Y with different phase for each wave
            var y = 12 + Math.Sin((x * 0.3) + time * 2 + wave * 2) * (5 - wave);

            if (x > 0)
            {
                ctx.DrawLine(x - 1, prevY, x, y, color, strokeWidth: 1);
            }

            prevY = y;
        }
    }
};

