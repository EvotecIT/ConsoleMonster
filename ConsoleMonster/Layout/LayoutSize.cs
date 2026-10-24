using System;
using System.Globalization;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// A size specification: <see cref="Auto"/>, <see cref="Fixed(int)"/> cells, or a <see cref="Fraction(double, int)"/>
    /// of the remaining space with a minimum.
    /// </summary>
    public readonly struct LayoutSize {
        private LayoutSize(LayoutSizeKind kind, double value, int min) {
            Kind = kind;
            Value = value;
            Min = Math.Max(0, min);
        }

        /// <summary>The kind.</summary>
        public LayoutSizeKind Kind { get; }

        /// <summary>Cells for <see cref="LayoutSizeKind.Fixed"/>, weight for <see cref="LayoutSizeKind.Fraction"/>.</summary>
        public double Value { get; }

        /// <summary>Minimum cells (used before a layout collapses or clips).</summary>
        public int Min { get; }

        /// <summary>Size to content.</summary>
        public static LayoutSize Auto => new LayoutSize(LayoutSizeKind.Auto, 0, 0);

        /// <summary>A fixed number of cells.</summary>
        /// <param name="cells">Cells.</param>
        /// <returns>The specification.</returns>
        public static LayoutSize Fixed(int cells) => new LayoutSize(LayoutSizeKind.Fixed, Math.Max(0, cells), Math.Max(0, cells));

        /// <summary>A share of the remaining space.</summary>
        /// <param name="weight">Relative weight (default 1).</param>
        /// <param name="min">Minimum cells.</param>
        /// <returns>The specification.</returns>
        public static LayoutSize Fraction(double weight = 1, int min = 0) => new LayoutSize(LayoutSizeKind.Fraction, weight <= 0 ? 1 : weight, min);

        /// <summary>Returns a copy with a different minimum.</summary>
        /// <param name="min">Minimum cells.</param>
        /// <returns>The specification.</returns>
        public LayoutSize WithMinimum(int min) => new LayoutSize(Kind, Value, min);

        /// <inheritdoc />
        public override string ToString() {
            switch (Kind) {
                case LayoutSizeKind.Fixed:
                    return Value.ToString(CultureInfo.InvariantCulture);
                case LayoutSizeKind.Fraction:
                    return Value.ToString(CultureInfo.InvariantCulture) + "fr";
                default:
                    return "auto";
            }
        }
    }
}
