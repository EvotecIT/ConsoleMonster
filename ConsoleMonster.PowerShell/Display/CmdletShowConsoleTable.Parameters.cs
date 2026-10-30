using System;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    public sealed partial class CmdletShowConsoleTable {
        /// <summary><para type="description">The object, widget or text to display.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ParameterSetName = "Objects")]
        public override PSObject? InputObject { get; set; }

        /// <summary><para type="description">A widget, text or script block producing display content.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Content")]
        public override object? Content { get; set; }

        /// <summary><para type="description">Property names, patterns or calculated column records.</para></summary>
        [Parameter]
        public override object[]? Property { get; set; }

        /// <summary><para type="description">Groups rows or records by a property or script block.</para></summary>
        [Parameter]
        public override object? GroupBy { get; set; }

        /// <summary><para type="description">The view title, including markup.</para></summary>
        [Parameter]
        public override object? Title { get; set; }

        /// <summary><para type="description">A table caption.</para></summary>
        [Parameter]
        public override object? Caption { get; set; }

        /// <summary><para type="description">Layout width; zero resolves the terminal with a 100-cell fallback.</para></summary>
        [Parameter, ValidateRange(0, 1000)]
        public override int Width { get; set; }

        /// <summary><para type="description">The culture used for values.</para></summary>
        [Parameter]
        public override CultureInfo? Culture { get; set; }

        /// <summary><para type="description">Returns the original input after display.</para></summary>
        [Parameter]
        public override SwitchParameter PassThru { get; set; }

        /// <summary><para type="description">Shows a totals row for numeric columns.</para></summary>
        [Parameter]
        public override SwitchParameter ShowTotals { get; set; }

        /// <summary><para type="description">Maximum displayed rows or chart groups; zero shows all.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)]
        public override int MaxRows { get; set; }

        /// <summary><para type="description">Table border style; default Rounded.</para></summary>
        [Parameter]
        public override TableBorder Border { get; set; } = TableBorder.Rounded;

        /// <summary><para type="description">Table card breakpoint; default 60 cells.</para></summary>
        [Parameter]
        public override int CardsBelow { get; set; } = 60;

        /// <summary><para type="description">Underlines headers rather than drawing a separator.</para></summary>
        [Parameter]
        public override SwitchParameter NoHeaderSeparator { get; set; }

        /// <summary><para type="description">Expands a table to the available width.</para></summary>
        [Parameter]
        public override SwitchParameter Expand { get; set; }

        /// <summary><para type="description">The property shown through the status presenter.</para></summary>
        [Parameter]
        public override string? StatusProperty { get; set; }

        /// <summary><para type="description">Keeps the grouped property in table rows.</para></summary>
        [Parameter]
        public override SwitchParameter ShowGroupColumn { get; set; }

        /// <summary><para type="description">Preserves the input order of groups.</para></summary>
        [Parameter]
        public override SwitchParameter NoSort { get; set; }

        /// <summary><para type="description">Wraps long table values or code lines.</para></summary>
        [Parameter]
        public override SwitchParameter Wrap { get; set; }

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
