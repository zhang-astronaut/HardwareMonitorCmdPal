// Copyright (c) HardwareMonitor. MIT license.

using System.Diagnostics;
using System.Management;
using HardwareMonitorExtension.Localization;
using LibreHardwareMonitor.Hardware;

namespace HardwareMonitorExtension.Services;

/// <summary>
/// 全量枚举 LibreHardwareMonitor 传感器（CPU/GPU/存储/主板等）。
/// 说明：PawnIO 在普通权限下设备 ACL 拒绝访问（实测 0x80070005），
/// CPU 真实温度改由 SYSTEM 助手（TempHelperServer）提供，见 MergeElevatedCpuTemp。
/// </summary>
/// <summary>
/// Fully enumerates LibreHardwareMonitor sensors (CPU, GPU, storage, motherboard, etc.).
/// Note: Under normal user permissions, PawnIO device ACL denies access (measured 0x80070005);
/// authentic CPU temperature is provided by the SYSTEM helper (TempHelperServer), see MergeElevatedCpuTemp.
/// </summary>
/// <summary>
/// LibreHardwareMonitor センサー（CPU/GPU/ストレージ/マザーボードなど）を全件列挙。
/// 説明：通常権限では PawnIO デバイスの ACL によりアクセスが拒否されるため（実测 0x80070005）、
/// CPU の実際の温度は SYSTEM ヘルパー（TempHelperServer）から取得します（MergeElevatedCpuTemp 参照）。
/// </summary>
public sealed partial class SensorService : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _lhmReady;
    private Computer? _computer;
    private string _backendLabel = Loc.Current.BackendInitializing;
    private bool _disposed;
    private int _sampleFailures;

    public string BackendLabel => _backendLabel;
    public HistoryStore History { get; } = new(180);

    public async Task InitializeAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_disposed) return;
            _lhmReady = TryOpenLibreHardwareMonitor();
            _backendLabel = BuildBackendLabel(_lhmReady);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string BuildBackendLabel(bool lhm)
    {
        var pawn = PawnIoLocator.FindInstallDirectory();
        if (!lhm)
            return pawn is null ? Loc.Current.BackendLhmFailNoPawn : Loc.Current.BackendLhmFailPawn;
        return pawn is null
            ? Loc.Current.BackendLhmNoPawn
            : Loc.Current.BackendLhmWithPawn;
    }

    private bool TryOpenLibreHardwareMonitor()
    {
        try
        {
            _computer?.Close();
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsStorageEnabled = true,
                IsMotherboardEnabled = true,
                IsMemoryEnabled = true,
                IsNetworkEnabled = false,
                IsBatteryEnabled = true,
            };
            _computer.Open();
            return true;
        }
        catch
        {
            _computer = null;
            return false;
        }
    }

    public async Task<SensorSnapshot> SampleAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            IReadOnlyList<SensorReading> readings;
            var stale = false;

            if (_lhmReady && _computer is not null)
            {
                try
                {
                    readings = SampleFromLibreHardwareMonitor();
                    _sampleFailures = 0;
                }
                catch
                {
                    _sampleFailures++;
                    if (_sampleFailures >= 3)
                    {
                        // 驱动异常时尝试重开
                        // Attempt to re-open on driver exception
                        // ドライバー例外発生時に再オープンを試行
                        _lhmReady = TryOpenLibreHardwareMonitor();
                        _sampleFailures = 0;
                    }
                    readings = SampleFallback();
                    stale = true;
                }

                if (readings.Count == 0)
                {
                    readings = SampleFallback();
                    stale = true;
                }
            }
            else
            {
                readings = SampleFallback();
                stale = true;
            }

            PushHistory(readings);
            return new SensorSnapshot(DateTimeOffset.Now, readings, stale);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void PushHistory(IReadOnlyList<SensorReading> readings)
    {
        foreach (var r in readings)
        {
            if (double.IsFinite(r.Value))
                History.Push(r.Id, DateTimeOffset.UtcNow, r.Value);
        }
    }

    private IReadOnlyList<SensorReading> SampleFromLibreHardwareMonitor()
    {
        var list = new List<SensorReading>();
        if (_computer is null) return list;

        foreach (var hardware in EnumerateHardware(_computer))
        {
            // 多数功率/电流传感器在第二次 Update 后才有值（MSR 采样差分）
            // Most power/current sensors only produce values after the second Update (MSR sampling differential)
            // 多くの電力/電流センサーは、2回目の Update 後に初めて値を取得可能（MSR サンプリング差分）
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
                sub.Update();
            hardware.Update();

            var loads = CollectLoads(hardware);
            foreach (var sensor in hardware.Sensors)
            {
                if (!sensor.Value.HasValue) continue;
                var v = sensor.Value.Value;
                if (float.IsNaN(v) || float.IsInfinity(v)) continue;

                var kind = MapKind(sensor.SensorType);
                // 保留全部有限读数（含 Other）；温度仅丢弃明显非法值
                // Retain all finite readings (including Other); only discard blatantly invalid temperature values
                // すべての有限な測定値（Other を含む）を保持。温度は明らかに不正な値のみ破棄
                if (kind == SensorKind.Temperature && (v < -50 || v > 200))
                    continue;

                var group = MapGroup(hardware);
                var shortLabel = ShortLabel(hardware, sensor);
                string? mergedLoad = null;
                if (kind == SensorKind.Temperature && loads.TryGetValue("total", out var total))
                    mergedLoad = $"{Math.Round(total)}%";

                list.Add(new SensorReading(
                    Id: sensor.Identifier.ToString(),
                    Group: group,
                    Name: $"{hardware.Name} · {sensor.Name}",
                    Source: $"{hardware.HardwareType} / {sensor.SensorType}",
                    Kind: kind,
                    Value: v,
                    Unit: UnitOf(kind, sensor),
                    HardwareName: hardware.Name,
                    ShortLabel: shortLabel,
                    MergedLoadText: mergedLoad));
            }
        }

        // CPU 占用兜底（若 LHM 未给出）
        // Fallback CPU load (if not provided by LHM)
        // CPU 使用率のフォールバック（LHM から提供されない場合）
        if (!list.Any(s => s.Kind == SensorKind.Load && HardwareGroups.IsProcessor(s.Group)))
        {
            var cpuLoad = TryReadCpuCounter();
            if (cpuLoad is not null)
            {
                list.Add(new SensorReading(
                    "perf:cpu-total",
                    HardwareGroups.Processor,
                    Loc.Current.CpuTotalLoad,
                    "PerformanceCounter",
                    SensorKind.Load,
                    cpuLoad.Value,
                    "%",
                    "CPU",
                    "CPU",
                    null));
            }
        }

        // CPU 功率：驱动未提供或长期为 0 时，用 load×TDP 模型估算，避免界面一直 0W
        // CPU power: when driver does not provide or remains 0, estimate using load×TDP model to avoid staying at 0W on UI
        // CPU 電力：ドライバーから取得できない場合や常に 0 の場合、UI が 0W のままになるのを防ぐため load×TDP モデルで推定
        FixupCpuPower(list);
        // 提权数据注入（First-Source）：合并 SYSTEM 助手采集的真实 CPU、SSD 与主板读数
        // Elevated data injection (First-Source): merge authentic CPU, SSD, and Motherboard readings from SYSTEM helper
        // 昇格データ統合（First-Source）：SYSTEM ヘルパーが取得した実際の CPU、SSD、マザーボード測定値を統合
        MergeElevatedSensors(list);

        return Sort(list);
    }

    private static string NormalizeStorageShortLabel(string shortLabel, string name, string source)
    {
        if (source.Contains("Storage", StringComparison.OrdinalIgnoreCase) && source.Contains("Data", StringComparison.OrdinalIgnoreCase))
        {
            var m = System.Text.RegularExpressions.Regex.Match(shortLabel, @"SSD\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var prefix = m.Success ? $"SSD {m.Groups[1].Value}" : "SSD";

            if (shortLabel.Contains("Read", StringComparison.OrdinalIgnoreCase) || name.Contains("Read", StringComparison.OrdinalIgnoreCase))
                return $"{prefix} Total Reads";

            if (shortLabel.Contains("Writ", StringComparison.OrdinalIgnoreCase) || shortLabel.Contains("Data Wr", StringComparison.OrdinalIgnoreCase) || name.Contains("Writ", StringComparison.OrdinalIgnoreCase))
                return $"{prefix} Total Writes";
        }
        return shortLabel;
    }

    private static void MergeElevatedSensors(List<SensorReading> list)
    {
        var snapshot = ElevatedTempHelper.TryReadSnapshot();
        var elevatedCpu = snapshot?.CpuTemp ?? ElevatedTempHelper.TryReadCpuTemperature();

        // 1. CPU 温度处理（优先采用 SYSTEM 助手的 AMD Tctl/Intel Package 真实读数）
        // 1. CPU temperature handling (prioritizes authentic AMD Tctl / Intel Package from SYSTEM helper)
        // 1. CPU 温度処理（SYSTEM ヘルパーの AMD Tctl / Intel Package の実際の測定値を優先）
        if (elevatedCpu is null or < 1 or > 150)
        {
            // 修复点：无助手数据时，普通权限下 CPU 温度常读出 0（AMD SMN 需内核驱动），
            // 直接丢弃无效读数，避免界面显示误导性的 0°C。
            // Fix: without helper data, CPU temperature often reads 0 under normal permissions (AMD SMN requires kernel driver);
            // directly discard invalid readings to avoid misleading 0°C on the UI.
            // 修正点：ヘルパー データがない場合、通常権限では CPU 温度が 0 と読み取られることが多いため（AMD SMN にはカーネル ドライバーが必要）、
            // UI に誤解を招く 0°C が表示されないよう、無効な測定値を直接破棄。
            list.RemoveAll(s =>
                s.Kind == SensorKind.Temperature &&
                HardwareGroups.IsProcessor(s.Group) &&
                s.Value <= 0);
            return;
        }

        // 替换优先级：封装/Tctl/Tdie > 第一个 CPU 温度项 > 新增
        // Replacement priority: Package/Tctl/Tdie > first CPU temperature item > add new
        // 置換優先順位：Package/Tctl/Tdie > 最初の CPU 温度項目 > 新規追加
        var idx = list.FindIndex(s =>
            s.Kind == SensorKind.Temperature && HardwareGroups.IsProcessor(s.Group) &&
            (s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
             s.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
             s.Name.Contains("Tdie", StringComparison.OrdinalIgnoreCase)));
        if (idx < 0)
            idx = list.FindIndex(s =>
                s.Kind == SensorKind.Temperature && HardwareGroups.IsProcessor(s.Group));

        if (idx >= 0)
        {
            list[idx] = list[idx] with
            {
                Value = elevatedCpu.Value,
                Source = list[idx].Source + " · admin-helper",
            };
        }
        else
        {
            list.Add(new SensorReading(
                "helper:cpu-tctl",
                HardwareGroups.Processor,
                Loc.Current.CpuPackageTemp,
                "Elevated helper · LHM",
                SensorKind.Temperature,
                elevatedCpu.Value,
                "°C",
                "CPU",
                Loc.Current.CpuPackage,
                null));
        }

        // 2. 存储与主板提权传感器注入（解决普通权限下 \\.\PhysicalDrive 与 SuperIO LPC 无法读取的问题）
        // 2. Storage & Motherboard elevated sensor injection (resolves un-elevated access denial to \\.\PhysicalDrive and SuperIO LPC)
        // 2. ストレージおよびマザーボードの昇格センサー統合（通常権限での \\.\PhysicalDrive および SuperIO LPC へのアクセス拒否を解決）
        if (snapshot?.Sensors is { Count: > 0 } elevatedSensors)
        {
            foreach (var es in elevatedSensors)
            {
                // Discard invalid temperatures (<= 0°C), but 0 is completely valid for activities, throughput, and loads
                if (es.Kind.Equals("temperature", StringComparison.OrdinalIgnoreCase) && es.Value <= 0) continue;
                if (es.Value < 0) continue;

                if (!Enum.TryParse<SensorKind>(es.Kind, ignoreCase: true, out var kind))
                    continue;

                var label = NormalizeStorageShortLabel(es.ShortLabel, es.Name, es.Source);
                var existingIdx = list.FindIndex(s => s.Id == es.Id || s.Name == es.Name);
                if (existingIdx >= 0)
                {
                    list[existingIdx] = list[existingIdx] with
                    {
                        Value = es.Value,
                        Unit = es.Unit,
                        Source = es.Source,
                        HardwareName = !string.IsNullOrEmpty(es.HardwareName) ? es.HardwareName : list[existingIdx].HardwareName,
                        ShortLabel = !string.IsNullOrEmpty(label) ? label : list[existingIdx].ShortLabel,
                    };
                }
                else
                {
                    list.Add(new SensorReading(
                        Id: es.Id,
                        Group: es.Group,
                        Name: es.Name,
                        Source: es.Source,
                        Kind: kind,
                        Value: es.Value,
                        Unit: es.Unit,
                        HardwareName: es.HardwareName,
                        ShortLabel: label,
                        MergedLoadText: null));
                }
            }
        }
    }

    private static void FixupCpuPower(List<SensorReading> list)
    {
        var load = list.FirstOrDefault(s => s.Kind == SensorKind.Load && HardwareGroups.IsProcessor(s.Group))?.Value ?? 15.0;
        var cpuPower = list
            .Where(s => s.Kind == SensorKind.Power && HardwareGroups.IsProcessor(s.Group))
            .ToList();

        var hasReal = cpuPower.Any(p => p.Value > 0.5);
        if (hasReal) return;

        const double tdp = 65.0;
        const double idle = 12.0;
        var est = Math.Round(idle + tdp * (load / 100.0) * 0.92, 1);

        if (cpuPower.Count > 0)
        {
            // 用估算值替换全 0 的功率项
            // Replace all-zero power items with estimated value
            // すべて 0 の電力項目を推定値で置換
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Kind == SensorKind.Power && HardwareGroups.IsProcessor(list[i].Group) && list[i].Value <= 0.5)
                {
                    list[i] = list[i] with
                    {
                        Value = est,
                        Unit = "W",
                        Source = list[i].Source + " · est(load×TDP)",
                        Name = Loc.Current.FormatEstimatedName(list[i].Name),
                    };
                }
            }
        }
        else
        {
            list.Add(new SensorReading(
                "est:cpu-ppt",
                HardwareGroups.Processor,
                Loc.Current.CpuPackagePowerEst,
                "Estimated · load×TDP65",
                SensorKind.Power,
                est,
                "W",
                "CPU",
                Loc.Current.CpuPower,
                null));
        }
    }

    private static IEnumerable<IHardware> EnumerateHardware(Computer computer)
    {
        foreach (var hw in computer.Hardware)
        {
            yield return hw;
            foreach (var sub in hw.SubHardware)
                yield return sub;
        }
    }

    private static Dictionary<string, double> CollectLoads(IHardware hardware)
    {
        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in hardware.Sensors)
        {
            if (s.SensorType != SensorType.Load || !s.Value.HasValue || float.IsNaN(s.Value.Value))
                continue;
            var name = s.Name;
            map[name] = s.Value.Value;
            if (name.Contains("Total", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("GPU", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Memory", StringComparison.OrdinalIgnoreCase))
            {
                map["total"] = s.Value.Value;
            }
        }

        return map;
    }

    private static SensorKind MapKind(SensorType type) => type switch
    {
        SensorType.Temperature => SensorKind.Temperature,
        SensorType.Load => SensorKind.Load,
        SensorType.Clock => SensorKind.Clock,
        SensorType.Power => SensorKind.Power,
        SensorType.Fan => SensorKind.Fan,
        SensorType.Throughput or SensorType.Data => SensorKind.Data,
        SensorType.Voltage => SensorKind.Voltage,
        SensorType.Level => SensorKind.Level,
        SensorType.Flow => SensorKind.Flow,
        SensorType.Control => SensorKind.Control,
        SensorType.Factor => SensorKind.Factor,
        SensorType.Energy => SensorKind.Energy,
        SensorType.Noise => SensorKind.Noise,
        SensorType.TimeSpan => SensorKind.TimeSpan,
        _ => SensorKind.Other,
    };

    private static string UnitOf(SensorKind kind, ISensor? sensor = null) => kind switch
    {
        SensorKind.Temperature => "°C",
        SensorKind.Load => "%",
        SensorKind.Clock => "MHz",
        SensorKind.Power => "W",
        SensorKind.Fan => "RPM",
        SensorKind.Data => (sensor?.SensorType == SensorType.Throughput || 
                            sensor?.Name.Contains("Rate", StringComparison.OrdinalIgnoreCase) == true || 
                            sensor?.Name.Contains("Speed", StringComparison.OrdinalIgnoreCase) == true) ? "MB/s" : "GB",
        SensorKind.Voltage => "V",
        SensorKind.Level => "%",
        SensorKind.Flow => "L/h",
        SensorKind.Control => "%",
        SensorKind.Factor => string.Empty,
        SensorKind.Energy => "mWh",
        SensorKind.Noise => "dBA",
        SensorKind.TimeSpan => "s",
        _ => string.Empty,
    };

    private static string MapGroup(IHardware hardware) => hardware.HardwareType switch
    {
        HardwareType.Cpu => HardwareGroups.Processor,
        HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel => HardwareGroups.Graphics,
        HardwareType.Storage => HardwareGroups.Storage,
        HardwareType.Motherboard or HardwareType.SuperIO or HardwareType.EmbeddedController => HardwareGroups.Motherboard,
        HardwareType.Memory => HardwareGroups.Memory,
        HardwareType.Battery => HardwareGroups.Battery,
        HardwareType.Network => HardwareGroups.Network,
        _ => HardwareGroups.Other,
    };

    private static string ShortLabel(IHardware hardware, ISensor sensor)
    {
        var prefix = hardware.HardwareType switch
        {
            HardwareType.Cpu => "CPU",
            HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel => "GPU",
            HardwareType.Storage => "SSD",
            HardwareType.Motherboard => "MB",
            HardwareType.Memory => "RAM",
            HardwareType.Battery => "BAT",
            _ => "HW",
        };

        var n = sensor.Name;
        if (n.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Tdie", StringComparison.OrdinalIgnoreCase))
            return $"{prefix} {Loc.Current.ShortLabelPackage}";
        if (n.Contains("Hot Spot", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Junction", StringComparison.OrdinalIgnoreCase))
            return $"{prefix} {Loc.Current.ShortLabelHotSpot}";
        if (n.Equals("Total", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("CPU Total", StringComparison.OrdinalIgnoreCase) ||
            n.Equals("Core", StringComparison.OrdinalIgnoreCase) && hardware.HardwareType == HardwareType.Cpu)
            return prefix;
        if (hardware.HardwareType == HardwareType.Storage)
        {
            if (n.Contains("Temperature", StringComparison.OrdinalIgnoreCase))
                return $"{prefix} {Loc.Current.ShortLabelTemp}";
            if (n.Contains("Read Rate", StringComparison.OrdinalIgnoreCase))
                return $"{prefix} Read Rate";
            if (n.Contains("Write Rate", StringComparison.OrdinalIgnoreCase))
                return $"{prefix} Write Rate";
            if (n.Contains("Read Activity", StringComparison.OrdinalIgnoreCase))
                return $"{prefix} Read Act";
            if (n.Contains("Write Activity", StringComparison.OrdinalIgnoreCase))
                return $"{prefix} Write Act";
            if (n.Contains("Total Activity", StringComparison.OrdinalIgnoreCase))
                return $"{prefix} Act";
            if (n.Contains("Used Space", StringComparison.OrdinalIgnoreCase))
                return $"{prefix} Used";
            if (n.Contains("Free Space", StringComparison.OrdinalIgnoreCase))
                return $"{prefix} Free";
            if (n.Contains("Data Read", StringComparison.OrdinalIgnoreCase) || n.Equals("Read", StringComparison.OrdinalIgnoreCase))
                return $"{prefix} Total Reads";
            if (n.Contains("Data Written", StringComparison.OrdinalIgnoreCase) || n.Equals("Write", StringComparison.OrdinalIgnoreCase))
                return $"{prefix} Total Writes";
        }

        return $"{prefix} {Truncate(n, 18)}";
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..(max - 1)] + "…";

    private static double? TryReadCpuCounter()
    {
        try
        {
            using var cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpu.NextValue();
            Thread.Sleep(50);
            return Math.Clamp(cpu.NextValue(), 0, 100);
        }
        catch
        {
            return null;
        }
    }

    private IReadOnlyList<SensorReading> SampleFallback()
    {
        var list = new List<SensorReading>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\WMI",
                "SELECT InstanceName, CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            foreach (ManagementObject obj in searcher.Get())
            {
                var raw = Convert.ToDouble(obj["CurrentTemperature"]);
                var c = raw / 10.0 - 273.15;
                if (c is < -50 or > 200) continue;
                var name = obj["InstanceName"]?.ToString() ?? "ACPI";
                list.Add(new SensorReading(
                    $"acpi:{name}",
                    HardwareGroups.Motherboard,
                    Loc.Current.AcpiThermalZone(name),
                    "WMI · MSAcpi_ThermalZoneTemperature",
                    SensorKind.Temperature,
                    c,
                    "°C",
                    name,
                    "ACPI",
                    null));
            }
        }
        catch
        {
            // ignore
        }

        var load = TryReadCpuCounter();
        if (load is not null)
        {
            list.Add(new SensorReading(
                "perf:cpu-total",
                HardwareGroups.Processor,
                Loc.Current.CpuTotalLoad,
                "PerformanceCounter",
                SensorKind.Load,
                load.Value,
                "%",
                "CPU",
                "CPU",
                null));
        }

        if (list.Count == 0)
        {
            list.Add(new SensorReading(
                "status:none",
                Loc.Current.StatusTitle,
                Loc.Current.StatusCannotRead,
                Loc.Current.StatusCheckPerms,
                SensorKind.Other,
                0,
                string.Empty,
                "—",
                Loc.Current.StatusNoData,
                null));
        }

        return list;
    }

    /// <summary>层级排序：硬件类别 > 存储序号 > 指标类型 > 指标名称</summary>
    /// <summary>Hierarchical sort: Hardware Group > Drive Ordinal > Metric Kind > Name</summary>
    /// <summary>階層ソート：ハードウェア グループ > ストレージ連番 > 測定値種類 > 名称</summary>
    private static List<SensorReading> Sort(List<SensorReading> list)
    {
        static int MetricRank(SensorReading s)
        {
            var g = s.Group;
            var k = s.Kind;
            var n = s.Name;
            var sh = s.ShortLabel.ToLowerInvariant();

            if (HardwareGroups.IsProcessor(g))
            {
                return k switch
                {
                    SensorKind.Temperature => 0,
                    SensorKind.Load => 1,
                    SensorKind.Power => 2,
                    SensorKind.Voltage => 3,
                    SensorKind.Clock => 4,
                    _ => 5,
                };
            }
            if (HardwareGroups.IsMemory(g))
            {
                var isVirtual = n.Contains("Virtual", StringComparison.OrdinalIgnoreCase);
                var subRank = k == SensorKind.Load ? 0 : 1;
                return (isVirtual ? 10 : 0) + subRank;
            }
            if (HardwareGroups.IsGraphics(g))
            {
                if (k == SensorKind.Temperature)
                {
                    if (sh.Contains("core")) return 0;
                    if (sh.Contains("hot") || sh.Contains("junction")) return 1;
                    if (sh.Contains("mem")) return 2;
                    return 3;
                }
                return k switch
                {
                    SensorKind.Load => 10,
                    SensorKind.Power => 11,
                    SensorKind.Voltage => 12,
                    SensorKind.Clock => 13,
                    SensorKind.Fan => 14,
                    _ => 15,
                };
            }
            if (HardwareGroups.IsStorage(g))
            {
                var src = s.Source ?? string.Empty;
                var text = $"{s.ShortLabel} {s.Name}".ToLowerInvariant();
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
            return k switch
            {
                SensorKind.Temperature => 0,
                SensorKind.Fan => 1,
                SensorKind.Load => 2,
                SensorKind.Voltage => 3,
                _ => 4,
            };
        }

        static int DriveOrdinal(SensorReading s)
        {
            if (!HardwareGroups.IsStorage(s.Group)) return 0;
            var text = $"{s.HardwareName} {s.ShortLabel} {s.Id} {s.Name}";
            var m = System.Text.RegularExpressions.Regex.Match(text, @"(?:SSD|\/(?:nvme|storage)\/)\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return m.Success && int.TryParse(m.Groups[1].Value, out var idx) ? idx : 0;
        }

        return list
            .OrderBy(s => HardwareGroups.Rank(s.Group))
            .ThenBy(DriveOrdinal)
            .ThenBy(MetricRank)
            .ThenBy(s => s.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            _computer?.Close();
        }
        catch
        {
            // ignore
        }

        _gate.Dispose();
    }
}
