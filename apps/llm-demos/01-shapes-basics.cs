// ---
// app: LlmDemo01ShapesBasics
// displayName: Shapes - Basics
// author: Cumin & Potato
// description: All primitive shape APIs in one scene — background, rect, circle, line, point, arc
// ---

// INTENT: Show every drawing primitive on `DrawingContext` in one place so an LLM
// can pick the right one for a given task. This is the "what's in the toolbox"
// reference. Each shape is drawn once, labelled by position so you can see what
// each call produces.

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (DrawingContext ctx) =>
{
    // Background fills the entire 24x24 canvas. Always clear first — pixels
    // from previous frames otherwise persist (you'd see motion trails).
    ctx.DrawBackground(Colors.Black);

    // Rectangle — TWO overloads, pick whichever is more natural for your input:
    //   DrawRectXyWh(x, y, w, h, ...)  — top-left + size
    //   DrawRectXyXy(x1, y1, x2, y2, ...) — two corners (inclusive of x2,y2)
    ctx.DrawRectXyWh(1, 1, 6, 4, colorFill: Colors.DarkRed);
    ctx.DrawRectXyXy(9, 1, 14, 4, colorStroke: Colors.Yellow, strokeWidth: 1);

    // Circle — center coordinates + radius. `colorFill` for solid, `colorStroke`
    // for outline; specify both to fill and outline. If neither is given, the
    // default is `Colors.Lime` fill (handy for debugging).
    ctx.DrawCircle(20, 4, 3, colorFill: Colors.Cyan);
    ctx.DrawCircle(4, 12, 3, colorStroke: Colors.Magenta, strokeWidth: 1);

    // Line — two endpoints. Anti-aliased by default (smooth) — pass
    // isAntialias: false for crisp pixel-art lines.
    ctx.DrawLine(8, 8, 22, 8, color: Colors.White);
    ctx.DrawLine(8, 11, 22, 11, color: Colors.White, isAntialias: false);

    // Point — a single coloured pixel at coords (uses the same Paint pipeline
    // as the bigger shapes). For lots of single-pixel work prefer `SetPixel`
    // (see the per-pixel-write demo) — it skips the paint pipeline.
    ctx.DrawPoint(2, 22, color: Colors.Orange);
    ctx.DrawPoint(4, 22, color: Colors.Orange);
    ctx.DrawPoint(6, 22, color: Colors.Orange);

    // Arc — a pie slice in degrees. 0° = right (+X), 90° = down (+Y),
    // positive sweep = clockwise. Two flavours:
    //   DrawArc      — defined by a bounding rectangle (xy, wh)
    //   DrawArcCenter — defined by center+radius (often more natural)
    ctx.DrawArcCenter(14, 16, 6,
        startAngle: 200, sweepAngle: 140,
        colorFill: Colors.Lime);
};
