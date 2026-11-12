// Copyright (c) HardwareMonitor. MIT license.

using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Runtime.InteropServices;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HardwareMonitorExtension.Services;

/// <summary>
/// 提权传感器数据源：优先读取 ProgramData\HardwareMonitorTemp\elevated-sensors.json，
/// 兼顾命名管道与 cpu-temp.bin 兜底文件。计划任务的注册/重启也在这里。
/// </summary>
/// <summary>
/// Elevated sensor data source: prefers reading ProgramData\HardwareMonitorTemp\elevated-sensors.json,
/// with fallback to named pipe and cpu-temp.bin. Scheduled task registration and restart are also handled here.
/// </summary>
/// <summary>
/// 昇格センサー データソース：ProgramData\HardwareMonitorTemp\elevated-sensors.json の読み取りを優先し、
/// 名前付きパイプおよび cpu-temp.bin フォールバック ファイルにも対応。スケジュールされたタスクの登録/再起動もここで処理します。
/// </summary>
public static class ElevatedTempHelper
{
    public const string TaskName = "HardwareMonitorTempHelper";
    public const string PipeName = "HardwareMonitorTemp";
    private static readonly JsonSerializerOptions JsonReadOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    private static DateTimeOffset _lastRestart = DateTimeOffset.MinValue;

