namespace HardwareMonitorExtension.Localization;

public sealed class EnUsLocalizationProvider : ILocalizationProvider
{
    public string LocaleCode => "en-US";
    public string NativeName => "English (US)";

    public string ExtensionDisplayName => "Hardware Monitor";
    public string AllSensorsSubtitle => "All sensors · Can be pinned to dock";
    public string OpenDashboardTitle => "Open Real-Time Dashboard";
    public string DashboardSubtitle => "btop-style offline index.html";
    public string InstallHelperTitle => "Install CPU Temp Startup Sampling (One-time)";
    public string InstallHelperSubtitle => "Runs at startup automatically; normal user CPU Tctl is 0";

    public string ToggleCompactName => "Toggle Compact Layout";
    public string PinToggleName => "Pin to Dock / Unpin";
    public string ViewChartName => "View Trend Chart";

    public string RefreshIntervalLabel => "Content refresh interval (ms)";
    public string RefreshIntervalDesc => "500–3000, active when page is visible";
    public string HotThresholdLabel => "Overheat threshold (°C)";
    public string HotThresholdDesc => "Shows orange when reaching this temperature";
    public string CriticalThresholdLabel => "Critical threshold (°C)";
    public string CriticalThresholdDesc => "Shows red and triggers alert when reaching this temperature";
    public string CompactDefaultLabel => "Default compact layout";
    public string CompactDefaultDesc => "Hide secondary fields when opened (Compact)";
    public string MergeLoadLabel => "Merge usage in Dock";
    public string MergeLoadDesc => "Append usage percentage after temperature reading to save space";

    public string DockBandTitle => "Hardware Readings";
    public string NoDataSubtitle => "No data available";

    public string ListPageTitle => "Hardware Monitor";
    public string ListPageAction => "Open";
    public string SearchPlaceholder => "Filter sensors…";
    public string StaleSuffix => " · May be stale";
    public string ListFooter(int total, int tempCount, int pinCount, double peak, string marker) =>
        $"{total} items · Temp {tempCount} · Pinned {pinCount} · Peak {Math.Round(peak)}°C{marker}";

    public string ChartPageTitle => "Reading Details";
    public string ChartPageAction => "View";
    public string NotFoundMarkdown(string sensorId) => $"# Reading Not Found\n\n`{sensorId}` is not in current snapshot.";
    public string SourceLabel => "Source:";
    public string HardwareLabel => "Hardware:";
    public string GroupLabel => "Group:";
    public string HistoryPointsLabel => "History points:";
    public string SamplingNotice => "_Sampling in progress, trend will appear shortly…_";
    public string TrendHeading => "## Trend (asciichart)";
    public string ChartMeta(string displayValue, string unit, int windowMin, int sampled, int total) =>
        $"Current **{displayValue}** · Unit `{unit}` · Window ~{windowMin} min · Points {sampled}/{total}";
    public string Thresholds(double warm, double hot, double crit) =>
        $"Thresholds: Warm {warm:0}° · Hot {hot:0}° · Critical {crit:0}°";
    public string RecentSamplesHeading => "## Recent Samples";
    public string TableHeaderTime => "Time";
    public string TableHeaderValue => "Value";

    public string ChartNotEnoughPoints => "Insufficient samples, accumulating…";
    public string ChartHotLabel => "Hot";
    public string ChartCritLabel => "Critical";
    public string ChartRange(double last, string unit, double min, double max, int points) =>
        $"Current {last:0}{unit}  ·  Range {min:0}–{max:0}  ·  Points {points}";

    public string GroupProcessor => "Processor";
    public string GroupGraphics => "Graphics";
    public string GroupStorage => "Storage";
    public string GroupMotherboard => "Motherboard";
    public string GroupMemory => "Memory";
    public string GroupBattery => "Battery";
    public string GroupNetwork => "Network";
    public string GroupOther => "Other";

    public string GetGroupDisplayName(string groupKey) => groupKey switch
    {
        "Processor" or "处理器" => GroupProcessor,
        "Graphics" or "显卡" => GroupGraphics,
        "Storage" or "存储" => GroupStorage,
        "Motherboard" or "主板" => GroupMotherboard,
        "Memory" or "内存" => GroupMemory,
        "Battery" or "电池" => GroupBattery,
        "Network" or "网络" => GroupNetwork,
        _ => GroupOther,
    };

    public string BackendInitializing => "Initializing";
    public string BackendLhmNoPawn => "LibreHardwareMonitor (No PawnIO)";
    public string BackendLhmWithPawn => "LibreHardwareMonitor + PawnIO installed";
    public string BackendLhmFailNoPawn => "LHM open failed · No PawnIO";
    public string BackendLhmFailPawn => "LHM open failed · PawnIO installed";
    public string StatusCannotRead => "Unable to read sensors";
    public string StatusCheckPerms => "Please check PawnIO / Administrator privileges";
    public string StatusNoData => "No data";
    public string StatusTitle => "Status";
    public string AcpiThermalZone(string zoneName) => $"ACPI Thermal Zone {zoneName}";
    public string CpuPackageTemp => "CPU Package Temperature (Tctl)";
    public string CpuPackage => "CPU Package";
    public string CpuTotalLoad => "CPU Total Load";
    public string CpuPackagePowerEst => "CPU Package Power (Estimated)";
    public string CpuPower => "CPU Power";
}
