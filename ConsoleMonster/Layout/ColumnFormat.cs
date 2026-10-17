using System;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// How a widget lays out one of its stat columns (counts, percentages, values, details) across rows. Every row of a
    /// widget shares the column's width, so numbers line up; during a live session the width never shrinks, so columns
    /// do not jump when a value grows from 9 to 10 or drops back.
    /// </summary>
    /// <remarks>
    /// The width of a column is the largest of: its widest current value, <see cref="MinWidth"/>, the width reserved by
    /// <see cref="ReserveDigits"/> (numeric columns), the width of a known maximum the widget supplies (a lane total, an
    /// overall total), and the widest width the column has used so far.
    /// </remarks>
    public sealed class ColumnFormat {
        private int _minWidth;
        private int _reserveDigits;

        /// <summary>Creates a column format.</summary>
        /// <param name="alignment">Alignment of values inside the column.</param>
        /// <param name="minWidth">Minimum width in cells.</param>
        /// <param name="reserveDigits">Digits reserved for numeric values (0 = only what the widget knows or has seen).</param>
        public ColumnFormat(HorizontalAlignment alignment = HorizontalAlignment.Right, int minWidth = 0, int reserveDigits = 0) {
            Alignment = alignment;
            MinWidth = minWidth;
            ReserveDigits = reserveDigits;
        }

        /// <summary>Alignment of values inside the column; numbers default to right.</summary>
        public HorizontalAlignment Alignment { get; set; }

        /// <summary>Minimum width in cells (never negative).</summary>
        public int MinWidth {
            get => _minWidth;
            set => _minWidth = Math.Max(0, value);
        }

        /// <summary>
        /// Digits reserved for numeric values, for example 4 to keep room for 9999 from the first frame. Zero (the default)
        /// reserves only what the widget knows (its totals) or has already displayed.
        /// </summary>
        public int ReserveDigits {
            get => _reserveDigits;
            set => _reserveDigits = Math.Max(0, value);
        }

        /// <summary>When false, the column follows its current values and may shrink; defaults to true.</summary>
        public bool NeverShrink { get; set; } = true;
    }
}
