using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>Lays console widgets out in an equal-width grid.</summary>
    /// <remarks><para>Children fill the grid row by row. When columns would become narrower than 24 cells the grid uses fewer columns.</para></remarks>
    /// <example>
    ///   <para>Three charts in a row</para>
    ///   <code>New-ConsoleLayoutGrid -Columns 3 -Content { New-ConsoleChart -WidgetId a -Type Sparkline; New-ConsoleChart -WidgetId b -Type Sparkline; New-ConsoleChart -WidgetId c -Type Sparkline }</code>
    /// </example>
    /// <example>
    /// <para>Arrange an inventory count beside its status</para>
    /// <code>New-ConsoleLayoutGrid -Columns 2 -Content {
    ///     New-ConsolePanel 'Inventory' -Content { New-ConsoleText '2 servers' }
    ///     New-ConsolePanel 'Status' -Content { New-ConsoleText 'Online' -Role Pass }
    /// } | Show-Console
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleLayoutGrid", HelpUri = "https://consolemonster.dev/powershell/commands/new-consolelayoutgrid/")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleLayoutGrid : ContainerCmdlet {
        /// <summary><para type="description">Scriptblock that emits the cells.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [AllowNull, AllowEmptyCollection]
        public override object? Content { get; set; }

        /// <summary><para type="description">Number of columns (default 2).</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "2")]
        public int Columns { get; set; } = 2;

        /// <summary><para type="description">Blank columns between cells.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] [PSDefaultValue(Help = "1")]
        public int ColumnGap { get; set; } = 1;
        /// <summary><para type="description">Blank rows between grid rows.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] [PSDefaultValue(Help = "0")]
        public int RowGap { get; set; }
        /// <summary><para type="description">Minimum column width before reducing the column count.</para></summary>
        [Parameter, ValidateRange(1, int.MaxValue)] [PSDefaultValue(Help = "24")]
        public int MinColumnWidth { get; set; } = 24;
        /// <summary><para type="description">Fits rows to content instead of the default-on height fill.</para></summary>
        [Parameter] [PSDefaultValue(Help = "$false")]
        public SwitchParameter NoFillHeight { get; set; }

        /// <inheritdoc />
        protected override IWidget Build(List<IWidget> children) {
            LayoutGrid grid = new LayoutGrid(Columns, children.ToArray()) { RowGap = RowGap, MinColumnWidth = MinColumnWidth, FillHeight = !NoFillHeight.IsPresent };
            if (MyInvocation.BoundParameters.ContainsKey(nameof(ColumnGap))) { grid.ColumnGap = ColumnGap; }
            return grid;
        }
    }
}
