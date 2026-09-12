// Copyright (c) HardwareMonitor. MIT license.

using HardwareMonitorExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HardwareMonitorExtension.Pages;

/// <summary>在列表页上下文菜单中切换 Compact / Default 密度。</summary>
internal sealed partial class ToggleCompactCommand : InvokableCommand
{
    private readonly SensorsListPage _page;

    public ToggleCompactCommand(SensorsListPage page)
    {
        _page = page;
        Name = "切换紧凑布局";
        Icon = new IconInfo("☰");
    }

    public override CommandResult Invoke()
    {
        _page.ToggleCompact();
        return CommandResult.KeepOpen();
    }
}

/// <summary>切换固定到快捷栏。</summary>
internal sealed partial class TogglePinSensorCommand : InvokableCommand
{
    private readonly SensorHub _hub;
    private readonly string _sensorId;

    public TogglePinSensorCommand(SensorHub hub, string sensorId)
    {
        _hub = hub;
        _sensorId = sensorId;
        Name = "固定到快捷栏 / 取消固定";
        Icon = new IconInfo("📌");
    }

    public override CommandResult Invoke()
    {
        _hub.Pinned?.Toggle(_sensorId);
        return CommandResult.KeepOpen();
    }
}

/// <summary>一键打开浏览器看板（独立 index.html）。</summary>
internal sealed partial class OpenDashboardCommand : InvokableCommand
{
    public OpenDashboardCommand()
    {
        Name = "打开实时图表看板";
        Icon = new IconInfo("🌐");
    }

    public override CommandResult Invoke()
    {
        DashboardHost.OpenDashboard();
        return CommandResult.KeepOpen();
    }
}
