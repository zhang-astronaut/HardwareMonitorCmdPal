# Localization & Telemetry Sensor Updates

**Creator**: Nathan Myron  
**Organization**: MyronTech Solutions  
**Attribution**: Nathan Myron (with assistance from Gemini via Google Antigravity)  

---

## Part 1: Unified Matrix of Sensor Groups (All 3 Languages)

| Canonical Constant | English (`en-US`) | Simplified Chinese (`zh-CN`) | Japanese (`ja-JP`) | Sort Rank | Dock Tag |
| :--- | :--- | :--- | :--- | :---: | :---: |
| `HardwareGroups.Processor` | `Processor` | `处理器` | `プロセッサ` | **0** | `C` |
| `HardwareGroups.Graphics` | `Graphics` | `显卡` | `グラフィックス` | **1** | `G` |
| `HardwareGroups.Storage` | `Storage` | `存储` | `ストレージ` | **2** | `S` |
| `HardwareGroups.Motherboard` | `Motherboard` | `主板` | `マザーボード` | **3** | `M` |
| `HardwareGroups.Memory` | `Memory` | `内存` | `メモリ` | **4** | `R` |
| `HardwareGroups.Battery` | `Battery` | `电池` | `バッテリー` | **5** | `B` |
| `HardwareGroups.Network` | `Network` | `网络` | `ネットワーク` | **6** | `N` |
| `HardwareGroups.Other` | `Other` | `其他` | `その他` | **99** | `#` |

---

## Part 2: Unified Code Diffs

### 1. `HardwareMonitorExtension/Services/SensorModels.cs`
*Centralizes group recognition, sorting rank, and compact Dock tags across all 3 languages.*

```diff
--- a/HardwareMonitorExtension/Services/SensorModels.cs
+++ b/HardwareMonitorExtension/Services/SensorModels.cs
@@ -16,6 +16,42 @@ public static class HardwareGroups
     public const string Battery = "Battery";
     public const string Network = "Network";
     public const string Other = "Other";
+
+    public static bool IsProcessor(string? group) =>
+        group is Processor or "处理器" or "プロセッサ";
+
+    public static bool IsGraphics(string? group) =>
+        group is Graphics or "显卡" or "グラフィックス";
+
+    public static bool IsStorage(string? group) =>
+        group is Storage or "存储" or "ストレージ";
+
+    public static bool IsMotherboard(string? group) =>
+        group is Motherboard or "主板" or "マザーボード";
+
+    public static bool IsMemory(string? group) =>
+        group is Memory or "内存" or "メモリ";
+
+    public static bool IsBattery(string? group) =>
+        group is Battery or "电池" or "バッテリー";
+
+    public static bool IsNetwork(string? group) =>
+        group is Network or "网络" or "ネットワーク";
+
+    /// <summary>排序权重：CPU > GPU > 存储 > 主板 > 内存 > 电池 > 网络 > 其他</summary>
+    /// <summary>Sorting rank: CPU > GPU > Storage > Motherboard > Memory > Battery > Network > Other</summary>
+    /// <summary>ソート優先度：CPU > GPU > ストレージ > マザーボード > メモリ > バッテリー > ネットワーク > その他</summary>
+    public static int Rank(string? group) => group switch
+    {
+        _ when IsProcessor(group) => 0,
+        _ when IsGraphics(group) => 1,
+        _ when IsStorage(group) => 2,
+        _ when IsMotherboard(group) => 3,
+        _ when IsMemory(group) => 4,
+        _ when IsBattery(group) => 5,
+        _ when IsNetwork(group) => 6,
+        _ => 99,
+    };
 }
 
@@ -112,13 +148,13 @@ public sealed record SensorReading(
     public string FormatDockCompactTitle(bool mergeLoad)
     {
         var prefix = Group switch
         {
-            HardwareGroups.Processor or "处理器" or "プロセッサ" => "C",
-            HardwareGroups.Graphics or "显卡" or "グラフィックス" => "G",
-            HardwareGroups.Storage or "存储" or "ストレージ" => "S",
-            HardwareGroups.Motherboard or "主板" or "マザーボード" => "M",
-            HardwareGroups.Memory or "内存" or "メモリ" => "R",
-            HardwareGroups.Battery or "电池" or "バッテリー" => "B",
-            HardwareGroups.Network or "网络" or "ネットワーク" => "N",
+            _ when HardwareGroups.IsProcessor(Group) => "C",
+            _ when HardwareGroups.IsGraphics(Group) => "G",
+            _ when HardwareGroups.IsStorage(Group) => "S",
+            _ when HardwareGroups.IsMotherboard(Group) => "M",
+            _ when HardwareGroups.IsMemory(Group) => "R",
+            _ when HardwareGroups.IsBattery(Group) => "B",
+            _ when HardwareGroups.IsNetwork(Group) => "N",
             _ => "#",
         };
         return $"{prefix} {FormatDockTitle(mergeLoad)}";
     }
```

