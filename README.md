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
- **Terminal:** Run `./start.sh` (macOS/Linux) or `start.cmd` (Windows with Git Bash/WSL).

**First time?** Don't worry! The start script automatically checks if you have everything installed (.NET SDK 10, VS Code extensions) and provides clear instructions if anything is missing.

**Examples:** Check out the example apps in `apps/demos/` and the clock faces in `apps/clockFaces/`

---

## Table of Contents

1. [Order Your PXL Clock](#order-your-pxl-clock)
2. [Get In Touch](#get-in-touch)
3. [About PXL Clock](#about-pxl-clock)
4. [Releases](#releases)
5. [Filing Issues and Ideas](#filing-issues-and-ideas)
6. [Developing Your Own Apps](#developing-your-own-apps)
7. [Contributing](#contributing)
8. [License](LICENSE.md)

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


### Troubleshooting

Run `./build/setup-check.sh` to verify your setup (requires [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)).

---

## Contributing

Contributions from the community are highly encouraged. If you want to help make PXL Clock better, you can:

1. **Create an Issue:** File a new issue for suggestions, bug reports, or feature requests.
2. **Submit a Pull Request:** Fork this repo, make your changes, and submit a pull request. Make sure to include a clear description of what you’ve changed or fixed.

Before contributing, please review our [**Code of Conduct**](CODE_OF_CONDUCT.md) (if available) to ensure a positive experience for everyone.

---

see: [LICENSE.md](LICENSE.md)

---

Thank you for your interest in the PXL Clock! We look forward to seeing your ideas and contributions. If you have any questions or suggestions, feel free to open an issue or start a discussion. Let’s make time more fun—together!
