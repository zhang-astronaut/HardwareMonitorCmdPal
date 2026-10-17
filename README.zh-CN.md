# HardwareMonitorExtension — PowerToys 命令面板扩展

[English](README.md) · [繁體中文](README.zh-TW.md) · [日本語](README.ja.md) · 简体中文

扩展跟随 Windows 显示语言，支持英文、繁体中文、日文及简体中文；其他语言回退为英文。更改 Windows 显示语言后，请重启／重新加载扩展并重新打开看板。从扩展打开的看板使用同一语言，直接打开 HTML 文件时使用浏览器语言偏好。硬件型号与技术标识符保留原文。国际化架构与检查见 [localization.md](docs/localization.md)。

通过 **PawnIO / LibreHardwareMonitor** 读取 CPU、GPU、磁盘温度与占用，支持：

- 命令面板顶级命令 + **Dock 快捷栏温度带**（`GetDockBands`）
- Compact / Default 双密度布局（Title 自包含温度，Dock Compact 隐藏 Subtitle 仍可读）
- 定期刷新 + 高温着色标记（`⚠` / `▲` / `🔥`）
- 温度历史：Markdown 迷你折线（180 点内存环缓冲）

文档依据：

- [扩展工作原理](https://learn.microsoft.com/zh-cn/windows/powertoys/command-palette/extensibility-overview)
- [扩展开发](https://learn.microsoft.com/zh-cn/windows/powertoys/command-palette/extension-development)
- [创建扩展](https://learn.microsoft.com/zh-cn/windows/powertoys/command-palette/creating-an-extension)
- [添加命令](https://learn.microsoft.com/zh-cn/windows/powertoys/command-palette/adding-commands)
- [发布扩展](https://learn.microsoft.com/zh-cn/windows/powertoys/command-palette/publish-extension)

## 本机依赖

| 组件 | 路径/说明 |
| --- | --- |
| PawnIO | `C:\Program Files\PawnIO`（`PawnIOLib.dll`） |
| PowerToys | 需启用 Command Palette（Win+Alt+Space） |
| .NET SDK | 与官方模板一致：`net10.0-windows10.0.26100.0` |
| VS | WinUI / Windows App SDK 工作负载，开发者模式开启 |

## 工程结构（对齐官方模板）

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
   ├─ Assets/                       # StoreLogo 等
   ├─ Services/
   │  ├─ PawnIoLocator.cs           # 注册表定位 + P/Invoke
   │  ├─ SensorModels.cs
   │  └─ SensorService.cs           # LHM + PawnIO 探测 + ACPI 兜底
   ├─ Pages/
   │  ├─ SensorsListPage.cs         # ListPage + RaiseItemsChanged
   │  └─ SensorHistoryPage.cs       # ContentPage + MarkdownContent
   ├─ Dock/
   │  └─ TemperatureDockBand.cs     # GetDockBands → 快捷栏
   └─ Properties/PublishProfiles/
```

CLSID（三处一致）：`3F8A2C91-6B4E-4D2A-9C17-8E5F0A7B4D63`

## 安装（用户）

下载 Release 里的 `HardwareMonitorExtension-Setup-<版本>-x64.exe`，以管理员运行即可：

1. 安装器自动把签名证书导入本机 `TrustedPeople`
2. 自动 `Add-AppxPackage` 注册 MSIX 扩展（**无需开发者模式**）
3. 命令面板输入 `Reload` → 选择 **Reload Command Palette Extension**
4. 打开「硬件监视器」；在 Dock 编辑或右键 → 将 **硬件温度** 固定到快捷栏

卸载：控制面板卸载本程序时，会同步移除 MSIX 包与证书。

## 本地部署（开发）

1. 用 Visual Studio 打开 `HardwareMonitorExtension.sln`
2. 配置选 **Debug | x64**
3. **生成 → 部署 HardwareMonitorExtension**（不要用 “Unpackaged” 运行）
4. 命令面板输入 `Reload` → 选择 **Reload Command Palette Extension**

命令行（需 .NET 10 SDK + Windows SDK 10.0.26100）：

```powershell
cd HardwareMonitorExtension
dotnet build -c Debug -p:Platform=x64
Add-AppxPackage -Register "bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\AppxManifest.xml"
# 布局注册（-Register）不需签名，但需要开发者模式；仅 build 不会注册包
```

## 构建安装器（维护者）

```powershell
.\build-exe.ps1            # x64；加 -Platforms @("x64","arm64") 构建多架构
```

脚本依次完成：确保自签名证书（`cert/HardwareMonitor.pfx`，Subject 必须与清单 Publisher `CN=HardwareMonitor` 一致，`*.pfx` 已 gitignore，勿提交仓库）→ 构建签名 MSIX → Inno Setup 编译安装器（输出到 `bin/Release/installer`）。安装器内嵌 msix + 证书公钥，装完自动注册。

注意：MSIX 只认「包身份」里的 `Publisher` 与签名证书 Subject 完全匹配；换证书须同步改 `Package.appxmanifest`。

## 数据路径

```
PawnIOLib (已安装)
   └─ SensorHub 定时采样
        ├─ SensorsListPage     RaiseItemsChanged
        ├─ SensorHistoryPage   Markdown 火花线
        └─ TemperatureDockBand Title 更新 (PropChanged)
```

阈值默认：偏热 70°C / 过热 80°C / 危险 90°C，可在扩展设置页修改。

## 发布

见 [PUBLISH.md](../PUBLISH.md)：Inno Setup EXE + WinGet（`windows-commandpalette-extension` 标签）+ 扩展库条目。