---

### 2. `HardwareMonitorExtension/SensorHub.cs`
*Completes trilingual XML `<summary>` and inline comments.*

```diff
--- a/HardwareMonitorExtension/SensorHub.cs
+++ b/HardwareMonitorExtension/SensorHub.cs
@@ -7,6 +7,12 @@ namespace HardwareMonitorExtension;
 /// <summary>
 /// 进程内单例采样枢纽：列表页、图表页、Dock 带共享快照 / 历史 / 固定列表。
 /// </summary>
+/// <summary>
+/// In-process singleton sampling hub: list page, chart page, and Dock band share snapshot / history / pinned list.
+/// </summary>
+/// <summary>
+/// プロセス内シングルトン サンプリング ハブ：リスト ページ、グラフ ページ、Dock バンドでスナップショット / 履歴 / 固定リストを共有。
+/// </summary>
 public sealed partial class SensorHub : IDisposable
 {
@@ -70,2 +76,4 @@ public sealed partial class SensorHub : IDisposable
                 // live-data.js 仅在看板活跃时写入（见 LiveSnapshotWriter.IsDashboardActive）
+                // live-data.js is only written when dashboard is active (see LiveSnapshotWriter.IsDashboardActive)
+                // live-data.js はダッシュボードがアクティブな時のみ書き込み（LiveSnapshotWriter.IsDashboardActive 参照）
                 LiveSnapshotWriter.Write(snap, History);
```

---

### 3. `HardwareMonitorExtension/Services/PinnedSensorStore.cs`
*Refactors manual string comparisons to use `HardwareGroups` methods.*

```diff
--- a/HardwareMonitorExtension/Services/PinnedSensorStore.cs
+++ b/HardwareMonitorExtension/Services/PinnedSensorStore.cs
@@ -139,10 +139,10 @@ public sealed class PinnedSensorStore
-            AddPreferred(list.Where(s => (s.Group == HardwareGroups.Processor || s.Group == "处理器" || s.Group == "プロセッサ") && s.Kind == SensorKind.Temperature)
+            AddPreferred(list.Where(s => HardwareGroups.IsProcessor(s.Group) && s.Kind == SensorKind.Temperature)
                 .OrderByDescending(s => s.ShortLabel.Contains(Loc.Current.ShortLabelPackage, StringComparison.OrdinalIgnoreCase) ||
                                          s.ShortLabel.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
                                          s.ShortLabel.Contains("封装", StringComparison.OrdinalIgnoreCase) ||
                                          s.ShortLabel.Contains("パッケージ", StringComparison.OrdinalIgnoreCase) ||
                                          s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
                                          s.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase)), 1);
-            AddPreferred(list.Where(s => (s.Group == HardwareGroups.Processor || s.Group == "处理器" || s.Group == "プロセッサ") && s.Kind == SensorKind.Load), 1);
-            AddPreferred(list.Where(s => (s.Group == HardwareGroups.Graphics || s.Group == "显卡" || s.Group == "グラフィックス") && s.Kind == SensorKind.Temperature), 1);
-            AddPreferred(list.Where(s => (s.Group == HardwareGroups.Graphics || s.Group == "显卡" || s.Group == "グラフィックス") && s.Kind == SensorKind.Load), 1);
+            AddPreferred(list.Where(s => HardwareGroups.IsProcessor(s.Group) && s.Kind == SensorKind.Load), 1);
+            AddPreferred(list.Where(s => HardwareGroups.IsGraphics(s.Group) && s.Kind == SensorKind.Temperature), 1);
+            AddPreferred(list.Where(s => HardwareGroups.IsGraphics(s.Group) && s.Kind == SensorKind.Load), 1);
```

