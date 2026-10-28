using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>How a table is drawn for the width it was given.</summary>
    internal enum TableMode {
        Grid = 0,
        Cards,
        Record
    }

    /// <summary>The kind of a laid-out table row.</summary>
    internal enum TableRowKind {
        Data = 0,
        Group,
        Subtotal,
        Total
    }

    /// <summary>One laid-out row: fitted cell lines per visible column, or a spanning group heading.</summary>
    internal sealed class TableLayoutRow {
        internal TableLayoutRow(TableRowKind kind) {
            Kind = kind;
        }

        internal TableRowKind Kind { get; }

        internal CellContent[]? Cells { get; set; }

        internal List<StyledLine>[]? Lines { get; set; }

        internal int[]? VisualCells { get; set; }

        internal StyledLine? Spanning { get; set; }

        internal int Height { get; set; } = 1;

        internal List<StyledLine>? Below { get; set; }

        internal int BodyHeight { get; set; } = 1;

        /// <summary>The source row index for a streaming layout, whose cells are presented again just before drawing.</summary>
        internal int Source { get; set; } = -1;
    }

    /// <summary>The negotiated layout of a table for one width.</summary>
    internal sealed class TableLayout {
        internal TableLayout(TableMode mode) {
            Mode = mode;
        }

        internal TableMode Mode { get; }

        internal IWidget? Record { get; set; }

        /// <summary>Column grids rendered separately so streaming never buffers the whole band container.</summary>
        internal IReadOnlyList<Table>? Bands { get; set; }

        internal int Gap { get; set; }

        internal int[] Gaps { get; set; } = new int[0];

        /// <summary>
        /// Whether the first body row is a group heading drawn in place of the header separator (a labelled rule closes the
        /// header and opens the first group on one line).
        /// </summary>
        internal bool MergeFirstGroup { get; set; }

        internal int[] Visible { get; set; } = new int[0];

        internal int[] Widths { get; set; } = new int[0];

        internal HorizontalAlignment[] Alignments { get; set; } = new HorizontalAlignment[0];

        internal List<StyledLine>[] HeaderLines { get; set; } = new List<StyledLine>[0];

        internal int HeaderHeight { get; set; }

        internal List<TableLayoutRow> Rows { get; } = new List<TableLayoutRow>();

        internal TableLayoutRow? Total { get; set; }

        internal List<string> Hidden { get; } = new List<string>();

        internal List<int> Below { get; } = new List<int>();

        internal StyledLine? HiddenMarker { get; set; }

        internal StyledLine? ShowingLine { get; set; }

        internal List<StyledLine> HintLines { get; } = new List<StyledLine>();

        internal int TotalWidth { get; set; }

        internal int AvailableWidth { get; set; }

        internal int TotalHeight { get; set; }
    }
}
