// Copyright (c) HardwareMonitor. MIT license.

using HardwareMonitorExtension.Pages;
using HardwareMonitorExtension.Services;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HardwareMonitorExtension.Dock;

/// <summary>
/// 快捷栏固定读数。
/// 性能：不再每秒生成 PNG 火花线；仅原地改 Title/Icon 字符串。
/// 布局：Title 只含数值（固定短宽），Subtitle 为名称，避免关键读数被截断。
/// </summary>
public sealed partial class PinnedSensorsDockBand : WrappedDockItem, IDisposable
{
    private readonly SensorHub _hub;
    private readonly Lock _updateLock = new();
    private readonly Dictionary<string, ListItem> _items = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;
    private bool _hooked;

    public PinnedSensorsDockBand(SensorHub hub)
        : base([], "hardwaremonitor.pinned_band", "硬件读数")
    {
        _hub = hub;
        Icon = new IconInfo("🌡");
        EnsureHubHook();
        _hub.Pinned.Changed += (_, _) => RebuildItems();
        RebuildItems();
    }

    private void EnsureHubHook()
    {
        if (_hooked) return;
        _hooked = true;
        _hub.SnapshotUpdated += (_, _) => ApplyFromSnapshot(_hub.Snapshot);
    }

    private void RebuildItems()
    {
        lock (_updateLock)
        {
            if (_disposed || _hub.Pinned is null) return;
            var snap = _hub.Snapshot;
            var list = new List<ListItem>();
            foreach (var id in _hub.Pinned.Ids)
            {
                if (!_items.TryGetValue(id, out var item))
                {
                    item = new ListItem(new SensorChartPage(_hub, id));
                    _items[id] = item;
                }

                ApplyItem(item, id, snap);
                list.Add(item);
            }

            var keep = new HashSet<string>(_hub.Pinned.Ids, StringComparer.OrdinalIgnoreCase);
            foreach (var key in _items.Keys.Where(k => !keep.Contains(k)).ToList())
                _items.Remove(key);

            Items = [.. list];
        }
    }

    private void ApplyFromSnapshot(SensorSnapshot snap)
    {
        lock (_updateLock)
        {
            if (_disposed) return;
            foreach (var (id, item) in _items)
                ApplyItem(item, id, snap);
        }
    }

    private void ApplyItem(ListItem item, string id, SensorSnapshot snap)
    {
        var sensor = snap.Find(id);
        if (sensor is null)
        {
            item.Title = "--";
            item.Subtitle = "暂无数据";
            item.Icon = new IconInfo("🌡");
            return;
        }

        // 数值在 Title（短、右对齐友好）；名称在 Subtitle（Dock Compact 会隐藏）
        item.Title = sensor.FormatDockTitle(_hub.MergeLoadInDock);
        item.Subtitle = $"{sensor.ShortLabel} · {sensor.Name}";
        item.Icon = IconFor(sensor);
    }

    private static IconInfo IconFor(SensorReading sensor)
    {
        if (sensor.Kind != SensorKind.Temperature)
            return new IconInfo("📈");
        var level = ThresholdPolicy.Classify(sensor.Value);
        return level switch
        {
            HeatLevel.Critical => new IconInfo("🔥"),
            HeatLevel.Hot => new IconInfo("⚠"),
            _ => new IconInfo("🌡"),
        };
    }

    public void Dispose()
    {
        lock (_updateLock)
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
