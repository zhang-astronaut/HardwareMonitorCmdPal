// Copyright (c) HardwareMonitor. MIT license.

using HardwareMonitorExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HardwareMonitorExtension.Pages;

/// <summary>
/// 主列表。
/// 刷新策略：定时器只原地改 Title/Subtitle/Icon，不 RaiseItemsChanged，
/// 以免右键菜单/列表滚动位置被重置。
/// </summary>
internal sealed partial class SensorsListPage : ListPage
{
    private readonly SensorHub _hub;
    private readonly HardwareMonitorSettingsManager _settings;
    private readonly Dictionary<string, ListItem> _itemsBySensor = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _lastTitle = new(StringComparer.OrdinalIgnoreCase);
    private readonly ListItem _footerItem = new(new NoOpCommand());
    private IListItem[] _cachedItems = [];
    private bool _compact;
    private bool _hooked;
    private bool _structureDirty = true;

    internal SensorsListPage(SensorHub hub, HardwareMonitorSettingsManager settings)
    {
        _hub = hub;
        _settings = settings;
        _compact = settings.CompactByDefault;
        Icon = new IconInfo("🌡");
        Title = "硬件监视器";
        Name = "打开";
        PlaceholderText = "筛选传感器…";
        EnsureHubHook();
    }

    private void EnsureHubHook()
    {
        if (_hooked) return;
        _hooked = true;
        _hub.SnapshotUpdated += (_, _) =>
        {
            try
            {
                // 仅在结构变化（传感器集合/紧凑切换/固定数变化）时才重建列表
                if (_structureDirty)
                {
                    _structureDirty = false;
                    RaiseItemsChanged();
                    return;
                }

                MutateInPlace(_hub.Snapshot);
            }
            catch
            {
                // ignore
            }
        };

        if (_hub.Pinned is not null)
            _hub.Pinned.Changed += (_, _) => _structureDirty = true;
    }

    public override IListItem[] GetItems()
    {
        EnsureHubHook();
        var snap = _hub.Snapshot;
        var sensors = FilterForLayout(snap.Sensors, _compact).ToList();
        var list = new List<IListItem>(sensors.Count + 1);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var sensor in sensors)
        {
            seen.Add(sensor.Id);
            var item = GetOrCreateItem(sensor.Id);
            ApplyItem(item, sensor, snap.IsStale);
            list.Add(item);
        }

        // 去掉已消失的
        foreach (var key in _itemsBySensor.Keys.Where(k => !seen.Contains(k)).ToList())
        {
            _itemsBySensor.Remove(key);
            _lastTitle.Remove(key);
        }

