# PXL Clock

**Support my Work**

Buy a **PXL Clock** and help me create more videos like this!
Use code **RONALD** for a **25€ discount**:

[https://www.pxlclock.com/?ref=RONALD](https://www.pxlclock.com/?ref=RONALD)

PXL Clock is a fun device, made with ❤️ - and it's programmable in an easy and quick way.

<p align="center">
  <a href="https://www.pxlclock.com/?ref=RONALD">
    <img width="842" height="832" alt="468354531-9b92c9d7-b20b-4316-8104-ac980fa449d5" src="https://github.com/user-attachments/assets/0a5a495d-731b-4f65-ac8f-3719f9b9010a" />
  </a>
</p>

Find out more:

- On the [PXL Clock Discord Server](https://discord.gg/KDbVdKQh5j)
- check out the [PXL Clock Repo on GitHub](https://github.com/CuminAndPotato/PXL-Clock)
- Visit the official [PXL Clock Store](https://www.pxlclock.com/?ref=RONALD)

<p align="center">
  <h3>Join the PXL Clock Community on Discord</h3>
  <a href="https://discord.gg/KDbVdKQh5j">
    <img src="https://img.shields.io/badge/Discord-Join%20Server-blue?style=flat-square&logo=discord" alt="Join Our Discord">
  </a>
</p>

---


Welcome to the **PXL Clock** repository! This repo serves as a central hub for:

- **Resources for creating your own custom PXL Clock applications**
- **Issue tracking** and **idea proposals** (hardware, software, use cases, features)

We’re excited to see what the community will build around the PXL Clock. Below you’ll find everything you need to get started.

---

## Quick-Start Development of PXL Clock Apps

**Getting Started (3 steps):**
1. Start the simulator (see below - either by VS Code or terminal)
2. Open the simulator at `http://localhost:5001`
3. Edit any `.cs` file in the `apps/` directory and save to see changes

**Starting the development environment:**

- **VS Code (easiest):** Press `Cmd+Shift+B` (macOS) or `Ctrl+Shift+B` (Windows/Linux) to run the preconfigured build task **PXL-CLOCK :: Start**.
- **Terminal:** Run `./start.sh` (macOS/Linux/WSL). On Windows, use Git Bash or WSL.

**First time?** Don't worry! The start script automatically checks if you have everything installed (.NET SDK 10, VS Code extensions) and provides clear instructions if anything is missing.

**Examples:** Start with `apps/demos/01-hello-pixel.cs` and work your way up through the numbered demos. Also check out the clock faces in `apps/clockFaces/` for more advanced examples.

> **Windows users:** You need [Git for Windows](https://git-scm.com/download/win) (includes Git Bash) or [WSL](https://learn.microsoft.com/windows/wsl/install) to run the shell scripts.

---

## Table of Contents

1. [About PXL Clock](#about-pxl-clock)
2. [Releases](#releases)
3. [Filing Issues and Ideas](#filing-issues-and-ideas)
4. [Developing Your Own Apps](#developing-your-own-apps)
5. [Contributing](#contributing)
6. [License](LICENSE.md)

---

## About PXL Clock

The **PXL Clock** is a device designed to display various fun clocks, animations, short stories, visuals and other creative things - all on a 24x24 pixel display. Whether you want to keep track of the current time in a futuristic manner or develop your own mini-apps to run on the clock, this project provides a flexible platform for creativity.

---

## Releases

You’ll find our official firmware and software packages under the [**Releases**](../../releases) section. The PXL Clock updates itself over-the-air, so no manual steps required.

---

## Filing Issues and Ideas

Have an idea for a new feature or discovered a bug? Help us improve the PXL Clock by creating a new issue in this repository. We welcome:

- Hardware-related feedback or design modifications
- Software feature requests, improvements, or bug reports
- Use case suggestions or creative ways to integrate PXL Clock into your projects

Just head over to the [**Issues**](../../issues) tab and click **New Issue** to get started.

---

## Developing Your Own Apps

[![NuGet](https://img.shields.io/nuget/v/Pxl.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/Pxl)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Pxl.svg?style=flat-square)](https://www.nuget.org/packages/Pxl)

The example apps always use the latest compatible versions of the Pxl NuGet package and tools. Running `./start.sh` automatically restores the correct tool versions.

### Getting Started with Code

A PXL Clock app is a simple C# script. Here's a minimal example — a bouncing ball:

```csharp
#:package Pxl@0.0.46

using Pxl.Ui.CSharp;

double x = 0;

var scene = PxlApp.CreateScene(ctx =>
{
    ctx.DrawBackground(Colors.Black);
    ctx.DrawCircle(x, 12, 3, colorFill: Colors.Red);
    x += 0.2;
    if (x > ctx.Width + 3) x = -3;
});

await PxlApp.SimulateAndSendToDevice(scene);
```

Or display the current time with a blinking colon:

```csharp
#:package Pxl@0.0.46

using Pxl.Ui.CSharp;

var scene = PxlApp.CreateScene(ctx =>
{
    ctx.DrawBackground(Colors.DarkBlue);
    ctx.DrawTextMono4x5(ctx.Now.ToString("HH:mm"), 0, 2, Colors.White);
    ctx.DrawTextMono4x5(ctx.Now.Second.ToString("00"), 8, 10, Colors.Yellow);
    if (ctx.Now.Millisecond < 500)
    {
        ctx.DrawPoint(12, 18, Colors.White, strokeWidth: 2);
        ctx.DrawPoint(12, 21, Colors.White, strokeWidth: 2);
    }
});

await PxlApp.SimulateAndSendToDevice(scene);
```

Find many more examples in `apps/demos/` (numbered tutorials from basics to advanced) and in `apps/clockFaces/` (the factory clock face apps that ship with every PXL Clock).

### Using Images and Assets

You can use images in your apps by placing them in an `assets/` folder next to your script. Supported formats: **PNG**, **GIF** (animated), and **JPEG**.

```csharp
// Load a static image
var logo = Image.LoadSingleImage("assets/logo.png");
ctx.DrawImage(logo.Resize(24, 24), 0, 0);

// Load an animated GIF
var animation = Image.LoadAnimatedGif("assets/mario.gif");
ctx.DrawImage(animation.Resize(24, 24), 0, 0, repeat: true);

// Load a sprite sheet and create animations from it
var sprites = Image.LoadSingleImage("assets/pacman_sprite.png")
    .Crop(left: 456, top: 0, right: 0, bottom: 0)
    .ToSpriteMap(cellWidth: 16, cellHeight: 16, frameDurationMs: 80);
var pacman = sprites.CreateAnimation((0, 0), (0, 1), (0, 2), (0, 1));
ctx.DrawImage(pacman, x, y);
```

**Important:** Asset paths must be **string literals** (not variables). The compiler embeds the assets into your app at compile time. Images are automatically scaled — use `.Resize(width, height)` to fit the 24x24 display.

See `apps/demos/28-image-static.cs`, `29-image-animated-gif.cs`, and `30-pacman-sprites.cs` for complete examples.

### Send to Your PXL Clock

To send your app to a real PXL Clock:

1. In the **PXL-App**, go to the **Settings** of your clock and set the mode to **"Development"**. The display will turn black, indicating it's ready to receive from your computer.

2. In your app script, choose one of the following methods and provide the **IP address or name** of your clock:

   ```csharp
   // Only send to the clock (no local simulator)
   await PxlApp.SendToDevice(scene, "192.168.1.42");

   // Run in the simulator AND send to the clock simultaneously
   await PxlApp.SimulateAndSendToDeviceAndSendToDevice(scene, "192.168.1.42");

   // Only run in the local simulator (default, no clock needed)
   await PxlApp.SimulateAndSendToDevice(scene);
   ```

3. Start the development environment as usual (see [Quick-Start](#quick-start-development-of-pxl-clock-apps)).


### Publish Your App to the PXL Clock

Once you're happy with your app in the simulator, you can publish it to your PXL Clock so it runs standalone — without your computer connected.

1. Make sure your PXL Clock is in **"Development"** mode (see above).
2. Run the publish script:

   ```bash
   ./publish-app.sh
   ```

   Or use the VS Code task: **PXL-CLOCK :: Publish App to Device**.

This compiles your app and installs it on the clock. After publishing, you can switch the clock back to normal mode in the PXL-App — your app will appear in the app list.

### Troubleshooting

Run `./build/setup-check.sh` to verify your setup (requires [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)).

---

## Contributing

Contributions from the community are highly encouraged. If you want to help make PXL Clock better, you can:

1. **Create an Issue:** File a new issue for suggestions, bug reports, or feature requests.
2. **Submit a Pull Request:** Fork this repo, make your changes, and submit a pull request. Make sure to include a clear description of what you’ve changed or fixed.

Please be respectful and constructive. Join our [Discord community](https://discord.gg/KDbVdKQh5j) if you have questions or want to discuss ideas.

---

see: [LICENSE.md](LICENSE.md)

---

Thank you for your interest in the PXL Clock! We look forward to seeing your ideas and contributions. If you have any questions or suggestions, feel free to open an issue or start a discussion. Let’s make time more fun—together!
