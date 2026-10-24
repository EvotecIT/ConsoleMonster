using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Formats pipeline objects as record views (labelled values) and writes them to the host.</para>
    /// <para type="description">Each object becomes a section titled by its key column (the first property, or one marked Key = $true) with the other properties as aligned label/value fields; empty values are hidden unless -ShowEmpty is given. Values go through presenters (statuses, sizes, durations, dates...), and complex values (nested objects, collections) expand as indented sections and nested tables down to -Depth levels. -Property works like Format-List, including calculated properties and the column option keys of Show-ConsoleTable. Redirected output is plain text.</para>
    /// </summary>
    /// <example>
    ///   <para>A process as a record</para>
    ///   <code>Get-Process -Id $PID | Show-ConsoleList -Property Name, Id, @{ Name = 'WorkingSet64'; Presenter = 'Bytes' }, StartTime, Modules -Depth 1</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleList")]

    public sealed class CmdletShowConsoleList : PSCmdlet {
        private readonly List<PSObject> _items = new List<PSObject>();

        /// <summary><para type="description">The objects to show (pipeline input).</para></summary>
        [Parameter(ValueFromPipeline = true, Position = 0)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Properties (names, wildcards, calculated properties and column option hashtables).</para></summary>
        [Parameter]
        public object[]? Property { get; set; }

        /// <summary><para type="description">Groups records under headings by a property name or a scriptblock.</para></summary>
        [Parameter]
        public object? GroupBy { get; set; }

        /// <summary><para type="description">A title on the summary line (markup).</para></summary>
        [Parameter]
        public object? Title { get; set; }

        /// <summary><para type="description">Shows empty values (hidden by default).</para></summary>
        [Parameter]
        public SwitchParameter ShowEmpty { get; set; }

        /// <summary><para type="description">How many levels of nested objects expand (default 1; 0 shows nested values as text).</para></summary>
        [Parameter]
        public int Depth { get; set; } = 1;

        /// <summary><para type="description">Lays the records out for this width instead of the terminal's.</para></summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int Width { get; set; }

        /// <summary><para type="description">The culture for numbers and dates, for example en-US or pl-PL (labels stay English). Defaults to CONSOLEMONSTER_CULTURE, then the current culture, as PowerShell's own formatting does.</para></summary>
        [Parameter]
        public System.Globalization.CultureInfo? Culture { get; set; }

        /// <summary><para type="description">Writes the view as one string of plain text instead of drawing it, to keep it in a variable or a file from any script, function or PowerShell version. Without it, PowerShell 7.2+ captures a view typed at the prompt ($x = ..., &gt; file, Out-File); inside scripts and functions, and on Windows PowerShell 5.1, the view is drawn on the screen. Piped into another command (Out-Host, Out-String, Select-String) the view goes on as plain lines, without colour. Cannot be combined with -PassThru.</para></summary>
        [Parameter]
        public SwitchParameter AsString { get; set; }
        /// <summary><para type="description">Also writes the input objects to the pipeline.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }


        /// <summary><para type="description">Returns the record view without drawing it.</para></summary>
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
            if (_items.Count == 0) {
                return;
            }
            Layout.IWidget view = PSFormatting.BuildList(_items, Property, GroupBy, ShowEmpty.IsPresent, Depth, PSText.Markup(Title), Culture, this);
            if (AsWidget.IsPresent) { WriteObject(view); return; }
            PSFormatting.Emit(this, view, Width, Culture, AsString.IsPresent, PassThru.IsPresent);
            if (PassThru.IsPresent) {
                foreach (PSObject item in _items) {
                    WriteObject(item);
                }
            }
        }
    }
}
