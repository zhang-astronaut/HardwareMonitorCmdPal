// Copyright (c) HardwareMonitor. MIT license.
// 趋势图：使用 MIT 项目 asciichart-sharp（kroitor/asciichart 的 C# 移植），
// 输出标准 ╭─╮ 盒线字符折线，非位图。

using System.Text;
using HardwareMonitorExtension.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace HardwareMonitorExtension.Pages;

internal sealed partial class SensorChartPage : ContentPage
{
    private readonly SensorHub _hub;
    private readonly string _sensorId;
    private bool _hooked;
    private DateTimeOffset _lastRefresh = DateTimeOffset.MinValue;

    public SensorChartPage(SensorHub hub, string sensorId)
    {
        _hub = hub;
        _sensorId = sensorId;
        Icon = new IconInfo("📈");
        Title = "读数详情";
        Name = "查看";
        Commands =
        [
            new CommandContextItem(new OpenDashboardCommand())
            {
                Title = "打开实时图表看板",
                Icon = new IconInfo("🌐"),
            },
        ];
        EnsureHubHook();
    }

    private void EnsureHubHook()
    {
        if (_hooked) return;
        _hooked = true;
        _hub.SnapshotUpdated += (_, _) =>
        {
            try
            {
                if (DateTimeOffset.UtcNow - _lastRefresh < TimeSpan.FromSeconds(2))
                    return;
                RaiseItemsChanged();
            }
            catch
            {
                // ignore
            }
        };
    }

    public override IContent[] GetContent()
    {
        EnsureHubHook();
        _lastRefresh = DateTimeOffset.UtcNow;

        var snap = _hub.Snapshot;
        var sensor = snap.Find(_sensorId);
        if (sensor is null)
            return [new MarkdownContent($"# 未找到读数\n\n`{_sensorId}` 不在当前快照中。")];

        var series = _hub.History.Snapshot(_sensorId);

        var sb = new StringBuilder();
        sb.AppendLine($"# {sensor.ShortLabel}  {sensor.DisplayValue}");
        sb.AppendLine();
        sb.AppendLine($"{sensor.Name}");
        sb.AppendLine();
        sb.AppendLine($"- 来源：`{sensor.Source}`");
        sb.AppendLine($"- 硬件：{sensor.HardwareName} · 分组：{sensor.Group}");
        sb.AppendLine($"- 历史点：{series.Count}/180");
        sb.AppendLine();

        if (series.Count < 2)
        {
            sb.AppendLine("_采样中，稍候即可看到趋势…_");
            return [new MarkdownContent(sb.ToString())];
        }

        // 降采样并保留时间戳，便于画 X 轴
        var maxPoints = 48;
        var step = Math.Max(1, series.Count / maxPoints);
        var picked = series.Where((_, i) => i % step == 0).ToList();
        if (picked.Count >= 2 && picked[^1].Ts != series[^1].Ts)
            picked.Add(series[^1]);

        var sampled = picked.Select(p => p.Value).ToArray();
        var isTemp = sensor.Kind == SensorKind.Temperature;

        // Y 轴只增不减：首次见到的极值进入包络后永不收缩
        var sticky = StickyYRanges.Default.Expand(_sensorId, sampled);
        var options = new AsciiChart.Sharp.Options
        {
            Height = 10,
            AxisLabelLeftMargin = 3,
            AxisLabelRightMargin = 0,
            AxisLabelFormat = isTemp ? "0.0" : "0.##",
            LowerBound = sticky?.Lower,
            UpperBound = sticky?.Upper,
        };

        string chart;
        try
        {
            chart = AsciiChart.Sharp.AsciiChart.Plot(sampled, options);
            chart = System.Text.RegularExpressions.Regex.Replace(chart, "\x1b\\[[0-9;]*m", string.Empty);
        }
        catch
        {
            chart = string.Join(" ", sampled.Select(v => v.ToString("0.#")));
        }

        // 在图下方追加时间轴（asciichart-sharp 本身无 X 轴 API）
        var timeAxis = BuildTimeAxis(picked);

        sb.AppendLine("## 趋势（asciichart · 代码绘制，非位图）");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine(chart);
        sb.AppendLine(timeAxis);
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine($"当前 **{sensor.DisplayValue}** · 单位 `{sensor.Unit}` · 窗口约 3 分钟 · 点 {sampled.Length}/{series.Count}");

        if (isTemp)
        {
            sb.AppendLine();
            sb.AppendLine($"阈值：偏热 {ThresholdPolicy.WarmC:0}° · 过热 {ThresholdPolicy.HotC:0}° · 危险 {ThresholdPolicy.CriticalC:0}°");
        }

        sb.AppendLine();
        sb.AppendLine("## 近期采样");
        sb.AppendLine();
        sb.AppendLine("| 时间 | 值 |");
        sb.AppendLine("| --- | --- |");
        foreach (var (ts, v) in series.TakeLast(8).Reverse())
        {
            var text = isTemp ? $"{v:0}°C" : $"{v:0.#}{sensor.Unit}";
            sb.AppendLine($"| {ts.ToLocalTime():HH:mm:ss} | {text} |");
        }

        return [new MarkdownContent(sb.ToString())];
    }

    /// <summary>
    /// 根据首尾时间生成与图表等宽的 X 轴标签行。
    /// 左侧留出与 Y 轴标签相近的缩进（约 8 字符）。
    /// </summary>
    private static string BuildTimeAxis(
        IReadOnlyList<(DateTimeOffset Ts, double Value)> points)
    {
        if (points.Count < 2) return string.Empty;

        var t0 = points[0].Ts.ToLocalTime();
        var t1 = points[^1].Ts.ToLocalTime();
        var mid = points[points.Count / 2].Ts.ToLocalTime();

        // 与 asciichart 左缘对齐：Offset(3) + 列宽
        const string pad = "        "; // 8 spaces
        var left = t0.ToString("HH:mm:ss");
        var center = mid.ToString("HH:mm:ss");
        var right = t1.ToString("HH:mm:ss");

        // 在一条基线上均匀放置 3 个时间戳
        var span = 56; // 图宽近似
        var line = new char[span];
        for (var i = 0; i < span; i++) line[i] = ' ';

        void Place(string text, int col)
        {
            if (col < 0) col = 0;
            for (var i = 0; i < text.Length && col + i < span; i++)
                line[col + i] = text[i];
        }

        Place(left, 0);
        Place(center, Math.Max(left.Length + 2, (span - center.Length) / 2));
        Place(right, Math.Max(0, span - right.Length));

        return pad + new string(line).TrimEnd();
    }
}
