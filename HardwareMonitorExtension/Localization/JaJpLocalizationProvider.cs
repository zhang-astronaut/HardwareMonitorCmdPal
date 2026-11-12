namespace HardwareMonitorExtension.Localization;

public sealed class JaJpLocalizationProvider : ILocalizationProvider
{
    public string LocaleCode => "ja-JP";
    public string NativeName => "日本語";

    public string ExtensionDisplayName => "ハードウェア モニター";
    public string AllSensorsSubtitle => "すべてのセンサー · クイック アクセス バーに固定可能";
    public string OpenDashboardTitle => "リアルタイム ダッシュボードを開く";
    public string DashboardSubtitle => "btop スタイル オフライン index.html";
    public string InstallHelperTitle => "CPU 温度起動サンプリングのインストール（1回のみ）";
    public string InstallHelperSubtitle => "登録後は起動時に自動実行されます。通常権限の CPU Tctl は 0 です";

    public string ToggleCompactName => "コンパクト レイアウトの切り替え";
    public string PinToggleName => "ショートカット バーに固定 / 固定解除";
    public string ViewChartName => "トレンド グラフを表示";

    public string RefreshIntervalLabel => "コンテンツ ページの更新間隔 (ms)";
    public string RefreshIntervalDesc => "500–3000、ページ表示中に有効";
    public string HotThresholdLabel => "過熱しきい値 (°C)";
    public string HotThresholdDesc => "この温度に達するとオレンジ色で表示";
    public string CriticalThresholdLabel => "危険しきい値 (°C)";
    public string CriticalThresholdDesc => "この温度に達すると赤色で警告表示";
    public string CompactDefaultLabel => "デフォルトでコンパクト レイアウト";
    public string CompactDefaultDesc => "リストを開いたときに補足項目を非表示（Compact）";
    public string MergeLoadLabel => "ショートカット バーで使用率を結合表示";
    public string MergeLoadDesc => "温度の後に同じハードウェアの使用率を追加して省スペース化";

    public string DockBandTitle => "ハードウェア測定値";
    public string NoDataSubtitle => "データがありません";

    public string ListPageTitle => "ハードウェア モニター";
    public string ListPageAction => "開く";
    public string SearchPlaceholder => "センサーをフィルター…";
    public string StaleSuffix => " · 古い可能性があります";
    public string ListFooter(int total, int tempCount, int pinCount, double peak, string marker) =>
        $"{total} 項目 · 温度 {tempCount} · 固定済み {pinCount} · ピーク {Math.Round(peak)}°C{marker}";

    public string ChartPageTitle => "測定値の詳細";
    public string ChartPageAction => "表示";
    public string NotFoundMarkdown(string sensorId) => $"# 測定値が見つかりません\n\n`{sensorId}` は現在のスナップショットに存在しません。";
    public string SourceLabel => "ソース：";
    public string HardwareLabel => "ハードウェア：";
    public string GroupLabel => "グループ：";
    public string HistoryPointsLabel => "履歴ポイント：";
    public string SamplingNotice => "_サンプリング中、しばらくするとトレンドが表示されます…_";
    public string TrendHeading => "## トレンド（asciichart · コード描画）";
    public string ChartMeta(string displayValue, string unit, int windowMin, int sampled, int total) =>
        $"現在 **{displayValue}** · 単位 `{unit}` · ウィンドウ約 {windowMin} 分 · ポイント {sampled}/{total}";
    public string Thresholds(double warm, double hot, double crit) =>
        $"しきい値：微熱 {warm:0}° · 過熱 {hot:0}° · 危険 {crit:0}°";
    public string RecentSamplesHeading => "## 最近のサンプリング";
    public string TableHeaderTime => "時刻";
    public string TableHeaderValue => "値";

    public string ChartNotEnoughPoints => "サンプリング ポイントが不足しています、蓄積中…";
    public string ChartHotLabel => "過熱";
    public string ChartCritLabel => "危険";
    public string ChartRange(double last, string unit, double min, double max, int points) =>
        $"現在 {last:0}{unit}  ·  範囲 {min:0}–{max:0}  ·  ポイント数 {points}";

    public string GroupProcessor => "プロセッサ";
    public string GroupGraphics => "グラフィックス";
    public string GroupStorage => "ストレージ";
    public string GroupMotherboard => "マザーボード";
    public string GroupMemory => "メモリ";
    public string GroupBattery => "バッテリー";
    public string GroupNetwork => "ネットワーク";
    public string GroupOther => "その他";

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

    public string BackendInitializing => "初期化中";
    public string BackendLhmNoPawn => "LibreHardwareMonitor（PawnIO なし）";
    public string BackendLhmWithPawn => "LibreHardwareMonitor + PawnIO インストール済み";
    public string BackendLhmFailNoPawn => "LHM の起動に失敗 · PawnIO なし";
    public string BackendLhmFailPawn => "LHM の起動に失敗 · PawnIO インストール済み";
    public string StatusCannotRead => "センサーを読み取れません";
    public string StatusCheckPerms => "PawnIO / 管理者権限を確認してください";
    public string StatusNoData => "データなし";
    public string StatusTitle => "ステータス";
    public string AcpiThermalZone(string zoneName) => $"ACPI サーマル ゾーン {zoneName}";
    public string CpuPackageTemp => "CPU パッケージ温度 (Tctl)";
    public string CpuPackage => "CPU パッケージ";
    public string CpuTotalLoad => "CPU 合計使用率";
    public string CpuPackagePowerEst => "CPU パッケージ消費電力（推定）";
    public string CpuPower => "CPU 電力";

    public string ShortLabelPackage => "パッケージ";
    public string ShortLabelHotSpot => "ホットスポット";
    public string ShortLabelTemp => "温度";
    public string EstimatedSuffix => "（推定）";
    public string FormatEstimatedName(string baseName) =>
        baseName.Contains("推定") ||
        baseName.Contains("估算") ||
        baseName.Contains("(Estimated)", StringComparison.OrdinalIgnoreCase)
            ? baseName
            : $"{baseName}（推定）";
}
