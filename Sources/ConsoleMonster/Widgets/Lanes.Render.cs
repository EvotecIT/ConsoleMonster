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
            int countsWidth = 0;
            foreach (Lane lane in visible) {
                countsWidth = Math.Max(countsWidth, CountsWidth(lane, context));
            }
            int fixedWidth = 2 + labelWidth + 2 + 5 + 2 + countsWidth;
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
                DrawLane(region, i, visible[i], labelWidth, barWidth, countsWidth, detailWidth, context);
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

        private void DrawLane(Region region, int y, Lane lane, int labelWidth, int barWidth, int countsWidth, int detailWidth, RenderContext context) {
            IGlyphSet glyphs = context.Glyphs;
            Style muted = context.Style(ThemeToken.Muted);
            string marker;
            Style markerStyle;
            if (lane.Status == StatusKind.Running) {
                marker = glyphs.Tier == GlyphTier.Ascii ? glyphs.SpinnerFrames[(int)(context.Tick % glyphs.SpinnerFrames.Count)] : glyphs.SpinnerFrames[(int)((context.Tick + lane.Order) % glyphs.SpinnerFrames.Count)];
                markerStyle = context.Style(ThemeToken.Running);
            } else {
                marker = glyphs.Tier == GlyphTier.Ascii ? AsciiMarker(lane.Status) : glyphs.StatusMarker(lane.Status);
                markerStyle = context.Style(lane.Status);
            }
            region.Write(0, y, marker, markerStyle);
            Style labelStyle = lane.Status == StatusKind.Pending ? muted : context.Style(ThemeToken.Text);
            region.Write(2, y, TextWrapper.Truncate(lane.Label, labelWidth, context.Ellipsis), labelStyle);

            int x = 2 + labelWidth + 2;
            Style fill = lane.IsFinished ? context.Style(ThemeToken.Muted) : context.Style(ThemeToken.Accent);
            BarDrawer.Draw(region, x, y, barWidth, lane.Fraction, fill, context, true, BarShape.Line);
            x += barWidth + 1;
            string percent = lane.Status == StatusKind.Pending ? string.Empty : NumberFormat.Percent(lane.Fraction);
            region.Write(x + (4 - DisplayWidth.Measure(percent)), y, percent, muted);
            x += 4 + 2;
            DrawCounts(region, x, y, lane, context);
            x += countsWidth + 2;
            if (detailWidth > 0) {
                string detail;
                if (lane.IsFinished) {
                    bool timed = lane.Started.HasValue && lane.Finished.HasValue && lane.Finished.Value > lane.Started.Value;
                    detail = timed ? "done in " + NumberFormat.Duration(lane.Finished!.Value - lane.Started!.Value) : "done";
                } else if (lane.Status == StatusKind.Pending) {
                    detail = "queued";
                } else {
                    detail = lane.Detail ?? string.Empty;
                }
                region.Write(x, y, TextWrapper.Truncate(detail, detailWidth, context.Ellipsis), muted);
            }
        }

        private static string AsciiMarker(StatusKind status) {
            switch (status) {
                case StatusKind.Pass:
                    return "+";
                case StatusKind.Fail:
                    return "x";
                case StatusKind.Warn:
                    return "!";
                case StatusKind.Skip:
                    return "-";
                default:
                    return ".";
            }
        }

        private static int CountsWidth(Lane lane, RenderContext context) {
            if (lane.Status == StatusKind.Pending && lane.Done == 0) {
                return 0;
            }
            IGlyphSet g = context.Glyphs;
            bool ascii = g.Tier == GlyphTier.Ascii;
            return DisplayWidth.Measure((ascii ? "P" : g.Get(Glyph.Check)) + lane.Passed.ToString(CultureInfo.InvariantCulture))
                + 1 + DisplayWidth.Measure((ascii ? "F" : g.Get(Glyph.Cross)) + lane.Failed.ToString(CultureInfo.InvariantCulture))
                + 1 + DisplayWidth.Measure((ascii ? "W" : g.Get(Glyph.Warning)) + lane.Warned.ToString(CultureInfo.InvariantCulture));
        }

        private static void DrawCounts(Region region, int x, int y, Lane lane, RenderContext context) {
            if (lane.Status == StatusKind.Pending && lane.Done == 0) {
                return;
            }
            IGlyphSet g = context.Glyphs;
            bool ascii = g.Tier == GlyphTier.Ascii;
            Style muted = context.Style(ThemeToken.Muted);
            x = region.Write(x, y, (ascii ? "P" : g.Get(Glyph.Check)) + lane.Passed.ToString(CultureInfo.InvariantCulture), muted) + 1;
            x = region.Write(x, y, (ascii ? "F" : g.Get(Glyph.Cross)) + lane.Failed.ToString(CultureInfo.InvariantCulture), lane.Failed > 0 ? context.Style(ThemeToken.Fail) : muted) + 1;
            region.Write(x, y, (ascii ? "W" : g.Get(Glyph.Warning)) + lane.Warned.ToString(CultureInfo.InvariantCulture), lane.Warned > 0 ? context.Style(ThemeToken.Warn) : muted);
        }
    }
}
