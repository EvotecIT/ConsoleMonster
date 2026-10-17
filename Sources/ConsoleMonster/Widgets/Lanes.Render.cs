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
            int rows = Math.Max(1, _lanes.Count);
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? rows : Math.Min(rows, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Height <= 0 || region.Width <= 0) {
                return;
            }
            Style muted = context.Style(ThemeToken.Muted);
            if (_lanes.Count == 0) {
                region.Write(0, 0, "waiting for work" + context.Ellipsis, muted);
                return;
            }
            List<Lane> visible = SelectVisible(region.Height);
            int hidden = _lanes.Count - visible.Count;

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
                int done = 0;
                int queued = 0;
                foreach (Lane lane in _lanes) {
                    if (visible.Contains(lane)) {
                        continue;
                    }
                    if (lane.IsFinished) {
                        done++;
                    } else {
                        queued++;
                    }
                }
                string more = "+" + hidden.ToString(CultureInfo.InvariantCulture) + " more " + context.Glyphs.Get(Glyph.Dot) + " "
                    + done.ToString(CultureInfo.InvariantCulture) + " done " + context.Glyphs.Get(Glyph.Dot) + " "
                    + queued.ToString(CultureInfo.InvariantCulture) + " queued";
                region.Write(2, visible.Count, more, muted);
            }
        }

        private List<Lane> SelectVisible(int height) {
            if (_lanes.Count <= height) {
                return _lanes;
            }
            int slots = Math.Max(0, height - 1);
            List<Lane> chosen = new List<Lane>();
            foreach (Lane lane in _lanes) {
                if (chosen.Count < slots && lane.Status == StatusKind.Running) {
                    chosen.Add(lane);
                }
            }
            List<Lane> finished = new List<Lane>();
            foreach (Lane lane in _lanes) {
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
            foreach (Lane lane in _lanes) {
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
            if (lane.Status == StatusKind.Running) {
                marker = context.Spinner(glyphs.Tier == GlyphTier.Ascii ? 0 : lane.Order);
                markerStyle = context.Style(ThemeToken.Running);
            } else {
                marker = glyphs.Tier == GlyphTier.Ascii ? glyphs.StatusSymbol(lane.Status) : glyphs.StatusMarker(lane.Status);
                markerStyle = context.Style(lane.Status);
            }
            region.Write(0, y, marker, markerStyle);
            Style labelStyle = lane.Status == StatusKind.Pending ? muted : context.Style(ThemeToken.Text);
            region.Write(2, y, TextWrapper.Truncate(RichLines.Line(lane.LabelText, lane.Label, labelStyle, context), labelWidth, context.Ellipsis));

            int x = 2 + labelWidth + 2;
            Style fill = lane.IsFinished ? context.Style(ThemeToken.Muted) : context.Style(ThemeToken.Accent);
            BarDrawer.Draw(region, x, y, barWidth, lane.Fraction, fill, context, true, Shape);
            x += barWidth + 1;
            if (percentWidth > 0) {
                string percent = lane.Status == StatusKind.Pending ? string.Empty : NumberFormat.Percent(lane.Fraction);
                StatColumn.Write(region, x, y, percentWidth, percent, muted, PercentColumn.Alignment);
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
                    detail = timed ? "done in " + NumberFormat.Duration(lane.Finished!.Value - lane.Started!.Value) : "done";
                } else if (lane.Status == StatusKind.Pending) {
                    detail = "queued";
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
            if (lane.Status == StatusKind.Pending && lane.Done == 0) {
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
