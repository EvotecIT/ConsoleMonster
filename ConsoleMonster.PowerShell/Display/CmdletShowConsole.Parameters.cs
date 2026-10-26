using System;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    public sealed partial class CmdletShowConsole {
        /// <summary><para type="description">The object, widget or text to display.</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ParameterSetName = "Objects")]
        public override PSObject? InputObject { get; set; }

        /// <summary><para type="description">A file for Code, Json or Markdown.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Path")]
        public override string? Path { get; set; }

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

        /// <summary><para type="description">Returns plain text instead of displaying it.</para></summary>
        [Parameter]
        public override SwitchParameter AsString { get; set; }

        /// <summary><para type="description">Returns the constructed widget.</para></summary>
        [Parameter]
        public override SwitchParameter AsWidget { get; set; }

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

        /// <summary><para type="description">Keeps empty fields in record views.</para></summary>
        [Parameter]
        public override SwitchParameter ShowEmpty { get; set; }

        /// <summary><para type="description">Maximum nesting depth; default three.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)]
        public override int Depth { get; set; } = 3;

        /// <summary><para type="description">The property containing a hierarchy node children.</para></summary>
        [Parameter]
        public override string? ChildrenProperty { get; set; }

        /// <summary><para type="description">Produces children for a hierarchy node.</para></summary>
        [Parameter]
        public override ScriptBlock? Children { get; set; }

        /// <summary><para type="description">The hierarchy label property.</para></summary>
        [Parameter]
        public override string? LabelProperty { get; set; }

        /// <summary><para type="description">The hierarchy detail property.</para></summary>
        [Parameter]
        public override string? DetailProperty { get; set; }

        /// <summary><para type="description">Maximum inspected items per collection; default 100.</para></summary>
        [Parameter, ValidateRange(1, int.MaxValue)]
        public override int MaxItems { get; set; } = 100;

        /// <summary><para type="description">Maximum hierarchy nodes; default 1000.</para></summary>
        [Parameter, ValidateRange(1, int.MaxValue)]
        public override int MaxNodes { get; set; } = 1000;

        /// <summary><para type="description">Object members to include.</para></summary>
        [Parameter]
        public override string[]? Include { get; set; }

        /// <summary><para type="description">Object members omitted without reading their getters.</para></summary>
        [Parameter]
        public override string[]? Exclude { get; set; }

        /// <summary><para type="description">Code language; default Auto.</para></summary>
        [Parameter]
        public override string Language { get; set; } = "Auto";

        /// <summary><para type="description">Shows code line numbers.</para></summary>
        [Parameter]
        public override SwitchParameter LineNumbers { get; set; }

        /// <summary><para type="description">Line numbers or ranges to highlight.</para></summary>
        [Parameter]
        public override string[]? Highlight { get; set; }

        /// <summary><para type="description">The first code line number; default one.</para></summary>
        [Parameter]
        public override int FirstLine { get; set; } = 1;

        /// <summary><para type="description">Maximum displayed code lines; default 2000.</para></summary>
        [Parameter]
        public override int MaxLines { get; set; } = 2000;

        /// <summary><para type="description">Hides a code file title.</para></summary>
        [Parameter]
        public override SwitchParameter NoTitle { get; set; }

        /// <summary><para type="description">Numeric chart representation: Bar, Histogram or Sparkline. Line, Stacked and Scatter charts take series; build them with New-ConsoleChart.</para></summary>
        [Parameter]
        [ValidateSet("Bar", "Histogram", "Sparkline")]
        public override ChartType Type { get; set; }

        /// <summary><para type="description">The chart label property or expression; default Name.</para></summary>
        [Parameter]
        public override object Label { get; set; } = "Name";

        /// <summary><para type="description">Chart ordering.</para></summary>
        [Parameter]
        public override PipelineChartSort Sort { get; set; }

        /// <summary><para type="description">Histogram bins; default ten.</para></summary>
        [Parameter, ValidateRange(1, 128)]
        public override int Bins { get; set; } = 10;

        /// <summary><para type="description">The chart numeric unit.</para></summary>
        [Parameter]
        public override string? Unit { get; set; }

        /// <summary><para type="description">Shows framework stack frames in error views.</para></summary>
        [Parameter]
        public override SwitchParameter ShowFrameworkFrames { get; set; }

        /// <summary><para type="description">Maximum application frames per error; default eight.</para></summary>
        [Parameter]
        public override int MaxFrames { get; set; } = 8;

        /// <summary><para type="description">Hides error stack frames.</para></summary>
        [Parameter]
        public override SwitchParameter NoStack { get; set; }

        /// <summary><para type="description">Uses the compact everyday error layout.</para></summary>
        [Parameter]
        public override SwitchParameter Compact { get; set; }

        /// <summary><para type="description">Maximum inner errors; default four.</para></summary>
        [Parameter, ValidateRange(0, int.MaxValue)]
        public override int MaxInner { get; set; } = 4;

        /// <summary><para type="description">Suppresses default-on source excerpts in error views.</para></summary>
        [Parameter]
        public override SwitchParameter NoSource { get; set; }

        /// <summary><para type="description">Disables default-on Markdown paging.</para></summary>
        [Parameter]
        public override SwitchParameter NoPager { get; set; }

        /// <summary><para type="description">The rule title alignment; default Left.</para></summary>
        [Parameter]
        public override HorizontalAlignment Alignment { get; set; }

        /// <summary><para type="description">Theme role for a rule line; default Border.</para></summary>
        [Parameter] public override Styling.ThemeToken LineRole { get; set; } = Styling.ThemeToken.Border;
        /// <summary><para type="description">Optional rule line glyph style.</para></summary>
        [Parameter] public override Glyphs.BoxStyle? LineStyle { get; set; }

        /// <summary><para type="description">Optional title indentation in cells.</para></summary>
        [Parameter]
        public override int? TitleIndent { get; set; }

        /// <summary><para type="description">Wraps column headings.</para></summary>
        [Parameter]
        public override SwitchParameter WrapHeaders { get; set; }

        /// <summary><para type="description">Renders consecutive grids when all columns cannot fit.</para></summary>
        [Parameter]
        public override SwitchParameter KeepAllColumns { get; set; }

        /// <summary><para type="description">Renders group headings as full-width rows.</para></summary>
        [Parameter]
        public override SwitchParameter GroupHeadersAsRows { get; set; }

        /// <summary><para type="description">Uses labelled records at or below this row count; zero disables.</para></summary>
        [Parameter]
        public override int RecordViewRows { get; set; } = 1;

        /// <summary><para type="description">Repeats the full key in record and card layouts.</para></summary>
        [Parameter]
        public override SwitchParameter ShowKeyField { get; set; }

        /// <summary><para type="description">Suppresses the default-on hidden-column footer.</para></summary>
        [Parameter]
        public override SwitchParameter NoHiddenColumns { get; set; }

        /// <summary><para type="description">Allows column widths to shrink between renders.</para></summary>
        [Parameter]
        public override SwitchParameter NoStableWidths { get; set; }

        /// <summary><para type="description">Suppresses the default-on header row.</para></summary>
        [Parameter]
        public override SwitchParameter NoHeader { get; set; }

        /// <summary><para type="description">Uses record cards at every width.</para></summary>
        [Parameter]
        public override SwitchParameter AlwaysCards { get; set; }

        /// <summary><para type="description">Optional per-row titles for record and card views.</para></summary>
        [Parameter]
        public override string[]? RowTitles { get; set; }

        /// <summary><para type="description">Shows group subtotals.</para></summary>
        [Parameter]
        public override SwitchParameter ShowSubtotals { get; set; }

        /// <summary><para type="description">Text for the totals row.</para></summary>
        [Parameter]
        public override string? TotalLabel { get; set; }

        /// <summary><para type="description">Maximum wrapped lines per value; zero has no limit.</para></summary>
        [Parameter]
        public override int WrapLines { get; set; } = 3;

        /// <summary><para type="description">Total count when the input contains a subset.</para></summary>
        [Parameter]
        public override int? TotalRowCount { get; set; }

        /// <summary><para type="description">Typed culture, clock and time-zone configuration for values.</para></summary>
        [Parameter]
        public override ConsoleMonster.Presenters.PresentationOptions? Presentation { get; set; }

    }
}
