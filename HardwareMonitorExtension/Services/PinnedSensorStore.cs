// Copyright (c) HardwareMonitor. MIT license.

namespace HardwareMonitorExtension.Services;

/// <summary>用户固定到 Dock 的读数 Id 列表（JSON 文件持久化）。</summary>
public sealed class PinnedSensorStore
{
    /// <summary>
    /// Version recorded when defaults are first selected or the user edits pins.
    /// Existing files do not record whether pins were customized, so preserve them.
    /// </summary>
    public const int CurrentDefaultVersion = 2;

    private readonly object _gate = new();
    private readonly string _path;
    private List<string> _ids;
    private int _version;
    private bool _hasConfiguration;

    public event EventHandler? Changed;

    public PinnedSensorStore(string path)
    {
        _path = path;
        (_ids, _version, _hasConfiguration) = Load(path);
    }

    /// <summary>Whether initial defaults are still waiting for usable sensor readings.</summary>
    public bool NeedsDefaultRefresh
    {
        get { lock (_gate) return !_hasConfiguration; }
    }

    public IReadOnlyList<string> Ids
    {
        get
        {
            lock (_gate) return _ids.ToList();
        }
    }

    public bool IsPinned(string id)
    {
        lock (_gate) return _ids.Contains(id, StringComparer.OrdinalIgnoreCase);
    }

    public void Pin(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        lock (_gate)
        {
            if (_ids.Contains(id, StringComparer.OrdinalIgnoreCase)) return;
            _ids.Add(id);
            MarkConfigured();
            Save();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Unpin(string id)
    {
        lock (_gate)
        {
            var before = _ids.Count;
            _ids.RemoveAll(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase));
            if (_ids.Count == before) return;
            MarkConfigured();
            Save();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Toggle(string id)
    {
        if (IsPinned(id)) Unpin(id);
        else Pin(id);
    }

    public void Set(IEnumerable<string> ids)
    {
        lock (_gate)
        {
            _ids = ids.Where(x => !string.IsNullOrWhiteSpace(x))
                      .Distinct(StringComparer.OrdinalIgnoreCase)
                      .ToList();
            MarkConfigured();
            Save();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 默认固定方案 v2（精简版）：只保留 Dock 上最有价值的 4 项——
    /// CPU 封装温度、CPU 占用、GPU 温度、GPU 占用。
    /// 其余读数（存储/主板/内存温度、风扇、功率估算等）在列表页全量可查，不占 Dock。
    /// </summary>
    public void EnsureDefaults(IEnumerable<SensorReading> sensors)
    {
        lock (_gate)
        {
            if (_hasConfiguration) return;

            var list = sensors.ToList();
            var picks = new List<string>();

            void AddPreferred(IEnumerable<SensorReading> q, int n)
            {
                foreach (var s in q.Take(n))
                {
                    if (!picks.Contains(s.Id, StringComparer.OrdinalIgnoreCase))
                        picks.Add(s.Id);
                }
            }

            // CPU 封装温度优先；GPU 取首条温度
            AddPreferred(list.Where(s => s.Group == "处理器" && s.Kind == SensorKind.Temperature)
                .OrderByDescending(s => s.ShortLabel.Contains("封装", StringComparison.Ordinal)), 1);
            AddPreferred(list.Where(s => s.Group == "处理器" && s.Kind == SensorKind.Load), 1);
            AddPreferred(list.Where(s => s.Group == "显卡" && s.Kind == SensorKind.Temperature), 1);
            AddPreferred(list.Where(s => s.Group == "显卡" && s.Kind == SensorKind.Load), 1);

            // 机器缺 GPU/读数异常时兜底，保证 Dock 不空
            if (picks.Count < 2)
            {
                picks = list.Where(s => s.Kind == SensorKind.Temperature || s.Kind == SensorKind.Load)
                            .Take(4).Select(s => s.Id).ToList();
            }

            if (picks.Count == 0) return;

            _ids = picks;
            MarkConfigured();
            Save();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void MarkConfigured()
    {
        _version = CurrentDefaultVersion;
        _hasConfiguration = true;
    }

    private static (List<string> Ids, int Version, bool HasConfiguration) Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return ([], 0, false);
            var json = File.ReadAllText(path);
            // v2 格式：{"version":2,"ids":[...]}；v1 为纯数组
            try
            {
                var obj = System.Text.Json.JsonSerializer.Deserialize<Stored>(json);
                if (obj?.Ids is not null) return (obj.Ids, obj.Version, true);
            }
            catch (System.Text.Json.JsonException)
            {
                var ids = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                if (ids is not null) return (ids, 0, true);
            }

            return ([], 0, false);
        }
        catch
        {
            return ([], 0, false);
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, System.Text.Json.JsonSerializer.Serialize(
                new Stored(_version, _ids)));
        }
        catch
        {
            // ignore
        }
    }

    private sealed record Stored(int Version, List<string> Ids);
}
