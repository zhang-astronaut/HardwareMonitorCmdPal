// Copyright (c) HardwareMonitor. MIT license.

using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using Microsoft.Win32;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HardwareMonitorExtension.Services;

/// <summary>
/// CPU Tctl 补充源（内存映射，零写盘）。用隐藏 PowerShell 启动 h.exe，避免 WSH/CMD 弹窗。
/// </summary>
public static class ElevatedTempHelper
{
    public const string TaskName = "HardwareMonitorTempHelper";
    private const string MmfName = @"Local\HardwareMonitorCpuTemp";

    private static MemoryMappedFile? _mmf;
    private static MemoryMappedViewAccessor? _view;
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
        var dir = Dir;
        var ps1 = Path.Combine(dir, "run-hidden.ps1");
        try
        {
            File.WriteAllText(ps1,
                "$ErrorActionPreference = 'SilentlyContinue'\n" +
                "$p = Join-Path $PSScriptRoot 'h.exe'\n" +
                "if (Test-Path $p) { Start-Process -FilePath $p -WindowStyle Hidden }\n");
        }
        catch { }
    }

    public static bool IsTaskInstalled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\" + TaskName);
            return key is not null;
        }
        catch
        {
            return false;
        }
    }

    public static bool EnsureAutoStart()
    {
        EnsureScript();
        var script = Path.Combine(Path.GetTempPath(), "hwmon-install-temp-task.ps1");
        File.WriteAllText(script, BuildInstallScript());
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
        catch
        {
            return false;
        }
    }

    public static void TryRestartHelper()
    {
        if (DateTimeOffset.UtcNow - _lastRestart < TimeSpan.FromSeconds(20))
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

    public static double? TryReadCpuTemperature()
    {
        try
        {
            if (_view is null)
            {
                _mmf ??= MemoryMappedFile.OpenExisting(MmfName);
                _view = _mmf.CreateViewAccessor(0, 16, MemoryMappedFileAccess.Read);
            }

            var t = _view.ReadDouble(0);
            var ticks = _view.ReadInt64(8);
            if (!double.IsFinite(t) || t < 1 || t > 150 || ticks <= 0)
                return null;

            var age = DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc);
            if (age > TimeSpan.FromSeconds(5))
                TryRestartHelper();
            if (age > TimeSpan.FromMinutes(2))
                return null;
            return t;
        }
        catch
        {
            TryRestartHelper();
            return null;
        }
    }

    /// <summary>隐藏 PowerShell 启动；仅 AtLogOn（PS 5.1 无 AtUnlock）。</summary>
    private static string BuildInstallScript()
    {
        var ps1 = Path.Combine(Dir, "run-hidden.ps1").Replace("'", "''");
        return $"""
            $ErrorActionPreference = 'SilentlyContinue'
            $ps1 = '{ps1}'
            $action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$ps1`""
            $trigger = New-ScheduledTaskTrigger -AtLogOn
            $principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -RunLevel Highest -LogonType Interactive
            $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -RestartCount 5 -RestartInterval (New-TimeSpan -Minutes 1) -ExecutionTimeLimit ([TimeSpan]::Zero)
            Register-ScheduledTask -TaskName '{TaskName}' -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null
            Start-ScheduledTask -TaskName '{TaskName}' | Out-Null
            """;
    }
}
