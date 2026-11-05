using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>What a key did to an open find bar.</summary>
    internal enum FindKey { Unhandled, Kept, Edited, Next, Previous, Closed }

    /// <summary>
    /// The find bar shared by LogViewport, TextArea and the pager: a one-line query editor, the matches over the owner's
    /// lines (smart case: a query with a capital letter matches case), the current match and the highlighting of every match
    /// in the drawn rows. The owner says which lines it has, reveals a match and draws the bar where it fits.
    /// </summary>
    internal sealed class FindBar {
        private readonly LineEditor _editor = new LineEditor();
        private readonly List<Match> _matches = new List<Match>();

        /// <summary>Whether the bar is open and takes typing.</summary>
        internal bool Open { get; private set; }

        /// <summary>Whether matches are highlighted (the bar is open, or closed with a query kept for n and N).</summary>
        internal bool Active { get; private set; }

        internal string Query => _editor.Text;

        internal IReadOnlyList<Match> Matches => _matches;

        /// <summary>The current match index, or -1.</summary>
        internal int Current { get; private set; } = -1;

        internal void Start() { Open = true; Active = true; }

        /// <summary>Closes the bar; <paramref name="keep"/> keeps the matches highlighted for n and N.</summary>
        internal void Close(bool keep) {
            Open = false;
            Active = keep && _editor.Text.Length > 0;
            if (!Active) { _matches.Clear(); Current = -1; }
        }

        /// <summary>Keys while the bar is open: typing edits the query, Enter and Shift+Enter move, Escape closes.</summary>
        internal FindKey Handle(InputEvent input) {
            if (input.Is(ConsoleKey.Escape)) { return FindKey.Closed; }
            if (input.Is(ConsoleKey.Enter, KeyModifiers.Shift)) { return FindKey.Previous; }
            if (input.Is(ConsoleKey.Enter)) { return FindKey.Next; }
            if (input.Kind != InputEventKind.Paste && input.Kind != InputEventKind.Key) { return FindKey.Unhandled; }
            string before = _editor.Text;
            if (!_editor.Handle(input)) { return FindKey.Unhandled; }
            return _editor.Text == before ? FindKey.Kept : FindKey.Edited;
        }

        /// <summary>Whether an open bar takes the key itself (for the footer).</summary>
        internal static bool Takes(KeyChord chord) => chord.Key == ConsoleKey.Enter || chord.Key == ConsoleKey.Escape || LineEditor.Takes(chord);

        /// <summary>The keys an open bar lists in a footer.</summary>
        internal static void AddKeys(List<KeyBinding> keys, PromptLabels labels) {
            keys.Add(new KeyBinding(labels.KeyNext, () => { }, new KeyChord(ConsoleKey.Enter)) { Order = 1 });
            keys.Add(new KeyBinding(labels.KeyPrevious, () => { }, new KeyChord(ConsoleKey.Enter, KeyModifiers.Shift)) { Order = 2 });
            keys.Add(new KeyBinding(labels.KeyClose, () => { }, new KeyChord(ConsoleKey.Escape)) { Order = 3 });
        }

        /// <summary>Finds every match in the lines; the current match becomes the first one at or after <paramref name="anchor"/>.</summary>
        internal void Search(IReadOnlyList<string> lines, int anchor, int column = 0) {
            _matches.Clear();
            Current = -1;
            string query = _editor.Text;
            if (query.Length == 0) { return; }
            StringComparison comparison = Comparison(query);
            for (int line = 0; line < lines.Count; line++) {
                string text = lines[line] ?? string.Empty;
                for (int at = text.IndexOf(query, comparison); at >= 0; at = text.IndexOf(query, at + query.Length, comparison)) {
                    _matches.Add(new Match(line, at, query.Length));
                    if (Current < 0 && (line > anchor || (line == anchor && at >= column))) { Current = _matches.Count - 1; }
                }
            }
            if (Current < 0 && _matches.Count > 0) { Current = 0; }
        }

        /// <summary>Moves to the next (1) or previous (-1) match, wrapping; false when there are none.</summary>
        internal bool Step(int delta) {
            if (_matches.Count == 0) { return false; }
            Current = ((Current < 0 ? (delta > 0 ? -1 : 0) : Current) + delta + _matches.Count) % _matches.Count;
            return true;
        }

        /// <summary>The ordinal of the current match among the matches on its line, or -1 when it is not on that line.</summary>
        internal int CurrentOrdinal(int line) {
            if (Current < 0 || _matches[Current].Line != line) { return -1; }
            int ordinal = 0;
            for (int i = Current - 1; i >= 0 && _matches[i].Line == line; i--) { ordinal++; }
            return ordinal;
        }

        /// <summary>Draws the bar on one row: the caption, the query with its caret and the match position.</summary>
        internal void Draw(Region row, RenderContext context, PromptLabels labels) {
            if (row.Width <= 0 || row.Height <= 0) { return; }
            row.Fill(context.Style(ThemeToken.Text));
            string status = _editor.Text.Length == 0 ? string.Empty
                : _matches.Count == 0 ? labels.NoMatches
                : PromptLabels.Format(labels.FindMatches, (Current + 1).ToString(CultureInfo.CurrentCulture), _matches.Count.ToString(CultureInfo.CurrentCulture));
            int statusWidth = DisplayWidth.Measure(status);
            int x = row.Write(0, 0, labels.Find + ": ", context.Style(Open ? ThemeToken.Focus : ThemeToken.Muted).With(TextDecoration.Bold));
            bool showStatus = status.Length > 0 && statusWidth < row.Width - x - 2;
            int room = Math.Max(0, row.Width - x - (showStatus ? statusWidth + 2 : 0));
            if (room > 0) {
                string text = _editor.Text;
                string query = Glyphs.SymbolFallback.Constrain(text, context.Glyphs.Tier);
                int caret = DisplayWidth.Measure(Glyphs.SymbolFallback.Constrain(text.Substring(0, Math.Min(text.Length, _editor.Caret)), context.Glyphs.Tier));
                int offset = _editor.ScrollOffset;
                if (caret - offset >= room) { offset = caret - room + 1; }
                if (caret < offset) { offset = Math.Max(0, caret - room / 3); }
                if (DisplayWidth.Measure(query) < room) { offset = 0; }
                _editor.ScrollOffset = offset;
                int column = 0;
                foreach (TextCell cell in DisplayWidth.Cells(query)) {
                    int at = column - offset;
                    if (at >= 0 && at + cell.Width <= room) { row.Put(x + at, 0, cell.Glyph, context.Style(ThemeToken.Text), cell.Width); }
                    column += cell.Width;
                    if (column - offset >= room) { break; }
                }
                if (Open) {
                    int cursor = x + Math.Min(room - 1, caret - offset);
                    Cell cell = row.Get(cursor, 0);
                    if (context.HasColor) { row.Put(cursor, 0, cell.Glyph, cell.Style.With(TextDecoration.Reverse), Math.Max(1, (int)cell.Width)); }
                    else { row.Write(cursor, 0, "_", context.Style(ThemeToken.Text)); }
                }
            }
            if (showStatus) {
                row.Write(row.Width - statusWidth, 0, status, context.Style(_matches.Count == 0 ? ThemeToken.Warn : ThemeToken.Muted));
            }
        }

        /// <summary>A one-row widget drawing this bar, for the overlay layer.</summary>
        internal IWidget Widget(PromptLabels labels) => new BarWidget(this, labels);

        /// <summary>
        /// Highlights every occurrence of the query in one drawn row, starting at <paramref name="x"/>; the occurrence at
        /// <paramref name="current"/> (counting from zero along the row) is marked as the current match.
        /// </summary>
        internal void Highlight(Region region, int y, int x, int current, RenderContext context) {
            string query = _editor.Text;
            if (!Active || query.Length == 0 || y < 0 || y >= region.Height) { return; }
            StringBuilder text = new StringBuilder();
            List<int> starts = new List<int>();
            List<int> columns = new List<int>();
            for (int column = Math.Max(0, x); column < region.Width; column++) {
                Cell cell = region.Get(column, y);
                if (cell.IsContinuation) { continue; }
                starts.Add(text.Length);
                columns.Add(column);
                text.Append(cell.Glyph.Length == 0 ? " " : cell.Glyph);
            }
            string row = text.ToString();
            StringComparison comparison = Comparison(query);
            Style all = context.Style(ThemeToken.Highlight).With(TextDecoration.Bold | TextDecoration.Underline);
            Style mark = context.Style(ThemeToken.Accent).With(TextDecoration.Bold | TextDecoration.Reverse);
            int ordinal = 0;
            for (int at = row.IndexOf(query, comparison); at >= 0; at = row.IndexOf(query, at + query.Length, comparison), ordinal++) {
                for (int i = 0; i < starts.Count; i++) {
                    if (starts[i] < at || starts[i] >= at + query.Length) { continue; }
                    Cell cell = region.Get(columns[i], y);
                    Style style = ordinal == current ? mark : all;
                    region.Put(columns[i], y, cell.Glyph, cell.Style.WithColor(style.Color).With(style.Decoration), Math.Max(1, (int)cell.Width));
                }
            }
        }

        /// <summary>Whether the text holds the query, by the bar's smart-case rule.</summary>
        internal static bool Contains(string text, string query) => query.Length > 0 && (text ?? string.Empty).IndexOf(query, Comparison(query)) >= 0;

        private static StringComparison Comparison(string query) {
            foreach (char c in query) { if (char.IsUpper(c)) { return StringComparison.Ordinal; } }
            return StringComparison.OrdinalIgnoreCase;
        }

        // Owners with soft-wrapped source text map each match fragment into display cells themselves.
        internal void HighlightCells(Region region, int y, int from, int to, bool current, RenderContext context) {
            Style style = current ? context.Style(ThemeToken.Accent).With(TextDecoration.Bold | TextDecoration.Reverse)
                : context.Style(ThemeToken.Highlight).With(TextDecoration.Bold | TextDecoration.Underline);
            for (int x = Math.Max(0, from); x < Math.Min(to, region.Width); x++) {
                Cell cell = region.Get(x, y);
                if (!cell.IsContinuation) { region.Put(x, y, cell.Glyph, cell.Style.WithColor(style.Color).With(style.Decoration), Math.Max(1, (int)cell.Width)); }
            }
        }

        internal readonly struct Match {
            internal Match(int line, int start, int length) { Line = line; Start = start; Length = length; }
            internal int Line { get; }
            internal int Start { get; }
            internal int Length { get; }
        }

        private sealed class BarWidget : IWidget {
            private readonly FindBar _bar;
            private readonly PromptLabels _labels;
            internal BarWidget(FindBar bar, PromptLabels labels) { _bar = bar; _labels = labels; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, Math.Min(1, constraints.MaxHeight));
            public void Render(Region region, RenderContext context) => _bar.Draw(region, context, _labels);
        }
    }
}
