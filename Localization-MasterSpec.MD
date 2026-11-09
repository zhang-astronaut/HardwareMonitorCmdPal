# Master Specification: Hardware Monitor Localization & Verification Framework

**Product**: Hardware Monitor for PowerToys Command Palette  
**Author**: zhang-astronaut  
**Contributor**: Nathan Myron, MyronTech Solutions  
**Namespace Scope**: Original designer namespace (`HardwareMonitorExtension` & `HardwareMonitorExtension.Localization`)  
**Scope**: Full-stack localization covering C# Extension, COM Host, Domain Models, and Web Dashboard (`en-US` & `zh-Hans`)  

---

## 1. Architectural Blueprint

```
┌────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                       C# Extension & COM Host Layer                                    │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  HardwareMonitorExtension.Localization/                                                                │
│    ├── ILocalizationProvider.cs      (Strongly typed contract for all strings and formatters)          │
│    ├── EnUsLocalizationProvider.cs   (English [US]: System Default & Fallback)                         │
│    ├── ZhHansLocalizationProvider.cs (Chinese Simplified [简体中文])                                   │
│    └── Loc.cs                        (Thread-safe Loc.Current accessor, CultureInfo auto-detection)    │
│                                                                                                        │
│  Domain Decoupling Layer (Logic Protection):                                                           │
│    • HardwareGroups Invariant Enum/Constants (Processor, Graphics, Storage, Motherboard, etc.)         │
│    • Preserves internal filters: Pinned defaults, synthetic CPU metrics, compact initial prefixes      │
│    • Localized Projections: s.LocalizedGroup, s.LocalizedName, s.FormatListTitle(), etc.               │
└───────────────────────────────────────────────────┬────────────────────────────────────────────────────┘
                                                    │ Ingests live-data.js (JSON telemetry)
                                                    ▼
┌────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                   Offline Web Dashboard Layer (btop-style)                             │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│  Assets/index.html, Assets/styles.css, Assets/app.js:                                                  │
│    • Extensible Locale Catalog (LOCALES registry for en-US, zh-Hans, and future additions)             │
│    • Auto-Detect from navigator.languages with LocalStorage override persistence                       │
│    • Dynamic Perspective Dropdown:                                                                     │
│        - Left: Autonym (native language name)                                                          │
│        - Right: Perspective name dynamically rendered in the currently hovered language                │
│    • Localized DOM elements, KPI cards, interactive charts, and live data table                        │
└────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Phase 1: C# Core Localization Framework

### File Architecture
Create directory `HardwareMonitorExtension/Localization/`:
1. **`ILocalizationProvider.cs`**: Interface defining strongly typed properties for every UI string and parameterized formatter methods with compile-time checked signatures.
2. **`EnUsLocalizationProvider.cs`**: Primary English (US) dictionary and invariant fallback.
3. **`ZhHansLocalizationProvider.cs`**: Simplified Chinese dictionary mirroring existing application terminology.
4. **`Loc.cs`**:
   - Holds static `ILocalizationProvider Current`.
   - Auto-detects culture via `CultureInfo.CurrentUICulture.Name` (if starts with `zh` → `ZhHans`; otherwise defaults to `EnUs`).
   - Supports explicit manual override via `Loc.SetCulture("en-US" | "zh-CN" | "auto")`.

---

## 3. Phase 2: Domain Decoupling & Component Integration

### Domain & Services Decoupling
To ensure existing business logic never breaks when strings are translated:
1. **`SensorModels.cs`**:
   - Replace literal Chinese group strings with invariant `HardwareGroups` constants (`"Processor"`, `"Graphics"`, `"Storage"`, `"Motherboard"`, `"Memory"`, `"Battery"`, `"Network"`, `"Other"`).
   - Retain compact dock initials mapping to invariant keys (`Processor` → `"C"`, `Graphics` → `"G"`, `Storage` → `"S"`, `Motherboard` → `"M"`, `Memory` → `"R"`, `Battery` → `"B"`).
   - Expose `sensor.LocalizedGroup` via `Loc.Current`.
2. **`SensorService.cs`**:
   - Map LHM hardware types directly to invariant `HardwareGroups`.
   - Update fallback CPU calculations and synthetic sensor naming to use `Loc.Current`.
   - Update `BackendLabel` and sensor read failure statuses to use `Loc.Current`.
3. **`PinnedSensorStore.cs`**:
   - Query default pins using invariant keys (`HardwareGroups.Processor`, `HardwareGroups.Graphics`).

### Complete C# String Catalog

| Section / Component | Key / Method | English (`en-US`) | Simplified Chinese (`zh-Hans`) |
| :--- | :--- | :--- | :--- |
| **Commands Provider** | `ExtensionDisplayName` | Hardware Monitor | 硬件监视器 |
| (`HardwareMonitorCommandsProvider.cs`) | `AllSensorsSubtitle` | All sensors · Can be pinned to dock | 全部传感器 · 可固定到快捷栏 |
| | `OpenDashboardTitle` | Open Real-Time Dashboard | 打开实时图表看板 |
| | `DashboardSubtitle` | btop-style offline index.html | btop 风格离线 index.html |
| | `InstallHelperTitle` | Install CPU Temp Startup Sampling (One-time) | 安装 CPU 温度开机采样（一次） |
| | `InstallHelperSubtitle` | Runs at startup automatically; normal user CPU Tctl is 0 | 注册后开机自动，无需再点；普通权限 CPU Tctl 为 0 |
| **Context Commands** | `ToggleCompactName` | Toggle Compact Layout | 切换紧凑布局 |
| (`ListCommands.cs`) | `PinToggleName` | Pin to Dock / Unpin | 固定到快捷栏 / 取消固定 |
| | `ViewChartName` | View Trend Chart | 查看趋势图 |
| **Settings Page** | `RefreshIntervalLabel` | Content refresh interval (ms) | 内容页刷新间隔 (ms) |
| (`HardwareMonitorSettingsManager.cs`) | `RefreshIntervalDesc` | 500–3000, active when page is visible | 500–3000，页面可见时生效 |
| | `HotThresholdLabel` | Overheat threshold (°C) | 过热阈值 (°C) |
| | `HotThresholdDesc` | Shows orange when reaching this temperature | 达到该温度显示橙色 |
| | `CriticalThresholdLabel` | Critical threshold (°C) | 危险阈值 (°C) |
| | `CriticalThresholdDesc` | Shows red and triggers alert when reaching this temperature | 达到该温度显示红色并预警 |
| | `CompactDefaultLabel` | Default compact layout | 默认紧凑布局 |
| | `CompactDefaultDesc` | Hide secondary fields when opened (Compact) | 打开列表时隐藏次要字段（Compact） |
| | `MergeLoadLabel` | Merge usage in Dock | 快捷栏合并显示占用 |
| | `MergeLoadDesc` | Append usage percentage after temperature reading to save space | 温度读数后附加同硬件占用百分比，节省空间 |
| | `LanguageLabel` | Interface Language | 界面语言 |
| | `LanguageDesc` | Auto (System Default), English, or 简体中文 | 跟随系统、English 或 简体中文 |
| **Dock Band** | `DockBandTitle` | Hardware Readings | 硬件读数 |
| (`PinnedSensorsDockBand.cs`) | `NoDataSubtitle` | No data available | 暂无数据 |
| **Sensors List Page** | `ListPageTitle` | Hardware Monitor | 硬件监视器 |
| (`SensorsListPage.cs`) | `ListPageAction` | Open | 打开 |
| | `SearchPlaceholder` | Filter sensors… | 筛选传感器… |
| | `StaleSuffix` | · May be stale | · 可能过期 |
| | `ListFooterFormat` | `{0} items · Temp {1} · Pinned {2} · Peak {3}°C{4}` | `{0} 项 · 温度 {1} · 已固定 {2} · 峰值 {3}°C{4}` |
| **Chart Page & GDI+** | `ChartPageTitle` | Reading Details | 读数详情 |
| (`SensorChartPage.cs` & | `ChartPageAction` | View | 查看 |
| `ChartRenderer.cs`) | `NotFoundTitle` | # Reading Not Found | # 未找到读数 |
| | `NotFoundDesc` | `{0}` is not in current snapshot. | `{0}` 不在当前快照中。 |
| | `SourceLabel` | Source: | 来源： |
| | `HardwareLabel` | Hardware: | 硬件： |
| | `GroupLabel` | Group: | 分组： |
| | `HistoryPointsLabel` | History points: | 历史点： |
| | `SamplingNotice` | _Sampling in progress, trend will appear shortly…_ | _采样中，稍候即可看到趋势…_ |
| | `TrendHeading` | ## Trend (asciichart) | ## 趋势（asciichart · 代码绘制，非位图） |
| | `ChartMetaFormat` | `Current **{0}** · Unit `{1}` · Window ~3 min · Points {2}/{3}` | `当前 **{0}** · 单位 `{1}` · 窗口约 3 分钟 · 点 {2}/{3}` |
| | `ThresholdsFormat` | `Thresholds: Warm {0:0}° · Hot {1:0}° · Critical {2:0}°` | `阈值：偏热 {0:0}° · 过热 {1:0}° · 危险 {2:0}°` |
| | `RecentSamplesHeading`| ## Recent Samples | ## 近期采样 |
| | `TableHeaderTime` | Time | 时间 |
| | `TableHeaderValue` | Value | 值 |
| | `ChartNotEnoughPoints` | Insufficient samples, accumulating… | 采样点不足，正在积累… |
| | `ChartHotLabel` | Hot | 过热 |
| | `ChartCritLabel` | Critical | 危险 |
| | `ChartRangeFormat` | `Current {0}{1} · Range {2}–{3} · Points {4}` | `当前 {0}{1} · 区间 {2}–{3} · 点数 {4}` |
| **Hardware Groups** | `GroupProcessor` | Processor | 处理器 |
| (`SensorService.cs`) | `GroupGraphics` | Graphics | 显卡 |
| | `GroupStorage` | Storage | 存储 |
| | `GroupMotherboard` | Motherboard | 主板 |
| | `GroupMemory` | Memory | 内存 |
| | `GroupBattery` | Battery | 电池 |
| | `GroupNetwork` | Network | 网络 |
| | `GroupOther` | Other | 其他 |
| **Backend & Status** | `BackendInitializing` | Initializing | 初始化中 |
| | `BackendLhmNoPawn` | LibreHardwareMonitor (No PawnIO) | LibreHardwareMonitor（无 PawnIO） |
| | `BackendLhmWithPawn` | LibreHardwareMonitor + PawnIO installed | LibreHardwareMonitor + PawnIO 已安装 |
| | `BackendLhmFailNoPawn`| LHM open failed · No PawnIO | LHM 打开失败 · 无 PawnIO |
| | `BackendLhmFailPawn` | LHM open failed · PawnIO installed | LHM 打开失败 · PawnIO 已安装 |
| | `StatusCannotRead` | Unable to read sensors | 无法读取传感器 |
| | `StatusCheckPerms` | Please check PawnIO / Administrator privileges | 请检查 PawnIO / 管理员权限 |
| | `StatusNoData` | No data | 无数据 |
| | `StatusTitle` | Status | 状态 |
| | `AcpiThermalZone` | ACPI Thermal Zone | ACPI 热区 |
| | `EstimatedSuffix` | (Estimated) | （估算） |
| | `CpuPackageTemp` | CPU Package Temperature (Tctl) | CPU 封装温度 (Tctl) |
| | `CpuTotalLoad` | CPU Total Load | CPU 总占用 |
| | `CpuPackagePowerEst` | CPU Package Power (Estimated) | CPU 封装功率（估算） |

---

## 4. Retooled Phase 3: Web Dashboard Localization

### 1. Extensible Locale Registry (`LOCALES`)
Implemented in `Assets/app.js`:
```javascript
const LOCALES = {
  "en-US": {
    code: "en-US",
    nativeName: "English (US)",
    names: {
      "en-US": "English (US)",
      "zh-Hans": "英语 (美国)",
    },
    translations: {
      metaInit: "Real-time sampling · Preparing...",
      metaPaused: " · Paused",
      metaLive: "Real data · PawnIO/LHM",
      metaDemo: "Demo data (live-data.js not detected)",
      metaStale: "Real data paused (keep extension running) · Last ",
      stress: "Stress",
      pause: "Pause",
      resume: "Resume",
      dockTitle: "Command Palette · Dock",
      allSensors: "All Sensors",
      detailedMetrics: "Detailed Metrics",
      tableNote: "Click row to switch primary chart",
      thSensor: "Sensor",
      thGroup: "Group",
      thKind: "Kind",
      thCurrent: "Current",
      thSource: "Source",
      pin: "Pin",
      pinned: "Pinned",
      pointsSuffix: "pts",
      groups: {
        Processor: "Processor",
        Graphics: "Graphics",
        Storage: "Storage",
        Motherboard: "Motherboard",
        Memory: "Memory",
        Battery: "Battery",
        Network: "Network",
        Other: "Other"
      },
      kpis: {
        cpuTemp: "CPU Temp",
        cpuPower: "CPU Power",
        cpuLoad: "CPU Load",
        gpuTemp: "GPU Temp",
        ssdTemp: "SSD Temp",
        fan: "Fan",
        motherboard: "Motherboard",
        memory: "Memory"
      }
    }
  },
  "zh-Hans": {
    code: "zh-Hans",
    nativeName: "简体中文",
    names: {
      "en-US": "Chinese (Simplified)",
      "zh-Hans": "简体中文",
    },
    translations: {
      metaInit: "实时采样 · 准备中…",
      metaPaused: " · 已暂停",
      metaLive: "真实数据 · PawnIO/LHM",
      metaDemo: "演示数据（未检测到 live-data.js）",
      metaStale: "真实数据已停更（请保持扩展运行）· 最后 ",
      stress: "压测",
      pause: "暂停",
      resume: "恢复",
      dockTitle: "命令面板 · Dock",
      allSensors: "全部读数",
      detailedMetrics: "明细表",
      tableNote: "点击行可切换主图",
      thSensor: "指标",
      thGroup: "分组",
      thKind: "类型",
      thCurrent: "当前值",
      thSource: "来源",
      pin: "固定",
      pinned: "已固定",
      pointsSuffix: "点",
      groups: {
        Processor: "处理器",
        Graphics: "显卡",
        Storage: "存储",
        Motherboard: "主板",
        Memory: "内存",
        Battery: "电池",
        Network: "网络",
        Other: "其他"
      },
      kpis: {
        cpuTemp: "CPU 温",
        cpuPower: "CPU 功率",
        cpuLoad: "CPU 占用",
        gpuTemp: "GPU 温",
        ssdTemp: "SSD 温",
        fan: "风扇",
        motherboard: "主板",
        memory: "内存"
      }
    }
  }
};
```

### 2. Auto-Detection & Persistence Engine
1. **Saved User Choice**: Reads `localStorage.getItem("hwmon_lang")`.
2. **Browser Fallback**: Inspects `navigator.languages` / `navigator.language` (e.g. `zh-CN`, `zh` → `zh-Hans`; `en-US`, `en` → `en-US`).
3. **Safe Global Fallback**: Defaults to `en-US` for any unsupported locale.

### 3. Dynamic Perspective Dropdown Behavior
The language selector button sits in `.top-actions` of `Assets/index.html`.

```
Dropdown trigger: [ 🌐 English (US) ▾ ]

