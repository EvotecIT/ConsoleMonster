using System;
using System.Management.Automation;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a table whose rows change while a dashboard runs.</para>
    /// <para type="description">Rows have stable keys and are added, updated, removed or cleared with Update-ConsoleTable. Column widths only grow, so rows never jitter. -MaxRows keeps only the most recent rows; -SortBy keeps rows sorted by a column.</para>
    /// </summary>
    /// <example>
    ///   <para>The 20 most recent jobs, newest state first</para>
    ///   <code>New-ConsoleLiveTable -Name jobs -Column 'Job', 'State', 'Duration' -MaxRows 20</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleLiveTable")]
    [OutputType(typeof(LiveTable))]
    public sealed class CmdletNewConsoleLiveTable : PSCmdlet {
        /// <summary><para type="description">The live name used by Update-ConsoleTable (default table).</para></summary>
        [Parameter(Position = 0)]
        public string Name { get; set; } = "table";

        /// <summary><para type="description">The column headers, in order.</para></summary>
        [Parameter]
        public string[]? Column { get; set; }

        /// <summary><para type="description">A table that defines the columns and options instead (for example from New-ConsoleTable); its rows are replaced.</para></summary>
        [Parameter]
        public Table? Table { get; set; }

        /// <summary><para type="description">A title above the table (markup).</para></summary>
        [Parameter]
        public object? Title { get; set; }

        /// <summary><para type="description">The border style.</para></summary>
        [Parameter]
        public TableBorder Border { get; set; } = TableBorder.Rounded;

        /// <summary><para type="description">Keeps only the most recently added rows (0 keeps all).</para></summary>
        [Parameter]
        public int MaxRows { get; set; }

        /// <summary><para type="description">The column (header or index) to keep rows sorted by.</para></summary>
        [Parameter]
        public object? SortBy { get; set; }

        /// <summary><para type="description">Sorts from the largest value down.</para></summary>
        [Parameter]
        public SwitchParameter Descending { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Table table = Table ?? new Table { Border = Border, Title = PSText.Markup(Title) };
            if (Table == null) {
                if (Column == null || Column.Length == 0) {
                    ThrowTerminatingError(new ErrorRecord(new PSArgumentException("Give the columns with -Column or a table with -Table."), "ConsoleMonsterNoColumns", ErrorCategory.InvalidArgument, null));
                    return;
                }
                foreach (string header in Column) {
                    table.AddColumn(Text.Markup.Escape(header));
                }
            }
            LiveTable live = new LiveTable(table, Name) { MaxRows = Math.Max(0, MaxRows), SortDescending = Descending.IsPresent };
            if (SortBy != null) {
                live.SortColumn = ResolveColumn(table, PSWidgets.ToText(SortBy));
            }
            WriteObject(live);
        }

        private int ResolveColumn(Table table, string column) {
            if (int.TryParse(column, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int index)) {
                return index;
            }
            for (int i = 0; i < table.Columns.Count; i++) {
                if (string.Equals(Text.Markup.Strip(table.Columns[i].Header), column, StringComparison.OrdinalIgnoreCase)) {
                    return i;
                }
            }
            ThrowTerminatingError(new ErrorRecord(new PSArgumentException("The table has no column '" + column + "'."), "ConsoleMonsterUnknownColumn", ErrorCategory.InvalidArgument, column));
            return 0;
        }
    }
}
