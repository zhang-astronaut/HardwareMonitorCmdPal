using System.Text.Json;
using System.Threading.Channels;
using HardwareMonitorExtension;
using HardwareMonitorExtension.Services;

var directory = Path.Combine(Path.GetTempPath(), "HardwareMonitorPinTests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
var failures = 0;
var tests = new (string Name, Func<Task> Run)[]
{
    ("New store selects CPU/GPU defaults and persists them", () => Run(() =>
    {
        var path = NewPath();
        var store = new PinnedSensorStore(path);
        store.EnsureDefaults(Sensors());
        Equal(["cpu-temp", "cpu-load", "gpu-temp", "gpu-load"], store.Ids);
        Equal(store.Ids, new PinnedSensorStore(path).Ids);
        Check(!store.NeedsDefaultRefresh, "Default selection should be complete.");
    })),
    ("Unpinning every default survives a restart", () => Run(() =>
    {
        var path = NewPath();
        var store = new PinnedSensorStore(path);
        store.EnsureDefaults(Sensors());
        foreach (var id in store.Ids) store.Unpin(id);
        var restarted = new PinnedSensorStore(path);
        restarted.EnsureDefaults(Sensors());
        Equal([], restarted.Ids);
    })),
    ("Explicit empty selection before discovery survives restart", () => Run(() =>
    {
        var path = NewPath();
        new PinnedSensorStore(path).Set([]);
        var restarted = new PinnedSensorStore(path);
        restarted.EnsureDefaults(Sensors());
        Equal([], restarted.Ids);
    })),
    ("Legacy custom pins survive default-version changes", () => Run(() =>
    {
        var path = NewPath();
        File.WriteAllText(path, "[\"custom-fan\"]");
        var store = new PinnedSensorStore(path);
        store.EnsureDefaults(Sensors());
        Equal(["custom-fan"], store.Ids);
    })),
    ("Old versioned custom pins survive default-version changes", () => Run(() =>
    {
        var path = NewPath();
        File.WriteAllText(path, JsonSerializer.Serialize(new { Version = 1, Ids = new List<string> { "custom-power" } }));
        var store = new PinnedSensorStore(path);
        store.EnsureDefaults(Sensors());
        Equal(["custom-power"], store.Ids);
    })),
    ("Legacy empty selection stays empty", () => Run(() =>
    {
        var path = NewPath();
        File.WriteAllText(path, "[]");
        var store = new PinnedSensorStore(path);
        store.EnsureDefaults(Sensors());
        Equal([], store.Ids);
    })),
    ("Pinning before discovery preserves the user's choice", () => Run(() =>
    {
        var path = NewPath();
        var store = new PinnedSensorStore(path);
        store.Pin("custom-fan");
        store.EnsureDefaults(Sensors());
        Equal(["custom-fan"], store.Ids);
        Equal(store.Ids, new PinnedSensorStore(path).Ids);
    })),
    ("Empty discovery stays pending without writing a file", () => Run(() =>
    {
        var path = NewPath();
        var store = new PinnedSensorStore(path);
        var changes = 0;
        store.Changed += (_, _) => changes++;
        store.EnsureDefaults([]);
        Check(store.NeedsDefaultRefresh, "An empty discovery must stay pending.");
        Check(!File.Exists(path), "Empty discovery must not persist a selection.");
        Check(changes == 0, "No selection change should be reported.");
        store.EnsureDefaults(Sensors());
        Check(store.Ids.Count == 4 && changes == 1, "Later discovery should select defaults once.");
    })),
    ("Non-default sensor kinds leave discovery pending", () => Run(() =>
    {
        var store = new PinnedSensorStore(NewPath());
        store.EnsureDefaults([Reading("fan", "主板", SensorKind.Fan)]);
        Check(store.NeedsDefaultRefresh, "No usable defaults must stay pending.");
        store.EnsureDefaults(Sensors());
        Check(store.Ids.Count == 4, "A later usable snapshot should initialize defaults.");
    })),
    ("Set keeps case-insensitive deduplication", () => Run(() =>
    {
        var path = NewPath();
        var store = new PinnedSensorStore(path);
        store.Set(["fan", "FAN", "", " ", "power"]);
        store.Pin("FAN");
        Equal(["fan", "power"], new PinnedSensorStore(path).Ids);
        store.Unpin("FAN");
        Equal(["power"], store.Ids);
    })),
    ("Hub retries defaults after its first empty snapshot", async () =>
    {
        SensorService.Samples = Channel.CreateUnbounded<SensorSnapshot>();
        using var hub = new SensorHub();
        hub.ConfigurePins(NewPath());
        hub.SetIntervalMs(500);
        var updates = Channel.CreateUnbounded<bool>();
        hub.SnapshotUpdated += (_, _) => updates.Writer.TryWrite(true);
        await hub.StartAsync();
        await SensorService.Samples.Writer.WriteAsync(new(DateTimeOffset.UtcNow, [], true));
        await updates.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        await SensorService.Samples.Writer.WriteAsync(new(DateTimeOffset.UtcNow, Sensors(), false));
        await updates.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        Check(hub.Pinned.Ids.Count == 4, "Hub should apply defaults when usable readings arrive.");
    }),
};

try
{
    foreach (var test in tests)
    {
        try { await test.Run(); Console.WriteLine("PASS " + test.Name); }
        catch (Exception ex) { failures++; Console.WriteLine("FAIL " + test.Name + ": " + ex.Message); }
    }
    Console.WriteLine($"{tests.Length - failures}/{tests.Length} passed");
    return failures == 0 ? 0 : 1;
}
finally
{
    // Exact directory created by this process, containing only this run's fixtures.
    foreach (var fixture in Directory.EnumerateFiles(directory)) File.Delete(fixture);
    Directory.Delete(directory);
}

string NewPath() => Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
static Task Run(Action test) { test(); return Task.CompletedTask; }
static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
static void Equal(IEnumerable<string> expected, IEnumerable<string> actual) =>
    Check(expected.SequenceEqual(actual), $"Expected [{string.Join(",", expected)}], got [{string.Join(",", actual)}].");
static SensorReading Reading(string id, string group, SensorKind kind) =>
    new(id, group, id, "Test", kind, 42, "", "Test", id);
static SensorReading[] Sensors() =>
[
    Reading("cpu-temp", "处理器", SensorKind.Temperature),
    Reading("cpu-load", "处理器", SensorKind.Load),
    Reading("gpu-temp", "显卡", SensorKind.Temperature),
    Reading("gpu-load", "显卡", SensorKind.Load),
];
