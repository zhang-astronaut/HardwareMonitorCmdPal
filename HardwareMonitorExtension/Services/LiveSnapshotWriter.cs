// Copyright (c) HardwareMonitor. MIT license.

using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using HardwareMonitorExtension.Services;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HardwareMonitorExtension.Services;

/// <summary>
/// 将真实采样快照写成 live-data.js，供同目录 index.html 以 script 方式加载
/// （file:// 下 fetch 可能被拦，script 引用最稳）。
/// </summary>
public static class LiveSnapshotWriter
{
    private static readonly object Gate = new();
    private static DateTimeOffset _lastWrite = DateTimeOffset.MinValue;
    private static readonly JsonSerializerOptions Json = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static string DashboardDir =>
        Utilities.BaseSettingsPath("HardwareMonitorExtension");

    public static void Write(SensorSnapshot snap, HistoryStore history, int maxHistory = 90)
    {
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

            var dir = DashboardDir;
            Directory.CreateDirectory(dir);
            lock (Gate)
            {
                File.WriteAllText(Path.Combine(dir, "live-data.js"), js, new UTF8Encoding(false));
            }
        }
        catch
        {
            // 看板写失败不影响采样
        }
    }
}
