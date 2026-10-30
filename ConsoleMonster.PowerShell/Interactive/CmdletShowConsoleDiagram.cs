using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;
using ChartForgeX.Diagnostics;
using ChartForgeX.Markup;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ConsoleMonster.Graphics;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows a ChartForgeX architecture or topology diagram in the terminal, with use case playback.</para>
    /// <para type="description">Reads a Markdown file with ChartForgeX visual fences, or a plain architecture file without fences, and draws the diagram in terminal cells: boxes, routed connections, dashed groups and a legend. When the diagram has use cases, the first one is selected and its steps, payloads and keys are shown. In an interactive console, Left and Right step through the use case, Space plays it, Tab switches use cases, Up and Down move the focus between nodes, D shows the focused node's details and Esc ends the view. In redirected, CI, line and non-interactive sessions, or with -StaticSnapshot, one static view is printed at the chosen step. Markup errors stop the command with the file line and diagnostic code; warnings are written as PowerShell warnings.</para>
    /// </summary>
    /// <example><para>Explore a diagram and its use cases with the keyboard.</para><code>Show-ConsoleDiagram -Path ./ad-replication.md</code></example>
    /// <example><para>Print one step of a use case without reading keys.</para><code>Show-ConsoleDiagram -Path ./ad-replication.md -Scenario replicate -Step 6 -StaticSnapshot</code></example>
    /// <example><para>Capture the diagram as plain text for a log or a ticket.</para><code>Show-ConsoleDiagram -Path ./tierbridge.md -Scenario blocked -Step 7 -Width 120 -AsString</code></example>
    [Cmdlet(VerbsCommon.Show, "ConsoleDiagram")]
    [OutputType(typeof(TopologyDiagram), typeof(string))]
    public sealed class CmdletShowConsoleDiagram : NoPipelineInputCmdlet {
        private const int MaximumDiagnostics = 10;
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private bool _shown;

        /// <summary><para type="description">The Markdown or architecture file to read. A file without visual fences is read as one ChartForgeX architecture diagram.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
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

        /// <summary><para type="description">Rich runs the keyboard view. Line, Accessible and None print the static view.</para></summary>
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
        /// <summary><para type="description">Returns the diagram widget without drawing it, for Show-ConsoleApp layouts or live updates.</para></summary>
        [Parameter] public SwitchParameter AsWidget { get; set; }

        /// <inheritdoc />
        protected override void ProcessWithoutPipelineInput() {
            // A null pipeline record still reaches this method; draw the diagram once.
            if (_shown) { return; }
            _shown = true;
            Show();
        }

        /// <inheritdoc />
        protected override void EndProcessing() => _stop.Dispose();

        private void Show() {
            PSOutputExport.Validate(this);
            string path = ResolvePath();
            TopologyChart chart = Load(Read(path), path);
            TopologyDiagram diagram = new TopologyDiagram(chart);
            Select(diagram, chart);

            if (AsWidget.IsPresent) { WriteObject(diagram); return; }
            InteractiveOptions interactive = PSInteractive.Options(this, Mode);
            if (StaticSnapshot.IsPresent || AsString.IsPresent || interactive.ResolveMode() != InteractionMode.Rich) {
                PSFormatting.Emit(this, diagram, Width, null, AsString.IsPresent);
                return;
            }

            try {
                diagram.Run(new ConsoleAppOptions { Interactive = interactive }, _stop.Token);
            } catch (OperationCanceledException) when (_stop.IsCancellationRequested) {
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

        private string Read(string path) {
            try {
                return File.ReadAllText(path);
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) {
                throw Failure("The diagram file cannot be read: " + ex.Message, "DiagramFileUnreadable", ErrorCategory.ReadError, Path);
            }
        }

        private TopologyChart Load(string text, string path) {
            VisualMarkupScanResult scan = VisualMarkupScanner.Scan(text);
            if (scan.Blocks.Count == 0 && scan.Diagnostics.Count == 0) {
                string extension = System.IO.Path.GetExtension(path);
                if (string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".markdown", StringComparison.OrdinalIgnoreCase)) {
                    throw Failure("The Markdown file has no ChartForgeX visual fences.", "DiagramNotFound", ErrorCategory.ObjectNotFound, path);
                }
                MarkupParseResult<TopologyChart> architecture = new MarkupArchitectureParser().Parse(text);
                Report(architecture.Diagnostics, path, fatal: true);
                return architecture.Document ?? throw Failure("The architecture file produced no diagram.", "DiagramNotFound", ErrorCategory.ObjectNotFound, path);
            }

            // Parse each fence on its own so a broken fence elsewhere in the file does not hide the diagram you asked for.
            VisualMarkupParser parser = new VisualMarkupParser();
            List<Candidate> diagrams = new List<Candidate>();
            List<MarkupDiagnostic> others = new List<MarkupDiagnostic>(scan.Diagnostics);
            foreach (VisualMarkupBlock block in scan.Blocks) {
                VisualMarkupParseResult result = parser.ParseBlocks(new[] { block });
                bool found = false;
                if (!result.HasErrors) {
                    foreach (VisualArtifact artifact in result.Artifacts) {
                        TopologyChart? chart = artifact.Model as TopologyChart ?? (artifact.Model is FlowArtifact flow ? flow.ToTopologyChart() : null);
                        if (chart != null) { diagrams.Add(new Candidate(artifact.Id, chart, result.Diagnostics)); found = true; }
                    }
                }
                if (!found) { others.AddRange(result.Diagnostics); }
            }

            Candidate? selected = null;
            if (Diagram != null) {
                selected = diagrams.FirstOrDefault(item => string.Equals(item.Id, Diagram, StringComparison.Ordinal));
            } else if (diagrams.Count == 1) {
                selected = diagrams[0];
            }
            if (selected == null) {
                // Nothing to show: the errors elsewhere in the file explain why.
                Report(others, path, fatal: true);
                if (diagrams.Count == 0) { throw Failure("The file has no architecture, topology or flow diagram.", "DiagramNotFound", ErrorCategory.ObjectNotFound, path); }
                if (Diagram != null) { throw Failure("No diagram has id '" + Diagram + "'. Diagrams: " + Ids(diagrams) + ".", "DiagramNotFound", ErrorCategory.ObjectNotFound, Diagram); }
                throw Failure("The file has " + diagrams.Count.ToString(CultureInfo.InvariantCulture) + " diagrams; choose one with -Diagram: " + Ids(diagrams) + ".", "DiagramAmbiguous", ErrorCategory.InvalidArgument, path);
            }

            Report(selected.Diagnostics, path, fatal: false);
            Report(others, path, fatal: false);
            return selected.Chart;
        }

        private void Select(TopologyDiagram diagram, TopologyChart chart) {
            if (Scenario != null) {
                if (!chart.Scenarios.Any(item => string.Equals(item.Id, Scenario, StringComparison.Ordinal))) {
                    string known = chart.Scenarios.Count == 0 ? "The diagram has no use cases." : "Use cases: " + string.Join(", ", chart.Scenarios.Select(item => item.Id)) + ".";
                    throw Failure("No use case has id '" + Scenario + "'. " + known, "DiagramScenarioNotFound", ErrorCategory.ObjectNotFound, Scenario);
                }
                try { diagram.SelectScenario(Scenario); }
                catch (ArgumentException ex) {
                    // The use case exists but names nodes or connections the diagram does not have.
                    throw Failure("Use case '" + Scenario + "' cannot be shown: " + Reason(ex), "DiagramScenarioInvalid", ErrorCategory.InvalidData, Scenario);
                }
            } else if (diagram.Scenario == null && chart.Scenarios.Count > 0) {
                WriteWarning("None of the diagram's use cases can be shown; they name nodes or connections the diagram does not have.");
            } else if (chart.Scenarios.Count > 0 && diagram.ScenarioId != chart.Scenarios[0].Id) {
                WriteWarning("Use case '" + chart.Scenarios[0].Id + "' names nodes or connections the diagram does not have; showing '" + diagram.ScenarioId + "'.");
            }
            if (!MyInvocation.BoundParameters.ContainsKey(nameof(Step))) { return; }
            TopologyResolvedScenario? scenario = diagram.Scenario;
            if (scenario == null) { throw Failure("The diagram has no use case that can be stepped through.", "DiagramStepUnavailable", ErrorCategory.InvalidArgument, Step); }
            if (scenario.Steps.Count == 0) { throw Failure("Use case '" + scenario.Id + "' has no steps.", "DiagramStepUnavailable", ErrorCategory.InvalidArgument, Step); }
            if (Step > scenario.Steps.Count) {
                throw Failure("Step accepts a number from 1 to " + scenario.Steps.Count.ToString(CultureInfo.InvariantCulture) + " for use case '" + scenario.Id + "'.", "DiagramStepOutOfRange", ErrorCategory.InvalidArgument, Step);
            }
            diagram.Step(Step - 1);
        }

        private void Report(IEnumerable<MarkupDiagnostic> diagnostics, string path, bool fatal) {
            StringBuilder errors = new StringBuilder();
            int count = 0;
            foreach (MarkupDiagnostic diagnostic in diagnostics) {
                string line = System.IO.Path.GetFileName(path) + ":" + diagnostic.Line.ToString(CultureInfo.InvariantCulture)
                    + (diagnostic.Column > 0 ? ":" + diagnostic.Column.ToString(CultureInfo.InvariantCulture) : string.Empty)
                    + (string.IsNullOrEmpty(diagnostic.Code) ? string.Empty : " " + diagnostic.Code) + " " + diagnostic.Message;
                if (!fatal || diagnostic.Severity != VisualDiagnosticSeverity.Error) { WriteWarning(line); continue; }
                if (++count <= MaximumDiagnostics) { errors.Append(errors.Length == 0 ? string.Empty : Environment.NewLine).Append(line); }
            }
            if (count == 0) { return; }
            if (count > MaximumDiagnostics) { errors.Append(Environment.NewLine).Append("... and ").Append((count - MaximumDiagnostics).ToString(CultureInfo.InvariantCulture)).Append(" more errors."); }
            throw Failure("The diagram markup has errors:" + Environment.NewLine + errors, "DiagramMarkupInvalid", ErrorCategory.InvalidData, path);
        }

        // ArgumentException appends the parameter name ("Parameter name: x" on .NET Framework, " (Parameter 'x')" on .NET); people only need the reason.
        private static string Reason(ArgumentException ex) {
            string message = ex.Message;
            int newline = message.IndexOf('\n');
            if (newline >= 0) { message = message.Substring(0, newline).TrimEnd('\r'); }
            return ex.ParamName == null ? message : message.Replace(" (Parameter '" + ex.ParamName + "')", string.Empty);
        }

        private static string Ids(List<Candidate> diagrams) => string.Join(", ", diagrams.Select(item => item.Id));

        // ThrowTerminatingError always throws; returning an exception lets callers write `throw Failure(...)` so the compiler sees the exit.
        private PipelineStoppedException Failure(string message, string errorId, ErrorCategory category, object target) {
            ThrowTerminatingError(new ErrorRecord(new InvalidOperationException(message), errorId, category, target));
            return new PipelineStoppedException();
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            try { _stop.Cancel(); } catch (ObjectDisposedException) { }
        }

        private sealed class Candidate {
            public Candidate(string id, TopologyChart chart, IEnumerable<MarkupDiagnostic> diagnostics) {
                Id = id;
                Chart = chart;
                Diagnostics = new List<MarkupDiagnostic>(diagnostics);
            }

            public string Id { get; }
            public TopologyChart Chart { get; }
            public List<MarkupDiagnostic> Diagnostics { get; }
        }
    }
}
