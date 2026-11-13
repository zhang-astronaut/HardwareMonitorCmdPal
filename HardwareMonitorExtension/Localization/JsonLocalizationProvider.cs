using System.Text.Json;

namespace HardwareMonitorExtension.Localization;

public sealed class JsonLocalizationProvider : ILocalizationProvider
{
    private readonly Dictionary<string, string> _strings = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILocalizationProvider? _fallback;

    public string LocaleCode { get; }
    public string NativeName { get; }

    public JsonLocalizationProvider(string jsonContent, string fallbackLocaleCode, ILocalizationProvider? fallback = null)
    {
        _fallback = fallback;

        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            foreach (var top in doc.RootElement.EnumerateObject())
            {
                if (top.Value.ValueKind == JsonValueKind.Object)
                {
                    // Headings: "Shared", "HardwareMonitorSettingsManager", etc.
                    foreach (var prop in top.Value.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.String)
                        {
                            var val = prop.Value.GetString() ?? string.Empty;
                            _strings[$"{top.Name}:{prop.Name}"] = val;
                            _strings.TryAdd(prop.Name, val);
                        }
                    }
                }
                else if (top.Value.ValueKind == JsonValueKind.String)
                {
                    var val = top.Value.GetString() ?? string.Empty;
                    _strings[top.Name] = val;
                }
            }
        }
        catch
        {
            // Parse error: gracefully relies on fallback
        }

        LocaleCode = GetValue(nameof(LocaleCode), fallbackLocaleCode);
        NativeName = GetValue(nameof(NativeName), LocaleCode);
    }

    private string GetValue(string key, string defaultValue = "")
    {
        if (_strings.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
        {
            return val;
        }

        if (_fallback != null)
        {
            var fallbackProp = typeof(ILocalizationProvider).GetProperty(key);
            if (fallbackProp?.GetValue(_fallback) is string fallbackStr && !string.IsNullOrWhiteSpace(fallbackStr))
            {
                return fallbackStr;
            }
        }

        return defaultValue;
    }

    // Commands & Identity
    public string ExtensionDisplayName => GetValue(nameof(ExtensionDisplayName), "Hardware Monitor");
    public string AllSensorsSubtitle => GetValue(nameof(AllSensorsSubtitle));
    public string OpenDashboardTitle => GetValue(nameof(OpenDashboardTitle));
    public string DashboardSubtitle => GetValue(nameof(DashboardSubtitle));
    public string InstallHelperTitle => GetValue(nameof(InstallHelperTitle));
    public string InstallHelperSubtitle => GetValue(nameof(InstallHelperSubtitle));

    // Action Commands
    public string ToggleCompactName => GetValue(nameof(ToggleCompactName));
    public string PinToggleName => GetValue(nameof(PinToggleName));
    public string ViewChartName => GetValue(nameof(ViewChartName));

    // Settings
    public string RefreshIntervalLabel => GetValue(nameof(RefreshIntervalLabel));
    public string RefreshIntervalDesc => GetValue(nameof(RefreshIntervalDesc));
    public string HotThresholdLabel => GetValue(nameof(HotThresholdLabel));
    public string HotThresholdDesc => GetValue(nameof(HotThresholdDesc));
    public string CriticalThresholdLabel => GetValue(nameof(CriticalThresholdLabel));
    public string CriticalThresholdDesc => GetValue(nameof(CriticalThresholdDesc));
    public string CompactDefaultLabel => GetValue(nameof(CompactDefaultLabel));
    public string CompactDefaultDesc => GetValue(nameof(CompactDefaultDesc));
    public string MergeLoadLabel => GetValue(nameof(MergeLoadLabel));
    public string MergeLoadDesc => GetValue(nameof(MergeLoadDesc));

    // Dock Band
    public string DockBandTitle => GetValue(nameof(DockBandTitle));
    public string NoDataSubtitle => GetValue(nameof(NoDataSubtitle));

    // List Page
    public string ListPageTitle => GetValue(nameof(ListPageTitle));
    public string ListPageAction => GetValue(nameof(ListPageAction));
    public string SearchPlaceholder => GetValue(nameof(SearchPlaceholder));
    public string StaleSuffix => GetValue(nameof(StaleSuffix));
    public string ListFooter(int total, int tempCount, int pinCount, double peak, string marker) =>
        string.Format(GetValue(nameof(ListFooter), "{0} items · Temp {1} · Pinned {2} · Peak {3}°C{4}"),
            total, tempCount, pinCount, Math.Round(peak), marker);

    // Chart & Markdown
    public string ChartPageTitle => GetValue(nameof(ChartPageTitle));
    public string ChartPageAction => GetValue(nameof(ChartPageAction));
    public string NotFoundMarkdown(string sensorId) =>
        string.Format(GetValue(nameof(NotFoundMarkdown), "# Reading Not Found\n\n`{0}` is not in current snapshot."), sensorId);
    public string SourceLabel => GetValue(nameof(SourceLabel));
    public string HardwareLabel => GetValue(nameof(HardwareLabel));
    public string GroupLabel => GetValue(nameof(GroupLabel));
    public string HistoryPointsLabel => GetValue(nameof(HistoryPointsLabel));
    public string SamplingNotice => GetValue(nameof(SamplingNotice));
    public string TrendHeading => GetValue(nameof(TrendHeading));
    public string ChartMeta(string displayValue, string unit, int windowMin, int sampled, int total) =>
        string.Format(GetValue(nameof(ChartMeta), "Current **{0}** · Unit `{1}` · Window ~{2} min · Points {3}/{4}"),
            displayValue, unit, windowMin, sampled, total);
    public string Thresholds(double warm, double hot, double crit) =>
        string.Format(GetValue(nameof(Thresholds), "Thresholds: Warm {0:0}° · Hot {1:0}° · Critical {2:0}°"),
            warm, hot, crit);
    public string RecentSamplesHeading => GetValue(nameof(RecentSamplesHeading));
    public string TableHeaderTime => GetValue(nameof(TableHeaderTime));
    public string TableHeaderValue => GetValue(nameof(TableHeaderValue));

    // GDI+ Chart Renderer
    public string ChartNotEnoughPoints => GetValue(nameof(ChartNotEnoughPoints));
    public string ChartHotLabel => GetValue(nameof(ChartHotLabel));
    public string ChartCritLabel => GetValue(nameof(ChartCritLabel));
    public string ChartRange(double last, string unit, double min, double max, int points) =>
        string.Format(GetValue(nameof(ChartRange), "Current {0:0}{1}  ·  Range {2:0}–{3:0}  ·  Points {4}"),
            last, unit, min, max, points);

    // Hardware Groups
    public string GroupProcessor => GetValue(nameof(GroupProcessor), "Processor");
    public string GroupGraphics => GetValue(nameof(GroupGraphics), "Graphics");
    public string GroupStorage => GetValue(nameof(GroupStorage), "Storage");
    public string GroupMotherboard => GetValue(nameof(GroupMotherboard), "Motherboard");
    public string GroupMemory => GetValue(nameof(GroupMemory), "Memory");
    public string GroupBattery => GetValue(nameof(GroupBattery), "Battery");
    public string GroupNetwork => GetValue(nameof(GroupNetwork), "Network");
    public string GroupOther => GetValue(nameof(GroupOther), "Other");

    public string GetGroupDisplayName(string groupKey) => groupKey switch
    {
        "Processor" or "处理器" or "プロセッサ" => GroupProcessor,
        "Graphics" or "显卡" or "グラフィックス" => GroupGraphics,
        "Storage" or "存储" or "ストレージ" => GroupStorage,
        "Motherboard" or "主板" or "マザーボード" => GroupMotherboard,
        "Memory" or "内存" or "メモリ" => GroupMemory,
        "Battery" or "电池" or "バッテリー" => GroupBattery,
        "Network" or "网络" or "ネットワーク" => GroupNetwork,
        _ => GroupOther,
    };

    // Backend & Status
    public string BackendInitializing => GetValue(nameof(BackendInitializing));
    public string BackendLhmNoPawn => GetValue(nameof(BackendLhmNoPawn));
    public string BackendLhmWithPawn => GetValue(nameof(BackendLhmWithPawn));
    public string BackendLhmFailNoPawn => GetValue(nameof(BackendLhmFailNoPawn));
    public string BackendLhmFailPawn => GetValue(nameof(BackendLhmFailPawn));
    public string StatusCannotRead => GetValue(nameof(StatusCannotRead));
    public string StatusCheckPerms => GetValue(nameof(StatusCheckPerms));
    public string StatusNoData => GetValue(nameof(StatusNoData));
    public string StatusTitle => GetValue(nameof(StatusTitle));
    public string AcpiThermalZone(string zoneName) =>
        string.Format(GetValue(nameof(AcpiThermalZone), "ACPI Thermal Zone {0}"), zoneName);
    public string CpuPackageTemp => GetValue(nameof(CpuPackageTemp));
    public string CpuPackage => GetValue(nameof(CpuPackage));
    public string CpuTotalLoad => GetValue(nameof(CpuTotalLoad));
    public string CpuPackagePowerEst => GetValue(nameof(CpuPackagePowerEst));
    public string CpuPower => GetValue(nameof(CpuPower));

    // Sensor Short Labels & Formatting
    public string ShortLabelPackage => GetValue(nameof(ShortLabelPackage));
    public string ShortLabelHotSpot => GetValue(nameof(ShortLabelHotSpot));
    public string ShortLabelTemp => GetValue(nameof(ShortLabelTemp));
    public string EstimatedSuffix => GetValue(nameof(EstimatedSuffix));
    public string FormatEstimatedName(string baseName) =>
        baseName.Contains("(Estimated)", StringComparison.OrdinalIgnoreCase) ||
        baseName.Contains("估算") ||
        baseName.Contains("推定")
            ? baseName
            : $"{baseName}{EstimatedSuffix}";
}
