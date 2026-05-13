# 3D-Pixel-Art-Prinzipien für Inseln (24×24)

Festgehalten nach mehreren Iterationsrunden. Diese Prinzipien sind das, was den
Unterschied macht zwischen "Farbklecks / aufeinandergestapelte Kästen" und einer
Insel, die wirklich plastisch im Wasser steht. Sie sind **stil-unabhängig** und
gelten genauso für Vulkan-, Fels-, Korallen- oder Leuchtturm-Inseln — nicht nur
für Palmen.

Kanonische Referenzen (alle in `embedded-palms/`):
- `PalmIsland11_EllipticalSilhouette.cs` — der Durchbruch, "klassische" elliptische Form
- `PalmIsland12_WideFlat.cs` — flache Variante, breiter Stretch (~22×6)
- `PalmIsland15_OrganicCove.cs` — organische Variante mit Bucht & Sandbank


## Die 8 Prinzipien

### 1. Elliptische Silhouette mit variierenden Zeilenbreiten
Der entscheidende Punkt. Jede Zeile hat eine **andere Breite**, die einer
Ellipsenkurve folgt: oben schmal → in der Mitte am breitesten → unten wieder
schmal. Dadurch entsteht eine geschlossene, gerundete Form statt eines
Streifenstapels.

Beispiel (Sand-Silhouette aus PalmIsland11, 18×8):

```
row 13: width  6   <- oben (schmal)
row 14: width 10
row 15: width 14
row 16: width 18   <- am breitesten
row 17: width 18
row 18: width 16
row 19: width 12
row 20: width  6   <- unten (schmal)
```

**Warum das wirkt:** Die variierende Breite liest sich als perspektivische
Verkürzung — als würde man auf eine flache Kreis-/Ovalform leicht von oben
schauen (Kameraneigung ~15–25°).


### 2. Outline-Pixel am Silhouettenrand
Genau **ein** Pixel in der dunkelsten Materialfarbe (z.B. `cSandWet`) am äußeren
Rand jeder Zeile. Das Outline-Pixel rahmt die Form klar ein und trennt sie
sauber vom Wasser. Ohne Outline franst die Insel ins Wasser aus.

Konvention im Sprite-String: `o` = Outline-Pixel.


### 3. Innere kleinere Ellipse als zweite Materialebene
Gras (oder eine andere Decklage) wird als **kleinere** Ellipse **oben auf** die
Sandellipse gesetzt. Beide folgen demselben elliptischen Prinzip, die innere ist
nur deutlich kleiner und sitzt nicht zentriert, sondern leicht "nach hinten"
versetzt — so bleibt vorne ein sichtbarer Sandstreifen sichtbar.

Beispiel (Gras aus PalmIsland11, 10×4):

```
row 10: width  4
row 11: width  8
row 12: width 10   <- am breitesten
row 13: width  8
```


### 4. Goldener Übergangsrand zwischen Materialien
Eine dünne Linie in `cGoldRim` (warmes Ockergelb) genau an der Naht zwischen
Gras und Sand. Visuelle Funktion: trennt die zwei Materialien sauber und
suggeriert "Sonnenlicht trifft auf die Kante".

```csharp
foreach (var rx in new[] { 8, 9, 10, 11, 12, 13, 14, 15 })
    put(rx, 14, cGoldRim);
```


### 5. DKC-Style 4–5-Shade-Gradient pro Material, formfolgend
Jedes Material hat **4 oder 5 Helligkeitsstufen**, niemals nur eine Farbe.
Anordnung folgt einer impliziten Lichtquelle (oben-links):

- **Highlight** (`H`) — oben/links, wo das Licht aufschlägt
- **Light** (`s`) — größerer heller Bereich
- **Mid** (`m`/`M`) — Übergang zur Schattenseite
- **Dark** (`d`) — Schattenseite (unten/rechts)
- **Outline/Wet** (`o`) — Rand & Waterline

