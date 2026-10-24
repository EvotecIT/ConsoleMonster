using System;
using ConsoleMonster.Glyphs;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class RunDashboard {
        private static void Bar(Region region, RenderContext context, int done, int total) {
            double fraction = total > 0 ? Math.Min(1, (double)done / total) : 1;
            int filled = (int)Math.Round(region.Width * fraction);
            bool ascii = context.Glyphs.Tier == GlyphTier.Ascii;
            region.Write(0, 0, new string(ascii ? '=' : '━', filled), context.Style(ThemeToken.Accent));
            region.Write(filled, 0, new string(ascii ? '-' : '─', region.Width - filled), context.Style(ThemeToken.Muted));
        }
        private sealed class HeaderRow : IWidget {
            private readonly string _title;
            private readonly RunSnapshot _snapshot;
            internal HeaderRow(string title, RunSnapshot snapshot) { _title = title; _snapshot = snapshot; }
            public Size Measure(Constraints c, RenderContext context) => new Size(c.MaxWidth, Math.Min(1, c.MaxHeight));
            public void Render(Region region, RenderContext context) {
                string counts = (_snapshot.Total > 0 ? 100 * _snapshot.Done / _snapshot.Total : 100) + "%  " + _snapshot.Done + "/" + _snapshot.Total;
                int countWidth = DisplayWidth.Measure(counts);
                int titleWidth = Math.Min(DisplayWidth.Measure(_title), Math.Max(0, (region.Width - countWidth - 6) / 2));
                Label(_title).Render(region.Sub(0, 0, titleWidth, 1), context);
                int barWidth = Math.Max(0, region.Width - titleWidth - countWidth - 4);
                Bar(region.Sub(titleWidth + 2, 0, barWidth, 1), context, _snapshot.Done, _snapshot.Total);
                Line(counts).Render(region.Sub(Math.Max(0, region.Width - countWidth), 0, Math.Min(region.Width, countWidth), 1), context);
            }
        }
        private sealed class GroupRow : IWidget {
            private readonly string _name;
            private readonly RunItemSnapshot[] _items;
            internal GroupRow(string name, RunItemSnapshot[] items) { _name = name; _items = items; }
            public Size Measure(Constraints c, RenderContext context) => new Size(c.MaxWidth, Math.Min(1, c.MaxHeight));
            public void Render(Region region, RenderContext context) {
                int done = _items.Count(i => RunTracker.IsTerminal(i.State));
                int labelWidth = Math.Min(18, region.Width / 4);
                Line(_name, ThemeToken.Muted).Render(region.Sub(0, 0, labelWidth, 1), context);
                int barWidth = Math.Min(24, Math.Max(0, region.Width - labelWidth - 40));
                Bar(region.Sub(labelWidth + 1, 0, barWidth, 1), context, done, _items.Length);
                string states = " " + done + "/" + _items.Length;
                foreach (StatusKind state in new[] { StatusKind.Pass, StatusKind.Fail, StatusKind.Warn }) {
                    int count = _items.Count(i => i.State == state);
                    states += "  " + CountMarkup(state, count, context.Glyphs.StatusSymbol(state) + count);
                }
                new TextBlock(states).Render(region.Sub(labelWidth + barWidth + 2, 0, Math.Max(0, region.Width - labelWidth - barWidth - 2), 1), context);
            }
        }
        private sealed class ActiveRow : IWidget {
            private readonly RunItemSnapshot[] _items;
            private readonly string _spin;
            internal ActiveRow(RunItemSnapshot[] items, string spin) { _items = items; _spin = spin; }
            public Size Measure(Constraints c, RenderContext context) => new Size(c.MaxWidth, Math.Min(1, c.MaxHeight));
            public void Render(Region region, RenderContext context) {
                int width = region.Width / Math.Max(1, _items.Length);
                for (int i = 0; i < _items.Length; i++) {
                    RunItemSnapshot item = _items[i];
                    int left = i * width;
                    string duration = NumberFormat.Duration(item.Duration);
                    int detailWidth = Math.Max(0, width - DisplayWidth.Measure(duration) - 2);
                    string detail = "  " + _spin + " " + ItemName(item) + (item.Phase.Length > 0 ? " · " + item.Phase : "");
                    region.Write(left, 0, TextWrapper.Truncate(detail, detailWidth, context.Ellipsis), context.Style(ThemeToken.Text));
                    region.Write(left + Math.Max(0, width - duration.Length - 1), 0, duration, context.Style(ThemeToken.Muted));
                }
            }
        }
        private sealed class RateRow : IWidget {
            private readonly RunSnapshot _snapshot;
            private readonly string _text;
            private readonly bool _trend;
            internal RateRow(RunSnapshot snapshot, string text, bool trend) { _snapshot = snapshot; _text = text; _trend = trend; }
            public Size Measure(Constraints c, RenderContext context) => new Size(c.MaxWidth, Math.Min(1, c.MaxHeight));
            public void Render(Region region, RenderContext context) {
                int textWidth = DisplayWidth.Measure(_text);
                Line(_text, ThemeToken.Muted).Render(region, context);
                if (_trend && _snapshot.Rates.Count >= 5 && textWidth + 14 <= region.Width) {
                    double[] rates = _snapshot.Rates.Skip(Math.Max(0, _snapshot.Rates.Count - 12)).ToArray();
                    double min = rates.Min(), range = rates.Max() - min;
                    string ramp = context.Glyphs.Tier == GlyphTier.Ascii ? "._-=+*#" : "▁▂▃▄▅▆▇";
                    string trend = new string(rates.Select(rate => ramp[range < Math.Max(1e-9, Math.Abs(min) * 1e-6) ? 0 : (int)Math.Round(6 * (rate - min) / range)]).ToArray()).PadLeft(12);
                    region.Write(textWidth + 2, 0, trend, context.Style(ThemeToken.Muted));
                }
            }
        }
    }
}
