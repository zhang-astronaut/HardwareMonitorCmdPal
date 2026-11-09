# Comprehensive Code Modification Plan & Diff Specification

**Product**: Hardware Monitor for PowerToys Command Palette  
**Author**: zhang-astronaut  
**Contributor**: Nathan Myron, MyronTech Solutions  
**Target Repository Folder**: `HardwareMonitorCmdPal.Localized`  
**Reference (Pristine) Folder**: `HardwareMonitorCmdPal.Original` (Untouched)  

---

## Summary of Changes

| Target File | Change Type | Purpose |
| :--- | :--- | :--- |
| `Localization/ILocalizationProvider.cs` | **NEW** | Defines strongly typed interface for all UI labels, messages, and formatters. |
| `Localization/EnUsLocalizationProvider.cs` | **NEW** | English (US) implementation (Default & Fallback). |
| `Localization/ZhHansLocalizationProvider.cs` | **NEW** | Simplified Chinese implementation matching original application strings. |
| `Localization/Loc.cs` | **NEW** | Global thread-safe accessor with `CultureInfo.CurrentUICulture` auto-detection and override API. |
| `Services/SensorModels.cs` | Modify | Adds `HardwareGroups` invariant constants, `LocalizedGroup` property, and decouples compact dock prefixes. |
| `Services/SensorService.cs` | Modify | Replaces hardcoded Chinese group assignments, status labels, and synthetic CPU sensor names with `Loc.Current`. |
| `Services/PinnedSensorStore.cs` | Modify | Decouples default pin matching queries to work on invariant keys and both English/Chinese labels. |
| `HardwareMonitorCommandsProvider.cs` | Modify | Localizes top-level commands, subtitles, and context commands. |
| `HardwareMonitorSettingsManager.cs` | Modify | Localizes setting labels and descriptions. |
| `Dock/PinnedSensorsDockBand.cs` | Modify | Localizes Dock Band title and empty status message. |
| `Pages/SensorsListPage.cs` | Modify | Localizes list page title, placeholder, footer summary, stale notice, and layout groupings. |
| `Pages/ListCommands.cs` | Modify | Localizes context menu action names. |
| `Pages/SensorChartPage.cs` | Modify | Localizes markdown headings, metadata fields, table headers, and threshold lines. |
| `Services/ChartRenderer.cs` | Modify | Localizes GDI+ chart labels and fallback messages. |
| `Services/DashboardHost.cs` | Modify | Localizes fallback HTML message. |
| `Assets/index.html` | Modify | Adds dynamic perspective language dropdown and `data-i18n` attributes. |
| `Assets/styles.css` | Modify | Adds styling for dropdown menu, two-column hover matrix, and caret. |
| `Assets/app.js` | Modify | Implements `LOCALES` registry, browser auto-detection, dynamic perspective hover switching, and group normalization. |

---

## Detailed Code Modifications & Diffs

### 1. New File: `HardwareMonitorExtension/Localization/ILocalizationProvider.cs`
```csharp
namespace HardwareMonitorExtension.Localization;

public interface ILocalizationProvider
{
    string LocaleCode { get; }
    string NativeName { get; }

    // Commands & Identity
    string ExtensionDisplayName { get; }
    string AllSensorsSubtitle { get; }
    string OpenDashboardTitle { get; }
    string DashboardSubtitle { get; }
    string InstallHelperTitle { get; }
    string InstallHelperSubtitle { get; }

    // Action Commands
    string ToggleCompactName { get; }
    string PinToggleName { get; }
    string ViewChartName { get; }

    // Settings
    string RefreshIntervalLabel { get; }
    string RefreshIntervalDesc { get; }
    string HotThresholdLabel { get; }
    string HotThresholdDesc { get; }
    string CriticalThresholdLabel { get; }
    string CriticalThresholdDesc { get; }
    string CompactDefaultLabel { get; }
    string CompactDefaultDesc { get; }
    string MergeLoadLabel { get; }
    string MergeLoadDesc { get; }

    // Dock Band
    string DockBandTitle { get; }
    string NoDataSubtitle { get; }

    // List Page
    string ListPageTitle { get; }
    string ListPageAction { get; }
    string SearchPlaceholder { get; }
    string StaleSuffix { get; }
    string ListFooter(int total, int tempCount, int pinCount, double peak, string marker);

    // Chart & Markdown
    string ChartPageTitle { get; }
    string ChartPageAction { get; }
    string NotFoundMarkdown(string sensorId);
    string SourceLabel { get; }
    string HardwareLabel { get; }
    string GroupLabel { get; }
    string HistoryPointsLabel { get; }
    string SamplingNotice { get; }
    string TrendHeading { get; }
    string ChartMeta(string displayValue, string unit, int windowMin, int sampled, int total);
    string Thresholds(double warm, double hot, double crit);
    string RecentSamplesHeading { get; }
    string TableHeaderTime { get; }
    string TableHeaderValue { get; }

    // GDI+ Chart Renderer
    string ChartNotEnoughPoints { get; }
    string ChartHotLabel { get; }
    string ChartCritLabel { get; }
    string ChartRange(double last, string unit, double min, double max, int points);

    // Hardware Groups
    string GroupProcessor { get; }
    string GroupGraphics { get; }
    string GroupStorage { get; }
    string GroupMotherboard { get; }
    string GroupMemory { get; }
    string GroupBattery { get; }
    string GroupNetwork { get; }
    string GroupOther { get; }
    string GetGroupDisplayName(string groupKey);

    // Backend & Status
    string BackendInitializing { get; }
    string BackendLhmNoPawn { get; }
    string BackendLhmWithPawn { get; }
    string BackendLhmFailNoPawn { get; }
    string BackendLhmFailPawn { get; }
    string StatusCannotRead { get; }
    string StatusCheckPerms { get; }
    string StatusNoData { get; }
    string StatusTitle { get; }
    string AcpiThermalZone(string zoneName);
    string CpuPackageTemp { get; }
    string CpuPackage { get; }
    string CpuTotalLoad { get; }
    string CpuPackagePowerEst { get; }
    string CpuPower { get; }
}
```

---

