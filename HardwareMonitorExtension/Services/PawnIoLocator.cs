// Copyright (c) HardwareMonitor. MIT license.

using Microsoft.Win32;

namespace HardwareMonitorExtension.Services;

/// <summary>
/// 定位本机 PawnIO 安装目录（仅用于后端标签显示）。
/// 说明：PawnIO 设备 ACL 在普通权限下拒绝访问（0x80070005），
/// 传感器数据不走 PawnIO；CPU 真实温度由 SYSTEM 助手提供，见 TempHelperServer。
/// </summary>
/// <summary>
/// Locates local PawnIO installation directory (used only for backend label display).
/// Note: PawnIO device ACL denies access under normal privileges (0x80070005);
/// sensor data does not route through PawnIO; authentic CPU temp is provided by SYSTEM helper (see TempHelperServer).
/// </summary>
/// <summary>
/// ローカルの PawnIO インストール ディレクトリを特定（バックエンド ラベル表示のみに使用）。
/// 説明：通常権限では PawnIO デバイスの ACL によりアクセスが拒否されるため（0x80070005）、
/// センサー データは PawnIO を経由しません。CPU の実際の温度は SYSTEM ヘルパーから提供されます（TempHelperServer 参照）。
/// </summary>
public static class PawnIoLocator
{
    private const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO";

    public static string? FindInstallDirectory()
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var key = baseKey.OpenSubKey(UninstallKey);
                var loc = key?.GetValue("InstallLocation") as string;
                if (!string.IsNullOrWhiteSpace(loc) && Directory.Exists(loc))
                    return Path.GetFullPath(loc);
            }
            catch
            {
                // ignore
            }
        }

        var fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "PawnIO");
        return Directory.Exists(fallback) ? fallback : null;
    }
}
