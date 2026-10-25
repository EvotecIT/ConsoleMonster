using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>What a presenter produced for one value: styled text, an optional small graphic and layout hints.</summary>
    public sealed class PresentedValue {
        /// <summary>Creates a presented value.</summary>
        /// <param name="line">The styled text.</param>
        /// <param name="alignment">The preferred alignment, or null to let the column decide.</param>
        public PresentedValue(StyledLine line, HorizontalAlignment? alignment = null) {
            Line = line ?? throw new ArgumentNullException(nameof(line));
            Alignment = alignment;
        }

        /// <summary>An empty value.</summary>
        public static PresentedValue Empty { get; } = new PresentedValue(StyledLine.Empty);

        /// <summary>The styled text.</summary>
        public StyledLine Line { get; }

        /// <summary>The plain text (for measuring, middle truncation and line output).</summary>
        public string Text => Line.PlainText;

        /// <summary>The preferred alignment (numbers prefer right), or null.</summary>
        public HorizontalAlignment? Alignment { get; }

        /// <summary>The tone the value carries (status, booleans, tone rules), or null.</summary>
        public ThemeToken? Role { get; set; }

        /// <summary>The graphic kind drawn in the cell.</summary>
        public CellVisualKind Visual { get; set; }

        /// <summary>The fraction (0-1) for <see cref="CellVisualKind.Bar"/> and <see cref="CellVisualKind.Progress"/>.</summary>
        public double Fraction { get; set; }

        /// <summary>The series for <see cref="CellVisualKind.Sparkline"/>.</summary>
        public IReadOnlyList<double>? Series { get; set; }

        /// <summary>Cells the graphic would like (the table may give it less).</summary>
        public int VisualWidth { get; set; } = 10;

        /// <summary>The numeric value behind the text (for totals, data bars and tone rules), or null.</summary>
        public double? Number { get; set; }

        /// <summary>Whether the value is empty (null, empty string), so record views can hide it.</summary>
        public bool IsEmpty { get; set; }
    }
}
