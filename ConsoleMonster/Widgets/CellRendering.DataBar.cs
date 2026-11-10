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
            BarDrawer.Draw(region, x, y, cells, fraction, style, context, false, BarShape.Block);
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
            if (value.Visual == CellVisualKind.Bar && !value.IsEmpty && StatusKinds.TryFromToken(tone, out StatusKind status)) {
                spans.Add(new TextSpan((value.Line.Width > 0 ? " " : string.Empty) + (showMarker || context.Profile.IsAccessible ? context.Glyphs.StatusSymbol(status) : context.Labels.StatusWord(status)), style));
            }
            return new PresentedValue(new StyledLine(spans), value.Alignment) {
                Role = tone, Visual = value.Visual, Fraction = value.Fraction, Series = value.Series,
                VisualWidth = value.VisualWidth, Number = value.Number, IsEmpty = value.IsEmpty
            };
        }

    }
}
