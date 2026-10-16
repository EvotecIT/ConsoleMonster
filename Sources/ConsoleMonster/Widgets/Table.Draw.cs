using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class Table {
        private bool IsBoxed => Border == TableBorder.Rounded || Border == TableBorder.Square || Border == TableBorder.Double;

        private static BoxStyle BoxStyleFor(TableBorder border) {
            switch (border) {
                case TableBorder.Square:
                    return BoxStyle.Single;
                case TableBorder.Double:
                    return BoxStyle.Double;
                default:
                    return BoxStyle.Rounded;
            }
        }

        private void Draw(Region region, TableLayout layout, RenderContext context) {
            Style border = context.Style(ThemeToken.Border);
            bool boxed = IsBoxed;
            BoxGlyphs box = context.Glyphs.GetBox(BoxStyleFor(Border));
            int y = 0;
            if (!string.IsNullOrEmpty(Title)) {
                StyledLine title = TextWrapper.Truncate(context.Markup(Title!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Title)), region.Width, context.Ellipsis);
                region.Write(boxed ? 1 : 0, y++, title);
            }
            if (boxed) {
                DrawRule(region, y++, layout, box.TopLeft, box.TopJoint, box.TopRight, box.Horizontal, border);
            }
            if (ShowHeader) {
                y = DrawCells(region, y, layout, layout.HeaderLines, null, null, layout.HeaderHeight, box, border, context);
                if (!HeaderSeparator) {
                    // The underlined header carries the separation.
                } else if (boxed) {
                    DrawRule(region, y++, layout, box.LeftJoint, box.Cross, box.RightJoint, box.Horizontal, border);
                } else if (Border == TableBorder.Minimal) {
                    DrawRule(region, y++, layout, string.Empty, " ", string.Empty, context.Glyphs.Get(Glyph.RuleLine), border);
                }
            }
            foreach (TableLayoutRow row in layout.Rows) {
                if (row.Kind == TableRowKind.Group) {
                    y = DrawSpanning(region, y, layout, row.Spanning!, box, border, context);
                } else {
                    y = DrawCells(region, y, layout, row.Lines!, row.Cells, row.VisualCells, row.Height, box, border, context);
                }
            }
            if (layout.Total != null) {
                if (boxed) {
                    DrawRule(region, y++, layout, box.LeftJoint, box.Cross, box.RightJoint, box.Horizontal, border);
                } else if (Border == TableBorder.Minimal) {
                    DrawRule(region, y++, layout, string.Empty, " ", string.Empty, context.Glyphs.Get(Glyph.RuleLine), border);
                }
                y = DrawCells(region, y, layout, layout.Total.Lines!, layout.Total.Cells, layout.Total.VisualCells, layout.Total.Height, box, border, context);
            }
            if (boxed) {
                DrawRule(region, y++, layout, box.BottomLeft, box.BottomJoint, box.BottomRight, box.Horizontal, border);
            }
            int indent = boxed ? 1 : 0;
            if (layout.HiddenMarker != null && y < region.Height) {
                region.Write(indent, y++, layout.HiddenMarker);
            }
            if (layout.ShowingLine != null && y < region.Height) {
                region.Write(indent, y++, layout.ShowingLine);
            }
            if (!string.IsNullOrEmpty(Caption) && y < region.Height) {
                StyledLine caption = TextWrapper.Truncate(context.Markup(Caption!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Muted)), region.Width, context.Ellipsis);
                region.Write(indent, y, caption);
            }
        }

        private void DrawRule(Region region, int y, TableLayout layout, string left, string joint, string right, string line, Style style) {
            if (y >= region.Height) {
                return;
            }
            int x = 0;
            if (left.Length > 0) {
                region.Put(x++, y, left, style);
            }
            for (int v = 0; v < layout.Widths.Length; v++) {
                int span = layout.Widths[v] + (IsBoxed ? 2 : 0);
                region.HorizontalLine(x, y, span, line, style);
                x += span;
                if (v < layout.Widths.Length - 1) {
                    if (IsBoxed) {
                        region.Put(x++, y, joint, style);
                    } else {
                        x += layout.Gap;
                    }
                }
            }
            if (right.Length > 0) {
                region.Put(x, y, right, style);
            }
        }

        private int DrawSpanning(Region region, int y, TableLayout layout, StyledLine text, BoxGlyphs box, Style border, RenderContext context) {
            if (y >= region.Height) {
                return y;
            }
            int inner = layout.TotalWidth - (IsBoxed ? 4 : 0);
            if (IsBoxed) {
                region.Put(0, y, box.Vertical, border);
                region.Put(layout.TotalWidth - 1, y, box.Vertical, border);
            }
            region.Write(IsBoxed ? 2 : 0, y, TextWrapper.Truncate(text, Math.Max(1, inner), context.Ellipsis));
            return y + 1;
        }

        private int DrawCells(Region region, int y, TableLayout layout, List<StyledLine>[] lines, CellContent[]? cells, int[]? visuals, int height, BoxGlyphs box, Style border, RenderContext context) {
            for (int line = 0; line < height && y < region.Height; line++, y++) {
                int x = 0;
                if (IsBoxed) {
                    region.Put(x++, y, box.Vertical, border);
                }
                for (int v = 0; v < layout.Widths.Length; v++) {
                    int width = layout.Widths[v];
                    if (IsBoxed) {
                        x++;
                    }
                    int visual = visuals != null ? visuals[v] : 0;
                    if (visual > 0 && line == 0 && cells != null) {
                        PresentedValueDraw(region, x, y, visual, cells[layout.Visible[v]], context);
                    }
                    int textX = x + (visual > 0 ? visual + 1 : 0);
                    int textWidth = width - (visual > 0 ? visual + 1 : 0);
                    if (line < lines[v].Count) {
                        StyledLine text = lines[v][line];
                        int offset = 0;
                        HorizontalAlignment alignment = layout.Alignments[v];
                        if (alignment == HorizontalAlignment.Right) {
                            offset = Math.Max(0, textWidth - text.Width);
                        } else if (alignment == HorizontalAlignment.Center) {
                            offset = Math.Max(0, (textWidth - text.Width) / 2);
                        }
                        region.Write(textX + offset, y, text, textWidth - offset);
                    }
                    x += width;
                    if (IsBoxed) {
                        x++;
                        region.Put(x++, y, box.Vertical, border);
                    } else if (v < layout.Widths.Length - 1) {
                        x += layout.Gap;
                    }
                }
            }
            return y;
        }

        private static void PresentedValueDraw(Region region, int x, int y, int cells, CellContent content, RenderContext context) {
            if (content.Presented != null) {
                CellRendering.DrawVisual(region, x, y, cells, content.Presented, context);
            }
        }
    }
}
