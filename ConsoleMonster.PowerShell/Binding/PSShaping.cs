using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Presenters;
using ConsoleMonster.Values;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell.Binding {
    /// <summary>
    /// The PowerShell side of the shared value rules (<see cref="ValueShaper"/>): PowerShell objects read through their
    /// PowerShell properties, PowerShell type names for registered shapers, and script-block shapers from
    /// Register-ConsoleView -Shaper. Everything else (shapes, fields, titles, cell text, collections) is the core's.
    /// </summary>
    internal static class PSShaping {
        /// <summary>Shape options for PowerShell objects.</summary>
        /// <param name="culture">The culture, or null for the current one.</param>
        /// <returns>The options.</returns>
        internal static ShapeOptions Options(CultureInfo? culture = null) {
            ShapeOptions options = new ShapeOptions {
                Unwrap = PSObjectMemberSource.UnwrapPSObject,
                Identity = PSObjectMemberSource.Identity,
                TypeNames = TypeNamesOf,
                Culture = culture
            };
            options.Sources.Add(new DirectoryEntryMemberSource());
            options.Sources.Add(new PSObjectMemberSource());
            return options;
        }

        private static IEnumerable<string> TypeNamesOf(object value) => value is PSObject item ? (IEnumerable<string>)item.TypeNames : Array.Empty<string>();

        /// <summary>Whether an item is shown as a value (a scalar, or a collection) rather than as a record with fields.</summary>
        /// <param name="item">The item.</param>
        /// <param name="options">Shape options.</param>
        /// <returns>True for a value.</returns>
        internal static bool IsValueLike(object? item, ShapeOptions options) {
            object? prepared = ValueShaper.Prepare(item, options);
            if (prepared == null || ValueShaper.IsScalar(prepared)) {
                return true;
            }
            return !ValueShaper.IsStructural(prepared) && ValueShaper.TryGetItems(prepared, out _);
        }

        /// <summary>
        /// The cell value for a column: scalars stay typed for presenters, everything else becomes its shared text (joined
        /// items, Key=Value pairs, a record's title, a masked secret, a warning marker for a failed getter).
        /// </summary>
        /// <param name="value">The evaluated value.</param>
        /// <param name="column">The column.</param>
        /// <param name="options">Shape options.</param>
        /// <param name="budget">The pass budget.</param>
        /// <returns>The cell value.</returns>
        internal static object? Cell(object? value, TableColumn column, ShapeOptions options, ShapeBudget budget) {
            if (value is TableCell || value is Text.RichText) {
                return value;
            }
            return ValueShaper.Cell(value, options, budget);
        }

        /// <summary>The title of a record (the shared title rule, with an Id after a name).</summary>
        /// <param name="item">The record.</param>
        /// <param name="options">Shape options.</param>
        /// <param name="budget">The pass budget, or null.</param>
        /// <returns>The title.</returns>
        internal static string Title(object item, ShapeOptions options, ShapeBudget? budget = null) => ValueShaper.Title(item, options, budget);

        /// <summary>Registers a script block that turns objects of a type name into what is shown instead (Register-ConsoleView -Shaper).</summary>
        /// <param name="typeName">The type name (PowerShell type names included).</param>
        /// <param name="shaper">The script block; $_ and its first argument are the object.</param>
        internal static void RegisterShaper(string typeName, ScriptBlock shaper) {
            System.Management.Automation.Runspaces.Runspace? owner = System.Management.Automation.Runspaces.Runspace.DefaultRunspace;
            ValueShaper.Register(typeName, value => Invoke(shaper, value, owner));
        }

        private static object? Invoke(ScriptBlock shaper, object value, System.Management.Automation.Runspaces.Runspace? owner) {
            // A script block belongs to the runspace that defined it: other runspaces (thread jobs, ForEach-Object -Parallel)
            // show the object as it is instead of running it on the wrong thread.
            if (owner != null && !ReferenceEquals(System.Management.Automation.Runspaces.Runspace.DefaultRunspace, owner)) {
                return value;
            }
            try {
                System.Collections.ObjectModel.Collection<PSObject> output = shaper.InvokeWithContext(null, new List<PSVariable> { new PSVariable("_", value) }, value);
                if (output.Count == 0) {
                    return value;
                }
                if (output.Count == 1) {
                    return output[0];
                }
                return new List<PSObject>(output);
            } catch (Exception ex) when (!(ex is PipelineStoppedException) && !(ex is OperationCanceledException)) {
                // A failing shaper never breaks the output: the object is shown as it is.
                return value;
            }
        }

        /// <summary>Whether a value of a presenter-formatted column is a plain scalar the presenter can format.</summary>
        /// <param name="value">The value.</param>
        /// <returns>True for a scalar.</returns>
        internal static bool IsPresentable(object? value) => value == null || ValueShaper.IsScalar(value) || PresenterValues.TryGetNumber(value, out _);
    }
}