### 2. New File: `HardwareMonitorExtension/Localization/EnUsLocalizationProvider.cs`
```csharp
namespace HardwareMonitorExtension.Localization;

public sealed class EnUsLocalizationProvider : ILocalizationProvider
{
    public string LocaleCode => "en-US";
    public string NativeName => "English (US)";

    public string ExtensionDisplayName => "Hardware Monitor";
    public string AllSensorsSubtitle => "All sensors · Can be pinned to dock";
    public string OpenDashboardTitle => "Open Real-Time Dashboard";
    public string DashboardSubtitle => "btop-style offline index.html";
    public string InstallHelperTitle => "Install CPU Temp Startup Sampling (One-time)";
    public string InstallHelperSubtitle => "Runs at startup automatically; normal user CPU Tctl is 0";

    public string ToggleCompactName => "Toggle Compact Layout";
    public string PinToggleName => "Pin to Dock / Unpin";
    public string ViewChartName => "View Trend Chart";

    public string RefreshIntervalLabel => "Content refresh interval (ms)";
    public string RefreshIntervalDesc => "500–3000, active when page is visible";
    public string HotThresholdLabel => "Overheat threshold (°C)";
    public string HotThresholdDesc => "Shows orange when reaching this temperature";
    public string CriticalThresholdLabel => "Critical threshold (°C)";
    public string CriticalThresholdDesc => "Shows red and triggers alert when reaching this temperature";
    public string CompactDefaultLabel => "Default compact layout";
    public string CompactDefaultDesc => "Hide secondary fields when opened (Compact)";
    public string MergeLoadLabel => "Merge usage in Dock";
    public string MergeLoadDesc => "Append usage percentage after temperature reading to save space";

    public string DockBandTitle => "Hardware Readings";
    public string NoDataSubtitle => "No data available";

    public string ListPageTitle => "Hardware Monitor";
    public string ListPageAction => "Open";
    public string SearchPlaceholder => "Filter sensors…";
    public string StaleSuffix => " · May be stale";
    public string ListFooter(int total, int tempCount, int pinCount, double peak, string marker) =>
        $"{total} items · Temp {tempCount} · Pinned {pinCount} · Peak {Math.Round(peak)}°C{marker}";

    public string ChartPageTitle => "Reading Details";
    public string ChartPageAction => "View";
    public string NotFoundMarkdown(string sensorId) => $"# Reading Not Found\n\n`{sensorId}` is not in current snapshot.";
    public string SourceLabel => "Source:";
    public string HardwareLabel => "Hardware:";
    public string GroupLabel => "Group:";
    public string HistoryPointsLabel => "History points:";
    public string SamplingNotice => "_Sampling in progress, trend will appear shortly…_";
    public string TrendHeading => "## Trend (asciichart)";
    public string ChartMeta(string displayValue, string unit, int windowMin, int sampled, int total) =>
        $"Current **{displayValue}** · Unit `{unit}` · Window ~{windowMin} min · Points {sampled}/{total}";
    public string Thresholds(double warm, double hot, double crit) =>
        $"Thresholds: Warm {warm:0}° · Hot {hot:0}° · Critical {crit:0}°";
    public string RecentSamplesHeading => "## Recent Samples";
    public string TableHeaderTime => "Time";
    public string TableHeaderValue => "Value";

    public string ChartNotEnoughPoints => "Insufficient samples, accumulating…";
    public string ChartHotLabel => "Hot";
    public string ChartCritLabel => "Critical";
    public string ChartRange(double last, string unit, double min, double max, int points) =>
        $"Current {last:0}{unit}  ·  Range {min:0}–{max:0}  ·  Points {points}";

    public string GroupProcessor => "Processor";
    public string GroupGraphics => "Graphics";
    public string GroupStorage => "Storage";
    public string GroupMotherboard => "Motherboard";
    public string GroupMemory => "Memory";
    public string GroupBattery => "Battery";
    public string GroupNetwork => "Network";
    public string GroupOther => "Other";

    public string GetGroupDisplayName(string groupKey) => groupKey switch
    {
        "Processor" or "处理器" => GroupProcessor,
        "Graphics" or "显卡" => GroupGraphics,
        "Storage" or "存储" => GroupStorage,
        "Motherboard" or "主板" => GroupMotherboard,
        "Memory" or "内存" => GroupMemory,
        "Battery" or "电池" => GroupBattery,
        "Network" or "网络" => GroupNetwork,
        _ => GroupOther,
    };

    public string BackendInitializing => "Initializing";
    public string BackendLhmNoPawn => "LibreHardwareMonitor (No PawnIO)";
    public string BackendLhmWithPawn => "LibreHardwareMonitor + PawnIO installed";
    public string BackendLhmFailNoPawn => "LHM open failed · No PawnIO";
    public string BackendLhmFailPawn => "LHM open failed · PawnIO installed";
    public string StatusCannotRead => "Unable to read sensors";
    public string StatusCheckPerms => "Please check PawnIO / Administrator privileges";
    public string StatusNoData => "No data";
    public string StatusTitle => "Status";
    public string AcpiThermalZone(string zoneName) => $"ACPI Thermal Zone {zoneName}";
    public string CpuPackageTemp => "CPU Package Temperature (Tctl)";
    public string CpuPackage => "CPU Package";
    public string CpuTotalLoad => "CPU Total Load";
    public string CpuPackagePowerEst => "CPU Package Power (Estimated)";
    public string CpuPower => "CPU Power";
}
```

---

