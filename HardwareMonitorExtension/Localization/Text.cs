// Copyright (c) HardwareMonitor. MIT license.

using System.Globalization;
using System.Resources;

[assembly: NeutralResourcesLanguage("en")]

namespace HardwareMonitorExtension.Localization;

/// <summary>
/// Display-only translations. Sensor identifiers, group matching, serialized fields and
/// settings keys retain their original values. Unknown hardware/vendor names pass through.
/// </summary>
internal static class Text
{
    private static readonly ResourceManager Resources = new(
        "HardwareMonitorExtension.Localization.Strings", typeof(Text).Assembly);

    public static string Get(string key) =>
        Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public static string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);

    // These raw values are also consumed by sampling, default pin selection and live-data.js.
    // Translate only when rendering; never write translations back into SensorReading.
    private static readonly Dictionary<string, string> DisplayKeys = new(StringComparer.Ordinal)
    {
        ["处理器"] = "GroupCpu", ["显卡"] = "GroupGpu", ["存储"] = "GroupStorage",
        ["主板"] = "GroupBoard", ["内存"] = "GroupMemory", ["电池"] = "GroupBattery",
        ["网络"] = "GroupNetwork", ["其他"] = "GroupOther", ["状态"] = "GroupStatus",
        ["CPU 占用"] = "CpuLoad", ["CPU 总占用"] = "CpuTotalLoad",
        ["CPU 封装温度 (Tctl)"] = "CpuPackageTctl", ["CPU 封装温度"] = "CpuPackageTemperature",
        ["CPU 核心均温"] = "CpuCoreTemperature", ["CPU 封装功率"] = "CpuPackagePower",
        ["CPU 封装功率（估算）"] = "CpuPackagePowerEstimated", ["CPU 核心频率"] = "CpuCoreClock",
        ["GPU 核心温度"] = "GpuCoreTemperature", ["GPU 核心占用"] = "GpuLoad",
        ["GPU 功耗"] = "GpuPower", ["SSD 温度"] = "SsdTemperature",
        ["SSD 读取速率"] = "SsdReadRate", ["主板温度"] = "BoardTemperature",
        ["CPU 风扇转速"] = "CpuFanSpeed", ["内存占用"] = "MemoryLoad",
        ["无法读取传感器"] = "SensorReadFailed", ["请检查 PawnIO / 管理员权限"] = "CheckPermissions",
        ["无数据"] = "NoData", ["demo · 模拟"] = "DemoSource",
        ["GPU Hot Spot"] = "GpuHotSpot", ["Elevated helper · LHM"] = "HelperSource",
        ["Estimated · load×TDP65"] = "EstimatedSource",
    };

    public static string DisplayGroup(string raw) => DisplayName(raw);

    public static string DisplayLabel(string raw)
    {
        if (DisplayKeys.TryGetValue(raw, out var key)) return Get(key);
        foreach (var (suffix, resource) in LabelSuffixes)
            if (raw.EndsWith(suffix, StringComparison.Ordinal))
                return Format(resource, raw[..^suffix.Length]);
        return raw;
    }

    private static readonly (string Suffix, string Resource)[] LabelSuffixes =
    [
        (" 封装", "LabelPackage"), (" 热点", "LabelHotSpot"), (" 温度", "LabelTemperature"),
        (" 功率", "LabelPower"), (" 核心", "LabelCore"), (" 频率", "LabelClock"),
        (" 读", "LabelRead"), (" 风扇", "LabelFan"),
    ];

    public static string DisplayName(string raw)
    {
        if (DisplayKeys.TryGetValue(raw, out var key)) return Get(key);
        if (raw.StartsWith("ACPI 热区 ", StringComparison.Ordinal))
            return Format("AcpiZone", raw[8..]);
        const string estimated = "（估算）";
        if (raw.EndsWith(estimated, StringComparison.Ordinal))
            return Format("EstimatedName", DisplayName(raw[..^estimated.Length]));
        return raw;
    }
}
