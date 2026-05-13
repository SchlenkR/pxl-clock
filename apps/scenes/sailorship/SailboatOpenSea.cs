// ---
// app: SailboatOpenSea
// displayName: Sailboat — Open Sea
// author: Ronald Schlenker
// description: Wooden sailboat under a 24s day-night cycle — sun, moon, stars, light tint on the boat
// appType: Scene
// duration: 30
// ---

#:package Pxl@*

using Pxl.Ui.CSharp;

// ============================================================================
// TUNABLE PARAMETERS — adjust scene behaviour here
// ============================================================================

// --- scene mode (testing toggles) -------------------------------------------
// When true, the whole day-night cycle animates over `dayDuration` seconds.
// When false, the scene freezes at `sceneFixedHour` (in 24h clock notation)
// so you can study a specific moment.
//   0.0  =  0:00 midnight        12.0 = 12:00 midday
//   6.0  =  6:00 sunrise         18.0 = 18:00 sunset
//   23.5 = 23:30 (= half past 11 PM)
const bool   sceneAnimateDayNight = true;
const double sceneFixedHour       = 8.0;

// Boat visibility toggle — turn off to study just the sky/sea/sun/moon.
const bool   sceneShowBoat    = true;
const bool   sceneShowIslands = true;

// At most this many islands are visible at any one moment. The full pool of
// island instances rotates through visibility slots over `islandScheduleCycle`
// seconds — so the user sees variety without the canvas getting cluttered.
const int    sceneMaxVisibleIslands = 2;
const double islandScheduleCycle    = 60.0;
const double islandFadeFraction     = 0.18;   // % of each slot used for fade-in / fade-out at edges

// --- world geometry ---------------------------------------------------------
const double horizonY    = 10.0;          // y of the horizon line (sky 0..10, water 10..24)
const double dayDuration = 24.0;          // seconds for one full day-night cycle

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

// --- islands ----------------------------------------------------------------
// Each island sits at a `depth` ∈ [0,1]: 0 = far on the horizon, 1 = right
// in the foreground. Depth drives THREE things at once (perspective effect):
//   • screenY (lower on canvas the closer the island)
//   • scale   (bigger the closer)
//   • haze    (more atmospheric blend with the horizon colour the further away)
// Islands drift horizontally — closer ones drift faster (parallax), so the
// boat looks like it's actually sailing past land.
const double islandYAtFar         =  0.0;    // pixels BELOW horizonY for depth=0 (right on horizon)
const double islandYAtNear        =  8.0;    // pixels BELOW horizonY for depth=1 (so closest islands can sit IN FRONT of the boat)
const double islandScaleFar       =  1.05;   // shape scale at depth=0 — bigger so even the far mountain has enough pixels for visible 3D shading
const double islandScaleNear      =  2.30;   // shape scale at depth=1 — foreground island fills a big chunk of canvas
const double islandHazeFar        =  0.50;   // 0..1 blend toward horizon haze colour at depth=0 (was 0.70 — too washed out for the real low-contrast LED panel)
const double islandHazeNear       =  0.05;   // …at depth=1 (mostly opaque)
const double islandDriftSpeedBase =  0.45;   // baseline px/sec drift; per-island multiplier on top
const double islandWrapRange      = 75.0;    // x-wrap distance: even larger now so usually only 1 island is on-screen at a time

// --- island 3D shading ------------------------------------------------------
// Per-pixel directional shading: the side facing the active light source (sun
// during the day, moon at night) brightens; the opposite side dims. Top pixels
// get an additional sky-light boost. Together this gives a clear sense of
// volume — on the low-contrast LED panel, this is what stops islands from
// reading as flat coloured blobs.
const double islandShadeHorizontal =  0.28;   // strength of side-light vs side-shadow (0 = none, ~0.3 = strong)
const double islandShadeVertical   =  0.10;   // top-of-island brightness boost from sky light
const double islandShadeMin        =  0.40;   // shadow side floor — go quite dark for real volume on low-contrast LED panel
const double islandShadeMax        =  1.65;   // lit side ceiling — strong highlight side for clear contrast
// Rim darkening: pixels where the silhouette is curving away from the camera
// (low coverage = edge pixel) get an additional brightness drop. Together with
// the directional shading, this gives every island the classic 3-tone look:
// HIGHLIGHT (lit interior) → MIDTONE (shadow interior) → DEEP SHADOW (rim).
// Without this, even directional shading reads as flat-painted cardboard.
const double islandRimDarken       =  0.40;   // 0 = no rim, 1 = rim goes to black

