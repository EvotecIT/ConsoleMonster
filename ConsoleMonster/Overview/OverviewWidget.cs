using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Overview {
    /// <summary>A compact host overview composed from the existing panel, row, column and progress widgets.</summary>
    public sealed class OverviewWidget : IWidget {
        /// <summary>Creates a print-once view of an immutable host snapshot.</summary>
        /// <param name="snapshot">The data to render; text is displayed literally.</param>
        public OverviewWidget(OverviewSnapshot snapshot) {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        /// <summary>The snapshot rendered by this widget.</summary>
        public OverviewSnapshot Snapshot { get; }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) {
            int width = Math.Min(120, constraints.MaxWidth);
            int height = width >= 80 ? Math.Min(24, constraints.MaxHeight) : constraints.MaxHeight;
            return Build(width, context).Measure(new Constraints(width, height), context);
        }

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) {
            int width = Math.Min(120, region.Width);
            int height = width >= 80 ? Math.Min(24, region.Height) : region.Height;
            Build(width, context).Render(region.Sub(0, 0, width, height), context);
        }

        private IWidget Build(int width, RenderContext context) {
            string uptime = Snapshot.Uptime.HasValue ? "up " + ((int)Snapshot.Uptime.Value.TotalDays).ToString(CultureInfo.InvariantCulture) + "d " + Snapshot.Uptime.Value.ToString(@"hh\:mm", CultureInfo.InvariantCulture) : string.Empty;
            // Preserve host, uptime and local clock when the OS description needs truncation on small terminals.
            string separator = context.Profile.Glyphs == Capabilities.GlyphTier.Ascii ? " | " : " · ";
            string tail = separator + (uptime.Length > 0 ? uptime + separator : string.Empty)
                + Snapshot.CapturedAt.ToString("HH:mm zzz", CultureInfo.InvariantCulture);
            string head = Clean(Snapshot.HostName) + separator + Clean(Snapshot.OperatingSystem);
            int available = Math.Max(0, width - DisplayWidth.Measure(tail));
            StyledLine headline = TextWrapper.Truncate(StyledLine.From(head), available, context.Ellipsis);
            Rows result = new Rows(Truncated(TextBlock.Plain(headline.PlainText + tail, ThemeToken.Title)));
            List<OverviewSection> visible = Snapshot.Sections.Where(section => section.IsPending || section.Items.Count > 0).ToList();
            if (width < 80) {
                foreach (OverviewSection section in visible) {
                    result.Add(BuildPanel(section, Math.Max(3, section.Items.Count + 2), width, context));
                }
                return result;
            }
            int columns = width >= 80 ? 2 : 1;
            int groups = (visible.Count + columns - 1) / columns;
            if (groups == 0) { return result; }
            int gap = 1;
            int maxGroups = 5;
            bool omitted = groups > maxGroups;
            if (omitted) { visible = visible.Take(maxGroups * columns).ToList(); groups = maxGroups; }
            int[] heights = new int[groups];
            for (int group = 0; group < groups; group++) {
                int first = group * columns;
                int lines = visible[first].Items.Count;
                if (columns == 2 && first + 1 < visible.Count) { lines = Math.Max(lines, visible[first + 1].Items.Count); }
                heights[group] = Math.Max(3, Math.Min(7, lines + 2));
            }
            int budget = 23 - groups * gap - (omitted ? 1 + gap : 0);
            while (heights.Sum() > budget) {
                int largest = Array.IndexOf(heights, heights.Max());
                if (heights[largest] <= 3) { break; }
                heights[largest]--;
            }
            result.Gap = gap;
            for (int index = 0; index < visible.Count; index += columns) {
                int panelHeight = heights[index / columns];
                int panelWidth = index + 1 < visible.Count ? (width - 2) / columns : width;
                IWidget first = BuildPanel(visible[index], panelHeight, panelWidth, context);
                if (columns == 2 && index + 1 < visible.Count) {
                    result.Add(new Columns(first, BuildPanel(visible[index + 1], panelHeight, panelWidth, context)) { Gap = 2, CollapseBelow = 80 });
                } else { result.Add(first); }
            }
            if (omitted) { result.Add(TextBlock.Plain(context.Ellipsis + " more sections", ThemeToken.Muted)); }
            return result;
        }

        private static IWidget BuildPanel(OverviewSection section, int height, int width, RenderContext context) {
            Rows body = new Rows();
            int capacity = Math.Max(1, height - 2);
            string? footer = null;
            if (section.IsPending) {
                body.Add(TextBlock.Plain(section.Name == "Resources" ? "CPU: measuring" + context.Ellipsis : context.Ellipsis, ThemeToken.Muted));
            } else {
                bool overflow = section.Items.Count > capacity;
                int count = overflow ? Math.Max(1, capacity - 1) : section.Items.Count;
                IReadOnlyList<OverviewItem> items = overflow && string.Equals(section.Name, "Disks", StringComparison.OrdinalIgnoreCase)
                    ? section.Items.OrderByDescending(item => item.Percentage ?? -1).ToArray() : section.Items;
                OverviewItem[] metrics = items.Take(count).Where(item => item.Percentage.HasValue).ToArray();
                int labelWidth = metrics.Select(item => DisplayWidth.Measure(Clean(item.Label))).DefaultIfEmpty(0).Max();
                int statusWidth = metrics.Select(item => Pressure(item.Percentage!.Value).Length).DefaultIfEmpty(0).Max();
                int valueWidth = metrics.Select(item => DisplayWidth.Measure(Clean(item.Value))).DefaultIfEmpty(0).Max();
                for (int i = 0; i < count; i++) {
                    OverviewItem item = items[i];
                    if (item.Percentage.HasValue) {
                        double used = item.Percentage.Value;
                        string status = Pressure(used);
                        // At the two-column breakpoint retain the measured value before spending space on a bar.
                        if (width < labelWidth + valueWidth + statusWidth + 18) {
                            ThemeToken tone = used >= 90 ? ThemeToken.Fail : used >= 80 ? ThemeToken.Warn : item.Tone;
                            body.Add(Truncated(TextBlock.Plain(Clean(item.Label) + ": " + Clean(item.Value) + " " + NumberFormat.Percent(used / 100) + (status.Length > 0 ? " " + status : string.Empty), tone)));
                            continue;
                        }
                        ProgressBar bar = new ProgressBar(Markup.Escape(Clean(item.Label))) {
                            LabelWidth = labelWidth,
                            Value = used,
                            Total = 100,
                            ShowCount = false,
                            Shape = BarShape.Block,
                            Status = used >= 90 ? StatusKind.Fail : used >= 80 ? StatusKind.Warn : StatusKind.Pass
                        };
                        // Reserve the same suffix space for every metric so both bar edges and percentages align.
                        Columns row = new Columns { Gap = 1, CollapseBelow = 0 }.Add(bar);
                        if (statusWidth > 0) { row.Add(TextBlock.Plain(status, ThemeToken.Muted), LayoutSize.Fixed(statusWidth)); }
                        if (valueWidth > 0) { row.Add(Truncated(TextBlock.Plain(Clean(item.Value), ThemeToken.Muted)), LayoutSize.Fixed(valueWidth)); }
                        body.Add(row);
                    } else {
                        body.Add(Truncated(TextBlock.Plain(Clean(item.Label) + ": " + Clean(item.Value), item.Tone)));
                    }
                }
                if (overflow) {
                    string omitted = "+" + (section.Items.Count - count).ToString(CultureInfo.InvariantCulture) + " more";
                    if (capacity == 1) { footer = omitted; }
                    else { body.Add(TextBlock.Plain(omitted, ThemeToken.Muted)); }
                }
            }
            return new Panel(body, Markup.Escape(Clean(section.Title))) { Height = Math.Min(height, body.Children.Count + 2), Footer = footer };
        }

        private static string Pressure(double used) => used >= 90 ? "critical" : used >= 80 ? "high" : string.Empty;

        private static TextBlock Truncated(TextBlock block) { block.Overflow = TextOverflow.Ellipsis; return block; }

        private static string Clean(string value) => new string(value.Select(character => char.IsControl(character) ? ' ' : character).ToArray());
    }
}
