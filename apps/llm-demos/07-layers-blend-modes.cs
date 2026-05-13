// ---
// app: LlmDemo07LayersBlendModes
// displayName: Layers and Blend Modes
// author: Cumin & Potato
// description: NewLayer + Plus blend for additive glow, Multiply for shadow, the Fork variant
// ---

// INTENT: Layers (`Fork` and `NewLayer`) are how you isolate drawing so a blend
// mode applies to ONE element rather than to every subsequent draw. The most
// common use case is additive glow (BlendMode.Plus) — drawing it directly on
// `ctx` would Plus-blend every following draw too, which is rarely what you want.
//
//   ctx.Fork(...)     — new layer, COPIES the current canvas state into it
//   ctx.NewLayer(...) — new layer, BLANK (or filled with `clearColor`)
//
// You draw on the layer, then `.Apply(blendMode, interpolation)` to composite
// it back onto the parent context.

#:package Pxl@*

using Pxl.Ui.CSharp;

var scene = (RasterSurface ctx) =>
{
    var t = ctx.Elapsed.TotalSeconds;

    // Base scene: dark background and a magenta object we'll cast shadow onto.
    ctx.DrawBackground(Color.FromRgb(0.05, 0.05, 0.1));
    ctx.DrawRectXyWh(0, 16, 24, 8, colorFill: Color.FromRgb(0.2, 0.1, 0.25));

    // ===== ADDITIVE GLOW with NewLayer + BlendMode.Plus =====
    // We want a soft glow around a moving star. Drawing it on `ctx` directly
    // with semi-transparent circles works visually, but if you wanted Plus
    // blending it would affect everything drawn AFTER. Isolate via NewLayer.
    var sx = 4 + Math.Sin(t * 1.3) * 3;
    var sy = 8 + Math.Cos(t * 1.7) * 3;

    var glow = ctx.NewLayer(clearColor: Colors.Transparent);
    // Wide soft halo
    glow.DrawCircle(sx, sy, 8, colorFill: Colors.Yellow.WithAlpha(0.10));
    glow.DrawCircle(sx, sy, 5, colorFill: Colors.Orange.WithAlpha(0.30));
    glow.DrawCircle(sx, sy, 2, colorFill: Colors.White);
    // Plus blend = each layer pixel ADDED to whatever's underneath, clamped to
    // 1.0. Bright spots stack and saturate to white — the canonical "glow" look.
    glow.Apply(BlendMode.Plus);

    // ===== SHADOW with NewLayer + BlendMode.Multiply =====
    // Multiply darkens — multiplying by black gives black, multiplying by white
    // is a no-op. Perfect for casting a soft shadow.
    var shadow = ctx.NewLayer(clearColor: Colors.White);  // white = "no effect"
    shadow.DrawCircle(18, 11, 4, colorFill: Color.FromRgb(0.3, 0.3, 0.3));  // grey = shadow
    shadow.Apply(BlendMode.Multiply);

    // The thing we just shadowed
    ctx.DrawCircle(18, 10, 3, colorFill: Colors.Cyan);

    // ===== Fork vs NewLayer =====
    // ctx.Fork() COPIES the current canvas. Useful when you want to apply a
    // post-effect (e.g. blur, tint) to whatever is already there.
    //
    //   var snapshot = ctx.Fork();
    //   // ... transform / tint snapshot ...
    //   snapshot.Apply(BlendMode.SourceOver);
};
