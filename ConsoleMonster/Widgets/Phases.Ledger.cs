using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Phases {
        // Keep the full label area before deciding whether the detail can share its row.
        private List<(StyledLine Line, string Result, StatusKind Status)> LedgerLines(int width, RenderContext context) {
            var result = new List<(StyledLine, string, StatusKind)>();
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
                    result.Add((new StyledLine(spans), inline ? detail : string.Empty, phase.Status));
                }
                if (!inline && detail.Length > 0) {
                    foreach (StyledLine line in TextWrapper.Wrap(StyledLine.From(detail), labelWidth)) {
                        result.Add((StyledLine.Empty, line.PlainText, phase.Status));
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
            for (int y = 0; y < lines.Count && y < region.Height; y++) {
                var line = lines[y];
                region.Write(0, y, line.Line);
                region.Write(Math.Max(0, region.Width - DisplayWidth.Measure(line.Result)), y, line.Result, context.Style(line.Status));
            }
        }
    }
}
