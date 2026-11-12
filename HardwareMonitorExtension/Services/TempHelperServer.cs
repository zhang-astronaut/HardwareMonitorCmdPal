// Copyright (c) HardwareMonitor. MIT license.

using System.IO.Pipes;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;
using LibreHardwareMonitor.Hardware;

namespace HardwareMonitorExtension.Services;

/// <summary>
/// CPU 温度助手服务端（由 SYSTEM 计划任务以 -TempHelper 参数启动本 exe）。
/// SYSTEM 权限可加载 LHM 内核驱动，从而读到 AMD SMN / Intel MSR 的真实 CPU 温度；
/// 通过命名管道 HardwareMonitorTemp 供普通权限的扩展进程读取（16 字节：double 温度 + long ticks），
/// 并落盘 ProgramData 兜底文件，供管道不可用时读取。
/// 修复点：旧版计划任务指向不存在的 h.exe，且全库没有管道服务端，助手链路整体断裂。
/// </summary>
/// <summary>
/// CPU temperature helper server (started by SYSTEM scheduled task with -TempHelper argument).
/// SYSTEM privilege allows loading LHM kernel driver to read authentic CPU temperature via AMD SMN / Intel MSR;
/// exposed via named pipe HardwareMonitorTemp for unprivileged extension process (16 bytes: double temp + long ticks),
/// and persisted to ProgramData fallback file for reading when pipe is unavailable.
/// Fix: legacy scheduled task pointed to non-existent h.exe and lacked a pipe server, breaking helper integration.
/// </summary>
/// <summary>
/// CPU 温度ヘルパー サーバー（SYSTEM スケジュール タスクにより本 exe が -TempHelper 引数付きで起動）。
/// SYSTEM 権限で LHM カーネル ドライバーをロードし、AMD SMN / Intel MSR から実際の CPU 温度を読み取り可能。
/// 名前付きパイプ HardwareMonitorTemp 経由で通常権限の拡張機能プロセスに提供（16 バイト：double 温度 + long ticks）。
/// パイプが利用できない場合に備えて ProgramData のフォールバック ファイルにも書き込みます。
/// 修正点：旧バージョンのスケジュール タスクは存在しない h.exe を参照し、パイプ サーバーも存在せず、連携全体が機能していませんでした。
/// </summary>
public static class TempHelperServer
{
    private static readonly Dictionary<int, string> DriveLetters = BuildDriveLetterMap();
    private static readonly Dictionary<string, ElevatedSensorDto> LastKnownSensors = new();

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        out STORAGE_DEVICE_NUMBER lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [StructLayout(LayoutKind.Sequential)]
    private struct STORAGE_DEVICE_NUMBER
    {
        public int DeviceType;
        public int DeviceNumber;
        public int PartitionNumber;
    }

    private const uint IOCTL_STORAGE_GET_DEVICE_NUMBER = 0x002D1080;
    private const uint OPEN_EXISTING = 3;

