using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ChartForgeX.Diagnostics;
using ChartForgeX.Markup;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Graphics {
    /// <summary>Owns file import and diagnostics for Diagram.Load and the PowerShell file adapter.</summary>
    internal static class DiagramFileReader {
        private const int MaximumDiagnostics = 10;

        /// <summary>Reads a file once, selects a diagram and reports nonfatal diagnostics to the host or Trace.</summary>
        internal static Diagram Load(string path, string? id = null, Action<string>? warning = null) {
            if (path == null) throw new ArgumentNullException(nameof(path));
            string text = File.ReadAllText(path);
            VisualMarkupScanResult scan = VisualMarkupScanner.Scan(text);
            if (scan.Blocks.Count == 0 && scan.Diagnostics.Count == 0) {
                string extension = Path.GetExtension(path);
                if (string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".markdown", StringComparison.OrdinalIgnoreCase)) {
                    throw new ImportException("The Markdown file has no ChartForgeX visual fences.", "DiagramNotFound", path);
                }
                MarkupParseResult<TopologyChart> architecture = new MarkupArchitectureParser().Parse(text);
                Report(architecture.Diagnostics, path, warning, fatal: true);
                return DiagramConversion.From(architecture.Document ?? throw new ImportException("The architecture file produced no diagram.", "DiagramNotFound", path));
            }

            // Parsing individual fences would restart generated ids instead of preserving the file-wide sequence.
            VisualMarkupParseResult result = new VisualMarkupParser().Parse(scan);
            var diagrams = new List<Candidate>();
            foreach (VisualArtifact artifact in result.Artifacts) {
                TopologyChart? chart = artifact.Model as TopologyChart ?? (artifact.Model is FlowArtifact flow ? flow.ToTopologyChart() : null);
                if (chart != null) diagrams.Add(new Candidate(artifact, chart));
            }

            Candidate? selected = null;
            string? requestedId = id ?? (diagrams.Count == 1 ? diagrams[0].Id : null);
            List<VisualMarkupBlock> declared = scan.Blocks.Where(block => requestedId != null && IsDiagram(block)
                && block.Attributes.TryGetValue("id", out string? explicitId) && explicitId != null
                && string.Equals(explicitId.Trim(), requestedId, StringComparison.Ordinal)).ToList();
            if (declared.Count > 1) {
                throw new ImportException("More than one diagram has id '" + requestedId + "'; give each diagram a unique id.", "DiagramAmbiguous", requestedId!);
            }
            if (id != null) {
                List<Candidate> matches = diagrams.Where(item => string.Equals(item.Id, id, StringComparison.Ordinal)).ToList();
                if (matches.Count > 1) {
                    throw new ImportException("More than one diagram has id '" + id + "'; give each diagram a unique id.", "DiagramAmbiguous", id);
                }
                selected = matches.FirstOrDefault();
            } else if (diagrams.Count == 1) {
                selected = diagrams[0];
            }
            if (selected == null) {
                // Keep the original parser errors when the requested artifact could not be produced.
                Report(result.Diagnostics, path, warning, fatal: true);
                if (diagrams.Count == 0) throw new ImportException("The file has no architecture, topology or flow diagram.", "DiagramNotFound", path);
                string ids = string.Join(", ", diagrams.Select(item => item.Id).Distinct(StringComparer.Ordinal));
                if (id != null) throw new ImportException("No diagram has id '" + id + "'. Diagrams: " + ids + ".", "DiagramNotFound", id);
                throw new ImportException("The file has " + diagrams.Count.ToString(CultureInfo.InvariantCulture) + " diagrams; choose one by id: " + ids + ".", "DiagramAmbiguous", path);
            }

            // A malformed explicitly matching fence must not disappear behind a valid generated id.
            if (declared.Any(block => block.FenceLine != selected.SourceLine)) {
                Report(result.Diagnostics, path, warning, fatal: true);
                throw new ImportException("More than one diagram has id '" + selected.Id + "'; give each diagram a unique id.", "DiagramAmbiguous", selected.Id);
            }
            Report(result.Diagnostics, path, warning, fatal: false);
            return DiagramConversion.From(selected.Chart);
        }

        private static bool IsDiagram(VisualMarkupBlock block) => block.Kind == VisualMarkupKind.Architecture
            || block.Kind == VisualMarkupKind.Topology || block.Kind == VisualMarkupKind.Flow;

        private static void Report(IEnumerable<MarkupDiagnostic> diagnostics, string path, Action<string>? warning, bool fatal) {
            var errors = new StringBuilder();
            int count = 0;
            foreach (MarkupDiagnostic diagnostic in diagnostics) {
                string line = Path.GetFileName(path) + ":" + diagnostic.Line.ToString(CultureInfo.InvariantCulture)
                    + (diagnostic.Column > 0 ? ":" + diagnostic.Column.ToString(CultureInfo.InvariantCulture) : string.Empty)
                    + (string.IsNullOrEmpty(diagnostic.Code) ? string.Empty : " " + diagnostic.Code) + " " + diagnostic.Message;
                if (!fatal || diagnostic.Severity != VisualDiagnosticSeverity.Error) {
                    if (warning != null) warning(line); else Trace.TraceWarning(line);
                    continue;
                }
                if (++count <= MaximumDiagnostics) errors.Append(errors.Length == 0 ? string.Empty : Environment.NewLine).Append(line);
            }
            if (count == 0) return;
            if (count > MaximumDiagnostics) errors.Append(Environment.NewLine).Append("... and ").Append((count - MaximumDiagnostics).ToString(CultureInfo.InvariantCulture)).Append(" more errors.");
            throw new ImportException("The diagram markup has errors:" + Environment.NewLine + errors, "DiagramMarkupInvalid", path);
        }

        private sealed class Candidate {
            internal Candidate(VisualArtifact artifact, TopologyChart chart) {
                Id = artifact.Id;
                Chart = chart;
                if (artifact.Metadata.TryGetValue("sourceLine", out string? line)
                    && int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sourceLine)) SourceLine = sourceLine;
            }
            internal string Id { get; }
            internal TopologyChart Chart { get; }
            internal int SourceLine { get; }
        }

        /// <summary>Preserves the import failure and target while the PowerShell adapter assigns its error category.</summary>
        internal sealed class ImportException : ArgumentException {
            internal ImportException(string message, string errorId, string target) : base(message) { ErrorId = errorId; Target = target; }
            internal string ErrorId { get; }
            internal string Target { get; }
        }
    }
}
