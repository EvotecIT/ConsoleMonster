using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Live;
using ConsoleMonster.Text;
using ConsoleMonster.Values;

namespace ConsoleMonster.Widgets {
    public sealed partial class LiveTable {
        /// <summary>The input field used as the row key; null uses the first column's key or header.</summary>
        public string? KeyProperty { get; set; }

        /// <summary>Adds or replaces one ordinary object row, preserving typed cell values.</summary>
        /// <param name="inputObject">The record or dictionary.</param>
        /// <param name="keyProperty">An explicit key field, otherwise KeyProperty or the first column.</param>
        /// <param name="shape">Host member adapters; null uses ordinary C# data adapters.</param>
        /// <returns>This table.</returns>
        public LiveTable Add(object inputObject, string? keyProperty = null, ShapeOptions? shape = null) {
            ConsoleTableRow row = Project(inputObject, keyProperty, shape);
            Set(row.Key, row.Values);
            return this;
        }

        // Shared with Update-Console -Row, so an ordinary record fills the same columns before and after the session starts.
        internal ConsoleTableRow Project(object inputObject, string? keyProperty, ShapeOptions? shape) {
            if (inputObject == null) { throw new ArgumentNullException(nameof(inputObject)); }
            string keyName = keyProperty ?? KeyProperty ?? (Table.Columns.Count == 0 ? string.Empty : ColumnKey(Table.Columns[0]));
            if (!ValueShaper.TryGetField(inputObject, keyName, out object? key, shape) || key == null || key is FieldError) {
                throw new ArgumentException("The row has no '" + keyName + "' value to use as its key. Give KeyProperty or a value for the first column.", nameof(inputObject));
            }
            var values = new List<object?>();
            foreach (TableColumn column in Table.Columns) {
                ValueShaper.TryGetField(inputObject, ColumnKey(column), out object? value, shape);
                values.Add(value);
            }
            return new ConsoleTableRow { Key = Convert.ToString(key, CultureInfo.CurrentCulture) ?? string.Empty, Values = values.ToArray() };
        }

        private static string ColumnKey(TableColumn column) => string.IsNullOrEmpty(column.Key) ? Markup.Strip(column.Header) : column.Key!;
    }
}
