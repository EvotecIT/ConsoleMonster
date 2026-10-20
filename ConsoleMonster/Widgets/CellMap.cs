using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// One cell per item of a run, in item order, flowing to the width: each state has its own symbol (✓ ✗ ▴ – ○ in Full
    /// and Basic, <c>+ x ! - .</c> in ASCII), so outcomes read without colour; running items spin; a legend counts each
    /// state. Generic: tests, hosts, files, any list of work.
    /// <para>Live operations: <c>set</c> (Key = item index, State = status word), <c>add</c> (State, optional), <c>count</c>
    /// (Value = the number of items, new ones pending).</para>
    /// </summary>
    public sealed class CellMap : LiveWidget {
        /// <summary>Which rows a map shows when it has more than <see cref="MaxRows"/>.</summary>
        public enum CellWindow {
            /// <summary>The first rows, then "+N more" (default).</summary>
            Head,

            /// <summary>The rows that end at the first running cell (or the first pending one, or the last cell), with "+N earlier" above and "+N more" below, so a long run stays live.</summary>
            Active
        }

        private static readonly StatusKind[] LegendOrder = { StatusKind.Pass, StatusKind.Fail, StatusKind.Warn, StatusKind.Skip, StatusKind.Info, StatusKind.Running, StatusKind.Pending };
        private readonly List<StatusKind> _cells = new List<StatusKind>();

        /// <summary>Creates a cell map.</summary>
        /// <param name="name">The live name (default <c>cells</c>).</param>
        public CellMap(string name = "cells") : base(name) {
        }

        /// <summary>The most items a map holds (1,000,000): live events past it are ignored, so a mistyped index cannot exhaust memory.</summary>
        public const int MaxItems = 1000000;

        /// <summary>The number of items.</summary>
        public int Count => _cells.Count;

        /// <summary>The cells between symbols (default 1).</summary>
        public int Gap { get; set; } = 1;

        /// <summary>Whether a legend line with each state's count follows the cells (default true).</summary>
        public bool ShowLegend { get; set; } = true;

        /// <summary>The most rows of cells (0 = all); a last line then counts the items not shown.</summary>
        public int MaxRows { get; set; }

        /// <summary>Which rows show when there are more than <see cref="MaxRows"/> (default <see cref="CellWindow.Head"/>).</summary>
        public CellWindow Window { get; set; }

        /// <summary>Whether running cells spin (default true); false shows the still running symbol, when another part of the screen already spins.</summary>
        public bool SpinRunning { get; set; } = true;

        /// <summary>The state of an item.</summary>
        /// <param name="index">The item index.</param>
        /// <returns>The state.</returns>
        public StatusKind this[int index] => _cells[index];

        /// <summary>Adds an item.</summary>
        /// <param name="state">Its state (default pending).</param>
        /// <returns>The new item's index.</returns>
        public int Add(StatusKind state = StatusKind.Pending) {
            _cells.Add(state);
            return _cells.Count - 1;
        }

        /// <summary>Sets the number of items: new ones are pending, extra ones are removed.</summary>
        /// <param name="count">The count.</param>
        /// <returns>This instance.</returns>
        public CellMap Resize(int count) {
            count = Math.Max(0, Math.Min(MaxItems, count));
            while (_cells.Count < count) {
                _cells.Add(StatusKind.Pending);
            }
            if (_cells.Count > count) {
                _cells.RemoveRange(count, _cells.Count - count);
            }
            return this;
        }

        /// <summary>Sets an item's state, growing the map when the index is past the end.</summary>
        /// <param name="index">The item index.</param>
        /// <param name="state">The state.</param>
        /// <returns>This instance.</returns>
        public CellMap Set(int index, StatusKind state) {
            if (index < 0 || index >= MaxItems) {
                throw new ArgumentOutOfRangeException(nameof(index), "An item index runs from 0 to " + (MaxItems - 1).ToString(CultureInfo.InvariantCulture) + ".");
            }
            if (index >= _cells.Count) {
                Resize(index + 1);
            }
            _cells[index] = state;
            return this;
        }

        /// <summary>How many items are in a state.</summary>
        /// <param name="state">The state.</param>
        /// <returns>The count.</returns>
        public int CountOf(StatusKind state) {
            int count = 0;
            foreach (StatusKind cell in _cells) {
                if (cell == state) {
                    count++;
                }
            }
            return count;
        }

        /// <summary>A count with the word for its state in the current language: "12 passed", "3 failed", "45 queued".</summary>
        /// <param name="state">The state.</param>
        /// <param name="count">The count.</param>
        /// <param name="context">The render context (labels and culture).</param>
        /// <returns>The text.</returns>
        public static string CountLabel(StatusKind state, int count, RenderContext context) {
            Localization.WidgetLabels labels = context.Labels;
            string number = count.ToString("N0", context.Culture);
            switch (state) {
                case StatusKind.Pass:
                    return Localization.WidgetLabels.Format(labels.TestsPassedCount, number);
                case StatusKind.Fail:
                    return Localization.WidgetLabels.Format(labels.FailedCount, number);
                case StatusKind.Skip:
                    return Localization.WidgetLabels.Format(labels.SkippedCount, number);
                case StatusKind.Running:
                    return Localization.WidgetLabels.Format(labels.RunningCount, number);
                case StatusKind.Pending:
                    return Localization.WidgetLabels.Format(labels.QueuedCount, number);
                case StatusKind.Warn:
                    return Localization.WidgetLabels.Format(labels.WarningCount, number);
                default:
                    return number + " " + labels.StatusWord(state).ToLower(context.Culture);
            }
        }

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent == null) {
                return;
            }
            StatusKinds.TryParse(liveEvent.State, out StatusKind state);
            switch (liveEvent.Operation) {
                case "set":
                    if (int.TryParse(liveEvent.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) && index >= 0 && index < MaxItems) {
                        Set(index, state);
                    }
                    break;
                case "add":
                    if (_cells.Count < MaxItems) {
                        Add(string.IsNullOrEmpty(liveEvent.State) ? StatusKind.Pending : state);
                    }
                    break;
                case "count":
                    // Only a real count: a missing, negative or NaN value would wipe the map.
                    if (!double.IsNaN(liveEvent.Value) && liveEvent.Value > 0) {
                        Resize((int)Math.Min(MaxItems, liveEvent.Value));
                    }
                    break;
            }
        }

        private int PerRow(int width) => Math.Max(1, (width + Gap) / (1 + Gap));

        private int CellRows(int width) {
            int rows = (_cells.Count + PerRow(width) - 1) / PerRow(width);
            // A cut map adds the "+N more" line; an active window's "+N earlier" line takes one of its rows.
            return MaxRows > 0 && rows > MaxRows ? MaxRows + 1 : rows;
        }

        // The row the active window ends at: the first running cell, else the first pending one, else the last cell.
        private int AnchorRow(int perRow) {
            int anchor = _cells.IndexOf(StatusKind.Running);
            if (anchor < 0) {
                anchor = _cells.IndexOf(StatusKind.Pending);
            }
            if (anchor < 0) {
                anchor = _cells.Count - 1;
            }
            return Math.Max(0, anchor) / perRow;
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int width = Math.Max(1, constraints.MaxWidth);
            int rows = CellRows(width) + (ShowLegend && _cells.Count > 0 ? 1 : 0);
            return new Size(width, Math.Min(rows, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0 || _cells.Count == 0) {
                return;
            }
            int perRow = PerRow(region.Width);
            int totalRows = (_cells.Count + perRow - 1) / perRow;
            int rows = MaxRows > 0 && totalRows > MaxRows ? MaxRows : totalRows;
            int legendRows = ShowLegend ? 1 : 0;
            // The legend keeps its row; the "+N more" line takes one more only when cells are left out.
            int room = Math.Max(0, region.Height - legendRows);
            if (rows > room || rows < totalRows) {
                rows = Math.Max(0, Math.Min(rows, room - 1));
            }
            int first = 0;
            int y = 0;
            if (Window == CellWindow.Active && rows < totalRows && rows > 0) {
                first = Math.Max(0, Math.Min(totalRows - rows, AnchorRow(perRow) - rows + 1));
                if (first > 0 && rows > 1) {
                    // "+N earlier" takes the window's first row.
                    rows--;
                    first++;
                    region.Write(0, y++, Localization.WidgetLabels.Format(context.Labels.EarlierCount, (first * perRow).ToString("N0", context.Culture)), context.Style(ThemeToken.Muted));
                }
            }
            for (int row = first; row < first + rows; row++, y++) {
                for (int column = 0; column < perRow; column++) {
                    int index = row * perRow + column;
                    if (index >= _cells.Count) {
                        break;
                    }
                    StatusKind state = _cells[index];
                    string symbol = state == StatusKind.Running && SpinRunning ? context.Spinner(index) : context.Glyphs.StatusSymbol(state);
                    Style style = state == StatusKind.Pending ? context.Style(ThemeToken.Muted) : context.Style(state);
                    region.Put(column * (1 + Gap), y, symbol, style);
                }
            }
            int hidden = _cells.Count - Math.Min(_cells.Count, (first + rows) * perRow);
            if (hidden > 0 && y < region.Height - legendRows) {
                region.Write(0, y++, Localization.WidgetLabels.Format(context.Labels.MoreCount, hidden.ToString("N0", context.Culture)), context.Style(ThemeToken.Muted));
            }
            if (ShowLegend && y < region.Height) {
                region.Write(0, y, TextWrapper.Truncate(Legend(context), region.Width, context.Ellipsis));
            }
        }

        /// <summary>The legend: each state present with its symbol and count ("✓ 12 passed  ✗ 3 failed  ○ 45 queued").</summary>
        /// <param name="context">The render context.</param>
        /// <returns>The legend line.</returns>
        public StyledLine Legend(RenderContext context) {
            List<TextSpan> spans = new List<TextSpan>();
            foreach (StatusKind state in LegendOrder) {
                int count = CountOf(state);
                if (count == 0) {
                    continue;
                }
                if (spans.Count > 0) {
                    spans.Add(new TextSpan("  "));
                }
                Style style = state == StatusKind.Pending ? context.Style(ThemeToken.Muted) : context.Style(state);
                spans.Add(new TextSpan(context.Glyphs.StatusSymbol(state) + " ", style));
                spans.Add(new TextSpan(CountLabel(state, count, context), state == StatusKind.Pending ? context.Style(ThemeToken.Muted) : style));
            }
            return new StyledLine(spans);
        }
    }
}
