// ---
// app: OurTinySailboat
// displayName: Our Tiny Sailboat
// author: Ronald Schlenker
// description: A small sailboat on the open sea, beneath a full day's worth of sky.
// appType: ClockFace
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

// ============================================================================
// TUNABLE PARAMETERS — adjust scene behaviour here
// ============================================================================

// --- DAY-NIGHT TIME SOURCE --------------------------------------------------
// Where the sun + moon position comes from each frame. Three modes:
//   0 = realtime — drive from the actual wall-clock time via ctx.Now. The
//                  day cycle takes a real 24 hours. Use on the production
//                  clock so the sun rises at sunrise, etc.
//   1 = animated — fast-forward through one full cycle every `dayDuration`
//                  seconds (independent of wall-clock). Use for previews,
//                  GIF renders, demo videos.
//   2 = fixed    — freeze the sun + moon at `sceneFixedHour` (24h notation)
//                  so you can study a specific moment.
//
// `sceneFixedHour` reference points (only used in mode 2 = fixed):
//   0.0 = 0:00 midnight    6.0 = 6:00 sunrise
//   12.0 = 12:00 midday   18.0 = 18:00 sunset
const int    sceneTimeMode  = 0;          // 0 realtime · 1 animated · 2 fixed
const double sceneFixedHour = 12.0;
const double dayDuration    = 24.0;       // seconds per full cycle in "animated" mode

// --- WORLD-MOTION SPEED -----------------------------------------------------
// Multiplier for the boat's apparent motion and island streaming ONLY. Does
// NOT affect the day/night cycle, sun/moon paths, waves, or stars — those
// always run on their own clock. Useful to debug-scrub the boat/island
// choreography without speeding up the time of day.
//   1.0 = real-time   2.0 = twice as fast   5.0 = fast-forward
const double simulationSpeed = 1.0;

// --- SCENE CONTENT TOGGLES --------------------------------------------------
// Visibility flags for studying individual layers in isolation. The debug
// top-down view replaces the entire perspective scene with a bird's-eye map
// (sea band, islands as circles, boat marker, camera frustum lines).
const bool sceneShowBoat     = true;
const bool sceneShowIslands  = false;
const bool sceneDebugTopDown = false;

// --- TIME READOUT (CLOCK OVERLAY) -------------------------------------------
// HH/MM glyphs in the canvas corners — hours left, minutes right, no colon.
// Two positions (sky-top + water-bottom) with independent toggles:
//   • BOTH on   → crossfade by day/night (sky by day, water by night)
//   • ONLY ONE  → that clock shows at full strength all the time (no fade)
//   • BOTH off  → no time displayed
const bool   clockShowTop       = false;
const bool   clockShowBottom    = true;
// Master alpha multiplier on top of the day/night crossfade — single dial
// for "how present should the clock be at all?".
//   1.0 = fully opaque text       0.0 = invisible
const double clockTextIntensity = 0.75;

// Island visibility is purely positional — an island fades in when its
// projected screen-x enters the canvas band from the right, and fades out as
// it exits to the left.
//
// `islandFadeMarginPx` is the OFF-canvas band over which the alpha ramps from
// 0 to 1. Bigger margin = softer entry. With smoothstep the curve is also
// gentle at the edges, so even narrow (deep) islands read as a smooth
// appearance instead of a pop.
const double islandFadeMarginPx = 12.0;

// --- world geometry ---------------------------------------------------------
const double horizonY = 10.0;             // y of the horizon line (sky 0..10, water 10..24)

// --- celestial body paths ---------------------------------------------------
// Each body traces an ellipse:
//   bodyX = centerX - xAmp * cos(phase + phaseOffset)
//   bodyY = centerY - yAmp * sin(phase + phaseOffset)
// At phase=0 a body sits at (centerX-xAmp, centerY) — the LEFT of its ellipse.
// A quarter-cycle later it's at the TOP, half-cycle at right, three-quarters at BOTTOM.
//
// SUN
const double sunCenterX     = 12.0;
const double sunCenterY     = 10.0;       // = horizonY: rises/sets exactly at horizon line
const double sunXAmp        =  7.0;       // narrow x → sun stays near centre, never off-canvas
const double sunYAmp        = 12.0;       // tall y → sun climbs high (out of frame at noon)
const double sunPhaseOffset =  0.0;       // 0 = sun rises at left at t=0

// MOON — antipodal to sun (moon rises exactly when sun sets, like full moon).
// xAmp is wider than the canvas → the moon is OFF-SCREEN as it rises and sets,
// only becoming visible when it climbs above the horizon enough to enter the
// frame from the side.
//
// Other phase-offset suggestions:
//   Math.PI               — antipodal (full moon, rises as sun sets)     ← current
//   Math.PI * 1.5         — moon 90° ahead (moon high at sunset)
//   Math.PI * 0.5         — moon 90° behind (moon high at sunrise)
//   Math.PI * 1.25        — moon a bit ahead (sets a few hours after midnight)
const double moonCenterX     = 12.0;
const double moonCenterY     = 10.0;
const double moonXAmp        = 14.0;      // wider than canvas → moon enters/leaves off-frame
const double moonYAmp        =  6.0;      // LOW arc — hugs the water for nice reflections
const double moonPhaseOffset = Math.PI;
const double moonRadius      =  1.6;      // small disc — moon shouldn't look like a second sun
const double moonGlowRadius  =  4.5;      // soft radial glow around the disc (in pixels)
const double moonGlowStrength = 0.65;     // alpha of the glow at its centre (fades out radially)
const bool   moonShowPhases  = false;     // true = animated phases (sliver→half→full→…), false = always full
// Master brightness multiplier for the moon — applied uniformly to the disc,
// the radial glow/halo, and the water reflection. Lets you dim the entire
// moon presence with a single dial without re-tuning each individual layer.
//   1.0 = current full moon         0.0 = moon gone entirely (dark night sky)
const double moonBrightness  =  1.0;

// --- wave field shaping (break up the strict 1D look) -----------------------
// 0 = old behaviour (pure single-direction wave train).
// Higher values add more dimensionality — but keep them small (≤ 1) so the
// primary wave direction still dominates.
const double waveCrossStrength  = 0.35;   // gentle perpendicular flow on top of main fronts
const double waveDomainWarp     = 0.45;   // bends primary wave fronts so they're not perfectly straight

// --- moon reflection on water -----------------------------------------------
const double moonReflectionLength    = 13.0;  // pixel rows the reflection extends downward
const double moonReflectionWidthBase = 0.6;   // beam width right under the moon (top of cone)
const double moonReflectionWidthGrow = 0.55;  // how fast the cone widens per pixel of depth
const double moonReflectionBase      = 0.55;  // alpha of the always-visible silvery base beam
const double moonReflectionStrength  = 1.0;   // sparkle alpha multiplier on top of the base
const double moonReflectionMinPhase  = 0.3;   // |moonPhase| below this → no reflection

// --- stars ------------------------------------------------------------------
const double starTwinkleSpeed  = 0.1;         // radians/sec for the sin() drive (was 4.0). Lower = slower.
const double starBrightness    = 0.6;         // overall brightness multiplier applied to every star (1.0 = previous strong default)
const double starRayBrightness = 0.45;        // brightness of ray pixels relative to the centre pixel (0..1)
const double starDensity       = 0.035;       // ~3.5% of sky pixels become stars — keeps the night feeling sparse, not crowded
// Shape distribution — relative weights for each star shape. Don't have to
// sum to 1; they're normalised. Each star deterministically picks one shape
// from this distribution (so the same pixel is always the same shape).
//   Dot      = single centre pixel only (no rays)
//   DiagNwSe = "\" — rays at top-left + bottom-right
//   DiagNeSw = "/" — rays at top-right + bottom-left
//   Cross    = "+" — rays up + down + left + right through the centre
const double starShapeWeightDot      = 0.60;
const double starShapeWeightDiagNwSe = 0.18;
const double starShapeWeightDiagNeSw = 0.14;
const double starShapeWeightCross    = 0.08;

// --- horizon line wobble ----------------------------------------------------
const double horizonWobbleAmount = 0.55;     // 0 = razor-straight, larger = follows waves more

// ============================================================================
// 3D WORLD + CAMERA MODEL
// ============================================================================
// Think of the scene from above: the sea is an infinitely-wide BAND in the
// (wx, wz) plane. wx = lateral position (positive = right), wz = depth
// (distance from camera in world units; +z = away from camera).
//
// The CAMERA sits at world origin (0, camHeight, 0), looking along +z. To
// project a sea-surface world point (wx, 0, wz) to canvas pixel (sx, sy):
//   sx = canvasCenterX + camFocal *  wx        / wz
//   sy = horizonY      + camFocal *  camHeight / wz
//
// Inverse: a screen y (below horizon) maps back to world depth via
//   wz = camFocal * camHeight / (sy - horizonY)
//
// An object's apparent SIZE on canvas at depth wz is camFocal / wz units of
// world-space-per-pixel — so a 1-unit tall object spans (camFocal / wz)
// pixels vertically.
//
// Right-to-left motion: islands have FIXED world (wx, wz). The "wind" pushes
// them in -wx direction at `worldStreamSpeed` units/sec — so they enter from
// the right, drift past at perspective-correct angular speeds, and exit on
// the left, where they recycle to the far right with a fresh random wz.
const double camHeight        =  1.6;    // world units of camera above sea
const double camFocal         = 24.0;    // focal length: bigger = telephoto / smaller FOV
const double canvasCenterX    = 12.0;    // screen-x of camera-axis (centre column)

// World extents that map to the visible band of the canvas
const double worldDepthNear   =  3.0;    // wz at which an island is "right at the camera" (sy near canvas bottom)
const double worldDepthFar    = 30.0;    // wz beyond which an island is far past the horizon
const double worldStreamSpeed =  0.55;   // wx units/sec the world drifts left (= boat's apparent forward+side motion)

// Island recycling: once an island's wx falls below recycleX (well off-screen
// left), it teleports back to recycleStartX (well off-screen right) WITH THE
// SAME wz it had before. The wider the [recycleX, recycleStartX] range, the
// more "off-stage" buffer where islands are physically far enough to be
// invisible — so at any moment few islands are on-canvas, and they clearly
// enter from the right edge.
const double worldRecycleX      = -30.0;
const double worldRecycleStartX =  30.0;

// Atmospheric haze (still useful — far things blend into horizon colour).
// Now driven by world depth instead of an abstract [0,1] depth parameter.
const double islandHazeAtNear   = 0.05;
const double islandHazeAtFar    = 0.55;

// --- island shading ----------------------------------------------------------
// Island volume is primarily BAKED INTO THE SHAPE itself: front faces, top
// terraces, embedded objects, and water-contact bands. Dynamic shading stays
// off so the island keeps the same body impression as it crosses the frame.
// What's still applied: rim darkening at the silhouette edge (just outline,
// not lighting) for a touch of volume without a moving light effect.
const double islandShadeHorizontal =  0.0;    // OFF — was 0.28
const double islandShadeMin        =  0.50;   // floor for combined shade × rim multiplier (rim darkens to this)
const double islandShadeMax        =  1.0;    // ceiling — no over-bright lit side any more
const double islandRimDarken       =  0.35;   // silhouette-edge darken (0 = none, 1 = rim goes to black)

