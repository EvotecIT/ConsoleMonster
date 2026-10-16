using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// One bar split into proportional segments with a legend (label, value, share). Segments labelled with a status word
    /// (Passed, Failed, Warnings, Skipped...) take that status colour unless a token is given. Without colour, segments use distinct
    /// fill glyphs so the split stays readable. Live operations (Key = segment): <c>set</c> (Value), <c>add</c> (Value).
    /// </summary>
    public sealed class StackedBar : LiveWidget {
        private static readonly string[] AsciiFills = { "#", "=", "+", "-", ".", ":" };
        private readonly List<ChartItem> _segments = new List<ChartItem>();

        /// <summary>Creates a stacked bar.</summary>
        /// <param name="name">The live name (default <c>stacked</c>).</param>
        public StackedBar(string name = "stacked") : base(name) {
        }

        /// <summary>The segments.</summary>
        public IReadOnlyList<ChartItem> Segments => _segments;

        /// <summary>Whether the legend line is shown (default true).</summary>
        public bool ShowLegend { get; set; } = true;

        /// <summary>Adds or updates a segment.</summary>
        /// <param name="label">The label.</param>
        /// <param name="value">The value.</param>
        /// <param name="token">The colour token, or <c>null</c> for the next series colour.</param>
        /// <returns>This instance.</returns>
        public StackedBar Add(string label, double value, ThemeToken? token = null) {
            foreach (ChartItem segment in _segments) {
                if (string.Equals(segment.Label, label, StringComparison.Ordinal)) {
                    segment.Value = value;
                    if (token.HasValue) {
                        segment.Token = token;
                    }
                    return this;
                }
            }
            ThemeToken automatic = StatusKinds.TryParse(label, out StatusKind status) ? status.ToToken() : (ThemeToken)((int)ThemeToken.Series1 + (_segments.Count % 6));
            _segments.Add(new ChartItem(label, value, token ?? automatic));
            return this;
        }

        /// <inheritdoc />
        public override void Apply(LiveEvent liveEvent) {
            if (liveEvent.Key == null) {
                return;
            }
            if (liveEvent.Operation == "set") {
                Add(liveEvent.Key, liveEvent.Value);
            } else if (liveEvent.Operation == "add") {
                double current = 0;
                foreach (ChartItem segment in _segments) {
                    if (segment.Label == liveEvent.Key) {
                        current = segment.Value;
                    }
                }
                Add(liveEvent.Key, current + (liveEvent.Value == 0 ? 1 : liveEvent.Value));
            }
        }

        /// <inheritdoc />
        public override Size Measure(Constraints constraints, RenderContext context) {
            int lines = 1 + (ShowLegend ? LegendLines(constraints.MaxWidth, context).Count : 0);
            return new Size(constraints.MaxWidth, constraints.IsHeightUnbounded ? lines : Math.Min(lines, constraints.MaxHeight));
        }

        /// <inheritdoc />
        public override void Render(Region region, RenderContext context) {
            if (region.Height <= 0 || region.Width <= 0) {
                return;
            }
            int[] widths = SegmentWidths(region.Width);
            int x = 0;
            for (int i = 0; i < _segments.Count; i++) {
                string fill = FillGlyph(i, context);
                region.HorizontalLine(x, 0, widths[i], fill, context.Style(_segments[i].Token ?? ThemeToken.Series1));
                x += widths[i];
            }
            if (x < region.Width) {
                region.HorizontalLine(x, 0, region.Width - x, context.Glyphs.Get(Glyph.BarTrack), context.Style(ThemeToken.Subtle));
            }
            if (!ShowLegend) {
                return;
            }
            List<StyledLine> legend = LegendLines(region.Width, context);
            for (int i = 0; i < legend.Count && i + 1 < region.Height; i++) {
                region.Write(0, i + 1, legend[i]);
            }
        }

        private string FillGlyph(int index, RenderContext context) {
            IGlyphSet glyphs = context.Glyphs;
            if (glyphs.Tier == GlyphTier.Ascii) {
                return AsciiFills[index % AsciiFills.Length];
            }
            if (context.HasColor) {
                return glyphs.Get(Glyph.BlockFull);
            }
            Glyph[] shades = { Glyph.BlockFull, Glyph.ShadeDark, Glyph.ShadeMedium, Glyph.ShadeLight };
            return glyphs.Get(shades[index % shades.Length]);
        }

        private int[] SegmentWidths(int width) {
            double total = 0;
            foreach (ChartItem segment in _segments) {
                total += Math.Max(0, segment.Value);
            }
            int[] widths = new int[_segments.Count];
            if (total <= 0) {
                return widths;
            }
            double[] remainders = new double[_segments.Count];
            int used = 0;
            for (int i = 0; i < _segments.Count; i++) {
                double exact = Math.Max(0, _segments[i].Value) / total * width;
                widths[i] = (int)Math.Floor(exact);
                if (widths[i] == 0 && _segments[i].Value > 0) {
                    widths[i] = 1;
                }
                remainders[i] = exact - Math.Floor(exact);
                used += widths[i];
            }
            while (used < width) {
                int best = -1;
                for (int i = 0; i < remainders.Length; i++) {
                    if (_segments[i].Value > 0 && (best < 0 || remainders[i] > remainders[best])) {
                        best = i;
                    }
                }
                if (best < 0) {
                    break;
                }
                widths[best]++;
                remainders[best] = -1;
                used++;
            }
            while (used > width) {
                int widest = 0;
                for (int i = 1; i < widths.Length; i++) {
                    if (widths[i] > widths[widest]) {
                        widest = i;
                    }
                }
                widths[widest]--;
                used--;
            }
            return widths;
        }

        private List<StyledLine> LegendLines(int width, RenderContext context) {
            double total = 0;
            foreach (ChartItem segment in _segments) {
                total += Math.Max(0, segment.Value);
            }
            List<StyledLine> lines = new List<StyledLine>();
            List<TextSpan> current = new List<TextSpan>();
            int used = 0;
            for (int i = 0; i < _segments.Count; i++) {
                ChartItem segment = _segments[i];
                string marker = FillGlyph(i, context);
                string text = segment.Label + " " + NumberFormat.Compact(segment.Value);
                string share = total > 0 ? " " + NumberFormat.Percent(segment.Value / total) : string.Empty;
                int chipWidth = DisplayWidth.Measure(marker) + 1 + DisplayWidth.Measure(text) + DisplayWidth.Measure(share);
                if (current.Count > 0 && used + 3 + chipWidth > width) {
                    lines.Add(new StyledLine(current));
                    current = new List<TextSpan>();
                    used = 0;
                }
                if (current.Count > 0) {
                    current.Add(new TextSpan("   "));
                    used += 3;
                }
                current.Add(new TextSpan(marker, context.Style(segment.Token ?? ThemeToken.Series1)));
                current.Add(new TextSpan(" " + text, context.Style(ThemeToken.Text)));
                current.Add(new TextSpan(share, context.Style(ThemeToken.Muted)));
                used += chipWidth;
            }
            if (current.Count > 0) {
                lines.Add(new StyledLine(current));
            }
            return lines;
        }
    }
}