### 3. New File: `HardwareMonitorExtension/Localization/ZhHansLocalizationProvider.cs`
```csharp
namespace HardwareMonitorExtension.Localization;

public sealed class ZhHansLocalizationProvider : ILocalizationProvider
{
    public string LocaleCode => "zh-Hans";
    public string NativeName => "简体中文";

    public string ExtensionDisplayName => "硬件监视器";
    public string AllSensorsSubtitle => "全部传感器 · 可固定到快捷栏";
    public string OpenDashboardTitle => "打开实时图表看板";
    public string DashboardSubtitle => "btop 风格离线 index.html";
    public string InstallHelperTitle => "安装 CPU 温度开机采样（一次）";
    public string InstallHelperSubtitle => "注册后开机自动，无需再点；普通权限 CPU Tctl 为 0";

    public string ToggleCompactName => "切换紧凑布局";
    public string PinToggleName => "固定到快捷栏 / 取消固定";
    public string ViewChartName => "查看趋势图";

    public string RefreshIntervalLabel => "内容页刷新间隔 (ms)";
    public string RefreshIntervalDesc => "500–3000，页面可见时生效";
    public string HotThresholdLabel => "过热阈值 (°C)";
    public string HotThresholdDesc => "达到该温度显示橙色";
    public string CriticalThresholdLabel => "危险阈值 (°C)";
    public string CriticalThresholdDesc => "达到该温度显示红色并预警";
    public string CompactDefaultLabel => "默认紧凑布局";
    public string CompactDefaultDesc => "打开列表时隐藏次要字段（Compact）";
    public string MergeLoadLabel => "快捷栏合并显示占用";
    public string MergeLoadDesc => "温度读数后附加同硬件占用百分比，节省空间";

    public string DockBandTitle => "硬件读数";
    public string NoDataSubtitle => "暂无数据";

    public string ListPageTitle => "硬件监视器";
    public string ListPageAction => "打开";
    public string SearchPlaceholder => "筛选传感器…";
    public string StaleSuffix => " · 可能过期";
    public string ListFooter(int total, int tempCount, int pinCount, double peak, string marker) =>
        $"{total} 项 · 温度 {tempCount} · 已固定 {pinCount} · 峰值 {Math.Round(peak)}°C{marker}";

    public string ChartPageTitle => "读数详情";
    public string ChartPageAction => "查看";
    public string NotFoundMarkdown(string sensorId) => $"# 未找到读数\n\n`{sensorId}` 不在当前快照中。";
    public string SourceLabel => "来源：";
    public string HardwareLabel => "硬件：";
    public string GroupLabel => "分组：";
    public string HistoryPointsLabel => "历史点：";
    public string SamplingNotice => "_采样中，稍候即可看到趋势…_";
    public string TrendHeading => "## 趋势（asciichart · 代码绘制，非位图）";
    public string ChartMeta(string displayValue, string unit, int windowMin, int sampled, int total) =>
        $"当前 **{displayValue}** · 单位 `{unit}` · 窗口约 {windowMin} 分钟 · 点 {sampled}/{total}";
    public string Thresholds(double warm, double hot, double crit) =>
        $"阈值：偏热 {warm:0}° · 过热 {hot:0}° · 危险 {crit:0}°";
    public string RecentSamplesHeading => "## 近期采样";
    public string TableHeaderTime => "时间";
    public string TableHeaderValue => "值";

    public string ChartNotEnoughPoints => "采样点不足，正在积累…";
    public string ChartHotLabel => "过热";
    public string ChartCritLabel => "危险";
    public string ChartRange(double last, string unit, double min, double max, int points) =>
        $"当前 {last:0}{unit}  ·  区间 {min:0}–{max:0}  ·  点数 {points}";

    public string GroupProcessor => "处理器";
    public string GroupGraphics => "显卡";
    public string GroupStorage => "存储";
    public string GroupMotherboard => "主板";
    public string GroupMemory => "内存";
    public string GroupBattery => "电池";
    public string GroupNetwork => "网络";
    public string GroupOther => "其他";

    public string GetGroupDisplayName(string groupKey) => groupKey switch
    {
        "Processor" or "处理器" => GroupProcessor,
        "Graphics" or "显卡" => GroupGraphics,
        "Storage" or "存储" => GroupStorage,
        "Motherboard" or "主板" => GroupMotherboard,
        "Memory" or "内存" => GroupMemory,
        "Battery" or "电池" => GroupBattery,
        "Network" or "网络" => GroupNetwork,
        _ => GroupOther,
    };

    public string BackendInitializing => "初始化中";
    public string BackendLhmNoPawn => "LibreHardwareMonitor（无 PawnIO）";
    public string BackendLhmWithPawn => "LibreHardwareMonitor + PawnIO 已安装";
    public string BackendLhmFailNoPawn => "LHM 打开失败 · 无 PawnIO";
    public string BackendLhmFailPawn => "LHM 打开失败 · PawnIO 已安装";
    public string StatusCannotRead => "无法读取传感器";
    public string StatusCheckPerms => "请检查 PawnIO / 管理员权限";
    public string StatusNoData => "无数据";
    public string StatusTitle => "状态";
    public string AcpiThermalZone(string zoneName) => $"ACPI 热区 {zoneName}";
    public string CpuPackageTemp => "CPU 封装温度 (Tctl)";
    public string CpuPackage => "CPU 封装";
    public string CpuTotalLoad => "CPU 总占用";
    public string CpuPackagePowerEst => "CPU 封装功率（估算）";
    public string CpuPower => "CPU 功率";
}
```

---

### 4. New File: `HardwareMonitorExtension/Localization/Loc.cs`
```csharp
using System.Globalization;

namespace HardwareMonitorExtension.Localization;

public static class Loc
{
    private static readonly EnUsLocalizationProvider EnUs = new();
    private static readonly ZhHansLocalizationProvider ZhHans = new();
    private static ILocalizationProvider _current = ResolveFromCulture(CultureInfo.CurrentUICulture);

    public static ILocalizationProvider Current => _current;

    public static void SetCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName) || cultureName.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            _current = ResolveFromCulture(CultureInfo.CurrentUICulture);
            return;
        }

        try
        {
            _current = ResolveFromCulture(new CultureInfo(cultureName));
        }
        catch
        {
            _current = EnUs;
        }
    }

    private static ILocalizationProvider ResolveFromCulture(CultureInfo culture)
    {
        var name = culture.Name.ToLowerInvariant();
        if (name.StartsWith("zh"))
            return ZhHans;
        return EnUs;
    }
}
```

---

### 5. Mod: `HardwareMonitorExtension/Services/SensorModels.cs`
```diff
--- HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Services/SensorModels.cs
+++ HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Services/SensorModels.cs
@@ -1,4 +1,5 @@
 // Copyright (c) HardwareMonitor. MIT license.
+using HardwareMonitorExtension.Localization;
 
 namespace HardwareMonitorExtension.Services;
 
+public static class HardwareGroups
+{
+    public const string Processor = "Processor";
+    public const string Graphics = "Graphics";
+    public const string Storage = "Storage";
+    public const string Motherboard = "Motherboard";
+    public const string Memory = "Memory";
+    public const string Battery = "Battery";
+    public const string Network = "Network";
+    public const string Other = "Other";
+}
+
 public enum HeatLevel
 {
@@ -43,6 +55,7 @@
     string? MergedLoadText = null)
 {
     public double? TemperatureC => Kind == SensorKind.Temperature ? Value : null;
     public double? LoadPercent => Kind == SensorKind.Load ? Value : null;
+    public string LocalizedGroup => Loc.Current.GetGroupDisplayName(Group);
 
     public string DisplayValue => Kind switch
@@ -89,14 +102,14 @@
     public string FormatDockCompactTitle(bool mergeLoad)
     {
         var prefix = Group switch
         {
-            "处理器" => "C",
-            "显卡" => "G",
-            "存储" => "S",
-            "主板" => "M",
-            "内存" => "R",
-            "电池" => "B",
+            HardwareGroups.Processor or "处理器" => "C",
+            HardwareGroups.Graphics or "显卡" => "G",
+            HardwareGroups.Storage or "存储" => "S",
+            HardwareGroups.Motherboard or "主板" => "M",
+            HardwareGroups.Memory or "内存" => "R",
+            HardwareGroups.Battery or "电池" => "B",
             _ => "#",
         };
         return $"{prefix} {FormatDockTitle(mergeLoad)}";
     }
```

