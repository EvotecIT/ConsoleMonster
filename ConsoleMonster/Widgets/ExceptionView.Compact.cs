using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
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
            string label = (string.IsNullOrWhiteSpace(_info.CommandName) ? context.Labels.ErrorLabel : _info.CommandName!.Trim()) + ": ";
            if (DisplayWidth.Measure(label) >= width) {
                label = TextWrapper.Truncate(StyledLine.From(label), Math.Max(1, width - 1), context.Ellipsis).PlainText;
            }
            int bodyWidth = Math.Max(1, width - DisplayWidth.Measure(label));
            bool first = true;
            string message = CompactMessage(_info.Message, !ShowStack, out int hiddenStack);
            foreach (string paragraph in message.Split('\n')) {
                foreach (StyledLine part in TextWrapper.Wrap(StyledLine.From(paragraph, text), bodyWidth)) {
                    lines.Add(new StyledLine(new[] { new TextSpan(first ? label : new string(' ', DisplayWidth.Measure(label)), context.Style(ThemeToken.Fail).With(TextDecoration.Bold)), new TextSpan(part.PlainText, text) }));
                    first = false;
                }
            }
            AddInvocation(lines, width, context);
            string facts = string.IsNullOrWhiteSpace(_info.Category) || string.Equals(_info.Category, "NotSpecified", StringComparison.OrdinalIgnoreCase)
                ? _info.Type : _info.Category!;
            if (!string.IsNullOrEmpty(_info.Target) && facts.IndexOf(_info.Target!, StringComparison.Ordinal) < 0) {
                facts = Join(facts, _info.Target, context);
            }
            AddCompactWrapped(lines, facts, width, muted);
            string? footer = DetailsHint;
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
                if (count > 0) {
                    ExceptionInfo? cause = DistinctCause(message);
                    if (cause != null) {
                        string causeText = CompactMessage(cause.Message, true, out _);
                        AddCompactCause(lines, context.Labels.ErrorCause + " ", causeText, width, muted);
                    }
                    string causes = count == 1 ? context.Labels.OneInnerError : WidgetLabels.Format(context.Labels.InnerErrorCount, count.ToString(CultureInfo.InvariantCulture));
                    if (cause == null && _info.Inner != null) { causes += " (" + _info.Inner.Type + ")"; }
                    footer = Join(causes, DetailsHint, context);
                }
            }
            if (hiddenStack > 0) {
                string hidden = hiddenStack == 1 ? context.Labels.HiddenStackLine
                    : WidgetLabels.Format(context.Labels.HiddenStackLines, hiddenStack.ToString(CultureInfo.InvariantCulture));
                footer = Join(hidden, footer, context);
            }
            AddCompactWrapped(lines, footer, width, muted);
            return lines;
        }

        private static void AddCompactWrapped(List<StyledLine> lines, string? value, int width, Style style) {
            if (string.IsNullOrEmpty(value)) { return; }
            foreach (string paragraph in value!.Replace("\r\n", "\n").Split('\n')) {
                lines.AddRange(TextWrapper.Wrap(StyledLine.From(paragraph, style), Math.Max(1, width)));
            }
        }

        private static void AddCompactCause(List<StyledLine> lines, string prefix, string message, int width, Style style) {
            int indent = DisplayWidth.Measure(prefix);
            bool first = true;
            foreach (string paragraph in message.Split('\n')) {
                foreach (StyledLine part in TextWrapper.Wrap(StyledLine.From(paragraph, style), Math.Max(1, width - indent))) {
                    lines.Add(StyledLine.From((first ? prefix : new string(' ', indent)) + part.PlainText, style));
                    first = false;
                }
            }
        }
    }
}
