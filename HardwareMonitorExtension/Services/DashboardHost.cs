// Copyright (c) HardwareMonitor. MIT license.

using System.Diagnostics;
using System.IO;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HardwareMonitorExtension.Services;

/// <summary>
/// 将随包分发的 index.html 释放到用户可写目录并打开（file://），
/// 保证用户双击/一键打开即可，无需额外环境。
/// </summary>
/// <summary>
/// Extracts the bundled index.html into a user-writable directory and opens it (file://),
/// ensuring users can launch in one click without extra dependencies.
/// </summary>
/// <summary>
/// パッケージに同梱されている index.html をユーザー書き込み可能ディレクトリに展開して開く（file://）。
/// 追加の環境設定なしで、ダブルクリックまたはワンクリックで起動できるようにします。
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
        // Prefer copying full dashboard from bundled Assets (including css/js)
        // パッケージ内の Assets から完全なダッシュボード（css/js を含む）を優先的にコピー
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
                // May be locked by another process; retain existing file
                // 他のプロセスで使用中の可能性があるため、既存ファイルをそのまま使用
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
        // Open a live-data.js write window upon launch; subsequently renewed based on file access time
        // 起動後に一定時間の live-data.js 書き込みウィンドウを開始。以降はファイル アクセス時間に基づいて更新を継続
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
        <html lang="en"><head><meta charset="utf-8"><title>Hardware Monitor</title>
        <style>body{font:14px/1.5 Segoe UI,sans-serif;background:#0b0d10;color:#e8eef7;padding:24px}
        </style></head><body>
        <h1>Hardware Monitor</h1>
        <p>Full dashboard assets not extracted. Please copy <code>index.html</code> from the project to this folder.</p>
        </body></html>
        """;
}
