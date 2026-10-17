using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// The content of one cell after presentation: its text lines and, for value cells, the presented value (which may carry
    /// a data bar, sparkline or progress graphic). Shared by tables, record cards and record views.
    /// </summary>
    internal sealed class CellContent {
        internal CellContent(List<StyledLine> lines, PresentedValue? presented) {
            Lines = lines;
            Presented = presented;
        }

        internal List<StyledLine> Lines { get; }

        internal PresentedValue? Presented { get; }

        internal bool HasVisual => Presented != null && Presented.Visual != CellVisualKind.None;

        internal bool IsEmpty {
            get {
                if (Presented != null) {
                    return Presented.IsEmpty;
                }
                foreach (StyledLine line in Lines) {
                    if (line.Width > 0) {
                        return false;
                    }
                }
                return true;
            }
        }

        internal HorizontalAlignment? Alignment => Presented?.Alignment;

        internal int TextWidth {
            get {
                int width = 0;
                foreach (StyledLine line in Lines) {
                    width = Math.Max(width, line.Width);
                }
                return width;
            }
        }
    }

    /// <summary>Turns cells into content and draws visuals; one implementation for every widget that shows cells.</summary>
    internal static class CellRendering {
        /// <summary>Presents a cell: raw values through the column presenter and tone rules, markup as written, status cells with marker.</summary>
        internal static CellContent Content(TableCell cell, ICellPresenter? presenter, IReadOnlyList<ToneRule>? rules, PresenterContext context) {
            if (cell.HasValue && !(cell.Value is RichText)) {
                PresentedValue presented = CellPresenters.Present(cell.Value, presenter, rules, context);
                return new CellContent(new List<StyledLine> { presented.Line }, presented);
            }
            RenderContext render = context.Render;
            // Rich text values keep their own colours and line breaks, like markup cells.
            StyledText text = cell.Value is RichText rich ? rich.ToStyledText(render) : render.Markup(cell.Markup);
            List<StyledLine> lines = new List<StyledLine>(text.Lines);
            if (cell.Status.HasValue && lines.Count > 0) {
                Style style = render.Style(cell.Status.Value);
                List<TextSpan> spans = new List<TextSpan> { new TextSpan(render.Glyphs.StatusMarker(cell.Status.Value), style) };
                if (render.Glyphs.Tier != GlyphTier.Ascii || lines[0].Width > 0) {
                    spans.Add(new TextSpan(" ", style));
                    foreach (TextSpan span in lines[0]) {
                        spans.Add(new TextSpan(span.Text, style.Combine(span.Style)));
                    }
                }
                lines[0] = new StyledLine(spans);
            }
            return new CellContent(lines, null);
        }

        /// <summary>Cells a visual takes inside a cell of <paramref name="width"/> that also shows <paramref name="textWidth"/> cells of text.</summary>
        internal static int VisualCells(CellContent content, int width, int textWidth) {
            if (!content.HasVisual) {
                return 0;
            }
            int room = width - textWidth - (textWidth > 0 ? 1 : 0);
            return room >= 3 ? Math.Min(room, content.Presented!.VisualWidth) : 0;
        }

        /// <summary>Draws a cell's visual (bar, progress, sparkline) at x in <paramref name="cells"/> cells.</summary>
        internal static void DrawVisual(Region region, int x, int y, int cells, PresentedValue presented, RenderContext context) {
            if (cells <= 0) {
                return;
            }
            ThemeToken tone = presented.Tone ?? ThemeToken.Series1;
            switch (presented.Visual) {
                case CellVisualKind.Bar:
                    BarDrawer.Draw(region, x, y, cells, presented.Fraction, context.Style(tone), context, false, BarShape.Block);
                    break;
                case CellVisualKind.Progress:
                    BarDrawer.Draw(region, x, y, cells, presented.Fraction, context.Style(tone), context, true, BarShape.Line);
                    break;
                case CellVisualKind.Sparkline:
                    DrawSparkline(region, x, y, cells, presented.Series, context.Style(ThemeToken.Accent), context);
                    break;
            }
        }

        private static void DrawSparkline(Region region, int x, int y, int cells, IReadOnlyList<double>? series, Style style, RenderContext context) {
            if (series == null || series.Count == 0) {
                return;
            }
            int start = Math.Max(0, series.Count - cells);
            double min = double.MaxValue;
            double max = double.MinValue;
            for (int i = start; i < series.Count; i++) {
                min = Math.Min(min, series[i]);
                max = Math.Max(max, series[i]);
            }
            IReadOnlyList<string> ramp = context.Glyphs.VerticalRamp;
            for (int i = start; i < series.Count; i++) {
                double fraction = max > min ? (series[i] - min) / (max - min) : 0.5;
                int index = 1 + (int)Math.Round(fraction * (ramp.Count - 2));
                region.Put(x + (i - start), y, ramp[Math.Max(1, Math.Min(ramp.Count - 1, index))], style);
            }
        }
    }
}
