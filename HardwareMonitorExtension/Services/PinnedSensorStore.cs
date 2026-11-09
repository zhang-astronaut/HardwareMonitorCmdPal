// Copyright (c) HardwareMonitor. MIT license.

namespace HardwareMonitorExtension.Services;

/// <summary>用户固定到 Dock 的读数 Id 列表（JSON 文件持久化）。</summary>
public sealed class PinnedSensorStore
{
    /// <summary>
    /// 当前默认固定方案的版本号。提高它会让仍在使用旧方案的安装
    /// 在下次启动时重新应用新默认（Dock 空间有限，默认方案需要随设计演进）。
    /// </summary>
    public const int CurrentDefaultVersion = 2;

    private readonly object _gate = new();
    private readonly string _path;
    private List<string> _ids;
    private int _version;

    public event EventHandler? Changed;

    public PinnedSensorStore(string path)
    {
        _path = path;
        (_ids, _version) = Load(path);
    }

    /// <summary>存储的固定列表是否仍来自旧版默认方案。</summary>
    public bool NeedsDefaultRefresh => _version < CurrentDefaultVersion;

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

    /// <summary>
    /// 默认固定方案 v2（精简版）：只保留 Dock 上最有价值的 4 项——
    /// CPU 封装温度、CPU 占用、GPU 温度、GPU 占用。
    /// 其余读数（存储/主板/内存温度、风扇、功率估算等）在列表页全量可查，不占 Dock。
    /// </summary>
    public void EnsureDefaults(IEnumerable<SensorReading> sensors)
    {
        lock (_gate)
        {
            if (_ids.Count > 0 && !NeedsDefaultRefresh) return;

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
            AddPreferred(list.Where(s => (s.Group == HardwareGroups.Processor || s.Group == "处理器") && s.Kind == SensorKind.Temperature)
                .OrderByDescending(s => s.ShortLabel.Contains("封装", StringComparison.OrdinalIgnoreCase) || s.ShortLabel.Contains("Package", StringComparison.OrdinalIgnoreCase)), 1);
            AddPreferred(list.Where(s => (s.Group == HardwareGroups.Processor || s.Group == "处理器") && s.Kind == SensorKind.Load), 1);
            AddPreferred(list.Where(s => (s.Group == HardwareGroups.Graphics || s.Group == "显卡") && s.Kind == SensorKind.Temperature), 1);
            AddPreferred(list.Where(s => (s.Group == HardwareGroups.Graphics || s.Group == "显卡") && s.Kind == SensorKind.Load), 1);

            // 机器缺 GPU/读数异常时兜底，保证 Dock 不空
            if (picks.Count < 2)
            {
                picks = list.Where(s => s.Kind == SensorKind.Temperature || s.Kind == SensorKind.Load)
                            .Take(4).Select(s => s.Id).ToList();
            }

            _ids = picks;
            _version = CurrentDefaultVersion;
            Save();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private (List<string> Ids, int Version) Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return ([], 0);
            var json = File.ReadAllText(path);
            // v2 格式：{"version":2,"ids":[...]}；v1 为纯数组
            try
            {
                var obj = System.Text.Json.JsonSerializer.Deserialize<Stored>(json);
                if (obj?.Ids is not null) return (obj.Ids, obj.Version);
            }
            catch (System.Text.Json.JsonException)
            {
                var ids = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? [];
                return (ids, 0);
            }

            return ([], 0);
        }
        catch
        {
            return ([], 0);
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
