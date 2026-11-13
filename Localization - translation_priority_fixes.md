# Localization Priority & MakePRI Resource Fix Plan

## Overview
After migrating localization strings from C# provider classes to external JSON files, packaged MSIX builds displayed Chinese (`zh-CN`) short labels (e.g., `CPU 封装`, `GPU 热点`) in Command Palette on an `en-US` Windows system. This document captures the root cause and the implemented fix.

---

## Root Cause
1. **MSIX MakePRI Language Qualifier Collision**:
   - The translation files were originally placed in BCP-47 locale-named subfolders:
     - `HardwareMonitorExtension/Localization/en-US/en-US.json`
     - `HardwareMonitorExtension/Localization/ja-JP/ja-JP.json`
     - `HardwareMonitorExtension/Localization/zh-CN/zh-CN.json`
   - Because `Package.appxmanifest` uses `<Resource Language="x-generate"/>`, Windows MakePRI interpreted `en-US/`, `ja-JP/`, and `zh-CN/` as **MRT language qualifier folders** rather than ordinary subdirectories.
   - During build, this emitted three `PRI263` warnings:
     `No default or neutral resource given for 'Files/Localization/<locale>.json'`
   - Inside the installed `WindowsApps` package, `Loc.InitializeProviders()` could not resolve `Localization/en-US/en-US.json` reliably from disk and fell back to `zh-CN`.

---

## Implemented Changes

### 1. Move JSON Files into a Non-Locale Subfolder (`Localization/lang/`)
Moved all translation JSON files out of BCP-47 named folders into `HardwareMonitorExtension/Localization/lang/`:
- `HardwareMonitorExtension/Localization/en-US/en-US.json` → `HardwareMonitorExtension/Localization/lang/en-US.json`
- `HardwareMonitorExtension/Localization/ja-JP/ja-JP.json` → `HardwareMonitorExtension/Localization/lang/ja-JP.json`
- `HardwareMonitorExtension/Localization/zh-CN/zh-CN.json` → `HardwareMonitorExtension/Localization/lang/zh-CN.json`
- Removed the empty `en-US/`, `ja-JP/`, and `zh-CN/` directories.

### 2. Update `Loc.cs` Discovery Logic
Updated `Loc.InitializeProviders()` in `HardwareMonitorExtension/Localization/Loc.cs`:
- Target `langDir = Path.Combine(baseDir, "lang")` (falling back to `baseDir` if `lang/` is absent).
- Load `en-US.json` first from `langDir` to establish `defaultEn` as the baseline fallback provider.
- Scan `Directory.GetFiles(langDir, "*.json")` and register each provider by both its JSON `LocaleCode` and filename stem (`Path.GetFileNameWithoutExtension(targetFile)`).

### 3. Project Packaging (`HardwareMonitorExtension.csproj`)
- No `.csproj` change was required because `<Content Include="Localization\**\*.json">` already copies all nested JSON files under `Localization/` into the output and MSIX `PackageLayout`.

---

## Verification
- **JSON Validation**: Verified `en-US.json`, `ja-JP.json`, and `zh-CN.json` parse cleanly.
- **Solution Build**: Ran `dotnet build HardwareMonitorExtension.sln`:
  - **0 Errors**
  - **76 Warnings** (down from 79 — all 3 `PRI263` MakePRI warnings for `Localization/*.json` were eliminated).
- **Runtime**: After cleaning old MSIX artifacts and reinstalling, Command Palette displays `en-US` labels (`CPU Tctl`, `CPU`, etc.) as expected.
