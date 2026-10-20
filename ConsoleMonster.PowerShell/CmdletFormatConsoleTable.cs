using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.PowerShell.Binding;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Formats pipeline objects as a width-aware table and writes it to the host.</para>
    /// <para type="description">Columns come from -Property (like Format-Table: names, wildcards and calculated properties @{ Name; Expression }), from the type's DefaultDisplayPropertySet, or from the objects' properties. Column options go in the same hashtables: Overflow (Wrap, Truncate, TruncateMiddle, Never), MaxLines, Priority, Key, MinWidth, MaxWidth, Width, Alignment, Presenter (Text, Status, Number, Bytes, Duration, Date, DateRelative, DateBoth, Boolean, Bar, Sparkline, Progress, Link), Unit, Aggregate, Header and tone rules (FailAbove, WarnAbove, FailBelow, WarnBelow, PassAbove, PassBelow, Tone). When the table is too wide, low-priority columns hide with a footer that names them; below -CardsBelow cells the rows become record cards. One object stays a one-row table; -View Record shows objects as labelled fields and -View Cards as cards. Redirected output is plain text without escape codes.</para>
    /// </summary>
    /// <example>
    ///   <para>Largest files, sizes as bytes, paths shortened in the middle</para>
    ///   <code>Get-ChildItem -File | Sort-Object Length -Descending | Format-ConsoleTable -Top 10 -Total -Property Name, @{ Name = 'Length'; Presenter = 'Bytes'; Aggregate = 'Sum' }, @{ Name = 'FullName'; Overflow = 'TruncateMiddle'; Priority = 1 }</code>
    /// </example>
    /// <example>
    ///   <para>Services grouped by status</para>
    ///   <code>Get-Service | Format-ConsoleTable -GroupBy Status -Property Name, DisplayName, StartType</code>
    /// </example>
    [Cmdlet(VerbsCommon.Format, "ConsoleTable")]
    [Alias("fct")]
    public sealed class CmdletFormatConsoleTable : PSCmdlet {
        private readonly List<PSObject> _items = new List<PSObject>();

        /// <summary><para type="description">The objects to show (pipeline input).</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Properties (names, wildcards, calculated properties and column option hashtables).</para></summary>
        [Parameter(Position = 0)]
        public object[]? Property { get; set; }

        /// <summary><para type="description">Groups rows under headings by a property name or a scriptblock; with -Total each group gets a subtotal.</para></summary>
        [Parameter]
        public object? GroupBy { get; set; }

        /// <summary><para type="description">Adds a totals row; numeric columns without an Aggregate option are summed.</para></summary>
        [Parameter]
        public SwitchParameter Total { get; set; }

        /// <summary><para type="description">Shows only the first N rows followed by "Showing N of M" (totals still cover every row).</para></summary>
        [Parameter]
        public int Top { get; set; }

        /// <summary><para type="description">A title above the table (markup).</para></summary>
        [Parameter]
        public object? Title { get; set; }

        /// <summary><para type="description">A caption below the table (markup, muted).</para></summary>
        [Parameter]
        public object? Caption { get; set; }

        /// <summary><para type="description">Border style: Rounded, Square, Double, Minimal or None.</para></summary>
        [Parameter]
        public TableBorder Border { get; set; } = TableBorder.Rounded;

        /// <summary><para type="description">Below this width (in cells) rows become record cards when the grid cannot fit; 0 disables cards. Default 60.</para></summary>
        [Parameter]
        public int CardsBelow { get; set; } = 60;

        /// <summary><para type="description">Lays the table out for this width instead of the terminal's (for files, logs and previews).</para></summary>
        [Parameter]
        public int Width { get; set; }

        /// <summary><para type="description">The culture for numbers and dates, for example en-US or pl-PL (labels stay English). Defaults to CONSOLEMONSTER_CULTURE, then the current culture, as PowerShell's own formatting does.</para></summary>
        [Parameter]
        public System.Globalization.CultureInfo? Culture { get; set; }

        /// <summary><para type="description">Underlines the header instead of drawing a separator row.</para></summary>
        [Parameter]
        public SwitchParameter NoHeaderSeparator { get; set; }

        /// <summary><para type="description">Writes the view as one string of plain text instead of drawing it, to keep it in a variable or a file from any script, function or PowerShell version. Without it, PowerShell 7.2+ captures a view typed at the prompt ($x = ..., &gt; file, Out-File); inside scripts and functions, and on Windows PowerShell 5.1, the view is drawn on the screen. Piped into another command (Out-Host, Out-String, Select-String) the view goes on as plain lines, without colour. Cannot be combined with -PassThru.</para></summary>
        [Parameter]
        public SwitchParameter AsString { get; set; }
        /// <summary><para type="description">Also writes the input objects to the pipeline.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary><para type="description">Writes the table widget to the pipeline instead of drawing it (for Export-ConsoleFrame, New-ConsolePanel or a dashboard).</para></summary>
        [Parameter]
        public SwitchParameter AsWidget { get; set; }

        /// <summary><para type="description">Keeps the -GroupBy column in the rows (by default the group heading names the key and the column is left out).</para></summary>
        [Parameter]
        public SwitchParameter ShowGroupColumn { get; set; }

        /// <summary><para type="description">Keeps the groups of -GroupBy in the order their keys first appear (by default they are sorted by key).</para></summary>
        [Parameter]
        public SwitchParameter NoSort { get; set; }

        /// <summary><para type="description">How the rows are laid out: Table (default; one object stays a one-row table, as with Format-Table), or Record (Cards is the same) for each object as labelled fields, as a single object used to be shown and as narrow windows show rows; -Top, the "Showing" line and -Total still apply.</para></summary>
        [Parameter]
        [ValidateSet("Table", "Record", "Cards", IgnoreCase = true)]
        public string View { get; set; } = "Table";

        /// <summary><para type="description">Shows every line of long values. Without it a value wraps onto at most three lines and the last ends with an ellipsis (a column's MaxLines option still applies). Short values (numbers, sizes, booleans, dates, enums, statuses) never wrap either way.</para></summary>
        [Parameter]
        public SwitchParameter Wrap { get; set; }

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
            bool first = true;
            TableBuildOptions options = new TableBuildOptions { Total = Total.IsPresent, Cmdlet = this, Culture = Culture, ShowGroupColumn = ShowGroupColumn.IsPresent, NoSort = NoSort.IsPresent };
            // Objects of different kinds get one table each (as Format-Table switches format); the title heads the first.
            System.Collections.Generic.List<Layout.IWidget> tables = PSFormatting.BuildTables(_items, Property, GroupBy, options, table => {
                if (Title != null) {
                    table.Title = first ? PSText.Markup(Title) : null;
                }
                table.Caption = PSText.Markup(Caption);
                table.Border = Border;
                table.CardsBelow = CardsBelow;
                table.HeaderSeparator = !NoHeaderSeparator.IsPresent;
                table.MaxRows = Top > 0 ? Top : 0;
                if (Wrap.IsPresent) {
                    table.WrapLines = 0;
                }
                // One object stays a table unless asked; Record (or Cards) shows every row as labelled fields, with -Top,
                // the "Showing" line and the totals still applied.
                table.RecordViewRows = 0;
                table.AlwaysCards = !string.Equals(View, "Table", StringComparison.OrdinalIgnoreCase);
                first = false;
            });
            Layout.IWidget output = PSFormatting.Combine(tables);
            if (AsWidget.IsPresent) {
                WriteObject(Culture != null ? (object)new Layout.CultureScope(output, Culture) : output);
                return;
            }
            PSFormatting.Emit(this, output, Width, Culture, AsString.IsPresent, PassThru.IsPresent);
            if (PassThru.IsPresent) {
                foreach (PSObject item in _items) {
                    WriteObject(item);
                }
            }
        }
    }
}
