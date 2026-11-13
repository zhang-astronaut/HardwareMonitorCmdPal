# Drive and Motherboard Refinements

**Company**: MyronTech Solutions  
**Creator**: Nathan Myron  
**Attribution**: Nathan Myron (with assistance from Gemini via Google Antigravity)  

---

### 1. Executive Summary & Problem Analysis

| Issue | Root Cause | Proposed Solution |
| :--- | :--- | :--- |
| **Motherboard Temp & Fans show `—`** | In LibreHardwareMonitor, motherboard diodes (System, Chipset, VRM) and fan headers belong to child chip hardware classified as **`HardwareType.SuperIO`** (or `EmbeddedController`). In `TempHelperServer.cs` and `SensorService.cs`, the switch only matched `HardwareType.Motherboard`, causing SuperIO to evaluate to `null` and get discarded. | Include `HardwareType.SuperIO` and `HardwareType.EmbeddedController` under `HardwareGroups.Motherboard` so SuperIO temperatures and fans are fully captured. |
| **Storage Shows "Square Wave" Artifacts** | The Storage mini-chart was plotting temperatures (~`50°C`–`53°C`), space used (`82%`), and dynamic I/O activity bursts (`0%`–`8%`) all on the **same Y-axis**. | Filter the Storage mini-graph to plot **temperatures only** (`SSD 0 Temp`, `SSD 1 Temp`), giving smooth, artifact-free history curves. |
| **Metrics Labeled Identically as "SSD Temp"** | `TempHelperServer.cs` hardcoded `shortLabel = "SSD Temp"` for every storage sensor, so used space and activity were labeled "SSD Temp". | Differentiate short labels into: `Temp`, `Used`, `Free`, `Act`, `Read`, and `Write`. Synthesize `SSD {i} Free` (`100% - Used%`) when the drive reports Used Space. |
| **Missing Drive Grouping & Drive Letters** | All storage sensors appeared in a flat list with no visual distinction between physical drives. | Query WMI for drive letters (`Win32_DiskDrive` → `Win32_LogicalDisk`) and render clean section headers grouping each drive's metrics: <br>`SSD 0 - WDS500G3XHC-00SJG0 - Drive C`<br>`SSD 1 - WDS200T3XHC-00SJG0 - Drive D` |

---

### 2. Comprehensive Code Diffs

#### A. `HardwareMonitorExtension/Services/TempHelperServer.cs`
*Adds WMI drive letter mapping, ordinal drive extraction, SuperIO inclusion, storage metric differentiation, and automatic `SSD Free` synthesis.*

```diff
--- a/HardwareMonitorExtension/Services/TempHelperServer.cs
+++ b/HardwareMonitorExtension/Services/TempHelperServer.cs
@@ -3,6 +3,7 @@
 using System.IO.Pipes;
+using System.Management;
 using System.Text.Json;
 using LibreHardwareMonitor.Hardware;
 
 namespace HardwareMonitorExtension.Services;
@@ -30,6 +31,44 @@ public static class TempHelperServer
+    private static readonly Dictionary<int, string> DriveLetters = BuildDriveLetterMap();
+
+    private static Dictionary<int, string> BuildDriveLetterMap()
+    {
+        var map = new Dictionary<int, string>();
+        try
+        {
+            using var searcher = new ManagementObjectSearcher("SELECT DeviceID, Index FROM Win32_DiskDrive");
+            foreach (ManagementObject disk in searcher.Get())
+            {
+                if (disk["Index"] is null) continue;
+                var index = Convert.ToInt32(disk["Index"]);
+                var diskId = disk["DeviceID"]?.ToString() ?? "";
+                if (string.IsNullOrEmpty(diskId)) continue;
+
+                using var partSearcher = new ManagementObjectSearcher(
+                    $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{diskId.Replace("\\", "\\\\")}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition");
+                var letters = new List<string>();
+                foreach (ManagementObject part in partSearcher.Get())
+                {
+                    var partId = part["DeviceID"]?.ToString() ?? "";
+                    if (string.IsNullOrEmpty(partId)) continue;
+                    using var logSearcher = new ManagementObjectSearcher(
+                        $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partId.Replace("\\", "\\\\")}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
+                    foreach (ManagementObject log in logSearcher.Get())
+                    {
+                        var dev = log["DeviceID"]?.ToString();
+                        if (!string.IsNullOrEmpty(dev))
+                            letters.Add(dev.TrimEnd(':'));
+                    }
+                }
+                if (letters.Count > 0)
+                    map[index] = letters.Count == 1 ? $"Drive {letters[0]}" : $"Drives {string.Join("/", letters)}";
+            }
+        }
+        catch { }
+        return map;
+    }
+
+    private static int ExtractDriveIndex(IHardware hw)
+    {
+        var parts = hw.Identifier.ToString().Split('/', StringSplitOptions.RemoveEmptyEntries);
+        return parts.Length >= 2 && int.TryParse(parts[1], out var idx) ? idx : 0;
+    }
@@ -178,3 +217,3 @@ public static class TempHelperServer
                 HardwareType.Storage => HardwareGroups.Storage,
-                HardwareType.Motherboard => HardwareGroups.Motherboard,
+                HardwareType.Motherboard or HardwareType.SuperIO or HardwareType.EmbeddedController => HardwareGroups.Motherboard,
                 HardwareType.Memory => HardwareGroups.Memory,
@@ -216,4 +255,27 @@ public static class TempHelperServer
+                var driveIdx = hw.HardwareType == HardwareType.Storage ? ExtractDriveIndex(hw) : 0;
+                var driveLetter = hw.HardwareType == HardwareType.Storage && DriveLetters.TryGetValue(driveIdx, out var dl) ? $" - {dl}" : "";
+                var hwName = hw.HardwareType == HardwareType.Storage ? $"SSD {driveIdx} - {hw.Name}{driveLetter}" : hw.Name;
+
                 var shortLabel = hw.HardwareType switch
                 {
                     HardwareType.Cpu => s.Name.Contains("Tctl") ? "CPU Tctl" : s.Name.Contains("Package") ? "CPU Pkg" : "CPU",
-                    HardwareType.Storage => "SSD Temp",
+                    HardwareType.Storage => kind switch
+                    {
+                        "temperature" => $"SSD {driveIdx} Temp",
+                        "load" when s.Name.Contains("Used", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Used",
+                        "load" when s.Name.Contains("Free", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Free",
+                        "load" when s.Name.Contains("Activity", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Act",
+                        "load" when s.Name.Contains("Read", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Read",
+                        "load" when s.Name.Contains("Write", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Write",
+                        _ => $"SSD {driveIdx} {(s.Name.Length > 8 ? s.Name[..7] + "…" : s.Name)}",
+                    },
                     HardwareType.Motherboard => s.Name.Length > 12 ? s.Name[..11] + "…" : s.Name,
@@ -236,3 +298,3 @@ public static class TempHelperServer
-                    HardwareName = hw.Name,
+                    HardwareName = hwName,
                     ShortLabel = shortLabel,
                 });
+
+                // Automatically synthesize SSD Free Space when Used Space is reported
+                if (hw.HardwareType == HardwareType.Storage && kind == "load" && s.Name.Contains("Used", StringComparison.OrdinalIgnoreCase))
+                {
+                    var freeVal = Math.Clamp(100.0 - v, 0, 100);
+                    results.Add(new ElevatedSensorDto
+                    {
+                        Id = $"{s.Identifier}-free",
+                        Group = group,
+                        Name = $"{hw.Name} · Free Space",
+                        Source = $"{hw.HardwareType} / Load · Elevated",
+                        Kind = "load",
+                        Value = Math.Round(freeVal, 1),
+                        Unit = "%",
+                        HardwareName = hwName,
+                        ShortLabel = $"SSD {driveIdx} Free",
                     });
                 }
```

