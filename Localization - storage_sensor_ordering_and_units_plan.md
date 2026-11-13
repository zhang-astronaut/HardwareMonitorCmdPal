# Implementation Plan: Storage Sensor Ordering & Telemetry Units

## Goal Description
Correct the ordering, naming, and unit attribution for storage drives (NVMe/SSD) across **MyronTech Solutions' Hardware Monitor** Command Palette extension and offline dashboard. Currently, cumulative SMART data counters (`Data Read` and `Data Written`) are misattributed with the throughput unit `MB/s` instead of `GB`, and `Data Read` is sorted prematurely (between Read Activity and Write Activity) instead of placing both cumulative counters at the bottom of each drive's telemetry block.

---

## User Review Required

> [!IMPORTANT]
> The target ordering per storage device is strictly defined as:
> 1. **Temperature** (`°C`)
> 2. **Overall Activity** (`%`)
> 3. **Read Activity** (`%`)
> 4. **Read Rate** (`MB/s`)
> 5. **Write Activity** (`%`)
> 6. **Write Rate** (`MB/s`)
> 7. **Used Space** (`%`)
> 8. **Free Space** (`%`)
> 9. **Total Read Data** (`GB`)
> 10. **Total Written Data** (`GB`)

> [!NOTE]
> The sorting algorithm switches from loose substring matching to an explicit category-first condition matrix:
> `(Hardware Category && Sensor Category) && (Required Keywords) && !(Excluded Keywords)`
> This ensures `Load` (%), `Throughput` (`MB/s`), and `Data` (`GB`) cannot collide.

---

## Proposed Changes

### Core Extension Services

#### [MODIFY] [SensorService.cs](file:///C:/Users/nmyron/Repos/HardwareMonitorCmdPal/HardwareMonitorExtension/Services/SensorService.cs)
- **Fix `UnitOf`**: Return `"GB"` for storage `SensorKind.Data` unless the sensor is throughput / rate / speed (`"MB/s"`).
- **Propagate Unit in `MergeElevatedSensors`**: Add `Unit = es.Unit` when updating existing sensor instances from the elevated telemetry snapshot so elevated `GB` units are not overwritten with default units.
- **Implement Structured `MetricRank`**: Replace the previous storage ranking with the structured category-first matrix.
- **Normalize `ShortLabel`**: Add direct mapping for storage metrics so un-elevated or raw LHM sensors produce consistent labels (`SSD Read Rate`, `SSD Total Read`, etc.).

```csharp
// SensorService.cs - UnitOf Data Sensor Fix
SensorKind.Data => (sensor?.SensorType == SensorType.Throughput || 
                    sensor?.Name.Contains("Rate", StringComparison.OrdinalIgnoreCase) == true || 
                    sensor?.Name.Contains("Speed", StringComparison.OrdinalIgnoreCase) == true) ? "MB/s" : "GB",
```

```csharp
// SensorService.cs - MergeElevatedSensors Unit Propagation Fix
list[existingIdx] = list[existingIdx] with
{
    Value = es.Value,
    Unit = es.Unit,
    Source = es.Source,
    HardwareName = !string.IsNullOrEmpty(es.HardwareName) ? es.HardwareName : list[existingIdx].HardwareName,
    ShortLabel = !string.IsNullOrEmpty(es.ShortLabel) ? es.ShortLabel : list[existingIdx].ShortLabel,
};
```

```csharp
// SensorService.cs - MetricRank Storage Ordering
if (HardwareGroups.IsStorage(g))
{
    var isTemp = k == SensorKind.Temperature || src.Contains("Temperature", StringComparison.OrdinalIgnoreCase);
    var isLoad = k == SensorKind.Load || src.Contains("Load", StringComparison.OrdinalIgnoreCase);
    var isThroughput = src.Contains("Throughput", StringComparison.OrdinalIgnoreCase) || s.Unit.Equals("MB/s", StringComparison.OrdinalIgnoreCase);
    var isData = (k == SensorKind.Data || src.Contains("Data", StringComparison.OrdinalIgnoreCase)) && !isThroughput;

    // 1. Temp: Storage && Temperature
    if (isTemp) return 0;

    // 2. Overall Activity: Storage && Load && (act/total) && !(read/write/used/free)
    if (isLoad && (text.Contains("act") || text.Contains("total")) && !text.Contains("read") && !text.Contains("write") && !text.Contains("used") && !text.Contains("free")) return 1;

    // 3. Read Act: Storage && Load && read
    if (isLoad && text.Contains("read")) return 2;

    // 4. Read Rate: Storage && (Throughput/MB/s) && read
    if (isThroughput && text.Contains("read")) return 3;

    // 5. Write Act: Storage && Load && write
    if (isLoad && text.Contains("write")) return 4;

    // 6. Write Rate: Storage && (Throughput/MB/s) && write
    if (isThroughput && text.Contains("write")) return 5;

    // 7. Used: Storage && Load && used
    if (isLoad && text.Contains("used")) return 6;

    // 8. Free: Storage && Load && free
    if (isLoad && text.Contains("free")) return 7;

    // 9. Total Read Data: Storage && Data && read && !(rate/speed/act)
    if (isData && text.Contains("read") && !text.Contains("rate") && !text.Contains("act")) return 8;

    // 10. Total Written Data: Storage && Data && (write/written) && !(rate/speed/act)
    if (isData && (text.Contains("write") || text.Contains("written")) && !text.Contains("rate") && !text.Contains("act")) return 9;

    return 10;
}
```

