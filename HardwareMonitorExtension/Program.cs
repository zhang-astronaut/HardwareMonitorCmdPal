using HardwareMonitorExtension.Localization;
// Copyright (c) HardwareMonitor. MIT license.
// COM 宿主入口 —— 与官方 TemplateCmdPalExtension 一致。

using Microsoft.CommandPalette.Extensions;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;
using System.Runtime.InteropServices;
using HardwareMonitorExtension.Services;

namespace HardwareMonitorExtension;

public class Program
{
    [MTAThread]
    public static void Main(string[] args)
    {
        // SYSTEM 计划任务以 -TempHelper 启动：常驻采样 CPU 温度并服务命名管道
        if (args.Length > 0 && args[0] == "-TempHelper")
        {
            TempHelperServer.Run();
            return;
        }

        if (args.Length == 0 || args[0] != "-RegisterProcessAsComServer")
        {
            Console.WriteLine(Text.Get("NotExtension"));
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
