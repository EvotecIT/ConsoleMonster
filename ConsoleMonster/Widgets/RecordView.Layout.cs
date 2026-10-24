using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    public sealed partial class RecordView {
        private const int Gap = 2;

        private sealed class Entry {
            internal int Indent;
            internal int Height = 1;
            internal int Width;
            internal int SpaceBefore;
            internal StyledLine? Heading;
            internal string? Label;
            internal int LabelWidth;
            internal List<StyledLine>? Value;
            internal CellContent? Content;
            internal int VisualCells;
            internal IWidget? Widget;
        }

        private List<Entry> Build(int width, RenderContext context) {
            PresenterContext presenter = Presentation.CreateContext(context);
            List<Entry> entries = new List<Entry>();
            if (!string.IsNullOrEmpty(Title) || !string.IsNullOrEmpty(Summary)) {
                List<TextSpan> spans = new List<TextSpan>();
                if (!string.IsNullOrEmpty(Title)) {
                    foreach (TextSpan span in context.Markup(Title!).Lines[0].WithBaseStyle(context.Style(ThemeToken.Title))) {
                        spans.Add(span);
                    }
                }
                if (!string.IsNullOrEmpty(Summary)) {
                    string separator = spans.Count > 0 ? "  " + context.Glyphs.Get(Glyphs.Glyph.Dot) + "  " : string.Empty;
                    spans.Add(new TextSpan(separator + Summary, context.Style(ThemeToken.Muted)));
                }
                StyledLine title = TextWrapper.Truncate(new StyledLine(spans), width, context.Ellipsis);
                entries.Add(new Entry { Heading = title, Width = title.Width });
            }
            int i = 0;
            while (i < _items.Count) {
                RecordItem item = _items[i];
                int indent = Math.Min(item.Level * IndentWidth, Math.Max(0, width / 3));
                if (item.Kind == RecordItemKind.Heading) {
                    if (HideEmpty && !HasVisibleContent(i, presenter)) {
                        i++;
                        continue;
                    }
                    entries.Add(HeadingEntry(item, indent, width, entries.Count > 0, context));
                    i++;
                } else if (item.Kind == RecordItemKind.Widget) {
                    int available = Math.Max(1, width - indent);
                    Size size = item.Widget!.Measure(new Constraints(available), context);
                    entries.Add(new Entry { Indent = indent, Widget = item.Widget, Height = Math.Max(1, size.Height), Width = indent + size.Width });
                    i++;
                } else {
                    int end = i;
                    while (end < _items.Count && _items[end].Kind == RecordItemKind.Field && _items[end].Level == item.Level) {
                        end++;
                    }
                    AddFieldBlock(entries, i, end, indent, width, presenter, context);
                    i = end;
                }
            }
            return entries;
        }

        private Entry HeadingEntry(RecordItem item, int indent, int width, bool spaced, RenderContext context) {
            Style style = item.Level == 0 ? context.Style(ThemeToken.Title) : context.Style(ThemeToken.Header);
            if (item.Role.HasValue) {
                style = context.Style(item.Role.Value).With(TextDecoration.Bold);
            }
            List<TextSpan> spans = new List<TextSpan>();
            foreach (TextSpan span in context.Markup(item.Text).Lines[0].WithBaseStyle(style)) {
                spans.Add(span);
            }
            if (!string.IsNullOrEmpty(item.Note)) {
                spans.Add(new TextSpan("  " + item.Note, context.Style(ThemeToken.Muted)));
            }
            StyledLine line = TextWrapper.Truncate(new StyledLine(spans), Math.Max(1, width - indent), context.Ellipsis);
            // A blank line before each top-level section keeps records and cards apart.
            int before = spaced && item.Level == 0 ? 1 : 0;
            return new Entry { Indent = indent, Heading = line, Width = indent + line.Width, SpaceBefore = before, Height = 1 + before };
        }

        private void AddFieldBlock(List<Entry> entries, int start, int end, int indent, int width, PresenterContext presenter, RenderContext context) {
            List<RecordItem> fields = new List<RecordItem>();
            List<CellContent> contents = new List<CellContent>();
            int labelWidth = 0;
            for (int i = start; i < end; i++) {
                RecordItem item = _items[i];
                CellContent content = CellRendering.Content(item.Cell!, item.Presenter, item.Rules, presenter);
                if (HideEmpty && content.IsEmpty) {
                    continue;
                }
                fields.Add(item);
                contents.Add(content);
                labelWidth = Math.Max(labelWidth, DisplayWidth.Measure(item.Text));
            }
            int room = Math.Max(1, width - indent);
            labelWidth = Math.Min(labelWidth, Math.Max(4, (int)(room * MaxLabelFraction)));
            int valueWidth = Math.Max(1, room - labelWidth - Gap);
            for (int f = 0; f < fields.Count; f++) {
                CellContent content = contents[f];
                int textWidth = content.TextWidth;
                int visual = CellRendering.VisualCells(content, valueWidth, Math.Min(textWidth, valueWidth - 4));
                int textRoom = Math.Max(1, valueWidth - visual - (visual > 0 ? 1 : 0));
                List<StyledLine> lines = CellRendering.FitLines(content.LinesAtWidth(textRoom, context), textRoom, fields[f].Overflow, fields[f].MaxLines, context);
                int used = 0;
                foreach (StyledLine line in lines) {
                    used = Math.Max(used, line.Width);
                }
                entries.Add(new Entry {
                    Indent = indent,
                    Label = fields[f].Text,
                    LabelWidth = labelWidth,
                    Value = lines,
                    Content = content,
                    VisualCells = visual,
                    Height = lines.Count,
                    Width = indent + labelWidth + Gap + visual + (visual > 0 ? 1 : 0) + used
                });
            }
        }

        // A heading is shown when anything visible follows it before the next heading at the same or a higher level.
        private bool HasVisibleContent(int headingIndex, PresenterContext presenter) {
            int level = _items[headingIndex].Level;
            for (int i = headingIndex + 1; i < _items.Count; i++) {
                RecordItem item = _items[i];
                if (item.Kind == RecordItemKind.Heading && item.Level <= level) {
                    return false;
                }
                if (item.Kind == RecordItemKind.Widget) {
                    return true;
                }
                if (item.Kind == RecordItemKind.Field && !CellRendering.Content(item.Cell!, item.Presenter, item.Rules, presenter).IsEmpty) {
                    return true;
                }
            }
            return false;
        }

        private static void Draw(Region region, int y, Entry entry, RenderContext context) {
            y += entry.SpaceBefore;
            if (entry.Heading != null) {
                region.Write(entry.Indent, y, entry.Heading);
                return;
            }
            if (entry.Widget != null) {
                entry.Widget.Render(region.Sub(entry.Indent, y, Math.Max(0, region.Width - entry.Indent), Math.Max(0, Math.Min(entry.Height, region.Height - y))), context);
                return;
            }
            string label = TextWrapper.Truncate(entry.Label ?? string.Empty, entry.LabelWidth, context.Ellipsis);
            region.Write(entry.Indent, y, label, context.Style(ThemeToken.Muted));
            int x = entry.Indent + entry.LabelWidth + Gap;
            if (entry.VisualCells > 0 && entry.Content?.Presented != null) {
                CellRendering.DrawVisual(region, x, y, entry.VisualCells, entry.Content.Presented, context);
                x += entry.VisualCells + 1;
            }
            for (int line = 0; line < entry.Value!.Count && y + line < region.Height; line++) {
                region.Write(x, y + line, entry.Value[line]);
            }
        }
    }
}
