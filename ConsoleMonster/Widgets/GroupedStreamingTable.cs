using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Rendering;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Streams contiguous groups with fixed sampled column widths and one aggregate summary at each group boundary.
    /// Keeps at most 200 sampled rows and the current aggregate, rather than buffering a group or the whole input.
    /// A repeated group key after another group starts a new group. Early disposal does not declare a group complete.
    /// </summary>
    /// <typeparam name="T">The input record.</typeparam>
    /// <typeparam name="TState">The caller-owned aggregate state; use bounded state to retain bounded memory.</typeparam>
    public sealed class GroupedStreamingTable<T, TState> {
        private readonly List<TableColumn> _columns;
        private readonly Func<T, string> _group;
        private readonly Func<T, TableCell[]> _row;
        private readonly Func<string, TState> _seed;
        private readonly Func<TState, T, TState> _aggregate;
        private readonly Func<string, TState, long, IWidget?> _summary;

        /// <summary>Creates a streaming table. The summary is invoked exactly once per fully read contiguous group.</summary>
        /// <param name="columns">Data columns, copied without modifying the caller's definitions.</param>
        /// <param name="group">The plain text group key, compared ordinally.</param>
        /// <param name="row">Projects one record to cells in column order.</param>
        /// <param name="seed">Creates an aggregate when a group opens.</param>
        /// <param name="aggregate">Updates the aggregate for one record.</param>
        /// <param name="summary">Creates the closed group's summary using key, aggregate and row count; null omits it.</param>
        public GroupedStreamingTable(IEnumerable<TableColumn> columns, Func<T, string> group, Func<T, TableCell[]> row,
            Func<string, TState> seed, Func<TState, T, TState> aggregate, Func<string, TState, long, IWidget?> summary) {
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            _columns = new List<TableColumn>();
            foreach (TableColumn column in columns) _columns.Add(RoleTable.CopyColumn(column ?? throw new ArgumentException("Columns cannot be null.", nameof(columns))));
            if (_columns.Count == 0) throw new ArgumentException("At least one column is required.", nameof(columns));
            _group = group ?? throw new ArgumentNullException(nameof(group));
            _row = row ?? throw new ArgumentNullException(nameof(row));
            _seed = seed ?? throw new ArgumentNullException(nameof(seed));
            _aggregate = aggregate ?? throw new ArgumentNullException(nameof(aggregate));
            _summary = summary ?? throw new ArgumentNullException(nameof(summary));
        }

        /// <summary>Rows sampled once for stable widths, from 1 to 200 (default 200). Does not grow with group size.</summary>
        public int SampleRows {
            get => _sampleRows;
            set {
                if (value < 1 || value > 200) throw new ArgumentOutOfRangeException(nameof(value));
                _sampleRows = value;
            }
        }
        private int _sampleRows = 200;

        /// <summary>Culture, clock and time zone used by cell presenters.</summary>
        public Presenters.PresentationOptions Presentation { get; set; } = new Presenters.PresentationOptions();

        /// <summary>Table border style for this stream (ordinary streams retain no border).</summary>
        public TableBorder Border { get; set; } = TableBorder.None;

        /// <summary>Whether each group repeats the column header; false prints one header before the first group.</summary>
        public bool RepeatHeaderPerGroup { get; set; } = true;

        /// <summary>
        /// Lazily renders group headings, data rows and closure summaries. Disposing the result disposes the input.
        /// The first block reads at most <see cref="SampleRows"/> records; subsequent records stream one at a time.
        /// Columns hidden by the sampled layout are named once. Widths stay fixed for this enumeration.
        /// </summary>
        /// <param name="source">The ordered record stream; it is enumerated once.</param>
        /// <param name="width">Output width in cells.</param>
        /// <param name="context">The existing render capabilities, theme and glyphs.</param>
        /// <returns>Append-only rendered blocks in input order.</returns>
        public IEnumerable<CellBuffer> RenderChunks(IEnumerable<T> source, int width, RenderContext context) {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return Read(source, width, context);
        }

        private IEnumerable<CellBuffer> Read(IEnumerable<T> source, int width, RenderContext context) {
            using (IEnumerator<T> input = source.GetEnumerator()) {
                Queue<Entry> sample = new Queue<Entry>();
                Table measuring = NewTable(_columns, true);
                for (int i = 0; i < SampleRows && input.MoveNext(); i++) {
                    Entry entry = Project(input.Current);
                    sample.Enqueue(entry);
                    measuring.AddRow(entry.Cells);
                }
                if (sample.Count == 0) yield break;
                TableLayout layout = measuring.StreamingLayout(width, context);
                bool cards = layout.Mode == TableMode.Cards;
                int[] selected = layout.Visible;
                if (cards) {
                    selected = new int[_columns.Count];
                    for (int c = 0; c < selected.Length; c++) selected[c] = c;
                } else if (layout.Below.Count > 0) {
                    int[] complete = new int[selected.Length + layout.Below.Count];
                    Array.Copy(selected, complete, selected.Length);
                    layout.Below.CopyTo(complete, selected.Length);
                    selected = complete;
                }
                List<TableColumn> frozen = new List<TableColumn>();
                foreach (int c in selected) {
                    TableColumn copy = RoleTable.CopyColumn(_columns[c]);
                    int v = frozen.Count;
                    if (!cards && v < layout.Visible.Length) {
                        copy.Width = layout.Widths[v];
                        copy.MinWidth = null;
                        copy.MaxWidth = null;
                        copy.Alignment = layout.Alignments[v];
                    } else if (!cards) {
                        copy.Placement = ColumnPlacement.Below;
                        copy.BelowWidth = int.MaxValue;
                    }
                    frozen.Add(copy);
                }
                if (layout.HiddenMarker != null) yield return FrameComposer.RenderBlock(TextBlock.Plain(layout.HiddenMarker.PlainText, ThemeToken.Muted), width, context);
                string? active = null;
                bool hasGroup = false;
                TState state = default!;
                long count = 0;
                TableCell[]? previousCells = null;
                if (!RepeatHeaderPerGroup && !cards) yield return FrameComposer.RenderBlock(NewTable(frozen, true), width, context);
                while (sample.Count > 0 || input.MoveNext()) {
                    Entry entry = sample.Count > 0 ? sample.Dequeue() : Project(input.Current);
                    bool opening = !hasGroup || !string.Equals(active, entry.Group, StringComparison.Ordinal);
                    if (opening) {
                        if (hasGroup) {
                            IWidget? summary = _summary(active!, state, count);
                            if (summary != null) yield return FrameComposer.RenderBlock(summary, width, context);
                        }
                        active = entry.Group;
                        state = _seed(active);
                        count = 0;
                        hasGroup = true;
                        previousCells = null;
                        if (active.Length > 0) yield return FrameComposer.RenderBlock(new TextBlock("[bold]" + Markup.Escape(active) + "[/]") { Role = Border == TableBorder.Rule ? ThemeToken.Strong : ThemeToken.Accent }, width, context);
                    }
                    state = _aggregate(state, entry.Item);
                    count++;
                    Table body = NewTable(frozen, RepeatHeaderPerGroup && opening);
                    body.AlwaysCards = cards;
                    TableCell[] cells = new TableCell[selected.Length];
                    for (int v = 0; v < cells.Length; v++) {
                        int c = selected[v];
                        TableCell cell = entry.Cells[c];
                        TableCell? previous = previousCells == null ? null : previousCells[c];
                        bool repeats = previous != null && cell.HasValue == previous.HasValue && (cell.HasValue ? Equals(cell.Value, previous.Value) : cell.Markup == previous.Markup);
                        cells[v] = _columns[c].DimRepeats && repeats ? cell.WithTone(ThemeToken.Muted) : cell;
                    }
                    previousCells = entry.Cells;
                    body.AddRow(cells);
                    yield return FrameComposer.RenderBlock(body, width, context);
                }
                if (hasGroup) {
                    IWidget? summary = _summary(active!, state, count);
                    if (summary != null) yield return FrameComposer.RenderBlock(summary, width, context);
                }
            }
        }

        private Table NewTable(IEnumerable<TableColumn> columns, bool header) {
            Table table = new Table {
                Border = Border, RecordViewRows = 0, CardsBelow = 60, ShowHeader = header,
                HeaderSeparator = Border == TableBorder.Rule, StableWidths = false, Presentation = Presentation
            };
            foreach (TableColumn column in columns) table.AddColumn(column);
            return table;
        }

        private Entry Project(T item) {
            string group = _group(item) ?? throw new InvalidOperationException("A group key cannot be null.");
            TableCell[] cells = _row(item) ?? throw new InvalidOperationException("A row cannot be null.");
            if (cells.Length != _columns.Count) throw new InvalidOperationException("A row must contain one cell per column.");
            TableCell[] copy = (TableCell[])cells.Clone();
            for (int c = 0; c < copy.Length; c++) copy[c] = copy[c] ?? new TableCell(string.Empty);
            return new Entry(item, group, copy);
        }

        private sealed class Entry {
            internal Entry(T item, string group, TableCell[] cells) { Item = item; Group = group; Cells = cells; }
            internal T Item { get; }
            internal string Group { get; }
            internal TableCell[] Cells { get; }
        }
    }
}
