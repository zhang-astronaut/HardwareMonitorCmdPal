// Copyright (c) HardwareMonitor. MIT license.

using HardwareMonitorExtension.Services;

namespace HardwareMonitorExtension;

/// <summary>
/// 进程内单例采样枢纽：列表页、图表页、Dock 带共享快照 / 历史 / 固定列表。
/// </summary>
public sealed partial class SensorHub : IDisposable
{
    private readonly SensorService _service = new();
    private readonly object _gate = new();
    private SensorSnapshot _snap = new(DateTimeOffset.Now, [], true);
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private int _intervalMs = 1000;
    private bool _disposed;
    private bool _defaultsApplied;

    public event EventHandler? SnapshotUpdated;

    public SensorSnapshot Snapshot
    {
        get
        {
            lock (_gate) return _snap;
        }
    }

    public HistoryStore History => _service.History;
    public PinnedSensorStore Pinned { get; private set; } = null!;
    public string BackendLabel => _service.BackendLabel;
    public bool PawnIoConnected => _service.PawnIoConnected;
    public string? PawnIoError => _service.PawnIoError;

    public bool MergeLoadInDock { get; set; } = true;

    public void ConfigurePins(string pinsPath)
    {
        Pinned = new PinnedSensorStore(pinsPath);
    }

    public void SetIntervalMs(int ms)
    {
        _intervalMs = Math.Clamp(ms, 500, 10000);
    }

    public async Task StartAsync()
    {
        if (_loop is not null) return;
        await _service.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
        _cts = new CancellationTokenSource();
        _loop = RunLoopAsync(_cts.Token);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var snap = await _service.SampleAsync(ct).ConfigureAwait(false);
                lock (_gate) _snap = snap;

                if (!_defaultsApplied && Pinned is not null)
                {
                    _defaultsApplied = true;
                    Pinned.EnsureDefaults(snap.Sensors);
                }

                // 供浏览器看板读取的真实快照（live-data.js）
                LiveSnapshotWriter.Write(snap, History);

                SnapshotUpdated?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // keep last snapshot
            }

            try
            {
                await Task.Delay(_intervalMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _service.Dispose();
        }
        catch
        {
            // ignore
        }
    }
}
