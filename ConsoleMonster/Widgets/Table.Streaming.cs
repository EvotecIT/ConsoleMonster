using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;

namespace ConsoleMonster.Widgets {
    public partial class Table {
        // The grouped streaming composition freezes only the initial bounded sample's negotiated grid.
        internal TableLayout StreamingLayout(int width, Canvas.RenderContext context) => ComputeLayout(width, context);

        /// <summary>
        /// What a streaming layout learns from each row while it is measured: the column widths it wants and needs, the key
        /// column's widths (for the comfortable key width) and how many values align right (automatic alignment).
        /// </summary>
        private sealed class StreamStats {
            private readonly Table _table;
            private readonly int[] _desired;
            private readonly int[] _minimum;
            private readonly int[] _right;
            private readonly int[] _other;
            private readonly List<int> _keyWidths = new List<int>();

            public StreamStats(Table table, int count, int[] desired, int[] minimum) {
                _table = table;
                _desired = desired;
                _minimum = minimum;
                _right = new int[count];
                _other = new int[count];
            }

            public void Add(CellContent[] cells) {
                for (int c = 0; c < _desired.Length && c < cells.Length; c++) {
                    Measure(cells[c], _table._columns[c], ref _desired[c], ref _minimum[c], ref _table._freeText[c]);
                    CellContent content = cells[c];
                    if (content.Presented != null && !content.IsEmpty) {
                        if (content.Alignment == HorizontalAlignment.Right) {
                            _right[c]++;
                        } else {
                            _other[c]++;
                        }
                    }
                }
                int key = _table.KeyColumn;
                if (key < cells.Length) {
                    _keyWidths.Add(cells[key].TextWidth);
                }
            }

            public int KeyComfort() {
                if (_keyWidths.Count == 0) {
                    return KeyComfortWidth;
                }
                _keyWidths.Sort();
                int index = Math.Min(_keyWidths.Count - 1, (int)Math.Ceiling(_keyWidths.Count * 0.8) - 1);
                return Math.Max(16, Math.Min(KeyComfortWidth, _keyWidths[Math.Max(0, index)]));
            }

            public HorizontalAlignment? Alignment(int column) {
                if (_right[column] + _other[column] == 0) {
                    return null;
                }
                return _right[column] > _other[column] ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
        }
    }
}
