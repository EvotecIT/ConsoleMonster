using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    internal static partial class CellRendering {
        // Structured glyph/value composition never guesses meaning from arbitrary literal data.
        internal static StyledLine WithMarker(string marker, StyledLine value, Style style) {
            List<TextSpan> spans = new List<TextSpan> { new TextSpan(marker, style) };
            if (value.Width > 0) {
                spans.Add(new TextSpan(" ", style));
                spans.AddRange(value.WithBaseStyle(style));
            }
            return new StyledLine(spans);
        }

        private static PresentedValue DataBarTone(PresentedValue value, ThemeToken tone, RenderContext context) {
            Style style = context.Style(tone);
            List<TextSpan> spans = new List<TextSpan>();
            foreach (TextSpan span in value.Line) spans.Add(new TextSpan(span.Text, span.Style.WithColor(style.Color)));
            if (value.Visual == CellVisualKind.Bar && !value.IsEmpty && TryDataBarStatus(tone, out StatusKind status)) {
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
