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
        // or item folds its children into a "done/total" count and a running one keeps only its running and problem children.
        private List<LedgerLine> LedgerLines(int width, RenderContext context, bool collapse) {
            var result = new List<LedgerLine>();
            if (width <= 0) return result;
            int digits = Math.Max(2, _phases.Count.ToString(CultureInfo.InvariantCulture).Length);
            for (int i = 0; i < _phases.Count; i++) {
                StatusItem phase = _phases[i];
                _phaseItems.TryGetValue(phase.Key, out List<StatusItem>? items);
                bool folded = collapse && items != null && items.Count > 0 && IsFinished(phase.Status);
                string counter = "[" + (i + 1).ToString("D" + digits, CultureInfo.InvariantCulture) + "/" + _phases.Count.ToString("D" + digits, CultureInfo.InvariantCulture) + "] ";
                Style statusStyle = context.Style(phase.Status);
                StyledLine prefix = TextWrapper.Truncate(StyledLine.From(counter + Marker(phase.Status, context, i) + " ", statusStyle), Math.Max(0, width - 1), context.Ellipsis);
                int labelWidth = Math.Max(1, width - prefix.Width);
                StyledLine label = context.Markup(phase.Label).Lines[0].WithBaseStyle(context.Style(ThemeToken.Text));
                string detail = LedgerDetail(phase, phase.Key, context) ?? phase.Status.ToWord(context);
                if (folded) {
                    detail = Folded(items!, context) + (phase.Detail == null ? string.Empty : Separator(context) + detail);
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
                ItemLines(result, items, phase.Key, Math.Min(prefix.Width, Math.Max(0, width - 4)), width, i, context, collapse);
            }
            return result;
        }

        // Item rows hang under their phase, sub-item rows one tree level further in. A finished item with sub-items stays
        // as one folded row when collapsing; other finished rows give way to running and problem rows.
        private void ItemLines(List<LedgerLine> result, List<StatusItem> items, string owner, int indent, int width, int phase, RenderContext context, bool collapse) {
            string branch = context.Glyphs.Get(Glyph.TreeBranch);
            foreach (StatusItem item in items) {
                string path = owner + ItemSeparator + item.Key;
                _phaseItems.TryGetValue(path, out List<StatusItem>? children);
                bool hasChildren = children != null && children.Count > 0;
                bool folded = collapse && hasChildren && IsFinished(item.Status);
                if (collapse && !folded && item.Status != StatusKind.Running && item.Status != StatusKind.Fail && item.Status != StatusKind.Warn) { continue; }
                var spans = new List<TextSpan> {
                    new TextSpan(new string(' ', indent)),
                    new TextSpan(branch, context.Style(ThemeToken.Subtle)),
                    new TextSpan(context.Glyphs.StatusMarker(item.Status) + " ", context.Style(item.Status))
                };
                StyledLine label = context.Markup(item.Label).Lines[0].WithBaseStyle(context.Style(item.Status == StatusKind.Pending ? ThemeToken.Muted : ThemeToken.Text));
                spans.AddRange(label);
                string itemDetail = LedgerDetail(item, path, context) ?? string.Empty;
                if (folded) {
                    itemDetail = Folded(children!, context) + (itemDetail.Length == 0 ? string.Empty : Separator(context) + itemDetail);
                }
                StyledLine full = new StyledLine(spans);
                int room = width - (itemDetail.Length > 0 ? DisplayWidth.Measure(itemDetail) + 1 : 0);
                if (itemDetail.Length == 0 || room - (full.Width - label.Width) >= Math.Min(label.Width, MinimumItemLabel)) {
                    result.Add(new LedgerLine(TextWrapper.Truncate(full, Math.Max(1, room), context.Ellipsis), itemDetail, item.Status, phase));
                } else {
                    // The result would leave the label unreadable: it goes on the rows below, as a phase's detail does.
                    result.Add(new LedgerLine(TextWrapper.Truncate(full, width, context.Ellipsis), string.Empty, item.Status, phase));
                    foreach (StyledLine line in TextWrapper.Wrap(StyledLine.From(itemDetail), Math.Max(1, width - indent))) {
                        result.Add(new LedgerLine(StyledLine.Empty, line.PlainText, item.Status, phase));
                    }
                }
                if (hasChildren && !folded && owner.IndexOf(ItemSeparator) < 0) {
                    ItemLines(result, children!, path, Math.Min(indent + DisplayWidth.Measure(branch), Math.Max(0, width - 4)), width, phase, context, collapse);
                }
            }
        }

        // The fewest label cells an item row keeps beside its result before the result moves to the row below.
        private const int MinimumItemLabel = 8;

        private static bool IsFinished(StatusKind status) => status != StatusKind.Running && status != StatusKind.Pending;

        // "done/total" for a folded group.
        private static string Folded(List<StatusItem> items, RenderContext context) {
            int done = items.FindAll(item => IsFinished(item.Status)).Count;
            return done.ToString("N0", context.Culture) + "/" + items.Count.ToString("N0", context.Culture);
        }

        // The separator between a detail and its time: a middle dot, or a hyphen on the ASCII tier.
        private static string Separator(RenderContext context) => " " + context.Glyphs.Get(Glyph.Dot) + " ";

        // A running phase spins (a still marker in ASCII) so the row shows it is alive.
        private static string Marker(StatusKind status, RenderContext context, int index) =>
            status == StatusKind.Running && context.Glyphs.Tier != GlyphTier.Ascii ? context.Spinner(index) : context.Glyphs.StatusMarker(status);

        // A running row shows its latest detail and the time it has been running, from the session clock; a finished one
        // keeps its detail followed by the time it took (from its state events), or the detail alone. Null when it has neither.
        private string? LedgerDetail(StatusItem row, string path, RenderContext context) {
            string? time = null;
            if (row.Status == StatusKind.Running && context.Elapsed > TimeSpan.Zero && _started.TryGetValue(path, out TimeSpan started)) {
                time = NumberFormat.Duration(context.Elapsed - started < TimeSpan.Zero ? TimeSpan.Zero : context.Elapsed - started);
            } else if (IsFinished(row.Status) && _took.TryGetValue(path, out TimeSpan took)) {
                time = NumberFormat.Duration(took);
            }
            if (time == null) return row.Detail;
            return string.IsNullOrEmpty(row.Detail) ? time : row.Detail + Separator(context) + time;
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
                // Too many rows even so: keep the frontier phase's rows first, then pinned running rows. A frontier phase
                // taller than the area gives its last row to the next phase's row, so what comes next stays in view.
                List<int> keep = new List<int>();
                bool next = anchor + 1 < count && rows > 1 && Lines(anchor) >= rows;
                for (int i = start[anchor]; i < start[anchor + 1] && keep.Count < (next ? rows - 1 : rows); i++) { keep.Add(i); }
                if (next) { keep.Add(start[anchor + 1]); }
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
