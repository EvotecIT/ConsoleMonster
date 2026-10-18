using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Interactive;
using ConsoleMonster.PowerShell.Binding;
using ConsoleMonster.Presenters;
using ConsoleMonster.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows pipeline objects in an interactive grid and returns the rows you choose.</para>
    /// <para type="description">An Out-GridView for the console, on Windows PowerShell 5.1 and PowerShell 7. Typing filters the rows (every word must appear in a shown column); Up/Down, PgUp/PgDn and Ctrl+Home/Ctrl+End move; F3 sorts by the next column (Shift+F3 the previous one) and F4 reverses the order; Space marks rows (Ctrl+A marks all shown rows); Enter shows the focused row with every column; F2 chooses the columns; Ctrl+Enter (or F10) returns the marked rows, or the focused row when none is marked; Esc clears the filter, then cancels. Columns come from -Property with the same options as Format-ConsoleTable (presenters, priorities, widths, tone rules); the original objects are returned. The screen has the same header bar, panel and footer as menus and forms. In line mode the rows are a numbered list; without an interactive user the cmdlet fails with a clear error.</para>
    /// <example>
    ///   <para>Pick services to restart</para>
    ///   <code>Get-Service | Show-ConsoleGrid -Property Name, Status, StartType -Title 'Services' | Restart-Service -WhatIf</code>
    /// </example>
    /// <example>
    ///   <para>Browse processes sorted by memory</para>
    ///   <code>Get-Process | Show-ConsoleGrid -Property Name, Id, @{ Name = 'Memory'; Expression = { $_.WorkingSet64 }; Presenter = 'Bytes' } -SortBy Memory -Descending -OutputMode None</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Show, "ConsoleGrid")]
    [OutputType(typeof(object))]
    public sealed class CmdletShowConsoleGrid : PSCmdlet {
        private readonly List<PSObject> _items = new List<PSObject>();
        private CancellationTokenSource? _stop;

        /// <summary><para type="description">The objects to show (pipeline input).</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Columns: names, wildcards and calculated properties with the Format-ConsoleTable column options.</para></summary>
        [Parameter(Position = 0)]
        public object[]? Property { get; set; }

        /// <summary><para type="description">The title in the header bar (default Grid).</para></summary>
        [Parameter]
        public string Title { get; set; } = "Grid";

        /// <summary><para type="description">What is returned: Multiple (default; the marked rows or the focused row), Single (the focused row) or None (browse only).</para></summary>
        [Parameter]
        public GridSelection OutputMode { get; set; } = GridSelection.Multiple;

        /// <summary><para type="description">The filter to start with.</para></summary>
        [Parameter]
        public string? Filter { get; set; }

        /// <summary><para type="description">The column to sort by at the start.</para></summary>
        [Parameter]
        public string? SortBy { get; set; }

        /// <summary><para type="description">Sorts descending at the start.</para></summary>
        [Parameter]
        public SwitchParameter Descending { get; set; }

        /// <summary><para type="description">Columns hidden at the start (F2 shows them again).</para></summary>
        [Parameter]
        public string[]? Hide { get; set; }

        /// <summary><para type="description">The culture for numbers and dates (default CONSOLEMONSTER_CULTURE, then the current culture).</para></summary>
        [Parameter]
        public CultureInfo? Culture { get; set; }

        /// <summary><para type="description">Forces how to ask: Rich (the grid), Line or Accessible (a numbered list), or None (fail without a user).</para></summary>
        [Parameter]
        public InteractionMode? Mode { get; set; }

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
            IReadOnlyList<BoundColumn> columns = PropertyBinder.Bind(_items, Property);
            Grid grid = new Grid(Title) { Selection = OutputMode };
            if (Culture != null) {
                grid.Presentation = new PresentationOptions { Culture = Culture };
            }
            foreach (BoundColumn column in columns) {
                grid.AddColumn(column.Column);
            }
            foreach (PSObject item in _items) {
                object?[] values = new object?[columns.Count];
                for (int c = 0; c < columns.Count; c++) {
                    values[c] = PropertyBinder.Unwrap(columns[c].GetValue(item));
                }
                grid.AddRow(item, values);
            }
            GridOptions options = new GridOptions { Filter = Filter, SortDescending = Descending.IsPresent, Interactive = PSInteractive.Options(this, Mode) };
            for (int c = 0; c < columns.Count; c++) {
                string header = Markup.Strip(columns[c].Column.Header);
                if (SortBy != null && (string.Equals(columns[c].Name, SortBy, StringComparison.OrdinalIgnoreCase) || string.Equals(header, SortBy, StringComparison.OrdinalIgnoreCase))) {
                    options.SortColumn = c;
                }
                if (Hide != null && Array.Exists(Hide, h => string.Equals(h, columns[c].Name, StringComparison.OrdinalIgnoreCase) || string.Equals(h, header, StringComparison.OrdinalIgnoreCase))) {
                    options.HiddenColumns.Add(header);
                }
            }
            GridResult result;
            try {
                result = grid.Show(options, (_stop ??= new CancellationTokenSource()).Token);
            } catch (Exception ex) when (ex is PromptUnavailableException || ex is PromptCancelledException || ex is Input.InputClosedException) {
                PSInteractive.Fail(this, ex, Title);
                throw;
            }
            foreach (object? item in result.Items) {
                WriteObject(item);
            }
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            try {
                _stop?.Cancel();
            } catch (ObjectDisposedException) {
            }
            base.StopProcessing();
        }
    }
}