// --- boat drift (where it wanders on screen) --------------------------------
const double boatDriftCenterX = 12.0;
const double boatDriftAmpX    =  4.5;     // how far it wanders left/right of centre
const double boatDriftPeriodX = 62.8;     // 2π / 0.10 — slow side-to-side
const double boatDriftCenterY = 17.0;
const double boatDriftAmpY    =  2.5;     // how far it wanders forward/back (perspective)
const double boatDriftPeriodY = 89.8;     // 2π / 0.07 — even slower
const double boatMaxRollAngle  =  0.15;   // ±radians — limits how far the boat tips
// Boat actively dodges foreground islands instead of sailing through them.
// When a near island approaches the boat's lateral position, the boat smoothly
// veers AWAY (gaussian repulsion). Tuned so two passes per scene cycle visibly
// "thread through" the archipelago.
const double boatDodgeMinDepth  =  0.55;  // only islands closer than this push the boat
const double boatDodgeFalloff   =  4.5;   // px sigma of the repulsion gaussian (smaller = sharper avoid zone)
const double boatDodgeStrength  =  6.0;   // max px lateral push from a single island
const double boatDodgeClampX    =  3.0;   // keep boat within [clamp, 24-clamp] so it never sails off-screen
// Sprite size is continuously interpolated between FAR (small) and NEAR (large)
// dimensions across the boat's perspective Y range. No discrete switch.
const double boatHullDeckHalfWFar  = 2.8125;
const double boatHullDeckHalfWNear = 4.5;
const double boatHullBotHalfWFar   = 1.6875;
const double boatHullBotHalfWNear  = 3.375;
const double boatMastHeightFar     = 4.5;
const double boatMastHeightNear    = 6.75;
const double boatSailBaseWidthFar  = 3.375;
const double boatSailBaseWidthNear = 5.625;



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
var islandSand   = Color.FromRgb(0.95, 0.80, 0.45);  // warm sand / beach (more orange)
var islandVeg    = Color.FromRgb(0.15, 0.62, 0.20);  // tropical green (more saturated)
var islandRock   = Color.FromRgb(0.50, 0.42, 0.36);  // mountain rock (warmer, lighter — separates from veg)
var islandSnow   = Color.FromRgb(0.95, 0.97, 1.00);  // snowcap (near-white, slightly cool)
var islandTrunk  = Color.FromRgb(0.32, 0.17, 0.08);  // palm trunk (dark warm brown)
var islandFrond  = Color.FromRgb(0.08, 0.62, 0.18);  // palm fronds (vivid green vs veg)
var islandChest  = Color.FromRgb(0.45, 0.22, 0.06);  // treasure chest body (rich dark wood)
var islandGold   = Color.FromRgb(1.00, 0.80, 0.15);  // chest gold lid / metal trim

