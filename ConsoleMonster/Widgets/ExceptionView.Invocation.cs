using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Highlighting;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class ExceptionView {
        private void AddInvocation(List<StyledLine> lines, int width, RenderContext context) {
            Style muted = context.Style(ThemeToken.Muted);
            string? path = _info.ScriptPath;
            if (!string.IsNullOrEmpty(path) || _info.LineNumber > 0) {
                string location = (string.IsNullOrEmpty(path) ? context.Labels.CommandLine : ShortPath(path!))
                    + (_info.LineNumber > 0 ? ":" + _info.LineNumber.ToString(CultureInfo.InvariantCulture) : string.Empty)
                    + (_info.ColumnNumber > 0 ? ":" + _info.ColumnNumber.ToString(CultureInfo.InvariantCulture) : string.Empty);
                AddCompactWrapped(lines, location, width, muted);
            }
            if (string.IsNullOrEmpty(_info.SourceLine)) { return; }
            string original = _info.SourceLine!.TrimEnd('\r', '\n');
            int column = Math.Max(0, Math.Min(original.Length, _info.ColumnNumber - 1));
            int length = Math.Min(Math.Max(1, _info.ErrorLength), original.Length - column);
            int indent = 0;
            while (indent < column && (original[indent] == ' ' || original[indent] == '\t')) { indent++; }
            string source = original.Substring(indent);
            column -= indent;
            // Invocation offsets stay UTF-16; measuring expanded text keeps tabs, wide cells and graphemes aligned.
            int start = DisplayWidth.Measure(source.Substring(0, column).Replace("\t", "    "));
            int span = Math.Max(1, DisplayWidth.Measure(source.Substring(column, length).Replace("\t", "    ")));
            List<StyledLine> code = HighlightStyles.ToLines(source.Replace("\t", "    "), Highlighters.Get("powershell"), context, 1);
            StyledLineMap map = new StyledLineMap(code.Count > 0 ? code[0] : StyledLine.Empty);
            width = Math.Max(1, width);
            string ellipsis = context.Ellipsis;
            int ellipsisWidth = DisplayWidth.Measure(ellipsis);
            int budget = Math.Max(1, width - 2 * ellipsisWidth);
            int offset = map.Width <= width ? 0 : Math.Max(0, start - Math.Max(0, budget - span) / 2);
            // Snap to complete graphemes; never split a wide glyph or surrogate pair at the left edge.
            int first = map.PrefixEnd(0, offset);
            int skipped = 0;
            for (int n = 0; n < first; n++) { skipped += map.Clusters[n].Width; }
            string prefix = first > 0 && width > ellipsisWidth ? ellipsis : string.Empty;
            int prefixWidth = DisplayWidth.Measure(prefix);
            int available = Math.Max(1, width - prefixWidth);
            int end = map.PrefixEnd(first, available);
            bool suffix = end < map.Clusters.Count && available > ellipsisWidth;
            if (suffix) { end = map.PrefixEnd(first, available - ellipsisWidth); }
            List<TextSpan> visible = new List<TextSpan>();
            if (prefix.Length > 0) { visible.Add(new TextSpan(prefix, muted)); }
            foreach (TextSpan part in map.Slice(first, end)) { visible.Add(part); }
            if (suffix) { visible.Add(new TextSpan(ellipsis, muted)); }
            lines.Add(new StyledLine(visible));
            int caret = Math.Max(0, Math.Min(width - 1, prefixWidth + start - skipped));
            int cells = 0;
            for (int n = first; n < end; n++) { cells += map.Clusters[n].Width; }
            int carets = Math.Max(1, Math.Min(span, Math.Max(1, prefixWidth + cells - caret)));
            lines.Add(StyledLine.From(new string(' ', caret) + new string('^', carets), context.Style(ThemeToken.Fail).With(TextDecoration.Dim)));
        }
    }
}
