using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Phases {
        private List<(StyledLine Line, string Result, StatusKind Status, bool First)> LedgerLines(int width, RenderContext context) {
            var result = new List<(StyledLine, string, StatusKind, bool)>();
            int digits = Math.Max(2, _phases.Count.ToString(CultureInfo.InvariantCulture).Length);
            int resultWidth = Math.Min(Math.Max(0, width / 3), _phases.Select(p => DisplayWidth.Measure(p.Detail ?? p.Status.ToWord(context))).DefaultIfEmpty(0).Max());
            for (int i = 0; i < _phases.Count; i++) {
                StatusItem phase = _phases[i];
                string prefix = "[" + (i + 1).ToString("D" + digits, CultureInfo.InvariantCulture) + "/" + _phases.Count.ToString("D" + digits, CultureInfo.InvariantCulture) + "] ";
                var spans = new List<TextSpan> { new TextSpan(prefix + context.Glyphs.StatusMarker(phase.Status) + " ", context.Style(phase.Status)) };
                spans.AddRange(context.Markup(phase.Label).Lines[0].WithBaseStyle(context.Style(ThemeToken.Text)));
                var full = new StyledLine(spans);
                var wrapped = TextWrapper.Wrap(full, Math.Max(1, width - resultWidth - 1));
                bool first = true;
                foreach (StyledLine line in wrapped) {
                    result.Add((line, first ? TextWrapper.Truncate(phase.Detail ?? phase.Status.ToWord(context), resultWidth, context.Ellipsis) : string.Empty, phase.Status, first));
                    first = false;
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
