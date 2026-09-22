// Copyright (c) HardwareMonitor. MIT license.

using System.Diagnostics;
using System.Management;
using LibreHardwareMonitor.Hardware;

namespace HardwareMonitorExtension.Services;

/// <summary>
/// 全量枚举 LibreHardwareMonitor 传感器（CPU/GPU/存储/主板等），并探测 PawnIO。
/// 修复点：不再用脆弱的名称匹配丢读数；温度/占用/风扇等均输出。
/// </summary>
public sealed partial class SensorService : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _lhmReady;
    private Computer? _computer;
    private PawnIoNative? _pawn;
    private string? _pawnError;
    private string _backendLabel = "初始化中";
    private bool _disposed;
    private int _sampleFailures;

    public string BackendLabel => _backendLabel;
    public bool PawnIoConnected => _pawn is not null;
    public string? PawnIoError => _pawnError;
    public HistoryStore History { get; } = new(180);

    public async Task InitializeAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_disposed) return;

            if (PawnIoNative.TryOpen(out var pawn, out var err))
            {
                _pawn = pawn;
                _pawnError = null;
            }
            else
            {
                _pawnError = err;
            }

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
            return pawn is null ? "LHM 打开失败 · 无 PawnIO" : "LHM 打开失败 · PawnIO 已安装";
        return pawn is null
            ? "LibreHardwareMonitor（无 PawnIO）"
            : "LibreHardwareMonitor + PawnIO 已安装";
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
        if (!list.Any(s => s.Kind == SensorKind.Load && s.Group == "处理器"))
        {
            var cpuLoad = TryReadCpuCounter();
            if (cpuLoad is not null)
            {
                list.Add(new SensorReading(
                    "perf:cpu-total",
                    "处理器",
                    "CPU 占用",
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
        FixupCpuPower(list);

        return Sort(list);
    }

    private static void FixupCpuPower(List<SensorReading> list)
    {
        var load = list.FirstOrDefault(s => s.Kind == SensorKind.Load && s.Group == "处理器")?.Value ?? 15.0;
        var cpuPower = list
            .Where(s => s.Kind == SensorKind.Power && s.Group == "处理器")
            .ToList();

        var hasReal = cpuPower.Any(p => p.Value > 0.5);
        if (hasReal) return;

        const double tdp = 65.0;
        const double idle = 12.0;
        var est = Math.Round(idle + tdp * (load / 100.0) * 0.92, 1);

        if (cpuPower.Count > 0)
        {
            // 用估算值替换全 0 的功率项
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Kind == SensorKind.Power && list[i].Group == "处理器" && list[i].Value <= 0.5)
                {
                    list[i] = list[i] with
                    {
                        Value = est,
                        Unit = "W",
                        Source = list[i].Source + " · est(load×TDP)",
                        Name = list[i].Name.Contains("估算") ? list[i].Name : list[i].Name + "（估算）",
                    };
                }
            }
        }
        else
        {
            list.Add(new SensorReading(
                "est:cpu-ppt",
                "处理器",
                "CPU 封装功率（估算）",
                "Estimated · load×TDP65",
                SensorKind.Power,
                est,
                "W",
                "CPU",
                "CPU 功率",
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
        SensorKind.Data => sensor?.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) == true ? "MB" : "MB/s",
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
        HardwareType.Cpu => "处理器",
        HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel => "显卡",
        HardwareType.Storage => "存储",
        HardwareType.Motherboard => "主板",
        HardwareType.Memory => "内存",
        HardwareType.Battery => "电池",
        HardwareType.Network => "网络",
        _ => hardware.HardwareType.ToString(),
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
            return $"{prefix} 封装";
        if (n.Contains("Hot Spot", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Junction", StringComparison.OrdinalIgnoreCase))
            return $"{prefix} 热点";
        if (n.Equals("Total", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("CPU Total", StringComparison.OrdinalIgnoreCase) ||
            n.Equals("Core", StringComparison.OrdinalIgnoreCase) && hardware.HardwareType == HardwareType.Cpu)
            return prefix;
        if (n.Contains("Temperature", StringComparison.OrdinalIgnoreCase) && hardware.HardwareType == HardwareType.Storage)
            return $"{prefix} 温度";

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
                    "主板",
                    $"ACPI 热区 {name}",
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
                "处理器",
                "CPU 占用",
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
                "状态",
                "无法读取传感器",
                "请检查 PawnIO / 管理员权限",
                SensorKind.Other,
                0,
                string.Empty,
                "—",
                "无数据",
                null));
        }

        return list;
    }

    /// <summary>优先温度、负载，再按组与名称。</summary>
    private static List<SensorReading> Sort(List<SensorReading> list)
    {
        static int KindRank(SensorKind k) => k switch
        {
            SensorKind.Temperature => 0,
            SensorKind.Load => 1,
            SensorKind.Power => 2,
            SensorKind.Fan => 3,
            SensorKind.Clock => 4,
            _ => 5,
        };

        static int GroupRank(string g) => g switch
        {
            "处理器" => 0,
            "显卡" => 1,
            "存储" => 2,
            "主板" => 3,
            "内存" => 4,
            _ => 9,
        };

        return list
            .OrderBy(s => KindRank(s.Kind))
            .ThenBy(s => GroupRank(s.Group))
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

        _pawn?.Dispose();
        _gate.Dispose();
    }
}
