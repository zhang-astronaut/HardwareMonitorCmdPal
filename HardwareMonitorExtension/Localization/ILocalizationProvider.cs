namespace HardwareMonitorExtension.Localization;

public interface ILocalizationProvider
{
    string LocaleCode { get; }
    string NativeName { get; }

    // Commands & Identity
    string ExtensionDisplayName { get; }
    string AllSensorsSubtitle { get; }
    string OpenDashboardTitle { get; }
    string DashboardSubtitle { get; }
    string InstallHelperTitle { get; }
    string InstallHelperSubtitle { get; }

    // Action Commands
    string ToggleCompactName { get; }
    string PinToggleName { get; }
    string ViewChartName { get; }

    // Settings
    string RefreshIntervalLabel { get; }
    string RefreshIntervalDesc { get; }
    string HotThresholdLabel { get; }
    string HotThresholdDesc { get; }
    string CriticalThresholdLabel { get; }
    string CriticalThresholdDesc { get; }
    string CompactDefaultLabel { get; }
    string CompactDefaultDesc { get; }
    string MergeLoadLabel { get; }
    string MergeLoadDesc { get; }

    // Dock Band
    string DockBandTitle { get; }
    string NoDataSubtitle { get; }

    // List Page
    string ListPageTitle { get; }
    string ListPageAction { get; }
    string SearchPlaceholder { get; }
    string StaleSuffix { get; }
    string ListFooter(int total, int tempCount, int pinCount, double peak, string marker);

    // Chart & Markdown
    string ChartPageTitle { get; }
    string ChartPageAction { get; }
    string NotFoundMarkdown(string sensorId);
    string SourceLabel { get; }
    string HardwareLabel { get; }
    string GroupLabel { get; }
    string HistoryPointsLabel { get; }
    string SamplingNotice { get; }
    string TrendHeading { get; }
    string ChartMeta(string displayValue, string unit, int windowMin, int sampled, int total);
    string Thresholds(double warm, double hot, double crit);
    string RecentSamplesHeading { get; }
    string TableHeaderTime { get; }
    string TableHeaderValue { get; }

    // GDI+ Chart Renderer
    string ChartNotEnoughPoints { get; }
    string ChartHotLabel { get; }
    string ChartCritLabel { get; }
    string ChartRange(double last, string unit, double min, double max, int points);

    // Hardware Groups
    string GroupProcessor { get; }
    string GroupGraphics { get; }
    string GroupStorage { get; }
    string GroupMotherboard { get; }
    string GroupMemory { get; }
    string GroupBattery { get; }
    string GroupNetwork { get; }
    string GroupOther { get; }
    string GetGroupDisplayName(string groupKey);

    // Backend & Status
    string BackendInitializing { get; }
    string BackendLhmNoPawn { get; }
    string BackendLhmWithPawn { get; }
    string BackendLhmFailNoPawn { get; }
    string BackendLhmFailPawn { get; }
    string StatusCannotRead { get; }
    string StatusCheckPerms { get; }
    string StatusNoData { get; }
    string StatusTitle { get; }
    string AcpiThermalZone(string zoneName);
    string CpuPackageTemp { get; }
    string CpuPackage { get; }
    string CpuTotalLoad { get; }
    string CpuPackagePowerEst { get; }
    string CpuPower { get; }
}
