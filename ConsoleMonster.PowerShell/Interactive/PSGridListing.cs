using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.PowerShell.Binding;
using ConsoleMonster.Text;

namespace ConsoleMonster.PowerShell {
    // PowerShell objects as a grid listing: the same columns and cell values as Show-ConsoleTable (shared value rules),
    // used by Show-ConsoleGrid and New-ConsoleDataGrid.
    internal sealed class PSGridListing {
        private readonly ConsoleMonster.Values.ShapeOptions _shape;
        private readonly IReadOnlyList<BoundColumn> _columns;

        private PSGridListing(ConsoleMonster.Values.ShapeOptions shape, IReadOnlyList<BoundColumn> columns) { _shape = shape; _columns = columns; }

        // A collection passed as one object is its items.
        internal static PSGridListing Build(IEnumerable<PSObject> objects, object[]? property, CultureInfo? culture, PSCmdlet cmdlet, Grid grid, out IReadOnlyList<PSObject> items) {
            ConsoleMonster.Values.ShapeOptions shape = PSShaping.Options(culture);
            items = PSFormatting.Expand(new List<PSObject>(objects), shape);
            IReadOnlyList<BoundColumn> columns = PropertyBinder.Bind(items, property, shape, cmdlet);
            if (culture != null) { grid.Presentation = new Presenters.PresentationOptions { Culture = culture }; }
            foreach (BoundColumn column in columns) { grid.AddColumn(column.Column); }
            PSGridListing listing = new PSGridListing(shape, columns);
            ConsoleMonster.Values.ShapeBudget budget = ConsoleMonster.Values.ShapeBudget.For(shape);
            foreach (PSObject item in items) { grid.AddRow(item, listing.Cells(item, budget)); }
            return listing;
        }

        internal IReadOnlyList<BoundColumn> Columns => _columns;

        internal GridRow Row(PSObject item) => new GridRow(item, Cells(item, ConsoleMonster.Values.ShapeBudget.For(_shape)));

        private object?[] Cells(PSObject item, ConsoleMonster.Values.ShapeBudget readBudget) {
            object?[] values = new object?[_columns.Count];
            for (int c = 0; c < _columns.Count; c++) {
                object? value = PSFormatting.Read(_columns[c], item, readBudget);
                object? prepared = value is ConsoleMonster.Values.FieldError || value is ConsoleMonster.Values.BudgetExceeded ? value : ConsoleMonster.Values.ValueShaper.Prepare(value, _shape);
                object? cell = _columns[c].Column.Presenter != null && PSShaping.IsPresentable(prepared) ? prepared : PSShaping.Cell(value, _columns[c].Column, _shape, ConsoleMonster.Values.ShapeBudget.For(_shape));
                // The grid sorts and filters cell values: marked cells (empty collections, failed getters) become their text.
                values[c] = cell is ConsoleMonster.Widgets.TableCell marked ? Markup.Strip(marked.Markup) : cell;
            }
            return values;
        }
    }
}
