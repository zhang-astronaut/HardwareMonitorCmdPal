// Copyright (c) HardwareMonitor. MIT license.
// 对齐官方 Helpers/SettingsManager.cs 模式。

using System.IO;
using HardwareMonitorExtension.Services;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Foundation;

namespace HardwareMonitorExtension;

internal sealed partial class HardwareMonitorSettingsManager : JsonSettingsManager
{
    private readonly SensorHub _hub;

    private static readonly string Namespace = "HardwareMonitorExtension";

    private static string Namespaced(string propertyName) => $"{Namespace}.{propertyName}";

    private readonly TextSetting _refreshMs = new(
        Namespaced(nameof(RefreshMs)),
        "内容页刷新间隔 (ms)",
        "500–3000，页面可见时生效",
        "1000");

    private readonly TextSetting _hotC = new(
        Namespaced(nameof(HotC)),
        "过热阈值 (°C)",
        "达到该温度显示橙色",
        "80");

    private readonly TextSetting _critC = new(
        Namespaced(nameof(CriticalC)),
        "危险阈值 (°C)",
        "达到该温度显示红色并预警",
        "90");

    private readonly ToggleSetting _compactDefault = new(
        Namespaced(nameof(CompactByDefault)),
        "默认紧凑布局",
        "打开列表时隐藏次要字段（Compact）",
        false);

    private readonly ToggleSetting _mergeLoad = new(
        Namespaced(nameof(MergeLoadInDock)),
        "快捷栏合并显示占用",
        "温度读数后附加同硬件占用百分比，节省空间",
        true);

    public int RefreshMs => ParseInt(_refreshMs.Value, 1000, 500, 3000);
    public double HotC => ParseDouble(_hotC.Value, 80);
    public double CriticalC => ParseDouble(_critC.Value, 90);
    public bool CompactByDefault => _compactDefault.Value;
    public bool MergeLoadInDock => _mergeLoad.Value;

    internal static string SettingsJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("HardwareMonitorExtension");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "settings.json");
    }

    internal static string PinsJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("HardwareMonitorExtension");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "pinned-sensors.json");
    }

    public HardwareMonitorSettingsManager(SensorHub hub)
    {
        _hub = hub;
        FilePath = SettingsJsonPath();

        Settings.Add(_refreshMs);
        Settings.Add(_hotC);
        Settings.Add(_critC);
        Settings.Add(_compactDefault);
        Settings.Add(_mergeLoad);

        LoadSettings();

        Settings.SettingsChanged += OnSettingsChanged;
        ApplyToHub();
    }

    private void OnSettingsChanged(object sender, Settings e)
    {
        SaveSettings();
        ApplyToHub();
    }

    private void ApplyToHub()
    {
        ThresholdPolicy.HotC = HotC;
        ThresholdPolicy.CriticalC = CriticalC;
        _hub.SetIntervalMs(RefreshMs);
        _hub.MergeLoadInDock = MergeLoadInDock;
    }

    private static int ParseInt(string? raw, int fallback, int min, int max)
    {
        if (!int.TryParse(raw, out var v)) return fallback;
        return Math.Clamp(v, min, max);
    }

    private static double ParseDouble(string? raw, double fallback)
    {
        if (!double.TryParse(raw, out var v)) return fallback;
        return v;
    }
}
