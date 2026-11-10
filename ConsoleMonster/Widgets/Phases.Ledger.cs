using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Phases {
        // The furthest phase that has run. It only moves forward, so a gap between two adjacent steps (one finished, the
        // next not started yet) never takes a windowed ledger back to an older step that is still running.
        private int _frontier = -1;

        private void Advance(StatusItem phase) {
            if (phase.Status == StatusKind.Running) {
                _frontier = Math.Max(_frontier, _phases.IndexOf(phase));
            }
        }

        // Keep the full label area before deciding whether the detail can share its row.
        private List<(StyledLine Line, string Result, StatusKind Status, int Phase)> LedgerLines(int width, RenderContext context) {
            var result = new List<(StyledLine, string, StatusKind, int)>();
            if (width <= 0) return result;
            int digits = Math.Max(2, _phases.Count.ToString(CultureInfo.InvariantCulture).Length);
            for (int i = 0; i < _phases.Count; i++) {
                StatusItem phase = _phases[i];
                string counter = "[" + (i + 1).ToString("D" + digits, CultureInfo.InvariantCulture) + "/" + _phases.Count.ToString("D" + digits, CultureInfo.InvariantCulture) + "] ";
                Style statusStyle = context.Style(phase.Status);
                StyledLine prefix = TextWrapper.Truncate(StyledLine.From(counter + context.Glyphs.StatusMarker(phase.Status) + " ", statusStyle), Math.Max(0, width - 1), context.Ellipsis);
                int labelWidth = Math.Max(1, width - prefix.Width);
                StyledLine label = context.Markup(phase.Label).Lines[0].WithBaseStyle(context.Style(ThemeToken.Text));
                string detail = phase.Detail ?? phase.Status.ToWord(context);
                bool inline = prefix.Width + label.Width + 1 + DisplayWidth.Measure(detail) <= width;
                IReadOnlyList<StyledLine> labels = inline ? new[] { label } : TextWrapper.Wrap(label, labelWidth);
                for (int j = 0; j < labels.Count; j++) {
                    var spans = new List<TextSpan> { new TextSpan(j == 0 ? prefix.PlainText : new string(' ', prefix.Width), statusStyle) };
                    spans.AddRange(labels[j]);
                    result.Add((new StyledLine(spans), inline ? detail : string.Empty, phase.Status, i));
                }
                if (!inline && detail.Length > 0) {
                    foreach (StyledLine line in TextWrapper.Wrap(StyledLine.From(detail), labelWidth)) {
                        result.Add((StyledLine.Empty, line.PlainText, phase.Status, i));
                    }
                }
            }
            return result;
        }
        private Size LedgerMeasure(Constraints constraints, RenderContext context) {
            int height = LedgerLines(constraints.MaxWidth, context).Count;
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }
        private void LedgerRender(Region region, RenderContext context) {
            var lines = LedgerLines(region.Width, context);
            int first = 0, end = lines.Count, earlier = 0, more = 0, y = 0;
            if (lines.Count > region.Height && region.Height > 0) {
                LedgerWindow(lines, region.Height, out first, out end, out earlier, out more);
            }
            if (earlier > 0) {
                region.Write(0, y++, Localization.WidgetLabels.Format(context.Labels.EarlierCount, earlier.ToString("N0", context.Culture)), context.Style(ThemeToken.Muted));
            }
            for (int i = first; i < end && y < region.Height; i++, y++) {
                var line = lines[i];
                region.Write(0, y, line.Line);
                region.Write(Math.Max(0, region.Width - DisplayWidth.Measure(line.Result)), y, line.Result, context.Style(line.Status));
            }
            if (more > 0 && y < region.Height) {
                region.Write(0, y, Localization.WidgetLabels.Format(context.Labels.MoreCount, more.ToString("N0", context.Culture)), context.Style(ThemeToken.Muted));
            }
        }

        // Whole phases around the frontier: the frontier phase, the next one, then earlier phases, then later ones, while
        // they fit with a "+N earlier" row above and a "+N more" row below for what is left out. Before any phase has run
        // the window starts at the top.
        private void LedgerWindow(List<(StyledLine Line, string Result, StatusKind Status, int Phase)> lines, int rows,
            out int first, out int end, out int earlier, out int more) {
            int count = _phases.Count;
            int[] start = new int[count + 1];
            for (int i = 0, phase = 0; phase <= count; phase++) {
                while (i < lines.Count && lines[i].Phase < phase) { i++; }
                start[phase] = i;
            }
            int Size(int a, int b) => start[b + 1] - start[a] + (a > 0 ? 1 : 0) + (b < count - 1 ? 1 : 0);
            int anchor = Math.Max(0, Math.Min(count - 1, _frontier));
            int top = anchor, bottom = anchor;
            if (bottom + 1 < count && Size(top, bottom + 1) <= rows) { bottom++; }
            while (top > 0 && Size(top - 1, bottom) <= rows) { top--; }
            while (bottom + 1 < count && Size(top, bottom + 1) <= rows) { bottom++; }
            earlier = top;
            more = count - 1 - bottom;
            first = start[top];
            end = start[bottom + 1];
            if (Size(top, bottom) > rows) {
                // One phase taller than the window: its first rows, without the counts.
                earlier = more = 0;
                end = Math.Min(end, first + rows);
            }
        }
    }
}
