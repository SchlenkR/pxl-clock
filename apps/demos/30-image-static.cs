// ---
// app: Demo30ImageStatic
// displayName: Static Image
// author: Cumin & Potato
// ---
// Example 30: Static Image
// Load and draw a static PNG image

#:package Pxl@0.0.56

using Pxl.Ui.CSharp;

// Load a static image (PNG, JPEG, etc.)
var logo = Image.LoadSingleImage("assets/logo.png");

// Optional: resize to fit the display
var resized = logo.Resize(32, 24);

var scene = (DrawingContext ctx) =>
{
    ctx.DrawBackground(Colors.Black);
    ctx.DrawImage(resized, 0, 0);
};

