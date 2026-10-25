using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>PowerShell member selection and registered-view settings shared by construction and display.</summary>
    public sealed class ConsoleTableViewOptions {
        /// <summary>Property names, patterns or calculated column records.</summary>
        public object[]? Property { get; set; }
        /// <summary>A property or owning-runspace script block used for grouping.</summary>
        public object? GroupBy { get; set; }
        /// <summary>Includes numeric totals.</summary>
        public bool ShowTotals { get; set; }
        /// <summary>The culture for field values.</summary>
        public CultureInfo? Culture { get; set; }
        /// <summary>Retains the grouped field as a column.</summary>
        public bool ShowGroupColumn { get; set; }
        /// <summary>Preserves group input order rather than sorting it.</summary>
        public bool NoSort { get; set; }
    }

    /// <summary>Reusable C# access to PowerShell's calculated columns, registered views and object adaptation. Script blocks run on the caller's owning runspace thread.</summary>
    public static partial class ConsoleHostViews {
        /// <summary>Constructs one table model from PowerShell objects using the same binding as New-ConsoleTable.</summary>
        /// <param name="inputObject">Collected input rows.</param>
        /// <param name="options">Member, grouping and presentation settings.</param>
        /// <param name="source">Optional owning cmdlet for native view discovery and diagnostics.</param>
        /// <returns>The reusable table model.</returns>
        public static Table TableModel(IReadOnlyList<PSObject> inputObject, ConsoleTableViewOptions? options = null, PSCmdlet? source = null) {
            return TableModelCore(inputObject, options, source, null);
        }

        // Live tables retain these evaluated rows, including calculated expressions and FormatString results.
        internal static Table TableModelCore(IReadOnlyList<PSObject> inputObject, ConsoleTableViewOptions? options, PSCmdlet? source, Action<PSObject, object?[]>? boundRow) {
            if (inputObject == null) { throw new ArgumentNullException(nameof(inputObject)); }
            options ??= new ConsoleTableViewOptions();
            Values.ShapeOptions shape = Binding.PSShaping.Options(options.Culture);
            return PSFormatting.BuildTable(PSFormatting.Expand(inputObject, shape), options.Property, options.GroupBy, BuildOptions(options, source), shape, boundRow);
        }

        /// <summary>Constructs the registered table view, preserving consecutive type groups and dictionary record views.</summary>
        /// <param name="inputObject">Collected input rows.</param>
        /// <param name="options">Member, grouping and presentation settings.</param>
        /// <param name="configure">Optional configuration of each constructed table before it is displayed.</param>
        /// <param name="source">Optional owning cmdlet for native view discovery and diagnostics.</param>
        /// <returns>One table or the ordered composition of its registered views.</returns>
        public static IWidget Table(IReadOnlyList<PSObject> inputObject, ConsoleTableViewOptions? options = null, Action<Table>? configure = null, PSCmdlet? source = null) {
            if (inputObject == null) { throw new ArgumentNullException(nameof(inputObject)); }
            options ??= new ConsoleTableViewOptions();
            return PSFormatting.Combine(PSFormatting.BuildTables(inputObject, options.Property, options.GroupBy, BuildOptions(options, source), configure));
        }

        /// <summary>Constructs the record/list representation with PowerShell member and grouping selectors.</summary>
        /// <param name="inputObject">Collected records.</param>
        /// <param name="property">Member selectors.</param>
        /// <param name="groupBy">Optional grouping selector.</param>
        /// <param name="showEmpty">Includes empty values.</param>
        /// <param name="depth">Maximum nested field depth.</param>
        /// <param name="title">Optional markup title.</param>
        /// <param name="culture">Value culture.</param>
        /// <param name="source">Optional owning cmdlet for native view discovery and diagnostics.</param>
        /// <returns>The record view or scalar text.</returns>
        public static IWidget List(IReadOnlyList<PSObject> inputObject, object[]? property = null, object? groupBy = null, bool showEmpty = false, int depth = 3, string? title = null, CultureInfo? culture = null, PSCmdlet? source = null) {
            if (inputObject == null) { throw new ArgumentNullException(nameof(inputObject)); }
            return PSFormatting.BuildList(inputObject, property, groupBy, showEmpty, depth, title, culture, source);
        }

        /// <summary>Constructs a hierarchy from typed nodes, Label/Children records, dictionaries or explicit child selectors.</summary>
        /// <param name="inputObject">Root records.</param>
        /// <param name="options">Label, child and traversal configuration.</param>
        /// <returns>The tree model.</returns>
        public static Tree Tree(IReadOnlyList<object> inputObject, ConsoleTreeOptions? options = null) {
            if (inputObject == null) { throw new ArgumentNullException(nameof(inputObject)); }
            return PSTree.Build(inputObject, options ?? new ConsoleTreeOptions());
        }

        private static TableBuildOptions BuildOptions(ConsoleTableViewOptions options, PSCmdlet? source) => new TableBuildOptions {
            Total = options.ShowTotals, Culture = options.Culture, ShowGroupColumn = options.ShowGroupColumn, NoSort = options.NoSort, Cmdlet = source
        };
    }
}
