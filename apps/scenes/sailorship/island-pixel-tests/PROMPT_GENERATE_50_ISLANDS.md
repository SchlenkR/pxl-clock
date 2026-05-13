# Prompt — Generate 50 Pixel-Art Islands for the PXL Clock

> Hand this whole document to another AI. It is self-contained: it tells the AI
> what to read, what to apply, what to avoid, and what to deliver.

---

## Mission

Generate **50 new island designs** for the PXL Clock — a 24×24 RGB LED display.
Deliver them as **a single C# scene file** modeled on the existing
`IslandShowcase20.cs`, with a selector at the top to lock a specific design and
second-by-second cycling when the selector is `null`.

The PXL Clock is a real product (programmable in C# .NET 10). These designs
will be shown as background islands in a sailing scene and as standalone
showcase content. Quality bar is "shippable", not "sketch".


## Required reading before you start

These five files in `apps/scenes/sailorship/island-pixel-tests/` are the
canon. Read them in this order:

1. **`PRINCIPLES.md`** — the 8 design principles. Non-negotiable. The user spent
   days iterating to discover them.
2. **`IslandShowcase20.cs`** — the existing showcase with 20 designs. Your
   output file must mirror its structure (palette block, helpers, dispatch).
3. **`embedded-palms/PalmIsland11_EllipticalSilhouette.cs`** — gold-standard
   example of the elliptical-silhouette principle.
4. **`embedded-palms/PalmIsland12_WideFlat.cs`** — wide-flat variation (~22×6).
5. **`embedded-palms/PalmIsland15_OrganicCove.cs`** — organic asymmetric
   variation with cove + spit.

PalmIsland 11/12/15 are the **visual gold standard**. If your designs don't
hold up next to them, iterate.


## The 8 principles you MUST apply (scaled to size)

1. **Elliptical silhouette with varying row widths.** Every row of the island
   is a *different* width (narrow at top → widest in middle → narrow at
   bottom). Equal-width rows produce the "stacked stripes / color blob" look
   that the user explicitly rejected. Read PRINCIPLES.md for canonical
   width patterns.
2. **One outline pixel** at the silhouette edge of every row, in the darkest
   material color. This is what frames the island against the water.
3. **Inner smaller ellipse as a second material layer** (e.g. grass on sand),
   sitting on top of the larger one and shifted slightly *back* so the front
   of the lower layer stays visible.
4. **Golden transition rim** (warm ochre `cGoldRim`) at the seam between two
   materials.
5. **DKC-style 4–5 shade gradient per material.** Highlights upper-left, mids,
   shadows lower-right, outline at edges. The shading must follow the implied
   3D form, not just be 2D dithering.
6. **Painter's algorithm.** Strict back-to-front draw order:
   sky → water → silhouette → cover layer → goldrim → vertical objects →
   cast shadows → foam ring.
7. **Cylinder shading on cylindrical objects.** Trunks, columns, towers,
   chimneys: 3px wide, three columns of distinct shading
   (highlight – base – shadow), plus an optional dark accent at the base for
   ambient occlusion.
8. **Foam ring at the waterline.** Sparse white pixels in irregular spacing
   on one or two rows below the island. Not a continuous line — tupfer.


## Do's

- **VARY the silhouette shape.** Classic ellipse, wide-flat sandbar, organic
  with cove, atoll ring (two concentric ellipses with hollow center), peanut
  (two overlapping ellipses), vertical cliff wedge, narrow horizontal strip,
  L-shape, spiral, crescent.
- **VARY the size.** Include far-distance miniatures (5–8 px wide, sitting
  high near horizon at y≈10–14 to read as "far away"), mid-size, full-canvas
  detail pieces.
- **VARY the theme aggressively.** Go far beyond what's already done. Concrete
  pools to draw from:
  - **Tropics**: Caribbean cay, mangrove maze, coral reef breaking water,
    bioluminescent night beach, hurricane-ravaged stub
  - **East Asia**: Japanese torii on rock, pagoda island, Chinese karst
    pillar (Halong-Bay style), bamboo grove islet, miniature shrine
  - **Mediterranean (deeper)**: Roman aqueduct ruin, fishing village stacked
    on cliff, abandoned monastery, cypress grove, marble quarry coast
  - **Nordic (deeper)**: stave church, ice floe with seal, runestone islet,
    sauna cabin on lake rock, viking longship hull beached
  - **Tropical/exotic**: African baobab island, Madagascar lemur tree,
    Galapagos giant tortoise rock, Easter Island moai, Polynesian tiki
  - **Arctic/Antarctic**: penguin colony rock, iceberg with tunnel, ice
    cave island, frozen shipwreck mast
  - **Mythic / fantasy**: floating crystal isle, sunken Atlantis spire,
    dragon-skull rock, witch's hut on stilts, mushroom island
  - **Industrial / modern**: rusted oil rig stub, container-port micro,
    abandoned lighthouse-base, decommissioned submarine pen, offshore wind
    turbine mini-platform
  - **Wreckage / decay**: old shipwreck on reef, pirate-style careened hull,
    crashed bush plane on beach, half-sunken plane wing
  - **Natural curios**: arch rock (sea arch), blowhole cliff, hexagonal
    basalt columns (Giant's-Causeway style), thermal-vent fumarole,
    mud-volcano cone
- **Keep palettes coherent within a region.** Don't put a tropical-cyan
  lagoon next to a nordic-grey granite in the same design.
- **Add cast shadows** of vertical objects onto the silhouette where it
  reinforces depth (palm shadow on sand, lighthouse shadow on rock,
  pillar shadow across grass).
- **Reuse existing palette colors** where they fit. Only add a new color when
  a new material genuinely needs it (coral pink, jungle green, basalt black,
  copper-oxide turquoise).
- **Apply the silhouette + outline + DKC principle even on small islands**,
  just scaled down. A 6-px-wide island can still have one outline pixel and
  2–3 shades.


## Don'ts

- **DON'T** make rectangular silhouettes or stacks of equal-width rows. This
  was the #1 failure mode that took the user days to escape. Every row width
  must differ following an ellipse curve (or another deliberate curve for
  non-elliptical shapes).
- **DON'T** skip the outline pixel. It's the difference between
  "island in water" and "color blob on water".
- **DON'T** skip cylinder shading on trunks / columns / towers. Single-color
  trunks read as 2D sticks.
- **DON'T** count a "sunset palette" or "rainy mood" as a new design. That's
  a filter, not a structure. The user explicitly rejected this — variant
  PalmIsland16 was killed for exactly this reason.
- **DON'T** count "denser vegetation" or "two palms instead of one" as a
  meaningfully new design either. New shape, new architecture, new region —
  that's variation. Sprite-detail tweaks are not.
- **DON'T** use special characters (em-dashes `—`, arrows `→`, smart quotes
  `"" ''`) anywhere in the YAML front matter. They break parsing. ASCII only.
- **DON'T** invent colors that clash with the warm/cool coherence of their
  region.
- **DON'T** put structures floating in midair. Every vertical object must
  visibly sit on or extend from the island silhouette.
- **DON'T** stop at safe themes. 20 are already done. You need 50 *new* ones —
  push for genuine variety.
- **DON'T** repeat anything from the existing `IslandShowcase20.cs`:
  - South Pacific: classic palm, atoll lagoon, twin palms, volcano,
    driftwood spit, tiny palm, peanut double-ellipse
  - Mediterranean: akropolis, santorini, white chapel, olive hill, ruined
    columns, harbor, lone pillar
  - Nordic: lighthouse, skerry-houses, pine rock, fjord cliff, red
    boathouse, snow island


## Output format

**Single file:** `apps/scenes/sailorship/island-pixel-tests/IslandShowcase50.cs`

Structure must mirror `IslandShowcase20.cs` exactly:

1. **YAML front matter** (ASCII only):
   ```
   // ---
   // app: IslandShowcase50
   // displayName: Island Showcase 50
   // author: Ronald Schlenker
   // description: Fifty further island design drafts beyond the original 20. Themes span Asia, Caribbean, Arctic, mythic, industrial wreckage and natural curios. Cycles one per second by default. Set selectedIsland 1..50 at the top to lock one design.
   // appType: Scene
   // duration: 60
   // ---
   ```

2. **Package + using:**
   ```csharp
   #:package Pxl@*
   using Pxl.Ui.CSharp;
   ```

3. **Selector at the top:**
   ```csharp
   int? selectedIsland = null;
   ```

4. **Shared palette** — start by copying the full palette block from
   `IslandShowcase20.cs`, then append any new region-specific colors you need.

5. **Scene lambda** `var scene = (RasterSurface ctx) => { ... };` containing:
   - `Put` helper (with bounds check)
   - Sky + water gradient pass (copy from IslandShowcase20)
   - `Sprite` helper for sprite-string drawing
   - Material mappers (`SandMap`, `GrassMap`, `CrownMap`, plus new ones)
   - `FoamRing` and `Trunk` helpers (copy from IslandShowcase20)
   - 50 local functions `Island01()` through `Island50()`
   - Dispatch:
     ```csharp
     var actions = new Action[] { Island01, ..., Island50 };
     int idx;
     if (selectedIsland.HasValue && selectedIsland.Value >= 1 && selectedIsland.Value <= 50)
         idx = selectedIsland.Value - 1;
     else
     {
         var t = ctx.Elapsed.TotalSeconds;
         idx = ((int)Math.Floor(t)) % 50;
         if (idx < 0) idx += 50;
         Put(idx % canvasW, 0, cWaterFoam);  // tiny indicator (wraps if idx >= 24)
     }
     actions[idx]();
     ```

   For the cycle indicator with 50 islands: since canvas is only 24 wide, use
   two rows — top row for `idx % 24`, second row for `idx / 24`. Or pick any
   compact scheme that shows the index unambiguously without polluting the
   sky.


## Sprite-string convention

Each row is a string with one char per pixel. ' ' (space) and '.' = transparent.
Per-material letter convention (already established):

| Material        | Letters                                          |
|-----------------|--------------------------------------------------|
| Sand            | `H` `s` `m`/`M` `d` `o` (outline = sand-wet)    |
| Grass           | `h` `g` `G` `k`                                  |
| White stone     | `H` `S` `M` `D` `E` (highlight → edge)          |
| Granite         | `H` `b` `B` `m` `d` `o`                          |
| Snow            | `H` `s` `m`/`M` `d` `o` (mapped to snow palette) |
| Palm crown      | `v` `V` `L` `C` (frond hi/mid/dark/coconut)      |
| Lava rock       | `H` `b` `d` `G` (glow) `c` (core)                |

When introducing a new material, define a new letter scheme and a new mapper.
Don't shadow existing letter meanings within the same sprite.


## Self-check before delivering

Before you call this done, verify:

1. **It compiles.** Run `dotnet build apps/scenes/sailorship/island-pixel-tests/IslandShowcase50.cs`
   from the repo root. Zero errors.
2. **Selector works.** Setting `selectedIsland = 7` displays only island 7;
   leaving it `null` cycles through 50.
3. **No YAML special chars.** Run `head -10` on the file and visually confirm
   no em-dashes / arrows / smart quotes.
4. **50 are truly distinct.** Group them by shape (ellipse / wedge / ring /
   peanut / strip / etc.) and by theme. If any group has more than ~8
   members, you're repeating.
5. **Each island actually applies the principles.** Spot-check 5 random ones:
   does each have varying row widths? Outline pixels? A multi-shade gradient?
   Foam ring?
6. **Palettes stay regionally coherent.** No nordic granite next to tropical
   palms in the same design.


## Closing reminder

The user explicitly rejected — and made the assistant delete — earlier
attempts that:
- looked like "color blobs" or "stacked stripes"
- treated palette swaps as new designs
- used em-dashes in YAML and broke the parser
- put a palm "in" an island without it visibly resting on the surface

The principles in `PRINCIPLES.md` are the lessons from those failures. They
are not optional. If at any point you find yourself drawing equal-width rows,
or adding a "sunset variant", you have drifted. Go back and look at
PalmIsland11.