// --- BOAT NAVIGATION (target-driven) ----------------------------------------
// The boat does NOT oscillate. It has a CURRENT POSITION and a TARGET POINT
// in (wx, wz) world coords, and each frame it steers a step toward the target
// at boatTravelSpeed world-units/sec. When the target is reached, OR an island
// has drifted into the planned path, a NEW target is picked: a random point
// inside the camera frustum that is at least islandClearance world-units
// away from EVERY island (current position).
//
// This replaces the old sin-drift + dodge: the boat now plans its course.
const double boatBaseWz        = 10.0;   // initial depth on first frame
const double boatMinWz         =  4.0;   // hard near clamp (else boat overflows canvas)
const double boatMaxWz         = 16.0;   // hard far clamp (else boat sub-pixel)
const double boatMaxRollAngle  =  0.15;  // ±radians

// Camera frustum: at depth wz, visible lateral half-width = wz/2 (camFocal=24,
// canvas half-width = 12). Targets keep this margin INSIDE the cone.
const double frustumSafetyMargin = 0.8;

// Each island defines a HARD no-go disc around its (wx, wz) of radius
// (worldHalfW + islandClearance). The boat is physically not allowed to
// enter any such disc. Bigger islandClearance = comfortable buffer.
const double islandClearance      =  4.0;   // world-units of buffer around every island (HARD constraint)
const double pathSafetyMargin     =  1.5;   // EXTRA buffer the picker requires AROUND the chosen path — keeps boat from grazing edges
const double targetMinTravel      =  6.0;   // new target must be at least this far from current pos (force big swings)
const double targetReachRadius    =  0.7;   // when boat is within this distance of target, pick a new one
const int    targetPickerAttempts = 80;     // how many random samples to try per pass

// Depth bias — when boat is in one half of [boatMinWz, boatMaxWz], the picker
// prefers targets in the OTHER half. This forces the boat to swing between
// near (huge boat at canvas bottom) and far (tiny boat at horizon) rather
// than getting stuck in the comfortable middle. 0 = uniform, 1 = always
// opposite half.
const double targetDepthBias      = 0.90;

// LATERAL DRIFT — gentle ambient sideways motion. The boat does NOT actively
// chase the lateral position at full travel speed; instead the wx component
// of velocity is a SLOW pursuit (boatLateralPursuitRate per sec) toward the
// drift function. Depth motion uses its own boatDepthSpeed. This decouples
// the two and gives the boat the relaxed "rocking" feel of a real sailing
// boat instead of a hectic chase.
const double lateralDriftAmp        = 1.8;   // peak wx swing in world units (smaller = gentler)
const double lateralDriftPeriodA    = 33.0;  // seconds (slowest layer)
const double lateralDriftPeriodB    = 51.0;
const double lateralDriftPeriodC    = 73.0;
const double boatLateralPursuitRate = 0.15;  // 1/sec — how fast wx tracks the drift target (small = lazy following)
const double boatDepthSpeed         = 1.00;  // wu/sec — faster depth travel so the boat visibly comes near + recedes (was 0.35)

// Boat sprite size — REFERENCE dimensions in world units. They get scaled to
// canvas pixels by the perspective factor `Math.Max(boatMinPxPerWorld, camFocal / boatWz)`.
// The Max-clamp keeps the boat from getting unreadably small at the far end.
const double boatMinPxPerWorld    = 2.4;   // floor for the perspective scale (raise → boat stays bigger when far back)
const double boatRefHullDeckHalfW = 1.10;   // world units
const double boatRefHullBotHalfW  = 0.65;
const double boatRefMastHeight    = 1.65;
const double boatRefSailBaseWidth = 1.30;



var clamp01 = (double v) => Math.Max(0.0, Math.Min(1.0, v));
var mix = (Color a, Color b, double f) =>
{
    var k = clamp01(f);
    return new Color(a.R + (b.R - a.R) * k, a.G + (b.G - a.G) * k, a.B + (b.B - a.B) * k);
};
var smoothstep = (double v) => { var k = clamp01(v); return k * k * (3.0 - 2.0 * k); };
var hash = (double x, double y) =>
{
    var s = Math.Sin(x * 12.9898 + y * 78.233) * 43758.5453;
    return s - Math.Floor(s);
};

var waveValue = (double x, double y, double t) =>
{
    var v = y - horizonY;
    var vF = v + v * v * 0.025;

    // Domain warp — bend x slightly based on y and time. Makes the primary
    // wave fronts gently curved instead of perfectly straight horizontal lines.
    var xw = x + Math.Sin(vF * 0.18 + t * 0.7) * waveDomainWarp;

    // Primary wave train — same direction as before, now slightly warped
    var w1 = Math.Sin(xw * 0.15 + vF * 0.60 + t * 1.8);
    var w2 = Math.Sin(xw * 0.35 + vF * 1.00 + t * 2.5 + 1.7) * 0.65;
    var w3 = Math.Sin(xw * 0.55 + vF * 1.65 + t * 3.5 + 3.4) * 0.40;

    // Cross-direction components — propagate at different angles than the
    // primary set. Note the MINUS sign before vF: phase fronts move the
    // opposite way, giving the surface a subtle interference / criss-cross feel.
    var c1 = Math.Sin(x * 0.50 - vF * 0.15 + t * 1.4 + 0.7) * 0.30 * waveCrossStrength;
    var c2 = Math.Sin(x * 0.28 - vF * 0.38 + t * 2.1 + 1.5) * 0.20 * waveCrossStrength;

    return w1 + w2 + w3 + c1 + c2;
};

// 3-way palette mix: anchored at twilight (tod≈0), interpolating toward day or
// night by the corresponding factor. day+night never both > 0 (antipodal sun).
var palette3Mix = (Color twilight, Color day, Color night, double dayF, double nightF) =>
{
    var twF = 1.0 - dayF - nightF;
    if (twF < 0) twF = 0;
    return new Color(
        twilight.R * twF + day.R * dayF + night.R * nightF,
        twilight.G * twF + day.G * dayF + night.G * nightF,
        twilight.B * twF + day.B * dayF + night.B * nightF);
};

// Multiplicative light tint + brightness (used for the boat).
var litColor = (Color c, Color tint, double brightness) =>
{
    return new Color(
        clamp01(c.R * tint.R * brightness),
        clamp01(c.G * tint.G * brightness),
        clamp01(c.B * tint.B * brightness));
};

// ---- 3D camera projection -------------------------------------------------
// Project a sea-surface world point (wx, wz) to canvas pixel coords.
// Returns the screen X (sub-pixel double) for centred horizontal placement.
var projectScreenX = (double wx, double wz) => canvasCenterX + camFocal * wx / wz;
// Project to screen Y. wz must be > 0 (otherwise we'd divide by 0/negative).
var projectScreenY = (double wz)            => horizonY      + camFocal * camHeight / wz;
// Pixels-per-world-unit at this depth. Multiply by a world-space size to get
// the size in canvas pixels.
var projectScale   = (double wz)            => camFocal / wz;

// ============================================================================
// PALETTES — Sun/Moon/Far horizons get blended per-pixel by light direction
// ============================================================================

// DAY (high sun, almost out of frame, very bright)
var skyTopDay          = Color.FromRgb(0.45, 0.72, 1.00);
var skyHorizonSunDay   = Color.FromRgb(1.00, 1.00, 1.00);
var skyHorizonMoonDay  = Color.FromRgb(0.97, 1.00, 1.00);
var skyHorizonFarDay   = Color.FromRgb(0.97, 1.00, 1.00);
var waterTopSunDay     = Color.FromRgb(0.55, 0.85, 1.00);
var waterTopMoonDay    = Color.FromRgb(0.50, 0.80, 1.00);
var waterTopFarDay     = Color.FromRgb(0.50, 0.80, 1.00);
var waterDeepDay       = Color.FromRgb(0.05, 0.22, 0.50);
var horizonLineSunDay  = Color.FromRgb(1.00, 1.00, 1.00);
var horizonLineMoonDay = Color.FromRgb(1.00, 1.00, 1.00);
var horizonLineFarDay  = Color.FromRgb(1.00, 1.00, 1.00);
var foamColorDay       = Color.FromRgb(1.00, 1.00, 1.00);

// TWILIGHT (sun/moon at horizon, two distinct glow zones)
var skyTopTwilight          = Color.FromRgb(0.30, 0.18, 0.42);
var skyHorizonSunTwilight   = Color.FromRgb(1.00, 0.55, 0.25);  // warm orange near sun
var skyHorizonMoonTwilight  = Color.FromRgb(0.35, 0.42, 0.62);  // cool dim blue near moon
var skyHorizonFarTwilight   = Color.FromRgb(0.18, 0.10, 0.30);  // dark indigo elsewhere
var waterTopSunTwilight     = Color.FromRgb(0.85, 0.45, 0.35);
var waterTopMoonTwilight    = Color.FromRgb(0.22, 0.25, 0.40);
var waterTopFarTwilight     = Color.FromRgb(0.18, 0.10, 0.28);
var waterDeepTwilight       = Color.FromRgb(0.05, 0.03, 0.15);
var horizonLineSunTwilight  = Color.FromRgb(1.00, 0.70, 0.40);
var horizonLineMoonTwilight = Color.FromRgb(0.50, 0.55, 0.75);
var horizonLineFarTwilight  = Color.FromRgb(0.30, 0.18, 0.35);
var foamColorTwilight       = Color.FromRgb(1.00, 0.85, 0.65);

// NIGHT (moon up, deep blue, cold)
var skyTopNight          = Color.FromRgb(0.02, 0.03, 0.10);
var skyHorizonSunNight   = Color.FromRgb(0.20, 0.12, 0.22);     // very faint warm if sun barely below
var skyHorizonMoonNight  = Color.FromRgb(0.30, 0.38, 0.55);     // silvery moonglow
var skyHorizonFarNight   = Color.FromRgb(0.04, 0.05, 0.14);
var waterTopSunNight     = Color.FromRgb(0.18, 0.10, 0.20);
var waterTopMoonNight    = Color.FromRgb(0.18, 0.25, 0.40);
var waterTopFarNight     = Color.FromRgb(0.03, 0.05, 0.12);
var waterDeepNight       = Color.FromRgb(0.01, 0.02, 0.06);
var horizonLineSunNight  = Color.FromRgb(0.30, 0.20, 0.25);
var horizonLineMoonNight = Color.FromRgb(0.55, 0.62, 0.78);
var horizonLineFarNight  = Color.FromRgb(0.10, 0.12, 0.20);
var foamColorNight       = Color.FromRgb(0.70, 0.78, 0.95);

// Sun colors (twilight is hot orange-red, day is bright near-white)
var sunCoreTwilight = Color.FromRgb(1.00, 0.65, 0.30);
var sunGlowTwilight = Color.FromRgb(1.00, 0.40, 0.20);
var sunHaloTwilight = Color.FromRgb(0.95, 0.50, 0.40);
// Day sun: nearly pure white (only the faintest cream tint), no yellow glow.
var sunCoreDay      = Color.FromRgb(1.00, 1.00, 0.99);
var sunGlowDay      = Color.FromRgb(1.00, 0.99, 0.93);
var sunHaloDay      = Color.FromRgb(1.00, 1.00, 0.97);

