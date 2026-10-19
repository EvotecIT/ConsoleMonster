using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Data;
using ConsoleMonster.Layout;
using ConsoleMonster.PowerShell.Binding;
using ConsoleMonster.Views;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows pipeline input in the view that fits it best.</para>
    /// <para type="description">One object becomes an inspector; objects of one shape a table (cards when the window is narrow), using their DefaultDisplayPropertySet and presenters by type (bytes for Length and Size, dates, durations, Yes/No, status markers for enums such as Running/Stopped); hierarchical objects (a child collection of the same type, or -ChildProperty) a tree; a numeric series a bar chart; strings highlighted code when a language is detected, otherwise text. -View overrides the choice; Grid opens the interactive Show-ConsoleGrid and returns what you pick.</para>
    /// <example>
    ///   <para>Let it choose</para>
    ///   <code>Get-ChildItem | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>A tree of folders by their children</para>
    ///   <code>$tree | Show-Console -ChildProperty Children</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Show, "Console")]
    public sealed class CmdletShowConsole : PSCmdlet {
        private readonly List<PSObject> _items = new List<PSObject>();

        /// <summary><para type="description">The objects to show.</para></summary>
        [Parameter(ValueFromPipeline = true, Position = 0)]
        [AllowNull]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">The view instead of the automatic choice: Table, Cards, Tree, Inspector, Chart, Code, Text or Grid.</para></summary>
        [Parameter]
        public ConsoleViewKind? View { get; set; }

        /// <summary><para type="description">The properties for a table, as in Format-ConsoleTable (names, wildcards, calculated properties).</para></summary>
        [Parameter]
        public object[]? Property { get; set; }

        /// <summary><para type="description">The property that holds each object's children, for a tree.</para></summary>
        [Parameter]
        public string? ChildProperty { get; set; }

        /// <summary><para type="description">Shows objects as a bar chart (label from the first text property, value from the first number).</para></summary>
        [Parameter]
        public SwitchParameter Chart { get; set; }

        /// <summary><para type="description">A title (markup).</para></summary>
        [Parameter]
        public object? Title { get; set; }

        /// <summary><para type="description">The width in cells (default the window).</para></summary>
        [Parameter]
        public int Width { get; set; }

        /// <summary><para type="description">The deepest level an inspector or tree shows (default 3).</para></summary>
        [Parameter]
        public int Depth { get; set; } = 3;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _items.Add(InputObject);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_items.Count == 0) {
                return;
            }
            Values.ShapeOptions shape = PSShaping.Options();
            IReadOnlyList<PSObject> items = PSFormatting.Expand(_items, shape);
            ConsoleViewOptions options = new ConsoleViewOptions { View = View, ChildProperty = ChildProperty, Chart = Chart.IsPresent, Title = PSText.Markup(Title), Depth = Depth };
            options.Sources.Add(new DirectoryEntryMemberSource());
            options.Sources.Add(new PSObjectMemberSource());
            List<object?> values = new List<object?>(items.Count);
            foreach (PSObject item in items) {
                values.Add(PSShaping.IsValueLike(item, shape) ? Values.ValueShaper.Prepare(item, shape) : item);
            }
            ConsoleViewKind kind = ConsoleView.Decide(values, options);
            bool requested = View.HasValue;
            IWidget widget;
            switch (kind) {
                case ConsoleViewKind.Grid:
                    ShowGrid(items);
                    return;
                case ConsoleViewKind.Table:
                case ConsoleViewKind.Cards:
                    widget = Tables(items, kind == ConsoleViewKind.Cards, options.Title);
                    break;
                case ConsoleViewKind.Inspector when requested:
                    List<IWidget> views = new List<IWidget>();
                    foreach (PSObject item in items) {
#pragma warning disable IL2026
                        views.Add(new InspectorView(ObjectInspector.Inspect(item, PSFormatting.InspectorOptions(Depth, 50, null, null, 5000, item)), options.Title));
#pragma warning restore IL2026
                    }
                    widget = views.Count == 1 ? views[0] : new Rows(views.ToArray()) { Gap = 1 };
                    break;
                case ConsoleViewKind.Inspector:
                    // One object is a record (its fields, nested records expanded one level); objects of different kinds are
                    // one table per kind. The inspector (the whole object graph) is only shown on request: -View Inspector.
                    widget = items.Count == 1
                        ? PSFormatting.BuildList(items, Property, null, false, 1, options.Title, null, this)
                        : Tables(items, false, options.Title);
                    break;
                default:
                    options.View = kind;
                    widget = ConsoleView.Auto(values, options);
                    break;
            }
            PSFormatting.Write(this, widget, Width);
        }

        private IWidget Tables(IReadOnlyList<PSObject> items, bool cards, string? title) {
            bool first = true;
            List<IWidget> tables = PSFormatting.BuildTables(items, Property, null, new TableBuildOptions { AutoPresenters = true, Cmdlet = this }, table => {
                if (title != null) {
                    table.Title = first ? title : null;
                }
                table.AlwaysCards = cards;
                first = false;
            });
            return PSFormatting.Combine(tables);
        }
        private void ShowGrid(IReadOnlyList<PSObject> items) {
            using (System.Management.Automation.PowerShell grid = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)) {
                grid.AddCommand("ConsoleMonster\\Show-ConsoleGrid");
                if (Property != null) {
                    grid.AddParameter("Property", Property);
                }
                foreach (PSObject picked in grid.Invoke(items)) {
                    WriteObject(picked);
                }
            }
        }
    }
}
