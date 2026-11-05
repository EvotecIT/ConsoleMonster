using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// The tab strip shared by <see cref="Tabs"/> and the document viewer: the labels on one row with the active one
    /// underlined, and a rule under them that is heavier under the active label, so plain text and ASCII still show which
    /// tab is open. When the labels are wider than the row they scroll to keep the active tab in view, with ‹ and › (&lt; and
    /// &gt; in ASCII) marking the labels out of view.
    /// </summary>
    internal static class TabStrip {
        /// <summary>The rows the strip takes: the labels and the rule.</summary>
        internal const int Height = 2;

        /// <summary>Draws the strip at the top of the region and returns where each visible label is, in region coordinates.</summary>
        internal static List<Header> Draw(Region region, IReadOnlyList<string> labels, int selected, bool focused, RenderContext context) {
            List<Header> headers = new List<Header>();
            if (region.Width <= 0 || region.Height <= 0 || labels.Count == 0) { return headers; }
            GlyphTier tier = context.Glyphs.Tier;
            bool ascii = tier == GlyphTier.Ascii;
            string[] texts = new string[labels.Count];
            int[] widths = new int[labels.Count];
            int total = 0;
            for (int i = 0; i < labels.Count; i++) {
                texts[i] = " " + SymbolFallback.Constrain(labels[i], tier) + " ";
                widths[i] = DisplayWidth.Measure(texts[i]);
                total += widths[i];
            }
            int first = 0;
            bool scrolled = total > region.Width;
            int room = region.Width;
            if (scrolled) {
                // One column for each edge marker; the first visible label is the earliest that still shows the active one.
                room = Math.Max(1, region.Width - 2);
                int used = 0;
                first = selected;
                for (int i = selected; i >= 0 && used + widths[i] <= room; i--) { used += widths[i]; first = i; }
                if (used == 0) { first = selected; }
            }
            int x = scrolled ? 1 : 0;
            int end = scrolled ? region.Width - 1 : region.Width;
            int last = first - 1;
            Style rule = context.Style(ThemeToken.Border);
            if (region.Height > 1) { region.HorizontalLine(0, 1, region.Width, ascii ? "-" : "─", rule); }
            for (int i = first; i < labels.Count && x < end; i++) {
                string text = TextWrapper.Truncate(texts[i], end - x, context.Ellipsis);
                int width = DisplayWidth.Measure(text);
                if (width <= 0) { break; }
                bool active = i == selected;
                Style style = active
                    ? context.Style(focused ? ThemeToken.Focus : ThemeToken.Accent).With(TextDecoration.Bold | TextDecoration.Underline)
                    : context.Style(ThemeToken.Muted);
                region.Write(x, 0, text, style);
                if (active && region.Height > 1) {
                    region.HorizontalLine(x, 1, width, ascii ? "=" : tier == GlyphTier.Basic ? "═" : "━", context.Style(focused ? ThemeToken.Focus : ThemeToken.Accent));
                }
                headers.Add(new Header(i, new Rect(region.Bounds.X + x, region.Bounds.Y, width, 1)));
                last = i;
                x += width;
            }
            if (scrolled) {
                Style marker = context.Style(ThemeToken.Muted);
                if (first > 0) { region.Write(0, 0, ascii ? "<" : "‹", marker); }
                if (last < labels.Count - 1) { region.Write(region.Width - 1, 0, ascii ? ">" : "›", marker); }
            }
            return headers;
        }

        /// <summary>A visible label's index and area.</summary>
        internal readonly struct Header {
            internal Header(int index, Rect area) { Index = index; Area = area; }
            internal int Index { get; }
            internal Rect Area { get; }
            internal bool Contains(int column, int row) => column >= Area.X && column < Area.Right && row >= Area.Y && row < Area.Bottom;
        }
    }
}
