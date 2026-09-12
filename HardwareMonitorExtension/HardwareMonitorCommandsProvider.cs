// Copyright (c) HardwareMonitor. MIT license.

using HardwareMonitorExtension.Dock;
using HardwareMonitorExtension.Pages;
using HardwareMonitorExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HardwareMonitorExtension;

/// <summary>
/// 顶级命令提供程序。
/// </summary>
public partial class HardwareMonitorCommandsProvider : CommandProvider
{
    private readonly SensorHub _hub = new();
    private readonly HardwareMonitorSettingsManager _settingsManager;
    private readonly PinnedSensorsDockBand _dockBand;
    private readonly ICommandItem[] _commands;
    private readonly ICommandItem[] _bands;

    public HardwareMonitorCommandsProvider()
    {
        DisplayName = "硬件监视器";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        _settingsManager = new HardwareMonitorSettingsManager(_hub);
        _hub.ConfigurePins(HardwareMonitorSettingsManager.PinsJsonPath());
        _dockBand = new PinnedSensorsDockBand(_hub);

        var listPage = new SensorsListPage(_hub, _settingsManager);
        _commands =
        [
            new CommandItem(listPage)
            {
                Title = DisplayName,
                Subtitle = "全部传感器 · 可固定到快捷栏",
                MoreCommands = [
                    new CommandContextItem(new OpenDashboardCommand())
                    {
                        Title = "打开实时图表看板",
                        Icon = new IconInfo("🌐"),
                    },
                    new CommandContextItem(new ToggleCompactCommand(listPage)),
                    new CommandContextItem(_settingsManager.Settings.SettingsPage),
                ],
            },
            new CommandItem(new OpenDashboardCommand())
            {
                Title = "实时图表看板",
                Subtitle = "btop 风格离线 index.html",
                Icon = new IconInfo("🌐"),
            },
        ];

        _bands = [_dockBand];

        Settings = _settingsManager.Settings;
        _ = _hub.StartAsync();
    }

    public override ICommandItem[] TopLevelCommands() => _commands;

    public override ICommandItem[] GetDockBands() => _bands;

    public override void Dispose()
    {
        _dockBand.Dispose();
        _hub.Dispose();
        GC.SuppressFinalize(this);
        base.Dispose();
    }
}