---

### 4. `HardwareMonitorExtension/Services/ElevatedTempHelper.cs`
*Adds snapshot reader, `WaitForExit(1000)` on `/end` before `/run`, and trilingual DTOs.*

```diff
--- a/HardwareMonitorExtension/Services/ElevatedTempHelper.cs
+++ b/HardwareMonitorExtension/Services/ElevatedTempHelper.cs
@@ -4,6 +4,8 @@
 using System.Diagnostics;
 using System.IO;
 using System.IO.Pipes;
+using System.Text.Json;
 using System.Runtime.InteropServices;
 using Microsoft.CommandPalette.Extensions.Toolkit;
 
@@ -11,8 +13,14 @@ namespace HardwareMonitorExtension.Services;
 
 /// <summary>
-/// CPU Tctl 数据源：优先命名管道（SYSTEM 助手 TempHelperServer，零写盘）；
-/// 失败则读 ProgramData 兜底文件。计划任务的注册/重启也在这里。
+/// 提权传感器数据源：优先读取 ProgramData\HardwareMonitorTemp\elevated-sensors.json，
+/// 兼顾命名管道与 cpu-temp.bin 兜底文件。计划任务的注册/重启也在这里。
 /// </summary>
+/// <summary>
+/// Elevated sensor data source: prefers reading ProgramData\HardwareMonitorTemp\elevated-sensors.json,
+/// with fallback to named pipe and cpu-temp.bin. Scheduled task registration and restart are also handled here.
+/// </summary>
+/// <summary>
+/// 昇格センサー データソース：ProgramData\HardwareMonitorTemp\elevated-sensors.json の読み取りを優先し、
+/// 名前付きパイプおよび cpu-temp.bin フォールバック ファイルにも対応。スケジュールされたタスクの登録/再起動もここで処理します。
+/// </summary>
 public static class ElevatedTempHelper
 {
     public const string TaskName = "HardwareMonitorTempHelper";
     public const string PipeName = "HardwareMonitorTemp";
+    private static readonly JsonSerializerOptions JsonReadOpts = new()
     {
         PropertyNameCaseInsensitive = true,
     };
     private static DateTimeOffset _lastRestart = DateTimeOffset.MinValue;
 
@@ -87,6 +95,14 @@ public static class ElevatedTempHelper
         try
         {
+            Process.Start(new ProcessStartInfo
+            {
+                FileName = "schtasks.exe",
+                Arguments = $"/end /tn {TaskName}",
+                UseShellExecute = false,
+                CreateNoWindow = true,
+            })?.WaitForExit(1000);
+
             Process.Start(new ProcessStartInfo
             {
                 FileName = "schtasks.exe",
                 Arguments = $"/run /tn {TaskName}",
                 UseShellExecute = false,
                 CreateNoWindow = true,
             });
         }
         catch { }
     }
 
+    public static ElevatedSnapshotDto? TryReadSnapshot()
+    {
+        try
+        {
+            var file = @"C:\ProgramData\HardwareMonitorTemp\elevated-sensors.json";
+            if (!File.Exists(file)) return null;
+            var json = File.ReadAllText(file);
+            if (string.IsNullOrWhiteSpace(json)) return null;
+            var dto = JsonSerializer.Deserialize<ElevatedSnapshotDto>(json, JsonReadOpts);
+            if (dto is null || dto.TimestampUtcTicks <= 0) return null;
+            var dt = new DateTime(dto.TimestampUtcTicks, DateTimeKind.Utc);
+            if (DateTime.UtcNow - dt > TimeSpan.FromMinutes(5)) return null;
+            return dto;
+        }
+        catch
+        {
+            return null;
+        }
+    }
+
     public static double? TryReadCpuTemperature()
     {
+        var snap = TryReadSnapshot();
+        if (snap is not null && snap.CpuTemp is > 1 and < 150)
+            return snap.CpuTemp;
+
         var t = ReadFromPipe();
         if (t is > 1 and < 150) return t;
         return ReadFromFile();
     }
 }
+
+/// <summary>提权硬件指标快照 DTO</summary>
+/// <summary>Elevated hardware telemetry snapshot DTO</summary>
+/// <summary>昇格ハードウェア測定値スナップショット DTO</summary>
+public sealed class ElevatedSnapshotDto
+{
+    public long TimestampUtcTicks { get; set; }
+    public double CpuTemp { get; set; }
+    public List<ElevatedSensorDto> Sensors { get; set; } = [];
+}
+
+/// <summary>提权单个传感器 DTO</summary>
+/// <summary>Elevated individual sensor DTO</summary>
+/// <summary>昇格個別センサー DTO</summary>
+public sealed class ElevatedSensorDto
+{
+    public string Id { get; set; } = string.Empty;
+    public string Group { get; set; } = string.Empty;
+    public string Name { get; set; } = string.Empty;
+    public string Source { get; set; } = string.Empty;
+    public string Kind { get; set; } = string.Empty;
+    public double Value { get; set; }
+    public string Unit { get; set; } = string.Empty;
+    public string HardwareName { get; set; } = string.Empty;
+    public string ShortLabel { get; set; } = string.Empty;
+}
```

