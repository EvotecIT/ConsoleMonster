using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.PowerShell.Binding;
using ConsoleMonster.Values;

namespace ConsoleMonster.PowerShell {
    /// <summary>Retains raw values while the direct-host native formatter evaluates its selected columns.</summary>
    public sealed class NativeFormatCapture {
        private readonly PSCmdlet _cmdlet;
        private readonly string _kind;
        private readonly IDictionary _parameters;
        private readonly List<BoundColumn> _columns = new List<BoundColumn>();
        private readonly ShapeBudget _budget = ShapeBudget.For(PSShaping.Options());
        private object?[]? _current;
        private bool[]? _read;
        private bool _retained;
        private NativeFormatDefinition? _definition;
        private IReadOnlyList<BoundColumn>? _selected;
        internal readonly List<object?[]> Rows = new List<object?[]>();
        internal readonly List<PSObject> Sources = new List<PSObject>();
        internal readonly List<bool> Formatted = new List<bool>();
        internal IReadOnlyList<BoundColumn> Columns => _columns;
        internal ConsoleViewRegistration? Presentation { get; private set; }

        /// <summary>Creates the capture for one host-only formatter invocation.</summary>
        /// <param name="cmdlet">The executing proxy.</param><param name="kind">Table or List.</param><param name="parameters">Native bound parameters.</param>
        public NativeFormatCapture(PSCmdlet cmdlet, string kind, IDictionary parameters) {
            _cmdlet = cmdlet; _kind = kind;
            _parameters = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in parameters) { _parameters[entry.Key] = entry.Value; }
        }

        /// <summary>Native parameters with value-capturing expressions; unsupported controls keep their original parameters.</summary>
        /// <param name="first">First source object.</param><returns>The native parameter splat.</returns>
        public Hashtable Prepare(PSObject first) {
            Hashtable parameters = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in _parameters) { parameters[entry.Key] = entry.Value; }
            ConsoleViewRegistration? view = ConsoleFormatting.Find(first, ConsoleFormatting.TakeOverBuiltIns);
            bool decorate = view?.NativePresentation == true && !parameters.ContainsKey("Property") && !parameters.ContainsKey("View");
            // Resolve the selected native view before adding defaults. Its own grouping (including custom headers)
            // must retain the native route unless the caller explicitly supplied -GroupBy.
            NativeFormatDefinition? definition = _definition = NativeFormatDefinition.Resolve(_cmdlet, _kind, first, parameters);
            if (definition == null) { return parameters; }
            Presentation = decorate && _kind == "Table" ? view : null;
            if (decorate && !parameters.ContainsKey("GroupBy") && view!.GroupBy != null) { parameters["GroupBy"] = view.GroupBy; }
            IReadOnlyList<BoundColumn>? presentation = decorate ? PropertyBinder.Bind(new[] { first }, view!.Property) : null;
            List<object> properties = new List<object>();
            foreach (IDictionary original in definition.Columns) {
                Hashtable column = new Hashtable(StringComparer.OrdinalIgnoreCase);
                foreach (DictionaryEntry entry in original) { column[entry.Key] = entry.Value; }
                // Bind before replacing the expression; native formatting still owns the FormatString text.
                BoundColumn bound = PropertyBinder.Bind(new[] { first }, new object[] { original })[0];
                if (presentation != null) {
                    foreach (BoundColumn candidate in presentation) {
                        string? source = bound.SourceProperty(first);
                        if (string.Equals(bound.Name, candidate.Name, StringComparison.OrdinalIgnoreCase)
                            || (source != null && string.Equals(source, candidate.SourceProperty(first), StringComparison.OrdinalIgnoreCase))) {
                            bound.Column.Presenter = candidate.Column.Presenter;
                            break;
                        }
                    }
                }
                Formatted.Add(!string.IsNullOrEmpty(bound.FormatString));
                bound.FormatString = null;
                _columns.Add(bound);
                int index = _columns.Count - 1;
                ScriptBlock expression = (ScriptBlock)_cmdlet.InvokeCommand.InvokeScript(
                    "param($capture,$index) { $capture.Read($_, $index) }.GetNewClosure()", true,
                    System.Management.Automation.Runspaces.PipelineResultTypes.None, null, this, index)[0].BaseObject;
                column.Remove("E");
                column["Expression"] = expression;
                properties.Add(column);
            }
            parameters.Remove("View");
            parameters["Property"] = properties.ToArray();
            if (_kind == "Table" && definition.Wrap) { parameters["Wrap"] = true; }
            if (_kind == "Table" && !parameters.ContainsKey("HideTableHeaders") && definition.HideTableHeaders) { parameters["HideTableHeaders"] = true; }
            if (_kind == "Table" && !parameters.ContainsKey("AutoSize") && definition.AutoSize) { parameters["AutoSize"] = true; }
            return parameters;
        }

        /// <summary>Starts one source item; its evaluated cells keep the original object identity and typed values.</summary>
        /// <param name="item">The source item.</param>
        public void BeginItem(PSObject item) {
            if (_columns.Count == 0) { return; }
            _current = new object?[_columns.Count];
            _read = new bool[_columns.Count];
            _retained = false;
            List<BoundColumn> selected = new List<BoundColumn>();
            foreach (IDictionary spec in _definition!.ColumnsFor(item)) {
                BoundColumn bound = PropertyBinder.Bind(new[] { item }, new object[] { spec })[0];
                bound.FormatString = null;
                selected.Add(bound);
            }
            _selected = selected;
        }

        /// <summary>Reads a selected property once, within the shared slow-getter budget.</summary>
        /// <param name="item">The source object supplied by PowerShell.</param><param name="column">Selected column index.</param><returns>The original evaluated value.</returns>
        public object? Read(PSObject item, int column) {
            if (_current == null || _read == null) { return null; }
            if (!_retained) { Rows.Add(_current); Sources.Add(item); _retained = true; }
            if (!_read[column]) { _current[column] = PSFormatting.Read(_selected![column], item, _budget, _columns[column]); _read[column] = true; }
            return _current[column];
        }
    }
}
