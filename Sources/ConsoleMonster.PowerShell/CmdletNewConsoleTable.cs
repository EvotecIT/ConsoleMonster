using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a console table from pipeline objects.</para>
    /// <para type="description">Columns come from -Property or from the first object's properties (or hashtable keys). Columns size to their content, numbers are right-aligned, long values wrap. A -StatusProperty column shows status markers (pass, fail, warn...) as the row's one colour signal.</para>
    /// <example>
    ///   <para>Services as a table with a status column</para>
    ///   <code>Get-Service | Select-Object -First 8 Name, Status, StartType | New-ConsoleTable -Title 'Services' | Write-ConsoleWidget</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleTable")]
    [OutputType(typeof(Table))]
    public class CmdletNewConsoleTable : PSCmdlet {
        private readonly List<PSObject> _items = new List<PSObject>();

        /// <summary><para type="description">The objects to show, one row each.</para></summary>
        [Parameter(ValueFromPipeline = true, Position = 0)]
        public PSObject[]? InputObject { get; set; }

        /// <summary><para type="description">Properties to show, in order.</para></summary>
        [Parameter]
        public string[]? Property { get; set; }

        /// <summary><para type="description">A property whose values are statuses (pass/fail/warn/skip/running).</para></summary>
        [Parameter]
        public string? StatusProperty { get; set; }

        /// <summary><para type="description">A title above the table.</para></summary>
        [Parameter]
        public string? Title { get; set; }

        /// <summary><para type="description">A muted caption below the table.</para></summary>
        [Parameter]
        public string? Caption { get; set; }

        /// <summary><para type="description">Border: Rounded, Square, Double, Minimal or None.</para></summary>
        [Parameter]
        public TableBorder Border { get; set; } = TableBorder.Rounded;

        /// <summary><para type="description">Fill the available width (spare width goes to the widest text column).</para></summary>
        [Parameter]
        public SwitchParameter Expand { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _items.AddRange(InputObject);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Emit(BuildTable());
        }

        /// <summary>Builds the table from the collected input.</summary>
        /// <returns>The table.</returns>
        protected Table BuildTable() {
            Table table = PSTableMapper.Build(_items, Property, StatusProperty);
            table.Title = Title;
            table.Caption = Caption;
            table.Border = Border;
            table.Expand = Expand.IsPresent;
            return table;
        }

        /// <summary>Emits the built table (Write-ConsoleTable overrides this to write it).</summary>
        /// <param name="table">The table.</param>
        protected virtual void Emit(Table table) => WriteObject(table);
    }
}
