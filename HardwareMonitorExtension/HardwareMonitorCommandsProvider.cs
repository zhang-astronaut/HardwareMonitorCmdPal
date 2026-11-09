// Copyright (c) HardwareMonitor. MIT license.

using HardwareMonitorExtension.Dock;
using HardwareMonitorExtension.Pages;
using HardwareMonitorExtension.Localization;
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
        DisplayName = Loc.Current.ExtensionDisplayName;
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
                Subtitle = Loc.Current.AllSensorsSubtitle,
                MoreCommands = [
                    new CommandContextItem(new OpenDashboardCommand())
                    {
                        Title = Loc.Current.OpenDashboardTitle,
                        Icon = new IconInfo("🌐"),
                    },
                    new CommandContextItem(new ToggleCompactCommand(listPage)),
                    new CommandContextItem(_settingsManager.Settings.SettingsPage),
                ],
            },
            new CommandItem(new OpenDashboardCommand())
            {
                Title = Loc.Current.OpenDashboardTitle,
                Subtitle = Loc.Current.DashboardSubtitle,
                Icon = new IconInfo("🌐"),
            },
            new CommandItem(new StartElevatedTempHelperCommand())
            {
                Title = Loc.Current.InstallHelperTitle,
                Subtitle = Loc.Current.InstallHelperSubtitle,
                Icon = new IconInfo("🔐"),
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
