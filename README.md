# PXL Clock

Welcome to the **PXL Clock** repository! This repo serves as a central hub for:

- **Resources for creating custom PXL Clock applications**
- **Issue tracking** and **idea proposals** (hardware, software, use cases, features)

We’re excited to see what the community will build around the PXL Clock. Below you’ll find everything you need to get started.

<p align="center">
  <a href="https://www.pxlclock.com">
    <img width="842" height="832" alt="image" src="https://github.com/user-attachments/assets/9b92c9d7-b20b-4316-8104-ac980fa449d5" />
  </a>
  <!--<img width="640" alt="image" src="https://github.com/user-attachments/assets/4c898f7e-56ae-4a8b-be34-464ad83a5ffb" />-->
</p>

---

## Quick-Start Development of PXL Clock Apps

**Getting Started (3 steps):**
1. Run `./start.sh` in the terminal
2. Open the simulator at `http://localhost:5001`
3. Edit any `.cs` file in the `apps/` directory and save to see changes

> **Windows users:** Use Git Bash, WSL, or run `bash start.sh` in any terminal

**First time?** Don't worry! The start script automatically checks if you have everything installed (.NET SDK 10, VS Code extensions) and provides clear instructions if anything is missing.

**Examples:** Check out the example apps in `apps/demos/` and the clock faces in `apps/clockFaces/`

---

## Order Your PXL Clock!

Exciting news: ordering the PXL Clock will soon be possible! 🎉 You can find more information and updates on our official website: [pxlclock.com](https://pxlclock.com)

We’re currently working on the first 100 units, the MK1 edition! We’re in the certification and refining all the little details that make this a fine product. We’re fully committed to delivering something amazing, and we’ll keep you updated every step of the way.

Stay in the loop by following the #pxlclock hashtag on our channels for the latest news and progress updates.

Thank you for your patience and support! 💡

---

## Get In Touch

### Discord

Get in touch with us and others on our [**Discord Server**](https://discord.gg/KDbVdKQh5j)

<p align="center">
  <h3>Join the PXL Clock Community on Discord</h3>
  <a href="https://discord.gg/KDbVdKQh5j">
    <img src="https://img.shields.io/badge/Discord-Join%20Server-blue?style=flat-square&logo=discord" alt="Join Our Discord">
  </a>
</p>

### Bluesky

Follow the [**#pxlclock hashtag**](https://bsky.app/hashtag/PXLclock) on **Bluesky** for getting news and see what others do!

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

### Use Your PXL Clock for Development

1. In the PXL-App, go to the "Settings" tab of your connected PXL Clock, and "Turn Off Display".

2. In your app script, set the target device to your connected PXL Clock.

3. Start as usual (see above).


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
