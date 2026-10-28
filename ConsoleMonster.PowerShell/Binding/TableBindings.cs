using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using ConsoleMonster.Live;
using ConsoleMonster.Values;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell.Binding {
    // PowerShell expressions belong to the host layer. Weak column keys keep their lifetime tied to the table,
    // and preserve bindings when a caller wraps an existing table or changes its column order.
    internal static class TableBindings {
        private static readonly ConditionalWeakTable<TableColumn, BoundColumn> Columns = new ConditionalWeakTable<TableColumn, BoundColumn>();

        internal static void Remember(BoundColumn column) => Columns.Add(column.Column, column.WithOptions(column.Options?.Clone() ?? PSShaping.Options()));

        internal static ConsoleTableRow Project(LiveTable table, object source, string? keyProperty = null) {
            var columns = new List<BoundColumn>(table.Table.Columns.Count);
            ShapeOptions? shape = null;
            bool bound = false;
            foreach (TableColumn column in table.Table.Columns) {
                if (Columns.TryGetValue(column, out BoundColumn? binding) && binding != null) {
                    shape ??= binding.Options?.Clone() ?? PSShaping.Options();
                    columns.Add(binding);
                    bound = true;
                } else {
                    columns.Add(new BoundColumn(column.Id, column, column.Id));
                }
            }
            if (!bound) { return table.Project(source, keyProperty, PSShaping.Options()); }
            shape ??= PSShaping.Options();
            for (int i = 0; i < columns.Count; i++) { columns[i] = columns[i].WithOptions(shape); }
            object?[] values = PSFormatting.Values(columns, PSObject.AsPSObject(source), shape, ShapeBudget.For(shape), out object? key);
            string? keyName = keyProperty ?? table.KeyProperty;
            if (keyName != null && (!ValueShaper.TryGetField(source, keyName, out key, shape) || key == null || key is FieldError)) {
                throw new ArgumentException("The row has no '" + keyName + "' value to use as its key.", nameof(source));
            }
            if (key is TableCell cell) { key = cell.HasValue ? cell.Value : Text.Markup.Strip(cell.Markup); }
            if (key == null || key is FieldError || key is BudgetExceeded) {
                throw new ArgumentException("The row needs an unformatted first-column value or an explicit KeyProperty for its stable key.", nameof(source));
            }
            return new ConsoleTableRow { Key = PSWidgets.ToText(key), Values = values };
        }
    }
}
