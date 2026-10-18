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
            ConsoleViewOptions options = new ConsoleViewOptions { View = View, ChildProperty = ChildProperty, Chart = Chart.IsPresent, Title = PSText.Markup(Title), Depth = Depth };
            options.Sources.Add(new PSObjectMemberSource());
            List<object?> values = new List<object?>(_items.Count);
            foreach (PSObject item in _items) {
                values.Add(ObjectInspector.IsScalarValue(item.BaseObject) ? item.BaseObject : item);
            }
            ConsoleViewKind kind = ConsoleView.Decide(values, options);
            IWidget widget;
            switch (kind) {
                case ConsoleViewKind.Grid:
                    ShowGrid();
                    return;
                case ConsoleViewKind.Table:
                case ConsoleViewKind.Cards:
                    Table table = PSFormatting.BuildTable(_items, Property, null, false, true);
                    table.Title = options.Title;
                    if (kind == ConsoleViewKind.Cards) {
                        table.AlwaysCards = true;
                    }
                    widget = table;
                    break;
                case ConsoleViewKind.Inspector:
                    InspectorOptions inspect = PSFormatting.InspectorOptions(Depth, 50, null, null);
                    List<IWidget> views = new List<IWidget>();
                    foreach (PSObject item in _items) {
#pragma warning disable IL2026
                        views.Add(new InspectorView(ObjectInspector.Inspect(item, inspect), options.Title));
#pragma warning restore IL2026
                    }
                    widget = views.Count == 1 ? views[0] : new Rows(views.ToArray()) { Gap = 1 };
                    break;
                default:
                    options.View = kind;
                    widget = ConsoleView.Auto(values, options);
                    break;
            }
            PSFormatting.Write(this, widget, Width);
        }

        private void ShowGrid() {
            using (System.Management.Automation.PowerShell grid = System.Management.Automation.PowerShell.Create(RunspaceMode.CurrentRunspace)) {
                grid.AddCommand("ConsoleMonster\\Show-ConsoleGrid");
                if (Property != null) {
                    grid.AddParameter("Property", Property);
                }
                foreach (PSObject picked in grid.Invoke(_items)) {
                    WriteObject(picked);
                }
            }
        }
    }
}
