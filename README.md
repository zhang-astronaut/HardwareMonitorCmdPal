# HardwareMonitorExtension — PowerToys 命令面板扩展

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

## 本地部署（开发）

1. 用 Visual Studio 打开 `HardwareMonitorExtension.sln`
2. 配置选 **Debug | x64**
3. **生成 → 部署 HardwareMonitorExtension**（不要用 “Unpackaged” 运行）
4. 命令面板输入 `Reload` → 选择 **Reload Command Palette Extension**
5. 打开「硬件监视器」；在 Dock 编辑或右键 → 将 **硬件温度** 固定到快捷栏

命令行（需已装 .NET 10 SDK）：

```powershell
cd HardwareMonitorExtension
dotnet build -c Debug -p:Platform=x64
# VS 部署 MSIX 才会被 CmdPal 发现；仅 build 不会注册包
```

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
