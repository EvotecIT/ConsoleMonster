using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell.Display;

// Streaming cannot discover a later row's preferred width before writing its header.
// Measure the available first row and declared limits; never change the caller's columns.
internal sealed class DisplayStreamColumns {
    internal DisplayStreamColumns(Table prototype, DefaultDisplayContext context) {
        List<int> selected = Enumerable.Range(0, prototype.Columns.Count).ToList();
        int[] minimum = new int[selected.Count];
        int[] desired = new int[selected.Count];
        bool hasKey = prototype.Columns.Any(column => column.IsKey);
        PresenterContext presenter = prototype.Presentation.CreateContext(Terminal.CreateContext(context.Profile));
        for (int index = 0; index < selected.Count; index++) {
            TableColumn column = prototype.Columns[index];
            int limit = Math.Max(1, column.MaxWidth ?? context.Width);
            if (Math.Max(1, column.MinWidth ?? 1) > limit) { throw new ArgumentException("A stream column's minimum cannot exceed its maximum."); }
            TableCell cell = prototype.Rows[0][index];
            string text = cell.HasValue ? CellPresenters.Present(cell.Value, column.Presenter,
                (IReadOnlyList<RoleRule>)column.RoleRules, presenter).Text : Markup.Strip(cell.Markup);
            int content = text.Split('\n').Select(line => DisplayWidth.Measure(line.TrimEnd('\r'), context.Profile.WidthProfile)).DefaultIfEmpty(0).Max();
            int header = DisplayWidth.Measure(Markup.Strip(column.Header), context.Profile.WidthProfile);
            // A long header must not reserve a whole text column for a one-digit value or an empty field.
            desired[index] = Math.Min(limit, Math.Max(content, Math.Min(header, content == 0 ? 12 : Math.Max(4, content))));
            minimum[index] = Math.Max(1, column.MinWidth ?? Math.Min(desired[index], IsNumeric(column, cell) ? Math.Max(1, content) : 8));
            bool identity = column.IsKey || !hasKey && index == 0;
            if (identity) {
                // Reserve useful identity space before dropping detail columns. The first row cannot predict later
                // names, so use the core table's 32-cell key comfort within the caller's maximum.
                desired[index] = Math.Max(desired[index], Math.Min(32, limit));
                minimum[index] = Math.Max(minimum[index], desired[index]);
            }
            desired[index] = Math.Max(desired[index], minimum[index]);
            if (column.Width.HasValue) { desired[index] = minimum[index] = Math.Max(1, column.Width.Value); }
        }
        List<string> hidden = new List<string>();
        // Preserve the core's record-card transition below 60 cells. At grid widths, retain important columns first.
        if (context.Width >= 60) {
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
            int[] order = Enumerable.Range(0, selected.Count)
                .OrderByDescending(v => prototype.Columns[selected[v]].IsKey || !hasKey && selected[v] == 0)
                .ThenByDescending(v => prototype.Columns[selected[v]].Priority).ToArray();
            foreach (int v in order) {
                int add = Math.Min(spare, desired[selected[v]] - widths[v]);
                widths[v] += add; spare -= add;
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
