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
    public sealed partial class Lanes {
        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int rows = DesiredRows();
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? rows : Math.Min(rows, constraints.MaxHeight));
        }

        private bool AllFinished() {
            if (_lanes.Count == 0) {
                return false;
            }
            foreach (Lane lane in _lanes) {
                if (!lane.IsFinished) {
                    return false;
                }
            }
            return true;
        }

        // Rows the display wants: zero once a transient display is done, one when collapsed, otherwise a row per shown lane
        // plus the summary line for finished lanes that HideCompleted keeps out.
        private int DesiredRows() {
            if (_lanes.Count == 0) {
                return 1;
            }
            bool finished = AllFinished();
            if (finished && Transient) {
                return 0;
            }
            if (finished && CollapseWhenFinished) {
                return 1;
            }
            List<Lane> candidates = Candidates();
            int collapsed = Collapsed().Count;
            bool summary = candidates.Count + collapsed < _lanes.Count;
            return Math.Max(1, candidates.Count + (summary ? 1 : 0) + ExtraRows());
        }

        // Lanes on their own rows: not hidden by HideCompleted and not folded by CollapseWhen.
        private List<Lane> Candidates() {
            if (!HideCompleted && !CollapseWhen.HasValue) {
                return _lanes;
            }
            List<Lane> open = new List<Lane>();
            foreach (Lane lane in _lanes) {
                if ((!HideCompleted || !lane.IsFinished) && !(CollapseWhen.HasValue && lane.Status == CollapseWhen.Value)) {
                    open.Add(lane);
                }
            }
            return open;
        }

        private List<Lane> Collapsed() {
            List<Lane> folded = new List<Lane>();
            if (CollapseWhen.HasValue) {
                foreach (Lane lane in _lanes) {
                    if (lane.Status == CollapseWhen.Value) {
                        folded.Add(lane);
                    }
                }
            }
            return folded;
        }

        private int RunningCount() {
            int running = 0;
            foreach (Lane lane in _lanes) {
                if (lane.Status == StatusKind.Running) {
                    running++;
                }
            }
            return running;
        }

        // The rows after the lanes: the folded lanes' line and the running count.
        private int ExtraRows() => (Collapsed().Count > 0 ? 1 : 0) + (ShowRunningCount && RunningCount() > 0 ? 1 : 0);

        // "– Skipped: Alpha (46) · Beta (5)" and "● 3 running", from row y down.
        private void DrawExtras(Region region, int y, RenderContext context) {
            List<Lane> folded = Collapsed();
            if (folded.Count > 0 && y < region.Height) {
                StatusKind state = CollapseWhen!.Value;
                string marker = context.Glyphs.StatusSymbol(state);
                region.Write(0, y, marker, context.Style(state));
                List<string> names = new List<string>();
                foreach (Lane lane in folded) {
                    names.Add(lane.Label + (lane.Total > 0 ? " (" + lane.Total.ToString("N0", context.Culture) + ")" : string.Empty));
                }
                string text = CellMap.CountLabel(state, folded.Count, context) + ": " + string.Join(" " + context.Glyphs.Get(Glyph.Dot) + " ", names.ToArray());
                region.Write(2, y++, TextWrapper.Truncate(text, Math.Max(1, region.Width - 2), context.Ellipsis), context.Style(ThemeToken.Muted));
            }
            int running = RunningCount();
            if (ShowRunningCount && running > 0 && y < region.Height) {
                region.Write(0, y, context.Spinner() + " " + Localization.WidgetLabels.Format(context.Labels.RunningCount, running.ToString("N0", context.Culture)), context.Style(ThemeToken.Running));
            }
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Height <= 0 || region.Width <= 0) {
                return;
            }
            Style muted = context.Style(ThemeToken.Muted);
            Localization.WidgetLabels labels = context.Labels;
            if (_lanes.Count == 0) {
                region.Write(0, 0, labels.WaitingForWork + context.Ellipsis, muted);
                return;
            }
            bool finished = AllFinished();
            if (finished && Transient) {
                return;
            }
            if (finished && CollapseWhenFinished) {
                DrawCollapsed(region, context);
                return;
            }
            List<Lane> candidates = Candidates();
            int folded = Collapsed().Count;
            int extras = ExtraRows();
            bool reserved = candidates.Count + folded < _lanes.Count;
            List<Lane> visible = SelectVisible(candidates, region.Height - (reserved ? 1 : 0) - extras, reserved);
            int hidden = _lanes.Count - visible.Count - folded;
            int after = visible.Count + (hidden > 0 && visible.Count < region.Height ? 1 : 0);
            if (Columns != null && Columns.Count > 0) {
                RenderColumns(region, visible, context);
                if (hidden > 0) {
                    DrawMore(region, visible, hidden, context);
                }
                DrawExtras(region, after, context);
                return;
            }

            int labelWidth = 0;
            foreach (Lane lane in _lanes) {
                labelWidth = Math.Max(labelWidth, DisplayWidth.Measure(lane.Label));
            }
            labelWidth = Math.Min(labelWidth, Math.Min(MaxLabelWidth, Math.Max(6, region.Width / 3)));
            CountLayout counts = ResolveCounts(context);
            int percentWidth = _percentColumn.Resolve(4);
            const int MinimumBar = 4;
            // Narrow regions drop whole columns, lowest value first: the counts, then the percentage (the bar still shows it).
            bool showCounts = 2 + labelWidth + 2 + MinimumBar + 1 + percentWidth + 2 + counts.Width <= region.Width;
            bool showPercent = 2 + labelWidth + 2 + MinimumBar + 1 + percentWidth <= region.Width;
            if (!showCounts) {
                counts = CountLayout.Hidden;
            }
            if (!showPercent) {
                percentWidth = 0;
            }
            int fixedWidth = 2 + labelWidth + 2 + (percentWidth > 0 ? 1 + percentWidth : 0) + (counts.Width > 0 ? 2 + counts.Width : 0);
            int rest = region.Width - fixedWidth;
            int barWidth;
            int detailWidth;
            if (ShowDetail && rest >= 30) {
                barWidth = Math.Max(10, Math.Min(36, (int)(rest * 0.45)));
                detailWidth = rest - barWidth - 2;
            } else {
                barWidth = Math.Max(4, rest);
                detailWidth = 0;
            }

            for (int i = 0; i < visible.Count; i++) {
                DrawLane(region, i, visible[i], labelWidth, barWidth, percentWidth, counts, detailWidth, context);
            }
            if (hidden > 0) {
                DrawMore(region, visible, hidden, context);
            }
            DrawExtras(region, after, context);
        }

        // "+3 more · 2 done · 1 queued" for the lanes that are not shown.
        private void DrawMore(Region region, List<Lane> visible, int hidden, RenderContext context) {
            if (visible.Count >= region.Height) {
                return;
            }
            Localization.WidgetLabels labels = context.Labels;
            int done = 0;
            int queued = 0;
            foreach (Lane lane in _lanes) {
                if (visible.Contains(lane) || (CollapseWhen.HasValue && lane.Status == CollapseWhen.Value)) {
                    continue;
                }
                if (lane.IsFinished) {
                    done++;
                } else {
                    queued++;
                }
            }
            string dot = " " + context.Glyphs.Get(Glyph.Dot) + " ";
            List<string> parts = new List<string>();
            if (hidden > done || !HideCompleted) {
                parts.Add(Localization.WidgetLabels.Format(labels.MoreCount, hidden.ToString(CultureInfo.InvariantCulture)));
            }
            parts.Add(Localization.WidgetLabels.Format(labels.DoneCount, done.ToString(CultureInfo.InvariantCulture)));
            if (queued > 0 || !HideCompleted) {
                parts.Add(Localization.WidgetLabels.Format(labels.QueuedCount, queued.ToString(CultureInfo.InvariantCulture)));
            }
            region.Write(2, visible.Count, string.Join(dot, parts.ToArray()), context.Style(ThemeToken.Muted));
        }

        // Every lane finished and CollapseWhenFinished: "✓ 12 done in 1m 05s · 1 failed".
        private void DrawCollapsed(Region region, RenderContext context) {
            Localization.WidgetLabels labels = context.Labels;
            int failed = 0;
            TimeSpan? first = null;
            TimeSpan? last = null;
            foreach (Lane lane in _lanes) {
                if (lane.Status == StatusKind.Fail) {
                    failed++;
                }
                if (lane.Started.HasValue && (!first.HasValue || lane.Started.Value < first.Value)) {
                    first = lane.Started;
                }
                if (lane.Finished.HasValue && (!last.HasValue || lane.Finished.Value > last.Value)) {
                    last = lane.Finished;
                }
            }
            StatusKind overall = failed > 0 ? StatusKind.Fail : StatusKind.Pass;
            string marker = context.Glyphs.Tier == GlyphTier.Ascii ? context.Glyphs.StatusSymbol(overall) : context.Glyphs.StatusMarker(overall);
            region.Write(0, 0, marker, context.Style(overall));
            string count = _lanes.Count.ToString(CultureInfo.InvariantCulture);
            string text = first.HasValue && last.HasValue
                ? Localization.WidgetLabels.Format(labels.AllDoneIn, count, NumberFormat.Duration(last.Value - first.Value))
                : Localization.WidgetLabels.Format(labels.DoneCount, count);
            if (failed > 0) {
                text += " " + context.Glyphs.Get(Glyph.Dot) + " " + Localization.WidgetLabels.Format(labels.FailedCount, failed.ToString(CultureInfo.InvariantCulture));
            }
            region.Write(2, 0, TextWrapper.Truncate(text, Math.Max(1, region.Width - 2), context.Ellipsis), context.Style(ThemeToken.Muted));
        }

        private void RenderColumns(Region region, List<Lane> visible, RenderContext context) {
            List<ProgressRow> all = new List<ProgressRow>(_lanes.Count);
            Dictionary<Lane, ProgressRow> byLane = new Dictionary<Lane, ProgressRow>();
            foreach (Lane lane in _lanes) {
                ProgressRow row = RowFor(lane, context.Elapsed);
                all.Add(row);
                byLane[lane] = row;
            }
            IList<ProgressColumn> columns = Columns!;
            int[] widths = ProgressLayout.Arrange(columns, all, region.Width, context);
            for (int i = 0; i < visible.Count && i < region.Height; i++) {
                ProgressLayout.Draw(region, i, columns, widths, byLane[visible[i]], context);
            }
        }

        // The running lanes through the columns, for the stream heartbeat line: "; srv01 45% 12/30 6s left".
        private string RunningSummary() {
            RenderContext context = new RenderContext(TerminalProfile.Fixed(200, 1, ColorDepth.None, GlyphTier.Ascii, OutputMode.Stream), Theme.Dark, null, 0, _lastEventTime);
            List<string> parts = new List<string>();
            foreach (Lane lane in _lanes) {
                if (lane.Status == StatusKind.Running && parts.Count < 3) {
                    parts.Add(ProgressLayout.Summarize(Columns!, RowFor(lane, _lastEventTime), context));
                }
            }
            return parts.Count == 0 ? string.Empty : "; " + string.Join("; ", parts.ToArray());
        }

        private ProgressRow RowFor(Lane lane, TimeSpan now) {
            lane.Meter.Window = RateWindow;
            double rate = lane.IsFinished ? 0 : lane.Meter.Rate(now);
            return new ProgressRow(lane.Key, lane.Label, lane.LabelText, lane.Detail, lane.DetailText, lane.Status, lane.Value, lane.Total,
                lane.Passed, lane.Failed, lane.Warned, lane.Skipped, lane.Started, lane.Finished, now, rate, lane.Order);
        }

        // Running lanes first, then the most recently finished, then queued ones; a row is kept for the summary line unless
        // the caller reserved it already.
        private static List<Lane> SelectVisible(List<Lane> lanes, int height, bool summaryReserved) {
            height = Math.Max(1, height);
            if (lanes.Count <= height) {
                return lanes;
            }
            int slots = Math.Max(0, summaryReserved ? height : height - 1);
            List<Lane> chosen = new List<Lane>();
            foreach (Lane lane in lanes) {
                if (chosen.Count < slots && lane.Status == StatusKind.Running) {
                    chosen.Add(lane);
                }
            }
            List<Lane> finished = new List<Lane>();
            foreach (Lane lane in lanes) {
                if (lane.IsFinished) {
                    finished.Add(lane);
                }
            }
            finished.Sort((a, b) => (b.Finished ?? TimeSpan.Zero).CompareTo(a.Finished ?? TimeSpan.Zero));
            foreach (Lane lane in finished) {
                if (chosen.Count < slots) {
                    chosen.Add(lane);
                }
            }
            foreach (Lane lane in lanes) {
                if (chosen.Count < slots && !lane.IsFinished && lane.Status != StatusKind.Running) {
                    chosen.Add(lane);
                }
            }
            chosen.Sort((a, b) => a.Order.CompareTo(b.Order));
            return chosen;
        }

        private void DrawLane(Region region, int y, Lane lane, int labelWidth, int barWidth, int percentWidth, CountLayout counts, int detailWidth, RenderContext context) {
            IGlyphSet glyphs = context.Glyphs;
            Style muted = context.Style(ThemeToken.Muted);
            string marker;
            Style markerStyle;
            // A running lane whose work is all done reads as passed: a check mark, not a spinner.
            StatusKind shown = lane.Status == StatusKind.Running && lane.Total > 0 && lane.Value >= lane.Total ? StatusKind.Pass : lane.Status;
            if (shown == StatusKind.Running) {
                marker = context.Spinner(glyphs.Tier == GlyphTier.Ascii ? 0 : lane.Order);
                markerStyle = context.Style(ThemeToken.Running);
            } else {
                marker = glyphs.Tier == GlyphTier.Ascii ? glyphs.StatusSymbol(shown) : glyphs.StatusMarker(shown);
                markerStyle = context.Style(shown);
            }
            region.Write(0, y, marker, markerStyle);
            Style labelStyle = lane.Status == StatusKind.Pending && DimPending ? muted : context.Style(ThemeToken.Text);
            if (BoldActive && lane.Status == StatusKind.Running) {
                labelStyle = labelStyle.With(TextDecoration.Bold);
            }
            region.Write(2, y, TextWrapper.Truncate(RichLines.Line(lane.LabelText, lane.Label, labelStyle, context), labelWidth, context.Ellipsis));

            int x = 2 + labelWidth + 2;
            Style fill = lane.IsFinished ? context.Style(ThemeToken.Muted) : context.Style(ThemeToken.Accent);
            BarDrawer.Draw(region, x, y, barWidth, lane.Fraction, fill, context, true, Shape);
            x += barWidth + 1;
            if (percentWidth > 0) {
                string percent = lane.Status == StatusKind.Pending ? string.Empty : NumberFormat.Percent(lane.Fraction);
                StatColumn.Write(region, x, y, percentWidth, percent, muted, PercentColumn.Alignment, context.Ellipsis);
                x += percentWidth + 2;
            } else {
                x += 1;
            }
            if (counts.Width > 0) {
                DrawCounts(region, x, y, lane, counts, context);
                x += counts.Width + 2;
            }
            if (detailWidth > 0) {
                string detail;
                RichText? richDetail = null;
                if (lane.IsFinished) {
                    bool timed = lane.Started.HasValue && lane.Finished.HasValue && lane.Finished.Value > lane.Started.Value;
                    detail = timed ? Localization.WidgetLabels.Format(context.Labels.DoneIn, NumberFormat.Duration(lane.Finished!.Value - lane.Started!.Value)) : context.Labels.Done;
                } else if (lane.Status == StatusKind.Pending) {
                    detail = context.Labels.Queued;
                } else {
                    detail = lane.Detail ?? string.Empty;
                    richDetail = lane.DetailText;
                }
                region.Write(x, y, TextWrapper.Truncate(RichLines.Line(richDetail, detail, muted, context), detailWidth, context.Ellipsis));
            }
        }

        private CountLayout ResolveCounts(RenderContext context) {
            IGlyphSet g = context.Glyphs;
            bool ascii = g.Tier == GlyphTier.Ascii;
            string pass = ascii ? "P" : g.Get(Glyph.Check);
            string fail = ascii ? "F" : g.Get(Glyph.Cross);
            string warn = CountSkipped ? (ascii ? "S" : g.Get(Glyph.Skip)) : ascii ? "W" : g.Get(Glyph.Warning);
            int passed = 0;
            int failed = 0;
            int warned = 0;
            double total = 0;
            foreach (Lane lane in _lanes) {
                passed = Math.Max(passed, lane.Passed);
                failed = Math.Max(failed, lane.Failed);
                warned = Math.Max(warned, CountSkipped ? lane.Skipped : lane.Warned);
                total = Math.Max(total, lane.Total);
            }
            // A lane can never count more steps than its total, so a known total reserves every column from the first frame.
            int totalDigits = total > 0 ? Digits((int)Math.Min(int.MaxValue, Math.Ceiling(total))) : 0;
            return new CountLayout(
                pass, Resolve(_passColumn, pass, passed, totalDigits),
                fail, Resolve(_failColumn, fail, failed, totalDigits),
                warn, Resolve(_warnColumn, warn, warned, totalDigits));
        }

        private static int Resolve(StatColumn column, string symbol, int widest, int totalDigits) {
            int symbolWidth = DisplayWidth.Measure(symbol);
            return column.Resolve(symbolWidth + Digits(widest), symbolWidth, totalDigits > 0 ? symbolWidth + totalDigits : 0);
        }

        private static int Digits(int value) => value.ToString(CultureInfo.InvariantCulture).Length;

        private void DrawCounts(Region region, int x, int y, Lane lane, CountLayout counts, RenderContext context) {
            if (lane.Status == StatusKind.Pending && lane.Value == 0) {
                return;
            }
            Style muted = context.Style(ThemeToken.Muted);
            x = DrawCount(region, x, y, counts.PassSymbol, counts.PassWidth, lane.Passed, muted, muted);
            x = DrawCount(region, x + 1, y, counts.FailSymbol, counts.FailWidth, lane.Failed, lane.Failed > 0 ? context.Style(ThemeToken.Fail) : muted, muted);
            int third = CountSkipped ? lane.Skipped : lane.Warned;
            DrawCount(region, x + 1, y, counts.WarnSymbol, counts.WarnWidth, third, third > 0 ? context.Style(CountSkipped ? ThemeToken.Skip : ThemeToken.Warn) : muted, muted);
        }

        private int DrawCount(Region region, int x, int y, string symbol, int width, int value, Style style, Style muted) {
            int symbolWidth = DisplayWidth.Measure(symbol);
            region.Write(x, y, symbol, style, width);
            StatColumn.Write(region, x + symbolWidth, y, width - symbolWidth, value.ToString(CultureInfo.InvariantCulture), style, CountColumn.Alignment);
            return x + width;
        }

        /// <summary>The resolved count columns for one frame: symbol and total width per column.</summary>
        private readonly struct CountLayout {
            internal CountLayout(string passSymbol, int passWidth, string failSymbol, int failWidth, string warnSymbol, int warnWidth) {
                PassSymbol = passSymbol;
                PassWidth = passWidth;
                FailSymbol = failSymbol;
                FailWidth = failWidth;
                WarnSymbol = warnSymbol;
                WarnWidth = warnWidth;
            }

            internal string PassSymbol { get; }

            internal int PassWidth { get; }

            internal string FailSymbol { get; }

            internal int FailWidth { get; }

            internal string WarnSymbol { get; }

            internal int WarnWidth { get; }

            internal int Width => PassWidth == 0 ? 0 : PassWidth + 1 + FailWidth + 1 + WarnWidth;

            internal static CountLayout Hidden => new CountLayout(string.Empty, 0, string.Empty, 0, string.Empty, 0);
        }    }
}
