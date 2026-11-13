# Proposed Modification Plan: Japanese (ja-JP) Localization Support

Thanks to the extensible localization framework we established in Phases 1–3, adding Japanese (`ja-JP`) requires only registering a new provider in C# and adding a locale entry to the web dashboard catalog—zero structural refactoring required.

Per our Code Modification and Review Policy, here is the complete plan and code diffs for your review and explicit approval before modifying any files in `HardwareMonitorCmdPal.Localized`.

---

### Files Affected

| Target File | Change Type | Purpose |
| :--- | :--- | :--- |
| `Localization/JaJpLocalizationProvider.cs` | **NEW** | Implements `ILocalizationProvider` for Japanese (`ja-JP` / 日本語). |
| `Localization/Loc.cs` | Modify | Adds `JaJp` provider instance and culture routing for `ja` / `ja-JP`. |
| `Assets/app.js` | Modify | Registers `ja-JP` in `LOCALES`, updates 3-way dynamic hover perspective matrix, and adds Japanese demo data. |

---

### Detailed Code Diffs

#### 1. New File: `HardwareMonitorExtension/Localization/JaJpLocalizationProvider.cs`
```csharp
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
        "Processor" or "处理器" => GroupProcessor,
        "Graphics" or "显卡" => GroupGraphics,
        "Storage" or "存储" => GroupStorage,
        "Motherboard" or "主板" => GroupMotherboard,
        "Memory" or "内存" => GroupMemory,
        "Battery" or "电池" => GroupBattery,
        "Network" or "网络" => GroupNetwork,
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
}
```

---

#### 2. Mod: `HardwareMonitorExtension/Localization/Loc.cs`
```diff
--- HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Localization/Loc.cs
+++ HardwareMonitorCmdPal.Localized/HardwareMonitorExtension/Localization/Loc.cs
@@ -7,2 +7,3 @@
     private static readonly EnUsLocalizationProvider EnUs = new();
     private static readonly ZhHansLocalizationProvider ZhHans = new();
+    private static readonly JaJpLocalizationProvider JaJp = new();
     private static ILocalizationProvider _current = ResolveFromCulture(CultureInfo.CurrentUICulture);
@@ -34,4 +35,6 @@
         var name = culture.Name.ToLowerInvariant();
         if (name.StartsWith("zh"))
             return ZhHans;
+        if (name.StartsWith("ja"))
+            return JaJp;
         return EnUs;
     }
```

---

#### 3. Mod: `HardwareMonitorExtension/Assets/app.js`
1. Expand the 3-way interactive hover matrix for `LOCALES`:
   - Under `"en-US"`:
     - `names: { "en-US": "English (US)", "zh-CN": "英语 (美国)", "ja-JP": "英語 (米国)" }`
   - Under `"zh-CN"`:
     - `names: { "en-US": "Chinese (Simplified)", "zh-CN": "简体中文", "ja-JP": "中国語 (簡体字)" }`
   - Add `"ja-JP"` entry:
     - `names: { "en-US": "Japanese", "zh-CN": "日语", "ja-JP": "日本語" }`
     - Full Japanese translations for KPIs, buttons, groups, and status lines:
```javascript
    "ja-JP": {
      code: "ja-JP",
      nativeName: "日本語",
      names: {
        "en-US": "Japanese",
        "zh-CN": "日语",
        "ja-JP": "日本語",
      },
      desc: "Japanese",
      strings: {
        metaInit: "リアルタイム サンプリング · 準備中…",
        metaLive: "リアルデータ · PawnIO/LHM",
        metaDemo: "デモ データ（live-data.js 未検出）",
        metaPaused: "一時停止中",
        metaUpdated: "更新",
        metaItems: "項目",
        metaStale: "リアルデータ更新停止（拡張機能を実行したままにしてください）· 最終 ",
        stress: "負荷テスト",
        pause: "一時停止",
        resume: "再開",
        dockTitle: "コマンド パレット · Dock",
        allSensors: "すべての測定値",
        groupStorage: "ストレージ",
        groupMisc: "マザーボード / ファン / メモリ",
        detailedMetrics: "詳細テーブル",
        tableNote: "行をクリックしてメイン グラフを切り替え",
        thSensor: "センサー",
        thGroup: "グループ",
        thKind: "種類",
        thCurrent: "現在値",
        thSource: "ソース",
        pin: "固定",
        pinned: "固定済み",
        points: "件",
        kpi: {
          cpuTemp: "CPU 温度",
          cpuPower: "CPU 電力",
          cpuLoad: "CPU 使用率",
          gpuTemp: "GPU 温度",
          diskTemp: "SSD 温度",
          fan: "ファン",
          motherboard: "マザーボード",
          memory: "メモリ",
        },
        groups: {
          Processor: "プロセッサ",
          Graphics: "グラフィックス",
          Storage: "ストレージ",
          Motherboard: "マザーボード",
          Memory: "メモリ",
          Battery: "バッテリー",
          Network: "ネットワーク",
          Other: "その他",
        },
      },
    },
```
2. In `detectLocale()`: add match for `raw.startsWith("ja") => "ja-JP"`.
3. In `demoSensors()`: add Japanese localized sensor labels when running in demo mode.