---

### 6. Mod: `HardwareMonitorExtension/Services/SensorService.cs`
```diff
--- HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Services/SensorService.cs
+++ HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Services/SensorService.cs
@@ -3,4 +3,5 @@
 using System.Diagnostics;
 using System.Management;
+using HardwareMonitorExtension.Localization;
 using LibreHardwareMonitor.Hardware;
 
@@ -19,3 +20,3 @@
-    private string _backendLabel = "初始化中";
+    private string _backendLabel = Loc.Current.BackendInitializing;
     private bool _disposed;
     private int _sampleFailures;
@@ -44,6 +45,6 @@
         var pawn = PawnIoLocator.FindInstallDirectory();
         if (!lhm)
-            return pawn is null ? "LHM 打开失败 · 无 PawnIO" : "LHM 打开失败 · PawnIO 已安装";
+            return pawn is null ? Loc.Current.BackendLhmFailNoPawn : Loc.Current.BackendLhmFailPawn;
         return pawn is null
-            ? "LibreHardwareMonitor（无 PawnIO）"
-            : "LibreHardwareMonitor + PawnIO 已安装";
+            ? Loc.Current.BackendLhmNoPawn
+            : Loc.Current.BackendLhmWithPawn;
     }
@@ -179,3 +180,3 @@
     {
-        if (!list.Any(s => s.Kind == SensorKind.Load && s.Group == "处理器"))
+        if (!list.Any(s => s.Kind == SensorKind.Load && (s.Group == HardwareGroups.Processor || s.Group == "处理器")))
         {
@@ -186,6 +187,6 @@
                 new SensorReading(
                     "cpu/fallback/load",
-                    "处理器",
-                    "CPU 占用",
+                    HardwareGroups.Processor,
+                    Loc.Current.CpuTotalLoad,
                     "PerfCounter",
                     SensorKind.Load,
                     cpuLoad.Value,
                     "%",
                     "CPU",
                     "CPU"));
@@ -216,9 +217,9 @@
             var hasValidCpuTemp = list.Any(s =>
-                s.Group == "处理器" &&
+                (s.Group == HardwareGroups.Processor || s.Group == "处理器") &&
                 s.Kind == SensorKind.Temperature &&
                 s.Value > 1 &&
                 s.Value < 150);
             var hasZeroCpuTemp = list.Any(s =>
-                s.Kind == SensorKind.Temperature && s.Group == "处理器" && s.Value is >= -50 and <= 1);
+                s.Kind == SensorKind.Temperature && (s.Group == HardwareGroups.Processor || s.Group == "处理器") && s.Value is >= -50 and <= 1);
             var hasAnyCpuTemp = list.Any(s =>
-                s.Kind == SensorKind.Temperature && s.Group == "处理器");
+                s.Kind == SensorKind.Temperature && (s.Group == HardwareGroups.Processor || s.Group == "处理器"));
 
@@ -243,9 +244,9 @@
                 var elevatedReading = new SensorReading(
                     "cpu/elevated/tctl",
-                    "处理器",
-                    "CPU 封装温度 (Tctl)",
+                    HardwareGroups.Processor,
+                    Loc.Current.CpuPackageTemp,
                     "Helper/MSR",
                     SensorKind.Temperature,
                     tctl.Value,
                     "°C",
                     "CPU",
-                    "CPU 封装");
+                    Loc.Current.CpuPackage);
                 list.Insert(0, elevatedReading);
@@ -257,4 +258,4 @@
     {
-        var load = list.FirstOrDefault(s => s.Kind == SensorKind.Load && s.Group == "处理器")?.Value ?? 15.0;
+        var load = list.FirstOrDefault(s => s.Kind == SensorKind.Load && (s.Group == HardwareGroups.Processor || s.Group == "处理器"))?.Value ?? 15.0;
         var hasPower = list
-            .Where(s => s.Kind == SensorKind.Power && s.Group == "处理器")
+            .Where(s => s.Kind == SensorKind.Power && (s.Group == HardwareGroups.Processor || s.Group == "处理器"))
             .Any(s => s.Value > 0.5);
@@ -274,3 +275,3 @@
         {
-            if (list[i].Kind == SensorKind.Power && list[i].Group == "处理器" && list[i].Value <= 0.5)
+            if (list[i].Kind == SensorKind.Power && (list[i].Group == HardwareGroups.Processor || list[i].Group == "处理器") && list[i].Value <= 0.5)
             {
@@ -290,6 +291,6 @@
                 new SensorReading(
                     "cpu/estimate/power",
-                    "处理器",
-                    "CPU 封装功率（估算）",
+                    HardwareGroups.Processor,
+                    Loc.Current.CpuPackagePowerEst,
                     "PerfCounter(估算)",
                     SensorKind.Power,
                     est,
                     "W",
                     "CPU",
-                    "CPU 功率"));
+                    Loc.Current.CpuPower));
         }
@@ -372,7 +373,7 @@
         return type switch
         {
-            HardwareType.Cpu => "处理器",
-            HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel => "显卡",
-            HardwareType.Storage => "存储",
-            HardwareType.Motherboard => "主板",
-            HardwareType.Memory => "内存",
-            HardwareType.Battery => "电池",
-            HardwareType.Network => "网络",
-            _ => "其他",
+            HardwareType.Cpu => HardwareGroups.Processor,
+            HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel => HardwareGroups.Graphics,
+            HardwareType.Storage => HardwareGroups.Storage,
+            HardwareType.Motherboard => HardwareGroups.Motherboard,
+            HardwareType.Memory => HardwareGroups.Memory,
+            HardwareType.Battery => HardwareGroups.Battery,
+            HardwareType.Network => HardwareGroups.Network,
+            _ => HardwareGroups.Other,
         };
@@ -447,4 +448,4 @@
                 list.Add(new SensorReading(
                     $"acpi/thermalzone/{name}",
-                    "主板",
-                    $"ACPI 热区 {name}",
+                    HardwareGroups.Motherboard,
+                    Loc.Current.AcpiThermalZone(name),
                     "WMI/ACPI",
                     SensorKind.Temperature,
@@ -468,4 +469,4 @@
             list.Add(new SensorReading(
                 "cpu/wmi/load",
-                "处理器",
-                "CPU 占用",
+                HardwareGroups.Processor,
+                Loc.Current.CpuTotalLoad,
                 "WMI/Win32_Processor",
                 SensorKind.Load,
@@ -483,9 +484,9 @@
             list.Add(new SensorReading(
                 "status/nodata",
-                "状态",
-                "无法读取传感器",
-                "请检查 PawnIO / 管理员权限",
+                Loc.Current.StatusTitle,
+                Loc.Current.StatusCannotRead,
+                Loc.Current.StatusCheckPerms,
                 SensorKind.Other,
                 0,
-                "无数据",
+                Loc.Current.StatusNoData,
                 "System",
@@ -512,12 +513,12 @@
     private static int GroupRank(string g) => g switch
     {
-        "处理器" => 0,
-        "显卡" => 1,
-        "存储" => 2,
-        "主板" => 3,
-        "内存" => 4,
-        "电池" => 5,
-        "网络" => 6,
+        HardwareGroups.Processor or "处理器" => 0,
+        HardwareGroups.Graphics or "显卡" => 1,
+        HardwareGroups.Storage or "存储" => 2,
+        HardwareGroups.Motherboard or "主板" => 3,
+        HardwareGroups.Memory or "内存" => 4,
+        HardwareGroups.Battery or "电池" => 5,
+        HardwareGroups.Network or "网络" => 6,
         _ => 99,
     };
```