// ============================================================================
// ISLAND SHAPE LIBRARY — 4 hand-crafted designs, one per depth level
// ============================================================================
// Each shape is sampled in LOCAL coordinates:
//   • lx = horizontal distance from island centre (negative = left)
//   • ly = vertical distance from waterline (NEGATIVE = above water, going up)
// Returns a layer code:
//   0 = transparent      1 = sand        2 = veg        3 = rock
//   4 = snow             5 = palm trunk  6 = palm frond
//   7 = chest body       8 = chest gold
// Shape IDs:
//   0 Mountain        — for FAR (snow-capped silhouette)
//   1 Volcano         — for MID-FAR (sharp rocky cone)
//   2 PalmAtoll       — for MID-NEAR (sand base with prominent palm)
//   3 TreasureCove    — for NEAR (wide sand, palm + treasure chest)
var sampleIslandLayer = (int shapeId, double lx, double ly) =>
{
    switch (shapeId)
    {
        case 0: // Mountain — wide base, snow-capped peak (FAR)
        {
            if (ly > 1.5 || ly < -4.0) return 0;
            // Front sand foot — asymmetric lobe extending DOWN-RIGHT below the
            // main waterline. Breaks the flat-bottom paper-cutout look.
            if (ly > 0.0 && ly <= 1.4)
            {
                var ff = lx - 1.9;
                if (ff >= -1.0 && ff <= 1.0 && ly <= 1.4 - ff * ff * 0.7) return 1;
            }
            // Back rocky ridge — small bump tucked LEFT-BEHIND the main peak,
            // gives the silhouette parallax depth.
            if (ly > -2.2 && ly <= -0.5)
            {
                var br = lx + 2.6;
                if (br >= -0.8 && br <= 0.8 && ly <= -0.5 - 0.4 * (1.0 - br * br)) return 3;
            }
            // Main triangular mountain
            if (ly <= 0.4)
            {
                var halfW = 2.5 + ly * 0.5;
                if (halfW > 0 && Math.Abs(lx) <= halfW)
                {
                    if (ly > -0.3) return 1;     // sand shore
                    if (ly > -1.4) return 2;     // veg lower slopes
                    if (ly > -3.0) return 3;     // rock middle / upper
                    return 4;                    // snow cap
                }
            }
            return 0;
        }
        case 1: // Volcano — sharp rocky cone (MID-FAR)
        {
            if (ly > 1.5 || ly < -4.0) return 0;
            // Front rock-scree foot — pile of rocks at the lava base, LEFT-FRONT
            if (ly > 0.0 && ly <= 1.0)
            {
                var ff = lx + 1.6;
                if (ff >= -1.0 && ff <= 1.0 && ly <= 1.0 - ff * ff * 0.6) return 3;
            }
            // Back smaller satellite cone on the RIGHT
            if (ly > -1.6 && ly <= -0.4)
            {
                var br = lx - 2.3;
                if (br >= -0.7 && br <= 0.7 && ly <= -0.4 - 0.5 * (1.0 - br * br)) return 3;
            }
            // Main cone
            if (ly <= 0.4)
            {
                var halfW = 2.2 + ly * 0.5;
                if (halfW > 0 && Math.Abs(lx) <= halfW)
                {
                    if (ly > -0.5) return 1;     // sand base
                    return 3;                    // rock cone all the way up
                }
            }
            return 0;
        }
        case 2: // PalmAtoll — small sand atoll with ONE big palm tree (MID-NEAR)
        {
            if (ly > 1.5) return 0;
            // Front sand spit — extends RIGHT-FRONT of the atoll, below water
            if (ly > 0.0 && ly <= 1.3)
            {
                var ff = lx - 1.6;
                if (ff >= -1.2 && ff <= 1.2 && ly <= 1.3 - ff * ff * 0.5) return 1;
            }
            // Back small sand hump on the LEFT (peeks behind main mound)
            if (ly > -1.0 && ly <= -0.2)
            {
                var br = lx + 1.9;
                if (br >= -0.7 && br <= 0.7 && ly <= -0.2 - 0.6 * (1.0 - br * br)) return 1;
            }
            // Main sand mound — slightly raised in centre, bounded above water
            if (ly > -0.5 && ly <= 0.4 && Math.Abs(lx) <= 1.8) return 1;
            if (ly > -1.0 && ly <= -0.5 && Math.Abs(lx) <= 1.0) return 1;
            // ----- palm — pointy crown, drooping fronds out and down -----
            if (ly > -4.0 && ly <= -3.5 && Math.Abs(lx) <= 0.4) return 6;
            if (ly > -3.5 && ly <= -3.0 && Math.Abs(lx) <= 1.4) return 6;
            if (ly > -3.0 && ly <= -2.5 && Math.Abs(lx) > 0.4 && Math.Abs(lx) <= 2.4) return 6;
            if (ly > -3.0 && ly <= -1.0 && Math.Abs(lx) <= 0.4) return 5;
            return 0;
        }
        case 3: // TreasureCove — wide sand island, palm on the LEFT, chest on the RIGHT (NEAR)
        {
            if (ly > 1.5) return 0;
            // Front sand foot — biggest of all, broad and low — major 3D depth
            if (ly > 0.0 && ly <= 1.4)
            {
                var ff = lx - 0.6;
                if (ff >= -2.0 && ff <= 2.0 && ly <= 1.4 - ff * ff * 0.28) return 1;
            }
            // Back dune behind the palm (LEFT) — smaller, peeks above main sand
            if (ly > -1.5 && ly <= -0.4)
            {
                var br = lx + 2.5;
                if (br >= -0.8 && br <= 0.8 && ly <= -0.4 - 0.7 * (1.0 - br * br)) return 1;
            }
            // Main sand base — wide lower tier + raised middle
            if (ly > -0.5 && ly <= 0.4 && Math.Abs(lx) <= 3.0) return 1;
            if (ly > -1.0 && ly <= -0.5 && Math.Abs(lx) <= 2.0) return 1;
            // tiny veg tuft between palm and chest
            if (ly > -1.4 && ly <= -1.0 && Math.Abs(lx + 0.2) <= 0.4) return 2;

            // ----- palm tree on the LEFT (centred at lx ≈ -1.4) -----
            var ptx = lx + 1.4;
            if (ly > -4.0 && ly <= -3.5 && Math.Abs(ptx) <= 0.4) return 6;
            if (ly > -3.5 && ly <= -3.0 && Math.Abs(ptx) <= 1.2) return 6;
            if (ly > -3.0 && ly <= -2.5 && Math.Abs(ptx) > 0.4 && Math.Abs(ptx) <= 2.0) return 6;
            if (ly > -3.0 && ly <= -1.0 && Math.Abs(ptx) <= 0.4) return 5;

            // ----- treasure chest on the RIGHT (centred at lx ≈ +1.5) -----
            var ccx = lx - 1.5;
            if (ly > -1.0 && ly <= -0.5 && Math.Abs(ccx) <= 0.7) return 7;
            if (ly > -1.5 && ly <= -1.0 && Math.Abs(ccx) <= 0.8) return 8;

            return 0;
        }
        default: return 0;
    }
};

