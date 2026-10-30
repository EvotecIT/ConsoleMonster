using System;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Values;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>A table column bound to a property name or calculated property expression.</summary>
    internal sealed class BoundColumn {
        private readonly object _expression;

        /// <summary>Creates a bound column.</summary>
        public BoundColumn(string name, TableColumn column, object expression) {
            Name = name;
            Column = column;
            _expression = expression;
        }

        /// <summary>The source property name or calculated property label.</summary>
        public string Name { get; }

        /// <summary>The rendering column configuration.</summary>
        public TableColumn Column { get; }

        /// <summary>The first expression error, if one occurred while reading values.</summary>
        public string? LastError { get; private set; }

        /// <summary>A .NET format string for the values (FormatString in a calculated property, as Format-Table takes), or null.</summary>
        public string? FormatString { get; internal set; }

        internal ShapeOptions? Options { get; set; }

        // Reuse the expression and formatting without retaining a previous shaping pass's input caches.
        internal BoundColumn WithOptions(ShapeOptions options) => new BoundColumn(Name, Column, _expression) {
            FormatString = FormatString,
            Options = options
        };

        // Formatting turns numbers into text. Retain their alignment from the first raw value without guessing that
        // every FormatString is numeric; explicit alignment always wins. The native capture calls the same step.
        internal void InferFormattedAlignment(object? value) {
            if (Column.Alignment.HasValue || value == null) { return; }
            bool numeric = !(value is string) && (Presenters.PresenterValues.TryGetNumber(value, out _) || Presenters.PresenterValues.IsNonFinite(value));
            Column.Alignment = numeric ? Layout.HorizontalAlignment.Right : Layout.HorizontalAlignment.Left;
        }

        /// <summary>
        /// The property the column's values come from on <paramref name="sample"/>, past alias properties: a property name,
        /// or a script block that only reads one property (<c>{ $_.Name }</c>, as format views often define columns).
        /// Null for any other calculated column.
        /// </summary>
        /// <param name="sample">An item to resolve alias properties on, or null.</param>
        /// <returns>The source property name, or null.</returns>
        public string? SourceProperty(PSObject? sample) => PropertyBinder.SourceProperty(_expression, sample);

        /// <summary>
        /// Evaluates the column against one item. A getter or script block that throws gives a <see cref="FieldError"/>,
        /// shown as a warning marker in the cell, instead of an empty cell or a lost record; Ctrl+C still stops the command.
        /// </summary>
        public object? GetValue(PSObject item) => GetValue(item, out _);

        // The unformatted identity is captured from the same evaluation as the display value.
        internal object? GetValue(PSObject item, out object? identity) {
            object? value;
            try {
                value = PropertyBinder.Evaluate(item, _expression, Options);
            }
            catch (Exception ex) when (!(ex is System.Management.Automation.PipelineStoppedException) && !(ex is OperationCanceledException)) {
                // Ctrl+C (PipelineStoppedException) must stop the cmdlet, not mark the cell.
                LastError ??= ex.Message;
                identity = FieldError.From(ex);
                return identity;
            }
            identity = value;
            if (value is FieldError error) {
                LastError ??= error.Message;
                return value;
            }
            if (!string.IsNullOrEmpty(FormatString)) { InferFormattedAlignment(value); }
            if (!string.IsNullOrEmpty(FormatString) && value is IFormattable formattable) {
                CultureInfo culture = Options?.ResolvedCulture ?? CultureInfo.CurrentCulture;
                try {
                    return FormatString!.IndexOf("{0", StringComparison.Ordinal) >= 0
                        ? string.Format(culture, FormatString!, value)
                        : formattable.ToString(FormatString, culture);
                } catch (FormatException) {
                    return value;
                }
            }
            return value;
        }
    }
}