    public static bool IsTaskInstalled()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/query /tn {TaskName}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            });
            proc?.WaitForExit(3000);
            return proc?.ExitCode == 0;
        }
        catch { return false; }
    }

    public static bool EnsureAutoStart()
    {
        var script = Path.Combine(Path.GetTempPath(), "hwmon-install-temp-task.ps1");
        // 修复点：旧版指向设置目录下不存在的 h.exe（全库无该产物、无管道服务端）。
        // 现改为复用包内主 exe 的 -TempHelper 模式（见 TempHelperServer）。
        // Fix: legacy version pointed to non-existent h.exe in settings dir (no such artifact in repo, no pipe server).
        // Now reuses the packaged main exe in -TempHelper mode (see TempHelperServer).
        // 修正点：旧バージョンでは設定ディレクトリ内に存在しない h.exe を参照していました（リポジトリ内に該当ファイルやパイプ サーバーなし）。
        // パッケージ内のメイン exe を -TempHelper モードで再利用するように変更（TempHelperServer 参照）。
        var exe = Path.Combine(AppContext.BaseDirectory, "HardwareMonitorExtension.exe").Replace("'", "''");
        File.WriteAllText(script, $"""
            $action = New-ScheduledTaskAction -Execute '{exe}' -Argument '-TempHelper'
            $trigger = New-ScheduledTaskTrigger -AtStartup
            $principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -RunLevel Highest
            $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -RestartCount 5 -RestartInterval (New-TimeSpan -Minutes 1) -ExecutionTimeLimit ([TimeSpan]::Zero)
            Register-ScheduledTask -TaskName '{TaskName}' -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null
            Start-ScheduledTask -TaskName '{TaskName}' | Out-Null
            """);
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{script}\"",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            });
            return true;
        }
        catch { return false; }
    }

    public static void TryRestartHelper()
    {
        if (DateTimeOffset.UtcNow - _lastRestart < TimeSpan.FromSeconds(30))
            return;
        _lastRestart = DateTimeOffset.UtcNow;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/end /tn {TaskName}",
                UseShellExecute = false,
                CreateNoWindow = true,
            })?.WaitForExit(1000);

            Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/run /tn {TaskName}",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
        }
        catch { }
    }

    /// <summary>从 ProgramData 读取 SYSTEM 助手写入的完整提权传感器快照</summary>
    /// <summary>Reads complete elevated sensor snapshot written by SYSTEM helper from ProgramData</summary>
    /// <summary>SYSTEM ヘルパーが ProgramData に書き込んだ完全な昇格センサー スナップショットを読み取り</summary>
    public static ElevatedSnapshotDto? TryReadSnapshot()
    {
        try
        {
            var file = @"C:\ProgramData\HardwareMonitorTemp\elevated-sensors.json";
            if (!File.Exists(file)) return null;
            var json = File.ReadAllText(file);
            if (string.IsNullOrWhiteSpace(json)) return null;
            var dto = JsonSerializer.Deserialize<ElevatedSnapshotDto>(json, JsonReadOpts);
            if (dto is null || dto.TimestampUtcTicks <= 0) return null;
            var dt = new DateTime(dto.TimestampUtcTicks, DateTimeKind.Utc);
            if (DateTime.UtcNow - dt > TimeSpan.FromMinutes(5)) return null;
            return dto;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>优先读取提权快照；失败则回退到命名管道与 16 字节 bin 兜底文件</summary>
    /// <summary>Prefers reading elevated snapshot; falls back to named pipe and 16-byte bin file on failure</summary>
    /// <summary>昇格スナップショットの読み取りを優先。失敗時は名前付きパイプおよび 16 バイト bin ファイルにフォールバック</summary>
    public static double? TryReadCpuTemperature()
    {
        var snap = TryReadSnapshot();
        if (snap is not null && snap.CpuTemp is > 1 and < 150)
            return snap.CpuTemp;

        var t = ReadFromPipe();
        if (t is > 1 and < 150) return t;
        return ReadFromFile();
    }

    private static double? ReadFromPipe()
    {
        try
        {
            using var client = new NamedPipeClientStream(
                ".", PipeName, PipeDirection.In, PipeOptions.Asynchronous);
            client.Connect(300);
            var buf = new byte[16];
            var n = 0;
            while (n < buf.Length)
            {
                var r = client.Read(buf, n, buf.Length - n);
                if (r <= 0) break;
                n += r;
            }

            if (n >= 8)
            {
                var temp = BitConverter.ToDouble(buf, 0);
                if (!double.IsFinite(temp) || temp <= 1 || temp >= 150)
                    return null;
                // 带时间戳时校验新鲜度（服务端每轮采样后应答）
                // Validate freshness when timestamp is present (server responds after each sampling round)
                // タイムスタンプが存在する場合は鮮度を検証（サーバーはサンプリング ラウンドごとに応答）
                if (n >= 16)
                {
                    var ticks = BitConverter.ToInt64(buf, 8);
                    if (ticks <= 0 || DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc) > TimeSpan.FromMinutes(5))
                        return null;
                }
                return temp;
            }
        }
        catch
        {
            TryRestartHelper();
        }

        return null;
    }

    private static double? ReadFromFile()
    {
        try
        {
            var file = @"C:\ProgramData\HardwareMonitorTemp\cpu-temp.bin";
            if (!File.Exists(file)) return null;
            var bytes = File.ReadAllBytes(file);
            if (bytes.Length < 16) return null;
            var t = BitConverter.ToDouble(bytes, 0);
            var ticks = BitConverter.ToInt64(bytes, 8);
            if (!double.IsFinite(t) || t < 1 || t > 150 || ticks <= 0) return null;
            if (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc) > TimeSpan.FromMinutes(5))
                return null;
            return t;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>提权硬件指标快照 DTO</summary>
/// <summary>Elevated hardware telemetry snapshot DTO</summary>
/// <summary>昇格ハードウェア測定値スナップショット DTO</summary>
public sealed class ElevatedSnapshotDto
{
    public long TimestampUtcTicks { get; set; }
    public double CpuTemp { get; set; }
    public List<ElevatedSensorDto> Sensors { get; set; } = [];
}

/// <summary>提权单个传感器 DTO</summary>
/// <summary>Elevated individual sensor DTO</summary>
/// <summary>昇格個別センサー DTO</summary>
public sealed class ElevatedSensorDto
{
    public string Id { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string HardwareName { get; set; } = string.Empty;
    public string ShortLabel { get; set; } = string.Empty;
}