// ============================================================================
// ISLAND INSTANCES — 4 islands across 4 depth levels, wide-spaced so usually
// only ONE is on-screen at any moment. Each gets a distinctive shape so the
// viewer recognises "ah, a mountain… now a volcano… now palms…".
// ============================================================================
// (shape, depth, startX, speedMult, flip)
//   depth ∈ [0,1]: 0 = far on horizon, 1 = foreground
//   startX in 0..islandWrapRange — where the island sits at t=0 (then drifts left)
//   speedMult: parallax — closer ones drift faster
//   flip: true = horizontally mirror the shape (free variety)
var islandData = new (int shape, double depth, double startX, double speed, bool flip)[]
{
    // 4 instances across wrap range (75). Two are foreground so the boat
    // visibly passes IN FRONT of (or BEHIND) a near island multiple times in
    // one scene cycle, instead of just one rare event.
    (0, 0.10,  4.0, 0.50, false),  // Mountain     — FAR (slow, hazy on horizon)
    (1, 0.42, 25.0, 1.00, true),   // Volcano      — MID-FAR
    (3, 0.78, 47.0, 1.65, false),  // TreasureCove — NEAR (clear passing in front of boat)
    (2, 0.93, 65.0, 2.10, true),   // PalmAtoll    — VERY NEAR (passes large + clearly in front)
};

