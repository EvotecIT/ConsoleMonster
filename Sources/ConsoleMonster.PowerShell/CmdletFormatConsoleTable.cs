using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.PowerShell.Binding;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Formats pipeline objects as a width-aware table and writes it to the host.</para>
    /// <para type="description">Columns come from -Property (like Format-Table: names, wildcards and calculated properties @{ Name; Expression }), from the type's DefaultDisplayPropertySet, or from the objects' properties. Column options go in the same hashtables: Overflow (Wrap, Truncate, TruncateMiddle, Never), MaxLines, Priority, Key, MinWidth, MaxWidth, Width, Alignment, Presenter (Text, Status, Number, Bytes, Duration, Date, DateRelative, DateBoth, Boolean, Bar, Sparkline, Progress, Link), Unit, Aggregate, Header and tone rules (FailAbove, WarnAbove, FailBelow, WarnBelow, PassAbove, PassBelow, Tone). When the table is too wide, low-priority columns hide with a footer that names them; below -CardsBelow cells the rows become record cards; a single object becomes a record view. Redirected output is plain text without escape codes.</para>
    /// <example>
    ///   <para>Largest files, sizes as bytes, paths shortened in the middle</para>
    ///   <code>Get-ChildItem -File | Sort-Object Length -Descending | Format-ConsoleTable -Top 10 -Total -Property Name, @{ Name = 'Length'; Presenter = 'Bytes'; Aggregate = 'Sum' }, @{ Name = 'FullName'; Overflow = 'TruncateMiddle'; Priority = 1 }</code>
    /// </example>
    /// <example>
    ///   <para>Services grouped by status</para>
    ///   <code>Get-Service | Format-ConsoleTable -GroupBy Status -Property Name, DisplayName, StartType</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "ConsoleTable")]
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
        public string? Title { get; set; }

        /// <summary><para type="description">A caption below the table (markup, muted).</para></summary>
        [Parameter]
        public string? Caption { get; set; }

        /// <summary><para type="description">Border style: Rounded, Square, Double, Minimal or None.</para></summary>
        [Parameter]
        public TableBorder Border { get; set; } = TableBorder.Rounded;

        /// <summary><para type="description">Below this width (in cells) rows become record cards when the grid cannot fit; 0 disables cards. Default 60.</para></summary>
        [Parameter]
        public int CardsBelow { get; set; } = 60;

        /// <summary><para type="description">Lays the table out for this width instead of the terminal's (for files, logs and previews).</para></summary>
        [Parameter]
        public int Width { get; set; }

        /// <summary><para type="description">Underlines the header instead of drawing a separator row.</para></summary>
        [Parameter]
        public SwitchParameter NoHeaderSeparator { get; set; }

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
            Table table = PSFormatting.BuildTable(_items, Property, GroupBy, Total.IsPresent);
            table.Title = Title;
            table.Caption = Caption;
            table.Border = Border;
            table.CardsBelow = CardsBelow;
            table.HeaderSeparator = !NoHeaderSeparator.IsPresent;
            table.MaxRows = Top > 0 ? Top : 0;
            PSFormatting.Write(table, Width);
            if (PassThru.IsPresent) {
                foreach (PSObject item in _items) {
                    WriteObject(item);
                }
            }
        }
    }
}
