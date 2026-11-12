# Localization - All Sensors - Storage - Graphing Refinements

**Company**: MyronTech Solutions  
**Creator**: Nathan Myron  
**Attribution**: Nathan Myron (with assistance from Gemini via Google Antigravity)  

---

### Executive Summary

Here is the complete, unified implementation plan covering:
1. **Multi-Select Graphing** on the primary chart (via row checkboxes or `Ctrl+Click` / `Shift+Click`).
2. **Comprehensive Sidebar Sorting** (`CPU` → `Memory` → `GPU` → `Disks by ordinal` → `Motherboard` → others).
3. **Read/Write % and MB/s Throughput Telemetry** for Storage.
4. **Win32 Native Drive Letter Resolution** (`- Drive C`, `- Drive D`).
5. **Persistent Metric Caching** (idle `0%` and `0 MB/s` never drop out).

---

### How Multi-Select Graphing Works

- **Dual-Mode Selection**:
  - **Normal Click**: Selects that single sensor for focused graphing.
  - **Checkbox Click or `Ctrl+Click` / `Shift+Click`**: Toggles multiple sensors into/out of the chart.
- **Dynamic Multi-Series Rendering**:
  - Plots each selected sensor with a distinct vibrant color from an 8-color palette (`#00d4ff`, `#ff4d6d`, `#00f5d4`, `#ffbe0b`, `#f72585`, `#7209b7`, `#4cc9f0`, `#52b788`).
  - When comparing compatible metrics (like multiple temperatures), it automatically unifies the Y-axis range so their curves scale proportionately.
- **Multi-Series Legend**:
  - The chart footer displays color swatches, labels, and live readings for every plotted sensor (e.g., `<i style="background:#00d4ff"></i> CPU Tctl (64°C) &nbsp; <i style="background:#ff4d6d"></i> GPU Core (46°C)`).

---

### Detailed Code Diffs

#### 1. `HardwareMonitorExtension/Assets/styles.css`
*Adds styling for the multi-select checkbox on sensor rows.*

```diff
--- a/HardwareMonitorExtension/Assets/styles.css
+++ b/HardwareMonitorExtension/Assets/styles.css
@@ -198,2 +198,24 @@
+.row-chk {
+  width: 14px;
+  height: 14px;
+  border-radius: 3px;
+  border: 1px solid rgba(255, 255, 255, 0.2);
+  margin-right: 8px;
+  flex-shrink: 0;
+  cursor: pointer;
+  display: flex;
+  align-items: center;
+  justify-content: center;
+  transition: all 0.15s ease;
+}
+.row:hover .row-chk { border-color: var(--accent); }
+.row-chk.checked { background: var(--accent); border-color: var(--accent); }
+.row-chk.checked::after {
+  content: "";
+  width: 4px;
+  height: 7px;
+  border: solid #000;
+  border-width: 0 2px 2px 0;
+  transform: rotate(45deg);
+  margin-bottom: 2px;
+}
```

#### 2. `HardwareMonitorExtension/Assets/app.js`
*Implements multi-select state (`state.selectedIds`), multi-series canvas rendering, and the strict hierarchical sensor sorting.*