var scene = (RasterSurface ctx) =>
{
    var t = ctx.Elapsed.TotalSeconds;

    // --- celestial bodies (parameters defined at top of file) --------------
    // Time-of-day phase. Either animates with elapsed time, or stays frozen
    // at sceneFixedTimeOfDay for testing a specific moment.
    // Map sceneFixedHour (0..24 clock notation) to dayProgress (0..1 cycle):
    //   00:00 midnight → 0.75   06:00 sunrise → 0.00
    //   12:00 midday   → 0.25   18:00 sunset  → 0.50
    var dayProgress = sceneAnimateDayNight
        ? (t / dayDuration) % 1.0
        : (sceneFixedHour / 24.0 - 0.25 + 1.0) % 1.0;
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
            c = mix(c, moonGlow, Math.Exp(-d2m /  glowSigma2)        * moonGlowStrength * moonFade);
            c = mix(c, moonHalo, Math.Exp(-d2m / (glowSigma2 * 2.5)) * moonGlowStrength * 0.5 * moonFade);

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
                c = mix(c, moonCore, moonAlpha * shadowMask * moonFade);
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
                var baseAlpha = widthFalloff * alpha * moonReflectionBase;
                ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), moonGlow, baseAlpha));

                // Layer 1 — bright sparkle on crests (moonCore = near-white)
                if (w > 0.0)
                {
                    var sparkle = clamp01(w / 1.5) * widthFalloff * alpha * moonReflectionStrength;
                    ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), moonCore, sparkle));
                }
            }
        }
    }


    // ---- boat BASE position (drift only — dodge is added below after we
    //      know where the foreground islands actually are) ------------------
    var boatBaseX = boatDriftCenterX + Math.Sin(t * 2.0 * Math.PI / boatDriftPeriodX)       * boatDriftAmpX;
    var boatBaseY = boatDriftCenterY + Math.Sin(t * 2.0 * Math.PI / boatDriftPeriodY + 1.3) * boatDriftAmpY;

    // ---- islands: precompute lit colours, drift positions, render helper ---
    var sandLit  = litColor(islandSand,  lightTint, lightBrightness);
    var vegLit   = litColor(islandVeg,   lightTint, lightBrightness);
    var rockLit  = litColor(islandRock,  lightTint, lightBrightness);
    var snowLit  = litColor(islandSnow,  lightTint, Math.Min(1.0, lightBrightness + 0.05));
    var trunkLit = litColor(islandTrunk, lightTint, lightBrightness);
    var frondLit = litColor(islandFrond, lightTint, lightBrightness);
    var chestLit = litColor(islandChest, lightTint, lightBrightness);
    var goldLit  = litColor(islandGold,  lightTint, Math.Min(1.0, lightBrightness + 0.10));  // gold gets a slight boost so it pops

    // Compute current x position + screen Y for every island, then sort
    // back-to-front so we can draw islands BEHIND the boat first, then the
    // boat, then islands IN FRONT of the boat (proper occlusion).
    //
    // Each island also gets a `vis ∈ [0,1]` from a time-slotted scheduler:
    // out of N total instances, only `sceneMaxVisibleIslands` are inside their
    // slot at any moment. Slots are evenly offset in time and have a smooth
    // fade-in / fade-out at the edges so islands don't pop in.
    var slotFraction = Math.Min(1.0, sceneMaxVisibleIslands / (double)islandData.Length);
    var schedulePhase = (t / islandScheduleCycle) % 1.0;
    var islandInsts = new (int shape, double depth, double xPos, double screenY, bool flip, double vis)[islandData.Length];
    for (var i = 0; i < islandData.Length; i++)
    {
        var d = islandData[i];
        var raw = (d.startX - d.speed * islandDriftSpeedBase * t) % islandWrapRange;
        if (raw < 0) raw += islandWrapRange;
        var xPos = raw - 8.0;   // wrap range starts off-screen left at -8
        var screenY = horizonY + islandYAtFar + d.depth * (islandYAtNear - islandYAtFar);

        // Visibility schedule: island is "active" while its localPhase is
        // inside [0, slotFraction). Trapezoid envelope with fade edges.
        var localPhase = (schedulePhase - i / (double)islandData.Length + 1.0) % 1.0;
        double vis;
        if (localPhase >= slotFraction)
        {
            vis = 0.0;
        }
        else
        {
            var fadeWidth = slotFraction * islandFadeFraction;
            if (localPhase < fadeWidth)                         vis = localPhase / fadeWidth;
            else if (localPhase > slotFraction - fadeWidth)      vis = (slotFraction - localPhase) / fadeWidth;
            else                                                 vis = 1.0;
        }

        islandInsts[i] = (d.shape, d.depth, xPos, screenY, d.flip, vis);
    }
    Array.Sort(islandInsts, (a, b) => a.screenY.CompareTo(b.screenY));

    // ---- BOAT DODGE: lateral push away from approaching foreground islands.
    // For each foreground island (depth ≥ boatDodgeMinDepth), apply a gaussian
    // repulsion centred on its xPos. The boat is fixed in the middle of the
    // canvas, so islands "navigate around" it just by physics — when one is
    // close in screen-X, the boat veers to the opposite side. Visibility is
    // factored in so fading-out islands don't keep pushing.
    var dodgeOffsetX = 0.0;
    foreach (var ii in islandInsts)
    {
        if (ii.depth < boatDodgeMinDepth || ii.vis < 0.05) continue;
        var dx = boatBaseX - ii.xPos;
        var force = Math.Exp(-(dx * dx) / (boatDodgeFalloff * boatDodgeFalloff));
        var dir = dx >= 0 ? 1.0 : -1.0;
        dodgeOffsetX += dir * force * boatDodgeStrength * ii.vis * ii.depth;
    }
    boatBaseX = Math.Max(boatDodgeClampX, Math.Min(24.0 - boatDodgeClampX, boatBaseX + dodgeOffsetX));

    // ---- boat positioning (post-dodge) — wave bob, slope-driven roll, scale --
    var boatV  = boatBaseY - horizonY;
    var boatVF = boatV + boatV * boatV * 0.025;
    // Boat rides the FULL wave value at its own position — so it stays
    // visually in sync with the brightness modulation AND the foam.
    var localWave = waveValue(boatBaseX, boatBaseY, t);
    var bobAmp    = 0.8 + (boatBaseY - 14.5) / 5.0 * 1.6;
    var boatY     = boatBaseY - (localWave / 2.0) * bobAmp;

    // Tilt = local slope of the water surface around the boat.
    var slope = waveValue(boatBaseX + 1.5, boatY, t) - waveValue(boatBaseX - 1.5, boatY, t);
    var rollAngle = Math.Max(-boatMaxRollAngle, Math.Min(boatMaxRollAngle, -slope * 0.22));
    var rollSin = Math.Sin(rollAngle);
    var rollCos = Math.Cos(rollAngle);

    // Continuous perspective scaling.
    var boatScale = clamp01((boatBaseY - (boatDriftCenterY - boatDriftAmpY)) / (2.0 * boatDriftAmpY));
    var hullDeckHalfW = boatHullDeckHalfWFar  + boatScale * (boatHullDeckHalfWNear  - boatHullDeckHalfWFar);
    var hullBotHalfW  = boatHullBotHalfWFar   + boatScale * (boatHullBotHalfWNear   - boatHullBotHalfWFar);
    var mastHeight    = boatMastHeightFar     + boatScale * (boatMastHeightNear     - boatMastHeightFar);
    var sailBaseWidth = boatSailBaseWidthFar  + boatScale * (boatSailBaseWidthNear  - boatSailBaseWidthFar);
    var splashHalfW   = hullBotHalfW + 1.0;

    // The y coordinate the boat actually stands at — islands with screenY
    // below this should render IN FRONT of the boat, those above behind.
    var boatDepthY = sceneShowBoat ? boatY : double.PositiveInfinity;

    // Active light source for 3D shading: sun during the day, moon at night.
    // dayFactor + nightFactor = 1 during clear day/night, both lower at twilight;
    // we just pick the dominant one. lightDirX is the SIGN — negative = light
    // comes from the LEFT, so the LEFT side of an island brightens.
    var lightSourceX = dayFactor >= nightFactor ? sunX : moonX;

    var renderIsland = (int shape, double depth, double xPos, bool flip, double vis) =>
    {
        if (vis <= 0.001) return;   // skip entirely when scheduled out
        var scale = islandScaleFar + depth * (islandScaleNear - islandScaleFar);
        var yBase = horizonY + islandYAtFar + depth * (islandYAtNear - islandYAtFar);
        var haze  = islandHazeFar + depth * (islandHazeNear - islandHazeFar);
        var oneMinusHaze = 1.0 - haze;

        // Local horizon haze tint at this island's centre x — picks up sun warmth
        var dxFromSun = xPos - sunX;
        var sunPullIs = Math.Exp(-(dxFromSun * dxFromSun) / 50.0) * sunFade;
        var hazeTint  = mix(skyHorizonFar, skyHorizonSun, sunPullIs);

        // Light direction (LEFT = negative, RIGHT = positive). When the light
        // is to the right of the island, its right-hand pixels (lx > 0) catch
        // more light → multiplying lx by lightDirX gives a positive value on
        // the LIT side and negative on the SHADOW side.
        var lightDirX = lightSourceX >= xPos ? 1.0 : -1.0;

        var halfReachX = 4.5 * scale + 1.0;
        var topReach   = 6.0 * scale + 1.0;
        var botReach   = 1.6 * scale + 1.0;   // allow shapes to extend BELOW waterline (front-foot lobes)
        var bbMinX = (int)Math.Floor(xPos - halfReachX);
        var bbMaxX = (int)Math.Ceiling(xPos + halfReachX);
        var bbMinY = (int)Math.Floor(yBase - topReach);
        var bbMaxY = (int)Math.Ceiling(yBase + botReach);

        for (var py = bbMinY; py <= bbMaxY; py++)
        for (var px = bbMinX; px <= bbMaxX; px++)
        {
            if (px < 0 || px >= 24 || py < 0 || py >= 24) continue;

            // 4×4 supersample (16 sub-samples) — gives smoother edges on the
            // larger near-distance islands and resolves small features like
            // the chest's gold band cleanly.
            var counts = new int[9];   // index 0 unused (transparent); 1..8 = layers
            var hits = 0;
            for (var ssy = 0; ssy < 4; ssy++)
            for (var ssx = 0; ssx < 4; ssx++)
            {
                var fx = px + (ssx + 0.5) / 4.0;
                var fy = py + (ssy + 0.5) / 4.0;
                var lx = (fx - xPos) / scale;
                var ly = (fy - yBase) / scale;
                if (flip) lx = -lx;
                var layer = sampleIslandLayer(shape, lx, ly);
                if (layer != 0) { counts[layer]++; hits++; }
            }
            if (hits == 0) continue;

            // Pick the dominant layer for this pixel
            var bestLayer = 0;
            var bestCount = 0;
            for (var i = 1; i < counts.Length; i++)
                if (counts[i] > bestCount) { bestCount = counts[i]; bestLayer = i; }

            var layerColor = bestLayer switch
            {
                1 => sandLit,
                2 => vegLit,
                3 => rockLit,
                4 => snowLit,
                5 => trunkLit,
                6 => frondLit,
                7 => chestLit,
                8 => goldLit,
                _ => sandLit,
            };

            // ---- 3D directional shading -----------------------------------
            // Sample shading at the pixel CENTRE (not per sub-sample) — that
            // gives consistent volume across all pixels of one feature.
            var lxC = ((px + 0.5) - xPos) / scale;
            var lyC = ((py + 0.5) - yBase) / scale;
            if (flip) lxC = -lxC;
            // Side facing the light: positive on lit side, negative on shadow side.
            var horizontalShade = lxC * lightDirX;
            // Top of the island gets a sky-light boost; lyC is negative going up,
            // so -lyC is positive for higher pixels.
            var verticalShade = -lyC;
            var shadeFactor = horizontalShade * islandShadeHorizontal
                            + verticalShade   * islandShadeVertical;
            var shadeMult = Math.Max(islandShadeMin,
                            Math.Min(islandShadeMax, 1.0 + shadeFactor));

            // ---- rim darkening (the big anti-cardboard trick) ------------
            // Pixels with low sub-sample coverage are at the silhouette edge,
            // i.e. the surface is curving AWAY from the camera there. Darken
            // them. Combined with the directional shading above, this yields
            // the classic 3-tone pixel-art look (highlight → midtone → rim
            // shadow) and breaks the flat-cardboard appearance.
            var coverage = hits / 16.0;
            var rimFactor = Math.Max(0.0, 1.0 - coverage * 1.4);   // 0 if fully interior, ~1 at silhouette edge
            shadeMult *= 1.0 - rimFactor * islandRimDarken;

            var shadedColor = new Color(
                clamp01(layerColor.R * shadeMult),
                clamp01(layerColor.G * shadeMult),
                clamp01(layerColor.B * shadeMult));

            // Atmospheric haze: blend the SHADED colour toward the local horizon tint
            var hazedColor = new Color(
                shadedColor.R * oneMinusHaze + hazeTint.R * haze,
                shadedColor.G * oneMinusHaze + hazeTint.G * haze,
                shadedColor.B * oneMinusHaze + hazeTint.B * haze);

            // Use FULL coverage (no sqrt boost) for blending now — rim pixels
            // are already darkened by the rim factor, and going opaque at the
            // edges would erase the rim shadow back into a hard outline.
            var alpha = coverage;
            var existing = ctx.GetPixel(px, py);
            ctx.SetPixel(px, py, mix(existing, hazedColor, alpha * vis));
        }
    };

    // ---- back-pass: islands BEHIND the boat (and all islands when boat hidden)
    if (sceneShowIslands)
    {
        for (var i = 0; i < islandInsts.Length; i++)
        {
            var ii = islandInsts[i];
            if (ii.screenY <= boatDepthY)
                renderIsland(ii.shape, ii.depth, ii.xPos, ii.flip, ii.vis);
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
            if (ii.screenY > boatDepthY)
                renderIsland(ii.shape, ii.depth, ii.xPos, ii.flip, ii.vis);
        }
    }

    // (time display removed — scene-only for now)
};
