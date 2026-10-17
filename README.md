# HardwareMonitorCmdPal

English · [繁體中文](README.zh-TW.md) · [日本語](README.ja.md) · [简体中文](README.zh-CN.md)

A PowerToys Command Palette extension that reads CPU, GPU, storage, motherboard and other sensors through LibreHardwareMonitor/PawnIO.

- Browse all available readings and pin sensors to the Command Palette Dock.
- Switch between compact and standard layouts; optionally combine temperature and utilization in a Dock reading.
- View recent trends (up to 180 samples in memory), temperature warnings and an offline browser dashboard.

## Languages

The extension follows the Windows display language: **English, Traditional Chinese, Japanese and Simplified Chinese** are supported. Regional variants such as `zh-TW`, `zh-HK`, `ja-JP` and `zh-CN` use matching translations; other languages fall back to English. After changing Windows display language, restart/reload the extension and reopen its dashboard.

The dashboard opened from the extension uses the same UI language. When opened directly, it follows browser language preferences. Hardware/model names and technical identifiers retain their original spelling. MSIX and installer product branding remains English. See [localization architecture and checks](docs/localization.md).

## Install and use

1. Download the matching installer from [Releases](https://github.com/pingqLIN/HardwareMonitorCmdPal/releases) and run it as administrator. It registers the MSIX extension and signing certificate.
2. Enable Command Palette in PowerToys. Open it with **Win+Alt+Space**, run **Reload Command Palette Extension**, then open **Hardware Monitor**.
3. Use a reading's context menu to pin/unpin it in the Dock or view its trend. Add the **Hardware readings** band through Command Palette's Dock editor.
4. Open **Live dashboard** for the offline browser view. Without a live snapshot it shows labeled simulated data; its stress button changes the simulation.

CPU temperature availability depends on hardware, drivers and permissions. The existing **Install CPU temperature startup sampling (once)** command requests elevation to configure the startup helper. This is optional. CPU power may be labeled as an estimate when hardware does not provide it.

Settings control refresh interval (500–3000 ms), hot/critical temperature thresholds, default compact layout and combined Dock utilization. Defaults: warm 70°C, hot 80°C, critical 90°C. Pins and settings use the existing per-user directory. Uninstall through Windows installed apps.

## Build and verify

Use Windows, .NET 10 SDK, Windows SDK 10.0.26100 and Visual Studio with Windows/WinUI tooling. PawnIO is normally installed at `C:\Program Files\PawnIO`. PowerToys must have Command Palette enabled.

```powershell
dotnet build HardwareMonitorExtension.sln -c Debug -p:Platform=x64
dotnet run --project tests/LocalizationSmoke -p:Platform=x64
node tests/dashboard-localization.test.cjs
powershell -NoProfile -File scripts/Export-DashboardLocales.ps1 -Check
```

For local deployment, open the solution in Visual Studio and deploy the packaged `HardwareMonitorExtension` project. A build alone does not register it. Development layout registration requires Windows developer mode; the signed installer does not.

To build the existing signed MSIX/EXE installer (also requires Inno Setup 6):

```powershell
.\build-exe.ps1
.\build-exe.ps1 -Platforms @("x64", "arm64")
```

The packaging script manages a local signing certificate and creates installer artifacts. Keep private signing material outside Git; the manifest publisher must match the certificate subject.

`SensorHub` shares snapshots, history and pins between `Pages/`, `Dock/` and the offline dashboard in `Assets/`. `Services/` contains hardware collection and the existing temperature helper. `Localization/` contains shared managed/dashboard translation resources.

See [Microsoft's Command Palette documentation](https://learn.microsoft.com/windows/powertoys/command-palette/extensibility-overview) and [localization development/testing](docs/localization.md). Live hardware, elevation and actual Command Palette layout require manual acceptance.
