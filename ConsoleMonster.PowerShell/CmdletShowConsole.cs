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
    /// </summary>
    /// <example>
    ///   <para>Let it choose</para>
    ///   <code>Get-ChildItem | Show-Console</code>
    /// </example>
    /// <example>
    ///   <para>A tree of folders by their children</para>
    ///   <code>$tree | Show-Console -ChildProperty Children</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "Console")]
    public sealed class CmdletShowConsole : PSCmdlet {
        private readonly List<PSObject> _items = new List<PSObject>();

        /// <summary><para type="description">The objects to show.</para></summary>
        [Parameter(ValueFromPipeline = true, Position = 0)]
        [AllowNull]
        public PSObject? InputObject { get; set; }


        /// <summary><para type="description">The properties for a table, as in Show-ConsoleTable (names, wildcards, calculated properties).</para></summary>
        [Parameter]
        public object[]? Property { get; set; }



        /// <summary><para type="description">A title (markup).</para></summary>
        [Parameter]
        public object? Title { get; set; }

        /// <summary><para type="description">The width in cells (default the window).</para></summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int Width { get; set; }

        /// <summary><para type="description">The deepest level an inspector or tree shows (default 3).</para></summary>
        [Parameter]
        public int Depth { get; set; } = 3;

        /// <summary><para type="description">Writes the view as one string of plain text instead of drawing it, to keep it in a variable or a file from any script, function or PowerShell version. Without it, Show-Console behaves like the Format-Console commands: PowerShell 7.2+ captures a view typed at the prompt ($x = ..., &gt; file, Out-File), piped into another command the view goes on as plain lines, and inside scripts and functions or on Windows PowerShell 5.1 it is drawn on the screen.</para></summary>
        [Parameter]
        public SwitchParameter AsString { get; set; }

        /// <summary><para type="description">Returns the computed widget without drawing it.</para></summary>
        [Parameter] public SwitchParameter AsWidget { get; set; }

        /// <inheritdoc />
        protected override void BeginProcessing() => PSOutputExport.Validate(this);

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _items.Add(InputObject);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_items.Count == 0) { return; }
            List<object> items = new List<object>(_items);
            IWidget widget = PSOutputExport.Widget(items, this, Property, PSText.Markup(Title), Depth);
            if (AsWidget.IsPresent) { WriteObject(widget); return; }
            bool suppliedWidget = _items.Exists(item => item.BaseObject is IWidget || item.BaseObject is Canvas.CellBuffer || item.BaseObject is Live.LiveSession);
            PSFormatting.Emit(this, widget, Width, null, AsString.IsPresent, appendBlankLine: !suppliedWidget);
        }
    }
}