```diff
--- a/HardwareMonitorExtension/Assets/app.js
+++ b/HardwareMonitorExtension/Assets/app.js
@@ -256,3 +256,3 @@
     selected: null,
+    selectedIds: new Set(["cpu-pkg"]),
     pinned: ["cpu-pkg", "cpu-load", "cpu-ppt", "gpu-core", "ssd-temp", "mem-load"],
@@ -321,2 +321,63 @@
     }
+
+    state.sensors = sortSensors(next);
+    return true;
+  }
+
+  function sortSensors(sensors) {
+    function groupRank(g) {
+      switch (canonicalGroup(g)) {
+        case "Processor": return 0;
+        case "Memory": return 1;
+        case "Graphics": return 2;
+        case "Storage": return 3;
+        case "Motherboard": return 4;
+        case "Battery": return 5;
+        case "Network": return 6;
+        default: return 99;
+      }
+    }
+    function metricRank(s) {
+      const cg = canonicalGroup(s.group);
+      const k = s.kind;
+      const sh = (s.short || s.name || "").toLowerCase();
+      if (cg === "Processor") {
+        if (k === "temperature") return 0;
+        if (k === "load") return 1;
+        if (k === "power") return 2;
+        if (k === "voltage") return 3;
+        if (k === "clock") return 4;
+        return 5;
+      }
+      if (cg === "Memory") {
+        const isVirt = (s.name || "").toLowerCase().includes("virtual");
+        return (isVirt ? 10 : 0) + (k === "load" ? 0 : 1);
+      }
+      if (cg === "Graphics") {
+        if (k === "temperature") {
+          if (sh.includes("core")) return 0;
+          if (sh.includes("hot") || sh.includes("junction")) return 1;
+          if (sh.includes("mem")) return 2;
+          return 3;
+        }
+        if (k === "load") return 10;
+        if (k === "power") return 11;
+        if (k === "voltage") return 12;
+        if (k === "clock") return 13;
+        if (k === "fan") return 14;
+        return 15;
+      }
+      if (cg === "Storage") {
+        if (sh.includes("temp")) return 0;
+        if (sh.includes("act") && !sh.includes("read") && !sh.includes("write")) return 1;
+        if (sh.includes("read act") || (sh.includes("read") && sh.includes("act"))) return 2;
+        if (sh.includes("write act") || (sh.includes("write") && sh.includes("act"))) return 3;
+        if (sh.includes("read rate") || (sh.includes("read") && !sh.includes("act"))) return 4;
+        if (sh.includes("write rate") || (sh.includes("write") && !sh.includes("act"))) return 5;
+        if (sh.includes("used")) return 6;
+        if (sh.includes("free")) return 7;
+        return 8;
+      }
+      if (k === "temperature") return 0;
+      if (k === "fan") return 1;
+      if (k === "load") return 2;
+      if (k === "voltage") return 3;
+      return 4;
+    }
+    function driveOrd(s) {
+      if (canonicalGroup(s.group) !== "Storage") return 0;
+      const text = `${s.hardware || ""} ${s.short || ""} ${s.id || ""}`;
+      const m = text.match(/SSD\s*(\d+)/i) || text.match(/\/(?:nvme|storage)\/(\d+)/i);
+      return m ? parseInt(m[1], 10) : 0;
+    }
+    return sensors.slice().sort((a, b) => {
+      const gDiff = groupRank(a.group) - groupRank(b.group);
+      if (gDiff !== 0) return gDiff;
+      const dDiff = driveOrd(a) - driveOrd(b);
+      if (dDiff !== 0) return dDiff;
+      const mDiff = metricRank(a) - metricRank(b);
+      if (mDiff !== 0) return mDiff;
+      return (a.name || "").localeCompare(b.name || "");
+    });
+  }
@@ -518,13 +579,35 @@
   function drawMain() {
-    const s = state.sensors.find((x) => x.id === state.selected) || state.sensors[0];
-    if (!s) return;
+    const selected = Array.from(state.selectedIds)
+      .map((id) => state.sensors.find((x) => x.id === id))
+      .filter(Boolean);
+    if (selected.length === 0 && state.sensors.length > 0) selected.push(state.sensors[0]);
+    if (selected.length === 0) return;
+
     const g = setupCanvas($("mainChart"), 240);
     grid(g.ctx, g.w, g.h);
-    const color = s.kind === "temperature" ? COLORS.cpu : s.kind === "power" ? COLORS.power : COLORS.load;
-    const forced = stickyRange(s.id, s.history);
-    drawSeries(g.ctx, s.history, color, g.w, g.h, 8, forced);
-    $("chartTitle").textContent = s.name;
-    const mn = Math.min(...s.history), mx = Math.max(...s.history);
-    const loc = LOCALES[state.currentLocale] || LOCALES["en-US"];
-    $("chartStats").textContent = `${fmt(s)} · ${mn.toFixed(0)}–${mx.toFixed(0)} · ${s.history.length} ${loc.strings.points}`;
-    $("chartLegend").innerHTML = `<span><i style="background:${color}"></i>${s.short}</span><span>${s.source}</span>`;
+    const PALETTE = ["#00d4ff", "#ff4d6d", "#00f5d4", "#ffbe0b", "#f72585", "#7209b7", "#4cc9f0", "#52b788"];
+
+    const sameKind = selected.every((s) => s.kind === selected[0].kind);
+    let forced = null;
+    if (sameKind) {
+      const allVals = selected.flatMap((s) => s.history);
+      if (allVals.length > 0) {
+        forced = { min: Math.min(...allVals), max: Math.max(...allVals) };
+        if (forced.max - forced.min < 1) forced.max = forced.min + 1;
+      }
+    }
+
+    selected.forEach((s, i) => {
+      const color = PALETTE[i % PALETTE.length];
+      drawSeries(g.ctx, s.history, color, g.w, g.h, 8, forced || stickyRange(s.id, s.history));
+    });
+
+    if (selected.length === 1) {
+      const s = selected[0];
+      $("chartTitle").textContent = s.name;
+      const mn = Math.min(...s.history), mx = Math.max(...s.history);
+      $("chartStats").textContent = `${fmt(s)} · ${mn.toFixed(0)}–${mx.toFixed(0)} · ${s.history.length} pts`;
+      $("chartLegend").innerHTML = `<span><i style="background:${PALETTE[0]}"></i>${s.short}</span><span>${s.source}</span>`;
+    } else {
+      $("chartTitle").textContent = `${selected.length} Sensors Plotted`;
+      $("chartStats").textContent = selected.map((s) => `${s.short}: ${fmt(s)}`).join(" · ");
+      $("chartLegend").innerHTML = selected.map((s, i) =>
+        `<span><i style="background:${PALETTE[i % PALETTE.length]}"></i>${s.short} (${fmt(s)})</span>`
+      ).join("");
+    }
   }
@@ -544,4 +627,6 @@
   function rowHtml(s) {
     const lv = s.kind === "temperature" ? heat(s.value) : "";
     const grp = localizedGroupName(s.group, state.currentLocale);
-    return `<div class="row ${lv} ${s.id === state.selected ? "sel" : ""}" data-id="${s.id}">
+    const isSel = state.selectedIds.has(s.id);
+    return `<div class="row ${lv} ${isSel ? "sel" : ""}" data-id="${s.id}">
+      <div class="row-chk ${isSel ? "checked" : ""}" data-chk="${s.id}"></div>
       <div>
@@ -983,6 +1068,18 @@
   $("sensorList").addEventListener("click", (e) => {
+    const chk = e.target.closest(".row-chk[data-chk]");
+    if (chk) {
+      const id = chk.dataset.chk;
+      if (state.selectedIds.has(id)) {
+        if (state.selectedIds.size > 1) state.selectedIds.delete(id);
+      } else {
+        state.selectedIds.add(id);
+      }
+      render();
+      return;
+    }
     const row = e.target.closest(".row[data-id]");
     if (!row) return;
-    state.selected = row.dataset.id;
+    const id = row.dataset.id;
+    if (e.ctrlKey || e.metaKey || e.shiftKey) {
+      if (state.selectedIds.has(id)) {
+        if (state.selectedIds.size > 1) state.selectedIds.delete(id);
+      } else {
+        state.selectedIds.add(id);
+      }
+    } else {
+      state.selectedIds = new Set([id]);
+    }
     render();
   });
```

