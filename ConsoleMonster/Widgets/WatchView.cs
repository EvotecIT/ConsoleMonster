using System;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// The view of a command run again and again (<see cref="ConsoleWatch"/>, Watch-Console): a heading with the interval,
    /// the title, the run number and the time of the last run, then the latest output. With <see cref="Differences"/>, each
    /// value (a table cell, a word or number) that changed since the previous run is shown bold on a soft surface tone, over
    /// its own colours, until a run leaves it unchanged; without colour a marker before it (↑, ↓ or *) says so.
    /// <para>Live operation: <c>update</c> (Payload = the new content widget, Value = the run number, Text = a status such
    /// as an error message, or null).</para>
    /// </summary>
    public sealed class WatchView : LiveWidget {
        private IWidget _content = new TextBlock(string.Empty);
        private string[,]? _previous;
        private string[,]? _rendered;
        private DateTimeOffset? _lastRun;

        /// <summary>Creates a watch view.</summary>
        /// <param name="name">The live name (default <c>watch</c>).</param>
        public WatchView(string name = "watch") : base(name) {
        }

        /// <summary>What is being watched, shown in the heading (markup), or null.</summary>
        public string? Title { get; set; }

        /// <summary>The time between runs, shown in the heading.</summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>Whether values that changed since the previous run are highlighted (bold on a soft surface; a marker without colour).</summary>
        public bool Differences { get; set; }

        /// <summary>The number of the latest run (1 for the first).</summary>
        public int Iteration { get; private set; }

        /// <summary>A status shown after the heading (an error of the last run), or null.</summary>
        public string? Status { get; private set; }

        /// <summary>Shows the output of a new run; the previous output becomes the base for <see cref="Differences"/>.</summary>
        /// <param name="content">The new content.</param>
        /// <param name="iteration">The run number.</param>
        /// <param name="status">A status (an error message), or null.</param>
        /// <param name="time">The time of the run, or null for now.</param>
        /// <returns>This instance.</returns>
        public WatchView Update(IWidget content, int iteration, string? status = null, DateTimeOffset? time = null) {
            _previous = _rendered;
            _content = content ?? new TextBlock(string.Empty);
            Iteration = iteration;
            Status = status;
            _lastRun = time ?? DateTimeOffset.Now;
            return this;
        }

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent != null && liveEvent.Operation == "update" && liveEvent.Payload is IWidget content) {
                Update(content, (int)liveEvent.Value, liveEvent.Text);
            }
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            Size inner = _content.Measure(new Constraints(constraints.MaxWidth, Math.Max(0, constraints.MaxHeight - 1)), context);
            return new Size(constraints.MaxWidth, Math.Min(constraints.MaxHeight, inner.Height + 1));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Width <= 0 || region.Height <= 0) {
                return;
            }
            region.Write(0, 0, TextWrapper.Truncate(Heading(context), region.Width, context.Ellipsis));
            if (region.Height <= 1) {
                return;
            }
            Region body = region.Sub(0, 1, region.Width, region.Height - 1);
            _content.Render(body, context);
            string[,] grid = new string[body.Width, body.Height];
            for (int y = 0; y < body.Height; y++) {
                for (int x = 0; x < body.Width; x++) {
                    grid[x, y] = body.Get(x, y).Glyph;
                }
            }
            if (Differences && _previous != null) {
                for (int y = 0; y < body.Height; y++) {
                    MarkRow(body, grid, _previous, y, context);
                }
            }
            _rendered = grid;
        }

        // A value is a word, a number ("1 204 311") or a marked status ("▴ Warning"): when any of its cells
        // changed since the previous run, the whole value gets the change tone (bold on a soft surface, its own colours
        // kept), and only until the next run that leaves it unchanged. Without colour, a marker in the gap before it says
        // so: ↑ or ↓ for a number that went up or down, * otherwise.
        private static void MarkRow(Region body, string[,] grid, string[,] previous, int y, RenderContext context) {
            int width = body.Width;
            if (IsRuleRow(grid, y, width)) {
                // A border row ("+-----+", "──────") is layout, not a value, also when a column width changes.
                return;
            }
            int x = 0;
            while (x < width) {
                if (IsGap(grid, x, y, width)) {
                    x++;
                    continue;
                }
                int start = x;
                while (x < width && !IsGap(grid, x, y, width)) {
                    x++;
                }
                int end = x;
                // A value that got shorter leaves changed blank cells right after it; they belong to it.
                while (end < width && grid[end, y] == " " && Changed(previous, end, y, " ") && !IsBorder(At(previous, end, y))) {
                    end++;
                }
                bool changed = false;
                for (int i = start; i < end && !changed; i++) {
                    changed = Changed(previous, i, y, grid[i, y]);
                }
                if (changed) {
                    Mark(body, grid, previous, start, x, end, y, context);
                }
                x = Math.Max(x, end);
            }
        }

        private static void Mark(Region body, string[,] grid, string[,] previous, int start, int textEnd, int end, int y, RenderContext context) {
            if (context.Profile.ColorDepth == Capabilities.ColorDepth.None) {
                // In the gap before the value, or after it when the value starts the line.
                int at = start > 0 && grid[start - 1, y] == " " ? start - 1 : textEnd < body.Width && grid[textEnd, y] == " " ? textEnd : -1;
                if (at >= 0) {
                    body.Put(at, y, Marker(grid, previous, start, textEnd, y, context), context.Style(ThemeToken.Muted));
                }
                return;
            }
            Color tone = context.Style(ThemeToken.Surface).Background;
            for (int i = start; i < end; i++) {
                Cell cell = body.Get(i, y);
                if (cell.IsContinuation) {
                    continue;
                }
                Style style = cell.Style.With(TextDecoration.Bold);
                // A cell with its own background (a badge, a status pill) keeps it and is underlined instead.
                body.SetStyle(i, y, tone.IsDefault || !cell.Style.Background.IsDefault ? style.With(TextDecoration.Underline) : style.WithBackground(tone));
            }
        }

        private static bool IsRuleRow(string[,] grid, int y, int width) {
            bool any = false;
            for (int x = 0; x < width; x++) {
                string glyph = grid[x, y] ?? " ";
                if (glyph == " " || glyph.Length == 0) {
                    continue;
                }
                if (!IsBorder(glyph) && glyph != "+" && glyph != "-" && glyph != "=") {
                    return false;
                }
                any = true;
            }
            return any;
        }

        private static string Marker(string[,] grid, string[,] previous, int start, int end, int y, RenderContext context) {
            bool ascii = context.Glyphs.Tier == Capabilities.GlyphTier.Ascii;
            // The previous value may have been wider ("40" where "4" is now): read all of it.
            int from = Math.Min(start, previous.GetLength(0));
            int to = Math.Min(end, previous.GetLength(0));
            if (y < previous.GetLength(1)) {
                while (from > 0 && !IsGap(previous, from - 1, y, previous.GetLength(0))) {
                    from--;
                }
                while (to < previous.GetLength(0) && !IsGap(previous, to, y, previous.GetLength(0))) {
                    to++;
                }
            }
            System.Globalization.NumberFormatInfo numbers = context.Culture.NumberFormat;
            if (TryNumber(Text(grid, start, end, y), numbers, out double now) && TryNumber(Text(previous, from, to, y), numbers, out double before) && now != before) {
                return now > before ? (ascii ? "^" : "↑") : (ascii ? "v" : "↓");
            }
            return "*";
        }

        private static string Text(string[,] grid, int start, int end, int y) {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = start; i < end; i++) {
                sb.Append(At(grid, i, y));
            }
            return sb.ToString().Trim();
        }

        // The number a value shows, read with the culture's separators ("1 204,5" in pl-PL, "1,204.5" in en-US; spaces and
        // a percent sign allowed). Anything else is not a number, so its change is marked with "*".
        private static bool TryNumber(string text, NumberFormatInfo numbers, out double value) {
            string group = numbers.NumberGroupSeparator;
            string separator = numbers.NumberDecimalSeparator;
            System.Text.StringBuilder digits = new System.Text.StringBuilder();
            for (int i = 0; i < text.Length; i++) {
                char c = text[i];
                if (char.IsDigit(c) || c == '-') {
                    digits.Append(c);
                } else if (string.CompareOrdinal(text, i, separator, 0, separator.Length) == 0) {
                    digits.Append('.');
                    i += separator.Length - 1;
                } else if (string.CompareOrdinal(text, i, group, 0, group.Length) == 0) {
                    i += group.Length - 1;
                } else if (c != ' ' && c != '\u00a0' && c != '\u202f' && c != '%') {
                    value = 0;
                    return false;
                }
            }
            return double.TryParse(digits.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        // Value boundaries: table borders and spaces, except a single space inside a number or after a status marker.
        private static bool IsGap(string[,] grid, int x, int y, int width) {
            string glyph = grid[x, y];
            if (IsBorder(glyph)) {
                return true;
            }
            if (glyph != " ") {
                return false;
            }
            if (x == 0 || x + 1 >= width) {
                return true;
            }
            string before = grid[x - 1, y] ?? " ";
            string after = grid[x + 1, y] ?? " ";
            // An empty glyph is the second cell of a wide character (CJK, emoji): a space after it separates words.
            if (before.Length == 0 || after.Length == 0 || before == " " || after == " " || IsBorder(before) || IsBorder(after)) {
                return true;
            }
            // One space joins digit groups ("1 204 311") and a status marker to its word ("▴ Warning", "[WARN] Warning"); between words it
            // separates values.
            bool digits = char.IsDigit(before[0]) && char.IsDigit(after[0]);
            bool marker = ((!char.IsLetterOrDigit(before[0]) && before[0] > '\u007f') || before[0] == ']') && char.IsLetter(after[0]);
            return !(digits || marker);
        }

        private static bool IsBorder(string glyph) => glyph.Length == 1 && ((glyph[0] >= '\u2500' && glyph[0] <= '\u257f') || glyph[0] == '|');

        private static string At(string[,] grid, int x, int y) => x < grid.GetLength(0) && y < grid.GetLength(1) ? grid[x, y] ?? " " : " ";

        private static bool Changed(string[,] previous, int x, int y, string glyph) {
            string before = x < previous.GetLength(0) && y < previous.GetLength(1) ? previous[x, y] ?? " " : " ";
            return !string.Equals(before, glyph, StringComparison.Ordinal);
        }

        // "Every 2s: Get-Process   #3 · 14:05:33" and the status of the last run.
        private StyledLine Heading(RenderContext context) {
            Style muted = context.Style(ThemeToken.Muted);
            System.Collections.Generic.List<TextSpan> spans = new System.Collections.Generic.List<TextSpan> {
                new TextSpan(Localization.WidgetLabels.Format(context.Labels.WatchEvery, NumberFormat.Duration(Interval)), muted)
            };
            if (!string.IsNullOrEmpty(Title)) {
                spans.Add(new TextSpan(" "));
                spans.AddRange(context.Markup(Title!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Text)));
            }
            string separator = " " + Interactive.InteractiveGlyphs.For(context).Separator + " ";
            if (Iteration > 0) {
                spans.Add(new TextSpan("   #" + Iteration.ToString(CultureInfo.InvariantCulture), muted));
            }
            if (_lastRun.HasValue) {
                spans.Add(new TextSpan(separator + _lastRun.Value.ToLocalTime().ToString("HH:mm:ss", CultureInfo.InvariantCulture), muted));
            }
            if (!string.IsNullOrEmpty(Status)) {
                spans.Add(new TextSpan(separator + Status, context.Style(StatusKind.Fail)));
            }
            return new StyledLine(spans);
        }
    }
}
