using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Highlighting {
    /// <summary>
    /// Maps highlight roles to theme roles, so highlighted text follows the theme and the colour depth like every other
    /// widget: keywords Series5, strings Series2, numbers Series3, keys Series1, types and identifiers Series6, variables
    /// Accent2, comments Muted italic, punctuation Muted, links Accent underlined, levels and diff lines by status.
    /// </summary>
    public static class HighlightStyles {
        /// <summary>The style of a role.</summary>
        /// <param name="role">The role.</param>
        /// <param name="context">The render context.</param>
        /// <returns>The style.</returns>
        public static Style For(HighlightRole role, RenderContext context) {
            switch (role) {
                case HighlightRole.Keyword:
                    return context.Style(ThemeToken.Series5);
                case HighlightRole.String:
                    return context.Style(ThemeToken.Series2);
                case HighlightRole.Number:
                    return context.Style(ThemeToken.Series3);
                case HighlightRole.Literal:
                    return context.Style(ThemeToken.Series3).With(TextDecoration.Bold);
                case HighlightRole.Comment:
                    return context.Style(ThemeToken.Muted).With(TextDecoration.Italic);
                case HighlightRole.Key:
                    return context.Style(ThemeToken.Series1);
                case HighlightRole.Punctuation:
                    return context.Style(ThemeToken.Muted);
                case HighlightRole.Type:
                case HighlightRole.Identifier:
                    return context.Style(ThemeToken.Series6);
                case HighlightRole.Variable:
                    return context.Style(ThemeToken.Accent2);
                case HighlightRole.Error:
                case HighlightRole.Removed:
                    return context.Style(ThemeToken.Fail);
                case HighlightRole.Warning:
                    return context.Style(ThemeToken.Warn);
                case HighlightRole.Info:
                    return context.Style(ThemeToken.Info);
                case HighlightRole.Added:
                    return context.Style(ThemeToken.Pass);
                case HighlightRole.Header:
                case HighlightRole.Heading:
                    return context.Style(ThemeToken.Accent).With(TextDecoration.Bold);
                case HighlightRole.Timestamp:
                    return context.Style(ThemeToken.Muted);
                case HighlightRole.Link:
                    return context.Style(ThemeToken.Accent).With(TextDecoration.Underline);
                case HighlightRole.Emphasis:
                    return context.Style(ThemeToken.Text).With(TextDecoration.Italic);
                case HighlightRole.Strong:
                    return context.Style(ThemeToken.Text).With(TextDecoration.Bold);
                default:
                    return context.Style(ThemeToken.Text);
            }
        }

        /// <summary>
        /// Highlighted text as styled lines: tokens take their role's style, the rest plain. The text is literal (never
        /// markup); tabs become spaces to the next multiple of four and other control characters are dropped.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="highlighter">The highlighter, or null for plain text.</param>
        /// <param name="context">The render context.</param>
        /// <param name="maxLines">The most lines to return (0 for all).</param>
        /// <returns>The lines.</returns>
        public static List<StyledLine> ToLines(string text, ITextHighlighter? highlighter, RenderContext context, int maxLines = 0) {
            string source = text ?? string.Empty;
            IReadOnlyList<HighlightToken> tokens = highlighter?.Tokenize(source) ?? new HighlightToken[0];
            List<StyledLine> lines = new List<StyledLine>();
            List<TextSpan> spans = new List<TextSpan>();
            StringBuilder run = new StringBuilder();
            Style plain = context.Style(ThemeToken.Text);
            Style current = plain;
            int column = 0;
            int tokenIndex = 0;
            for (int i = 0; i <= source.Length; i++) {
                while (tokenIndex < tokens.Count && tokens[tokenIndex].Start + tokens[tokenIndex].Length <= i && i < source.Length) {
                    tokenIndex++;
                }
                Style style = plain;
                if (tokenIndex < tokens.Count && i >= tokens[tokenIndex].Start && i < tokens[tokenIndex].Start + tokens[tokenIndex].Length) {
                    HighlightToken token = tokens[tokenIndex];
                    style = For(token.Role, context);
                    if (token.Link != null) {
                        style = style.WithLink(token.Link);
                    }
                }
                bool end = i == source.Length;
                char c = end ? '\n' : source[i];
                if (!style.Equals(current) || c == '\n') {
                    if (run.Length > 0) {
                        spans.Add(new TextSpan(run.ToString(), current));
                        run.Clear();
                    }
                    current = style;
                }
                if (c == '\n') {
                    // A final line break does not start one more, empty line.
                    if (!end || source.Length == 0 || source[source.Length - 1] != '\n') {
                        lines.Add(new StyledLine(spans));
                    }
                    spans = new List<TextSpan>();
                    column = 0;
                    if (maxLines > 0 && lines.Count >= maxLines) {
                        break;
                    }
                    continue;
                }
                if (c == '\t') {
                    int pad = 4 - (column % 4);
                    run.Append(' ', pad);
                    column += pad;
                } else if (c == '\r' || char.IsControl(c)) {
                    continue;
                } else {
                    run.Append(c);
                    column++;
                }
            }
            return lines;
        }

        /// <summary>
        /// Highlighted text as markup with theme role names (for log regions and other markup sinks). The text is escaped,
        /// so nothing in it is read as markup; control characters other than line breaks are dropped.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="highlighter">The highlighter.</param>
        /// <returns>The markup.</returns>
        public static string ToMarkup(string text, ITextHighlighter highlighter) {
            string source = text ?? string.Empty;
            StringBuilder sb = new StringBuilder(source.Length + 32);
            int position = 0;
            foreach (HighlightToken token in highlighter.Tokenize(source)) {
                if (token.Start < position) {
                    continue;
                }
                sb.Append(Markup.Escape(Clean(source.Substring(position, token.Start - position))));
                string style = MarkupStyle(token.Role);
                if (token.Link != null && token.Link.IndexOfAny(new[] { '[', ']', ' ' }) < 0) {
                    style += " link=" + token.Link;
                }
                sb.Append('[').Append(style).Append(']').Append(Markup.Escape(Clean(source.Substring(token.Start, token.Length)))).Append("[/]");
                position = token.Start + token.Length;
            }
            sb.Append(Markup.Escape(Clean(source.Substring(position))));
            return sb.ToString();
        }

        private static string Clean(string text) {
            StringBuilder sb = new StringBuilder(text.Length);
            foreach (char c in text) {
                if (c == '\n' || c == '\t' || !char.IsControl(c)) {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string MarkupStyle(HighlightRole role) {
            switch (role) {
                case HighlightRole.Keyword:
                    return "series5";
                case HighlightRole.String:
                    return "series2";
                case HighlightRole.Number:
                    return "series3";
                case HighlightRole.Literal:
                    return "series3 bold";
                case HighlightRole.Comment:
                    return "muted italic";
                case HighlightRole.Key:
                    return "series1";
                case HighlightRole.Punctuation:
                case HighlightRole.Timestamp:
                    return "muted";
                case HighlightRole.Type:
                case HighlightRole.Identifier:
                    return "series6";
                case HighlightRole.Variable:
                    return "accent2";
                case HighlightRole.Error:
                case HighlightRole.Removed:
                    return "fail";
                case HighlightRole.Warning:
                    return "warn";
                case HighlightRole.Info:
                    return "info";
                case HighlightRole.Added:
                    return "pass";
                case HighlightRole.Header:
                case HighlightRole.Heading:
                    return "accent bold";
                case HighlightRole.Link:
                    return "accent underline";
                case HighlightRole.Emphasis:
                    return "italic";
                case HighlightRole.Strong:
                    return "bold";
                default:
                    return "text";
            }
        }    }
}