---

### 7. Mod: `HardwareMonitorExtension/Services/PinnedSensorStore.cs`
```diff
--- HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Services/PinnedSensorStore.cs
+++ HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Services/PinnedSensorStore.cs
@@ -111,6 +111,8 @@
             // 预选顺序：CPU 封装温 -> CPU 占用 -> GPU 温 -> GPU 占用
-            AddPreferred(list.Where(s => s.Group == "处理器" && s.Kind == SensorKind.Temperature)
-                .OrderByDescending(s => s.ShortLabel.Contains("封装", StringComparison.Ordinal)), 1);
-            AddPreferred(list.Where(s => s.Group == "处理器" && s.Kind == SensorKind.Load), 1);
-            AddPreferred(list.Where(s => s.Group == "显卡" && s.Kind == SensorKind.Temperature), 1);
-            AddPreferred(list.Where(s => s.Group == "显卡" && s.Kind == SensorKind.Load), 1);
+            AddPreferred(list.Where(s => (s.Group == HardwareGroups.Processor || s.Group == "处理器") && s.Kind == SensorKind.Temperature)
+                .OrderByDescending(s => s.ShortLabel.Contains("封装", StringComparison.OrdinalIgnoreCase) || s.ShortLabel.Contains("Package", StringComparison.OrdinalIgnoreCase)), 1);
+            AddPreferred(list.Where(s => (s.Group == HardwareGroups.Processor || s.Group == "处理器") && s.Kind == SensorKind.Load), 1);
+            AddPreferred(list.Where(s => (s.Group == HardwareGroups.Graphics || s.Group == "显卡") && s.Kind == SensorKind.Temperature), 1);
+            AddPreferred(list.Where(s => (s.Group == HardwareGroups.Graphics || s.Group == "显卡") && s.Kind == SensorKind.Load), 1);
```

---

### 8. Mod: `HardwareMonitorExtension/HardwareMonitorCommandsProvider.cs`
```diff
--- HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/HardwareMonitorCommandsProvider.cs
+++ HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/HardwareMonitorCommandsProvider.cs
@@ -4,2 +4,3 @@
 using HardwareMonitorExtension.Pages;
+using HardwareMonitorExtension.Localization;
 using Microsoft.CommandPalette.Extensions;
@@ -23,3 +24,3 @@
     public HardwareMonitorCommandsProvider()
     {
-        DisplayName = "硬件监视器";
+        DisplayName = Loc.Current.ExtensionDisplayName;
         Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
@@ -34,7 +35,7 @@
             new CommandItem(listPage)
             {
                 Title = DisplayName,
-                Subtitle = "全部传感器 · 可固定到快捷栏",
+                Subtitle = Loc.Current.AllSensorsSubtitle,
                 MoreCommands = [
                     new CommandContextItem(new OpenDashboardCommand())
                     {
-                        Title = "打开实时图表看板",
+                        Title = Loc.Current.OpenDashboardTitle,
                         Icon = new IconInfo("🌐"),
@@ -48,4 +49,4 @@
             new CommandItem(new OpenDashboardCommand())
             {
-                Title = "实时图表看板",
-                Subtitle = "btop 风格离线 index.html",
+                Title = Loc.Current.OpenDashboardTitle,
+                Subtitle = Loc.Current.DashboardSubtitle,
                 Icon = new IconInfo("🌐"),
@@ -53,4 +54,4 @@
             new CommandItem(new StartElevatedTempHelperCommand())
             {
-                Title = "安装 CPU 温度开机采样（一次）",
-                Subtitle = "注册后开机自动，无需再点；普通权限 CPU Tctl 为 0",
+                Title = Loc.Current.InstallHelperTitle,
+                Subtitle = Loc.Current.InstallHelperSubtitle,
                 Icon = new IconInfo("🔐"),
```

---

