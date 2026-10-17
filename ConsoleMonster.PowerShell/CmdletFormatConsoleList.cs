using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Formats pipeline objects as record views (labelled values) and writes them to the host.</para>
    /// <para type="description">Each object becomes a section titled by its key column (the first property, or one marked Key = $true) with the other properties as aligned label/value fields; empty values are hidden unless -ShowEmpty is given. Values go through presenters (statuses, sizes, durations, dates...), and complex values (nested objects, collections) expand as indented sections and nested tables down to -Depth levels. -Property works like Format-List, including calculated properties and the column option keys of Format-ConsoleTable. Redirected output is plain text.</para>
    /// <example>
    ///   <para>A process as a record</para>
    ///   <code>Get-Process -Id $PID | Format-ConsoleList -Property Name, Id, @{ Name = 'WorkingSet64'; Presenter = 'Bytes' }, StartTime, Modules -Depth 1</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "ConsoleList")]
    public sealed class CmdletFormatConsoleList : PSCmdlet {
        private readonly List<PSObject> _items = new List<PSObject>();

        /// <summary><para type="description">The objects to show (pipeline input).</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Properties (names, wildcards, calculated properties and column option hashtables).</para></summary>
        [Parameter(Position = 0)]
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
        public int Width { get; set; }

        /// <summary><para type="description">The culture for numbers and dates, for example en-US or pl-PL (labels stay English). Defaults to CONSOLEMONSTER_CULTURE, then the current culture, as PowerShell's own formatting does.</para></summary>
        [Parameter]
        public System.Globalization.CultureInfo? Culture { get; set; }

        /// <summary><para type="description">Also writes the input objects to the pipeline.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

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
            RecordView view = PSFormatting.BuildList(_items, Property, GroupBy, ShowEmpty.IsPresent, Depth, PSText.Markup(Title), Culture);
            PSFormatting.Write(this, view, Width, Culture);
            if (PassThru.IsPresent) {
                foreach (PSObject item in _items) {
                    WriteObject(item);
                }
            }
        }
    }
}
