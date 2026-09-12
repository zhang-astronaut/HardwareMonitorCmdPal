// Copyright (c) HardwareMonitor. MIT license.

namespace HardwareMonitorExtension.Services;

/// <summary>用户固定到 Dock 的读数 Id 列表（JSON 文件持久化）。</summary>
public sealed class PinnedSensorStore
{
    private readonly object _gate = new();
    private readonly string _path;
    private List<string> _ids;

    public event EventHandler? Changed;

    public PinnedSensorStore(string path)
    {
        _path = path;
        _ids = Load(path);
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
            Save();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>首次运行：每组取 1–2 条代表读数（温度/占用/功率等），而非只有 CPU/GPU。</summary>
    public void EnsureDefaults(IEnumerable<SensorReading> sensors)
    {
        lock (_gate)
        {
            if (_ids.Count > 0) return;
            var list = sensors.ToList();
            var picks = new List<string>();
            void AddRange(IEnumerable<SensorReading> q, int n)
            {
                foreach (var s in q.Take(n))
                {
                    if (!picks.Contains(s.Id, StringComparer.OrdinalIgnoreCase))
                        picks.Add(s.Id);
                }
            }

            foreach (var g in new[] { "处理器", "显卡", "存储", "主板", "内存" })
            {
                AddRange(list.Where(s => s.Group == g && s.Kind == SensorKind.Temperature), 1);
                AddRange(list.Where(s => s.Group == g && s.Kind == SensorKind.Load), 1);
            }

            AddRange(list.Where(s => s.Kind == SensorKind.Power), 2);
            if (picks.Count == 0)
                picks = list.Take(6).Select(s => s.Id).ToList();

            _ids = picks.Take(10).ToList();
            Save();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static List<string> Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return [];
            var json = File.ReadAllText(path);
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, System.Text.Json.JsonSerializer.Serialize(_ids));
        }
        catch
        {
            // ignore
        }
    }
}