### 9. Mod: `HardwareMonitorExtension/HardwareMonitorSettingsManager.cs`
```diff
--- HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/HardwareMonitorSettingsManager.cs
+++ HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/HardwareMonitorSettingsManager.cs
@@ -5,2 +5,3 @@
 using HardwareMonitorExtension.Services;
+using HardwareMonitorExtension.Localization;
 using Microsoft.CommandPalette.Extensions.Toolkit;
@@ -21,4 +22,4 @@
     private readonly TextSetting _refreshMs = new(
         Namespaced(nameof(RefreshMs)),
-        "内容页刷新间隔 (ms)",
-        "500–3000，页面可见时生效",
+        Loc.Current.RefreshIntervalLabel,
+        Loc.Current.RefreshIntervalDesc,
         "1000");
@@ -27,4 +28,4 @@
     private readonly TextSetting _hotC = new(
         Namespaced(nameof(HotC)),
-        "过热阈值 (°C)",
-        "达到该温度显示橙色",
+        Loc.Current.HotThresholdLabel,
+        Loc.Current.HotThresholdDesc,
         "80");
@@ -33,4 +34,4 @@
     private readonly TextSetting _critC = new(
         Namespaced(nameof(CriticalC)),
-        "危险阈值 (°C)",
-        "达到该温度显示红色并预警",
+        Loc.Current.CriticalThresholdLabel,
+        Loc.Current.CriticalThresholdDesc,
         "90");
@@ -39,4 +40,4 @@
     private readonly ToggleSetting _compactDefault = new(
         Namespaced(nameof(CompactByDefault)),
-        "默认紧凑布局",
-        "打开列表时隐藏次要字段（Compact）",
+        Loc.Current.CompactDefaultLabel,
+        Loc.Current.CompactDefaultDesc,
         false);
@@ -45,4 +46,4 @@
     private readonly ToggleSetting _mergeLoad = new(
         Namespaced(nameof(MergeLoadInDock)),
-        "快捷栏合并显示占用",
-        "温度读数后附加同硬件占用百分比，节省空间",
+        Loc.Current.MergeLoadLabel,
+        Loc.Current.MergeLoadDesc,
         true);
```

---

### 10. Mod: `HardwareMonitorExtension/Dock/PinnedSensorsDockBand.cs`
```diff
--- HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Dock/PinnedSensorsDockBand.cs
+++ HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Dock/PinnedSensorsDockBand.cs
@@ -4,2 +4,3 @@
 using HardwareMonitorExtension.Services;
+using HardwareMonitorExtension.Localization;
 using Microsoft.CommandPalette.Extensions.Toolkit;
@@ -23,3 +24,3 @@
     public PinnedSensorsDockBand(SensorHub hub)
-        : base([], "hardwaremonitor.pinned_band", "硬件读数")
+        : base([], "hardwaremonitor.pinned_band", Loc.Current.DockBandTitle)
     {
@@ -82,3 +83,3 @@
             item.Title = "--";
-            item.Subtitle = "暂无数据";
+            item.Subtitle = Loc.Current.NoDataSubtitle;
             item.Icon = new IconInfo("🌡");
```

---

### 11. Mod: `HardwareMonitorExtension/Pages/SensorsListPage.cs`
```diff
--- HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Pages/SensorsListPage.cs
+++ HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Pages/SensorsListPage.cs
@@ -4,2 +4,3 @@
 using HardwareMonitorExtension.Services;
+using HardwareMonitorExtension.Localization;
 using Microsoft.CommandPalette.Extensions;
@@ -32,3 +33,3 @@
         Icon = new IconInfo("🌡");
-        Title = "硬件监视器";
-        Name = "打开";
-        PlaceholderText = "筛选传感器…";
+        Title = Loc.Current.ListPageTitle;
+        Name = Loc.Current.ListPageAction;
         PlaceholderText = Loc.Current.SearchPlaceholder;
         EnsureHubHook();
@@ -121,3 +122,5 @@
-        foreach (var g in new[] { "处理器", "显卡", "存储", "主板", "内存", "电池", "网络", "其他" })
+        foreach (var g in new[] { HardwareGroups.Processor, HardwareGroups.Graphics, HardwareGroups.Storage, HardwareGroups.Motherboard, HardwareGroups.Memory, HardwareGroups.Battery, HardwareGroups.Network, HardwareGroups.Other, "处理器", "显卡", "存储", "主板", "内存", "电池", "网络", "其他" })
         {
             var temps = sensors.Where(s => s.Group == g && s.Kind == SensorKind.Temperature).ToList();
@@ -131,4 +134,4 @@
         // 兜底：若前置逻辑未抓到 CPU 温度，强制放回顶部
-        if (!result.Any(s => s.Group == "处理器" && s.Kind == SensorKind.Temperature))
-            result.InsertRange(0, sensors.Where(s => s.Group == "处理器" && s.Kind == SensorKind.Temperature));
+        if (!result.Any(s => (s.Group == HardwareGroups.Processor || s.Group == "处理器") && s.Kind == SensorKind.Temperature))
+            result.InsertRange(0, sensors.Where(s => (s.Group == HardwareGroups.Processor || s.Group == "处理器") && s.Kind == SensorKind.Temperature));
 
@@ -164,3 +167,3 @@
         item.Subtitle = isStale
-            ? $"{sensor.ShortLabel} · {sensor.Name} · 可能过期"
+            ? $"{sensor.ShortLabel} · {sensor.Name}{Loc.Current.StaleSuffix}"
             : $"{sensor.ShortLabel} · {sensor.Name}";
@@ -207,5 +210,5 @@
             new CommandContextItem(new TogglePinCommand(_hub.Pinned, sensor.Id))
             {
-                Title = "固定到快捷栏 / 取消固定",
+                Title = Loc.Current.PinToggleName,
             },
             new CommandContextItem(new SensorChartPage(_hub, sensor.Id))
             {
-                Title = "查看趋势图",
+                Title = Loc.Current.ViewChartName,
             },
@@ -225,3 +228,3 @@
-        _footerItem.Title = $"{snap.Sensors.Count} 项 · 温度 {tempCount} · 已固定 {pinCount} · 峰值 {Math.Round(peak)}°C{ThresholdPolicy.Marker(level)}";
+        _footerItem.Title = Loc.Current.ListFooter(snap.Sensors.Count, tempCount, pinCount, peak, ThresholdPolicy.Marker(level));
         _footerItem.Subtitle = _hub.BackendLabel;
```

---

