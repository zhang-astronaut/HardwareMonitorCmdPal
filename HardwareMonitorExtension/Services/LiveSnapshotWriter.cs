// Copyright (c) HardwareMonitor. MIT license.

using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HardwareMonitorExtension.Services;

/// <summary>
/// 将采样快照写成 live-data.js，供同目录 index.html 以 script 方式加载。
/// 按需写盘：仅当看板处于活跃状态时才覆盖写文件，避免无看板时的持续磁盘写入。
/// </summary>
/// <summary>
/// Writes sample snapshot to live-data.js for script-based loading by index.html in the same directory.
/// On-demand disk writing: only overwrites when the dashboard is active, avoiding continuous disk writes when idle.
/// </summary>
/// <summary>
/// サンプリング スナップショットを live-data.js に書き込み、同ディレクトリの index.html から script 方式で読み込めるようにします。
/// オンデマンド書き込み：ダッシュボードがアクティブな時のみ上書きし、非アクティブ時の不要なディスク書き込みを防止。
/// </summary>
public static class LiveSnapshotWriter
{
    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions Json = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>打开看板后的保活窗口：即使访问时间不可靠也能先写几帧。</summary>
    /// <summary>Keep-alive window after opening dashboard: writes initial frames even if access time is unreliable.</summary>
    /// <summary>ダッシュボード起動後のキープアライブ ウィンドウ：アクセス時間が不確実でも最初の数フレームを確実に書き込みます。</summary>
    private static readonly TimeSpan OpenGrace = TimeSpan.FromSeconds(90);

    /// <summary>live-data.js 被读取后多久内视为看板仍在用。</summary>
    /// <summary>Duration within which live-data.js is considered in active use after being read.</summary>
    /// <summary>live-data.js が読み取られた後、ダッシュボードが使用中とみなされる期間。</summary>
    private static readonly TimeSpan AccessTtl = TimeSpan.FromSeconds(15);

    private static DateTimeOffset _lastWrite = DateTimeOffset.MinValue;
    private static DateTimeOffset _lastWriteUtc = DateTimeOffset.MinValue;
    private static DateTimeOffset _dashboardOpenUntil = DateTimeOffset.MinValue;

    public static string DashboardDir =>
        Utilities.BaseSettingsPath("HardwareMonitorExtension");

    public static string LiveDataPath => Path.Combine(DashboardDir, "live-data.js");

    /// <summary>用户通过扩展命令打开看板时调用，开启一段写盘窗口。</summary>
    /// <summary>Invoked when user opens dashboard via extension command, opening a disk write window.</summary>
    /// <summary>ユーザーが拡張機能コマンドからダッシュボードを開いた際に呼び出され、書き込みウィンドウを開始します。</summary>
    public static void NotifyDashboardOpen()
    {
        _dashboardOpenUntil = DateTimeOffset.UtcNow + OpenGrace;
    }

    /// <summary>
    /// 看板是否活跃：
    /// 1) 最近通过扩展命令打开过；或
    /// 2) live-data.js 在上次写入之后被读取（script 轮询会触发访问时间）。
    /// </summary>
    /// <summary>
    /// Whether dashboard is active:
    /// 1) Recently opened via extension command; or
    /// 2) live-data.js was read after last write (script polling updates access time).
    /// </summary>
    /// <summary>
    /// ダッシュボードがアクティブかどうか：
    /// 1) 最近拡張機能コマンド経由で開かれた；または
    /// 2) 前回の書き込み以降に live-data.js が読み取られた（script ポーリングによってアクセス時間が更新されます）。
    /// </summary>
    public static bool IsDashboardActive()
    {
        var now = DateTimeOffset.UtcNow;
        if (now < _dashboardOpenUntil)
            return true;

        try
        {
            var path = LiveDataPath;
            if (!File.Exists(path))
                return false;

            var access = File.GetLastAccessTimeUtc(path);
            // 写入本身可能刷新访问时间；要求访问明显晚于上次写入
            // Writing itself may refresh access time; require access to be noticeably later than last write
            // 書き込み自体がアクセス時間を更新する可能性があるため、前回の書き込みより明確に遅いアクセスを要求
            if (_lastWriteUtc > DateTimeOffset.MinValue &&
                access <= _lastWriteUtc.AddSeconds(1))
            {
                return false;
            }

            return now - access < AccessTtl;
        }
        catch
        {
            return false;
        }
    }

    public static void Write(SensorSnapshot snap, HistoryStore history, int maxHistory = 90)
    {
        if (!IsDashboardActive())
            return;

        var now = DateTimeOffset.UtcNow;
        if (now - _lastWrite < TimeSpan.FromMilliseconds(900))
            return;
        _lastWrite = now;

        try
        {
            var sensors = snap.Sensors.Select(s =>
            {
                var series = history.Snapshot(s.Id)
                    .TakeLast(maxHistory)
                    .Select(p => Math.Round(p.Value, 2))
                    .ToArray();
                return new
                {
                    id = s.Id,
                    group = s.Group,
                    name = s.Name,
                    hardware = s.HardwareName,
                    @short = s.ShortLabel,
                    kind = s.Kind.ToString().ToLowerInvariant(),
                    unit = s.Unit,
                    value = Math.Round(s.Value, 2),
                    source = s.Source,
                    history = series,
                };
            }).ToArray();

            var payload = new
            {
                timestamp = snap.Timestamp.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
                stale = snap.IsStale,
                backend = "LHM+PawnIO",
                sensors,
            };

            var json = JsonSerializer.Serialize(payload, Json);
            var js = "window.__HW_LIVE__ = " + json + ";\n";

            var path = LiveDataPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            lock (Gate)
            {
                File.WriteAllText(path, js, new UTF8Encoding(false));
            }

            _lastWriteUtc = DateTimeOffset.UtcNow;
        }
        catch
        {
            // 看板写失败不影响采样
            // Dashboard write failures do not affect sampling
            // ダッシュボードへの書き込み失敗はサンプリングに影響しません
        }
    }
}
