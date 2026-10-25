using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    public abstract partial class ConsoleDisplayCmdlet {
        private IWidget BuildTableView(IReadOnlyList<PSObject> items) {
            bool first = true;
            TableBuildOptions options = new TableBuildOptions { Total = ShowTotals.IsPresent, Cmdlet = this, Culture = Culture, ShowGroupColumn = ShowGroupColumn.IsPresent, NoSort = NoSort.IsPresent };
            // Objects of different kinds get one table each (as Format-Table switches format); the title heads the first.
            System.Collections.Generic.List<Layout.IWidget> tables = PSFormatting.BuildTables(items, Property, GroupBy, options, table => {
                if (Title != null) {
                    table.Title = first ? PSText.Markup(Title) : null;
                }
                table.Caption = PSText.Markup(Caption);
                table.Border = Border;
                table.CardsBelow = CardsBelow;
                table.HeaderSeparator = !NoHeaderSeparator.IsPresent;
                table.MaxRows = MaxRows;
                if (Wrap.IsPresent) {
                    table.WrapLines = 0;
                }
                // One object stays a table unless asked; Record (or Cards) shows every row as labelled fields, with -Top,
                // the "Showing" line and the totals still applied.
                table.RecordViewRows = 0;
                table.AlwaysCards = false;
                table.Expand = Expand.IsPresent;
                if (StatusProperty != null) {
                    foreach (TableColumn column in table.Columns) {
                        if (string.Equals(column.Key, StatusProperty, System.StringComparison.OrdinalIgnoreCase)) { column.Presenter = Presenters.CellPresenters.StatusWord; }
                    }
                }
                first = false;
            });
            return PSFormatting.Combine(tables);
        }
    }
}
