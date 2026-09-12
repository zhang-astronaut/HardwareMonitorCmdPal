// Copyright (c) HardwareMonitor. MIT license.

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using HardwareMonitorExtension.Services;

namespace HardwareMonitorExtension.Services;

/// <summary>
/// 用 GDI+ 生成折线图 PNG。Command Palette 无 Canvas API，ContentPage 用 ImageContent 展示位图。
/// </summary>
public static class ChartRenderer
{
    public static byte[] RenderLineChart(
        IReadOnlyList<(DateTimeOffset Ts, double Value)> points,
        string title,
        string unit,
        bool isTemperature,
        int width = 720,
        int height = 280)
    {
        width = Math.Clamp(width, 160, 1200);
        height = Math.Clamp(height, 80, 600);

        using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(28, 28, 32));

        if (points.Count < 2)
        {
            using var emptyFont = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
            g.DrawString("采样点不足，正在积累…", emptyFont, Brushes.Gold, 16, 16);
            return EncodePng(bmp);
        }

        var padL = 48;
        var padR = 16;
        var padT = 36;
        var padB = 28;
        var plotW = width - padL - padR;
        var plotH = height - padT - padB;

        var values = points.Select(p => p.Value).ToArray();
        var minV = values.Min();
        var maxV = values.Max();
        if (isTemperature)
        {
            minV = Math.Min(minV, 30);
            maxV = Math.Max(maxV, Math.Ceiling(maxV / 10) * 10 + 5);
        }

        if (maxV - minV < 1) maxV = minV + 1;

        using var titleFont = new Font("Segoe UI", 13f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var labelFont = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var gridPen = new Pen(Color.FromArgb(58, 58, 64), 1f);
        using var axisPen = new Pen(Color.FromArgb(90, 90, 98), 1f);

        g.DrawString(title, titleFont, Brushes.WhiteSmoke, padL, 10);

        // grid + Y labels
        for (var i = 0; i <= 4; i++)
        {
            var y = padT + plotH * i / 4f;
            g.DrawLine(gridPen, padL, y, padL + plotW, y);
            var val = maxV - (maxV - minV) * i / 4f;
            g.DrawString($"{val:0}", labelFont, Brushes.Gray, 4, y - 8);
        }

        g.DrawLine(axisPen, padL, padT, padL, padT + plotH);
        g.DrawLine(axisPen, padL, padT + plotH, padL + plotW, padT + plotH);

        // threshold lines for temperature
        if (isTemperature)
        {
            DrawThreshold(g, ThresholdPolicy.HotC, minV, maxV, padL, padT, plotW, plotH, Color.FromArgb(255, 140, 66), "过热");
            DrawThreshold(g, ThresholdPolicy.CriticalC, minV, maxV, padL, padT, plotW, plotH, Color.FromArgb(255, 90, 90), "危险");
        }

        var pts = new PointF[points.Count];
        for (var i = 0; i < points.Count; i++)
        {
            var t = i / (float)(points.Count - 1);
            var norm = (points[i].Value - minV) / (maxV - minV);
            pts[i] = new PointF(padL + t * plotW, padT + (1 - (float)norm) * plotH);
        }

        var last = points[^1].Value;
        Color lineColor = !isTemperature
            ? Color.FromArgb(96, 205, 255)
            : ThresholdPolicy.Classify(last) switch
            {
                HeatLevel.Critical => Color.FromArgb(255, 90, 90),
                HeatLevel.Hot => Color.FromArgb(255, 140, 66),
                HeatLevel.Warm => Color.FromArgb(230, 184, 77),
                _ => Color.FromArgb(108, 203, 122),
            };

        // fill under curve
        using (var fill = new GraphicsPath())
        {
            fill.AddLines(pts);
            fill.AddLine(pts[^1].X, padT + plotH, pts[0].X, padT + plotH);
            fill.CloseFigure();
            using var brush = new LinearGradientBrush(
                new RectangleF(padL, padT, plotW, plotH),
                Color.FromArgb(70, lineColor),
                Color.FromArgb(5, lineColor),
                LinearGradientMode.Vertical);
            g.FillPath(brush, fill);
        }

        using var linePen = new Pen(lineColor, 2.2f);
        g.DrawLines(linePen, pts);
        using var dotBrush = new SolidBrush(lineColor);
        g.FillEllipse(dotBrush, pts[^1].X - 4, pts[^1].Y - 4, 8, 8);

        g.DrawString($"当前 {last:0}{unit}  ·  区间 {minV:0}–{maxV:0}  ·  点数 {points.Count}",
            labelFont, Brushes.Gainsboro, padL, height - 20);

        return EncodePng(bmp);
    }

    /// <summary>Dock 用迷你火花线（透明底、细线）。</summary>
    public static byte[] RenderSparkline(
        IReadOnlyList<(DateTimeOffset Ts, double Value)> points,
        bool isTemperature,
        int width = 72,
        int height = 28)
    {
        using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        if (points.Count < 2)
            return EncodePng(bmp);

        var values = points.Select(p => p.Value).ToArray();
        var minV = values.Min();
        var maxV = values.Max();
        if (maxV - minV < 1e-6) maxV = minV + 1;

        var pad = 2f;
        var pts = new PointF[points.Count];
        for (var i = 0; i < points.Count; i++)
        {
            var t = i / (float)(points.Count - 1);
            var norm = (points[i].Value - minV) / (maxV - minV);
            pts[i] = new PointF(pad + t * (width - pad * 2), pad + (1 - (float)norm) * (height - pad * 2));
        }

        var last = values[^1];
        Color c = !isTemperature
            ? Color.FromArgb(96, 205, 255)
            : ThresholdPolicy.Classify(last) switch
            {
                HeatLevel.Critical => Color.FromArgb(255, 90, 90),
                HeatLevel.Hot => Color.FromArgb(255, 140, 66),
                HeatLevel.Warm => Color.FromArgb(230, 184, 77),
                _ => Color.FromArgb(108, 203, 122),
            };

        using var pen = new Pen(c, 1.6f);
        g.DrawLines(pen, pts);
        using var brush = new SolidBrush(c);
        g.FillEllipse(brush, pts[^1].X - 2.2f, pts[^1].Y - 2.2f, 4.4f, 4.4f);
        return EncodePng(bmp);
    }

    private static void DrawThreshold(
        Graphics g, double v, double minV, double maxV,
        float padL, float padT, float plotW, float plotH, Color color, string label)
    {
        if (v < minV || v > maxV) return;
        var y = padT + (1 - (float)((v - minV) / (maxV - minV))) * plotH;
        using var pen = new Pen(Color.FromArgb(120, color), 1f) { DashStyle = DashStyle.Dash };
        g.DrawLine(pen, padL, y, padL + plotW, y);
        using var font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        g.DrawString($"{label} {v:0}°", font, brush, padL + plotW - 70, y - 14);
    }

    private static byte[] EncodePng(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
}