        UpdateFooter(snap);
        list.Add(_footerItem);
        _cachedItems = [.. list];
        _structureDirty = false;
        return _cachedItems;
    }

    private void MutateInPlace(SensorSnapshot snap)
    {
        foreach (var sensor in snap.Sensors)
        {
            if (!_itemsBySensor.TryGetValue(sensor.Id, out var item))
            {
                _structureDirty = true;
                return;
            }

            ApplyItem(item, sensor, snap.IsStale);
        }

        UpdateFooter(snap);
    }

    private static IEnumerable<SensorReading> FilterForLayout(
        IReadOnlyList<SensorReading> sensors,
        bool compact)
    {
        if (!compact)
            return sensors;

        // Compact：每组温度尽量 3 条 + 负载 + 功率；绝不丢掉 CPU 温度
        var result = new List<SensorReading>();
        foreach (var g in new[] { "处理器", "显卡", "存储", "主板", "内存", "电池", "网络", "其他" })
        {
            var temps = sensors.Where(s => s.Group == g && s.Kind == SensorKind.Temperature).ToList();
            result.AddRange(temps.Count <= 3 ? temps : temps.Take(3));
            var load = sensors.FirstOrDefault(s => s.Group == g && s.Kind == SensorKind.Load);
            if (load is not null) result.Add(load);
            var power = sensors.FirstOrDefault(s => s.Group == g && s.Kind == SensorKind.Power);
            if (power is not null) result.Add(power);
        }

        // 若过滤后没有处理器温度，退回包含全部处理器温度
        if (!result.Any(s => s.Group == "处理器" && s.Kind == SensorKind.Temperature))
            result.InsertRange(0, sensors.Where(s => s.Group == "处理器" && s.Kind == SensorKind.Temperature));

        // 其它分组漏网项：附加 Other/风扇等，避免丢指标
        foreach (var s in sensors)
        {
            if (!result.Exists(r => r.Id == s.Id))
                result.Add(s);
        }

        return result.Count > 0 ? result : sensors;
    }

    private ListItem GetOrCreateItem(string id)
    {
        if (_itemsBySensor.TryGetValue(id, out var existing))
            return existing;

        // MoreCommands 只在创建时挂一次，避免每秒重建导致右键菜单位置丢失
        var item = new ListItem(new SensorChartPage(_hub, id))
        {
            MoreCommands = BuildMoreCommands(id),
        };
        _itemsBySensor[id] = item;
        return item;
    }

    private void ApplyItem(ListItem item, SensorReading sensor, bool stale)
    {
        // 数值 Title（短）+ 名称 Subtitle，长 Identifier 不会挤掉读数
        item.Title = sensor.FormatListTitle();
        item.Subtitle = stale
            ? $"{sensor.ShortLabel} · {sensor.Name} · 可能过期"
            : $"{sensor.ShortLabel} · {sensor.Name}";

        var nextIcon = IconKey(sensor);
        if (_lastTitle.TryGetValue(sensor.Id, out var prev) && prev == nextIcon)
        {
            // icon 不变则不写，减少属性通知
        }
        else
        {
            item.Icon = IconFor(sensor);
            _lastTitle[sensor.Id] = nextIcon;
        }
    }

    private static string IconKey(SensorReading sensor) =>
        $"{sensor.Kind}:{(sensor.Kind == SensorKind.Temperature ? (int)ThresholdPolicy.Classify(sensor.Value) : 0)}";

    private static IconInfo IconFor(SensorReading sensor)
    {
        if (sensor.Kind != SensorKind.Temperature)
            return sensor.Kind switch
            {
                SensorKind.Load => new IconInfo("📊"),
                SensorKind.Fan => new IconInfo("🌀"),
                SensorKind.Power => new IconInfo("⚡"),
                _ => new IconInfo("•"),
            };

        return ThresholdPolicy.Classify(sensor.Value) switch
        {
            HeatLevel.Critical => new IconInfo("🔥"),
            HeatLevel.Hot => new IconInfo("⚠"),
            _ => new IconInfo("🌡"),
        };
    }

    private IContextItem[] BuildMoreCommands(string sensorId)
    {
        return
        [
            new CommandContextItem(new TogglePinSensorCommand(_hub, sensorId))
            {
                Title = "固定到快捷栏 / 取消固定",
                Icon = new IconInfo("📌"),
            },
            new CommandContextItem(new SensorChartPage(_hub, sensorId))
            {
                Title = "查看趋势图",
                Icon = new IconInfo("📈"),
            },
        ];
    }

    private void UpdateFooter(SensorSnapshot snap)
    {
        var peak = snap.PeakTemperatureC;
        var level = ThresholdPolicy.Classify(peak);
        var pawn = _hub.PawnIoConnected ? "PawnIO 已连接" : "PawnIO 未连接";
        var pinCount = _hub.Pinned?.Ids.Count ?? 0;
        var tempCount = snap.Sensors.Count(s => s.Kind == SensorKind.Temperature);

        _footerItem.Title = $"{snap.Sensors.Count} 项 · 温度 {tempCount} · 已固定 {pinCount} · 峰值 {Math.Round(peak)}°C{ThresholdPolicy.Marker(level)}";
        _footerItem.Subtitle = $"{_hub.BackendLabel} · {pawn} · {snap.Timestamp:HH:mm:ss}";
        _footerItem.Icon = new IconInfo("↻");
    }

    public void ToggleCompact()
    {
        _compact = !_compact;
        _structureDirty = true;
        RaiseItemsChanged();
    }

    public bool IsCompact => _compact;
}
