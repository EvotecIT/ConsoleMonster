using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Text {
    /// <summary>Parses nested Spectre-compatible style tags, theme tokens and escaped brackets.</summary>
    public static class Markup {
        /// <summary>Parses leniently: invalid tags stay literal, unmatched closing tags are ignored and open tags end at EOF.</summary>
        /// <param name="markup">Markup, or null for empty text.</param>
        /// <param name="theme">Theme token resolver; defaults to the built-in dark theme.</param>
        /// <param name="depth">Target colour depth.</param>
        /// <returns>Styled lines, with tabs expanded to four spaces.</returns>
        public static StyledText Parse(string? markup, ITheme? theme = null, ColorDepth depth = ColorDepth.TrueColor) {
            Process(markup ?? string.Empty, theme, depth, false, out List<TextSpan> spans, out string? _);
            return StyledText.FromSpans(spans);
        }

        /// <summary>Removes recognized tags and resolves escapes. Unknown tags remain literal.</summary>
        /// <param name="markup">Markup, or null.</param>
        /// <returns>Plain text with normalized newlines and expanded tabs.</returns>
        public static string Strip(string? markup) {
            StyledText parsed = Parse(markup);
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < parsed.Lines.Count; i++) {
                if (i > 0) {
                    result.Append('\n');
                }
                result.Append(parsed.Lines[i].PlainText);
            }
            return result.ToString();
        }

        /// <summary>Escapes brackets so text is shown literally inside markup.</summary>
        /// <param name="text">Plain text, or null.</param>
        /// <returns>Escaped text.</returns>
        public static string Escape(string? text) => (text ?? string.Empty).Replace("[", "[[").Replace("]", "]]");

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
            Stack<Style> parents = new Stack<Style>();
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
                        if (strict) { error = "Unterminated tag at position " + i + "."; return false; }
                        literal.Append(text, i, text.Length - i);
                        break;
                    }
                    string tag = text.Substring(i + 1, close - i - 1);
                    if (tag == "/") {
                        Flush(literal, spans, current);
                        if (parents.Count > 0) {
                            current = parents.Pop();
                        } else if (strict) {
                            error = "Closing tag without an opening tag at position " + i + ".";
                            return false;
                        }
                    } else if (MarkupStyle.TryParse(tag, theme, depth, out MarkupStyle style)) {
                        Flush(literal, spans, current);
                        parents.Push(current);
                        current = style.Apply(current);
                    } else {
                        if (strict) { error = "Invalid style tag at position " + i + ": [" + tag + "]."; return false; }
                        literal.Append(text, i, close - i + 1);
                    }
                    i = close;
                } else if (c == ']' && strict) {
                    error = "Unescaped closing bracket at position " + i + ".";
                    return false;
                } else {
                    literal.Append(c);
                }
            }
            if (strict && parents.Count > 0) {
                error = "Unclosed style tag.";
                return false;
            }
            Flush(literal, spans, current);
            return true;
        }

        private static void Flush(StringBuilder literal, List<TextSpan> spans, Style style) {
            if (literal.Length == 0) { return; }
            spans.Add(new TextSpan(literal.ToString(), style));
            literal.Clear();
        }
    }
}
