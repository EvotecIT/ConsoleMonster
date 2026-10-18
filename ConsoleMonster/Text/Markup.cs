using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Text {

    /// <summary>
    /// Parses the Spectre.Console markup dialect: nested style tags (<c>[bold red on blue]</c>, <c>[cadetblue]</c>,
    /// <c>[#ff8800]</c>, <c>[link=https://…]</c>), theme tokens (<c>[pass]</c>, <c>[muted]</c>), <c>[/]</c> to close the
    /// innermost tag, and <c>[[</c> / <c>]]</c> for literal brackets.
    /// </summary>
    /// <remarks>
    /// Lenient parsing (the default) never throws. A tag that is not a known style is removed and its text kept when a later
    /// <c>[/]</c> closes it (so <c>[unknowncolour]text[/]</c> shows <c>text</c>); an unknown tag that is never closed stays
    /// literal, because it is most likely text such as <c>[x]</c>. Set <see cref="Strict"/> (or pass <c>strict: true</c>) to
    /// throw a <see cref="FormatException"/> for anything <see cref="Validate(string, out string?)"/> rejects instead.
    /// </remarks>
    public static class Markup {

        /// <summary>
        /// When true, <see cref="Parse(string?, ITheme?, ColorDepth)"/> and everything built on it (for example
        /// <see cref="Terminal.WriteMarkupLine(string)"/>) throw a <see cref="FormatException"/> for invalid markup: unknown
        /// tags, unbalanced tags and unescaped brackets. Useful in tests and migration checks. Default false.
        /// </summary>
        public static bool Strict { get; set; }

        /// <summary>
        /// When true, <c>:name:</c> shortcodes in the text of markup become emoji (<c>:white_check_mark:</c> is ✅; see
        /// <see cref="Glyphs.Shortcodes"/>). Off by default, so colons in data are never touched. On glyph tiers that cannot
        /// show emoji, the symbol fallback turns them into ASCII or Basic equivalents.
        /// </summary>
        public static bool Shortcodes { get; set; }

        /// <summary>Parses markup; lenient unless <see cref="Strict"/> is set (see the remarks on <see cref="Markup"/>).</summary>
        /// <param name="markup">Markup, or null for empty text.</param>
        /// <param name="theme">Theme token resolver; defaults to the built-in dark theme.</param>
        /// <param name="depth">Target colour depth.</param>
        /// <returns>Styled lines, with tabs expanded to four spaces.</returns>
        /// <exception cref="FormatException">The markup is invalid and <see cref="Strict"/> is set.</exception>
        public static StyledText Parse(string? markup, ITheme? theme = null, ColorDepth depth = ColorDepth.TrueColor) => Parse(markup, theme, depth, Strict);

        /// <summary>Parses markup, leniently or strictly.</summary>
        /// <param name="markup">Markup, or null for empty text.</param>
        /// <param name="theme">Theme token resolver; defaults to the built-in dark theme.</param>
        /// <param name="depth">Target colour depth.</param>
        /// <param name="strict">Whether invalid markup throws instead of being rendered leniently.</param>
        /// <returns>Styled lines, with tabs expanded to four spaces.</returns>
        /// <exception cref="FormatException">The markup is invalid and <paramref name="strict"/> is true.</exception>
        public static StyledText Parse(string? markup, ITheme? theme, ColorDepth depth, bool strict) {
            string text = markup ?? string.Empty;
            if (strict && !Process(text, theme, depth, true, out List<TextSpan> _, out string? error)) {
                throw new FormatException("Invalid markup: " + error);
            }
            Process(text, theme, depth, false, out List<TextSpan> spans, out string? _);
            if (Shortcodes) {
                for (int i = 0; i < spans.Count; i++) {
                    string replaced = Glyphs.Shortcodes.Replace(spans[i].Text);
                    if (!ReferenceEquals(replaced, spans[i].Text)) {
                        spans[i] = new TextSpan(replaced, spans[i].Style);
                    }
                }
            }
            return StyledText.FromSpans(spans);
        }

        /// <summary>Removes tags and resolves escapes (lenient: an unknown tag that is never closed stays literal).</summary>
        /// <param name="markup">Markup, or null.</param>
        /// <returns>Plain text with normalized newlines and expanded tabs.</returns>
        public static string Strip(string? markup) {
            StyledText parsed = Parse(markup, null, ColorDepth.TrueColor, false);
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < parsed.Lines.Count; i++) {
                if (i > 0) {
                    result.Append('\n');
                }
                result.Append(parsed.Lines[i].PlainText);
            }
            return result.ToString();
        }

        /// <summary>Escapes brackets so text is shown literally inside markup (Spectre's <c>Markup.Escape</c>).</summary>
        /// <param name="text">Plain text, or null.</param>
        /// <returns>Escaped text.</returns>
        public static string Escape(string? text) => (text ?? string.Empty).Replace("[", "[[").Replace("]", "]]");

        /// <summary>
        /// Builds markup from an interpolated string, escaping every argument (format specifiers apply first), so values never
        /// become tags: <c>Markup.Interpolate($"[pass]{name}[/] took {elapsed:N1}s")</c>.
        /// </summary>
        /// <param name="value">The interpolated string.</param>
        /// <returns>Markup with escaped arguments.</returns>
        public static string Interpolate(FormattableString value) => Interpolate(CultureInfo.CurrentCulture, value);

        /// <summary>Builds markup from an interpolated string with an explicit culture, escaping every argument.</summary>
        /// <param name="provider">The culture for formatting the arguments.</param>
        /// <param name="value">The interpolated string.</param>
        /// <returns>Markup with escaped arguments.</returns>
        public static string Interpolate(IFormatProvider? provider, FormattableString value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            object?[] arguments = value.GetArguments();
            object?[] escaped = new object?[arguments.Length];
            for (int i = 0; i < arguments.Length; i++) {
                escaped[i] = new EscapedArgument(arguments[i]);
            }
            return string.Format(provider, value.Format, escaped);
        }

        /// <summary>Parses a style, including decorations, foreground/background colours, links and theme tokens.</summary>
        /// <param name="specification">Style words.</param>
        /// <param name="theme">Token resolver; defaults to the built-in dark theme.</param>
        /// <param name="depth">Target colour depth.</param>
        /// <param name="style">Parsed style, or plain when invalid.</param>
        /// <returns>Whether the entire specification is valid.</returns>
        public static bool TryParseStyle(string? specification, ITheme? theme, ColorDepth depth, out Style style) {
            bool valid = MarkupStyle.TryParse(specification, theme, depth, out MarkupStyle parsed);
            style = valid ? parsed.Style : Style.Plain;
            return valid;
        }

        /// <summary>Strictly checks tags, escapes and nesting using built-in theme tokens.</summary>
        /// <param name="markup">Markup to validate.</param>
        /// <param name="error">First error, or null when valid.</param>
        /// <returns>Whether all tags are recognized and balanced.</returns>
        public static bool Validate(string markup, out string? error) => Validate(markup, null, out error);

        /// <summary>Strictly checks markup with a custom theme token resolver.</summary>
        /// <param name="markup">Markup to validate.</param>
        /// <param name="theme">Token resolver; defaults to the built-in dark theme.</param>
        /// <param name="error">First error, or null when valid.</param>
        /// <returns>Whether all tags are recognized and balanced.</returns>
        public static bool Validate(string markup, ITheme? theme, out string? error) {
            return Process(markup ?? string.Empty, theme, ColorDepth.TrueColor, true, out List<TextSpan> _, out error);
        }

        private static bool Process(string text, ITheme? theme, ColorDepth depth, bool strict,
            out List<TextSpan> spans, out string? error) {
            spans = new List<TextSpan>();
            error = null;
            Stack<Frame> frames = new Stack<Frame>();
            List<int> dropped = new List<int>();
            Style current = Style.Plain;
            StringBuilder literal = new StringBuilder();
            for (int i = 0; i < text.Length; i++) {
                char c = text[i];
                if ((c == '[' || c == ']') && i + 1 < text.Length && text[i + 1] == c) {
                    literal.Append(c);
                    i++;
                } else if (c == '[') {
                    int close = text.IndexOf(']', i + 1);
                    if (close < 0) {
                        if (strict) {
                            error = "Unterminated tag at position " + i + ".";
                            return false;
                        }
                        literal.Append(text, i, text.Length - i);
                        break;
                    }
                    string tag = text.Substring(i + 1, close - i - 1);
                    if (tag == "/") {
                        Flush(literal, spans, current);
                        if (frames.Count > 0) {
                            Frame frame = frames.Pop();
                            if (frame.Placeholder >= 0) {
                                dropped.Add(frame.Placeholder);
                            }
                            if (frame.LinkFromText) {
                                LinkText(spans, frame.SpanStart, dropped);
                            }
                            current = frame.Parent;
                        } else if (strict) {
                            error = "Closing tag without an opening tag at position " + i + ".";
                            return false;
                        }
                    } else if (MarkupStyle.TryParse(tag, theme, depth, out MarkupStyle style)) {
                        Flush(literal, spans, current);
                        frames.Push(new Frame(current, -1, style.LinkFromText, spans.Count));
                        current = style.Apply(current);
                    } else {
                        if (strict) {
                            error = "Invalid style tag at position " + i + ": [" + tag + "].";
                            return false;
                        }
                        if (tag.Trim().Length == 0 || tag.IndexOf('[') >= 0) {
                            literal.Append(text, i, close - i + 1);
                        } else {
                            // An unknown tag: kept as a placeholder that is dropped if a [/] closes it, literal otherwise.
                            Flush(literal, spans, current);
                            spans.Add(new TextSpan(text.Substring(i, close - i + 1), current));
                            frames.Push(new Frame(current, spans.Count - 1, false, spans.Count));
                        }
                    }
                    i = close;
                } else if (c == ']' && strict) {
                    error = "Unescaped closing bracket at position " + i + ".";
                    return false;
                } else {
                    literal.Append(c);
                }
            }
            if (strict && frames.Count > 0) {
                error = "Unclosed style tag.";
                return false;
            }
            Flush(literal, spans, current);
            if (dropped.Count > 0) {
                dropped.Sort();
                for (int d = dropped.Count - 1; d >= 0; d--) {
                    spans.RemoveAt(dropped[d]);
                }
            }
            return true;
        }

        // [link]https://…[/]: the text inside becomes the target of spans that have no link of their own.
        private static void LinkText(List<TextSpan> spans, int start, List<int> dropped) {
            StringBuilder target = new StringBuilder();
            for (int s = start; s < spans.Count; s++) {
                if (!dropped.Contains(s)) {
                    target.Append(spans[s].Text);
                }
            }
            string url = target.ToString().Trim();
            if (url.Length == 0) {
                return;
            }
            for (int s = start; s < spans.Count; s++) {
                if (spans[s].Style.Link == null) {
                    spans[s] = new TextSpan(spans[s].Text, spans[s].Style.WithLink(url));
                }
            }
        }

        private static void Flush(StringBuilder literal, List<TextSpan> spans, Style style) {
            if (literal.Length == 0) {
                return;
            }
            spans.Add(new TextSpan(literal.ToString(), style));
            literal.Clear();
        }

        private readonly struct Frame {
            internal Frame(Style parent, int placeholder, bool linkFromText, int spanStart) {
                Parent = parent;
                Placeholder = placeholder;
                LinkFromText = linkFromText;
                SpanStart = spanStart;
            }

            internal Style Parent { get; }

            // The index of an unknown tag's literal span, or -1 for a known style.
            internal int Placeholder { get; }

            internal bool LinkFromText { get; }

            internal int SpanStart { get; }
        }

        private sealed class EscapedArgument : IFormattable {
            private readonly object? _value;

            internal EscapedArgument(object? value) {
                _value = value;
            }

            public string ToString(string? format, IFormatProvider? formatProvider) {
                string text = _value is IFormattable formattable
                    ? formattable.ToString(format, formatProvider)
                    : _value?.ToString() ?? string.Empty;
                return Escape(text);
            }

            public override string ToString() => ToString(null, CultureInfo.CurrentCulture);
        }
    }
}