#### 3. `HardwareMonitorExtension/Services/SensorModels.cs`
*Updates `HardwareGroups.Rank` priority.*

```diff
--- a/HardwareMonitorExtension/Services/SensorModels.cs
+++ b/HardwareMonitorExtension/Services/SensorModels.cs
@@ -42,7 +42,7 @@ public static class HardwareGroups
     public static int Rank(string? group) => group switch
     {
         _ when IsProcessor(group) => 0,
-        _ when IsGraphics(group) => 1,
-        _ when IsStorage(group) => 2,
-        _ when IsMotherboard(group) => 3,
-        _ when IsMemory(group) => 4,
+        _ when IsMemory(group) => 1,
+        _ when IsGraphics(group) => 2,
+        _ when IsStorage(group) => 3,
+        _ when IsMotherboard(group) => 4,
         _ when IsBattery(group) => 5,
```

#### 4. `HardwareMonitorExtension/Services/SensorService.cs`
*Implements backend hierarchical sorting and preserves `0%` and `0 MB/s` readings.*

```diff
--- a/HardwareMonitorExtension/Services/SensorService.cs
+++ b/HardwareMonitorExtension/Services/SensorService.cs
@@ -295,4 +295,7 @@ public sealed partial class SensorService : IDisposable
             foreach (var es in elevatedSensors)
             {
-                if (es.Value <= 0) continue;
+                // Discard invalid temperatures (<= 0°C), but 0 is completely valid for activities, throughput, and loads
+                if (es.Kind.Equals("temperature", StringComparison.OrdinalIgnoreCase) && es.Value <= 0) continue;
+                if (es.Value < 0) continue;
 
                 if (!Enum.TryParse<SensorKind>(es.Kind, ignoreCase: true, out var kind))
@@ -303,4 +306,4 @@ public sealed partial class SensorService : IDisposable
                 if (existingIdx >= 0)
                 {
                     list[existingIdx] = list[existingIdx] with
                     {
-                        Value = es.Value > 0 ? es.Value : list[existingIdx].Value,
+                        Value = es.Value,
@@ -576,14 +579,62 @@ public sealed partial class SensorService : IDisposable
     private static List<SensorReading> Sort(List<SensorReading> list)
     {
-        static int KindRank(SensorKind k) => k switch
+        static int MetricRank(SensorReading s)
         {
-            SensorKind.Temperature => 0,
-            SensorKind.Load => 1,
-            SensorKind.Power => 2,
-            SensorKind.Fan => 3,
-            SensorKind.Clock => 4,
-            _ => 5,
-        };
-
-        static int GroupRank(string g) => HardwareGroups.Rank(g);
-
-        return list
-            .OrderBy(s => KindRank(s.Kind))
-            .ThenBy(s => GroupRank(s.Group))
-            .ThenBy(s => s.Name, StringComparer.CurrentCulture)
-            .ToList();
+            var g = s.Group;
+            var k = s.Kind;
+            var n = s.Name;
+            var sh = s.ShortLabel.ToLowerInvariant();
+
+            if (HardwareGroups.IsProcessor(g))
+            {
+                return k switch
+                {
+                    SensorKind.Temperature => 0,
+                    SensorKind.Load => 1,
+                    SensorKind.Power => 2,
+                    SensorKind.Voltage => 3,
+                    SensorKind.Clock => 4,
+                    _ => 5,
+                };
+            }
+            if (HardwareGroups.IsMemory(g))
+            {
+                var isVirtual = n.Contains("Virtual", StringComparison.OrdinalIgnoreCase);
+                var subRank = k == SensorKind.Load ? 0 : 1;
+                return (isVirtual ? 10 : 0) + subRank;
+            }
+            if (HardwareGroups.IsGraphics(g))
+            {
+                if (k == SensorKind.Temperature)
+                {
+                    if (sh.Contains("core")) return 0;
+                    if (sh.Contains("hot") || sh.Contains("junction")) return 1;
+                    if (sh.Contains("mem")) return 2;
+                    return 3;
+                }
+                return k switch
+                {
+                    SensorKind.Load => 10,
+                    SensorKind.Power => 11,
+                    SensorKind.Voltage => 12,
+                    SensorKind.Clock => 13,
+                    SensorKind.Fan => 14,
+                    _ => 15,
+                };
+            }
+            if (HardwareGroups.IsStorage(g))
+            {
+                if (sh.Contains("temp")) return 0;
+                if (sh.Contains("act") && !sh.Contains("read") && !sh.Contains("write")) return 1;
+                if (sh.Contains("read act") || (sh.Contains("read") && sh.Contains("act"))) return 2;
+                if (sh.Contains("write act") || (sh.Contains("write") && sh.Contains("act"))) return 3;
+                if (sh.Contains("read rate") || (sh.Contains("read") && !sh.Contains("act"))) return 4;
+                if (sh.Contains("write rate") || (sh.Contains("write") && !sh.Contains("act"))) return 5;
+                if (sh.Contains("used")) return 6;
+                if (sh.Contains("free")) return 7;
+                return 8;
+            }
+            return k switch
+            {
+                SensorKind.Temperature => 0,
+                SensorKind.Fan => 1,
+                SensorKind.Load => 2,
+                SensorKind.Voltage => 3,
+                _ => 4,
+            };
+        }
+
+        static int DriveOrdinal(SensorReading s)
+        {
+            if (!HardwareGroups.IsStorage(s.Group)) return 0;
+            var text = $"{s.HardwareName} {s.ShortLabel} {s.Id} {s.Name}";
+            var m = System.Text.RegularExpressions.Regex.Match(text, @"(?:SSD|\/(?:nvme|storage)\/)\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
+            return m.Success && int.TryParse(m.Groups[1].Value, out var idx) ? idx : 0;
+        }
+
+        return list
+            .OrderBy(s => HardwareGroups.Rank(s.Group))
+            .ThenBy(DriveOrdinal)
+            .ThenBy(MetricRank)
+            .ThenBy(s => s.Name, StringComparer.CurrentCulture)
+            .ToList();
     }
```

