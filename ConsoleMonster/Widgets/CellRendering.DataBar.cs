using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    internal static partial class CellRendering {
        internal static void DrawDataBar(Region region, int x, int y, int cells, double fraction, Style style, RenderContext context) {
            bool consoleView = ConsoleViewStyle.IsApplied(context);
            region.HorizontalLine(x, y, cells, context.Glyphs.Get(Glyph.ShadeLight), context.Style(consoleView ? ThemeToken.Subtle : ThemeToken.Muted));
            double amount = cells * (double.IsNaN(fraction) ? 0 : Math.Max(0, Math.Min(1, fraction)));
            int eighths = (int)Math.Round(amount * 8, MidpointRounding.AwayFromZero);
            if (consoleView) {
                region.HorizontalLine(x, y, (int)Math.Round(amount, MidpointRounding.AwayFromZero), context.Glyphs.Get(Glyph.BlockFull), style);
                return;
            }
            int whole = eighths / 8;
            region.HorizontalLine(x, y, whole, context.Glyphs.Get(Glyph.BlockFull), style);
            string partial = context.Glyphs.HorizontalFill(eighths % 8);
            if (partial.Length > 0 && whole < cells) { region.Write(x + whole, y, partial, style); }
        }
        // Structured glyph/value composition never guesses meaning from arbitrary literal data.
        internal static StyledLine WithMarker(string marker, StyledLine value, Style style, Style? valueStyle = null) {
            List<TextSpan> spans = new List<TextSpan> { new TextSpan(marker, style) };
            if (value.Width > 0) {
                spans.Add(new TextSpan(" ", style));
                spans.AddRange(value.WithBaseStyle(valueStyle ?? style));
            }
            return new StyledLine(spans);
        }

        private static PresentedValue DataBarTone(PresentedValue value, ThemeToken tone, RenderContext context, bool showMarker = true) {
            Style style = context.Style(tone);
            List<TextSpan> spans = new List<TextSpan>();
            foreach (TextSpan span in value.Line) spans.Add(new TextSpan(span.Text, span.Style.WithColor(style.Color)));
            if (showMarker && value.Visual == CellVisualKind.Bar && !value.IsEmpty && TryDataBarStatus(tone, out StatusKind status)) {
                spans.Add(new TextSpan((value.Line.Width > 0 ? " " : string.Empty) + context.Glyphs.StatusSymbol(status), style));
            }
            return new PresentedValue(new StyledLine(spans), value.Alignment) {
                Role = tone, Visual = value.Visual, Fraction = value.Fraction, Series = value.Series,
                VisualWidth = value.VisualWidth, Number = value.Number, IsEmpty = value.IsEmpty
            };
        }

        private static bool TryDataBarStatus(ThemeToken tone, out StatusKind status) {
            switch (tone) {
                case ThemeToken.Pass: status = StatusKind.Pass; return true;
                case ThemeToken.Warn: status = StatusKind.Warn; return true;
                case ThemeToken.Fail: status = StatusKind.Fail; return true;
                case ThemeToken.Info: status = StatusKind.Info; return true;
                case ThemeToken.Running: status = StatusKind.Running; return true;
                case ThemeToken.Pending: status = StatusKind.Pending; return true;
                case ThemeToken.Skip: status = StatusKind.Skip; return true;
                default: status = default; return false;
            }
        }
    }
}
