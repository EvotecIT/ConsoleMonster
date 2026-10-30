using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    public partial class CmdletNewConsoleTable {
        /// <summary><para type="description">Optional title indentation in cells.</para></summary>
        [Parameter(DontShow = true)]
        public override int? TitleIndent { get; set; }

        /// <summary><para type="description">Wraps column headings.</para></summary>
        [Parameter(DontShow = true)]
        public override SwitchParameter WrapHeaders { get; set; }

        /// <summary><para type="description">Renders consecutive grids when all columns cannot fit.</para></summary>
        [Parameter(DontShow = true)]
        public override SwitchParameter KeepAllColumns { get; set; }

        /// <summary><para type="description">Renders group headings as full-width rows.</para></summary>
        [Parameter(DontShow = true)]
        public override SwitchParameter GroupHeadersAsRows { get; set; }

        /// <summary><para type="description">Uses labelled records at or below this row count; zero disables.</para></summary>
        [Parameter(DontShow = true)]
        public override int RecordViewRows { get; set; } = 1;

        /// <summary><para type="description">Repeats the full key in record and card layouts.</para></summary>
        [Parameter(DontShow = true)]
        public override SwitchParameter ShowKeyField { get; set; }

        /// <summary><para type="description">Suppresses the default-on hidden-column footer.</para></summary>
        [Parameter(DontShow = true)]
        public override SwitchParameter NoHiddenColumns { get; set; }

        /// <summary><para type="description">Allows column widths to shrink between renders.</para></summary>
        [Parameter(DontShow = true)]
        public override SwitchParameter NoStableWidths { get; set; }

        /// <summary><para type="description">Suppresses the default-on header row.</para></summary>
        [Parameter(DontShow = true)]
        public override SwitchParameter NoHeader { get; set; }

        /// <summary><para type="description">Optional per-row titles for record and card views.</para></summary>
        [Parameter(DontShow = true)]
        public override string[]? RowTitles { get; set; }

        /// <summary><para type="description">Shows group subtotals.</para></summary>
        [Parameter(DontShow = true)]
        public override SwitchParameter ShowSubtotals { get; set; }

        /// <summary><para type="description">Text for the totals row.</para></summary>
        [Parameter(DontShow = true)]
        public override string? TotalLabel { get; set; }

        /// <summary><para type="description">Maximum wrapped lines per value; zero has no limit.</para></summary>
        [Parameter(DontShow = true)]
        public override int WrapLines { get; set; } = 3;

        /// <summary><para type="description">Total count when the input contains a subset.</para></summary>
        [Parameter(DontShow = true)]
        public override int? TotalRowCount { get; set; }

        /// <summary><para type="description">Typed culture, clock and time-zone configuration for values.</para></summary>
        [Parameter(DontShow = true)]
        public override ConsoleMonster.Presenters.PresentationOptions? Presentation { get; set; }

    }
}
