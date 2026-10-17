using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Highlighting;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Source text with syntax highlighting: an optional title (a file name) on a panel, line numbers, highlighted line
    /// ranges marked in the gutter, long lines wrapped or truncated with the tier's ellipsis, and a line cap with
    /// "+n more lines". The text is literal: markup is not interpreted and control characters are dropped.
    /// </summary>
    public sealed class CodeView : IWidget {
        private readonly List<int[]> _ranges = new List<int[]>();
        private List<StyledLine>? _cache;
        private RenderContext? _cacheContext;

        /// <summary>Creates a view.</summary>
        /// <param name="text">The text.</param>
        /// <param name="highlighter">The highlighter, or null for plain text.</param>
        public CodeView(string text, ITextHighlighter? highlighter = null) {
            Text = text ?? string.Empty;
            Highlighter = highlighter;
        }

        /// <summary>
        /// Creates a view for a language name or file extension ("json", "ps1"): "auto" (or null) detects the language from
        /// the path and the content, "text" or "plain" shows plain text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="language">The language, "auto", "text", or null for auto.</param>
        /// <param name="path">The file it came from (for detection), or null.</param>
        /// <returns>The view.</returns>
        /// <exception cref="ArgumentException">The language is not registered.</exception>
        public static CodeView For(string text, string? language, string? path = null) {
            if (string.IsNullOrEmpty(language) || string.Equals(language, "auto", StringComparison.OrdinalIgnoreCase)) {
                return new CodeView(text, Highlighters.Detect(text, path));
            }
            if (string.Equals(language, "text", StringComparison.OrdinalIgnoreCase) || string.Equals(language, "plain", StringComparison.OrdinalIgnoreCase)) {
                return new CodeView(text, null);
            }
            ITextHighlighter highlighter = Highlighters.Get(language) ?? throw new ArgumentException("Unknown language '" + language + "'. Use Auto, Text, Smart or one of: " + string.Join(", ", Highlighters.Names) + ".", nameof(language));
            return new CodeView(text, highlighter);
        }
        /// <summary>The text.</summary>
        public string Text { get; }

        /// <summary>The highlighter, or null.</summary>
        public ITextHighlighter? Highlighter { get; }

        /// <summary>A title (a file name) drawn on a panel around the code, or null for no panel.</summary>
        public string? Title { get; set; }

        /// <summary>Shows line numbers.</summary>
        public bool LineNumbers { get; set; }

        /// <summary>The number of the first line (default 1), for excerpts.</summary>
        public int FirstLine { get; set; } = 1;

        /// <summary>Wraps long lines instead of truncating them.</summary>
        public bool Wrap { get; set; }

        /// <summary>The most lines shown (default 2000); the rest are counted.</summary>
        public int MaxLines { get; set; } = 2000;

        /// <summary>Marks lines (by their numbers) as highlighted.</summary>
        /// <param name="from">The first line.</param>
        /// <param name="to">The last line (default the same).</param>
        /// <returns>This view.</returns>
        public CodeView HighlightLines(int from, int to = 0) {
            _ranges.Add(new[] { from, Math.Max(from, to) });
            return this;
        }

        /// <summary>Marks lines given as text: a number ("10") or a range ("10-14", "10..14").</summary>
        /// <param name="range">The line or range.</param>
        /// <returns>This view.</returns>
        /// <exception cref="ArgumentException">The text is not a line number or a range.</exception>
        public CodeView HighlightLines(string range) {
            string[] parts = (range ?? string.Empty).Split(new[] { '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || parts.Length > 2 || !int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int from)) {
                throw new ArgumentException("'" + range + "' is not a line number or a range such as 10-14.", nameof(range));
            }
            int to = from;
            if (parts.Length == 2 && !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out to)) {
                throw new ArgumentException("'" + range + "' is not a line number or a range such as 10-14.", nameof(range));
            }
            return HighlightLines(from, to);
        }
        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            if (Title != null) {
                return Framed().Measure(constraints, context);
            }
            int rows = Rows(constraints.MaxWidth, context).Count;
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? rows : Math.Min(rows, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            if (Title != null) {
                Framed().Render(region, context);
                return;
            }
            List<StyledLine> rows = Rows(region.Width, context);
            for (int y = 0; y < rows.Count && y < region.Height; y++) {
                region.Write(0, y, rows[y]);
            }
        }

        /// <summary>The display rows at a width: gutter and code, wrapped or truncated.</summary>
        /// <param name="width">The width.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The rows.</returns>
        public List<StyledLine> Rows(int width, RenderContext context) {
            List<StyledLine> lines = Lines(context);
            int shown = lines.Count;
            int last = FirstLine + shown - 1;
            int digits = LineNumbers ? last.ToString(CultureInfo.InvariantCulture).Length : 0;
            string marker = context.Glyphs.Tier == Capabilities.GlyphTier.Ascii ? ">" : "▌";
            string separator = context.Glyphs.Get(Glyph.Separator);
            int gutter = LineNumbers ? 1 + digits + 3 : (_ranges.Count > 0 ? 2 : 0);
            int codeWidth = Math.Max(4, width - gutter);
            Style muted = context.Style(ThemeToken.Muted);
            Style accent = context.Style(ThemeToken.Accent).With(TextDecoration.Bold);
            List<StyledLine> rows = new List<StyledLine>();
            for (int i = 0; i < shown; i++) {
                int number = FirstLine + i;
                bool marked = IsMarked(number);
                IReadOnlyList<StyledLine> parts = Wrap ? TextWrapper.Wrap(lines[i], codeWidth) : new[] { TextWrapper.Truncate(lines[i], codeWidth, context.Ellipsis) };
                if (parts.Count == 0) {
                    parts = new[] { StyledLine.Empty };
                }
                for (int p = 0; p < parts.Count; p++) {
                    List<TextSpan> spans = new List<TextSpan>();
                    if (gutter > 0) {
                        spans.Add(new TextSpan(marked && p == 0 ? marker : " ", accent));
                        if (LineNumbers) {
                            string label = p == 0 ? number.ToString(CultureInfo.InvariantCulture).PadLeft(digits) : new string(' ', digits);
                            spans.Add(new TextSpan(label, marked ? accent : muted));
                            spans.Add(new TextSpan(" " + separator + " ", muted));
                        } else {
                            spans.Add(new TextSpan(" "));
                        }
                    }
                    spans.AddRange(parts[p]);
                    rows.Add(new StyledLine(spans));
                }
            }
            int total = TotalLines();
            if (total > shown) {
                int more = total - shown;
                rows.Add(StyledLine.From(new string(' ', gutter) + "+ " + more.ToString(CultureInfo.InvariantCulture) + (more == 1 ? " more line" : " more lines"), muted));
            }
            return rows;
        }

        private List<StyledLine> Lines(RenderContext context) {
            if (_cache == null || !ReferenceEquals(_cacheContext?.Theme, context.Theme) || _cacheContext!.Depth != context.Depth || _cacheContext.Glyphs.Tier != context.Glyphs.Tier) {
                // Only what can be shown is tokenized, so a huge file costs no more than the line cap.
                int cut = Cut(Text, Math.Max(1, MaxLines));
                _cache = HighlightStyles.ToLines(cut < Text.Length ? Text.Substring(0, cut) : Text, Highlighter, context, Math.Max(1, MaxLines));
                _cacheContext = context;
            }
            return _cache;
        }

        private int TotalLines() {
            int lines = 1;
            for (int i = 0; i < Text.Length; i++) {
                if (Text[i] == '\n' && i < Text.Length - 1) {
                    lines++;
                }
            }
            return lines;
        }
        // The index after the first lineCount lines.
        private static int Cut(string text, int lineCount) {
            int lines = 0;
            for (int i = 0; i < text.Length; i++) {
                if (text[i] == '\n' && ++lines >= lineCount) {
                    return i + 1;
                }
            }
            return text.Length;
        }

        private bool IsMarked(int number) {
            foreach (int[] range in _ranges) {
                if (number >= range[0] && number <= range[1]) {
                    return true;
                }
            }
            return false;
        }

        private Panel Framed() => new Panel(new CodeView(Text, Highlighter) { LineNumbers = LineNumbers, FirstLine = FirstLine, Wrap = Wrap, MaxLines = MaxLines }.CopyRanges(_ranges), Markup.Escape(Title!)) { PaddingTop = 0 };

        private CodeView CopyRanges(List<int[]> ranges) {
            _ranges.AddRange(ranges);
            return this;
        }
    }
}
