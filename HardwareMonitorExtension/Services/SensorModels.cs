// Copyright (c) HardwareMonitor. MIT license.

namespace HardwareMonitorExtension.Services;

public enum HeatLevel
{
    Normal,
    Warm,
    Hot,
    Critical,
}

public enum SensorKind
{
    Temperature,
    Load,
    Clock,
    Power,
    Fan,
    Data,
    Voltage,
    Level,
    Flow,
    Control,
    Factor,
    Energy,
    Noise,
    TimeSpan,
    Other,
}

/// <summary>统一读数模型。Id 使用 LHM Identifier，保证固定项跨刷新稳定。</summary>
public sealed record SensorReading(
    string Id,
    string Group,
    string Name,
    string Source,
    SensorKind Kind,
    double Value,
    string Unit,
    string HardwareName,
    string ShortLabel,
    string? MergedLoadText = null)
{
    public double? TemperatureC => Kind == SensorKind.Temperature ? Value : null;
    public double? LoadPercent => Kind == SensorKind.Load ? Value : null;

    public string DisplayValue => Kind switch
    {
        SensorKind.Temperature => $"{Math.Round(Value)}°",
        SensorKind.Load => $"{Math.Round(Value)}%",
        SensorKind.Clock => $"{Math.Round(Value)}M",
        SensorKind.Power => Value >= 100 ? $"{Math.Round(Value)}W" : $"{Value:0.#}W",
        SensorKind.Fan => $"{Math.Round(Value)}R",
        SensorKind.Data => $"{Value:0.#}",
        SensorKind.Voltage => $"{Value:0.##}V",
        SensorKind.Level => $"{Math.Round(Value)}%",
        SensorKind.Flow => $"{Value:0.#}L",
        SensorKind.Control => $"{Math.Round(Value)}%",
        SensorKind.Factor => $"{Value:0.##}",
        SensorKind.Energy => $"{Value:0.#}mWh",
        SensorKind.Noise => $"{Value:0.#}dBA",
        SensorKind.TimeSpan => $"{Value:0.#}s",
        _ => $"{Value:0.##}",
    };

    /// <summary>列表标题：数值优先、固定宽度右对齐风格，名称在副标题。</summary>
    public string FormatListTitle()
    {
        var marker = Kind == SensorKind.Temperature
            ? ThresholdPolicy.Marker(ThresholdPolicy.Classify(Value))
            : string.Empty;
        return $"{DisplayValue}{marker}";
    }

    /// <summary>Dock 标题：只放关键数值（±占用），避免长名称截断。</summary>
    public string FormatDockTitle(bool mergeLoad)
    {
        var marker = Kind == SensorKind.Temperature
            ? ThresholdPolicy.Marker(ThresholdPolicy.Classify(Value))
            : string.Empty;
        var load = mergeLoad && !string.IsNullOrEmpty(MergedLoadText) ? $" {MergedLoadText}" : string.Empty;
        return $"{DisplayValue}{load}{marker}";
    }

    /// <summary>Dock Compact 隐藏副标题时，加 1–2 字分组前缀。</summary>
    public string FormatDockCompactTitle(bool mergeLoad)
    {
        var prefix = Group switch
        {
            "处理器" => "C",
            "显卡" => "G",
            "存储" => "S",
            "主板" => "M",
            "内存" => "R",
            "电池" => "B",
            _ => "#",
        };
        return $"{prefix} {FormatDockTitle(mergeLoad)}";
    }

    public string FormatTitle(bool mergeLoad) => FormatDockTitle(mergeLoad);
}

public sealed record SensorSnapshot(
    DateTimeOffset Timestamp,
    IReadOnlyList<SensorReading> Sensors,
    bool IsStale)
{
    public SensorReading? Find(string id) => Sensors.FirstOrDefault(s => s.Id == id);

    public double PeakTemperatureC =>
        Sensors.Where(s => s.Kind == SensorKind.Temperature && s.Value is >= -50 and <= 200)
               .Select(s => s.Value)
               .DefaultIfEmpty(0)
               .Max();
}

public static class ThresholdPolicy
{
    public static double WarmC { get; set; } = 70;
    public static double HotC { get; set; } = 80;
    public static double CriticalC { get; set; } = 90;

    public static HeatLevel Classify(double? temperatureC)
    {
        if (temperatureC is null or <= 0 or > 150) return HeatLevel.Normal;
        var t = temperatureC.Value;
        if (t >= CriticalC) return HeatLevel.Critical;
        if (t >= HotC) return HeatLevel.Hot;
        if (t >= WarmC) return HeatLevel.Warm;
        return HeatLevel.Normal;
    }

    public static string Marker(HeatLevel level) => level switch
    {
        HeatLevel.Critical => " ⚠",
        HeatLevel.Hot => " ▲",
        HeatLevel.Warm => " ·",
        _ => string.Empty,
    };

    public static bool IsAlert(HeatLevel level) => level is HeatLevel.Hot or HeatLevel.Critical;
}

/// <summary>按传感器 Id 的历史环缓冲（内存，不落盘）。</summary>
public sealed class HistoryStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Queue<(long Ticks, double Value)>> _series = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _capacity;

    public HistoryStore(int capacity = 180) => _capacity = capacity;

    public void Push(string id, DateTimeOffset ts, double value)
    {
        if (string.IsNullOrEmpty(id) || !double.IsFinite(value)) return;
        lock (_gate)
        {
            if (!_series.TryGetValue(id, out var q))
            {
                q = new Queue<(long, double)>(_capacity);
                _series[id] = q;
            }

            q.Enqueue((ts.UtcTicks, value));
            while (q.Count > _capacity)
                q.Dequeue();
        }
    }

    public IReadOnlyList<(DateTimeOffset Ts, double Value)> Snapshot(string id)
    {
        lock (_gate)
        {
            if (!_series.TryGetValue(id, out var q) || q.Count == 0)
                return [];
            return q.Select(p => (new DateTimeOffset(p.Ticks, TimeSpan.Zero), p.Value)).ToList();
        }
    }

    public int Count(string id)
    {
        lock (_gate)
            return _series.TryGetValue(id, out var q) ? q.Count : 0;
    }
}
