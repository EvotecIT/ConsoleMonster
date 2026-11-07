using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// The find bar over a log tail's shown lines, shared by the app <see cref="LogViewport"/> and Watch-ConsoleLog's
    /// <see cref="Live.Tails.LogTailView"/>: <c>/</c> or Ctrl+F opens the bar, typing searches, Enter and Shift+Enter move,
    /// Escape closes it and keeps the matches, and with the bar closed <c>n</c> and <c>N</c> move and Escape clears them.
    /// It also keeps the horizontal scroll, so a match off to the right is brought into view.
    /// </summary>
    internal sealed class LogFind {
        private readonly LogTail _tail;
        private int _horizontalOffset;
        private LogEntry? _revealEntry;

        internal LogFind(LogTail tail) { _tail = tail; tail.Region.TransformLine = HighlightEntry; }

        internal FindBar Bar { get; } = new FindBar();

        /// <summary>Horizontal scroll in display cells, bounded by the longest retained message.</summary>
        internal int HorizontalOffset { get => _horizontalOffset; set => _horizontalOffset = Math.Max(0, Math.Min(32768, value)); }

        /// <summary>The width the rows were last drawn at, for bringing a match into view.</summary>
        internal int ContentWidth { get; set; }

        /// <summary>The glyph tier the rows were last drawn with, for measuring match columns.</summary>
        internal GlyphTier Glyphs { get; set; }

        /// <summary>Whether the key opens the bar: <c>/</c> without Ctrl or Alt, or Ctrl+F.</summary>
        internal static bool Opens(InputEvent input) => input.Is(ConsoleKey.F, KeyModifiers.Control)
            || (input.Kind == InputEventKind.Key && input.Char == '/' && (input.Modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == 0);

        /// <summary>Whether the bar takes the key: everything it edits while open, the opening keys, and n, N and Escape while matches are kept.</summary>
        internal bool Takes(KeyChord chord) => (Bar.Open && FindBar.Takes(chord))
            || chord.Equals(new KeyChord('/')) || chord.Equals(new KeyChord(ConsoleKey.F, KeyModifiers.Control))
            || (Bar.Active && (chord.Equals(new KeyChord('n')) || chord.Equals(new KeyChord('N')) || chord.Equals(new KeyChord(ConsoleKey.Escape))));

        /// <summary>Handles a find key; returns false for keys that are not the find bar's.</summary>
        internal bool Handle(InputEvent input) {
            if (Bar.Open) {
                switch (Bar.Handle(input)) {
                    case FindKey.Edited: Bar.Search(Lines(), FirstShown()); Reveal(); return true;
                    case FindKey.Next: if (Bar.Step(1)) { Reveal(); } return true;
                    case FindKey.Previous: if (Bar.Step(-1)) { Reveal(); } return true;
                    case FindKey.Closed: Bar.Close(true); return true;
                    case FindKey.Kept: return true;
                }
                return false;
            }
            if (Opens(input)) {
                Bar.Start();
                Bar.Search(Lines(), FirstShown());
                return true;
            }
            if (Bar.Active) {
                // With the bar closed, n and N keep moving through the matches and Escape clears them.
                if (input.Kind == InputEventKind.Key && input.Char == 'n' && input.Modifiers == KeyModifiers.None) { if (Bar.Step(1)) { Reveal(); } return true; }
                if (input.Kind == InputEventKind.Key && input.Char == 'N' && (input.Modifiers & ~KeyModifiers.Shift) == KeyModifiers.None) { if (Bar.Step(-1)) { Reveal(); } return true; }
                if (input.Is(ConsoleKey.Escape)) { Bar.Close(false); return true; }
            }
            return false;
        }

        /// <summary>Applies an edit to the log and searches again, keeping the current match where it was.</summary>
        internal void Edit(Action edit) {
            LogEntry? anchor = null;
            int column = 0;
            if (Bar.Active && Bar.Current >= 0) {
                FindBar.Match current = Bar.Matches[Bar.Current];
                anchor = Shown().ElementAtOrDefault(current.Line);
                column = current.Start;
            }
            edit();
            if (!Bar.Active) { return; }
            List<string> lines = new List<string>();
            int line = -1;
            foreach (LogEntry entry in Shown()) {
                if (ReferenceEquals(entry, anchor)) { line = lines.Count; }
                lines.Add(entry.Text);
            }
            Bar.Search(lines, line >= 0 ? line : FirstShown(), line >= 0 ? column : 0);
        }

        /// <summary>Draws the log at its search offset and highlights matches in the visible cells.</summary>
        internal void Render(Region region, RenderContext context) {
            ContentWidth = region.Width;
            Glyphs = context.Glyphs.Tier;
            if (_horizontalOffset == 0) { _tail.Region.Render(region, context, _revealEntry); }
            else {
                var content = new CellBuffer(region.Width + _horizontalOffset, region.Height);
                _tail.Region.Render(new Region(content), context);
                region.Buffer.Blit(content, _horizontalOffset, 0, region.Width, region.Height, region.Bounds.X, region.Bounds.Y);
            }
            _revealEntry = null;
        }

        // Apply search styling before LogRegion wraps/truncates. UTF-16 match offsets belong to the original text,
        // while the existing renderer retains the styles across graphemes, word breaks, prefixes and continuation rows.
        private StyledLine HighlightEntry(LogEntry entry, StyledLine source, RenderContext context) {
            if (!Bar.Active) { return source; }
            int line = Shown().TakeWhile(item => !ReferenceEquals(item, entry)).Count();
            List<TextSpan> spans = new List<TextSpan>();
            int position = 0;
            foreach (TextSpan span in source) {
                int end = position + span.Text.Length;
                int copied = position;
                for (int i = 0; i < Bar.Matches.Count; i++) {
                    FindBar.Match match = Bar.Matches[i];
                    if (match.Line != line) { continue; }
                    int from = Math.Max(position, match.Start), to = Math.Min(end, match.Start + match.Length);
                    if (from >= to) { continue; }
                    if (from > copied) { spans.Add(new TextSpan(span.Text.Substring(copied - position, from - copied), span.Style)); }
                    Style highlight = i == Bar.Current ? context.Style(ThemeToken.Accent).With(TextDecoration.Bold | TextDecoration.Reverse)
                        : context.Style(ThemeToken.Highlight).With(TextDecoration.Bold | TextDecoration.Underline);
                    spans.Add(new TextSpan(span.Text.Substring(from - position, to - from), span.Style.WithColor(highlight.Color).With(highlight.Decoration)));
                    copied = to;
                }
                if (copied < end) { spans.Add(new TextSpan(span.Text.Substring(copied - position), span.Style)); }
                position = end;
            }
            return new StyledLine(spans);
        }

        private IEnumerable<LogEntry> Shown() => _tail.Region.Entries.Where(entry => _tail.Region.Shows(entry));

        // The shown (filtered) lines, oldest first, as the find bar searches them.
        private List<string> Lines() => Shown().Select(entry => entry.Text).ToList();

        // The shown line drawn on the first visible row.
        private int FirstShown() => Math.Max(0, Shown().Count() - _tail.Offset - Math.Max(1, _tail.VisibleRows));

        // Scrolls the current match into view, about a third of the way down the rows.
        private void Reveal() {
            if (Bar.Current < 0) { return; }
            FindBar.Match match = Bar.Matches[Bar.Current];
            int line = match.Line;
            LogEntry entry = Shown().ElementAt(line);
            if (_tail.Region.Wrap) {
                _horizontalOffset = 0;
                // Resolve against the layout being drawn, after the viewport reserves its paused-state row. A later
                // key replaces this request even if no frame was drawn between the two keys.
                _revealEntry = entry;
                _tail.Pause();
                return;
            }
            int from = MatchColumn(entry, match.Start), to = MatchColumn(entry, match.Start + match.Length);
            if (from < _horizontalOffset) { _horizontalOffset = from; }
            else if (ContentWidth > 0 && to >= _horizontalOffset + ContentWidth) {
                // Reserve the last cell for the log renderer's truncation marker.
                _horizontalOffset = Math.Max(0, to - ContentWidth + 1);
            }
            int rows = Math.Max(1, _tail.VisibleRows);
            int first = FirstShown();
            if (line >= first && line < first + rows) { return; }
            int shown = Shown().Count();
            int top = Math.Max(0, Math.Min(Math.Max(0, shown - rows), line - rows / 3));
            _tail.ScrollTo(shown - rows - top);
        }

        private int MatchColumn(LogEntry entry, int offset) => _tail.Region.MessageColumn(entry)
            + Text.DisplayWidth.Measure(SymbolFallback.Constrain(entry.Text.Substring(0, offset), Glyphs));
    }
}
