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
const double moonRadius      =  2.6;      // small disc — moon shouldn't look like a second sun
const bool   moonShowPhases  = false;     // true = animated phases (sliver→half→full→…), false = always full

// --- wave field shaping (break up the strict 1D look) -----------------------
// 0 = old behaviour (pure single-direction wave train).
// Higher values add more dimensionality — but keep them small (≤ 1) so the
// primary wave direction still dominates.
const double waveCrossStrength  = 0.35;   // gentle perpendicular flow on top of main fronts
const double waveDomainWarp     = 0.45;   // bends primary wave fronts so they're not perfectly straight

// --- moon reflection on water -----------------------------------------------
const double moonReflectionLength   = 9.0;   // pixel rows the reflection extends downward
const double moonReflectionWidth    = 0.7;   // multiplier for column width (smaller = sharper beam)
const double moonReflectionBase     = 0.55;  // alpha of the always-visible silvery base beam
const double moonReflectionStrength = 1.0;   // sparkle alpha multiplier on top of the base
const double moonReflectionMinPhase = 0.3;   // |moonPhase| below this → no reflection

// --- horizon line wobble ----------------------------------------------------
const double horizonWobbleAmount = 0.55;     // 0 = razor-straight, larger = follows waves more

// --- boat drift (where it wanders on screen) --------------------------------
const double boatDriftCenterX = 12.0;
const double boatDriftAmpX    =  4.5;     // how far it wanders left/right of centre
const double boatDriftPeriodX = 62.8;     // 2π / 0.10 — slow side-to-side
const double boatDriftCenterY = 17.0;
const double boatDriftAmpY    =  2.5;     // how far it wanders forward/back (perspective)
const double boatDriftPeriodY = 89.8;     // 2π / 0.07 — even slower
const double boatMaxRollAngle  =  0.15;   // ±radians — limits how far the boat tips
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

// Boat-lighting tints (multiplicative)
var lightTintDay      = Color.FromRgb(1.00, 0.97, 0.85);  // warm white
var lightTintTwilight = Color.FromRgb(1.00, 0.65, 0.45);  // orange
var lightTintNight    = Color.FromRgb(0.45, 0.55, 0.85);  // cool blue
const double lightBrightnessDay      = 1.10;
const double lightBrightnessTwilight = 0.85;
const double lightBrightnessNight    = 0.40;

// Boat base colors
var hullDeck    = Color.FromRgb(0.62, 0.40, 0.22);
var hullSide    = Color.FromRgb(0.42, 0.25, 0.13);
var mastColor   = Color.FromRgb(0.35, 0.20, 0.10);
var sailColor   = Color.FromRgb(1.00, 1.00, 1.00);