    private static Dictionary<int, string> BuildDriveLetterMap()
    {
        var map = new Dictionary<int, string>();
        var driveLists = new Dictionary<int, List<string>>();

        // 1. Native Win32 IOCTL: queries volume directly (e.g. \\.\C: -> DeviceNumber 0 -> Drive C)
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.DriveType != DriveType.Fixed && d.DriveType != DriveType.Removable) continue;
                var letter = d.Name.TrimEnd('\\', ':');
                using var handle = CreateFile($@"\\.\{letter}:", 0, 1 | 2, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (!handle.IsInvalid && DeviceIoControl(handle, IOCTL_STORAGE_GET_DEVICE_NUMBER, IntPtr.Zero, 0, out var sdn, (uint)Marshal.SizeOf<STORAGE_DEVICE_NUMBER>(), out _, IntPtr.Zero))
                {
                    if (!driveLists.TryGetValue(sdn.DeviceNumber, out var list))
                    {
                        list = new List<string>();
                        driveLists[sdn.DeviceNumber] = list;
                    }
                    if (!list.Contains(letter)) list.Add(letter);
                }
            }
            foreach (var (idx, letters) in driveLists)
                map[idx] = letters.Count == 1 ? $"Drive {letters[0]}" : $"Drives {string.Join("/", letters)}";
        }
        catch { }

        if (map.Count > 0) return map;

        // 2. Fallback to WMI if Win32 returned nothing
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT DeviceID, Index FROM Win32_DiskDrive");
            foreach (ManagementObject disk in searcher.Get())
            {
                if (disk["Index"] is null) continue;
                var index = Convert.ToInt32(disk["Index"]);
                var diskId = disk["DeviceID"]?.ToString() ?? "";
                if (string.IsNullOrEmpty(diskId)) continue;

                using var partSearcher = new ManagementObjectSearcher(
                    $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{diskId.Replace("\\", "\\\\")}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition");
                var letters = new List<string>();
                foreach (ManagementObject part in partSearcher.Get())
                {
                    var partId = part["DeviceID"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(partId)) continue;
                    using var logSearcher = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partId.Replace("\\", "\\\\")}'}} WHERE AssocClass = Win32_LogicalDiskToPartition");
                    foreach (ManagementObject log in logSearcher.Get())
                    {
                        var dev = log["DeviceID"]?.ToString();
                        if (!string.IsNullOrEmpty(dev))
                            letters.Add(dev.TrimEnd(':'));
                    }
                }
                if (letters.Count > 0)
                    map[index] = letters.Count == 1 ? $"Drive {letters[0]}" : $"Drives {string.Join("/", letters)}";
            }
        }
        catch { }
        return map;
    }

    private static int ExtractDriveIndex(IHardware hw)
    {
        var parts = hw.Identifier.ToString().Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && int.TryParse(parts[1], out var idx) ? idx : 0;
    }

    public static void Run()
    {
        using var mutex = new Mutex(true, @"Local\HardwareMonitorTempHelper", out var isNew);
        if (!isNew) return; // 已有实例在跑
                            // Another instance is already running
                            // 既に実行中のインスタンスが存在します

        // 开启 CPU、存储与主板硬件监控（SYSTEM 权限可直通 MSR、SMART 与 SuperIO LPC）
        // Enable CPU, Storage, and Motherboard monitoring (SYSTEM privilege has direct MSR, SMART, and SuperIO LPC access)
        // CPU、ストレージ、マザーボードの監視を有効化（SYSTEM 権限により MSR、SMART、SuperIO LPC に直接アクセス可能）
        Computer? computer = null;
        try
        {
            computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = false,
                IsStorageEnabled = true,
                IsMotherboardEnabled = true,
                IsMemoryEnabled = true,
                IsNetworkEnabled = false,
                IsBatteryEnabled = false,
            };
            computer.Open();
        }
        catch
        {
            computer?.Close();
            computer = null;
        }

        try
        {
            RunLoop(computer);
        }
        finally
        {
            computer?.Close();
        }
    }

    private static void RunLoop(Computer? computer)
    {
        var lastTemp = 0d;
        var lastTicks = 0L;

        while (true)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // 1) 采样硬件指标（CPU、NVMe/SATA 存储、主板 SuperIO）
            // 1) Sample hardware metrics (CPU, NVMe/SATA storage, motherboard SuperIO)
            // 1) ハードウェア測定値をサンプリング（CPU、NVMe/SATA ストレージ、マザーボード SuperIO）
            try
            {
                var (cpuTemp, sensors) = SampleAllSensors(computer);
                if (cpuTemp is > 1 and < 150)
                {
                    lastTemp = cpuTemp.Value;
                    lastTicks = DateTime.UtcNow.Ticks;
                    WriteFallbackFile(lastTemp, lastTicks);
                }
                if (sensors.Count > 0)
                {
                    WriteSnapshotJson(new ElevatedSnapshotDto
                    {
                        TimestampUtcTicks = DateTime.UtcNow.Ticks,
                        CpuTemp = lastTemp,
                        Sensors = sensors,
                    });
                }
            }
            catch
            {
                // 采样失败保留上次值；兜底文件带时间戳，客户端会校验新鲜度
                // Retain previous value on sample failure; fallback file includes timestamp for client-side freshness check
                // サンプリング失敗時は前回の値を保持。フォールバック ファイルにはタイムスタンプが含まれ、クライアント側で鮮度を検証
            }

            // 2) 每轮新建管道实例：避免断连后的 broken 状态导致忙循环
            // 2) Create new pipe instance each round: avoids busy wait loop caused by broken state after disconnection
            // 2) ラウンドごとに新しいパイプ インスタンスを作成：切断後の破損状態でビジー ループに陥るのを防止
            NamedPipeServerStream? server = null;
            try
            {
                server = new NamedPipeServerStream(
                    ElevatedTempHelper.PipeName,
                    PipeDirection.Out,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                if (server.WaitForConnectionAsync().Wait(TimeSpan.FromSeconds(3)))
                {
                    var buf = new byte[16];
                    BitConverter.GetBytes(lastTemp).CopyTo(buf, 0);
                    BitConverter.GetBytes(lastTicks).CopyTo(buf, 8);
                    server.Write(buf, 0, buf.Length);
                    server.Flush();
                }
            }
            catch
            {
                // 客户端中断/超时：丢弃本轮，继续采样
                // Client disconnect / timeout: discard current round and continue sampling
                // クライアント切断/タイムアウト：このラウンドを破棄してサンプリングを継続
            }
            finally
            {
                server?.Dispose();
            }

            // 3) 每轮至少 500ms，杜绝任何路径下的忙循环
            // 3) At least 500ms per round to eliminate busy wait loops on all code paths
            // 3) 各ラウンド最低 500ms を確保し、すべてのコード パスでビジー ループを防止
            var remain = 500 - (int)sw.ElapsedMilliseconds;
            if (remain > 0) Thread.Sleep(remain);
        }
    }

    /// <summary>采样全部启用的硬件传感器（CPU、存储 NVMe SMART、主板 SuperIO LPC）</summary>
    /// <summary>Samples all enabled hardware sensors (CPU, Storage NVMe SMART, Motherboard SuperIO LPC)</summary>
    /// <summary>有効化されたすべてのハードウェア センサーをサンプリング（CPU、ストレージ NVMe SMART、マザーボード SuperIO LPC）</summary>
    private static (double? CpuTemp, List<ElevatedSensorDto> Sensors) SampleAllSensors(Computer? computer)
    {
        if (computer is null) return (null, []);

        var allHw = new List<IHardware>();
        foreach (var hw in computer.Hardware)
        {
            allHw.Add(hw);
            foreach (var sub in hw.SubHardware)
            {
                allHw.Add(sub);
                foreach (var sub2 in sub.SubHardware)
                    allHw.Add(sub2);
            }
        }

        foreach (var hw in allHw) hw.Update();
        foreach (var hw in allHw) hw.Update();

        var results = new List<ElevatedSensorDto>();
        var cpuTemps = new List<(string Name, double Value)>();

        foreach (var hw in allHw)
        {
            var group = hw.HardwareType switch
            {
                HardwareType.Cpu => HardwareGroups.Processor,
                HardwareType.Storage => HardwareGroups.Storage,
                HardwareType.Motherboard or HardwareType.SuperIO or HardwareType.EmbeddedController => HardwareGroups.Motherboard,
                HardwareType.Memory => HardwareGroups.Memory,
                _ => null,
            };
            if (group is null) continue;

            var driveIdx = hw.HardwareType == HardwareType.Storage ? ExtractDriveIndex(hw) : 0;
            var driveLetter = hw.HardwareType == HardwareType.Storage && DriveLetters.TryGetValue(driveIdx, out var dl) ? $" - {dl}" : "";
            var hwName = hw.HardwareType == HardwareType.Storage ? $"SSD {driveIdx} - {hw.Name}{driveLetter}" : hw.Name;

            foreach (var s in hw.Sensors)
            {
                if (!s.Value.HasValue) continue;
                var v = (double)s.Value.Value;
                if (double.IsNaN(v) || double.IsInfinity(v)) continue;

                var kind = s.SensorType switch
                {
                    SensorType.Temperature => "temperature",
                    SensorType.Fan => "fan",
                    SensorType.Load => "load",
                    SensorType.Throughput or SensorType.Data => "data",
                    _ => null,
                };
                if (kind is null) continue;

                if (kind == "temperature" && (v <= 0 || v >= 150)) continue;
                if (kind == "fan" && (v < 0 || v > 25000)) continue;

                var unit = kind switch
                {
                    "temperature" => "°C",
                    "fan" => "RPM",
                    "load" => "%",
                    "data" when s.SensorType == SensorType.Throughput || s.Name.Contains("Rate", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Speed", StringComparison.OrdinalIgnoreCase) => "MB/s",
                    "data" => "GB",
                    _ => "",
                };

                var shortLabel = hw.HardwareType switch
                {
                    HardwareType.Cpu => s.Name.Contains("Tctl") ? "CPU Tctl" : s.Name.Contains("Package") ? "CPU Pkg" : "CPU",
                    HardwareType.Storage => kind switch
                    {
                        "temperature" => $"SSD {driveIdx} Temp",
                        "load" when s.Name.Contains("Used", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Used",
                        "load" when s.Name.Contains("Free", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Free",
                        "load" when s.Name.Contains("Read", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Read Act",
                        "load" when s.Name.Contains("Write", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Write Act",
                        "load" when s.Name.Contains("Total", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Activity", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Act",
                        "data" when s.SensorType == SensorType.Throughput || s.Name.Contains("Rate", StringComparison.OrdinalIgnoreCase) =>
                            s.Name.Contains("Read", StringComparison.OrdinalIgnoreCase) ? $"SSD {driveIdx} Read Rate" : $"SSD {driveIdx} Write Rate",
                        "data" when s.Name.Contains("Read", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Total Read",
                        "data" when s.Name.Contains("Write", StringComparison.OrdinalIgnoreCase) => $"SSD {driveIdx} Total Written",
                        _ => $"SSD {driveIdx} {(s.Name.Length > 8 ? s.Name[..7] + "…" : s.Name)}",
                    },
                    HardwareType.Motherboard => s.Name.Length > 12 ? s.Name[..11] + "…" : s.Name,
                    _ => s.Name,
                };

                if (hw.HardwareType == HardwareType.Cpu && kind == "temperature")
                    cpuTemps.Add((s.Name, v));

                var dto = new ElevatedSensorDto
                {
                    Id = s.Identifier.ToString(),
                    Group = group,
                    Name = $"{hw.Name} · {s.Name}",
                    Source = $"{hw.HardwareType} / {s.SensorType} · Elevated",
                    Kind = kind,
                    Value = Math.Round(v, 1),
                    Unit = unit,
                    HardwareName = hwName,
                    ShortLabel = shortLabel,
                };
                LastKnownSensors[dto.Id] = dto;

                // Automatically synthesize SSD Free Space when Used Space is reported
                if (hw.HardwareType == HardwareType.Storage && kind == "load" && s.Name.Contains("Used", StringComparison.OrdinalIgnoreCase))
                {
                    var freeVal = Math.Clamp(100.0 - v, 0, 100);
                    var freeDto = new ElevatedSensorDto
                    {
                        Id = $"{s.Identifier}-free",
                        Group = group,
                        Name = $"{hw.Name} · Free Space",
                        Source = $"{hw.HardwareType} / Load · Elevated",
                        Kind = "load",
                        Value = Math.Round(freeVal, 1),
                        Unit = "%",
                        HardwareName = hwName,
                        ShortLabel = $"SSD {driveIdx} Free",
                    };
                    LastKnownSensors[freeDto.Id] = freeDto;
                }
            }
        }

        double? bestCpu = null;
        if (cpuTemps.Count > 0)
        {
            var pkg = cpuTemps.FirstOrDefault(t =>
                t.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("Tdie", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("Package", StringComparison.OrdinalIgnoreCase));
            bestCpu = pkg.Name is not null ? pkg.Value : cpuTemps.Max(t => t.Value);
        }

        return (bestCpu, LastKnownSensors.Values.ToList());
    }

    private static void WriteSnapshotJson(ElevatedSnapshotDto snapshot)
    {
        try
        {
            var dir = @"C:\ProgramData\HardwareMonitorTemp";
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(snapshot);
            var tmp = Path.Combine(dir, "elevated-sensors.json.tmp");
            var target = Path.Combine(dir, "elevated-sensors.json");
            File.WriteAllText(tmp, json);
            File.Move(tmp, target, overwrite: true);
        }
        catch { }
    }

    private static void WriteFallbackFile(double temp, long ticks)
    {
        try
        {
            var dir = @"C:\ProgramData\HardwareMonitorTemp";
            Directory.CreateDirectory(dir);
            var buf = new byte[16];
            BitConverter.GetBytes(temp).CopyTo(buf, 0);
            BitConverter.GetBytes(ticks).CopyTo(buf, 8);
            File.WriteAllBytes(Path.Combine(dir, "cpu-temp.bin"), buf);
        }
        catch
        {
            // 写盘失败不影响管道
            // Disk write failure does not affect named pipe
            // ディスク書き込みの失敗は名前付きパイプに影響しません
        }
    }
}