// Moon — silvery cool
var moonCore = Color.FromRgb(0.90, 0.93, 1.00);
var moonGlow = Color.FromRgb(0.55, 0.65, 0.85);
var moonHalo = Color.FromRgb(0.25, 0.32, 0.50);

// AMBIENT LIGHT MODEL
// -------------------
// The sun (and ONLY the sun) provides ambient light that lets objects (boat,
// islands) show their own colour. Moonlight is too weak in real life to give
// diffuse illumination — it's mostly seen as specular highlights on water.
//
// The ramp goes: ambient is 0 below `ambientStartAltitude` (sun far below
// horizon — pure night silhouettes), ramps up smoothly via smoothstep to 1.0
// at `ambientFullAltitude` (sun this many pixels above horizon → full colour).
// The smoothstep curve is what stops the day/night transition from feeling
// abrupt — the linear ramp it replaced had a hard kink right at the horizon.
//
// Tune these for the day/night feel:
//   • ambientStartAltitude (more negative = ambient lingers DEEPER below horizon → softer dusk)
//   • ambientFullAltitude  (larger      = ambient takes LONGER to reach 1.0 → softer dawn)
const double ambientStartAltitude = -3.0;  // px below horizon where ambient begins to rise from 0
const double ambientFullAltitude  =  9.0;  // px above horizon where ambient saturates at 1.0

// Light tint shifts from sunset orange (sun low) to neutral warm white (sun high).
var lightTintDay      = Color.FromRgb(1.00, 0.97, 0.85);  // warm white at noon
var lightTintTwilight = Color.FromRgb(1.00, 0.65, 0.45);  // orange near horizon

// Boat base colors
var hullDeck    = Color.FromRgb(0.62, 0.40, 0.22);
var hullSide    = Color.FromRgb(0.42, 0.25, 0.13);
var mastColor   = Color.FromRgb(0.35, 0.20, 0.10);
var sailColor   = Color.FromRgb(1.00, 1.00, 1.00);

// Island base colors — get multiplied by lightTint * lightBrightness like the boat
// Bumped saturation across the board so layers stay distinguishable on the
// low-contrast LED panel after lighting + haze + shading multiplications.
// VOXEL MATERIALS — each material has TWO shades: top face (lit, brighter)
// and front face (in shadow, darker). Side faces use the same as front.
// 5 materials (sand, rock, grass, trunk, leaves) → 10 colour constants.
var matSandTop      = Color.FromRgb(0.95, 0.82, 0.52);    // sand — sun-lit top
var matSandFront    = Color.FromRgb(0.62, 0.48, 0.28);    // sand — shadow side
var matRockTop      = Color.FromRgb(0.62, 0.58, 0.52);    // rock — top
var matRockFront    = Color.FromRgb(0.30, 0.27, 0.24);    // rock — shadow side
var matGrassTop     = Color.FromRgb(0.30, 0.68, 0.22);    // grass — vivid top
var matGrassFront   = Color.FromRgb(0.18, 0.38, 0.14);    // grass — shadow side
var matTrunkTop     = Color.FromRgb(0.50, 0.32, 0.16);    // trunk — top (lit)
var matTrunkFront   = Color.FromRgb(0.28, 0.16, 0.08);    // trunk — shadow side
var matLeavesTop    = Color.FromRgb(0.42, 0.78, 0.30);    // leaves — bright top
var matLeavesFront  = Color.FromRgb(0.18, 0.42, 0.14);    // leaves — shadow underside
// Legacy matSnow kept for any other reference (not used by voxel system)
var matSnow         = Color.FromRgb(0.95, 0.97, 1.00);

// ============================================================================
// ISLAND SHAPE LIBRARY — hand-pixelled sprites (from island-pixel-tests/)
// ============================================================================
// Each island is a 3D VOXEL MODEL — list of (vx, vy, vz, material) cubes.
//   vx: 0..sizeX-1, lateral (left-to-right)
//   vy: 0..sizeY-1, vertical (0 = bottom, at waterline; positive = up)
//   vz: 0..sizeZ-1, depth (0 = front, closest to camera; positive = back)
//
// Materials: 1=sand 2=rock 3=grass 4=trunk 5=leaves
//
// The renderer projects each voxel using oblique cabinet projection: depth
// shifts the voxel UP on canvas (back voxels project closer to horizon),
// and each voxel paints both a TOP face (lit) and a FRONT face (shadow).
// Painters' algorithm: sort back-to-front, top-to-bottom for occlusion.

var voxelPalmIsland = new (int x, int y, int z, int mat)[]
{
    // Sand base — y=0
    (1,0,0,1),(2,0,0,1),(3,0,0,1),
    (0,0,1,1),(1,0,1,1),(2,0,1,1),(3,0,1,1),(4,0,1,1),
    (1,0,2,1),(2,0,2,1),(3,0,2,1),
    // Sand top — y=1 (smaller than base, suggests rounded mound)
    (1,1,1,1),(2,1,1,1),(3,1,1,1),
    // Trunk — y=2..3 (centred)
    (2,2,1,4),(2,3,1,4),
    // Crown of leaves — y=4 (3x3 disc)
    (1,4,0,5),(2,4,0,5),(3,4,0,5),
    (1,4,1,5),(2,4,1,5),(3,4,1,5),
    (1,4,2,5),(2,4,2,5),(3,4,2,5),
    // Top leaf tuft — y=5
    (2,5,1,5),
};

var voxelMountain = new (int x, int y, int z, int mat)[]
{
    // Rock base — y=0 (5 wide, 3 deep)
    (0,0,0,2),(1,0,0,2),(2,0,0,2),(3,0,0,2),(4,0,0,2),
    (0,0,1,2),(1,0,1,2),(2,0,1,2),(3,0,1,2),(4,0,1,2),
    (1,0,2,2),(2,0,2,2),(3,0,2,2),
    // Mid — y=1 (narrower)
    (1,1,0,2),(2,1,0,2),(3,1,0,2),
    (1,1,1,2),(2,1,1,2),(3,1,1,2),
    (2,1,2,2),
    // Upper — y=2 (narrower still)
    (2,2,0,2),(3,2,0,2),
    (2,2,1,2),
    // Snow peak — y=3
    (2,3,0,5),
};

var voxelMesaShelf = new (int x, int y, int z, int mat)[]
{
    // Rock base — y=0 (6 wide, 3 deep)
    (0,0,0,2),(1,0,0,2),(2,0,0,2),(3,0,0,2),(4,0,0,2),(5,0,0,2),
    (0,0,1,2),(1,0,1,2),(2,0,1,2),(3,0,1,2),(4,0,1,2),(5,0,1,2),
    (1,0,2,2),(2,0,2,2),(3,0,2,2),(4,0,2,2),
    // Plateau (flat top) — y=1
    (1,1,0,2),(2,1,0,2),(3,1,0,2),(4,1,0,2),
    (1,1,1,2),(2,1,1,2),(3,1,1,2),(4,1,1,2),
    (2,1,2,2),(3,1,2,2),
    // Grass cap — y=2 (just the very top)
    (2,2,0,3),(3,2,0,3),
    (2,2,1,3),(3,2,1,3),
};

var voxelTwinPeaks = new (int x, int y, int z, int mat)[]
{
    // Wide rock base — y=0
    (0,0,0,2),(1,0,0,2),(2,0,0,2),(3,0,0,2),(4,0,0,2),
    (0,0,1,2),(1,0,1,2),(2,0,1,2),(3,0,1,2),(4,0,1,2),
    // Mid layer — y=1 (still wide, less deep)
    (1,1,0,2),(2,1,0,2),(3,1,0,2),
    (1,1,1,2),(3,1,1,2),                          // valley between peaks
    // Upper — y=2 (two pillars)
    (1,2,0,2),(3,2,0,2),
    (1,2,1,2),(3,2,1,2),
    // Top peaks — y=3
    (1,3,0,2),(3,3,0,2),
};

var voxelIslands = new[] { voxelMountain, voxelTwinPeaks, voxelPalmIsland, voxelMesaShelf };

// ============================================================================
// ISLAND INSTANCES — each has a fixed STARTING world position (wx0, wz).
// Per frame, the wind streams them in -wx at worldStreamSpeed, so they drift
// from right edge to left edge of the canvas through the perspective view.
// When their wx falls below worldRecycleX, they teleport to worldRecycleStartX
// (still off-screen RIGHT) with a fresh wz, so we get fresh combinations
// instead of a fixed rotation.
// ============================================================================
//   shape: 0=Mountain 1=Volcano 2=PalmAtoll 3=TreasureCove
//   wx0  : starting world X (positive = right of camera-axis)
//   wz   : depth in world units. Bigger = further away. Stays constant.
//          (Recycled on wrap with a fresh randomised value.)
//   worldHalfW: lateral footprint radius in WORLD units — drives boat dodge
//               and "is the boat in front of/behind this island" tests.
//   flip: horizontal mirror of the shape
// wx0 evenly spaced across the [-30, +30] = 60-wu track (15 wu apart). With
// the wider track, at any moment only ~2 islands are within the visible range
// of the debug canvas — the others sit off-canvas-right "waiting to spawn".
var islandData = new (int shape, double wx0, double wz, double worldHalfW, bool flip)[]
{
    (0, -22.5, 20.0, 2.0, false),  // Mountain     — far on the horizon
    (1,  -7.5, 14.0, 1.6, true),   // TwinPeaks    — mid distance
    (2,   7.5, 10.0, 2.0, false),  // PalmSingle   — close
    (3,  22.5,  8.0, 1.8, true),   // MesaShelf    — very close foreground
};

// Pseudo-random for recycling: deterministic from a stream index.
// ============================================================================
// MUTABLE BOAT NAVIGATION STATE
// ============================================================================
// These persist across frames (closure capture). On the first call, they
// initialise to "boat at centre, no target yet". Each frame we integrate the
// position toward the target, and pick a new target when needed.
var navInitialized = false;
var boatPosWx     = 0.0;
var boatPosWz     = boatBaseWz;
var boatVelWx     = 0.0;
var boatVelWz     = 0.0;
var boatTargetWx  = 0.0;
var boatTargetWz  = boatBaseWz;
var prevTWorld    = 0.0;
var navRng        = new Random(42);

// ============================================================================
// TINY 3x5 PIXEL FONT — digits 0-9 for the in-scene time readout
// ============================================================================
// Each glyph is top-down: row 0 = topmost pixel row. '#' = on, '.' = off.
// All digits 3 wide. Hand-tuned so each digit stays unambiguous at this very
// small size — closed loops on 0/6/8/9, an open notch on 4, and a clear
// S-spine on 2/3/5.
var clockGlyph = (char c) => c switch
{
    '0' => new[] { "###", "#.#", "#.#", "#.#", "###" },
    '1' => new[] { ".#.", "##.", ".#.", ".#.", "###" },
    '2' => new[] { "###", "..#", "###", "#..", "###" },
    '3' => new[] { "###", "..#", ".##", "..#", "###" },
    '4' => new[] { "#.#", "#.#", "###", "..#", "..#" },
    '5' => new[] { "###", "#..", "###", "..#", "###" },
    '6' => new[] { "###", "#..", "###", "#.#", "###" },
    '7' => new[] { "###", "..#", ".#.", ".#.", ".#." },
    '8' => new[] { "###", "#.#", "###", "#.#", "###" },
    '9' => new[] { "###", "#.#", "###", "..#", "###" },
    _   => new string[0],
};
// Two readouts that crossfade by day/night: the day version sits in the upper
// SKY corners, the night version in the lower WATER corners. HOURS go on the
// LEFT, MINUTES on the RIGHT — no colon. Each group is 1 px from its canvas
// edge (top/bottom and left/right).
const int clockTextYTop = 1;     // top row of the SKY glyphs (rows 1..5, 1px margin from top)
const int clockTextYBot = 18;    // top row of the WATER glyphs (rows 18..22, 1px margin from bottom)

