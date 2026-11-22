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
    /// <remarks>
    /// <para>Show-ConsoleGrid displays objects in Windows PowerShell 5.1 and PowerShell 7. Property configures the displayed columns using the same presenters, priorities and widths as Show-ConsoleTable. A selection returns the underlying objects to PowerShell.</para>
    /// <para>Terminal grid
    /// -------------</para>
    /// <para>Use the arrow keys to focus a row. Press F10 or Ctrl+Enter to accept the selection and resume the PowerShell pipeline. Single returns the focused row. In Multiple mode, Space toggles the mark on the focused row and Ctrl+A marks all visible rows. Accept returns the marked rows, or the focused row if none are marked. In None mode, the accept keys are disabled and the grid is browse-only.</para>
    /// <para>Use PgUp and PgDn to move a page at a time, or Ctrl+Home and Ctrl+End to move to the first or last row. Press Enter to inspect every column on the focused row. Type to filter. Each search word must appear in a displayed column. Press F2 to choose columns, F3 or Shift+F3 to cycle the sort column, and F4 to reverse the sort. Esc first clears an active filter. With the filter empty, Esc closes the grid without returning any objects.</para>
    /// <para>When Command supplies actions, Ctrl+K opens the action list and F6 cancels work in progress. Actions run inside the grid. Accepting a selection returns the rows to the calling script.</para>
    /// <para>Browser grid
    /// ------------</para>
    /// <para>With Window, the browser tab has a filter, sortable column headings and a table. Click a row to focus it. In Multiple mode, hold Ctrl or Command when clicking to toggle a row, or Shift-click to mark a range. Click OK to send the selection back to PowerShell. Browse-only mode has a Close button. Click Cancel or press Esc in the page to return without a selection. If the browser cannot open, the command warns and falls back to the terminal grid.</para>
    /// <para>Numbered list
    /// -------------</para>
    /// <para>Line mode prints numbered rows and prompts for a response. In Single mode, enter one row number or press Enter with an empty response to choose the first row. In Multiple mode, enter a comma-separated list of numbers, such as 1,3. An empty response with no preselection returns nothing.</para>
    /// <para>Static output
    /// -------------</para>
    /// <para>With Mode None, or when the command resolves to unattended output, it prints a static grid using the initial filter, sort and visible columns. It returns no selection.</para>
    /// </remarks>
    /// <example>
    /// <para>Inspect a server inside the terminal grid. Press Ctrl+K and choose Inspect to run the command against the focused or marked rows. Press F10 or Ctrl+Enter when you want to close the grid and return the selected rows to PowerShell.</para>
    /// <code>
    /// $servers = @(
    ///     [pscustomobject]@{ Name = 'web01'; Status = 'Pass'; Region = 'East' }
    ///     [pscustomobject]@{ Name = 'web02'; Status = 'Warn'; Region = 'West' }
    /// )
    /// $gridOptions = @{
    ///     Property = @('Name', 'Status', 'Region')
    ///     Title = 'Servers'
    ///     Command = @(@{ Inspect = { param($Row) $Row | Select-Object Name, Status, Region } })
    /// }
    /// $servers | Show-ConsoleGrid @gridOptions</code>
    /// </example>
    /// <example>
    /// <para>Browse processes sorted by memory. None turns off selection output. Press Enter to inspect a row and Esc to leave the grid.</para>
    /// <code>
    /// $gridOptions = @{
    ///     Descending = $true
    ///     Property = @('Name', 'Id', @{ Name = 'Memory'; Expression = { $_.WorkingSet64 }; Presenter = 'Bytes' })
    ///     SortColumn = 'Memory'
    ///     OutputMode = 'None'
    /// }
    /// Get-Process | Show-ConsoleGrid @gridOptions</code>
    /// </example>
    /// <example>
    /// <para>Preview stopping one process from the terminal grid. Use the arrows to focus a process, then press F10 or Ctrl+Enter. PowerShell resumes with the original Process object. Stop-Process receives that object, and WhatIf previews the stop. Cancelling the grid skips the preview.</para>
    /// <code>
    /// $gridOptions = @{
    ///     Title = 'Preview stopping a process'
    ///     Property = @('ProcessName', 'Id', 'CPU')
    ///     SortColumn = 'ProcessName'
    ///     OutputMode = 'Single'
    ///     Window = $false
    /// }
    /// $selectedProcess = Get-Process | Show-ConsoleGrid @gridOptions
    /// if ($null -ne $selectedProcess) {
    ///     Stop-Process -InputObject @($selectedProcess) -WhatIf
    /// }</code>
    /// </example>
    /// <example>
    /// <para>Preview stopping services from the browser grid. Choose rows and click OK to pass the original service objects to Stop-Service. WhatIf previews the operation for each selected service.</para>
    /// <code>
    /// $gridOptions = @{
    ///     Window = $true
    ///     Property = @('Name', 'DisplayName', 'Status')
    ///     Title = 'Stop services'
    /// }
    /// Get-Service | Where-Object Status -eq Running | Show-ConsoleGrid @gridOptions | Stop-Service -WhatIf</code>
    /// </example>
    /// <example>
    /// <para>Read the gallery script to see Show-ConsoleGrid in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>Get-ConsoleDemo -Slug 'service-browser' | Select-Object -ExpandProperty Source</code>
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

        /// <summary><para type="description">Controls what is returned when the operator accepts a selection. Multiple returns the marked rows, or the focused row if there are no marks in the terminal or browser grid. Single returns one row. None returns no objects. In the terminal, F10 and Ctrl+Enter accept a selection in Single and Multiple modes.</para></summary>
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

        /// <summary><para type="description">Controls how the grid prompts. Rich uses the interactive terminal grid. Line uses a numbered prompt. Accessible uses numbered prompts when a line console is available. None prints a static grid and returns no selection.</para></summary>
        [Parameter]
        public InteractionMode? Mode { get; set; }

        /// <summary><para type="description">Shows the grid in the default browser instead of the terminal, with the same columns, filter, sort and selection; the chosen rows return to the pipeline. The page is served from 127.0.0.1 behind a one-time address and loads nothing external. Ctrl+C or closing the tab cancels. When no browser can be opened, the terminal grid is shown instead. Sessions where nobody can answer (-Mode None, PowerShell -NonInteractive, CI) never open a browser and print the static view as without -Window. -Command and -RefreshRow apply to the terminal grid only. Set $PSDefaultParameterValues['Show-ConsoleGrid:Window'] = $true to use the browser by default, including for the Out-GridView fallback.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Window { get; set; }

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
            CancellationToken stop = (_stop ??= new CancellationTokenSource()).Token;
            // -Mode None, PowerShell -NonInteractive and CI print the static view below: nobody is there to answer a browser.
            if (Window.IsPresent && BrowserGridServer.CanAsk(options.Interactive) && TryShowInBrowser(grid, options, stop, out GridResult? browserResult)) {
                foreach (object? item in browserResult!.Items) {
                    WriteObject(item);
                }
                return;
            }
            try {
                result = grid.Show(options, stop);
            } catch (Exception ex) when (ex is PromptUnavailableException || ex is PromptCanceledException || ex is Input.InputClosedException) {
                PSInteractive.Fail(this, ex, Title);
                throw;
            }
            foreach (object? item in result.Items) {
                WriteObject(item);
            }
        }

        // Returns false (after a warning) when no browser could be opened, so the caller shows the terminal grid.
        private bool TryShowInBrowser(Grid grid, GridOptions options, CancellationToken stop, out GridResult? result) {
            if (Command != null || RefreshRow != null) {
                WriteWarning("-Command and -RefreshRow apply to the terminal grid only; the browser grid shows the rows without them.");
            }
            BrowserGridOptions browser = new BrowserGridOptions {
                Opened = address => WriteInformation(new HostInformationMessage {
                    Message = "The grid is open in your browser: " + address.AbsoluteUri + " (press Ctrl+C to cancel).",
                    NoNewLine = false
                }, new[] { "PSHOST" })
            };
            try {
                result = grid.ShowInBrowser(options, browser, stop);
                return true;
            } catch (PromptUnavailableException ex) {
                WriteWarning(ex.Message + " Showing the terminal grid instead.");
                result = null;
                return false;
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
