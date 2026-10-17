using System.Threading.Channels;

#pragma warning disable CA1822 // Instance members match the real SensorService contract.

namespace HardwareMonitorExtension.Services;

// Deterministic inputs for the linked production SensorHub; no hardware or dashboard I/O.
public sealed class SensorService : IDisposable
{
    public static Channel<SensorSnapshot> Samples { get; set; } = Channel.CreateUnbounded<SensorSnapshot>();
    public HistoryStore History { get; } = new();
    public string BackendLabel => "Test";
    public Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;
    public async Task<SensorSnapshot> SampleAsync(CancellationToken ct) =>
        await Samples.Reader.ReadAsync(ct);
    public void Dispose() { }
}

public static class LiveSnapshotWriter
{
    public static void Write(SensorSnapshot snapshot, HistoryStore history) { }
}
