<div align="center">

# Yaman PC Cleaner

### Straightforward, transparent Windows maintenance — under your control.

Monitor memory and disk usage, review cleanup opportunities, and access essential Windows maintenance tools from one desktop app.

[![GitHub stars](https://img.shields.io/github/stars/Myozan/YamanPcCleaner?style=for-the-badge&logo=github&label=Stars)](https://github.com/Myozan/YamanPcCleaner/stargazers)
[![Latest release](https://img.shields.io/github/v/release/Myozan/YamanPcCleaner?style=for-the-badge&logo=github&label=Release)](https://github.com/Myozan/YamanPcCleaner/releases/latest)
[![License](https://img.shields.io/github/license/Myozan/YamanPcCleaner?style=for-the-badge)](../LICENSE)
[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?style=for-the-badge&logo=windows)](https://github.com/Myozan/YamanPcCleaner)

[🌐 Website](https://myozan.github.io/YamanPcCleaner/?lang=en) · [⬇️ Latest release](https://github.com/Myozan/YamanPcCleaner/releases/latest) · [🧾 Changelog](CHANGELOG.md) · [🇹🇷 Türkçe](README.md)

</div>

<p align="center">
  <img src="kaynaklar/onizleme-10.png" alt="Yaman PC Cleaner desktop application" width="900">
</p>

## What it does

Yaman PC Cleaner is a free, open-source Windows desktop application that brings system monitoring and maintenance options together. It aims to explain what is happening and keep important actions under the user's control.

**Core principle:** Show first. Explain the action. Let the user decide.

## Features

- **System overview:** System score, virtual memory, disk space and health, battery and startup information.
- **Performance monitoring:** CPU, GPU, RAM, VRAM and temperature graphs, plus plain-language slowdown analysis.
- **Disk cleanup:** Review temporary files, caches and Windows leftovers before cleanup.
- **Memory tools:** RAM monitoring, tray memory guardian and virtual-memory settings.
- **Windows maintenance:** Reversible settings, DNS selection and network diagnostics.
- **Gaming profile:** Power-mode and gaming-focused configuration options.
- **Driver center:** NVIDIA update checks, signature verification and restore-point workflow.
- **Maintenance reports:** Local activity history and HTML reports.

<p align="center">
  <img src="kaynaklar/onizleme-59.png" alt="System tools" width="49%">
  <img src="kaynaklar/onizleme-255.png" alt="Maintenance tools" width="49%">
</p>

## Download

Get the latest `YamanPcCleaner.exe` from [GitHub Releases](https://github.com/Myozan/YamanPcCleaner/releases/latest). No installer wizard is required. Windows 10 or 11 is required.

> **SmartScreen note:** The application is not currently signed with a paid Windows Authenticode code-signing certificate, so Windows may show an unknown-publisher warning. Review the source and release assets before running it. The application's update-signature check is a separate mechanism.

## Build from source

Build on Windows using the .NET Framework 4.x C# compiler:

```bat
derle.cmd
```

The executable is created in the repository root. The build script automatically includes C# files under `src\`.

## Privacy and safety

- Source code is available for inspection.
- Important maintenance operations are user-initiated.
- Supported system settings are backed up before changes.
- The application is not designed to collect personal telemetry.
- The in-app update mechanism verifies update signatures before installation; this is distinct from Authenticode signing.

For vulnerability reports, follow [SECURITY.md](.github/SECURITY.md).

## Contributing

Code, testing, documentation, translations and bug reports are welcome. Read [CONTRIBUTING.md](.github/CONTRIBUTING.md) and our [Code of Conduct](.github/CODE_OF_CONDUCT.md).

## Project links

- [Website](https://myozan.github.io/YamanPcCleaner/?lang=en)
- [Releases](https://github.com/Myozan/YamanPcCleaner/releases)
- [Changelog](CHANGELOG.md)
- [Issues and feature requests](https://github.com/Myozan/YamanPcCleaner/issues)
- [Actions / CI](https://github.com/Myozan/YamanPcCleaner/actions)

## License

MIT License. See [LICENSE](LICENSE).

<div align="center">

**If Yaman PC Cleaner is useful to you, consider giving the repository a ⭐.**

[![Star on GitHub](https://img.shields.io/badge/⭐%20Star%20on%20GitHub-181717?style=for-the-badge&logo=github)](https://github.com/Myozan/YamanPcCleaner/stargazers)

**Yaman Software** · Technology · Innovation · Software

</div>
