using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using ConsoleMonster.Graphics;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using DiagramWidget = ConsoleMonster.Widgets.Diagram;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows a diagram from connection rows, a Diagram widget or a ChartForgeX file, with use case playback.</para>
    /// <para type="description">Pipe connection rows with From, To, Label, Status and Group, or select other columns with property names or script blocks using $_. New-ConsoleDiagram builds the same widget from nodes, links, groups and use cases. Nodes keep their subtitle and live value on separate lines. A narrow view becomes a grouped wrapping tree, retaining every connection label. The Needs attention list includes each non-Pass node and link; -NoSummary hides it.</para>
    /// <para type="description">Reads a Markdown file with ChartForgeX visual fences, or a plain architecture file without fences, and draws the diagram in terminal cells: boxes, routed connections, dashed groups and a legend. When the diagram has use cases, the first one is selected and its steps, payloads and keys are shown. In an interactive console, Left and Right step through the use case, Space plays it, Tab switches use cases, Up and Down move the focus between nodes or scroll a diagram taller than the viewport, and D shows the focused node's details. The app footer shows the exit key. In redirected, CI, line and non-interactive sessions, or with -StaticSnapshot, one static view is printed at the chosen step. Errors in the selected diagram stop the command with the file line and diagnostic code. Errors in other visual fences and markup warnings are written as PowerShell warnings.</para>
    /// </summary>
    /// <example><para>Explore a diagram and its use cases with the keyboard.</para><code>Show-ConsoleDiagram -Path ./ad-replication.md</code></example>
    /// <example><para>Print one step of a use case without reading keys.</para><code>Show-ConsoleDiagram -Path ./ad-replication.md -Scenario replicate -Step 6 -StaticSnapshot</code></example>
    /// <example><para>Capture the diagram as plain text for a log or a ticket.</para><code>Show-ConsoleDiagram -Path ./tierbridge.md -Scenario blocked -Step 7 -Width 120 -AsString</code></example>
    /// <example><para>Show connection rows using their standard columns.</para><code>$rows | Show-ConsoleDiagram -Title 'Replication' -StaticSnapshot</code></example>
    /// <example><para>Select columns and calculate connection labels.</para><code>$rows | Show-ConsoleDiagram -From Source -To Destination -Label { $_.Lag.ToString() + ' min' } -Status Health -Group Site -AsString</code></example>
    [Cmdlet(VerbsCommon.Show, "ConsoleDiagram", DefaultParameterSetName = "Rows")]
    [OutputType(typeof(DiagramWidget), typeof(string))]
    public sealed class CmdletShowConsoleDiagram : DiagramContentCmdlet {
        private CancellationTokenSource? _stop;
        private int _stopping;

        /// <summary><para type="description">The Markdown or architecture file to read. A file without visual fences is read as one ChartForgeX architecture diagram.</para></summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Path")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; } = string.Empty;

        /// <summary><para type="description">The diagram id to show when the file has more than one architecture, topology or flow diagram.</para></summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string? Diagram { get; set; }

        /// <summary><para type="description">The use case id to select. Defaults to the diagram's first use case.</para></summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string? Scenario { get; set; }

        /// <summary><para type="description">The one-based step to highlight in the selected use case. Interactive views start there; static views show that step.</para></summary>
        [Parameter]
        [ValidateRange(1, int.MaxValue)]
        public int Step { get; set; }

        /// <summary><para type="description">Rich runs the keyboard view. Line, Accessible, None and Snapshot print the static view.</para></summary>
        [Parameter]
        public InteractionMode? Mode { get; set; }

        /// <summary><para type="description">Width used by the static view; zero detects the host width.</para></summary>
        [Parameter]
        [ValidateRange(0, 32767)]
        public int Width { get; set; }

        /// <summary><para type="description">Prints a static view without reading keys.</para></summary>
        [Parameter] public SwitchParameter StaticSnapshot { get; set; }
        /// <summary><para type="description">Returns the static view as one plain-text string.</para></summary>
        [Parameter] public SwitchParameter AsString { get; set; }
        /// <summary><para type="description">Returns the diagram widget without drawing it, for static display or live updates in Show-ConsoleApp layouts. The consumer must handle keys to enable diagram navigation and playback. Cannot be combined with -AsString or -StaticSnapshot.</para></summary>
        [Parameter] public SwitchParameter AsWidget { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            var stop = new CancellationTokenSource();
            Volatile.Write(ref _stop, stop);
            try {
                if (Volatile.Read(ref _stopping) != 0) stop.Cancel();
                Show(stop.Token);
            } catch (OperationCanceledException) when (stop.IsCancellationRequested) {
                // Stop before or during preparation ends without emitting a partial snapshot or widget.
            } finally { stop.Dispose(); }
        }
        private void Show(CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            if (AsWidget.IsPresent && (AsString.IsPresent || StaticSnapshot.IsPresent)) {
                throw Failure("-AsWidget cannot be combined with -AsString or -StaticSnapshot.", "DiagramOutputModeInvalid", ErrorCategory.InvalidArgument, nameof(AsWidget));
            }
            PSOutputExport.Validate(this);
            DiagramRendererProvider.Register();
            DiagramWidget diagram;
            if (ParameterSetName == "Path") {
                string path = ResolvePath();
                diagram = Load(path);
                Configure(diagram);
            } else {
                diagram = BuildDiagram();
            }
            Select(diagram);
            cancellationToken.ThrowIfCancellationRequested();

            if (AsWidget.IsPresent) { WriteObject(diagram); return; }
            InteractiveOptions interactive = PSInteractive.Options(this, Mode);
            if (StaticSnapshot.IsPresent || AsString.IsPresent || interactive.ResolveMode() != InteractionMode.Rich) {
                PSFormatting.Emit(this, diagram, Width, null, AsString.IsPresent);
                return;
            }

            try {
                diagram.Run(new ConsoleAppOptions { Interactive = interactive }, cancellationToken);
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                // Ctrl+C ends the view like Esc.
            } catch (Exception ex) when (ex is PromptUnavailableException || ex is PromptCanceledException || ex is InputClosedException) {
                PSInteractive.Fail(this, ex, MyInvocation.MyCommand.Name);
            }
        }

        private string ResolvePath() {
            string path;
            ProviderInfo provider;
            try {
                path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path, out provider, out PSDriveInfo _);
            } catch (Exception ex) when (ex is System.Management.Automation.DriveNotFoundException || ex is ProviderNotFoundException || ex is ArgumentException) {
                throw Failure(ex.Message, "DiagramPathInvalid", ErrorCategory.InvalidArgument, Path);
            }
            if (!string.Equals(provider.Name, "FileSystem", StringComparison.OrdinalIgnoreCase)) {
                throw Failure("Path must name a FileSystem path.", "DiagramPathNotFileSystem", ErrorCategory.InvalidArgument, Path);
            }
            if (Directory.Exists(path)) {
                throw Failure("Path names a folder; pass the diagram file: " + path, "DiagramPathIsFolder", ErrorCategory.InvalidArgument, Path);
            }
            if (!File.Exists(path)) {
                throw Failure("The diagram file was not found: " + path, "DiagramFileNotFound", ErrorCategory.ObjectNotFound, Path);
            }
            return path;
        }

        private DiagramWidget Load(string path) {
            try {
                return DiagramFileReader.Load(path, Diagram, WriteWarning);
            } catch (DiagramFileReader.ImportException ex) {
                ErrorCategory category = ex.ErrorId == "DiagramMarkupInvalid" ? ErrorCategory.InvalidData
                    : ex.ErrorId == "DiagramNotFound" ? ErrorCategory.ObjectNotFound : ErrorCategory.InvalidArgument;
                throw Failure(ex.Message, ex.ErrorId, category, ex.Target);
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) {
                throw Failure("The diagram file cannot be read: " + ex.Message, "DiagramFileUnreadable", ErrorCategory.ReadError, Path);
            }
        }

        private void Select(DiagramWidget diagram) {
            if (Scenario != null) {
                if (!diagram.UseCases.Any(item => item.Id == Scenario)) {
                    string known = string.Join(", ", diagram.UseCases.Select(item => item.Id));
                    throw Failure("No use case has id '" + Scenario + "'. Use cases: " + (known.Length == 0 ? "none" : known) + ".", "DiagramScenarioNotFound", ErrorCategory.ObjectNotFound, Scenario);
                }
                try { diagram.SelectScenario(Scenario); }
                catch (ArgumentException ex) {
                    throw Failure("Use case '" + Scenario + "' cannot be shown: " + Reason(ex), "DiagramScenarioInvalid", ErrorCategory.InvalidData, Scenario);
                }
            } else if (diagram.UseCases.Count > 0 && diagram.ScenarioId == null) {
                WriteWarning("None of the diagram's use cases can be shown; they name nodes or connections the diagram does not have.");
            } else if (diagram.UseCases.Count > 0 && diagram.ScenarioId != diagram.UseCases[0].Id) {
                WriteWarning("Use case '" + diagram.UseCases[0].Id + "' names nodes or connections the diagram does not have; showing '" + diagram.ScenarioId + "'.");
            }
            if (!MyInvocation.BoundParameters.ContainsKey(nameof(Step))) return;
            string? selected = diagram.ScenarioId;
            if (selected == null) throw Failure("The diagram has no use case that can be stepped through.", "DiagramStepUnavailable", ErrorCategory.InvalidArgument, Step);
            int count = diagram.UseCases.First(item => item.Id == selected).Steps.Count;
            if (count == 0) throw Failure("Use case '" + selected + "' has no steps.", "DiagramStepUnavailable", ErrorCategory.InvalidArgument, Step);
            if (Step > count) throw Failure("Step accepts a number from 1 to " + count.ToString(CultureInfo.InvariantCulture) + " for use case '" + selected + "'.", "DiagramStepOutOfRange", ErrorCategory.InvalidArgument, Step);
            diagram.Step(Step - 1);
        }

        // ArgumentException appends the parameter name ("Parameter name: x" on .NET Framework, " (Parameter 'x')" on .NET); people only need the reason.
        private static string Reason(ArgumentException ex) {
            string message = ex.Message;
            int newline = message.IndexOf('\n');
            if (newline >= 0) { message = message.Substring(0, newline).TrimEnd('\r'); }
            return ex.ParamName == null ? message : message.Replace(" (Parameter '" + ex.ParamName + "')", string.Empty);
        }

        // ThrowTerminatingError always throws; returning an exception lets callers write `throw Failure(...)` so the compiler sees the exit.
        private PipelineStoppedException Failure(string message, string errorId, ErrorCategory category, object target) {
            ThrowTerminatingError(new ErrorRecord(new InvalidOperationException(message), errorId, category, target));
            return new PipelineStoppedException();
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            Volatile.Write(ref _stopping, 1);
            try { Volatile.Read(ref _stop)?.Cancel(); } catch (ObjectDisposedException) { }
        }
    }
}