### 12. Mod: `HardwareMonitorExtension/Pages/ListCommands.cs`
```diff
--- HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Pages/ListCommands.cs
+++ HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Pages/ListCommands.cs
@@ -4,2 +4,3 @@
 using HardwareMonitorExtension.Services;
+using HardwareMonitorExtension.Localization;
 using Microsoft.CommandPalette.Extensions;
@@ -17,3 +18,3 @@
     {
-        Name = "切换紧凑布局";
+        Name = Loc.Current.ToggleCompactName;
         Icon = new IconInfo("🗜");
@@ -38,3 +39,3 @@
     {
-        Name = "固定到快捷栏 / 取消固定";
+        Name = Loc.Current.PinToggleName;
         Icon = new IconInfo("📌");
@@ -54,3 +55,3 @@
     {
-        Name = "打开实时图表看板";
+        Name = Loc.Current.OpenDashboardTitle;
         Icon = new IconInfo("🌐");
@@ -70,3 +71,3 @@
     {
-        Name = "安装 CPU 温度开机采样（一次）";
+        Name = Loc.Current.InstallHelperTitle;
         Icon = new IconInfo("🔐");
```

---

### 13. Mod: `HardwareMonitorExtension/Pages/SensorChartPage.cs`
```diff
--- HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Pages/SensorChartPage.cs
+++ HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Pages/SensorChartPage.cs
@@ -7,2 +7,3 @@
 using HardwareMonitorExtension.Services;
+using HardwareMonitorExtension.Localization;
 using Microsoft.CommandPalette.Extensions;
@@ -24,5 +25,5 @@
         Icon = new IconInfo("📈");
-        Title = "读数详情";
-        Name = "查看";
+        Title = Loc.Current.ChartPageTitle;
+        Name = Loc.Current.ChartPageAction;
         Commands =
         [
             new CommandContextItem(new OpenDashboardCommand())
             {
-                Title = "打开实时图表看板",
+                Title = Loc.Current.OpenDashboardTitle,
                 Icon = new IconInfo("🌐"),
@@ -64,3 +65,3 @@
         if (sensor is null)
-            return [new MarkdownContent($"# 未找到读数\n\n`{_sensorId}` 不在当前快照中。")];
+            return [new MarkdownContent(Loc.Current.NotFoundMarkdown(_sensorId))];
 
@@ -73,6 +74,6 @@
-        sb.AppendLine($"- 来源：`{sensor.Source}`");
-        sb.AppendLine($"- 硬件：{sensor.HardwareName} · 分组：{sensor.Group}");
-        sb.AppendLine($"- 历史点：{series.Count}/180");
+        sb.AppendLine($"- {Loc.Current.SourceLabel} `{sensor.Source}`");
+        sb.AppendLine($"- {Loc.Current.HardwareLabel} {sensor.HardwareName} · {Loc.Current.GroupLabel} {sensor.LocalizedGroup}");
+        sb.AppendLine($"- {Loc.Current.HistoryPointsLabel} {series.Count}/180");
         sb.AppendLine();
 
         if (series.Count < 2)
         {
-            sb.AppendLine("_采样中，稍候即可看到趋势…_");
+            sb.AppendLine(Loc.Current.SamplingNotice);
             return [new MarkdownContent(sb.ToString())];
         }
@@ -120,3 +121,3 @@
-        sb.AppendLine("## 趋势（asciichart · 代码绘制，非位图）");
+        sb.AppendLine(Loc.Current.TrendHeading);
         sb.AppendLine();
         sb.AppendLine("```");
@@ -127,6 +128,6 @@
         sb.AppendLine("```");
         sb.AppendLine();
-        sb.AppendLine($"当前 **{sensor.DisplayValue}** · 单位 `{sensor.Unit}` · 窗口约 3 分钟 · 点 {sampled.Length}/{series.Count}");
-        sb.AppendLine($"阈值：偏热 {ThresholdPolicy.WarmC:0}° · 过热 {ThresholdPolicy.HotC:0}° · 危险 {ThresholdPolicy.CriticalC:0}°");
+        sb.AppendLine(Loc.Current.ChartMeta(sensor.DisplayValue, sensor.Unit, 3, sampled.Length, series.Count));
+        sb.AppendLine(Loc.Current.Thresholds(ThresholdPolicy.WarmC, ThresholdPolicy.HotC, ThresholdPolicy.CriticalC));
         sb.AppendLine();
-        sb.AppendLine("## 近期采样");
+        sb.AppendLine(Loc.Current.RecentSamplesHeading);
         sb.AppendLine();
-        sb.AppendLine("| 时间 | 值 |");
+        sb.AppendLine($"| {Loc.Current.TableHeaderTime} | {Loc.Current.TableHeaderValue} |");
         sb.AppendLine("| :--- | :--- |");
```

---

### 14. Mod: `HardwareMonitorExtension/Services/ChartRenderer.cs`
```diff
--- HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Services/ChartRenderer.cs
+++ HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Services/ChartRenderer.cs
@@ -7,2 +7,3 @@
 using HardwareMonitorExtension.Services;
+using HardwareMonitorExtension.Localization;
 
@@ -34,3 +35,3 @@
             using var emptyFont = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
-            g.DrawString("采样点不足，正在积累…", emptyFont, Brushes.Gold, 16, 16);
+            g.DrawString(Loc.Current.ChartNotEnoughPoints, emptyFont, Brushes.Gold, 16, 16);
             return EncodePng(bmp);
@@ -78,4 +79,4 @@
         if (isTemperature)
         {
-            DrawThreshold(g, ThresholdPolicy.HotC, minV, maxV, padL, padT, plotW, plotH, Color.FromArgb(255, 140, 66), "过热");
-            DrawThreshold(g, ThresholdPolicy.CriticalC, minV, maxV, padL, padT, plotW, plotH, Color.FromArgb(255, 90, 90), "危险");
+            DrawThreshold(g, ThresholdPolicy.HotC, minV, maxV, padL, padT, plotW, plotH, Color.FromArgb(255, 140, 66), Loc.Current.ChartHotLabel);
+            DrawThreshold(g, ThresholdPolicy.CriticalC, minV, maxV, padL, padT, plotW, plotH, Color.FromArgb(255, 90, 90), Loc.Current.ChartCritLabel);
         }
