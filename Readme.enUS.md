# HardwareMonitorExtension — PowerToys Command Palette Extension

Read CPU, GPU, and disk temperatures and usage via **PawnIO / LibreHardwareMonitor**, supporting:

- Command Palette top-level commands + **Dock shortcut temperature band** (`GetDockBands`)
- Compact / Default dual-density layouts (Title self-contains temperature; readable even when Dock Compact hides Subtitle)
- Periodic refresh + heat-level color markers (`⚠` / `▲` / `🔥`)
- Temperature history: Markdown mini sparkline (180-point in-memory circular buffer)

Documentation references:

- [How extensions work](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/extensibility-overview)
- [Extension development](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/extension-development)
- [Creating an extension](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/creating-an-extension)
- [Adding commands](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/adding-commands)
- [Publishing an extension](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/publish-extension)

## Local Dependencies

| Component | Path / Description |
| --- | --- |
| PawnIO | `C:\Program Files\PawnIO` (`PawnIOLib.dll`) |
| PowerToys | Requires Command Palette enabled (`Win+Alt+Space`) |
| .NET SDK | Aligned with official template: `net10.0-windows10.0.26100.0` |
| Visual Studio | WinUI / Windows App SDK workload with Developer Mode enabled |

## Project Structure (Aligned with official template)

```
HardwareMonitorExtension/
├─ Directory.Build.props
├─ Directory.Packages.props
├─ nuget.config
├─ HardwareMonitorExtension.sln
└─ HardwareMonitorExtension/
   ├─ HardwareMonitorExtension.csproj
   ├─ app.manifest
   ├─ Package.appxmanifest          # com.microsoft.commandpalette + COM CLSID
   ├─ Program.cs                    # -RegisterProcessAsComServer
   ├─ HardwareMonitorExtension.cs   # IExtension + [Guid]
   ├─ HardwareMonitorCommandsProvider.cs
   ├─ SensorHub.cs
   ├─ HardwareMonitorSettingsManager.cs
   ├─ Assets/                       # StoreLogo, web dashboard, etc.
   ├─ Localization/                 # Localization subsystem (en-US, zh-Hans)
   ├─ Services/
   │  ├─ PawnIoLocator.cs           # Registry lookup + P/Invoke
   │  ├─ SensorModels.cs
   │  └─ SensorService.cs           # LHM + PawnIO probing + ACPI fallback
   ├─ Pages/
   │  ├─ SensorsListPage.cs         # ListPage + RaiseItemsChanged
   │  └─ SensorChartPage.cs         # ContentPage + MarkdownContent
   ├─ Dock/
   │  └─ PinnedSensorsDockBand.cs   # GetDockBands → Shortcut bar
   └─ Properties/PublishProfiles/
```

CLSID (consistent across all three locations): `3F8A2C91-6B4E-4D2A-9C17-8E5F0A7B4D63`

## Installation (User)

Download `HardwareMonitorExtension-Setup-<Version>-x64.exe` from Releases and run as Administrator:

1. The installer automatically imports the signing certificate into the local machine's `TrustedPeople` certificate store.
2. Automatically registers the MSIX extension via `Add-AppxPackage` (**Developer Mode not required**).
3. In Command Palette, type `Reload` → select **Reload Command Palette Extension**.
4. Open "Hardware Monitor"; in Dock edit mode or via right-click context menu → pin **Hardware Readings** to the shortcut bar.

Uninstall: When uninstalling this program from Windows Installed Apps / Control Panel, the MSIX package and certificate are automatically removed.

## Local Deployment (Development)

1. Open `HardwareMonitorExtension.sln` with Visual Studio.
2. Set configuration to **Debug | x64**.
3. **Build → Deploy HardwareMonitorExtension** (do not run as "Unpackaged").
4. In Command Palette, type `Reload` → select **Reload Command Palette Extension**.

Command line (.NET 10 SDK + Windows SDK 10.0.26100 required):

```powershell
cd HardwareMonitorExtension
dotnet build -c Debug -p:Platform=x64
Add-AppxPackage -Register "bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\AppxManifest.xml"
# Layout registration (-Register) does not require a signature, but requires Developer Mode; build alone will not register the package.
```

## Building Installer (Maintainers)

```powershell
.\build-exe.ps1            # x64; add -Platforms @("x64","arm64") for multi-architecture builds
```

The script executes sequentially: ensures self-signed certificate (`cert/HardwareMonitor.pfx`, Subject must match manifest Publisher `CN=HardwareMonitor`, `*.pfx` is gitignored and not committed) → builds signed MSIX → compiles Inno Setup installer (output to `bin/Release/installer`). The installer embeds the MSIX and certificate public key, automatically registering after installation.

Note: MSIX strictly matches `Publisher` in package identity with the signing certificate Subject; if the certificate is changed, `Package.appxmanifest` must be updated synchronously.

## Data Pipeline

```
PawnIOLib (Installed)
   └─ SensorHub (Periodic sampling)
        ├─ SensorsListPage       RaiseItemsChanged
        ├─ SensorChartPage       Markdown sparkline
        └─ PinnedSensorsDockBand Title update (PropChanged)
```

Default thresholds: Warm 70°C / Hot 80°C / Critical 90°C (configurable in the extension settings page).

## Publishing

See [PUBLISH.md](../PUBLISH.md): Inno Setup EXE + WinGet (`windows-commandpalette-extension` tag) + extension catalog entry.
