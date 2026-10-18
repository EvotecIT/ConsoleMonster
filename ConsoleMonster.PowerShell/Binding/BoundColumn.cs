using System;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>A configured table column bound to a PowerShell property or expression.</summary>
    public sealed class BoundColumn {
        private readonly object _expression;

        /// <summary>Creates a bound column.</summary>
        public BoundColumn(string name, TableColumn column, object expression) {
            Name = name;
            Column = column;
            _expression = expression;
        }

        /// <summary>Logical property name used to bind this column.</summary>
        public string Name { get; }

        /// <summary>Configured rendering column.</summary>
        public TableColumn Column { get; }

        /// <summary>First evaluation error encountered, or null.</summary>
        public string? LastError { get; private set; }

        /// <summary>Evaluates this column for an item, returning null for a missing property or failed expression.</summary>
        public object? GetValue(PSObject item) {
            try {
                return PropertyBinder.Evaluate(item, _expression);
            }
            catch (Exception ex) when (!(ex is System.Management.Automation.PipelineStoppedException) && !(ex is OperationCanceledException)) {
                // Ctrl+C (PipelineStoppedException) must stop the cmdlet, not blank the column.
                LastError ??= ex.Message;
                return null;
            }
        }
    }
}
