// Copyright (c) HardwareMonitor. MIT license.

using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HardwareMonitorExtension.Services;

/// <summary>
/// CPU Tctl：优先命名管道（SYSTEM 助手，零写盘）；失败则读 ProgramData 文件。
/// </summary>
public static class ElevatedTempHelper
{
    public const string TaskName = "HardwareMonitorTempHelper";
    public const string PipeName = "HardwareMonitorTemp";
    private static DateTimeOffset _lastRestart = DateTimeOffset.MinValue;

    private static string Dir
    {
        get
        {
            var dir = Utilities.BaseSettingsPath("HardwareMonitorExtension");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static void EnsureScript()
    {
        // 无独立脚本；任务直接指向 h.exe（命名管道服务）
    }

    public static bool IsTaskInstalled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\" + TaskName);
            return key is not null;
        }
        catch { return false; }
    }

    public static bool EnsureAutoStart()
    {
        var script = Path.Combine(Path.GetTempPath(), "hwmon-install-temp-task.ps1");
        var exe = Path.Combine(Dir, "h.exe").Replace("'", "''");
        File.WriteAllText(script, $"""
            $action = New-ScheduledTaskAction -Execute '{exe}'
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
                Arguments = $"/run /tn {TaskName}",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
        }
        catch { }
    }

    /// <summary>命名管道读 16 字节：double 温度 + long ticks；超时/失败再读文件。</summary>
    public static double? TryReadCpuTemperature()
    {
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
            client.Connect(50);
            var buf = new byte[16];
            var n = client.Read(buf, 0, 16);
            if (n >= 8)
            {
                var temp = BitConverter.ToDouble(buf, 0);
                if (double.IsFinite(temp) && temp > 1 && temp < 150)
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
