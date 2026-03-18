#!/usr/bin/env node

/**
 * Reel Maker — Render script
 *
 * Usage:
 *   node render.mjs <config.json> [--format vertical|horizontal|both]
 *
 * The config.json defines the slides (text + animation) and output settings.
 * See example-config.json for the format.
 */

import { bundle } from "@remotion/bundler";
import { renderMedia, selectComposition } from "@remotion/renderer";
import path from "path";
import fs from "fs";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const args = process.argv.slice(2);
const configPath = args.find((a) => !a.startsWith("--"));
const formatArg =
  args.find((a) => a.startsWith("--format="))?.split("=")[1] ??
  (args.includes("--format")
    ? args[args.indexOf("--format") + 1]
    : "both");

if (!configPath) {
  console.error("Usage: node render.mjs <config.json> [--format vertical|horizontal|both]");
  process.exit(1);
}

const configRaw = fs.readFileSync(configPath, "utf-8");
const config = JSON.parse(configRaw);

// Resolve GIF paths relative to the config file's directory
const configDir = path.dirname(path.resolve(configPath));
const publicDir = path.join(__dirname, "public");

// Copy GIF files to public/ so Remotion can serve them as static files
const gifMap = new Map(); // original path -> static file name
for (const slide of config.slides) {
  if (slide.gifPath) {
    const absPath = path.isAbsolute(slide.gifPath)
      ? slide.gifPath
      : path.resolve(configDir, slide.gifPath);
    const fileName = path.basename(absPath);
    const destPath = path.join(publicDir, fileName);

    if (!fs.existsSync(destPath) || !fs.readFileSync(destPath).equals(fs.readFileSync(absPath))) {
      fs.copyFileSync(absPath, destPath);
    }
    gifMap.set(slide.gifPath, fileName);
    // Replace path with just the filename — the component will use staticFile()
    slide.gifPath = fileName;
  }
}

const fps = config.fps ?? 30;
const totalFrames = config.slides.reduce(
  (sum, s) => sum + Math.round(s.durationInSeconds * fps),
  0
);

const outputDir = config.outputDir
  ? path.resolve(configDir, config.outputDir)
  : configDir;

console.log(`Reel Maker`);
console.log(`  Config: ${configPath}`);
console.log(`  Slides: ${config.slides.length}`);
console.log(`  Duration: ${(totalFrames / fps).toFixed(1)}s (${totalFrames} frames @ ${fps}fps)`);
console.log(`  Format: ${formatArg}`);
console.log(`  Output: ${outputDir}`);
console.log(`  GIFs copied: ${gifMap.size}`);
console.log();

const entryPoint = path.join(__dirname, "src/index.ts");

async function renderFormat(compositionId, width, height, outputFile) {
  console.log(`Bundling for ${compositionId}...`);
  const bundleLocation = await bundle({
    entryPoint,
    webpackOverride: (config) => config,
    publicDir,
  });

  console.log(`Selecting composition ${compositionId}...`);
  const composition = await selectComposition({
    serveUrl: bundleLocation,
    id: compositionId,
    inputProps: { config },
  });

  // Override duration and dimensions
  composition.durationInFrames = totalFrames;
  composition.fps = fps;
  composition.width = width;
  composition.height = height;

  const outPath = path.join(outputDir, outputFile);
  console.log(`Rendering ${outPath}...`);

  await renderMedia({
    composition,
    serveUrl: bundleLocation,
    codec: "h264",
    outputLocation: outPath,
    inputProps: { config },
  });

  const stats = fs.statSync(outPath);
  console.log(`  Done: ${outPath} (${(stats.size / 1024).toFixed(0)} KB)`);
  return outPath;
}

async function main() {
  const formats =
    formatArg === "both"
      ? ["vertical", "horizontal"]
      : [formatArg];

  for (const fmt of formats) {
    if (fmt === "vertical") {
      const name = (config.clockfaceName ?? config.title ?? "reel").replace(/\s+/g, "");
      await renderFormat("ReelVertical", 1080, 1920, `${name}-vertical.mp4`);
    } else {
      const name = (config.clockfaceName ?? config.title ?? "reel").replace(/\s+/g, "");
      await renderFormat("ReelHorizontal", 1920, 1080, `${name}-horizontal.mp4`);
    }
  }

  // Clean up copied GIFs from public/
  for (const fileName of gifMap.values()) {
    const p = path.join(publicDir, fileName);
    if (fs.existsSync(p)) fs.unlinkSync(p);
  }

  console.log("\nAll done!");
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
