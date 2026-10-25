using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Text {
    /// <summary>
    /// Multicolour text: a sequence of spans, each literal text (never interpreted as markup) or markup, with a
    /// <see cref="RichStyle"/>. Rich text is accepted wherever text appears (table cells and headers, card and record
    /// fields, tree labels, titles, lane names, log lines, chart labels) and is itself a widget, so it can be written or
    /// placed in a layout directly. Colours and theme roles are resolved per colour tier when rendered; without colour the
    /// text stays readable.
    /// </summary>
    /// <remarks>
    /// Literal spans are safe for data: brackets and control characters are never interpreted. Build rich text with
    /// <see cref="RichTextBuilder"/>, <see cref="FromMarkup"/>, <see cref="FromPieces(IReadOnlyList{string}, IReadOnlyList{RichColor}, IReadOnlyList{RichColor}, RichStyle)"/> (Write-Color style parallel
    /// arrays) or <see cref="Gradient"/>. <see cref="ToString"/> returns the plain text.
    /// </remarks>
    public sealed class RichText : IWidget {
        private readonly RichSegment[] _segments;
        private string? _markup;

        /// <summary>Creates rich text from segments.</summary>
        /// <param name="segments">The segments, in order.</param>
        public RichText(IEnumerable<RichSegment> segments) {
            _segments = (segments ?? throw new ArgumentNullException(nameof(segments))).ToArray();
        }

        /// <summary>Empty rich text.</summary>
        public static RichText Empty { get; } = new RichText(Array.Empty<RichSegment>());

        /// <summary>The segments, in order.</summary>
        public IReadOnlyList<RichSegment> Segments => _segments;

        /// <summary>Alignment when written as a widget (default left).</summary>
        public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

        /// <summary>How the text fits when written as a widget (default wrap, keeping styles across lines).</summary>
        public TextOverflow Overflow { get; set; } = TextOverflow.Wrap;

        /// <summary>The text without styling (markup spans stripped).</summary>
        public string PlainText {
            get {
                StringBuilder sb = new StringBuilder();
                foreach (RichSegment segment in _segments) {
                    sb.Append(segment.IsMarkup ? Markup.Strip(segment.Text) : segment.Text);
                }
                return sb.ToString();
            }
        }

        /// <summary>The display width of the widest line, in terminal cells.</summary>
        public int Width => PlainText.Replace("\r\n", "\n").Split('\n').Select(line => DisplayWidth.Measure(line)).DefaultIfEmpty(0).Max();

        /// <summary>Literal text with one style.</summary>
        /// <param name="text">The text (shown as written).</param>
        /// <param name="style">The style.</param>
        /// <returns>The rich text.</returns>
        public static RichText Plain(string? text, RichStyle style = default) => new RichText(new[] { new RichSegment(text ?? string.Empty, style, false) });

        /// <summary>Rich text from Spectre-compatible markup (<c>[bold lime]Ready[/] in [muted]3s[/]</c>).</summary>
        /// <param name="markup">The markup.</param>
        /// <param name="style">A style around the whole markup.</param>
        /// <returns>The rich text.</returns>
        /// <exception cref="ArgumentException">The markup is not valid.</exception>
        public static RichText FromMarkup(string? markup, RichStyle style = default) {
            string text = markup ?? string.Empty;
            if (!Markup.Validate(text, out string? error)) {
                throw new ArgumentException("Invalid markup: " + error, nameof(markup));
            }
            return new RichText(new[] { new RichSegment(text, style, true) });
        }

        /// <summary>
        /// Rich text from parallel arrays, as Write-Color takes them: piece i gets colour i and background i; pieces beyond
        /// the colours get the first colour (and the first background), as Write-Color does.
        /// </summary>
        /// <param name="texts">The pieces (literal).</param>
        /// <param name="colors">Foreground colours, or null.</param>
        /// <param name="backgrounds">Background colours, or null.</param>
        /// <param name="style">Decorations and a link applied to every piece.</param>
        /// <returns>The rich text.</returns>
        public static RichText FromPieces(IReadOnlyList<string?> texts, IReadOnlyList<RichColor>? colors = null, IReadOnlyList<RichColor>? backgrounds = null, RichStyle style = default) {
            if (texts == null) {
                throw new ArgumentNullException(nameof(texts));
            }
            List<RichSegment> segments = new List<RichSegment>(texts.Count);
            for (int i = 0; i < texts.Count; i++) {
                RichColor foreground = Pick(colors, i, style.Color);
                RichColor background = Pick(backgrounds, i, style.BackgroundColor);
                segments.Add(new RichSegment(texts[i] ?? string.Empty, new RichStyle(foreground, background, style.Decoration, style.Link, style.Role), false));
            }
            return new RichText(segments);
        }

        /// <summary>
        /// Rich text from pieces as Write-ConsoleText takes them: strings (literal) or rich text (whose own colours win over
        /// the piece colours); piece i gets colour i and background i, and pieces beyond the colours get the first ones.
        /// </summary>
        /// <param name="pieces">The pieces: strings, rich text, or values shown with their invariant text.</param>
        /// <param name="colors">Foreground colours, or null.</param>
        /// <param name="backgrounds">Background colours, or null.</param>
        /// <param name="style">Decorations, a link and the fallback colour applied to every piece.</param>
        /// <returns>The rich text.</returns>
        public static RichText FromPieces(IReadOnlyList<object?> pieces, IReadOnlyList<RichColor>? colors, IReadOnlyList<RichColor>? backgrounds, RichStyle style) {
            if (pieces == null) {
                throw new ArgumentNullException(nameof(pieces));
            }
            List<RichText> parts = new List<RichText>(pieces.Count);
            for (int i = 0; i < pieces.Count; i++) {
                RichStyle pieceStyle = new RichStyle(Pick(colors, i, style.Color), Pick(backgrounds, i, style.BackgroundColor), style.Decoration, style.Link, style.Role);
                object? piece = pieces[i];
                parts.Add(piece is RichText rich ? rich.WithBaseStyle(pieceStyle) : Plain(Convert.ToString(piece, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty, pieceStyle));
            }
            return Concat(parts.ToArray());
        }

        /// <summary>
        /// Splits the text at a separator, keeping each piece's styles. Markup segments are split on their text only when
        /// they contain no tags.
        /// </summary>
        /// <param name="separator">The separator.</param>
        /// <returns>The parts (one when the separator does not occur).</returns>
        public List<RichText> Split(string separator) {
            if (string.IsNullOrEmpty(separator)) {
                return new List<RichText> { this };
            }
            List<RichText> result = new List<RichText>();
            List<RichSegment> current = new List<RichSegment>();
            foreach (RichSegment segment in Segments) {
                bool splittable = !segment.IsMarkup || segment.Text.IndexOf('[') < 0;
                if (!splittable || segment.Text.IndexOf(separator, StringComparison.Ordinal) < 0) {
                    current.Add(segment);
                    continue;
                }
                string[] pieces = segment.Text.Split(new[] { separator }, StringSplitOptions.None);
                for (int i = 0; i < pieces.Length; i++) {
                    if (i > 0) {
                        result.Add(new RichText(current));
                        current = new List<RichSegment>();
                    }
                    if (pieces[i].Length > 0) {
                        current.Add(new RichSegment(pieces[i], segment.Style, segment.IsMarkup));
                    }
                }
            }
            result.Add(new RichText(current));
            return result;
        }
        /// <summary>
        /// Literal text coloured as a gradient from one colour to another, per grapheme. Colours are 24-bit and are
        /// down-mapped per colour tier (on 16 colours the gradient becomes steps).
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="from">The first colour.</param>
        /// <param name="to">The last colour.</param>
        /// <param name="style">Decorations, background and link for every grapheme.</param>
        /// <param name="theme">The theme that resolves role colours, or null for the dark theme.</param>
        /// <returns>The rich text.</returns>
        public static RichText Gradient(string? text, RichColor from, RichColor to, RichStyle style = default, ITheme? theme = null) {
            string value = text ?? string.Empty;
            if (!from.TryGetRgb(theme, out byte r1, out byte g1, out byte b1) || !to.TryGetRgb(theme, out byte r2, out byte g2, out byte b2)) {
                return Plain(value, style.WithColor(from.IsDefault ? to : from));
            }
            // Graphemes per line; line breaks are kept as their own glyphs (DisplayWidth.Cells skips control characters).
            List<string> glyphs = new List<string>();
            string[] lines = value.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++) {
                if (i > 0) {
                    glyphs.Add("\n");
                }
                glyphs.AddRange(DisplayWidth.Cells(lines[i]).Select(cell => cell.Glyph));
            }
            int visible = glyphs.Count(glyph => glyph != "\n" && !string.IsNullOrWhiteSpace(glyph));
            List<RichSegment> segments = new List<RichSegment>(glyphs.Count);
            int step = 0;
            foreach (string glyph in glyphs) {
                if (glyph == "\n" || string.IsNullOrWhiteSpace(glyph)) {
                    segments.Add(new RichSegment(glyph, style, false));
                    continue;
                }
                double t = visible <= 1 ? 0 : (double)step / (visible - 1);
                step++;
                RichColor color = RichColor.FromRgb(Lerp(r1, r2, t), Lerp(g1, g2, t), Lerp(b1, b2, t));
                segments.Add(new RichSegment(glyph, style.WithColor(color), false));
            }
            return new RichText(segments);
        }

        /// <summary>Joins rich texts.</summary>
        /// <param name="parts">The parts.</param>
        /// <returns>The joined text.</returns>
        public static RichText Concat(params RichText?[] parts) {
            return new RichText((parts ?? Array.Empty<RichText>()).Where(part => part != null).SelectMany(part => part!._segments));
        }

        /// <summary>
        /// Converts a value: rich text passes through, anything else becomes literal text (its string form), so data is
        /// never interpreted as markup.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The rich text.</returns>
        public static RichText From(object? value) => value as RichText ?? Plain(value?.ToString());

        /// <summary>Returns a copy with literal text appended.</summary>
        /// <param name="text">The text.</param>
        /// <param name="style">Its style.</param>
        /// <returns>The new rich text.</returns>
        public RichText Append(string? text, RichStyle style = default) => new RichText(_segments.Concat(new[] { new RichSegment(text ?? string.Empty, style, false) }));

        /// <summary>Returns a copy with markup appended.</summary>
        /// <param name="markup">The markup.</param>
        /// <returns>The new rich text.</returns>
        public RichText AppendMarkup(string? markup) => Concat(this, FromMarkup(markup));

        /// <summary>Returns a copy with a style layered under every span (span colours and decorations win).</summary>
        /// <param name="style">The style.</param>
        /// <returns>The new rich text.</returns>
        public RichText WithBaseStyle(RichStyle style) {
            if (style.IsPlain) {
                return this;
            }
            return new RichText(_segments.Select(segment => new RichSegment(segment.Text, Layer(style, segment.Style), segment.IsMarkup)));
        }

        /// <summary>
        /// The text as Spectre-compatible markup: literal spans escaped, styled spans wrapped in tags, tabs expanded to the
        /// next multiple of eight cells. Pass it to any API that takes markup.
        /// </summary>
        /// <returns>The markup.</returns>
        public string ToMarkup() {
            if (_markup != null) {
                return _markup;
            }
            StringBuilder sb = new StringBuilder();
            int column = 0;
            foreach (RichSegment segment in _segments) {
                string specification = segment.Style.ToMarkup();
                if (segment.IsMarkup) {
                    // Wrap the complete segment: a nested tag may remain open across a newline.
                    string body = segment.Text.Replace("\r\n", "\n").Replace('\r', '\n');
                    if (specification.Length > 0) { sb.Append('[').Append(specification).Append(']'); }
                    sb.Append(body);
                    if (specification.Length > 0) { sb.Append("[/]"); }
                    string plain = Markup.Strip(body);
                    int lastBreak = plain.LastIndexOf('\n');
                    column = lastBreak >= 0 ? DisplayWidth.Measure(plain.Substring(lastBreak + 1)) : column + DisplayWidth.Measure(plain);
                    continue;
                }
                string[] lines = segment.Text.Replace("\r\n", "\n").Split('\n');
                for (int i = 0; i < lines.Length; i++) {
                    if (i > 0) {
                        sb.Append('\n');
                        column = 0;
                    }
                    string expanded = ExpandTabs(lines[i], ref column);
                    string body = Markup.Escape(expanded);
                    if (body.Length == 0) {
                        continue;
                    }
                    if (specification.Length == 0) {
                        sb.Append(body);
                    } else {
                        sb.Append('[').Append(specification).Append(']').Append(body).Append("[/]");
                    }
                }
            }
            _markup = sb.ToString();
            return _markup;
        }

        /// <summary>The styled lines for a render context (theme and colour tier applied).</summary>
        /// <param name="context">The render context.</param>
        /// <returns>The styled text.</returns>
        public StyledText ToStyledText(RenderContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            return context.Markup(ToMarkup());
        }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Block().Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Block().Render(region, context);

        /// <summary>The plain text.</summary>
        /// <returns>The text without styling.</returns>
        public override string ToString() => PlainText;

        private TextBlock Block() => new TextBlock(ToMarkup()) { Alignment = Alignment, Overflow = Overflow };

        private static RichColor Pick(IReadOnlyList<RichColor>? colors, int index, RichColor fallback) {
            if (colors == null || colors.Count == 0) {
                return fallback;
            }
            return index < colors.Count ? colors[index] : colors[0];
        }

        private static RichStyle Layer(RichStyle under, RichStyle over) {
            return new RichStyle(
                over.Color.IsDefault ? under.Color : over.Color,
                over.BackgroundColor.IsDefault ? under.BackgroundColor : over.BackgroundColor,
                under.Decoration | over.Decoration,
                over.Link ?? under.Link,
                over.Role ?? under.Role);
        }

        private static byte Lerp(byte a, byte b, double t) => (byte)Math.Round(a + ((b - a) * t));

        private static string ExpandTabs(string text, ref int column) {
            string[] parts = text.Split('\t');
            if (parts.Length == 1) {
                column += DisplayWidth.Measure(text);
                return text;
            }
            StringBuilder sb = new StringBuilder(text.Length + 8);
            for (int i = 0; i < parts.Length; i++) {
                sb.Append(parts[i]);
                column += DisplayWidth.Measure(parts[i]);
                if (i < parts.Length - 1) {
                    int spaces = 8 - (column % 8);
                    sb.Append(' ', spaces);
                    column += spaces;
                }
            }
            return sb.ToString();
        }
    }
}