#### 5. `HardwareMonitorExtension/Services/TempHelperServer.cs`
*Adds Win32 `IOCTL_STORAGE_GET_DEVICE_NUMBER` for `- Drive C/D`, persistent sensor caching, and Read/Write throughput + activity capture.*

```diff
--- a/HardwareMonitorExtension/Services/TempHelperServer.cs
+++ b/HardwareMonitorExtension/Services/TempHelperServer.cs
@@ -4,4 +4,6 @@ using System.IO.Pipes;
 using System.Management;
+using System.Runtime.InteropServices;
 using System.Text.Json;
+using Microsoft.Win32.SafeHandles;
 using LibreHardwareMonitor.Hardware;
 
@@ -32,2 +34,20 @@ public static class TempHelperServer
     private static readonly Dictionary<int, string> DriveLetters = BuildDriveLetterMap();
+    private static readonly Dictionary<string, ElevatedSensorDto> LastKnownSensors = new();
+
+    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
+    private static extern SafeFileHandle CreateFile(
+        string lpFileName, uint dwDesiredAccess, uint dwShareMode,
+        IntPtr lpSecurityAttributes, uint dwCreationDisposition,
+        uint dwFlagsAndAttributes, IntPtr hTemplateFile);
+
+    [DllImport("kernel32.dll", SetLastError = true)]
+    private static extern bool DeviceIoControl(
+        SafeFileHandle hDevice, uint dwIoControlCode, IntPtr lpInBuffer,
+        uint nInBufferSize, out STORAGE_DEVICE_NUMBER lpOutBuffer,
+        uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);
+
+    [StructLayout(LayoutKind.Sequential)]
+    private struct STORAGE_DEVICE_NUMBER { public int DeviceType; public int DeviceNumber; public int PartitionNumber; }
+    private const uint IOCTL_STORAGE_GET_DEVICE_NUMBER = 0x002D1080;
+    private const uint OPEN_EXISTING = 3;
 
@@ -37,3 +57,28 @@ public static class TempHelperServer
         var map = new Dictionary<int, string>();
+        var driveLists = new Dictionary<int, List<string>>();
+        try
+        {
+            foreach (var d in DriveInfo.GetDrives())
+            {
+                if (d.DriveType != DriveType.Fixed && d.DriveType != DriveType.Removable) continue;
+                var letter = d.Name.TrimEnd('\\', ':');
+                using var handle = CreateFile($@"\\.\{letter}:", 0, 1 | 2, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
+                if (!handle.IsInvalid && DeviceIoControl(handle, IOCTL_STORAGE_GET_DEVICE_NUMBER, IntPtr.Zero, 0, out var sdn, (uint)Marshal.SizeOf<STORAGE_DEVICE_NUMBER>(), out _, IntPtr.Zero))
+                {
+                    if (!driveLists.TryGetValue(sdn.DeviceNumber, out var list))
+                    {
+                        list = new List<string>();
+                        driveLists[sdn.DeviceNumber] = list;
+                    }
+                    if (!list.Contains(letter)) list.Add(letter);
+                }
+            }
+            foreach (var (idx, letters) in driveLists)
+                map[idx] = letters.Count == 1 ? $"Drive {letters[0]}" : $"Drives {string.Join("/", letters)}";
+        }
+        catch { }
+
+        if (map.Count > 0) return map;
+
         try
@@ -249,2 +294,3 @@ public static class TempHelperServer
                     SensorType.Load => "load",
+                    SensorType.Throughput or SensorType.Data => "data",
                     _ => null,
@@ -262,2 +308,3 @@ public static class TempHelperServer
                     "load" => "%",
+                    "data" => s.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Bytes", StringComparison.OrdinalIgnoreCase) ? "GB" : "MB/s",
                     _ => "",
@@ -271,7 +318,11 @@ public static class TempHelperServer
                         "load" when s.Name.Contains("Used", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Used",
                         "load" when s.Name.Contains("Free", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Free",
-                        "load" when s.Name.Contains("Activity", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Act",
-                        "load" when s.Name.Contains("Read", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Read",
-                        "load" when s.Name.Contains("Write", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Write",
+                        "load" when s.Name.Contains("Read", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Read Act",
+                        "load" when s.Name.Contains("Write", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Write Act",
+                        "load" when s.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Activity", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Act",
+                        "data" when s.Name.Contains("Read", StringComparison.OrdinalIgnoreCase) && (s.Name.Contains("Rate", StringComparison.OrdinalIgnoreCase) || s.SensorType == SensorType.Throughput) => $"SSD {driveIdx} Read Rate",
+                        "data" when s.Name.Contains("Write", StringComparison.OrdinalIgnoreCase) && (s.Name.Contains("Rate", StringComparison.OrdinalIgnoreCase) || s.SensorType == SensorType.Throughput) => $"SSD {driveIdx} Write Rate",
+                        "data" when s.Name.Contains("Read", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Read",
+                        "data" when s.Name.Contains("Write", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Write",
                         _ => $"SSD {driveIdx} {(s.Name.Length > 8 ? s.Name[..7] + "…" : s.Name)}",
@@ -285,13 +336,13 @@ public static class TempHelperServer
-                results.Add(new ElevatedSensorDto
+                var dto = new ElevatedSensorDto
                 {
                     Id = s.Identifier.ToString(),
                     Group = group,
                     Name = $"{hw.Name} · {s.Name}",
                     Source = $"{hw.HardwareType} / {s.SensorType} · Elevated",
                     Kind = kind,
                     Value = Math.Round(v, 1),
                     Unit = unit,
                     HardwareName = hwName,
                     ShortLabel = shortLabel,
-                });
+                };
+                LastKnownSensors[dto.Id] = dto;
 
                 // Automatically synthesize SSD Free Space when Used Space is reported
                 if (hw.HardwareType == HardwareType.Storage && kind == "load" && s.Name.Contains("Used", StringComparison.OrdinalIgnoreCase))
                 {
                     var freeVal = Math.Clamp(100.0 - v, 0, 100);
-                    results.Add(new ElevatedSensorDto
+                    var freeDto = new ElevatedSensorDto
                     {
                         Id = $"{s.Identifier}-free",
                         Group = group,
                         Name = $"{hw.Name} · Free Space",
                         Source = $"{hw.HardwareType} / Load · Elevated",
                         Kind = "load",
                         Value = Math.Round(freeVal, 1),
                         Unit = "%",
                         HardwareName = hwName,
                         ShortLabel = $"SSD {driveIdx} Free",
-                    });
+                    };
+                    LastKnownSensors[freeDto.Id] = freeDto;
                 }
             }
         }
 
-        return (cpuTemps.Count > 0 ? cpuTemps.MaxBy(t => t.Value).Value : null, results);
+        return (cpuTemps.Count > 0 ? cpuTemps.MaxBy(t => t.Value).Value : null, LastKnownSensors.Values.ToList());
     }
```
