using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
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

        // One row of the ledger: a phase row or one of its item rows, the right-aligned result and the phase it belongs to.
        private readonly struct LedgerLine {
            internal LedgerLine(StyledLine line, string result, StatusKind status, int phase) {
                Line = line; Result = result; Status = status; Phase = phase;
            }
            internal StyledLine Line { get; }
            internal string Result { get; }
            internal StatusKind Status { get; }
            internal int Phase { get; }
        }

        // Keep the full label area before deciding whether the detail can share its row. With collapse, a finished phase
        // folds its items into a "done/total" count and a running phase keeps only its running and problem items.
        private List<LedgerLine> LedgerLines(int width, RenderContext context, bool collapse) {
            var result = new List<LedgerLine>();
            if (width <= 0) return result;
            int digits = Math.Max(2, _phases.Count.ToString(CultureInfo.InvariantCulture).Length);
            for (int i = 0; i < _phases.Count; i++) {
                StatusItem phase = _phases[i];
                _phaseItems.TryGetValue(phase.Key, out List<StatusItem>? items);
                bool folded = collapse && items != null && items.Count > 0 && phase.Status != StatusKind.Running && phase.Status != StatusKind.Pending;
                string counter = "[" + (i + 1).ToString("D" + digits, CultureInfo.InvariantCulture) + "/" + _phases.Count.ToString("D" + digits, CultureInfo.InvariantCulture) + "] ";
                Style statusStyle = context.Style(phase.Status);
                StyledLine prefix = TextWrapper.Truncate(StyledLine.From(counter + Marker(phase.Status, context, i) + " ", statusStyle), Math.Max(0, width - 1), context.Ellipsis);
                int labelWidth = Math.Max(1, width - prefix.Width);
                StyledLine label = context.Markup(phase.Label).Lines[0].WithBaseStyle(context.Style(ThemeToken.Text));
                string detail = LedgerDetail(phase, context);
                if (folded) {
                    int done = items!.FindAll(item => item.Status != StatusKind.Pending && item.Status != StatusKind.Running).Count;
                    detail = done.ToString("N0", context.Culture) + "/" + items.Count.ToString("N0", context.Culture) + (phase.Detail == null ? string.Empty : " · " + detail);
                }
                bool inline = prefix.Width + label.Width + 1 + DisplayWidth.Measure(detail) <= width;
                IReadOnlyList<StyledLine> labels = inline ? new[] { label } : TextWrapper.Wrap(label, labelWidth);
                for (int j = 0; j < labels.Count; j++) {
                    var spans = new List<TextSpan> { new TextSpan(j == 0 ? prefix.PlainText : new string(' ', prefix.Width), statusStyle) };
                    spans.AddRange(labels[j]);
                    result.Add(new LedgerLine(new StyledLine(spans), inline ? detail : string.Empty, phase.Status, i));
                }
                if (!inline && detail.Length > 0) {
                    foreach (StyledLine line in TextWrapper.Wrap(StyledLine.From(detail), labelWidth)) {
                        result.Add(new LedgerLine(StyledLine.Empty, line.PlainText, phase.Status, i));
                    }
                }
                if (items == null || folded) { continue; }
                foreach (StatusItem item in items) {
                    if (collapse && item.Status != StatusKind.Running && item.Status != StatusKind.Fail && item.Status != StatusKind.Warn) { continue; }
                    var spans = new List<TextSpan> {
                        new TextSpan(new string(' ', Math.Min(prefix.Width, Math.Max(0, width - 4)))),
                        new TextSpan(context.Glyphs.Get(Glyph.TreeBranch), context.Style(ThemeToken.Subtle)),
                        new TextSpan(context.Glyphs.StatusMarker(item.Status) + " ", context.Style(item.Status))
                    };
                    spans.AddRange(context.Markup(item.Label).Lines[0].WithBaseStyle(context.Style(item.Status == StatusKind.Pending ? ThemeToken.Muted : ThemeToken.Text)));
                    string itemDetail = item.Detail ?? string.Empty;
                    StyledLine row = TextWrapper.Truncate(new StyledLine(spans), Math.Max(1, width - (itemDetail.Length > 0 ? DisplayWidth.Measure(itemDetail) + 1 : 0)), context.Ellipsis);
                    result.Add(new LedgerLine(row, itemDetail, item.Status, i));
                }
            }
            return result;
        }

        // A running phase spins (a still marker in ASCII) so the row shows it is alive.
        private static string Marker(StatusKind status, RenderContext context, int index) =>
            status == StatusKind.Running && context.Glyphs.Tier != GlyphTier.Ascii ? context.Spinner(index) : context.Glyphs.StatusMarker(status);

        // A running phase shows its latest detail and the time it has been running, from the session clock; a finished
        // one keeps the detail its state event gave it (by default the duration it took).
        private string LedgerDetail(StatusItem phase, RenderContext context) {
            if (phase.Status == StatusKind.Running && context.Elapsed > TimeSpan.Zero && _started.TryGetValue(phase.Key, out TimeSpan started)) {
                string elapsed = NumberFormat.Duration(context.Elapsed - started < TimeSpan.Zero ? TimeSpan.Zero : context.Elapsed - started);
                return phase.Detail == null ? elapsed : phase.Detail + " · " + elapsed;
            }
            return phase.Detail ?? phase.Status.ToWord(context);
        }

        private Size LedgerMeasure(Constraints constraints, RenderContext context) {
            int height = LedgerLines(constraints.MaxWidth, context, false).Count;
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? height : Math.Min(height, constraints.MaxHeight));
        }

        private void LedgerRender(Region region, RenderContext context) {
            if (region.Height <= 0) return;
            List<LedgerLine> lines = LedgerLines(region.Width, context, false);
            if (lines.Count > region.Height) {
                lines = LedgerLines(region.Width, context, true);
            }
            List<int> shown = new List<int>();
            int earlier = 0, more = 0, y = 0;
            if (lines.Count > region.Height) {
                LedgerWindow(lines, region.Height, shown, out earlier, out more);
            } else {
                for (int i = 0; i < lines.Count; i++) { shown.Add(i); }
            }
            if (earlier > 0) {
                region.Write(0, y++, Localization.WidgetLabels.Format(context.Labels.EarlierCount, earlier.ToString("N0", context.Culture)), context.Style(ThemeToken.Muted));
            }
            foreach (int index in shown) {
                if (y >= region.Height) break;
                LedgerLine line = lines[index];
                region.Write(0, y, line.Line);
                region.Write(Math.Max(0, region.Width - DisplayWidth.Measure(line.Result)), y, line.Result, context.Style(line.Status));
                y++;
            }
            if (more > 0 && y < region.Height) {
                region.Write(0, y, Localization.WidgetLabels.Format(context.Labels.MoreCount, more.ToString("N0", context.Culture)), context.Style(ThemeToken.Muted));
            }
        }

        // Whole phases around the frontier: the frontier phase, the next one, then earlier phases, then later ones, while
        // they fit with a "+N earlier" row above and a "+N more" row below for what is left out. Running phases above the
        // window stay pinned under "+N earlier", so every running row remains visible whatever finished rows do. Before
        // any phase has run the window starts at the top.
        private void LedgerWindow(List<LedgerLine> lines, int rows, List<int> shown, out int earlier, out int more) {
            int count = _phases.Count;
            int[] start = new int[count + 1];
            for (int i = 0, phase = 0; phase <= count; phase++) {
                while (i < lines.Count && lines[i].Phase < phase) { i++; }
                start[phase] = i;
            }
            int Lines(int phase) => start[phase + 1] - start[phase];
            int anchor = Math.Max(0, Math.Min(count - 1, _frontier));
            List<int> pinned = new List<int>();
            int top = anchor, bottom = anchor;
            for (int pass = 0; pass < 3; pass++) {
                int pinnedRows = 0;
                foreach (int phase in pinned) { pinnedRows += Lines(phase); }
                int Size(int a, int b) {
                    int hidden = a - pinned.FindAll(phase => phase < a).Count;
                    return start[b + 1] - start[a] + pinnedRows + (hidden > 0 ? 1 : 0) + (b < count - 1 ? 1 : 0);
                }
                top = anchor; bottom = anchor;
                if (bottom + 1 < count && Size(top, bottom + 1) <= rows) { bottom++; }
                while (top > 0 && Size(top - 1, bottom) <= rows) { top--; }
                while (bottom + 1 < count && Size(top, bottom + 1) <= rows) { bottom++; }
                List<int> next = new List<int>();
                for (int phase = 0; phase < top; phase++) {
                    if (_phases[phase].Status == StatusKind.Running) { next.Add(phase); }
                }
                if (next.Count == pinned.Count && next.TrueForAll(pinned.Contains)) { break; }
                pinned = next;
            }
            pinned.RemoveAll(phase => phase >= top);
            earlier = top - pinned.Count;
            more = count - 1 - bottom;
            int budget = rows - (earlier > 0 ? 1 : 0) - (more > 0 ? 1 : 0);
            foreach (int phase in pinned) {
                for (int i = start[phase]; i < start[phase + 1]; i++) { shown.Add(i); }
            }
            for (int i = start[top]; i < start[bottom + 1]; i++) { shown.Add(i); }
            if (shown.Count > budget) {
                // Too many rows even so: keep the frontier phase's rows first, then pinned running rows.
                List<int> keep = new List<int>();
                for (int i = start[anchor]; i < start[anchor + 1] && keep.Count < rows; i++) { keep.Add(i); }
                foreach (int index in shown) {
                    if (keep.Count >= rows) break;
                    if (!keep.Contains(index) && lines[index].Status == StatusKind.Running) { keep.Add(index); }
                }
                keep.Sort();
                shown.Clear();
                shown.AddRange(keep);
                earlier = more = 0;
            }
        }
    }
}