@@ -120,3 +121,3 @@
-        g.DrawString($"当前 {last:0}{unit}  ·  区间 {minV:0}–{maxV:0}  ·  点数 {points.Count}",
+        g.DrawString(Loc.Current.ChartRange(last, unit, minV, maxV, points.Count),
             labelFont, Brushes.Gainsboro, padL, height - padB + 6);
```

---

### 15. Mod: `HardwareMonitorExtension/Services/DashboardHost.cs`
```diff
--- HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Services/DashboardHost.cs
+++ HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Services/DashboardHost.cs
@@ -81,6 +81,6 @@
-        <!DOCTYPE html>
-        <html lang="zh-CN"><head><meta charset="utf-8"><title>Hardware Monitor</title>
+        <!DOCTYPE html>
+        <html lang="en"><head><meta charset="utf-8"><title>Hardware Monitor</title>
         <style>body{font:14px/1.5 Segoe UI,sans-serif;background:#0b0d10;color:#e8eef7;padding:24px}
         </style></head><body>
         <h1>Hardware Monitor</h1>
-        <p>完整看板未随包释放，请将项目中的 <code>index.html</code> 复制到本目录。</p>
+        <p>Full dashboard assets not extracted. Please copy <code>index.html</code> from the project to this folder.</p>
         </body></html>
```

---

### 16. Mod: `HardwareMonitorExtension/Assets/index.html`
```diff
--- HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Assets/index.html
+++ HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Assets/index.html
@@ -1,3 +1,3 @@
 <!DOCTYPE html>
-<html lang="zh-CN">
+<html lang="en">
 <head>
@@ -14,2 +14,2 @@
-        <p class="meta" id="metaLine">实时采样 · 准备中…</p>
+        <p class="meta" id="metaLine" data-i18n="metaInit">Real-time sampling · Preparing...</p>
       </div>
@@ -17,2 +17,8 @@
       <div class="top-actions">
+        <div class="lang-dropdown" id="langDropdown">
+          <button type="button" class="btn lang-btn" id="langBtn" aria-haspopup="true" aria-expanded="false">
+            🌐 <span id="currentLangLabel">English (US)</span> <span class="arrow">▾</span>
+          </button>
+          <div class="dropdown-menu" id="langMenu" role="menu"></div>
+        </div>
         <div class="seg" id="sizeSeg">
@@ -21,2 +27,2 @@
-        <button type="button" class="btn" id="stressBtn">压测</button>
-        <button type="button" class="btn" id="pauseBtn">暂停</button>
+        <button type="button" class="btn" id="stressBtn" data-i18n="stress">Stress</button>
+        <button type="button" class="btn" id="pauseBtn" data-i18n="pause">Pause</button>
@@ -31,2 +37,2 @@
-          <h2>命令面板 · Dock</h2>
+          <h2 data-i18n="dockTitle">Command Palette · Dock</h2>
           <span id="dockMode">Default</span>
@@ -38,2 +44,2 @@
-          <h2>全部读数</h2>
+          <h2 data-i18n="allSensors">All Sensors</h2>
           <span id="listCount">0</span>
@@ -66,2 +72,2 @@
-            <header><h3>存储</h3><span id="diskNow">—</span></header>
+            <header><h3 data-i18n="groupStorage">Storage</h3><span id="diskNow">—</span></header>
             <canvas id="diskMini" width="360" height="110"></canvas>
@@ -71,2 +77,2 @@
-            <header><h3>主板 / 风扇 / 内存</h3><span id="miscNow">—</span></header>
+            <header><h3 data-i18n="groupMisc">Motherboard / Fan / Memory</h3><span id="miscNow">—</span></header>
             <canvas id="miscMini" width="360" height="110"></canvas>
@@ -81,3 +87,3 @@
-        <h2>明细表</h2>
-        <span id="tableFilterNote">点击行可切换主图</span>
+        <h2 data-i18n="detailedMetrics">Detailed Metrics</h2>
+        <span id="tableFilterNote" data-i18n="tableNote">Click row to switch primary chart</span>
       </div>
@@ -88,3 +94,3 @@
-              <th>指标</th><th>分组</th><th>类型</th><th>当前值</th><th>来源</th><th></th>
+              <th data-i18n="thSensor">Sensor</th><th data-i18n="thGroup">Group</th><th data-i18n="thKind">Kind</th><th data-i18n="thCurrent">Current</th><th data-i18n="thSource">Source</th><th></th>
             </tr>
```

---

### 17. Mod: `HardwareMonitorExtension/Assets/styles.css`
```diff
--- HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Assets/styles.css
+++ HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Assets/styles.css
@@ -61,2 +61,31 @@
 .btn.on { color: var(--accent); border-color: rgba(90,209,255,.4); background: rgba(90,209,255,.08); }
 
+/* Dynamic Language Selector Dropdown */
+.lang-dropdown { position: relative; display: inline-block; }
+.lang-btn { display: inline-flex; align-items: center; gap: 6px; }
+.lang-btn .arrow { font-size: 10px; color: var(--muted); transition: transform .2s ease; }
+.lang-dropdown.open .lang-btn .arrow { transform: rotate(180deg); }
+.dropdown-menu {
+  display: none;
+  position: absolute;
+  right: 0;
+  top: calc(100% + 6px);
+  background: var(--panel);
+  border: 1px solid var(--stroke);
+  border-radius: 10px;
+  box-shadow: 0 12px 32px rgba(0,0,0,0.65);
+  z-index: 200;
+  min-width: 260px;
+  padding: 6px;
+  backdrop-filter: blur(8px);
+}
+.lang-dropdown.open .dropdown-menu { display: block; }
+.dropdown-item {
+  display: flex;
+  justify-content: space-between;
+  align-items: center;
+  padding: 8px 12px;
+  border-radius: 6px;
+  cursor: pointer;
+  font-size: 12px;
+  transition: background .15s ease;
+}
+.dropdown-item:hover { background: rgba(90,209,255,.12); }
+.dropdown-item.active { background: rgba(90,209,255,.08); color: var(--accent); }
+.opt-native { font-weight: 600; color: var(--ink); }
+.opt-desc { color: var(--muted); font-size: 11px; margin-left: 12px; }
+.dropdown-item:hover .opt-desc { color: var(--accent); }
```

---

### 18. Mod: `HardwareMonitorExtension/Assets/app.js`
Full replacement incorporating the `LOCALES` registry, browser language auto-detection, dynamic perspective hover switching, dynamic KPI titles, and bi-directional group normalization.

---

### Review & Sign-Off Request

All changes are strictly contained within `HardwareMonitorCmdPal.Localized`. The pristine reference folder `HardwareMonitorCmdPal.Original` remains 100% untouched.
The plan is compiled and documented in `ComprehensiveChangeOverview.md`.