var scene = (RasterSurface ctx) =>
{
    var t      = ctx.Elapsed.TotalSeconds;                    // real-time (sky, sun/moon, waves, stars)
    var tWorld = ctx.Elapsed.TotalSeconds * simulationSpeed;  // simulation-time (boat drift, island streaming)

    // ============================================================================
    // STEP 1: compute island world positions + visibility (used by nav AND
    //         debug AND main render). No lit colours yet — those are added later.
    // ============================================================================
    // Visibility is PURELY positional: an island is fully visible while its
    // projected screen-x sits inside [0, 24], and fades smoothly across an
    // `islandFadeMarginPx`-wide band on each side. When the island is well
    // off-canvas (which it is during the wx wrap), vis = 0 so the wz
    // randomisation between cycles is invisible to the viewer.
    var navIslands = new (double wx, double wz, double worldHalfW, double vis)[islandData.Length];
    for (var i = 0; i < islandData.Length; i++)
    {
        var d = islandData[i];
        var trackLen = worldRecycleStartX - worldRecycleX;
        var raw = ((d.wx0 - worldStreamSpeed * tWorld) - worldRecycleX) % trackLen;
        if (raw < 0) raw += trackLen;
        var wx = worldRecycleX + raw;
        // wz is FIXED per island — no random per-cycle re-roll. This makes
        // each island's appearance consistent across wraps.
        var wz = d.wz;

        // Positional fade with smoothstep — gentle at both ends.
        var sxIs = projectScreenX(wx, wz);
        double tFade;
        if (sxIs < -islandFadeMarginPx || sxIs > 24.0 + islandFadeMarginPx) tFade = 0.0;
        else if (sxIs < 0.0)        tFade = (sxIs + islandFadeMarginPx) / islandFadeMarginPx;
        else if (sxIs > 24.0)       tFade = (24.0 + islandFadeMarginPx - sxIs) / islandFadeMarginPx;
        else                        tFade = 1.0;
        var vis = smoothstep(tFade);

        navIslands[i] = (wx, wz, d.worldHalfW, vis);
    }

    // ============================================================================
    // STEP 2: target-driven boat navigation
    // ============================================================================
    // Helper: is (wx, wz) clear of every island? We check ALL islands —
    // including ones currently off-canvas (vis<0.05) — because their world
    // footprints are real obstacles even when not yet rendered. The previous
    // bug was filtering by vis here, which let the picker put targets inside
    // islands about to stream into view.
    // ============================================================================
    // BOAT NAVIGATION — clean 4-step model
    // ============================================================================
    //   1. Re-pick target if REACHED or PATH-BLOCKED.
    //   2. Compute velocity toward target.
    //   3. Integrate position.
    //   4. HARD CONSTRAINT: project boat out of every island's no-go disc.
    //
    // No layered defenses, no local-avoidance, no cooldowns. The hard
    // constraint guarantees no collision; the picker guarantees a viable path.

    // No-go disc radius for an island (single source of truth).
    var noGoRadius = (int i) => navIslands[i].worldHalfW + islandClearance;

    // Is point (px, pz) outside ALL no-go discs (with optional extra margin)?
    var pointFree = (double px, double pz, double extraMargin) =>
    {
        for (var i = 0; i < navIslands.Length; i++)
        {
            var ii = navIslands[i];
            var dx = px - ii.wx;
            var dz = pz - ii.wz;
            if (Math.Sqrt(dx * dx + dz * dz) < noGoRadius(i) + extraMargin) return false;
        }
        return true;
    };

    // Does the SEGMENT (a)-(b) keep `extraMargin` clear of every no-go disc?
    // `extraMargin = 0` means the path just barely misses the no-go boundary.
    var segmentFree = (double ax, double az, double bx, double bz, double extraMargin) =>
    {
        var sx = bx - ax;
        var sz = bz - az;
        var seg2 = sx * sx + sz * sz;
        if (seg2 < 1e-6) return true;
        for (var i = 0; i < navIslands.Length; i++)
        {
            var ii = navIslands[i];
            var dx = ii.wx - ax;
            var dz = ii.wz - az;
            var tp = Math.Max(0.0, Math.Min(1.0, (dx * sx + dz * sz) / seg2));
            var cx = ax + sx * tp;
            var cz = az + sz * tp;
            var pdx = ii.wx - cx;
            var pdz = ii.wz - cz;
            if (Math.Sqrt(pdx * pdx + pdz * pdz) < noGoRadius(i) + extraMargin) return false;
        }
        return true;
    };

    // Pick a new TARGET DEPTH (wz only). The lateral wx is determined by the
    // ambient drift, not by the picker. We validate the candidate by checking
    // that (lateralWxNow, candidateWz) is a free point AND the straight path
    // from boat to it doesn't cross any no-go disc.
    Func<double, double, double, double, double, double, (bool ok, double wz)> tryPickDepth =
        (fromWx, fromWz, lateralWxNow, minDepthDelta, endpointMargin, pathMargin) =>
    {
        var midWz = (boatMinWz + boatMaxWz) * 0.5;
        var fromIsClose = fromWz < midWz;
        for (var k = 0; k < targetPickerAttempts; k++)
        {
            // Depth bias: prefer the OPPOSITE half so the boat swings between
            // close (huge boat) and far (tiny near horizon).
            double wz;
            if (navRng.NextDouble() < targetDepthBias)
            {
                if (fromIsClose) wz = midWz + 0.5 + navRng.NextDouble() * (boatMaxWz - 1.0 - midWz);
                else             wz = boatMinWz + 0.5 + navRng.NextDouble() * (midWz - boatMinWz - 1.0);
            }
            else
            {
                wz = boatMinWz + 0.5 + navRng.NextDouble() * (boatMaxWz - boatMinWz - 1.0);
            }
            if (Math.Abs(wz - fromWz) < minDepthDelta) continue;
            if (!pointFree(lateralWxNow, wz, endpointMargin)) continue;
            if (!segmentFree(fromWx, fromWz, lateralWxNow, wz, pathMargin)) continue;
            return (true, wz);
        }
        return (false, 0.0);
    };
    var pickNewTargetDepth = (double fromWx, double fromWz, double lateralWxNow) =>
    {
        var r = tryPickDepth(fromWx, fromWz, lateralWxNow, targetMinTravel, 1.0, pathSafetyMargin);
        if (r.ok) return r.wz;
        r = tryPickDepth(fromWx, fromWz, lateralWxNow, targetMinTravel, 0.0, pathSafetyMargin);
        if (r.ok) return r.wz;
        r = tryPickDepth(fromWx, fromWz, lateralWxNow, 0.0, 0.0, pathSafetyMargin);
        if (r.ok) return r.wz;
        r = tryPickDepth(fromWx, fromWz, lateralWxNow, 0.0, 0.0, 0.0);
        if (r.ok) return r.wz;
        // Last resort: keep previous depth target.
        return boatTargetWz;
    };

    // ============================================================================
    // BOAT MOTION
    //   • LATERAL (wx): defined in CANVAS PIXELS, not world units. Per frame
    //     we convert to wx using the boat's current depth. Result: the boat
    //     appears to oscillate ±N px on canvas REGARDLESS of how close it is
    //     — no perspective magnification at close range → no jitter.
    //   • DEPTH (wz): target-based picker, boat sails at boatDepthSpeed.
    //     Slower than before so the size change feels relaxed.
    // ============================================================================
    // First-frame init
    if (!navInitialized)
    {
        boatPosWx = 0.0;
        boatPosWz = boatBaseWz;
        boatTargetWz = pickNewTargetDepth(0.0, boatBaseWz, 0.0);
        prevTWorld = tWorld;
        navInitialized = true;
    }
    var dtWorld = Math.Max(0.0, Math.Min(0.5, tWorld - prevTWorld));
    prevTWorld = tWorld;

    // LATERAL: oscillation defined in CANVAS PIXELS (consistent screen amp)
    const double lateralCanvasPx = 2.0;          // boat sways ±2 px on canvas
    const double lateralPeriodA  = 92.0;         // slow primary period
    const double lateralPeriodB  = 137.0;        // slower second harmonic for organic feel
    var canvasLateralOffsetPx = lateralCanvasPx * (
          Math.Sin(tWorld * 2.0 * Math.PI / lateralPeriodA) * 0.7
        + Math.Sin(tWorld * 2.0 * Math.PI / lateralPeriodB + 1.7) * 0.3);
    // Convert canvas-px to wx at current depth (sx = camFocal * wx / wz → wx = sx * wz / camFocal)
    boatPosWx = canvasLateralOffsetPx * boatPosWz / camFocal;

    // DEPTH: target-based, slower travel speed for a relaxed feel
    var depthDelta = boatTargetWz - boatPosWz;
    if (Math.Abs(depthDelta) < targetReachRadius)
    {
        boatTargetWz = pickNewTargetDepth(boatPosWx, boatPosWz, boatPosWx);
        depthDelta = boatTargetWz - boatPosWz;
    }
    boatPosWz += Math.Sign(depthDelta) * boatDepthSpeed * dtWorld;
    boatPosWz = Math.Max(boatMinWz, Math.Min(boatMaxWz, boatPosWz));

    // Mirror for debug viz / downstream code
    boatTargetWx = boatPosWx;
    boatVelWx = 0.0;
    boatVelWz = Math.Sign(depthDelta) * boatDepthSpeed;

    // ============================================================================
    // DEBUG TOP-DOWN VIEW — bypass perspective scene, render world as a map
    // ============================================================================
    // Layout:
    //   Camera at canvas (12, 23). +z (depth) goes UP on canvas.
    //   wx is mapped left/right around the camera column.
    //   World wx range [-25, +25] (= recycle range) maps to canvas x [0, 24].
    //   World wz range [0, worldDepthFar] maps to canvas y [23, 0].
    if (sceneDebugTopDown)
    {
        // Background — flat dark sea
        var seaCol     = Color.FromRgb(0.05, 0.18, 0.32);
        var camCol     = Color.FromRgb(1.00, 1.00, 1.00);
        var frustumCol = Color.FromRgb(0.30, 0.40, 0.55);
        var islandCol  = Color.FromRgb(0.20, 0.85, 0.25);
        var islandRing = Color.FromRgb(0.95, 0.95, 0.10);   // bright yellow rim
        var boatCol    = Color.FromRgb(1.00, 0.20, 0.15);

        for (var y = 0; y < 24; y++)
        for (var x = 0; x < 24; x++)
            ctx.SetPixel(x, y, seaCol);

        // Map helpers — world to top-down canvas pixel coords
        var halfRangeX = 25.0;                      // wx ∈ [-25..25] across canvas width
        var pxPerWx = 24.0 / (halfRangeX * 2.0);
        var pxPerWz = 23.0 / worldDepthFar;
        var camPxX  = 12.0;
        var camPxY  = 23.0;
        var w2cx = (double wx) => camPxX + wx * pxPerWx;
        var w2cy = (double wz) => camPxY - wz * pxPerWz;

        // Camera frustum: lines at slope wx/wz = ±0.5 (= half-canvas / camFocal)
        // Compute slope = (canvas_halfwidth_in_world) / wz where canvas spans
        // ±12 px = ±12*wz/camFocal in wx → slope = 12/camFocal.
        var fovSlope = 12.0 / camFocal;
        for (var y = 0; y < 24; y++)
        {
            // world depth at this canvas y
            var wzHere = (camPxY - y) / pxPerWz;
            if (wzHere <= 0) continue;
            // expected wx at the FOV edges
            var wxEdge = wzHere * fovSlope;
            var pxLeft  = (int)Math.Round(w2cx(-wxEdge));
            var pxRight = (int)Math.Round(w2cx( wxEdge));
            if (pxLeft  >= 0 && pxLeft  < 24) ctx.SetPixel(pxLeft,  y, frustumCol);
            if (pxRight >= 0 && pxRight < 24) ctx.SetPixel(pxRight, y, frustumCol);
        }

        // Render EVERY island at its actual world (wx, wz) position — even
        // when it's outside the camera frustum (so vis < 1 in the perspective
        // view). The debug map is an authoritative top-down of the world, so
        // the user can see islands ENTERING from the far right of this canvas
        // and exiting on the left.
        // Draw discs in WORLD coords (sample each canvas pixel back to world,
        // then check world distance) — so the drawn no-go circles match the
        // ACTUAL physical boundaries even though canvas x/z scales differ.
        for (var py = 0; py < 24; py++)
        for (var px = 0; px < 24; px++)
        {
            var pxWx = (px + 0.5 - camPxX) / pxPerWx;
            var pxWz = (camPxY - (py + 0.5)) / pxPerWz;
            for (var i = 0; i < navIslands.Length; i++)
            {
                var ii = navIslands[i];
                var dWx = pxWx - ii.wx;
                var dWz = pxWz - ii.wz;
                var worldDist = Math.Sqrt(dWx * dWx + dWz * dWz);
                if (worldDist < ii.worldHalfW)
                {
                    ctx.SetPixel(px, py, islandCol);
                    break;
                }
                if (Math.Abs(worldDist - (ii.worldHalfW + islandClearance)) < 0.5)
                {
                    ctx.SetPixel(px, py, islandRing);
                    break;
                }
            }
        }

        // ---- TARGET MARKER (where the boat is sailing TO) -----------------
        // Single bright magenta pixel — unambiguous position.
        var targetCol     = Color.FromRgb(1.00, 0.20, 1.00);   // magenta
        var targetLineCol = Color.FromRgb(0.50, 0.55, 0.65);   // dimmer line
        var tcx = w2cx(boatTargetWx);
        var tcy = w2cy(boatTargetWz);
        var bcx = w2cx(boatPosWx);
        var bcy = w2cy(boatPosWz);
        // Faint line from boat to target
        var lineSteps = (int)Math.Ceiling(Math.Sqrt((tcx - bcx) * (tcx - bcx) + (tcy - bcy) * (tcy - bcy)));
        for (var s = 0; s < lineSteps; s++)
        {
            var u = s / (double)lineSteps;
            var lpx = (int)Math.Round(bcx + (tcx - bcx) * u);
            var lpy = (int)Math.Round(bcy + (tcy - bcy) * u);
            if (lpx >= 0 && lpx < 24 && lpy >= 0 && lpy < 24)
                ctx.SetPixel(lpx, lpy, targetLineCol);
        }
        // Single magenta pixel at the target. If target is INSIDE an island
        // (which would be a picker bug), paint it RED instead so the bug
        // is visually obvious.
        var targetIsInsideIsland = !pointFree(boatTargetWx, boatTargetWz, 0.0);
        var actualTargetCol = targetIsInsideIsland
            ? Color.FromRgb(1.0, 0.0, 0.0)   // bright red = bug
            : targetCol;
        var tx = (int)Math.Round(tcx);
        var ty = (int)Math.Round(tcy);
        if (tx >= 0 && tx < 24 && ty >= 0 && ty < 24)
            ctx.SetPixel(tx, ty, actualTargetCol);

        // Boot als 2x2-Block. PURPLE if hard-constraint failed (boat inside
        // some island's no-go disc — should never happen).
        var boatInsideIsland = !pointFree(boatPosWx, boatPosWz, 0.0);
        var actualBoatCol = boatInsideIsland ? Color.FromRgb(0.8, 0.0, 1.0) : boatCol;
        for (var dy = -1; dy <= 0; dy++)
        for (var dx = -1; dx <= 0; dx++)
        {
            var bpx = (int)Math.Round(bcx) + dx;
            var bpy = (int)Math.Round(bcy) + dy;
            if (bpx >= 0 && bpx < 24 && bpy >= 0 && bpy < 24) ctx.SetPixel(bpx, bpy, actualBoatCol);
        }

        // Camera marker (bright pixel at the camera position)
        ctx.SetPixel((int)camPxX, (int)camPxY, camCol);
        return;
    }

    // --- celestial bodies (parameters defined at top of file) --------------
    // Time-of-day phase. Source depends on `sceneTimeMode` (top of file):
    //   0 realtime → drive from ctx.Now (wall-clock); cycle = real 24h
    //   1 animated → fast-forward via `dayDuration` seconds per cycle
    //   2 fixed    → freeze at `sceneFixedHour`
    // Map (clock-hour) → dayProgress: -0.25 shift makes 06:00 (sunrise) = 0.
    //   00:00 midnight → 0.75   06:00 sunrise → 0.00
    //   12:00 midday   → 0.25   18:00 sunset  → 0.50
    double dayProgress;
    // CS0162: branches are intentionally const-folded — that's the point of
    // sceneTimeMode being a tunable compile-time switch.
#pragma warning disable CS0162
    if (sceneTimeMode == 0)        // realtime
    {
        // Sub-minute precision so the sky transitions smoothly through the
        // day instead of in discrete hour steps.
        var hourFrac = ctx.Now.Hour + ctx.Now.Minute / 60.0 + ctx.Now.Second / 3600.0;
        dayProgress = (hourFrac / 24.0 - 0.25 + 1.0) % 1.0;
    }
    else if (sceneTimeMode == 1)   // animated
    {
        dayProgress = (t / dayDuration) % 1.0;
    }
    else                           // 2 = fixed
    {
        dayProgress = (sceneFixedHour / 24.0 - 0.25 + 1.0) % 1.0;
    }
#pragma warning restore CS0162
    var phase = dayProgress * 2.0 * Math.PI;
    var sunX  = sunCenterX  - sunXAmp  * Math.Cos(phase + sunPhaseOffset);
    var sunY  = sunCenterY  - sunYAmp  * Math.Sin(phase + sunPhaseOffset);
    var moonX = moonCenterX - moonXAmp * Math.Cos(phase + moonPhaseOffset);
    var moonY = moonCenterY - moonYAmp * Math.Sin(phase + moonPhaseOffset);

    var tod = Math.Sin(phase);                  // -1 (midnight) .. 1 (midday)
    var dayFactor   = smoothstep(clamp01(tod));
    var nightFactor = smoothstep(clamp01(-tod));

    // Continuous fade as the body sinks below the horizon — atmospheric glow
    // (halo + outer glow) lingers as afterglow long after the body itself is
    // below the horizon line. Reflections fade faster than the sky glow.
    var sunDepth  = Math.Max(0, sunY  - horizonY);
    var moonDepth = Math.Max(0, moonY - horizonY);
    var sunFade           = Math.Max(0, 1.0 - sunDepth  / 10.0);   // glow lingers ~10 px down
    var moonFade          = Math.Max(0, 1.0 - moonDepth / 10.0);
    var sunReflectionFade = Math.Max(0, 1.0 - sunDepth  /  4.0);   // reflection cuts at ~4 px
    var moonReflectionFade= Math.Max(0, 1.0 - moonDepth /  4.0);

    // Moon phase. When moonShowPhases is true, oscillates quasi-randomly
    // between -1 and +1 (sliver → half → full → half → sliver → …); when
    // false (default), the moon is always full.
    //   ±1 = full moon (shadow far from disc)
    //    0 = new moon (shadow exactly on disc)
    //   ±0.5 = half moon
    double moonPhase;
    if (moonShowPhases)
    {
        var moonPhaseRaw = Math.Sin(t * 0.04) + 0.5 * Math.Sin(t * 0.07 + 1.3);
        moonPhase = Math.Max(-1.0, Math.Min(1.0, moonPhaseRaw / 1.5));
    }
    else
    {
        moonPhase = 1.0;  // always full
    }
    // Shadow circle slides ±2*radius (full moon at extremes, new moon at 0)
    var moonShadowX = moonX + moonPhase * 2.0 * moonRadius;

    // Interpolated palette — Sun/Moon/Far horizons get blended per-pixel below
    // depending on each pixel's distance from sun and moon.
    var skyTop          = palette3Mix(skyTopTwilight,          skyTopDay,          skyTopNight,          dayFactor, nightFactor);
    var skyHorizonSun   = palette3Mix(skyHorizonSunTwilight,   skyHorizonSunDay,   skyHorizonSunNight,   dayFactor, nightFactor);
    var skyHorizonMoon  = palette3Mix(skyHorizonMoonTwilight,  skyHorizonMoonDay,  skyHorizonMoonNight,  dayFactor, nightFactor);
    var skyHorizonFar   = palette3Mix(skyHorizonFarTwilight,   skyHorizonFarDay,   skyHorizonFarNight,   dayFactor, nightFactor);
    var waterTopSun     = palette3Mix(waterTopSunTwilight,     waterTopSunDay,     waterTopSunNight,     dayFactor, nightFactor);
    var waterTopMoon    = palette3Mix(waterTopMoonTwilight,    waterTopMoonDay,    waterTopMoonNight,    dayFactor, nightFactor);
    var waterTopFar     = palette3Mix(waterTopFarTwilight,     waterTopFarDay,     waterTopFarNight,     dayFactor, nightFactor);
    var waterDeep       = palette3Mix(waterDeepTwilight,       waterDeepDay,       waterDeepNight,       dayFactor, nightFactor);
    var horizonLineSun  = palette3Mix(horizonLineSunTwilight,  horizonLineSunDay,  horizonLineSunNight,  dayFactor, nightFactor);
    var horizonLineMoon = palette3Mix(horizonLineMoonTwilight, horizonLineMoonDay, horizonLineMoonNight, dayFactor, nightFactor);
    var horizonLineFar  = palette3Mix(horizonLineFarTwilight,  horizonLineFarDay,  horizonLineFarNight,  dayFactor, nightFactor);
    var foamColor       = palette3Mix(foamColorTwilight,       foamColorDay,       foamColorNight,       dayFactor, nightFactor);

    // Sun colors interpolate twilight↔day; moon is its own palette
    var sunCore = mix(sunCoreTwilight, sunCoreDay, dayFactor);
    var sunGlow = mix(sunGlowTwilight, sunGlowDay, dayFactor);
    var sunHalo = mix(sunHaloTwilight, sunHaloDay, dayFactor);

    // How "low" the sun is in the sky: 0 at peak, 1 at horizon.
    // Drives X-stretch (atmospheric scattering streaks horizontally) + glow strength.
    var sunAltitude = horizonY - sunY;
    var sunLowness  = clamp01(1.0 - Math.Max(0, sunAltitude) / sunYAmp);
    var sunXStretch = 1.0 + sunLowness * 1.6;            // 1 (round) → 2.6 (elliptical)
    var sunGlowStr  = 0.45 + sunLowness * 0.55;          // dimmer up high, full strength at horizon

    // Sun and moon contribute SEPARATELY to the horizon — their warm/cool tints
    // appear at their own X positions. The "Far" colour fills everywhere else.

    // ---- AMBIENT LIGHT (sun-only) -------------------------------------------
    // ambientLight = how much diffuse light hits the scene's solid objects.
    // Driven purely by sun altitude — moon never contributes ambient.
    //   sunAltitude ≥ ambientFalloffHeight → ambient = 1 (full colour)
    //   sunAltitude = 0 (sun at horizon)   → ambient = 0 (silhouettes)
    //   sunAltitude < 0 (sun below)        → ambient = 0 (pure black objects)
    var ambientRaw      = clamp01((sunAltitude - ambientStartAltitude) / (ambientFullAltitude - ambientStartAltitude));
    var ambientLight    = smoothstep(ambientRaw);
    var lightTint       = mix(lightTintTwilight, lightTintDay, ambientLight);
    var lightBrightness = ambientLight;

    var hullDeckLit = litColor(hullDeck, lightTint, lightBrightness);
    var hullSideLit = litColor(hullSide, lightTint, lightBrightness);
    var mastLit     = litColor(mastColor, lightTint, lightBrightness);
    var sailLit     = litColor(sailColor, lightTint, lightBrightness);
    // Foam belongs to the WATER — water reflects whatever light source is up
    // (sun OR moon), so foam reads off the night palette directly rather than
    // being multiplied to black by ambient.
    var foamColorLit = foamColor;

    // ---- background: sky + water + sun glow + moon glow + horizontal pull ---
    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
    {
        var fx = x + 0.5;
        var fy = y + 0.5;

        // Sun gets a horizontal pull (warm tint at horizon below the sun).
        // Moon gets NO horizontal pull at all — it reads as a vertical cone
        // when combined with the moon's own radial halo. Moon shows only its
        // concentric radial glow (computed below).
        var dxFromSun = fx - sunX;
        var sunPull = Math.Exp(-(dxFromSun * dxFromSun) / 50.0) * sunFade;
        var skyHorizonLocal = skyHorizonFar;
        skyHorizonLocal = mix(skyHorizonLocal, skyHorizonSun, sunPull);
        var waterTopLocal = waterTopFar;
        waterTopLocal = mix(waterTopLocal, waterTopSun, sunPull);

        Color c;
        if (fy < horizonY)
        {
            c = mix(skyTop, skyHorizonLocal, fy / horizonY);
        }
        else
        {
            var depth = (fy - horizonY) / (24.0 - horizonY);
            var baseColor = mix(waterTopLocal, waterDeep, Math.Sqrt(depth));
            var w = waveValue(fx, fy, t);
            var modulation = w * 0.09 * (0.4 + depth * 0.6);
            c = new Color(
                clamp01(baseColor.R + modulation),
                clamp01(baseColor.G + modulation),
                clamp01(baseColor.B + modulation));
        }

        // Sun: halo + glow always rendered in the sky band, multiplied by
        // sunFade so they linger (afterglow) as the sun sets. Core (the
        // sharp disc) only when the sun itself is above the horizon.
        // Ellipse stretch + strength depend on how low the sun is.
        if (sunFade > 0.01 && fy < horizonY)
        {
            var dxs = (fx - sunX) / sunXStretch;     // x distance scaled → ellipse
            var dys = fy - sunY;                     // y distance unchanged
            var d2 = dxs * dxs + dys * dys;
            c = mix(c, sunHalo, Math.Exp(-d2 / 22.0) * 0.70 * sunFade * sunGlowStr);
            c = mix(c, sunGlow, Math.Exp(-d2 /  9.0) * 0.85 * sunFade * sunGlowStr);
            if (sunY < horizonY)
                c = mix(c, sunCore, Math.Exp(-d2 / 4.0));   // core stays sharp & bright
        }
        // Moon: small disc with phase shadow. No big radial glow — just a
        // dim halo, then the bright disc carved by an offset shadow circle.
        if (moonFade > 0.01 && fy < horizonY && moonY < horizonY)
        {
            var dxm = fx - moonX;
            var dym = fy - moonY;
            var d2m = dxm * dxm + dym * dym;

            // Tight halo (only 1-2 px around the disc, very subtle)
            // Soft radial glow around the disc — sized + intensified via the
            // moonGlowRadius / moonGlowStrength constants. Gauss falloff so it
            // dies out smoothly. moonGlow is the cooler tint, moonHalo is even
            // dimmer outer edge.
            var glowSigma2 = moonGlowRadius * moonGlowRadius;
            c = mix(c, moonGlow, Math.Exp(-d2m /  glowSigma2)        * moonGlowStrength * moonFade * moonBrightness);
            c = mix(c, moonHalo, Math.Exp(-d2m / (glowSigma2 * 2.5)) * moonGlowStrength * 0.5 * moonFade * moonBrightness);

            // Bright moon disc — only where the shadow circle doesn't cover.
            // Edge-distance AA: full alpha well inside the disc, smoothstep
            // transition over a 1-pixel band at the rim → genuinely round
            // disc instead of the boxy diamond shape that a d² falloff produces.
            var dMoon = Math.Sqrt(d2m);
            var moonAlpha = smoothstep(moonRadius + 0.5 - dMoon);
            if (moonAlpha > 0)
            {
                var dxs = fx - moonShadowX;
                var d2s = dxs * dxs + dym * dym;
                // shadowMask: 1 = pixel is OUTSIDE shadow (visible), 0 = inside shadow
                // 1-px smoothstep transition matches the disc edge.
                var shadowDist = Math.Sqrt(d2s) - moonRadius;
                var shadowMask = smoothstep(shadowDist + 0.5);
                c = mix(c, moonCore, moonAlpha * shadowMask * moonFade * moonBrightness);
            }
        }

        ctx.SetPixel(x, y, c);
    }

    // ---- stars (only at night, sparse, twinkling) -------------------------
    if (nightFactor > 0.05)
    {
        var starColor = Color.FromRgb(0.9, 0.95, 1.0);
        var skyMaxY   = (int)horizonY;
        var totalShapeWeight = starShapeWeightDot
                             + starShapeWeightDiagNwSe
                             + starShapeWeightDiagNeSw
                             + starShapeWeightCross;
        var drawStarPixel = (int px, int py, double b) =>
        {
            if (px < 0 || px >= 24 || py < 0 || py >= skyMaxY) return;
            var existing = ctx.GetPixel(px, py);
            ctx.SetPixel(px, py, mix(existing, starColor, b));
        };
        for (var sy = 0; sy < skyMaxY; sy++)
        for (var sx = 0; sx < 24; sx++)
        {
            var h = hash(sx, sy);
            var starThreshold = 1.0 - starDensity;
            if (h < starThreshold) continue;
            var twinkle = 0.5 + 0.5 * Math.Sin(t * starTwinkleSpeed + h * 100.0);
            var brightness = (h - starThreshold) / starDensity * nightFactor * twinkle * starBrightness;
            if (brightness < 0.05) continue;

            // Centre pixel — always drawn.
            drawStarPixel(sx, sy, brightness);

            // Pick a shape via a second, uncorrelated hash, weighted by the
            // shape distribution constants. Same star → same shape (stable).
            var pick = hash(sx + 101, sy + 211) * totalShapeWeight;
            var rayB = brightness * starRayBrightness;
            var t1 = starShapeWeightDot;
            var t2 = t1 + starShapeWeightDiagNwSe;
            var t3 = t2 + starShapeWeightDiagNeSw;
            if (pick < t1)
            {
                // Dot only — no rays.
            }
            else if (pick < t2)
            {
                // "\" — top-left + bottom-right
                drawStarPixel(sx - 1, sy - 1, rayB);
                drawStarPixel(sx + 1, sy + 1, rayB);
            }
            else if (pick < t3)
            {
                // "/" — top-right + bottom-left
                drawStarPixel(sx + 1, sy - 1, rayB);
                drawStarPixel(sx - 1, sy + 1, rayB);
            }
            else
            {
                // "+" — up + down + left + right
                drawStarPixel(sx,     sy - 1, rayB);
                drawStarPixel(sx,     sy + 1, rayB);
                drawStarPixel(sx - 1, sy,     rayB);
                drawStarPixel(sx + 1, sy,     rayB);
            }
        }
    }

    // ---- horizon line ------------------------------------------------------
    // Daytime: full line (warm/cool contrast across sky vs water).
    // Night: the line looks fake — sky and water are both deep blue and the
    // bright divider sticks out. So at night fade it out everywhere EXCEPT
    // right under the moon, where the moonlight implicitly defines the edge.
    //
    // The line also gently follows the wave field instead of being razor-
    // straight. Sub-pixel y-position is split across two pixels for smooth AA.
    var moonHorizonLight = moonY < horizonY ? Math.Abs(moonPhase) : 0.0;
    for (var x = 0; x < 24; x++)
    {
        var fx = x + 0.5;
        var dxFromSun = fx - sunX;
        var sunPull = Math.Exp(-(dxFromSun * dxFromSun) / 50.0) * sunFade;
        var lineColor = mix(horizonLineFar, horizonLineSun, sunPull);

        var dayAlpha = 1.0 - nightFactor;
        var dxFromMoon = fx - moonX;
        var moonSpot = Math.Exp(-(dxFromMoon * dxFromMoon) / 5.0) * moonHorizonLight;
        var nightAlpha = moonSpot * nightFactor;
        var lineAlpha = clamp01(dayAlpha + nightAlpha);
        if (lineAlpha < 0.02) continue;

        // Wobble: small y-offset driven by the same wave field that animates
        // the water below. Stays within ±~horizonWobbleAmount pixels.
        var wobble = waveValue(fx, horizonY, t) * (horizonWobbleAmount / 2.0);
        var lineY = horizonY + wobble;
        var floorY = (int)Math.Floor(lineY);
        var frac = lineY - floorY;
        if (floorY >= 0 && floorY < 24)
            ctx.SetPixel(x, floorY, mix(ctx.GetPixel(x, floorY), lineColor, lineAlpha * (1.0 - frac)));
        if (floorY + 1 >= 0 && floorY + 1 < 24)
            ctx.SetPixel(x, floorY + 1, mix(ctx.GetPixel(x, floorY + 1), lineColor, lineAlpha * frac));
    }

    // ---- foam --------------------------------------------------------------
    for (var y = (int)horizonY + 1; y < 24; y++)
    for (var x = 0; x < 24; x++)
    {
        var fx = x + 0.5;
        var fy = y + 0.5;
        var depth = (fy - horizonY) / (24.0 - horizonY);
        if (depth < 0.10) continue;
        var w = waveValue(fx, fy, t);
        if (w < 1.55) continue;
        var h = hash(x, y);
        var threshold = 0.55 + (1.0 - depth) * 0.30;
        if (h < threshold) continue;
        var strength = clamp01((w - 1.55) * 1.5) * (0.6 + depth * 0.4);
        var existing = ctx.GetPixel(x, y);
        ctx.SetPixel(x, y, mix(existing, foamColorLit, strength));
    }

    // ---- sun reflection on water --------------------------------------------
    // Strongest when the sun is LOW in the sky (near horizon — sunrise/sunset).
    // Use sunCore as base colour for clear contrast against the water.
    {
        var sunHorizonProximity = Math.Max(0, 1.0 - Math.Abs(sunY - horizonY) / 8.0);
        var reflectionAmount = sunHorizonProximity;
        if (reflectionAmount > 0.01)
        {
            for (var sy = (int)horizonY; sy < 24; sy++)
            {
                var d = sy - horizonY;
                var width = 0.8 + Math.Sqrt(d) * 0.9;
                var alpha = Math.Max(0, 1.0 - d / 14.0) * reflectionAmount;
                if (alpha <= 0) continue;
                for (var sx = 0; sx < 24; sx++)
                {
                    var dxFromSunX = sx + 0.5 - sunX;
                    var widthFalloff = Math.Exp(-(dxFromSunX * dxFromSunX) / (width * width));
                    if (widthFalloff < 0.04) continue;
                    var w = waveValue(sx + 0.5, sy + 0.5, t);
                    var existing = ctx.GetPixel(sx, sy);
                    var baseStrength = widthFalloff * alpha * 0.85;
                    ctx.SetPixel(sx, sy, mix(existing, sunCore, baseStrength));
                    if (w > -0.3)
                    {
                        var glowStrength = clamp01((w + 0.3) / 2.0) * widthFalloff * alpha * 0.65;
                        ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), sunGlow, glowStrength));
                    }
                    if (w > 0.5)
                    {
                        var sparkle = clamp01((w - 0.5) / 1.5) * widthFalloff * alpha * 0.95;
                        ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), sunCore, sparkle));
                    }
                }
            }
        }
    }

    // ---- moon reflection on water ------------------------------------------
    // Compact silvery glitter directly under the moon — NOT a vertical column.
    // Strength scales with how full the moon is (|moonPhase|) so a sliver moon
    // barely reflects, a full moon glitters clearly.
    if (moonY < horizonY && Math.Abs(moonPhase) > moonReflectionMinPhase)
    {
        var moonFullness = (Math.Abs(moonPhase) - moonReflectionMinPhase)
                         / (1.0 - moonReflectionMinPhase);     // 0..1
        var maxRows = (int)Math.Ceiling(moonReflectionLength);
        for (var sy = (int)horizonY; sy < Math.Min(24, (int)horizonY + maxRows); sy++)
        {
            var d = sy - horizonY;
            var alpha = Math.Max(0, 1.0 - d / moonReflectionLength) * moonFullness;
            if (alpha <= 0) continue;
            // Cone — narrow at the top (right under the moon) and widening
            // linearly as it goes down toward the viewer.
            var width = moonReflectionWidthBase + d * moonReflectionWidthGrow;
            var maxOffset = (int)Math.Ceiling(width * 2);
            for (var sx = (int)Math.Floor(moonX) - maxOffset; sx <= (int)Math.Ceiling(moonX) + maxOffset; sx++)
            {
                if (sx < 0 || sx >= 24) continue;
                var dxFromMoon = sx + 0.5 - moonX;
                var widthFalloff = Math.Exp(-(dxFromMoon * dxFromMoon) / (width * width));
                if (widthFalloff < 0.08) continue;
                var w = waveValue(sx + 0.5, sy + 0.5, t);

                // Layer 0 — always-visible silvery base beam. Mixes moonGlow
                // (cooler/dimmer) so the beam reads as a clear path on the
                // water even at low waves.
                var baseAlpha = widthFalloff * alpha * moonReflectionBase * moonBrightness;
                ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), moonGlow, baseAlpha));

                // Layer 1 — bright sparkle on crests (moonCore = near-white)
                if (w > 0.0)
                {
                    var sparkle = clamp01(w / 1.5) * widthFalloff * alpha * moonReflectionStrength * moonBrightness;
                    ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), moonCore, sparkle));
                }
            }
        }
    }


    // ---- islands: precompute lit voxel material colours -------------------
    // 5 materials × 2 face shades (top + front). Side faces reuse the front
    // shade for simplicity. All colours are multiplied by ambient light tint.
    var sandTopLit     = litColor(matSandTop,     lightTint, lightBrightness);
    var sandFrontLit   = litColor(matSandFront,   lightTint, lightBrightness);
    var rockTopLit     = litColor(matRockTop,     lightTint, lightBrightness);
    var rockFrontLit   = litColor(matRockFront,   lightTint, lightBrightness);
    var grassTopLit    = litColor(matGrassTop,    lightTint, lightBrightness);
    var grassFrontLit  = litColor(matGrassFront,  lightTint, lightBrightness);
    var trunkTopLit    = litColor(matTrunkTop,    lightTint, lightBrightness);
    var trunkFrontLit  = litColor(matTrunkFront,  lightTint, lightBrightness);
    var leavesTopLit   = litColor(matLeavesTop,   lightTint, lightBrightness);
    var leavesFrontLit = litColor(matLeavesFront, lightTint, lightBrightness);
    var snowLit        = litColor(matSnow,        lightTint, Math.Min(1.0, lightBrightness + 0.05));

    // ---- ISLAND INSTANCES — stream world coords from right to left ---------
    // Each island has a fixed `wz` (depth) and starts at `wx0`. Wind pushes
    // them in -wx at worldStreamSpeed, so they enter on the right side of
    // the camera frustum and exit on the left. When they go off-canvas-left,
    // they recycle: jumped to worldRecycleStartX with a fresh wz so the
    // viewer sees fresh combinations rather than a fixed loop.
    //
    // Visibility is now positional (see STEP 1 at top of scene). We just
    // mirror the same formula here so the renderer matches exactly.
    var islandInsts = new (int shape, double wx, double wz, double sx, double sy, double scale, double worldHalfW, bool flip, double vis)[islandData.Length];
    for (var i = 0; i < islandData.Length; i++)
    {
        var d = islandData[i];

        // World-streaming position (identical to navIslands above).
        var trackLen = worldRecycleStartX - worldRecycleX;
        var raw = ((d.wx0 - worldStreamSpeed * tWorld) - worldRecycleX) % trackLen;
        if (raw < 0) raw += trackLen;
        var wx = worldRecycleX + raw;
        // wz fixed per island (matches navIslands).
        var wz = d.wz;

        // Project to screen
        var sxp   = projectScreenX(wx, wz);
        var syp   = projectScreenY(wz);
        var scale = projectScale(wz);

        // Positional visibility with smoothstep (mirrors STEP 1)
        double tFade;
        if (sxp < -islandFadeMarginPx || sxp > 24.0 + islandFadeMarginPx) tFade = 0.0;
        else if (sxp < 0.0)        tFade = (sxp + islandFadeMarginPx) / islandFadeMarginPx;
        else if (sxp > 24.0)       tFade = (24.0 + islandFadeMarginPx - sxp) / islandFadeMarginPx;
        else                       tFade = 1.0;
        var vis = smoothstep(tFade);

        islandInsts[i] = (d.shape, wx, wz, sxp, syp, scale, d.worldHalfW, d.flip, vis);
    }
    // Sort by depth: BACK (large wz) → FRONT (small wz) so painters' algorithm
    // and the boat in/out occlusion test below both work correctly.
    Array.Sort(islandInsts, (a, b) => b.wz.CompareTo(a.wz));

    // Boat navigation has already run at the top of the scene (so the debug
    // top-down view sees the current state). Here we just bind the integrated
    // world position to the local boatWx/boatWz used by projection + render.
    var boatWx = boatPosWx;
    var boatWz = boatPosWz;

    // ---- project boat to screen + per-frame wave/roll/scale ---------------
    var boatBaseX = projectScreenX(boatWx, boatWz);
    var boatBaseY = projectScreenY(boatWz);
    var boatPxPerWorld = Math.Max(boatMinPxPerWorld, projectScale(boatWz));

    var boatV  = boatBaseY - horizonY;
    var boatVF = boatV + boatV * boatV * 0.025;
    // Boat rides the FULL wave value at its own position
    var localWave = waveValue(boatBaseX, boatBaseY, t);
    var bobAmp    = 0.8 + (boatBaseY - 14.5) / 5.0 * 1.6;
    var boatY     = boatBaseY - (localWave / 2.0) * bobAmp;

    // Tilt = local slope of the water surface around the boat.
    var slope = waveValue(boatBaseX + 1.5, boatY, t) - waveValue(boatBaseX - 1.5, boatY, t);
    var rollAngle = Math.Max(-boatMaxRollAngle, Math.Min(boatMaxRollAngle, -slope * 0.22));
    var rollSin = Math.Sin(rollAngle);
    var rollCos = Math.Cos(rollAngle);

    // Boat sprite size — perspective-scaled from REFERENCE world dimensions.
    var hullDeckHalfW = boatRefHullDeckHalfW * boatPxPerWorld;
    var hullBotHalfW  = boatRefHullBotHalfW  * boatPxPerWorld;
    var mastHeight    = boatRefMastHeight    * boatPxPerWorld;
    var sailBaseWidth = boatRefSailBaseWidth * boatPxPerWorld;
    var splashHalfW   = hullBotHalfW + 1.0;

    // Depth-occlusion test for boat ↔ islands. An island whose world wz is
    // GREATER than the boat's wz is FARTHER from the camera → must render BEHIND
    // the boat. Smaller wz → in front.
    var boatDepthWz = sceneShowBoat ? boatWz : -1.0;   // -1 means "all islands behind boat"

    // Active light source for 3D shading: sun during the day, moon at night.
    var lightSourceX = dayFactor >= nightFactor ? sunX : moonX;

    var renderIsland = (int shape, double xPos, double yBase, double scale, double wz, bool flip, double vis) =>
    {
        if (vis <= 0.001) return;
        if (shape < 0 || shape >= voxelIslands.Length) return;
        var voxels = voxelIslands[shape];

        // Atmospheric haze for far-distance fading
        var hazeRaw = (wz - worldDepthNear) / (worldDepthFar - worldDepthNear);
        var haze    = islandHazeAtNear + clamp01(hazeRaw) * (islandHazeAtFar - islandHazeAtNear);
        var oneMinusHaze = 1.0 - haze;
        var dxFromSun = xPos - sunX;
        var sunPullIs = Math.Exp(-(dxFromSun * dxFromSun) / 50.0) * sunFade;
        var hazeTint  = mix(skyHorizonFar, skyHorizonSun, sunPullIs);

        // Per-frame voxel size on canvas (scaled by perspective).
        // voxW=voxH for square cube faces; voxD foreshortened for oblique projection.
        var voxW = Math.Max(2, (int)Math.Round(scale * 1.5));
        var voxH = voxW;
        var voxD = Math.Max(1, voxH / 2);
        // Top half of voxel = top face; bottom half = front face.
        var topH = voxH / 2;
        var frontH = voxH - topH;

        // Find sizeX (max vx + 1) so we can centre the island laterally
        var sizeX = 1;
        foreach (var v in voxels) if (v.x + 1 > sizeX) sizeX = v.x + 1;

        // Sort voxels back-to-front (high vz first), then top-down (high vy first)
        // → painter's algorithm: front voxels paint over back voxels correctly.
        var sorted = voxels.ToArray();
        Array.Sort(sorted, (a, b) =>
        {
            var c = b.z.CompareTo(a.z);
            if (c != 0) return c;
            return b.y.CompareTo(a.y);
        });

        foreach (var v in sorted)
        {
            // Pick face colours for this material
            Color topCol, frontCol;
            switch (v.mat)
            {
                case 1: topCol = sandTopLit;   frontCol = sandFrontLit;   break;
                case 2: topCol = rockTopLit;   frontCol = rockFrontLit;   break;
                case 3: topCol = grassTopLit;  frontCol = grassFrontLit;  break;
                case 4: topCol = trunkTopLit;  frontCol = trunkFrontLit;  break;
                case 5: topCol = leavesTopLit; frontCol = leavesFrontLit; break;
                default: topCol = sandTopLit;  frontCol = sandFrontLit;   break;
            }

            // Apply atmospheric haze to both face colours
            var topH_color = new Color(
                topCol.R * oneMinusHaze + hazeTint.R * haze,
                topCol.G * oneMinusHaze + hazeTint.G * haze,
                topCol.B * oneMinusHaze + hazeTint.B * haze);
            var frontH_color = new Color(
                frontCol.R * oneMinusHaze + hazeTint.R * haze,
                frontCol.G * oneMinusHaze + hazeTint.G * haze,
                frontCol.B * oneMinusHaze + hazeTint.B * haze);

            // Project voxel to canvas coords. flip mirrors vx around the centre.
            var voxXEff = flip ? (sizeX - 1 - v.x) : v.x;
            var leftX = (int)Math.Round(xPos + (voxXEff - sizeX * 0.5 + 0.5) * voxW - voxW * 0.5);
            // Bottom row of voxel = waterline + back-shift
            var bottomY = (int)Math.Round(yBase - v.y * voxH - v.z * voxD);
            var topY = bottomY - voxH + 1;

            // Paint top face (rows topY..topY+topH-1)
            for (var dy = 0; dy < topH; dy++)
            for (var dx = 0; dx < voxW; dx++)
            {
                var px = leftX + dx;
                var py = topY + dy;
                if (px < 0 || px >= 24 || py < 0 || py >= 24) continue;
                var existing = ctx.GetPixel(px, py);
                ctx.SetPixel(px, py, mix(existing, topH_color, vis));
            }
            // Paint front face (rows topY+topH..bottomY)
            for (var dy = 0; dy < frontH; dy++)
            for (var dx = 0; dx < voxW; dx++)
            {
                var px = leftX + dx;
                var py = topY + topH + dy;
                if (px < 0 || px >= 24 || py < 0 || py >= 24) continue;
                var existing = ctx.GetPixel(px, py);
                ctx.SetPixel(px, py, mix(existing, frontH_color, vis));
            }
        }
    };

    // ---- back-pass: islands BEHIND the boat (and all islands when boat hidden)
    if (sceneShowIslands)
    {
        for (var i = 0; i < islandInsts.Length; i++)
        {
            var ii = islandInsts[i];
            // BACK pass: islands FARTHER than the boat (larger world wz)
            if (ii.wz >= boatDepthWz)
                renderIsland(ii.shape, ii.sx, ii.sy, ii.scale, ii.wz, ii.flip, ii.vis);
        }
    }

    // ---- boat (everything float-positioned with rotation + supersampling AA)
    if (sceneShowBoat) {

    var maxReach = Math.Max(hullDeckHalfW, sailBaseWidth + 1) + mastHeight;
    var bbMinX = (int)Math.Floor(boatBaseX - maxReach);
    var bbMaxX = (int)Math.Ceiling(boatBaseX + maxReach);
    var bbMinY = (int)Math.Floor(boatY - mastHeight - 1);
    var bbMaxY = (int)Math.Ceiling(boatY + 3);

    for (var sy = bbMinY; sy <= bbMaxY; sy++)
    for (var sx = bbMinX; sx <= bbMaxX; sx++)
    {
        if (sx < 0 || sx >= 24 || sy < 0 || sy >= 24) continue;

        var deckCount = 0;
        var hullBotCount = 0;
        var splashCount = 0;
        var mastCount = 0;
        var sailCount = 0;

        for (var subY = 0; subY < 4; subY++)
        for (var subX = 0; subX < 4; subX++)
        {
            var px = sx + (subX + 0.5) / 4.0;
            var py = sy + (subY + 0.5) / 4.0;
            var dx = px - boatBaseX;
            var dy = py - boatY;
            var lx = dx * rollCos + dy * rollSin;
            var ly = -dx * rollSin + dy * rollCos;

            if (ly < 0)
            {
                if (Math.Abs(lx) <= 0.5 && ly >= -mastHeight)
                {
                    mastCount++;
                }
                else if (lx >= 0.5 && ly >= -mastHeight)
                {
                    var sailMaxX = 0.5 + sailBaseWidth * ((ly + mastHeight) / mastHeight);
                    if (lx <= sailMaxX) sailCount++;
                }
            }
            else if (ly < 1.0)
            {
                if (Math.Abs(lx) <= hullDeckHalfW) deckCount++;
            }
            else if (ly < 2.0)
            {
                if (Math.Abs(lx) <= hullBotHalfW) hullBotCount++;
            }
            else if (ly < 3.0)
            {
                if (Math.Abs(lx) <= splashHalfW) splashCount++;
            }
        }

        if (splashCount > 0)
        {
            var coverage = splashCount / 16.0;
            var wLocal = waveValue(sx + 0.5, sy + 0.5, t);
            if (wLocal > 0.2)
            {
                var distFromCentre = Math.Abs(sx + 0.5 - boatBaseX);
                var sideFade = distFromCentre > hullBotHalfW ? 0.5 : 1.0;
                var strength = clamp01((wLocal - 0.2) / 1.8) * 0.55 * sideFade * coverage;
                if (strength >= 0.05)
                    ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), foamColorLit, strength));
            }
        }
        // Coverage → alpha: pixels with coverage ≥ 0.5 (majority of supersamples
        // hit the boat) are FULLY OPAQUE. Only outermost edge pixels (coverage
        // < 0.5) get partial alpha for anti-aliasing. This stops the interior
        // of the boat from reading as see-through against the water.
        if (hullBotCount > 0)
        {
            var coverage = hullBotCount / 16.0;
            var wLocal = waveValue(sx + 0.5, sy + 0.5, t);
            var foamTint = clamp01((wLocal + 0.3) / 2.0) * 0.40;
            var hullSoft = mix(hullSideLit, foamColorLit, foamTint);
            ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), hullSoft, clamp01(coverage * 2.0)));
        }
        if (deckCount > 0)
        {
            ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), hullDeckLit, clamp01(deckCount / 16.0 * 2.0)));
        }
        if (sailCount > 0)
        {
            ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), sailLit, clamp01(sailCount / 16.0 * 2.0)));
        }
        if (mastCount > 0)
        {
            ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), mastLit, clamp01(mastCount / 16.0 * 2.0)));
        }
    }
    } // end of `if (sceneShowBoat)` block

    // ---- front-pass: islands IN FRONT of the boat (only meaningful when
    //      the boat is visible; otherwise the back-pass already drew them all)
    if (sceneShowIslands && sceneShowBoat)
    {
        for (var i = 0; i < islandInsts.Length; i++)
        {
            var ii = islandInsts[i];
            // FRONT pass: islands CLOSER than the boat (smaller world wz)
            if (ii.wz < boatDepthWz)
                renderIsland(ii.shape, ii.sx, ii.sy, ii.scale, ii.wz, ii.flip, ii.vis);
        }
    }

    // ---- TIME OVERLAY ------------------------------------------------------
    // Two tiny 3x5 readouts (no colon): HOURS sit in the LEFT corner, MINUTES
    // in the RIGHT corner — both with a 1 px margin to the canvas edge. The
    // SKY pair (top corners) shows by day, the WATER pair (bottom corners) by
    // night, crossfading via `ambientLight`. `HH` always pads the hour with a
    // leading zero (02 at 2 AM) so the group width stays constant.
    //
    // Per-pixel colour is the brightness inverse of whatever sits underneath
    // ("negative multiplier"): bright bg → dark text, dark bg → light text.
    //
    // Drawn LAST so it sits on top of every other layer (sky, sun, moon,
    // stars, islands, boat) — rendering order means it never gets covered.
    {
        var hh = $"{ctx.Now:HH}";
        var mm = $"{ctx.Now:mm}";

        // Two-digit group width = 3 + 1 + 3 = 7 px (constant for any HH/mm).
        const int groupW    = 7;
        const int edgeMargin = 1;
        var hhStartX = edgeMargin;                  // hours hug the LEFT edge (cols 1..7)
        var mmStartX = 24 - edgeMargin - groupW;    // minutes hug the RIGHT edge (cols 16..22)

        var paintGroup = (string text, int penX0, int yTop, double alpha) =>
        {
            var penX = penX0;
            for (var i = 0; i < text.Length; i++)
            {
                var g = clockGlyph(text[i]);
                var gw = g[0].Length;
                for (var row = 0; row < g.Length; row++)
                for (var col = 0; col < gw; col++)
                {
                    if (g[row][col] != '#') continue;
                    var px = penX + col;
                    var py = yTop + row;
                    if (px < 0 || px >= 24 || py < 0 || py >= 24) continue;

                    // Negative-multiplier: invert the perceived brightness of
                    // the background pixel and paint that as neutral grey.
                    // Using luminance (not raw RGB inversion) keeps text
                    // monochrome — a bright blue sky doesn't push the glyph
                    // toward orange. Final pixel is mixed with the existing
                    // colour by `alpha` so the readout fades in/out smoothly
                    // over the day-night transition.
                    var bg = ctx.GetPixel(px, py);
                    var lum = 0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B;
                    var k = clamp01(1.0 - lum);
                    ctx.SetPixel(px, py, mix(bg, new Color(k, k, k), alpha));
                }
                penX += gw + 1;
            }
        };

        var drawClock = (int yTop, double alpha) =>
        {
            if (alpha <= 0.01) return;
            paintGroup(hh, hhStartX, yTop, alpha);
            paintGroup(mm, mmStartX, yTop, alpha);
        };

        // Position alpha:
        //   - both enabled  → crossfade by ambientLight (sky day, water night)
        //   - only one      → that one shows at full strength all the time
        //   - none          → drawClock's alpha gate skips both
        var topAlpha = clockShowTop
            ? (clockShowBottom ? ambientLight             : 1.0)
            : 0.0;
        var botAlpha = clockShowBottom
            ? (clockShowTop    ? clamp01(1.0 - ambientLight) : 1.0)
            : 0.0;

        drawClock(clockTextYTop, topAlpha * clockTextIntensity);
        drawClock(clockTextYBot, botAlpha * clockTextIntensity);
    }
};
