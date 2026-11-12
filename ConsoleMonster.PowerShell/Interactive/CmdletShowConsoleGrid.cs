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
    /// <summary>Shows pipeline objects in an interactive grid and returns the rows you choose.</summary>
    /// <remarks><para>An Out-GridView for the console, on Windows PowerShell 5.1 and PowerShell 7. Typing filters the rows (every word must appear in a shown column); Up/Down, PgUp/PgDn and Ctrl+Home/Ctrl+End move; F3 sorts by the next column (Shift+F3 the previous one) and F4 reverses the order; Space marks rows (Ctrl+A marks all shown rows); Enter shows the focused row with every column; F2 chooses the columns; Ctrl+Enter (or F10) returns the marked rows, or the focused row when none is marked; Esc clears the filter, then cancels. Columns come from -Property with the same options as Show-ConsoleTable (presenters, priorities, widths, tone rules); the original objects are returned. The screen has the same header bar, panel and footer as menus and forms. In line mode the rows are a numbered list. Without an interactive user (redirected output, a job, CI or -Mode None), one static view honours the initial filter, sort and hidden columns. Nothing is selected or returned.</para></remarks>
    /// <example>
    /// <para>Inspect selected objects with an embedded command</para>
    /// <code>$servers = @(
    ///     [pscustomobject]@{ Name = 'web01'; Status = 'Pass'; Region = 'East' }
    ///     [pscustomobject]@{ Name = 'web02'; Status = 'Warn'; Region = 'West' }
    /// )
    /// $servers | Show-ConsoleGrid -Property Name, Status, Region -Title 'Servers' -Command @{ Inspect = { param($Row) $Row | Select-Object Name, Status, Region } }</code>
    /// </example>
    /// <example>
    /// <para>Browse processes sorted by memory</para>
    /// <code>Get-Process | Show-ConsoleGrid -Property Name, Id, @{ Name = 'Memory'; Expression = { $_.WorkingSet64 }; Presenter = 'Bytes' } -SortColumn Memory -Descending -OutputMode None</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleGrid", HelpUri = "https://consolemonster.dev/powershell/commands/show-consolegrid/")]
    [OutputType(typeof(object))]
    public sealed class CmdletShowConsoleGrid : PSCmdlet {
        private readonly List<PSObject> _items = new List<PSObject>();
        private CancellationTokenSource? _stop;

        /// <summary><para type="description">The objects to show (pipeline input).</para></summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject? InputObject { get; set; }

        /// <summary><para type="description">Columns: names, wildcards and calculated properties with the Show-ConsoleTable column options.</para></summary>
        [Parameter(Position = 0)]
        public object[]? Property { get; set; }

        /// <summary><para type="description">The title in the header bar (default Grid).</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "Grid")]
        public string Title { get; set; } = "Grid";

        /// <summary><para type="description">What is returned: Multiple (default; the marked rows or the focused row), Single (the focused row) or None (browse only).</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "Multiple")]
        public GridSelection OutputMode { get; set; } = GridSelection.Multiple;

        /// <summary><para type="description">The filter to start with.</para></summary>
        [Parameter]
        public string? Filter { get; set; }

        /// <summary><para type="description">The column to sort by at the start.</para></summary>
        [Parameter]
        public string? SortColumn { get; set; }

        /// <summary><para type="description">Sorts descending at the start.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Descending { get; set; }

        /// <summary><para type="description">Columns hidden at the start (F2 shows them again).</para></summary>
        [Parameter]
        public string[]? Exclude { get; set; }

        /// <summary><para type="description">In-app commands or a hashtable of action keys and script blocks. Use New-ConsoleCommand for shortcuts, optional confirmation and explicit arguments/modules. Ctrl+K opens actions; F6 cancels work.</para></summary>
        [Parameter]
        public object[]? Command { get; set; }

        /// <summary><para type="description">Refreshes each acted-on row on the caller's input thread after successful completion. Receives param($Row, $CommandKey) and returns one replacement object. Keep the callback short; action work belongs in New-ConsoleCommand.</para></summary>
        [Parameter]
        public ScriptBlock? RefreshRow { get; set; }

        /// <summary><para type="description">The culture for numbers and dates (default CONSOLEMONSTER_CULTURE, then the current culture).</para></summary>
        [Parameter]
        public CultureInfo? Culture { get; set; }

        /// <summary><para type="description">Forces how to ask: Rich (the grid), Line or Accessible (a numbered list), or None (fail without a user).</para></summary>
        [Parameter]
        public InteractionMode? Mode { get; set; }

        /// <summary>The unavailable Out-GridView adapter requests unattended display in jobs, transcripts and redirected sessions.</summary>
        [Parameter(DontShow = true)]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter UnavailableFallback { get; set; }

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
            // Shaping and input routing can touch Terminal before Grid.Show; keep the same outer operation boundary.
            using Output.TerminalGuard outputMode = new Output.TerminalGuard(() => { }, false);
            Grid grid = new Grid(Title) { OutputMode = OutputMode };
            PSGridListing listing = PSGridListing.Build(_items, Property, Culture, this, grid, out _);
            GridOptions options = new GridOptions { Filter = Filter, SortDescending = Descending.IsPresent, Interactive = PSInteractive.Options(this, Mode) };
            foreach (AppCommand command in PSCommandActions.Collect(Command)) { options.Command.Add(command); }
            if (RefreshRow != null) {
                options.RefreshRow = (command, row) => {
                    System.Collections.ObjectModel.Collection<PSObject> output = RefreshRow.Invoke(row.Item, command.Key);
                    if (output.Count != 1) { throw new InvalidOperationException("RefreshRow must return exactly one row object."); }
                    PSObject item = output[0];
                    return listing.Row(item);
                };
            }
            if (UnavailableFallback.IsPresent && options.Interactive.Input == null && options.Interactive.Capture == null
                && (!ConsoleFormatting.Displayed(this) || FormatCommandRouter.IsTranscribing(this))) {
                // A background job's host can offer line prompts by blocking the job. An unavailable-command fallback
                // must print the existing table instead; recording a session must likewise never open a rich screen.
                options.Interactive.Mode = null;
                options.Interactive.NonInteractive = true;
            }
            IReadOnlyList<BoundColumn> columns = listing.Columns;
            for (int c = 0; c < columns.Count; c++) {
                string header = Markup.Strip(columns[c].Column.Header);
                if (SortColumn != null && (string.Equals(columns[c].Name, SortColumn, StringComparison.OrdinalIgnoreCase) || string.Equals(header, SortColumn, StringComparison.OrdinalIgnoreCase))) {
                    options.SortColumn = c;
                }
                if (Exclude != null && Array.Exists(Exclude, h => string.Equals(h, columns[c].Name, StringComparison.OrdinalIgnoreCase) || string.Equals(h, header, StringComparison.OrdinalIgnoreCase))) {
                    options.HiddenColumns.Add(header);
                }
            }
            GridResult result;
            try {
                result = grid.Show(options, (_stop ??= new CancellationTokenSource()).Token);
            } catch (Exception ex) when (ex is PromptUnavailableException || ex is PromptCanceledException || ex is Input.InputClosedException) {
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
