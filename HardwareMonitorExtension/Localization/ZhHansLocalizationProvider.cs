namespace HardwareMonitorExtension.Localization;

public sealed class ZhHansLocalizationProvider : ILocalizationProvider
{
    public string LocaleCode => "zh-Hans";
    public string NativeName => "简体中文";

    public string ExtensionDisplayName => "硬件监视器";
    public string AllSensorsSubtitle => "全部传感器 · 可固定到快捷栏";
    public string OpenDashboardTitle => "打开实时图表看板";
    public string DashboardSubtitle => "btop 风格离线 index.html";
    public string InstallHelperTitle => "安装 CPU 温度开机采样（一次）";
    public string InstallHelperSubtitle => "注册后开机自动，无需再点；普通权限 CPU Tctl 为 0";

    public string ToggleCompactName => "切换紧凑布局";
    public string PinToggleName => "固定到快捷栏 / 取消固定";
    public string ViewChartName => "查看趋势图";

    public string RefreshIntervalLabel => "内容页刷新间隔 (ms)";
    public string RefreshIntervalDesc => "500–3000，页面可见时生效";
    public string HotThresholdLabel => "过热阈值 (°C)";
    public string HotThresholdDesc => "达到该温度显示橙色";
    public string CriticalThresholdLabel => "危险阈值 (°C)";
    public string CriticalThresholdDesc => "达到该温度显示红色并预警";
    public string CompactDefaultLabel => "默认紧凑布局";
    public string CompactDefaultDesc => "打开列表时隐藏次要字段（Compact）";
    public string MergeLoadLabel => "快捷栏合并显示占用";
    public string MergeLoadDesc => "温度读数后附加同硬件占用百分比，节省空间";

    public string DockBandTitle => "硬件读数";
    public string NoDataSubtitle => "暂无数据";

    public string ListPageTitle => "硬件监视器";
    public string ListPageAction => "打开";
    public string SearchPlaceholder => "筛选传感器…";
    public string StaleSuffix => " · 可能过期";
    public string ListFooter(int total, int tempCount, int pinCount, double peak, string marker) =>
        $"{total} 项 · 温度 {tempCount} · 已固定 {pinCount} · 峰值 {Math.Round(peak)}°C{marker}";

    public string ChartPageTitle => "读数详情";
    public string ChartPageAction => "查看";
    public string NotFoundMarkdown(string sensorId) => $"# 未找到读数\n\n`{sensorId}` 不在当前快照中。";
    public string SourceLabel => "来源：";
    public string HardwareLabel => "硬件：";
    public string GroupLabel => "分组：";
    public string HistoryPointsLabel => "历史点：";
    public string SamplingNotice => "_采样中，稍候即可看到趋势…_";
    public string TrendHeading => "## 趋势（asciichart · 代码绘制，非位图）";
    public string ChartMeta(string displayValue, string unit, int windowMin, int sampled, int total) =>
        $"当前 **{displayValue}** · 单位 `{unit}` · 窗口约 {windowMin} 分钟 · 点 {sampled}/{total}";
    public string Thresholds(double warm, double hot, double crit) =>
        $"阈值：偏热 {warm:0}° · 过热 {hot:0}° · 危险 {crit:0}°";
    public string RecentSamplesHeading => "## 近期采样";
    public string TableHeaderTime => "时间";
    public string TableHeaderValue => "值";

    public string ChartNotEnoughPoints => "采样点不足，正在积累…";
    public string ChartHotLabel => "过热";
    public string ChartCritLabel => "危险";
    public string ChartRange(double last, string unit, double min, double max, int points) =>
        $"当前 {last:0}{unit}  ·  区间 {min:0}–{max:0}  ·  点数 {points}";

    public string GroupProcessor => "处理器";
    public string GroupGraphics => "显卡";
    public string GroupStorage => "存储";
    public string GroupMotherboard => "主板";
    public string GroupMemory => "内存";
    public string GroupBattery => "电池";
    public string GroupNetwork => "网络";
    public string GroupOther => "其他";

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

    public string BackendInitializing => "初始化中";
    public string BackendLhmNoPawn => "LibreHardwareMonitor（无 PawnIO）";
    public string BackendLhmWithPawn => "LibreHardwareMonitor + PawnIO 已安装";
    public string BackendLhmFailNoPawn => "LHM 打开失败 · 无 PawnIO";
    public string BackendLhmFailPawn => "LHM 打开失败 · PawnIO 已安装";
    public string StatusCannotRead => "无法读取传感器";
    public string StatusCheckPerms => "请检查 PawnIO / 管理员权限";
    public string StatusNoData => "无数据";
    public string StatusTitle => "状态";
    public string AcpiThermalZone(string zoneName) => $"ACPI 热区 {zoneName}";
    public string CpuPackageTemp => "CPU 封装温度 (Tctl)";
    public string CpuPackage => "CPU 封装";
    public string CpuTotalLoad => "CPU 总占用";
    public string CpuPackagePowerEst => "CPU 封装功率（估算）";
    public string CpuPower => "CPU 功率";

    public string ShortLabelPackage => "封装";
    public string ShortLabelHotSpot => "热点";
    public string ShortLabelTemp => "温度";
    public string EstimatedSuffix => "（估算）";
    public string FormatEstimatedName(string baseName) =>
        baseName.Contains("估算") ||
        baseName.Contains("推定") ||
        baseName.Contains("(Estimated)", StringComparison.OrdinalIgnoreCase)
            ? baseName
            : $"{baseName}（估算）";
}
