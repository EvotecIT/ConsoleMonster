using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Highlighting;
using ConsoleMonster.Localization;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class ExceptionView {
        /// <summary>Uses a message-first layout with the invocation line and a folded inner-exception count.</summary>
        public bool Compact { get; set; }

        /// <summary>A muted next-step hint, or null for no hint.</summary>
        public string? DetailsHint { get; set; }

        private List<StyledLine> CompactLines(int width, RenderContext context) {
            List<StyledLine> lines = new List<StyledLine>();
            Style text = context.Style(ThemeToken.Text);
            Style muted = context.Style(ThemeToken.Muted);
            string label = context.Labels.ErrorLabel + ": ";
            int bodyWidth = Math.Max(1, width - DisplayWidth.Measure(label));
            bool first = true;
            foreach (string paragraph in _info.Message.Replace("\r\n", "\n").Split('\n')) {
                foreach (StyledLine part in TextWrapper.Wrap(StyledLine.From(paragraph, text), bodyWidth)) {
                    lines.Add(new StyledLine(new[] { new TextSpan(first ? label : new string(' ', DisplayWidth.Measure(label)), context.Style(ThemeToken.Fail).With(TextDecoration.Bold)), new TextSpan(part.PlainText, text) }));
                    first = false;
                }
            }
            AddInvocation(lines, width, context);
            string facts = Join(_info.Category, UsefulErrorId(_info), context);
            if (!string.IsNullOrEmpty(_info.Target) && facts.IndexOf(_info.Target!, StringComparison.Ordinal) < 0) {
                facts = Join(facts, _info.Target, context);
            }
            AddCompactWrapped(lines, facts, width, muted);
            if (ShowStack) {
                AddFrames(lines, _info.ScriptFrames, string.Empty, width, context);
                AddFrames(lines, _info.Frames, string.Empty, width, context);
                ExceptionInfo? inner = _info.Inner;
                for (int n = 0; inner != null && n < MaxInner; n++, inner = inner.Inner) {
                    AddCompactWrapped(lines, context.Labels.CausedBy + " " + inner.Type + ": " + inner.Message, width, muted);
                }
            } else {
                int count = 0;
                for (ExceptionInfo? inner = _info.Inner; inner != null; inner = inner.Inner) { count++; }
                if (count > 0) { AddCompactWrapped(lines, count == 1 ? context.Labels.OneInnerError : WidgetLabels.Format(context.Labels.InnerErrorCount, count.ToString(CultureInfo.InvariantCulture)), width, muted); }
            }
            AddCompactWrapped(lines, DetailsHint, width, muted);
            return lines;
        }

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
            string source = _info.SourceLine!.TrimEnd('\r', '\n');
            int column = Math.Max(0, Math.Min(source.Length, _info.ColumnNumber - 1));
            int length = Math.Max(1, Math.Min(Math.Max(1, source.Length - column), _info.ErrorLength));
            // Expand tabs before measuring terminal cells. UTF-16 offsets from InvocationInfo stay in the original line.
            string before = source.Substring(0, column).Replace("\t", "    ");
            string marked = source.Substring(column, Math.Min(length, source.Length - column)).Replace("\t", "    ");
            int start = DisplayWidth.Measure(before);
            int span = Math.Max(1, DisplayWidth.Measure(marked));
            string expanded = source.Replace("\t", "    ");
            List<StyledLine> code = HighlightStyles.ToLines(expanded, Highlighters.Get("powershell"), context, 1);
            // Keep the failing expression visible at narrow widths instead of clipping away its marker.
            int offset = Math.Max(0, start + Math.Min(span, Math.Max(1, width / 2)) - Math.Max(1, width));
            StyledLineMap map = new StyledLineMap(code.Count > 0 ? code[0] : StyledLine.Empty);
            int first = map.PrefixEnd(0, offset);
            int skipped = 0;
            for (int n = 0; n < first; n++) { skipped += map.Clusters[n].Width; }
            StyledLine visible = map.Slice(first, map.PrefixEnd(first, Math.Max(1, width)));
            lines.Add(visible);
            int caret = Math.Max(0, start - skipped);
            int carets = Math.Max(1, Math.Min(span, Math.Max(1, width - caret)));
            lines.Add(StyledLine.From(new string(' ', caret) + new string('^', carets), muted));
        }

        private static void AddCompactWrapped(List<StyledLine> lines, string? value, int width, Style style) {
            if (string.IsNullOrEmpty(value)) { return; }
            foreach (string paragraph in value!.Replace("\r\n", "\n").Split('\n')) {
                lines.AddRange(TextWrapper.Wrap(StyledLine.From(paragraph, style), Math.Max(1, width)));
            }
        }
    }
}
