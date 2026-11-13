// Copyright (c) HardwareMonitor. MIT license.

using HardwareMonitorExtension.Localization;

namespace HardwareMonitorExtension.Services;

public static class HardwareGroups
{
    public const string Processor = "Processor";
    public const string Graphics = "Graphics";
    public const string Storage = "Storage";
    public const string Motherboard = "Motherboard";
    public const string Memory = "Memory";
    public const string Battery = "Battery";
    public const string Network = "Network";
    public const string Other = "Other";

    public static bool IsProcessor(string? group) =>
        group is Processor or "处理器" or "プロセッサ";

    public static bool IsGraphics(string? group) =>
        group is Graphics or "显卡" or "グラフィックス";

    public static bool IsStorage(string? group) =>
        group is Storage or "存储" or "ストレージ";

    public static bool IsMotherboard(string? group) =>
        group is Motherboard or "主板" or "マザーボード";

    public static bool IsMemory(string? group) =>
        group is Memory or "内存" or "メモリ";

    public static bool IsBattery(string? group) =>
        group is Battery or "电池" or "バッテリー";

    public static bool IsNetwork(string? group) =>
        group is Network or "网络" or "ネットワーク";

    /// <summary>排序权重：CPU > 内存 > GPU > 存储 > 主板 > 电池 > 网络 > 其他</summary>
    /// <summary>Sorting rank: CPU > Memory > GPU > Storage > Motherboard > Battery > Network > Other</summary>
    /// <summary>ソート優先度：CPU > メモリ > GPU > ストレージ > マザーボード > バッテリー > ネットワーク > その他</summary>
    public static int Rank(string? group) => group switch
    {
        _ when IsProcessor(group) => 0,
        _ when IsMemory(group) => 1,
        _ when IsGraphics(group) => 2,
        _ when IsStorage(group) => 3,
        _ when IsMotherboard(group) => 4,
        _ when IsBattery(group) => 5,
        _ when IsNetwork(group) => 6,
        _ => 99,
    };
}

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
/// <summary>Unified reading model. Id uses LHM Identifier to ensure pinned items remain stable across refreshes.</summary>
/// <summary>統合測定値モデル。Id には LHM Identifier を使用し、更新をまたいでも固定項目の同一性を維持します。</summary>
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
    public string LocalizedGroup => Loc.Current.GetGroupDisplayName(Group);

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
    /// <summary>List title: value-first, fixed-width right-aligned style; name in subtitle.</summary>
    /// <summary>リスト タイトル：数値優先、固定幅右揃えスタイル、名称はサブタイトルに表示。</summary>
    public string FormatListTitle()
    {
        var marker = Kind == SensorKind.Temperature
            ? ThresholdPolicy.Marker(ThresholdPolicy.Classify(Value))
            : string.Empty;
        return $"{DisplayValue}{marker}";
    }

    /// <summary>Dock 标题：只放关键数值（±占用），避免长名称截断。</summary>
    /// <summary>Dock title: only key values (± load) to prevent long names from truncating.</summary>
    /// <summary>Dock タイトル：長い名称の省略を防ぐため、主要な数値（±使用率）のみを表示。</summary>
    public string FormatDockTitle(bool mergeLoad)
    {
        var marker = Kind == SensorKind.Temperature
            ? ThresholdPolicy.Marker(ThresholdPolicy.Classify(Value))
            : string.Empty;
        var load = mergeLoad && !string.IsNullOrEmpty(MergedLoadText) ? $" {MergedLoadText}" : string.Empty;
        return $"{DisplayValue}{load}{marker}";
    }

    /// <summary>Dock Compact 隐藏副标题时，加 1–2 字分组前缀。</summary>
    /// <summary>Dock Compact: adds a 1-2 character group prefix when subtitle is hidden.</summary>
    /// <summary>Dock Compact：サブタイトルを非表示にする際、1〜2 文字のグループ プレフィックスを付加。</summary>
    public string FormatDockCompactTitle(bool mergeLoad)
    {
        var prefix = Group switch
        {
            _ when HardwareGroups.IsProcessor(Group) => "C",
            _ when HardwareGroups.IsGraphics(Group) => "G",
            _ when HardwareGroups.IsStorage(Group) => "S",
            _ when HardwareGroups.IsMotherboard(Group) => "M",
            _ when HardwareGroups.IsMemory(Group) => "R",
            _ when HardwareGroups.IsBattery(Group) => "B",
            _ when HardwareGroups.IsNetwork(Group) => "N",
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
/// <summary>Historical circular buffer by sensor Id (in-memory, no disk persistence).</summary>
/// <summary>センサー Id ごとの履歴リング バッファー（インメモリ、ディスク書き込みなし）。</summary>
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
