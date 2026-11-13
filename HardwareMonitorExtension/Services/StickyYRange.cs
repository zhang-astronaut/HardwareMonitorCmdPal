// Copyright (c) HardwareMonitor. MIT license.

using System.Collections.Concurrent;

namespace HardwareMonitorExtension.Services;

/// <summary>
/// 每个传感器独立的 Y 轴包络。
/// 规则：
/// - 曲线触碰边界 → 该边界向外扩展；
/// - 曲线未触碰 → 边界保持不变（永不收缩，也不会因 padding 叠加而持续外扩）。
/// </summary>
/// <summary>
/// Independent Y-axis envelope for each sensor.
/// Rules:
/// - Curve touches boundary -> boundary expands outward;
/// - Curve does not touch -> boundary remains unchanged (never contracts, no runaway padding).
/// </summary>
/// <summary>
/// センサーごとに独立した Y 軸エンベロープ。
/// ルール：
/// - 曲線が境界に接触 → その境界を外側に拡張。
/// - 曲線が未接触 → 境界は変更なし（縮小せず、パディングの重複による不要な継続拡張もなし）。
/// </summary>
public sealed class StickyYRange
{
    private readonly ConcurrentDictionary<string, (double Min, double Max)> _map = new();
    private readonly double _padRatio;
    private readonly double _minSpan;

    public StickyYRange(double padRatio = 0.04, double minSpan = 1.0)
    {
        _padRatio = padRatio;
        _minSpan = minSpan;
    }

    public (double Lower, double Upper)? Expand(string key, IReadOnlyList<double> values)
    {
        if (values is null || values.Count == 0) return null;
        var finite = values.Where(double.IsFinite).ToList();
        if (finite.Count == 0) return null;

        var dataMin = finite.Min();
        var dataMax = finite.Max();
        if (dataMax - dataMin < 1e-9)
        {
            dataMin -= 0.5;
            dataMax += 0.5;
        }

        var range = _map.AddOrUpdate(
            key,
            _ => WithInitialPad(dataMin, dataMax),
            (_, old) => ExpandOnlyIfTouched(old, dataMin, dataMax));

        return range;
    }

    private (double, double) ExpandOnlyIfTouched(
        (double Min, double Max) old,
        double dataMin,
        double dataMax)
    {
        var lo = old.Min;
        var hi = old.Max;
        var expanded = false;

        // 仅当数据真正越过当前边界时才外扩；否则完全不动
        // Only expand outward when data genuinely crosses current boundary; otherwise remain unchanged
        // データが現在の境界を実際に超えた場合のみ外側に拡張し、それ以外は一切変更しません
        if (dataMin < lo)
        {
            lo = dataMin;
            expanded = true;
        }

        if (dataMax > hi)
        {
            hi = dataMax;
            expanded = true;
        }

        if (!expanded)
            return old; // 曲线未触碰 → 原样返回，边界不缩也不扩
                        // Curve did not touch -> return original; boundary neither shrinks nor expands
                        // 曲線が未接触 → 元の値を返却（境界は縮小も拡大もしない）

        // 触碰后扩到新极值；只在跨度不足时补最小跨度，不叠加百分比 padding
        // Expand to new extreme upon contact; enforce minimum span only when span is insufficient, without stacking percentage padding
        // 接触後は新しい極値まで拡張。スパンが不足している場合のみ最小スパンを補正し、パーセンテージ パディングの累積は行いません
        if (hi - lo < _minSpan)
        {
            var mid = (lo + hi) / 2.0;
            lo = mid - _minSpan / 2.0;
            hi = mid + _minSpan / 2.0;
        }

        return (lo, hi);
    }

    /// <summary>仅首次见到该序列时做一次视觉留白。</summary>
    /// <summary>Apply visual padding only when the sequence is first observed.</summary>
    /// <summary>シーケンスが初めて検出されたときのみ、視覚的な余白（パディング）を適用します。</summary>
    private (double, double) WithInitialPad(double min, double max)
    {
        var span = Math.Max(max - min, _minSpan);
        var pad = span * _padRatio;
        var lo = min - pad;
        var hi = max + pad;
        if (hi - lo < _minSpan)
        {
            var mid = (lo + hi) / 2.0;
            lo = mid - _minSpan / 2.0;
            hi = mid + _minSpan / 2.0;
        }

        return (lo, hi);
    }

    public bool TryGet(string key, out (double Min, double Max) range) =>
        _map.TryGetValue(key, out range);

    public void Reset(string key) => _map.TryRemove(key, out _);

    public void Clear() => _map.Clear();
}

/// <summary>进程内共享：列表/详情页共用同一套只增不减包络。</summary>
/// <summary>In-process shared: list and detail pages share the same monotonically expanding envelope.</summary>
/// <summary>プロセス内共有：リスト ページと詳細ページで、単調増加（縮小しない）エンベロープを同一共有。</summary>
public static class StickyYRanges
{
    public static readonly StickyYRange Default = new();
}