---

#### B. `HardwareMonitorExtension/Services/SensorService.cs`
*Aligns `MapGroup` to recognize `SuperIO` and `EmbeddedController` under Motherboard.*

```diff
--- a/HardwareMonitorExtension/Services/SensorService.cs
+++ b/HardwareMonitorExtension/Services/SensorService.cs
@@ -453,3 +453,3 @@ public sealed class SensorService : IDisposable
         HardwareType.Storage => HardwareGroups.Storage,
-        HardwareType.Motherboard => HardwareGroups.Motherboard,
+        HardwareType.Motherboard or HardwareType.SuperIO or HardwareType.EmbeddedController => HardwareGroups.Motherboard,
         HardwareType.Memory => HardwareGroups.Memory,
```

---

#### C. `HardwareMonitorExtension/Services/LiveSnapshotWriter.cs`
*Serializes `hardware` name (holding `"SSD 0 - Model - Drive C"`) to `live-data.js`.*

```diff
--- a/HardwareMonitorExtension/Services/LiveSnapshotWriter.cs
+++ b/HardwareMonitorExtension/Services/LiveSnapshotWriter.cs
@@ -126,2 +126,3 @@ public static class LiveSnapshotWriter
                     name = s.Name,
+                    hardware = s.HardwareName,
                     @short = s.ShortLabel,
```

---

#### D. `HardwareMonitorExtension/Assets/styles.css`
*Adds subtle section header divider styling matching the btop dark theme.*

```diff
--- a/HardwareMonitorExtension/Assets/styles.css
+++ b/HardwareMonitorExtension/Assets/styles.css
@@ -218,2 +218,10 @@
+.sub-hdr {
+  font-size: 11px;
+  font-weight: 600;
+  color: var(--accent);
+  padding: 5px 6px 2px;
+  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
+  margin-top: 4px;
+}
```

---

#### E. `HardwareMonitorExtension/Assets/app.js`
*Renders Storage metrics grouped by their drive section header, and keeps the mini-graph focused on drive temperatures.*

```diff
--- a/HardwareMonitorExtension/Assets/app.js
+++ b/HardwareMonitorExtension/Assets/app.js
@@ -747,4 +747,21 @@
-    const diskItems = pickByGroup("Storage").slice(0, 6);
-    drawMini("diskMini", diskItems.map((s) => s.id));
-    fillRows("diskRows", diskItems);
+    const storageTemps = pickByGroup("Storage", "temperature");
+    const storageAll = pickByGroup("Storage");
+    drawMini("diskMini", (storageTemps.length > 0 ? storageTemps : storageAll).map((s) => s.id));
+
+    // Group storage items under their drive headers
+    const driveGroups = new Map();
+    for (const s of storageAll) {
+      const hdr = s.hardware || "SSD";
+      if (!driveGroups.has(hdr)) driveGroups.set(hdr, []);
+      driveGroups.get(hdr).push(s);
+    }
+    let diskHtml = "";
+    for (const [hdr, items] of driveGroups.entries()) {
+      diskHtml += `<div class="sub-hdr">${hdr}</div>`;
+      for (const s of items) {
+        diskHtml += `<div class="r"><span>${s.short}</span><span>${fmt(s)}</span></div>`;
+      }
+    }
+    $("diskRows").innerHTML = diskHtml;
```