---

#### [MODIFY] [TempHelperServer.cs](file:///C:/Users/nmyron/Repos/HardwareMonitorCmdPal/HardwareMonitorExtension/Services/TempHelperServer.cs)
- Standardize the short label generation for storage cumulative read and written data:
```csharp
"data" when s.SensorType == SensorType.Throughput || s.Name.Contains("Rate", StringComparison.OrdinalIgnoreCase) =>
    s.Name.Contains("Read", StringComparison.OrdinalIgnoreCase) ? $"SSD {driveIdx} Read Rate" : $"SSD {driveIdx} Write Rate",
"data" when s.Name.Contains("Read", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Total Read",
"data" when s.Name.Contains("Write", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Total Written",
```

---

### Dashboard Webview Assets

#### [MODIFY] [app.js](file:///C:/Users/nmyron/Repos/HardwareMonitorCmdPal/HardwareMonitorExtension/Assets/app.js)
- Update `storageMetricOrder(s)` (storage panel sorting) and `metricRank(s)` (overall table sorting) to use the identical structured evaluation:

```javascript
function storageMetricOrder(s) {
  const isTemp = s.kind === "temperature" || s.source?.includes("Temperature");
  const isLoad = s.kind === "load" || s.source?.includes("Load");
  const isThroughput = s.source?.includes("Throughput") || s.unit === "MB/s";
  const isData = (s.kind === "data" || s.source?.includes("Data")) && !isThroughput;

  const name = `${s.short || ""} ${s.name || ""}`.toLowerCase();

  // 1. Temp: Storage && Temperature
  if (isTemp) return 0;

  // 2. Overall Activity: Storage && Load && (act/total) && !(read/write/used/free)
  if (isLoad && (name.includes("act") || name.includes("total")) && !name.includes("read") && !name.includes("write") && !name.includes("used") && !name.includes("free")) return 1;

  // 3. Read Act: Storage && Load && read
  if (isLoad && name.includes("read")) return 2;

  // 4. Read Rate: Storage && (Throughput/MB/s) && read
  if (isThroughput && name.includes("read")) return 3;

  // 5. Write Act: Storage && Load && write
  if (isLoad && name.includes("write")) return 4;

  // 6. Write Rate: Storage && (Throughput/MB/s) && write
  if (isThroughput && name.includes("write")) return 5;

  // 7. Used: Storage && Load && used
  if (isLoad && name.includes("used")) return 6;

  // 8. Free: Storage && Load && free
  if (isLoad && name.includes("free")) return 7;

  // 9. Total Read Data: Storage && Data && read && !(rate/speed/act)
  if (isData && name.includes("read") && !name.includes("rate") && !name.includes("act")) return 8;

  // 10. Total Written Data: Storage && Data && (write/written) && !(rate/speed/act)
  if (isData && (name.includes("write") || name.includes("written")) && !name.includes("rate") && !name.includes("act")) return 9;

  return 10;
}
```

---

## Verification Plan

### Automated Build Verification
- Execute `dotnet build HardwareMonitorExtension.sln` to confirm clean compilation with 0 errors.

### Manual Verification
1. Inspect the resulting sensor list in Command Palette:
   - Verify `Total Activity` $\rightarrow$ `Read Activity` $\rightarrow$ `Read Rate` $\rightarrow$ `Write Activity` $\rightarrow$ `Write Rate` $\rightarrow$ `Used Space` $\rightarrow$ `Free Space` $\rightarrow$ `Total Read Data` $\rightarrow$ `Total Written Data`.
   - Verify `Total Read Data` displays in `GB` (e.g. `20070.0 GB`).
   - Verify `Total Written Data` displays in `GB` (e.g. `69405.0 GB`).
2. Open the real-time offline dashboard (`index.html`) and verify the Storage panel renders the drive blocks with the identical 10-item sequence.
