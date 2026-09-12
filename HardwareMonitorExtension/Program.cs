// Copyright (c) HardwareMonitor. MIT license.
// COM 宿主入口 —— 与官方 TemplateCmdPalExtension 一致。

using Microsoft.CommandPalette.Extensions;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;
using System.Runtime.InteropServices;

namespace HardwareMonitorExtension;

public class Program
{
    [MTAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 0 || args[0] != "-RegisterProcessAsComServer")
        {
            Console.WriteLine("Not being launched as a Extension... exiting.");
            return;
        }

        global::Shmuelie.WinRTServer.ComServer server = new();
        ManualResetEvent extensionDisposedEvent = new(false);

        // 单实例：宿主每次取 IExtension 时返回同一对象，便于共享 SensorHub。
        HardwareMonitorExtension extensionInstance = new(extensionDisposedEvent);
        server.RegisterClass<HardwareMonitorExtension, IExtension>(() => extensionInstance);
        server.Start();

        extensionDisposedEvent.WaitOne();
        server.Stop();
        server.UnsafeDispose();
    }
}
