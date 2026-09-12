// Copyright (c) HardwareMonitor. MIT license.

using System.Diagnostics;
using System.IO;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HardwareMonitorExtension.Services;

/// <summary>
/// 将随包分发的 index.html 释放到用户可写目录并打开（file://），
/// 保证用户双击/一键打开即可，无需额外环境。
/// </summary>
public static class DashboardHost
{
    public const string FileName = "index.html";

    public static string EnsureDashboardFile()
    {
        var dir = Utilities.BaseSettingsPath("HardwareMonitorExtension");
        Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir, FileName);

        // 优先从包内 Assets 复制完整看板（含 css/js）
        var assets = Path.Combine(AppContext.BaseDirectory, "Assets");
        var packaged = Path.Combine(assets, FileName);
        if (File.Exists(packaged))
        {
            try
            {
                File.Copy(packaged, dest, overwrite: true);
                foreach (var extra in new[] { "styles.css", "app.js", "live-data.js" })
                {
                    var src = Path.Combine(assets, extra);
                    if (File.Exists(src))
                        File.Copy(src, Path.Combine(dir, extra), overwrite: true);
                }
            }
            catch
            {
                // 可能被占用，沿用旧文件
            }

            return dest;
        }

        if (!File.Exists(dest))
            File.WriteAllText(dest, FallbackHtml);

        return dest;
    }

    public static void OpenDashboard()
    {
        var path = EnsureDashboardFile();
        // 打开后开启一段 live-data.js 写盘窗口；之后靠文件访问时间续期
        LiveSnapshotWriter.NotifyDashboardOpen();
        var uri = new Uri(path).AbsoluteUri;
        try
        {
            Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        }
        catch
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
    }

    public static OpenUrlCommand CreateOpenCommand()
    {
        var path = EnsureDashboardFile();
        return new OpenUrlCommand(new Uri(path).AbsoluteUri);
    }

    private const string FallbackHtml =
        """
        <!DOCTYPE html>
        <html lang="zh-CN"><head><meta charset="utf-8"><title>Hardware Monitor</title>
        <style>body{font:14px/1.5 Segoe UI,sans-serif;background:#0b0d10;color:#e8eef7;padding:24px}
        </style></head><body>
        <h1>Hardware Monitor</h1>
        <p>完整看板未随包释放，请将项目中的 <code>index.html</code> 复制到本目录。</p>
        </body></html>
        """;
}