1. Resting / Default State (or Hovering en-US Option):
┌─────────────────────────────────────────────────────────────┐
│  English (US)              English (US)                     │ <-- Hovered en-US
│  简体中文                   Chinese (Simplified)             │
└─────────────────────────────────────────────────────────────┘

2. Hovering 简体中文 Option (Perspective flips dynamically to zh-Hans):
┌─────────────────────────────────────────────────────────────┐
│  English (US)              英语 (美国)                       │
│  简体中文                   简体中文                         │ <-- Hovered zh-Hans
└─────────────────────────────────────────────────────────────┘
```

- **Left column (`.opt-native`)**: Always displays the language in its own script (`English (US)`, `简体中文`).
- **Right column (`.opt-desc`)**: Dynamically updates on `mouseover` across all options to display their names as translated by the hovered language option.
- **On `mouseleave`**: Right column labels revert smoothly to the currently selected active language.
- **On Click**: Updates active language, re-renders the dashboard in-place, saves preference in `localStorage`, and closes menu.

### 4. Group Normalization & Dynamic Ingestion
- `app.js` normalizes incoming groups from `live-data.js`: whether the payload provides `"Processor"` or legacy `"处理器"`, it translates to the active locale's display label and correctly feeds `pickByGroup(...)` for CPU, GPU, Storage, and Motherboard mini-charts.

---

## 5. Phase 4: Verification & Test Plan

| Test ID | Test Category | Objective | Verification Procedure & Acceptance Criteria |
| :--- | :--- | :--- | :--- |
| **T-01** | **Build & Static Analysis** | Verify compile health and zero regressions. | Run `dotnet build -c Debug -p:Platform=x64 -p:GenerateAppxPackageOnBuild=false -p:EnableMsixTooling=false`.<br>**Acceptance**: Must exit with code `0`, 0 compilation errors. |
| **T-02** | **Domain Key Integrity & Invariants** | Ensure core monitoring and pinning logic remain intact. | 1. Verify `PinnedSensorStore.EnsureDefaults` pins processor/GPU temp and load correctly without hardcoded string matching.<br>2. Verify synthetic CPU load/power estimates compute accurately in `SensorService`.<br>3. Verify compact dock initial prefixes (`"C"`, `"G"`, `"S"`, `"M"`) map accurately under all cultures. |
| **T-03** | **C# Runtime Culture Switching** | Validate localization across extension UI components. | 1. Set `CultureInfo.CurrentUICulture = new CultureInfo("en-US")` → verify Command Provider, Settings, Dock Band, List Page footer, and ASCII charts display in English.<br>2. Set `CultureInfo.CurrentUICulture = new CultureInfo("zh-CN")` → verify all UI elements display in Simplified Chinese.<br>3. Set `CultureInfo.CurrentUICulture = new CultureInfo("ja-JP")` (unsupported) → verify graceful fallback to `en-US` without runtime faults. |
| **T-04** | **Dashboard Auto-Detection & Persistence** | Verify browser language detection in web UI. | 1. Launch `index.html` with browser language set to `en-US` → UI defaults to `en-US`.<br>2. Launch with browser language set to `zh-CN` → UI defaults to `zh-Hans`.<br>3. Launch with unsupported locale (e.g. `de-DE`) → defaults to `en-US`.<br>4. Manually switch language → reload page → verify manual choice persists via `localStorage`. |
| **T-05** | **Dynamic Dropdown Hover Matrix** | Validate dynamic perspective hover behavior. | 1. Open dropdown while `en-US` is active: verify right column shows `English (US)` and `Chinese (Simplified)`.<br>2. Hover over `简体中文`: verify right column dynamically updates to `英语 (美国)` and `简体中文`.<br>3. Move mouse out of menu: verify right column reverts to `en-US` perspective.<br>4. Click `简体中文`: verify UI instantly translates to Chinese and menu closes. |
| **T-06** | **Telemetry Ingestion & Group Filtering** | Verify live data and fallback demo mode. | 1. In Demo mode: verify all demo sensors, units, and KPIs render in the active language.<br>2. In Live mode with simulated `live-data.js`: verify `pickByGroup` populates mini-charts and data table regardless of language selection. |

---

## 6. Review & Sign-Off

The specification header has been updated:
- **Author**: `zhang-astronaut`
- **Contributor**: `Nathan Myron, MyronTech Solutions`

All code modifications will be compiled, detailed, and presented for explicit approval before modifying source files.
