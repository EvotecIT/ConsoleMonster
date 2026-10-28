using System;
using System.Collections.Generic;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell.Display;

internal static class DisplayStreamSample {
    internal static Table BuildTable(DefaultDisplayRegistration registration, DefaultDisplayContext context, IReadOnlyList<object> values) {
        return BuildTable(registration, context, values, out _);
    }
    internal static Table BuildTable(DefaultDisplayRegistration registration, DefaultDisplayContext context, IReadOnlyList<object> values, out IReadOnlyList<Table> prepared) {
        List<Table> rows = new List<Table>();
        Table? sample = null;
        foreach (object value in values) {
            if (!(registration.Build(new[] { value }, context) is Table row) || row.Rows.Count != 1 || row.Columns.Count == 0) {
                throw new InvalidOperationException("A Stream builder must return a Table with one row and stable columns.");
            }
            rows.Add(row);
            if (sample == null) {
                sample = new Table { Presentation = row.Presentation, Title = row.Title, Border = TableBorder.Rule, RecordViewRows = 0, CardsBelow = row.CardsBelow };
                foreach (TableColumn column in row.Columns) sample.AddColumn(column);
                sample.AddRow(row.Rows[0]);
                continue;
            }
            if (sample.Columns.Count != row.Columns.Count) { throw new InvalidOperationException("A Stream builder changed its column definitions."); }
            for (int index = 0; index < sample.Columns.Count; index++) {
                if (sample.Columns[index].Key != row.Columns[index].Key || sample.Columns[index].Header != row.Columns[index].Header) {
                    throw new InvalidOperationException("A Stream builder changed its column definitions.");
                }
            }
            sample.AddRow(row.Rows[0]);
        }
        prepared = rows;
        return sample ?? throw new InvalidOperationException("A Stream sample must contain at least one row.");
    }
}
