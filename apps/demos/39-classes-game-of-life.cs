// ---
// app: Demo39ClassesGameOfLife
// displayName: Game of Life (Classes)
// author: Cumin & Potato
// ---
// Example 39: Using classes — Conway's Game of Life with a Grid helper class

#:package Pxl@0.0.61

using Pxl.Ui.CSharp;

var grid = new Grid(24, 24);
grid.Randomize(new Random(123), density: 0.3);
double lastStep = -1;

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);

    // Step every 200ms
    var step = Math.Floor(ctx.Elapsed.TotalMilliseconds / 200);
    if (step > lastStep)
    {
        grid.Step();
        lastStep = step;
    }

    for (var y = 0; y < 24; y++)
    for (var x = 0; x < 24; x++)
    {
        if (grid.IsAlive(x, y))
            ctx.DrawPoint(x, y, Colors.Lime);
    }
};

class Grid(int width, int height)
{
    private bool[,] cells = new bool[width, height];

    public bool IsAlive(int x, int y) => cells[x, y];

    public void Randomize(Random rng, double density)
    {
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            cells[x, y] = rng.NextDouble() < density;
    }

    public void Step()
    {
        var next = new bool[width, height];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var n = CountNeighbors(x, y);
            next[x, y] = cells[x, y] ? (n == 2 || n == 3) : (n == 3);
        }
        cells = next;
    }

    private int CountNeighbors(int cx, int cy)
    {
        var count = 0;
        for (var dy = -1; dy <= 1; dy++)
        for (var dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0) continue;
            var x = (cx + dx + width) % width;
            var y = (cy + dy + height) % height;
            if (cells[x, y]) count++;
        }
        return count;
    }
}
