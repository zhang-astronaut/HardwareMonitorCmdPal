using System;

namespace AsciiChart.Sharp
{
    public class Options
    {
        int _axisLabelLeftMargin = 1;
        int _axisLabelRightMargin = 1;

        public int AxisLabelLeftMargin
        {
            get => _axisLabelLeftMargin;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Margin must be >= 0");
                _axisLabelLeftMargin = value;
            }
        }

        public int AxisLabelRightMargin
        {
            get => _axisLabelRightMargin;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Margin must be >= 0");
                _axisLabelRightMargin = value;
            }
        }

        public int? Height { get; set; }

        public char Fill { get; set; } = ' ';

        public string AxisLabelFormat { get; set; } = "0.00";

        /// <summary>固定 Y 下界（与 StickyYRange 配合实现只增不减）。</summary>
        public double? LowerBound { get; set; }

        /// <summary>固定 Y 上界。</summary>
        public double? UpperBound { get; set; }

        public AnsiColor AxisColor { get; set; }

        public AnsiColor LabelColor { get; set; }

        public AnsiColor[] SeriesColors { get; set; }
    }
}