var scene = (DrawingContext ctx) =>
{
    var t = ctx.Elapsed.TotalSeconds;

    // --- celestial bodies (parameters defined at top of file) --------------
    var phase = (t / dayDuration) * 2.0 * Math.PI;
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

    // Boat lighting — pre-tint hull/mast/sail for this moment
    var lightTint = palette3Mix(lightTintTwilight, lightTintDay, lightTintNight, dayFactor, nightFactor);
    var lightBrightness =
        lightBrightnessTwilight * (1 - dayFactor - nightFactor) +
        lightBrightnessDay * dayFactor +
        lightBrightnessNight * nightFactor;
    var hullDeckLit = litColor(hullDeck, lightTint, lightBrightness);
    var hullSideLit = litColor(hullSide, lightTint, lightBrightness);
    var mastLit     = litColor(mastColor, lightTint, lightBrightness);
    var sailLit     = litColor(sailColor, lightTint, lightBrightness);
    var foamColorLit = litColor(foamColor, lightTint, Math.Min(1.0, lightBrightness + 0.1));

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
            c = mix(c, moonHalo, Math.Exp(-d2m / 4.0) * 0.30 * moonFade);

            // Bright moon disc — only where the shadow circle doesn't cover.
            // moonAlpha fades smoothly to 0 at the edge for cheap AA.
            var moonAlpha = Math.Max(0, 1.0 - d2m / (moonRadius * moonRadius));
            if (moonAlpha > 0)
            {
                var dxs = fx - moonShadowX;
                var d2s = dxs * dxs + dym * dym;
                // shadowMask: 1 = pixel is OUTSIDE shadow (visible), 0 = inside shadow
                // 1-px transition for soft edge.
                var shadowDist = Math.Sqrt(d2s) - moonRadius;
                var shadowMask = Math.Max(0, Math.Min(1, shadowDist + 0.5));
                c = mix(c, moonCore, moonAlpha * shadowMask * moonFade);
            }
        }

        ctx.SetPixel(x, y, c);
    }

    // ---- stars (only at night, sparse, twinkling) -------------------------
    if (nightFactor > 0.05)
    {
        for (var sy = 0; sy < (int)horizonY; sy++)
        for (var sx = 0; sx < 24; sx++)
        {
            var h = hash(sx, sy);
            if (h < 0.92) continue;
            var twinkle = 0.5 + 0.5 * Math.Sin(t * 4.0 + h * 100.0);
            var brightness = (h - 0.92) / 0.08 * nightFactor * twinkle;
            if (brightness < 0.05) continue;
            var existing = ctx.GetPixel(sx, sy);
            ctx.SetPixel(sx, sy, mix(existing, Color.FromRgb(0.9, 0.95, 1.0), brightness));
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
            for (var sy = (int)horizonY + 1; sy < 24; sy++)
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
        for (var sy = (int)horizonY + 1; sy < Math.Min(24, (int)horizonY + 1 + maxRows); sy++)
        {
            var d = sy - horizonY;
            var alpha = Math.Max(0, 1.0 - d / moonReflectionLength) * moonFullness;
            if (alpha <= 0) continue;
            // Sharp narrow column — width tighter than sun reflection.
            var width = (0.4 + Math.Sqrt(d) * 0.35) * moonReflectionWidth;
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


    // ---- boat (everything float-positioned with rotation + supersampling AA)
    var boatBaseX = boatDriftCenterX + Math.Sin(t * 2.0 * Math.PI / boatDriftPeriodX)       * boatDriftAmpX;
    var boatBaseY = boatDriftCenterY + Math.Sin(t * 2.0 * Math.PI / boatDriftPeriodY + 1.3) * boatDriftAmpY;

    var boatV  = boatBaseY - horizonY;
    var boatVF = boatV + boatV * boatV * 0.025;
    // Boat rides the FULL wave value at its own position — so it stays
    // visually in sync with the brightness modulation AND the foam (both of
    // which use the same waveValue). Bright crests with foam → boat is on top;
    // dark troughs → boat is in the trough.
    var localWave = waveValue(boatBaseX, boatBaseY, t);
    var bobAmp    = 0.8 + (boatBaseY - 14.5) / 5.0 * 1.6;
    var boatY     = boatBaseY - (localWave / 2.0) * bobAmp;

    // Tilt = local slope of the water surface around the boat. If the water is
    // higher on the RIGHT (waveValue larger right than left), the boat lists to
    // the LEFT (rests against the incoming swell), so the mast tip leans LEFT.
    var slope = waveValue(boatBaseX + 1.5, boatY, t) - waveValue(boatBaseX - 1.5, boatY, t);
    var rollAngle = Math.Max(-boatMaxRollAngle, Math.Min(boatMaxRollAngle, -slope * 0.22));
    var rollSin = Math.Sin(rollAngle);
    var rollCos = Math.Cos(rollAngle);

    // Continuous perspective scaling — boat smoothly grows as it drifts
    // closer to the camera (larger boatBaseY) and shrinks as it recedes
    // toward the horizon. Supersampling AA makes the sub-pixel sizes work.
    var boatScale = clamp01((boatBaseY - (boatDriftCenterY - boatDriftAmpY)) / (2.0 * boatDriftAmpY));
    var hullDeckHalfW = boatHullDeckHalfWFar  + boatScale * (boatHullDeckHalfWNear  - boatHullDeckHalfWFar);
    var hullBotHalfW  = boatHullBotHalfWFar   + boatScale * (boatHullBotHalfWNear   - boatHullBotHalfWFar);
    var mastHeight    = boatMastHeightFar     + boatScale * (boatMastHeightNear     - boatMastHeightFar);
    var sailBaseWidth = boatSailBaseWidthFar  + boatScale * (boatSailBaseWidthNear  - boatSailBaseWidthFar);
    var splashHalfW   = hullBotHalfW + 1.0;

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
        // Coverage → alpha: square-root boost so partial-coverage edge pixels
        // appear solid rather than transparent. AA still smooths edges (any
        // coverage between 0 and 1 still gives a partial alpha) but the boat
        // body reads as opaque instead of see-through. coverage 0.25 → 0.50,
        // 0.50 → 0.71, 1.00 → 1.00.
        if (hullBotCount > 0)
        {
            var coverage = hullBotCount / 16.0;
            var wLocal = waveValue(sx + 0.5, sy + 0.5, t);
            var foamTint = clamp01((wLocal + 0.3) / 2.0) * 0.40;
            var hullSoft = mix(hullSideLit, foamColorLit, foamTint);
            ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), hullSoft, Math.Sqrt(coverage) * 0.85));
        }
        if (deckCount > 0)
        {
            ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), hullDeckLit, Math.Sqrt(deckCount / 16.0)));
        }
        if (sailCount > 0)
        {
            ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), sailLit, Math.Sqrt(sailCount / 16.0)));
        }
        if (mastCount > 0)
        {
            ctx.SetPixel(sx, sy, mix(ctx.GetPixel(sx, sy), mastLit, Math.Sqrt(mastCount / 16.0)));
        }
    }

    // (time display removed — scene-only for now)
};
