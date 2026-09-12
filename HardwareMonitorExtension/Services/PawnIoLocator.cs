// Copyright (c) HardwareMonitor. MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace HardwareMonitorExtension.Services;

/// <summary>
/// 定位本机 PawnIO 安装目录。官方安装器固定路径，注册表 InstallLocation 优先。
/// 见 https://github.com/namazso/PawnIO.Modules/wiki/Using-PawnIO-Modules
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

    public static string? FindLibraryPath()
    {
        var dir = FindInstallDirectory();
        if (dir is null) return null;
        var dll = Path.Combine(dir, "PawnIOLib.dll");
        return File.Exists(dll) ? dll : null;
    }
}

/// <summary>
/// P/Invoke 包装 PawnIOLib。优先解析 *_win32 导出。
/// </summary>
public sealed class PawnIoNative : IDisposable
{
    private readonly SafeLibraryHandle _lib;
    private readonly IntPtr _handle;
    private bool _disposed;

    private delegate int PawnioOpen(out IntPtr handle);
    private delegate int PawnioClose(IntPtr handle);
    private delegate int PawnioLoad(IntPtr handle, byte[] blob, nuint size);
    private delegate int PawnioExecute(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        ulong[]? inn, nuint inSize,
        ulong[]? output, nuint outSize,
        out nuint returnSize);

    private readonly PawnioOpen _open;
    private readonly PawnioClose _close;
    private readonly PawnioLoad _load;
    private readonly PawnioExecute _execute;

    public string LibraryPath { get; }

    public PawnIoNative(string libraryPath)
    {
        LibraryPath = libraryPath;
        _lib = NativeLoadLibrary(libraryPath);
        _open = GetDelegate<PawnioOpen>(_lib, "pawnio_open_win32");
        _close = GetDelegate<PawnioClose>(_lib, "pawnio_close_win32");
        _load = GetDelegate<PawnioLoad>(_lib, "pawnio_load_win32");
        _execute = GetDelegate<PawnioExecute>(_lib, "pawnio_execute_win32");

        var hr = _open(out _handle);
        if (hr < 0 || _handle == IntPtr.Zero)
            Marshal.ThrowExceptionForHR(hr);
    }

    public static bool TryOpen(out PawnIoNative? native, out string? error)
    {
        native = null;
        error = null;
        var dll = PawnIoLocator.FindLibraryPath();
        if (dll is null)
        {
            error = "未找到 PawnIOLib.dll（请安装 PawnIO）";
            return false;
        }

        try
        {
            native = new PawnIoNative(dll);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public void LoadModule(byte[] blob) => Check(_load(_handle, blob, (nuint)blob.Length));

    public ulong[] Execute(string name, ulong[]? input = null, int outputCount = 4)
    {
        var output = new ulong[outputCount];
        var inn = input ?? Array.Empty<ulong>();
        Check(_execute(_handle, name, inn, (nuint)inn.Length, output, (nuint)output.Length, out var ret));
        if ((long)ret < output.Length)
            Array.Resize(ref output, (int)ret);
        return output;
    }

    private static void Check(int hr)
    {
        if (hr < 0) Marshal.ThrowExceptionForHR(hr);
    }

    private static T GetDelegate<T>(SafeLibraryHandle lib, string name) where T : Delegate
    {
        var p = GetProcAddress(lib, name);
        if (p == IntPtr.Zero)
            throw new EntryPointNotFoundException(name);
        return Marshal.GetDelegateForFunctionPointer<T>(p);
    }

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibraryW(string path);

    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr GetProcAddress(SafeLibraryHandle lib, string name);

    [DllImport("kernel32", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr lib);

    private static SafeLibraryHandle NativeLoadLibrary(string path)
    {
        var h = LoadLibraryW(path);
        if (h == IntPtr.Zero)
            throw new DllNotFoundException(path);
        return new SafeLibraryHandle(h);
    }

    private sealed class SafeLibraryHandle : SafeHandle
    {
        public SafeLibraryHandle(IntPtr handle) : base(IntPtr.Zero, true) => SetHandle(handle);
        public override bool IsInvalid => handle == IntPtr.Zero;
        protected override bool ReleaseHandle() => FreeLibrary(handle);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            if (_handle != IntPtr.Zero) _close(_handle);
        }
        finally
        {
            _lib.Dispose();
        }
    }
}