---

### 5. `HardwareMonitorExtension/Services/TempHelperServer.cs`
*Enables CPU, Storage, Motherboard, Memory; implements recursive dual `Update()`; writes `elevated-sensors.json` atomically.*

```diff
--- a/HardwareMonitorExtension/Services/TempHelperServer.cs
+++ b/HardwareMonitorExtension/Services/TempHelperServer.cs
@@ -3,6 +3,7 @@
 using System.IO.Pipes;
+using System.Text.Json;
 using LibreHardwareMonitor.Hardware;
 
 namespace HardwareMonitorExtension.Services;
@@ -46,9 +47,9 @@ public static class TempHelperServer
             {
                 IsCpuEnabled = true,
                 IsGpuEnabled = false,
-                IsStorageEnabled = false,
-                IsMotherboardEnabled = false,
-                IsMemoryEnabled = false,
+                IsStorageEnabled = true,
+                IsMotherboardEnabled = true,
+                IsMemoryEnabled = true,
                 IsNetworkEnabled = false,
                 IsBatteryEnabled = false,
             };
@@ -84,14 +85,24 @@ public static class TempHelperServer
             try
             {
-                var t = SampleCpuTemp(computer);
-                if (t is > 1 and < 150)
+                var (cpuTemp, sensors) = SampleAllSensors(computer);
+                if (cpuTemp is > 1 and < 150)
                 {
-                    lastTemp = t.Value;
+                    lastTemp = cpuTemp.Value;
                     lastTicks = DateTime.UtcNow.Ticks;
                     WriteFallbackFile(lastTemp, lastTicks);
                 }
+                if (sensors.Count > 0)
+                {
+                    WriteSnapshotJson(new ElevatedSnapshotDto
+                    {
+                        TimestampUtcTicks = DateTime.UtcNow.Ticks,
+                        CpuTemp = lastTemp,
+                        Sensors = sensors,
+                    });
+                }
             }
             catch
             {
@@ -145,29 +156,108 @@ public static class TempHelperServer
-    private static double? SampleCpuTemp(Computer? computer)
+    /// <summary>采样全部启用的硬件传感器（CPU、存储 NVMe SMART、主板 SuperIO LPC）</summary>
+    /// <summary>Samples all enabled hardware sensors (CPU, Storage NVMe SMART, Motherboard SuperIO LPC)</summary>
+    /// <summary>有効化されたすべてのハードウェア センサーをサンプリング（CPU、ストレージ NVMe SMART、マザーボード SuperIO LPC）</summary>
+    private static (double? CpuTemp, List<ElevatedSensorDto> Sensors) SampleAllSensors(Computer? computer)
     {
-        if (computer is null) return null;
+        if (computer is null) return (null, []);
 
-        var cpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
-        if (cpu is null) return null;
-
-        cpu.Update();
-
-        var temps = cpu.Sensors
-            .Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue)
-            .Select(s => (Name: s.Name, Value: (double)s.Value!.Value))
-            .Where(s => s.Value > 0 && s.Value < 150)
-            .ToList();
-
-        if (temps.Count == 0) return null;
-
-        var package = temps.FirstOrDefault(s =>
-            s.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
-            s.Name.Contains("Tdie", StringComparison.OrdinalIgnoreCase) ||
-            s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase));
-
-        double? best = package.Name is not null ? package.Value : null;
-        best ??= temps.Max(s => s.Value);
-        return best;
+        var allHw = new List<IHardware>();
+        foreach (var hw in computer.Hardware)
+        {
+            allHw.Add(hw);
+            foreach (var sub in hw.SubHardware)
+            {
+                allHw.Add(sub);
+                foreach (var sub2 in sub.SubHardware)
+                    allHw.Add(sub2);
+            }
+        }
+
+        foreach (var hw in allHw) hw.Update();
+        foreach (var hw in allHw) hw.Update();
+
+        var results = new List<ElevatedSensorDto>();
+        var cpuTemps = new List<(string Name, double Value)>();
+
+        foreach (var hw in allHw)
+        {
+            var group = hw.HardwareType switch
+            {
+                HardwareType.Cpu => HardwareGroups.Processor,
+                HardwareType.Storage => HardwareGroups.Storage,
+                HardwareType.Motherboard => HardwareGroups.Motherboard,
+                HardwareType.Memory => HardwareGroups.Memory,
+                _ => null,
+            };
+            if (group is null) continue;
+
+            foreach (var s in hw.Sensors)
+            {
+                if (!s.Value.HasValue) continue;
+                var v = (double)s.Value.Value;
+                if (double.IsNaN(v) || double.IsInfinity(v)) continue;
+
+                var kind = s.SensorType switch
+                {
+                    SensorType.Temperature => "temperature",
+                    SensorType.Fan => "fan",
+                    SensorType.Load => "load",
+                    _ => null,
+                };
+                if (kind is null) continue;
+
+                if (kind == "temperature" && (v <= 0 || v >= 150)) continue;
+                if (kind == "fan" && (v < 0 || v > 25000)) continue;
+
+                var unit = kind switch
+                {
+                    "temperature" => "°C",
+                    "fan" => "RPM",
+                    "load" => "%",
+                    _ => "",
+                };
+
+                var shortLabel = hw.HardwareType switch
+                {
+                    HardwareType.Cpu => s.Name.Contains("Tctl") ? "CPU Tctl" : s.Name.Contains("Package") ? "CPU Pkg" : "CPU",
+                    HardwareType.Storage => "SSD Temp",
+                    HardwareType.Motherboard => s.Name.Length > 12 ? s.Name[..11] + "…" : s.Name,
+                    _ => s.Name,
+                };
+
+                if (hw.HardwareType == HardwareType.Cpu && kind == "temperature")
+                    cpuTemps.Add((s.Name, v));
+
+                results.Add(new ElevatedSensorDto
+                {
+                    Id = s.Identifier.ToString(),
+                    Group = group,
+                    Name = $"{hw.Name} · {s.Name}",
+                    Source = $"{hw.HardwareType} / {s.SensorType} · Elevated",
+                    Kind = kind,
+                    Value = Math.Round(v, 1),
+                    Unit = unit,
+                    HardwareName = hw.Name,
+                    ShortLabel = shortLabel,
+                });
+            }
+        }
+
+        double? bestCpu = null;
+        if (cpuTemps.Count > 0)
+        {
+            var pkg = cpuTemps.FirstOrDefault(t =>
+                t.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
+                t.Name.Contains("Tdie", StringComparison.OrdinalIgnoreCase) ||
+                t.Name.Contains("Package", StringComparison.OrdinalIgnoreCase));
+            bestCpu = pkg.Name is not null ? pkg.Value : cpuTemps.Max(t => t.Value);
+        }
+
+        return (bestCpu, results);
     }
+
+    private static void WriteSnapshotJson(ElevatedSnapshotDto snapshot)
+    {
+        try
+        {
+            var dir = @"C:\ProgramData\HardwareMonitorTemp";
+            Directory.CreateDirectory(dir);
+            var json = JsonSerializer.Serialize(snapshot);
+            var tmp = Path.Combine(dir, "elevated-sensors.json.tmp");
+            var target = Path.Combine(dir, "elevated-sensors.json");
+            File.WriteAllText(tmp, json);
+            File.Move(tmp, target, overwrite: true);
+        }
+        catch { }
+    }
```

