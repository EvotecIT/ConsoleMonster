using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>The computed geometry of a <see cref="Table"/> for one width.</summary>
    internal sealed class TableLayout {
        public TableLayout(int columns, int gap) {
            Widths = new int[columns];
            Alignments = new HorizontalAlignment[columns];
            HeaderLines = new List<StyledLine>[columns];
            Gap = gap;
        }

        public int Gap { get; }

        public int[] Widths { get; set; }

        public HorizontalAlignment[] Alignments { get; }

        public List<StyledLine>[] HeaderLines { get; }

        public int HeaderHeight { get; set; }

        public List<List<StyledLine>[]> RowLines { get; } = new List<List<StyledLine>[]>();

        public List<int> RowHeights { get; } = new List<int>();

        public List<StyledLine>[]? FooterLines { get; set; }

        public int FooterHeight { get; set; }

        public int TotalWidth { get; set; }

        public int TotalHeight { get; set; }
    }
}
