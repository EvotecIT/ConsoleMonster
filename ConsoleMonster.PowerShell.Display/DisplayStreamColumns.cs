using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell.Display;

// Streaming cannot discover a later row's preferred width before writing its header.
// Allocate from the terminal and declared limits instead; never change the caller's columns.
internal sealed class DisplayStreamColumns {
    internal DisplayStreamColumns(Table prototype, DefaultDisplayContext context) {
        List<int> selected = Enumerable.Range(0, prototype.Columns.Count).ToList();
        int[] minimum = prototype.Columns.Select(column => Math.Max(1, column.MinWidth ?? 1) > (column.MaxWidth ?? int.MaxValue)
            ? throw new ArgumentException("A stream column's minimum cannot exceed its maximum.")
            : Math.Max(column.MinWidth ?? 1, Math.Min(column.MaxWidth ?? int.MaxValue, column.Width ?? column.MinWidth
                ?? DisplayWidth.Measure(Markup.Strip(column.Header), context.Profile.WidthProfile)))).ToArray();
        int[] maximum = prototype.Columns.Select((column, index) => Math.Max(minimum[index], column.Width ?? column.MaxWidth
            ?? (IsNumeric(column, prototype.Rows[0][index]) ? Math.Max(16, minimum[index]) : context.Width))).ToArray();
        List<string> hidden = new List<string>();
        // Preserve the core's record-card transition below 60 cells. At grid widths, retain important columns first.
        if (context.Width >= 60) {
            bool hasKey = prototype.Columns.Any(column => column.IsKey);
            while (selected.Count > 1 && selected.Sum(index => minimum[index]) + 2 * (selected.Count - 1) > context.Width) {
                int victim = selected.Where(index => !prototype.Columns[index].IsKey && (hasKey || index != 0))
                    .OrderBy(index => prototype.Columns[index].Priority).ThenByDescending(index => index).DefaultIfEmpty(-1).First();
                if (victim < 0) { break; }
                selected.Remove(victim); hidden.Add(Markup.Strip(prototype.Columns[victim].Header));
            }
        }
        int available = Math.Max(selected.Count, context.Width - 2 * (selected.Count - 1));
        int[] widths = selected.Select(index => minimum[index]).ToArray();
        bool fits = widths.Sum() <= available;
        if (fits) {
            int spare = available - widths.Sum();
            int[] order = Enumerable.Range(0, selected.Count).OrderByDescending(v => prototype.Columns[selected[v]].Priority).ToArray();
            while (spare > 0) {
                bool grew = false;
                foreach (int v in order) {
                    int index = selected[v];
                    int add = Math.Min(spare, Math.Min(maximum[index] - widths[v], prototype.Columns[index].Flexible ? 4 : 1));
                    if (add <= 0) { continue; }
                    widths[v] += add; spare -= add; grew = true;
                }
                if (!grew) { break; }
            }
        }
        Indices = selected.ToArray();
        Columns = selected.Select((index, v) => {
            TableColumn source = prototype.Columns[index];
            TableColumn copy = new TableColumn(source.Header) {
                Key = source.Key, Alignment = source.Alignment ?? (IsNumeric(source, prototype.Rows[0][index]) ? HorizontalAlignment.Right : (HorizontalAlignment?)null),
                Priority = source.Priority, IsKey = source.IsKey, Flexible = source.Flexible,
                MinWidth = source.MinWidth, MaxWidth = source.MaxWidth, Width = source.Width,
                Presenter = source.Presenter, Aggregate = source.Aggregate, Overflow = ColumnOverflow.Truncate, MaxLines = 1
            };
            foreach (var rule in source.RoleRules) { copy.RoleRules.Add(rule); }
            if (fits || selected.Count == 1) {
                copy.Width = fits ? widths[v] : Math.Max(1, available);
                copy.MinWidth = null; copy.MaxWidth = null;
                copy.Header = Markup.Escape(TextWrapper.Truncate(Markup.Strip(source.Header), copy.Width.Value, context.Profile.WidthProfile));
            }
            return copy;
        }).ToArray();
        Hidden = hidden.Count == 0 ? null : "+" + hidden.Count.ToString(context.Culture) + " more (" + string.Join(", ", hidden) + ") · pipe to Format-Table for every row";
    }
    internal int[] Indices { get; }
    internal TableColumn[] Columns { get; }
    internal string? Hidden { get; }
    private static bool IsNumeric(TableColumn column, TableCell cell) {
        if (column.Alignment.HasValue) { return column.Alignment.Value == HorizontalAlignment.Right; }
        object? value = cell.Value;
        return value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint
            || value is long || value is ulong || value is float || value is double || value is decimal;
    }
}
