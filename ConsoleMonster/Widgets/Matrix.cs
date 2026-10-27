using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>A labelled status grid with contiguous axis bands, a counted legend and explicit narrow fallbacks.</summary>
    public sealed class Matrix : IWidget {
        private readonly List<MatrixLabel> _rows;
        private readonly List<MatrixLabel> _columns;
        private readonly Dictionary<(int Row, int Column), MatrixCell> _cells = new Dictionary<(int, int), MatrixCell>();

        /// <summary>Creates a matrix with fixed axes; labels and cells contain plain text.</summary>
        /// <param name="rows">The row labels.</param>
        /// <param name="columns">The column labels.</param>
        public Matrix(IEnumerable<MatrixLabel> rows, IEnumerable<MatrixLabel> columns) {
            _rows = Labels(rows, nameof(rows));
            _columns = Labels(columns, nameof(columns));
        }

        /// <summary>The row labels in input order.</summary>
        public IReadOnlyList<MatrixLabel> Rows => _rows;

        /// <summary>The column labels in input order.</summary>
        public IReadOnlyList<MatrixLabel> Columns => _columns;

        /// <summary>An optional markup title.</summary>
        public string? Title { get; set; }

        /// <summary>Show counts of supplied statuses, self cells and absent observations (default true).</summary>
        public bool ShowLegend { get; set; } = true;

        /// <summary>Always render the non-healthy list instead of the grid.</summary>
        public bool NonHealthyOnly { get; set; }

        /// <summary>The largest axis allowed in grid mode (default 16). Larger axes use the attention list.</summary>
        public int MaxGridSize { get; set; } = 16;

        /// <summary>The most attention entries shown (default 10; zero shows all), followed by an omission count.</summary>
        public int MaxListRows { get; set; } = 10;

        /// <summary>Classifies supplied statuses for the attention list; null uses Pass, Skip and absent as healthy.</summary>
        public Func<StatusKind?, bool>? IsHealthy { get; set; }

        /// <summary>Sets an observation without deriving health or assuming the axes describe the same entities.</summary>
        /// <param name="row">Zero-based row index.</param>
        /// <param name="column">Zero-based column index.</param>
        /// <param name="status">The supplied status, or null.</param>
        /// <param name="text">Optional short plain text.</param>
        /// <param name="diagonal">True for an explicit self marker.</param>
        /// <returns>This matrix.</returns>
        public Matrix SetCell(int row, int column, StatusKind? status, string? text = null, bool diagonal = false) {
            CheckIndex(row, column);
            _cells[(row, column)] = new MatrixCell(status, text, diagonal);
            return this;
        }

        /// <summary>Gets an observation, or null when none has been supplied.</summary>
        /// <param name="row">Zero-based row index.</param>
        /// <param name="column">Zero-based column index.</param>
        /// <returns>The observation.</returns>
        public MatrixCell? GetCell(int row, int column) {
            CheckIndex(row, column);
            return _cells.TryGetValue((row, column), out MatrixCell? cell) ? cell : null;
        }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            return WidgetLines.Measure(Lines(constraints.MaxWidth, context), constraints);
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => WidgetLines.Render(Lines(region.Width, context), region);

        private List<StyledLine> Lines(int width, RenderContext context) {
            List<StyledLine> lines = new List<StyledLine>();
            if (!string.IsNullOrEmpty(Title)) WidgetLines.Wrap(lines, context.Markup(Title!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Accent).With(TextDecoration.Bold)), width);
            int cellWidth = 1;
            foreach (MatrixCell cell in _cells.Values) cellWidth = Math.Max(cellWidth, CellLine(cell, context).Width);
            bool numbered = width < 80;
            int labelWidth = RowWidth(numbered, width);
            int[] widths = ColumnWidths(cellWidth, width, numbered);
            if (!numbered && RequiredWidth(labelWidth, widths) > width) {
                numbered = true;
                labelWidth = RowWidth(true, width);
                widths = ColumnWidths(cellWidth, width, true);
            }
            bool list = NonHealthyOnly || width < 60 || _rows.Count > MaxGridSize || _columns.Count > MaxGridSize || RequiredWidth(labelWidth, widths) > width;
            if (list) Attention(lines, width, context);
            else Grid(lines, width, labelWidth, widths, numbered, context);
            if (ShowLegend) Legend(lines, width, context);
            return lines;
        }

        private void Grid(List<StyledLine> lines, int width, int labelWidth, int[] widths, bool numbered, RenderContext context) {
            bool bands = _columns.Exists(label => !string.IsNullOrEmpty(label.Band));
            if (bands) {
                List<TextSpan> spans = new List<TextSpan> { new TextSpan(new string(' ', labelWidth + 2)) };
                for (int c = 0; c < _columns.Count;) {
                    int end = c + 1;
                    while (end < _columns.Count && _columns[end].Band == _columns[c].Band) end++;
                    int spanWidth = 0;
                    for (int i = c; i < end; i++) spanWidth += widths[i] + 2;
                    AppendPadded(spans, StyledLine.From(_columns[c].Band ?? string.Empty, context.Style(ThemeToken.Muted)), spanWidth, context);
                    c = end;
                }
                lines.Add(new StyledLine(spans));
            }
            List<TextSpan> header = new List<TextSpan>();
            AppendPadded(header, StyledLine.Empty, labelWidth + 2, context);
            for (int c = 0; c < _columns.Count; c++) AppendPadded(header, StyledLine.From(AxisText(_columns[c], c, numbered, width, false), context.Style(ThemeToken.Header)), widths[c] + 2, context);
            lines.Add(new StyledLine(header));
            string? previousBand = null;
            for (int r = 0; r < _rows.Count; r++) {
                string? band = _rows[r].Band;
                if (!string.IsNullOrEmpty(band) && band != previousBand) WidgetLines.Wrap(lines, StyledLine.From(band!, context.Style(ThemeToken.Muted)), width);
                previousBand = band;
                List<TextSpan> row = new List<TextSpan>();
                AppendPadded(row, StyledLine.From(AxisText(_rows[r], r, numbered, width, true), context.Style(ThemeToken.Text)), labelWidth + 2, context);
                for (int c = 0; c < _columns.Count; c++) AppendPadded(row, CellLine(GetCell(r, c), context), widths[c] + 2, context);
                lines.Add(new StyledLine(row));
            }
            if (numbered) {
                List<string> labels = new List<string>();
                for (int c = 0; c < _columns.Count; c++) labels.Add((c + 1).ToString(CultureInfo.InvariantCulture) + " " + _columns[c].Text);
                WidgetLines.Wrap(lines, StyledLine.From(string.Join(context.Glyphs.Tier == GlyphTier.Ascii ? " | " : " · ", labels), context.Style(ThemeToken.Muted)), width);
            }
        }

        private void Attention(List<StyledLine> lines, int width, RenderContext context) {
            List<KeyValuePair<(int Row, int Column), MatrixCell>> attention = new List<KeyValuePair<(int, int), MatrixCell>>();
            foreach (KeyValuePair<(int Row, int Column), MatrixCell> entry in _cells) {
                MatrixCell cell = entry.Value;
                bool healthy = IsHealthy?.Invoke(cell.Status) ?? (!cell.Status.HasValue || cell.Status == StatusKind.Pass || cell.Status == StatusKind.Skip);
                if (!cell.Diagonal && !healthy) attention.Add(entry);
            }
            attention.Sort((a, b) => {
                int rank = Rank(a.Value.Status).CompareTo(Rank(b.Value.Status));
                if (rank != 0) return rank;
                int row = a.Key.Row.CompareTo(b.Key.Row);
                return row != 0 ? row : a.Key.Column.CompareTo(b.Key.Column);
            });
            int shown = MaxListRows <= 0 ? attention.Count : Math.Min(MaxListRows, attention.Count);
            for (int i = 0; i < shown; i++) {
                var entry = attention[i];
                List<TextSpan> spans = new List<TextSpan> { new TextSpan(_rows[entry.Key.Row].Text + " / " + _columns[entry.Key.Column].Text + "  ", context.Style(ThemeToken.Text)) };
                spans.AddRange(CellLine(entry.Value, context));
                WidgetLines.Wrap(lines, new StyledLine(spans), width);
            }
            if (shown == 0) WidgetLines.Wrap(lines, StyledLine.From("0 non-healthy cells", context.Style(ThemeToken.Muted)), width);
            if (shown < attention.Count) WidgetLines.Wrap(lines, StyledLine.From("+" + (attention.Count - shown).ToString(CultureInfo.InvariantCulture) + " more", context.Style(ThemeToken.Muted)), width);
        }

        private void Legend(List<StyledLine> lines, int width, RenderContext context) {
            Dictionary<StatusKind, int> counts = new Dictionary<StatusKind, int>();
            int self = 0, observed = 0;
            foreach (MatrixCell cell in _cells.Values) {
                if (cell.Diagonal) self++;
                else if (cell.Status.HasValue) {
                    observed++;
                    counts[cell.Status.Value] = counts.TryGetValue(cell.Status.Value, out int n) ? n + 1 : 1;
                }
            }
            List<TextSpan> spans = new List<TextSpan>();
            foreach (StatusKind status in new[] { StatusKind.Pending, StatusKind.Running, StatusKind.Pass, StatusKind.Fail, StatusKind.Warn, StatusKind.Skip, StatusKind.Info }) {
                if (!counts.TryGetValue(status, out int count)) continue;
                spans.Add(new TextSpan(context.Glyphs.StatusSymbol(status) + " " + status.ToWord() + " " + count.ToString(CultureInfo.InvariantCulture) + "  ", context.Style(status)));
            }
            long absent = (long)_rows.Count * _columns.Count - observed - self;
            spans.Add(new TextSpan(Self(context) + " self " + self.ToString(CultureInfo.InvariantCulture) + "  " + None(context) + " none " + absent.ToString(CultureInfo.InvariantCulture), context.Style(ThemeToken.Muted)));
            WidgetLines.Wrap(lines, new StyledLine(spans), width);
        }

        private int RowWidth(bool numbered, int width) {
            int result = 0;
            for (int r = 0; r < _rows.Count; r++) result = Math.Max(result, DisplayWidth.Measure(AxisText(_rows[r], r, numbered, width, true)));
            return Math.Min(result, Math.Max(8, width / 3));
        }

        private int[] ColumnWidths(int cellWidth, int width, bool numbered) {
            int[] result = new int[_columns.Count];
            for (int c = 0; c < result.Length; c++) result[c] = Math.Max(cellWidth, DisplayWidth.Measure(AxisText(_columns[c], c, numbered, width, false)));
            return result;
        }

        private static string AxisText(MatrixLabel label, int index, bool numbered, int width, bool row) => numbered
            ? (index + 1).ToString(CultureInfo.InvariantCulture) + (row ? " " + (label.ShortText ?? label.Text) : string.Empty)
            : width < 120 ? label.ShortText ?? label.Text : label.Text;

        private static int RequiredWidth(int label, int[] widths) {
            int result = label + 2;
            foreach (int width in widths) result += width + 2;
            return result;
        }

        private static StyledLine CellLine(MatrixCell? cell, RenderContext context) {
            if (cell?.Diagonal == true) return StyledLine.From(Self(context), context.Style(ThemeToken.Muted));
            string marker = cell?.Status is StatusKind status ? context.Glyphs.StatusSymbol(status) : None(context);
            return StyledLine.From(marker + (cell?.Text ?? string.Empty), cell?.Status is StatusKind state ? context.Style(state) : context.Style(ThemeToken.Muted));
        }

        private static string Self(RenderContext context) => context.Glyphs.Tier == GlyphTier.Ascii ? "-" : "—";
        private static string None(RenderContext context) => context.Glyphs.Tier == GlyphTier.Ascii ? "." : "·";
        private static int Rank(StatusKind? status) => status == StatusKind.Fail ? 0 : status == StatusKind.Warn ? 1 : 2;

        private static void AppendPadded(List<TextSpan> spans, StyledLine line, int width, RenderContext context) {
            StyledLine fitted = TextWrapper.Truncate(line, Math.Max(0, width - 2), context.Ellipsis);
            spans.AddRange(fitted);
            spans.Add(new TextSpan(new string(' ', Math.Max(0, width - fitted.Width))));
        }

        private void CheckIndex(int row, int column) {
            if (row < 0 || row >= _rows.Count) throw new ArgumentOutOfRangeException(nameof(row));
            if (column < 0 || column >= _columns.Count) throw new ArgumentOutOfRangeException(nameof(column));
        }

        private static List<MatrixLabel> Labels(IEnumerable<MatrixLabel> labels, string parameter) {
            if (labels == null) throw new ArgumentNullException(parameter);
            List<MatrixLabel> result = new List<MatrixLabel>(labels);
            if (result.Exists(label => label == null)) throw new ArgumentException("Axis labels cannot be null.", parameter);
            return result;
        }
    }
}