---

### 6. `HardwareMonitorExtension/Services/SensorService.cs`
*Replaces `MergeElevatedCpuTemp` with `MergeElevatedSensors`; replaces all ad-hoc string comparisons with `HardwareGroups.IsProcessor` and `HardwareGroups.Rank`.*

```diff
--- a/HardwareMonitorExtension/Services/SensorService.cs
+++ b/HardwareMonitorExtension/Services/SensorService.cs
@@ -199,3 +199,3 @@ public sealed class SensorService : IDisposable
-        if (!list.Any(s => s.Kind == SensorKind.Load && (s.Group == HardwareGroups.Processor || s.Group == "处理器")))
+        if (!list.Any(s => s.Kind == SensorKind.Load && HardwareGroups.IsProcessor(s.Group)))
@@ -222,4 +222,4 @@ public sealed class SensorService : IDisposable
         FixupCpuPower(list);
-        // CPU 温度：LHM/PawnIO 普通权限为 0 时，合并登录任务助手写入的真实 Tctl
-        MergeElevatedCpuTemp(list);
+        // 提权数据注入（First-Source）：合并 SYSTEM 助手采集的真实 CPU、SSD 与主板读数
+        MergeElevatedSensors(list);
@@ -230,22 +230,68 @@ public sealed class SensorService : IDisposable
-    private static void MergeElevatedCpuTemp(List<SensorReading> list)
+    private static void MergeElevatedSensors(List<SensorReading> list)
     {
-        var elevated = ElevatedTempHelper.TryReadCpuTemperature();
+        var snapshot = ElevatedTempHelper.TryReadSnapshot();
+        var elevatedCpu = snapshot?.CpuTemp ?? ElevatedTempHelper.TryReadCpuTemperature();
 
-        if (elevated is null or < 1 or > 150)
+        // 1. CPU 温度处理（优先采用 SYSTEM 助手的 AMD Tctl/Intel Package 真实读数）
+        if (elevatedCpu is null or < 1 or > 150)
         {
             list.RemoveAll(s =>
                 s.Kind == SensorKind.Temperature &&
-                (s.Group == HardwareGroups.Processor || s.Group == "处理器") &&
+                HardwareGroups.IsProcessor(s.Group) &&
                 s.Value <= 0);
-            return;
         }
-
-        var idx = list.FindIndex(s =>
-            s.Kind == SensorKind.Temperature && (s.Group == HardwareGroups.Processor || s.Group == "处理器") &&
-            (s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
-             s.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
-             s.Name.Contains("Tdie", StringComparison.OrdinalIgnoreCase)));
-        if (idx < 0)
-            idx = list.FindIndex(s =>
-                s.Kind == SensorKind.Temperature && (s.Group == HardwareGroups.Processor || s.Group == "处理器"));
+        else
+        {
+            var idx = list.FindIndex(s =>
+                s.Kind == SensorKind.Temperature && HardwareGroups.IsProcessor(s.Group) &&
+                (s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
+                 s.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
+                 s.Name.Contains("Tdie", StringComparison.OrdinalIgnoreCase)));
+            if (idx < 0)
+                idx = list.FindIndex(s =>
+                    s.Kind == SensorKind.Temperature && HardwareGroups.IsProcessor(s.Group));
+
+            if (idx >= 0)
+            {
+                list[idx] = list[idx] with
+                {
+                    Value = elevatedCpu.Value,
+                    Source = list[idx].Source + " · admin-helper",
+                };
+            }
+            else
+            {
+                list.Add(new SensorReading(
+                    "helper:cpu-tctl",
+                    HardwareGroups.Processor,
+                    Loc.Current.CpuPackageTemp,
+                    "Elevated helper · LHM",
+                    SensorKind.Temperature,
+                    elevatedCpu.Value,
+                    "°C",
+                    "CPU",
+                    Loc.Current.CpuPackage,
+                    null));
+            }
+        }
+
+        // 2. 存储与主板提权传感器注入（解决普通权限下 \\.\PhysicalDrive 与 SuperIO LPC 无法读取的问题）
+        if (snapshot?.Sensors is { Count: > 0 } elevatedSensors)
+        {
+            foreach (var es in elevatedSensors)
+            {
+                if (es.Value <= 0) continue;
+                if (!Enum.TryParse<SensorKind>(es.Kind, ignoreCase: true, out var kind))
+                    continue;
+
+                var existingIdx = list.FindIndex(s => s.Id == es.Id || s.Name == es.Name);
+                if (existingIdx >= 0)
+                {
+                    if (list[existingIdx].Value <= 0)
+                    {
+                        list[existingIdx] = list[existingIdx] with
+                        {
+                            Value = es.Value,
+                            Source = es.Source,
+                        };
+                    }
+                }
+                else
+                {
+                    list.Add(new SensorReading(
+                        Id: es.Id,
+                        Group: es.Group,
+                        Name: es.Name,
+                        Source: es.Source,
+                        Kind: kind,
+                        Value: es.Value,
+                        Unit: es.Unit,
+                        HardwareName: es.HardwareName,
+                        ShortLabel: es.ShortLabel,
+                        MergedLoadText: null));
+                }
+            }
+        }
     }
@@ -287,5 +333,5 @@ public sealed class SensorService : IDisposable
-        var load = list.FirstOrDefault(s => s.Kind == SensorKind.Load && (s.Group == HardwareGroups.Processor || s.Group == "处理器"))?.Value ?? 15.0;
+        var load = list.FirstOrDefault(s => s.Kind == SensorKind.Load && HardwareGroups.IsProcessor(s.Group))?.Value ?? 15.0;
         var cpuPower = list
-            .Where(s => s.Kind == SensorKind.Power && (s.Group == HardwareGroups.Processor || s.Group == "处理器"))
+            .Where(s => s.Kind == SensorKind.Power && HardwareGroups.IsProcessor(s.Group))
             .ToList();
@@ -306,3 +352,3 @@ public sealed class SensorService : IDisposable
-                if (list[i].Kind == SensorKind.Power && (list[i].Group == HardwareGroups.Processor || list[i].Group == "处理器") && list[i].Value <= 0.5)
+                if (list[i].Kind == SensorKind.Power && HardwareGroups.IsProcessor(list[i].Group) && list[i].Value <= 0.5)
@@ -544,11 +590,3 @@ public sealed class SensorService : IDisposable
-        static int GroupRank(string g) => g switch
-        {
-            HardwareGroups.Processor or "处理器" => 0,
-            HardwareGroups.Graphics or "显卡" => 1,
-            HardwareGroups.Storage or "存储" => 2,
-            HardwareGroups.Motherboard or "主板" => 3,
-            HardwareGroups.Memory or "内存" => 4,
-            HardwareGroups.Battery or "电池" => 5,
-            HardwareGroups.Network or "网络" => 6,
-            _ => 99,
-        };
+        static int GroupRank(string g) => HardwareGroups.Rank(g);
```