Konvention im Sprite-String: jeweils ein Buchstabe pro Shade. Großbuchstaben
(`H`, `M`) markieren Stellen, an denen die Form sich krümmt — so folgt das
Shading der **3D-Form**, nicht nur einer 2D-Karte.


### 6. Painter's Algorithm — Layer-Reihenfolge ist alles
Strikte Zeichenreihenfolge von hinten nach vorn:

1. Sky + Water Gradient (Hintergrund)
2. Insel-Silhouette (Sand)
3. Decklage (Gras)
4. Goldrim
5. Vegetation/Objekte (Palmenstamm, dann Krone)
6. Cast Shadows
7. Foam Ring (vorderste Ebene am Wasser)

Wenn man Krone vor Stamm zeichnet oder Foam vor Sand, geht die Tiefenwirkung
sofort kaputt.


### 7. Cylinder-Shading auf zylindrischen Objekten
Stämme, Säulen, Türme: **3 Pixel breit**, jede Spalte eine andere Helligkeit:

```csharp
put(trunkCol - 1, sy, cTrunkHi);    // links: Highlight
put(trunkCol,     sy, cTrunkBase);  // Mitte: Base
put(trunkCol + 1, sy, cTrunkMid);   // rechts: Schatten
```

Plus optional ein einzelnes `cTrunkDark`-Pixel rechts an der Basis als
Ambient-Occlusion-Akzent.


### 8. Foam Ring an der Waterline
Ein bis zwei Zeilen unter der Insel: vereinzelte weiße Foam-Pixel in
unregelmäßigen Abständen. Nicht durchgängig — nur Tupfer. Funktion: zeigt, dass
die Insel **im** Wasser steht (nicht darüber schwebt) und gibt der Szene
Bewegung.

```csharp
foreach (var fx in new[] { 6, 9, 12, 15, 18 })
    put(fx, 21, cWaterFoam);
```


## Variationsachsen (was darf sich ändern, ohne das Prinzip zu brechen)

| Achse | Beispiel | Datei |
|---|---|---|
| Seitenverhältnis | klassisch ~18×8 | PalmIsland11 |
| Seitenverhältnis | flach ~22×6 (Sandbank) | PalmIsland12 |
| Symmetrie | regelmäßige Ellipse | PalmIsland11 |
| Symmetrie | organisch mit Bucht + Sandbank | PalmIsland15 |
| Material-Mix | Sand + Gras + Palme | alle drei |
| Material-Mix | nur Sand, nur Fels, Schnee/Eis, Lava, … | (zukünftig) |
| Decklage | Gras | bisher |
| Decklage | Schnee, Moos, Korallen, Pflaster, Lava-Kruste, … | (zukünftig) |


## Was **kein** eigenes Prinzip ist

- **Sunset/Tageszeiten-Palette**: das ist nur ein Farbtausch, kein
  Form-/Strukturprinzip. Sunset-Variante (#16) wurde verworfen, weil sie
  nichts am eigentlichen Prinzip ändert.
- **Höhere/dichtere Vegetation**: ebenfalls nur Sprite-Detail, kein eigenes
  Prinzip. TwoTier (#14) und TallCompact (#13) wurden verworfen, weil sie
  ohne neuen strukturellen Mehrwert nur Variation auf der Palmen-Achse waren.


## Sprite-Konvention (für künftige Insel-Stile)

Damit Inseln vergleichbar bleiben, gilt:

- Canvas: `24×24`
- Horizont: typisch `y = 8`
- Insel zentriert um `y ≈ 17`
- Sand-Silhouette typisch zwischen `y = 13` und `y = 20`
- Sprite-Strings: ein Zeichen pro Pixel, `' '` = transparent
- Buchstaben-Konvention pro Material:
  - `H` Highlight, `s` Light, `m`/`M` Mid, `d` Dark, `o` Outline (für Sand)
  - eigene Buchstaben für Decklage, gerne unique pro Material

Beim Bauen einer neuen Insel: **erst die Silhouette als Zeilenbreiten-Liste
notieren**, dann Outline-Pixel setzen, dann von hinten nach vorn füllen. Nicht
mit der Vegetation anfangen.
