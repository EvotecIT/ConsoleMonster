using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Lays console widgets out in an equal-width grid.</para>
    /// <para type="description">Children fill the grid row by row. When columns would become narrower than 24 cells the grid uses fewer columns.</para>
    /// </summary>
    /// <example>
    ///   <para>Three charts in a row</para>
    ///   <code>New-ConsoleLayoutGrid -Columns 3 -Content { New-ConsoleChart -WidgetId a -Type Sparkline; New-ConsoleChart -WidgetId b -Type Sparkline; New-ConsoleChart -WidgetId c -Type Sparkline }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleLayoutGrid")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleLayoutGrid : ContainerCmdlet {
        /// <summary><para type="description">Scriptblock that emits the cells.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        [AllowNull, AllowEmptyCollection]
        public override object? Content { get; set; }

        /// <summary><para type="description">Number of columns (default 2).</para></summary>
        [Parameter]
        public int Columns { get; set; } = 2;

        /// <summary><para type="description">Blank columns between cells.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] public int ColumnGap { get; set; } = 1;
        /// <summary><para type="description">Blank rows between grid rows.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)] public int RowGap { get; set; }
        /// <summary><para type="description">Minimum column width before reducing the column count.</para></summary>
        [Parameter, ValidateRange(1, int.MaxValue)] public int MinColumnWidth { get; set; } = 24;
        /// <summary><para type="description">Fits rows to content instead of the default-on height fill.</para></summary>
        [Parameter] public SwitchParameter NoFillHeight { get; set; }

        /// <inheritdoc />
        protected override IWidget Build(List<IWidget> children) {
            return new Grid(Columns, children.ToArray()) { ColumnGap = ColumnGap, RowGap = RowGap, MinColumnWidth = MinColumnWidth, FillHeight = !NoFillHeight.IsPresent };
        }
    }
}