---

### 7. `HardwareMonitorExtension/Assets/app.js`
*Adds `findMotherboardTemperature` and connects it to the top KPI and mini-panels.*

```diff
--- a/HardwareMonitorExtension/Assets/app.js
+++ b/HardwareMonitorExtension/Assets/app.js
@@ -656,6 +656,19 @@
     return null;
   }
 
+  function findMotherboardTemperature(sensors) {
+    const mbTemps = sensors.filter((s) => canonicalGroup(s.group) === "Motherboard" && s.kind === "temperature" && s.value > 0);
+    if (mbTemps.length === 0) return null;
+    // Prefer System, Motherboard, Mainboard, Chipset over socket thermistors
+    const preferred = mbTemps.find((s) => {
+      const n = (s.name + " " + (s.short || "")).toLowerCase();
+      return n.includes("system") || n.includes("motherboard") || n.includes("mainboard") ||
+             n.includes("chipset") || n.includes("pch") || n.includes("vrm") ||
+             n.includes("主板") || n.includes("マザーボード");
+    });
+    return preferred || mbTemps[0];
+  }
+
   function render() {
     const loc = LOCALES[state.currentLocale] || LOCALES["en-US"];
     const t = loc.strings;
@@ -666,7 +679,7 @@
     const diskT = pick("ssd-temp") || pickByGroup("Storage", "temperature")[0];
     const fan = pick("cpu-fan") || state.sensors.find((s) => s.kind === "fan");
-    const mb = pick("mb-temp") || pickByGroup("Motherboard", "temperature")[0];
+    const mb = pick("mb-temp") || findMotherboardTemperature(state.sensors) || pickByGroup("Motherboard", "temperature")[0];
     const mem = pick("mem-load") || pickByGroup("Memory", "load")[0];
```
