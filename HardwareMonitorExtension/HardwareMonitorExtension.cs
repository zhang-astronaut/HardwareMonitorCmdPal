// Copyright (c) HardwareMonitor. MIT license.

using Microsoft.CommandPalette.Extensions;
using System.Runtime.InteropServices;

namespace HardwareMonitorExtension;

/// <summary>
/// 扩展入口。CLSID 必须与 Package.appxmanifest 中三处 ClassId/CreateInstance 一致。
/// </summary>
[Guid("3F8A2C91-6B4E-4D2A-9C17-8E5F0A7B4D63")]
public sealed partial class HardwareMonitorExtension : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;
    private readonly HardwareMonitorCommandsProvider _provider = new();

    public HardwareMonitorExtension(ManualResetEvent extensionDisposedEvent)
    {
        _extensionDisposedEvent = extensionDisposedEvent;
    }

    public object? GetProvider(ProviderType providerType) => providerType switch
    {
        ProviderType.Commands => _provider,
        _ => null,
    };

    public void Dispose() => _extensionDisposedEvent.Set();
}
