using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    internal sealed partial class GridView {
        // Filters (every word must appear in a shown column) and sorts the rows when the filter, sort or columns changed.
        private void Refresh() {
            string filter = _filter.Text.Trim();
            if (string.Equals(filter, _appliedFilter, StringComparison.Ordinal)) {
                return;
            }
            int focused = _cursor >= 0 && _cursor < _visible.Count ? _visible[_cursor] : -1;
            _appliedFilter = filter;
            string[] words = filter.Length == 0 ? new string[0] : filter.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<int> shown = ShownColumns();
            List<int> rows = new List<int>();
            for (int i = 0; i < _grid.Rows.Count; i++) {
                if (Matches(_grid.Rows[i], words, shown)) {
                    rows.Add(i);
                }
            }
            _visible = Sorted(rows);
            int keep = focused < 0 ? -1 : _visible.IndexOf(focused);
            _cursor = keep >= 0 ? keep : Math.Min(Math.Max(0, _cursor), Math.Max(0, _visible.Count - 1));
            _table = null;
        }

        private static bool Matches(GridRow row, string[] words, List<int> shown) {
            foreach (string word in words) {
                bool found = false;
                foreach (int c in shown) {
                    if (row.Search![c].IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    return false;
                }
            }
            return true;
        }

        private List<int> Sorted(IEnumerable<int> rows) {
            List<int> list = new List<int>(rows);
            if (_sortColumn < 0) {
                return list;
            }
            int column = _sortColumn;
            int sign = _descending ? -1 : 1;
            list.Sort((a, b) => {
                int result = Compare(_grid.Rows[a], _grid.Rows[b], column);
                return result != 0 ? sign * result : a.CompareTo(b);
            });
            return list;
        }

        // Numbers as numbers, dates as dates, other comparable values by their own order, the rest as text; empty values last.
        private int Compare(GridRow a, GridRow b, int column) {
            object? x = column < a.Values.Count ? a.Values[column] : null;
            object? y = column < b.Values.Count ? b.Values[column] : null;
            bool xEmpty = PresenterValues.IsEmpty(x);
            bool yEmpty = PresenterValues.IsEmpty(y);
            if (xEmpty || yEmpty) {
                return xEmpty == yEmpty ? 0 : xEmpty ? 1 : -1;
            }
            if (!(x is string) && !(y is string) && PresenterValues.TryGetNumber(x, out double dx) && PresenterValues.TryGetNumber(y, out double dy)) {
                return dx.CompareTo(dy);
            }
            if (x is IComparable cx && y != null && x.GetType() == y.GetType()) {
                try {
                    return x is string ? string.Compare((string)x, (string)y, _culture, CompareOptions.IgnoreCase) : cx.CompareTo(y);
                } catch (ArgumentException) {
                }
            }
            return string.Compare(a.Search![column], b.Search![column], _culture, CompareOptions.IgnoreCase);
        }

        private Table BuildTable(RenderContext context) {
            Table table = new Table { Border = TableBorder.Minimal, CardsBelow = 0, RecordViewRows = 0, Presentation = _grid.Presentation, StableWidths = false };
            bool marks = _grid.Selection == GridSelection.Multiple;
            if (marks) {
                table.AddColumn(new TableColumn(" ") { IsKey = true, Width = 1, Alignment = HorizontalAlignment.Left });
            }
            List<int> shown = ShownColumns();
            foreach (int c in shown) {
                TableColumn source = _grid.Columns[c];
                TableColumn column = new TableColumn(source.Header) {
                    Key = source.Key,
                    Alignment = source.Alignment,
                    Presenter = source.Presenter,
                    Priority = source.Priority,
                    IsKey = source.IsKey,
                    Flexible = source.Flexible,
                    MinWidth = source.MinWidth,
                    MaxWidth = source.MaxWidth,
                    Width = source.Width,
                    Overflow = ColumnOverflow.Truncate,
                    MaxLines = 1
                };
                foreach (RoleRule rule in source.RoleRules) {
                    column.RoleRules.Add(rule);
                }
                table.AddColumn(column);
            }
            string check = Markup.Escape(context.Glyphs.Get(Glyph.Check));
            foreach (int index in _visible) {
                GridRow row = _grid.Rows[index];
                List<TableCell> cells = new List<TableCell>();
                if (marks) {
                    cells.Add(new TableCell(_marked.Contains(index) ? "[accent bold]" + check + "[/]" : string.Empty));
                }
                foreach (int c in shown) {
                    cells.Add(TableCell.FromValue(c < row.Values.Count ? row.Values[c] : null));
                }
                table.AddRow(cells.ToArray());
            }
            return table;
        }

        /// <summary>The visible rows: a window of the table, the focused row on the Focus background.</summary>
        private sealed class GridBody : IWidget {
            private readonly GridView _view;

            public GridBody(GridView view) {
                _view = view;
            }

            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? 10 : constraints.MaxHeight);

            public void Render(Region region, RenderContext context) {
                GridView view = _view;
                view.Refresh();
                if (view._table == null || view._tableTier != context.Glyphs.Tier) {
                    view._table = view.BuildTable(context);
                    view._tableTier = context.Glyphs.Tier;
                }
                int head = 2;
                int count = Math.Max(1, region.Height - head);
                view._pageRows = Math.Max(1, count - 1);
                if (view._cursor < view._top) {
                    view._top = view._cursor;
                } else if (view._cursor >= view._top + count) {
                    view._top = view._cursor - count + 1;
                }
                view._top = Math.Max(0, Math.Min(view._top, Math.Max(0, view._visible.Count - count)));
                CellBuffer buffer = view._table.RenderWindow(region.Width, view._top, count, context, out int headHeight, out List<int> rowLines, out string? hidden);
                for (int y = 0; y < buffer.Height && y < region.Height; y++) {
                    for (int x = 0; x < buffer.Width && x < region.Width; x++) {
                        Cell cell = buffer[x, y];
                        if (!cell.IsContinuation) {
                            region.Put(x, y, cell.Glyph, cell.Style, cell.Width);
                        }
                    }
                }
                if (view._visible.Count == 0) {
                    region.Write(0, Math.Min(region.Height - 1, headHeight), view.Labels.NoMatches, context.Style(ThemeToken.Muted));
                    return;
                }
                int focused = view._cursor - view._top;
                if (focused >= 0 && focused < rowLines.Count && rowLines[focused] < region.Height) {
                    Style focus = context.Style(ThemeToken.Focus);
                    int y = rowLines[focused];
                    for (int x = 0; x < region.Width; x++) {
                        Cell cell = region.Get(x, y);
                        Style style = context.HasColor ? cell.Style.WithBackgroundColor(focus.BackgroundColor).With(TextDecoration.Bold) : cell.Style.With(TextDecoration.Reverse);
                        region.SetStyle(x, y, style);
                    }
                }
                if (hidden != null && region.Height > 0) {
                    string note = TextWrapper.Truncate(hidden, region.Width, context.Ellipsis);
                    region.Write(Math.Max(0, region.Width - DisplayWidth.Measure(note)), region.Height - 1, note, context.Style(ThemeToken.Muted));
                }
            }
        }
    }

    /// <summary>The focused row as a record (every column, including hidden ones); Up and Down move to the next row.</summary>
    internal sealed class GridDetailModal : Modal {
        private readonly GridView _view;

        public GridDetailModal(GridView view) {
            _view = view;
        }

        public override int MaxWidth => 72;

        public override IWidget Build(ViewFrame frame, CaretSlot caret) {
            GridRow? row = _view.FocusedRow;
            RecordView record = new RecordView { Presentation = _view.Grid.Presentation, HideEmpty = false };
            string title = string.Empty;
            if (row != null) {
                for (int c = 0; c < _view.Grid.Columns.Count; c++) {
                    TableColumn column = _view.Grid.Columns[c];
                    object? value = c < row.Values.Count ? row.Values[c] : null;
                    if (title.Length == 0 && !PresenterValues.IsEmpty(value)) {
                        title = row.Search![c];
                    }
                    record.AddField(Markup.Strip(column.Header), TableCell.FromValue(value), column.Presenter, column.RoleRules.Count > 0 ? new List<RoleRule>(column.RoleRules) : null);
                }
            }
            Rows rows = new Rows();
            rows.Add(record);
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, frame);
            rows.Add(new KeyFooter(bindings));
            return DialogChrome.Frame(rows, title.Length == 0 ? _view.Grid.Title : title);
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            bindings.Add(new KeyBinding(_view.Labels.KeyMove, () => _view.StepFocus(-1), new KeyChord(ConsoleKey.UpArrow)) { KeyText = InteractiveGlyphs.For(frame.Context).KeyUpDown, Order = 10 });
            bindings.Add(new KeyBinding(string.Empty, () => _view.StepFocus(1), new KeyChord(ConsoleKey.DownArrow)) { InFooter = false });
            bindings.Add(new KeyBinding(_view.Labels.KeyClose, Close, new KeyChord(ConsoleKey.Escape), new KeyChord(ConsoleKey.Enter)) { KeyText = "esc", Order = 40 });
        }
    }

    /// <summary>The column chooser: Space shows or hides the focused column (one always stays).</summary>
    internal sealed class GridColumnsModal : Modal {
        private readonly GridView _view;
        private int _focus;

        public GridColumnsModal(GridView view) {
            _view = view;
        }

        public override int MaxWidth => 48;

        public override IWidget Build(ViewFrame frame, CaretSlot caret) {
            Rows rows = new Rows();
            rows.Add(new UiLine(context => StyledLine.Empty));
            for (int c = 0; c < _view.Grid.Columns.Count; c++) {
                int column = c;
                rows.Add(new UiLine(context => {
                    InteractiveGlyphs g = InteractiveGlyphs.For(context);
                    bool shown = !_view.IsHidden(column);
                    bool focused = column == _focus;
                    Style text = focused ? context.Style(ThemeToken.Focus).With(TextDecoration.Bold) : context.Style(ThemeToken.Text);
                    return new StyledLine(new[] {
                        new TextSpan(focused ? "▌" : " ", context.Style(ThemeToken.Accent)),
                        new TextSpan(shown ? "[" + context.Glyphs.Get(Glyph.Check) + "] " : "[ ] ", shown ? context.Style(ThemeToken.Accent) : context.Style(ThemeToken.Subtle)),
                        new TextSpan(Markup.Strip(_view.Grid.Columns[column].Header), text)
                    });
                }));
            }
            List<KeyBinding> bindings = new List<KeyBinding>();
            AddBindings(bindings, frame);
            rows.Add(new KeyFooter(bindings));
            return DialogChrome.Frame(rows, _view.Labels.Columns);
        }

        public override void AddBindings(List<KeyBinding> bindings, ViewFrame frame) {
            int count = _view.Grid.Columns.Count;
            bindings.Add(new KeyBinding(_view.Labels.KeyMove, () => _focus = (_focus + count - 1) % count, new KeyChord(ConsoleKey.UpArrow)) { KeyText = InteractiveGlyphs.For(frame.Context).KeyUpDown, Order = 10 });
            bindings.Add(new KeyBinding(string.Empty, () => _focus = (_focus + 1) % count, new KeyChord(ConsoleKey.DownArrow)) { InFooter = false });
            bindings.Add(new KeyBinding(_view.Labels.KeyToggle, () => _view.SetHidden(_focus, !_view.IsHidden(_focus)), new KeyChord(ConsoleKey.Spacebar), new KeyChord(' ')) { KeyText = "space", Order = 20 });
            bindings.Add(new KeyBinding(_view.Labels.KeyClose, Close, new KeyChord(ConsoleKey.Escape), new KeyChord(ConsoleKey.Enter)) { KeyText = "esc", Order = 40 });
        }
    }

    /// <summary>Runs a <see cref="Grid"/> on the screen, or as a numbered list in line mode.</summary>
    internal static class GridRunner {
        public static GridResult Run(Grid grid, GridOptions options, System.Threading.CancellationToken cancellationToken) {
            // Capability discovery can enable VT. Capture the caller's output mode before resolving labels or profiles.
            using ConsoleMonster.Output.TerminalGuard outputMode = new ConsoleMonster.Output.TerminalGuard(() => { }, false);
            InteractiveOptions interactive = options.Interactive ?? InteractiveOptions.Default;
            PromptLabels labels = interactive.ResolveLabels();
            switch (interactive.ResolveMode()) {
                case InteractionMode.None:
                    throw new PromptUnavailableException("Grid '" + grid.Title + "' needs an interactive session.") { Prompt = grid.Title };
                case InteractionMode.Line:
                case InteractionMode.Accessible:
                    return Lines(grid, new LinePrompter(interactive.ResolveLineConsole(), labels, interactive.ResolveMode() == InteractionMode.Accessible), cancellationToken);
            }
            using (InputLease lease = ConsoleInput.Acquire(interactive.Input)) {
                Capabilities.TerminalProfile profile = interactive.ResolveProfile();
                RenderContext context = Terminal.CreateContext(profile, interactive.ResolveTheme());
                IPromptSurface? surface = interactive.Surface;
                bool owns = false;
                if (surface == null) {
                    surface = PromptHost.Resolve(context) ?? new ScreenSurface(profile);
                    owns = true;
                }
                try {
                    GridView view = new GridView(grid, options, labels);
                    InteractionLoop.Run(view, lease.Source, surface, context, cancellationToken);
                    surface.Complete(view.Summary(context));
                    return new GridResult(view.Accepted, view.Accepted ? view.ChosenItems() : new List<object?>());
                } finally {
                    if (owns) {
                        surface.Dispose();
                    }
                }
            }
        }

        private static GridResult Lines(Grid grid, LinePrompter lines, System.Threading.CancellationToken cancellationToken) {
            List<Choice> choices = new List<Choice>();
            CultureInfo culture = grid.Presentation.Culture ?? CultureInfo.CurrentCulture;
            foreach (GridRow row in grid.Rows) {
                List<string> parts = new List<string>();
                foreach (object? value in row.Values) {
                    parts.Add(PresenterValues.ToText(value, culture));
                }
                choices.Add(new Choice(row.Item, string.Join(" | ", parts)));
            }
            List<object?> items = new List<object?>();
            if (grid.Selection == GridSelection.Multiple) {
                foreach (int index in lines.MultiSelect(grid.Title, null, choices, null, null, cancellationToken)) {
                    items.Add(grid.Rows[index].Item);
                }
            } else {
                int index = lines.Select(grid.Title, null, choices, 0, cancellationToken);
                if (grid.Selection == GridSelection.Single && index >= 0) {
                    items.Add(grid.Rows[index].Item);
                }
            }
            return new GridResult(true, items);
        }
    }
}
