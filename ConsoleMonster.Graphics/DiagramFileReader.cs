using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ChartForgeX.Diagnostics;
using ChartForgeX.Markup;
using ChartForgeX.Markup.Mermaid;
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
            string extension = Path.GetExtension(path);
            // A bare Mermaid file is one diagram; reading it as a fence keeps one parse path for files and Markdown.
            if (string.Equals(extension, ".mmd", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".mermaid", StringComparison.OrdinalIgnoreCase)) {
                text = MermaidFence(text);
            }
            VisualMarkupScanResult scan = VisualMarkupScanner.Scan(text);
            if (scan.Blocks.Count == 0 && scan.Diagnostics.Count == 0) {
                if (string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".markdown", StringComparison.OrdinalIgnoreCase)) {
                    throw new ImportException("The Markdown file has no ChartForgeX visual fences.", "DiagramNotFound", path);
                }
                MarkupParseResult<TopologyChart> architecture = new MarkupArchitectureParser().Parse(text);
                Report(architecture.Diagnostics, path, warning, fatal: true);
                return DiagramConversion.From(architecture.Document ?? throw new ImportException("The architecture file produced no diagram.", "DiagramNotFound", path));
            }

            // Parsing individual fences would restart generated ids instead of preserving the file-wide sequence.
            VisualMarkupParseResult result = Parser().Parse(scan);
            var diagrams = new List<Candidate>();
            foreach (VisualArtifact artifact in result.Artifacts) {
                TopologyChart? chart = Topology(artifact, scan);
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
                if (diagrams.Count == 0) {
                    VisualArtifact? mermaid = result.Artifacts.FirstOrDefault(artifact => artifact.Kind == VisualArtifactKind.Mermaid);
                    if (mermaid != null) throw new ImportException("Mermaid '" + MermaidType(mermaid) + "' diagrams don't draw in the console. Use " + MermaidTopologyTypes + ".", "DiagramNotFound", path);
                    throw new ImportException("The file has no architecture, topology, flow or Mermaid diagram.", "DiagramNotFound", path);
                }
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

        /// <summary>Draws one Markdown fence as a diagram, or returns null so the document keeps it as code.</summary>
        /// <remarks>Only Mermaid fences are taken; a fence with errors or a type that is not a topology stays readable source.</remarks>
        internal static Diagram? ParseFence(string? language, string content) {
            if (content == null || !string.Equals(language?.Trim(), "mermaid", StringComparison.OrdinalIgnoreCase)) return null;
            VisualMarkupScanResult scan = VisualMarkupScanner.Scan(MermaidFence(content));
            VisualMarkupParseResult result = Parser().Parse(scan);
            if (result.Diagnostics.Any(diagnostic => diagnostic.Severity == VisualDiagnosticSeverity.Error)) return null;
            TopologyChart? chart = result.Artifacts.Count == 1 ? Topology(result.Artifacts[0], scan) : null;
            return chart == null ? null : DiagramConversion.From(chart);
        }

        // Mermaid types the adapter lays out as topology; the others (sequence, pie, gantt, timeline, gitGraph and
        // similar) produce charts or blocks the terminal diagram cannot draw.
        private const string MermaidTopologyTypes = "flowchart/graph, classDiagram, stateDiagram, erDiagram, architecture-beta, mindmap, C4 or requirementDiagram";

        private static VisualMarkupParser Parser() => new VisualMarkupParser(new MermaidVisualMarkupBlockParser());

        private static TopologyChart? Topology(VisualArtifact artifact, VisualMarkupScanResult scan) {
            TopologyChart? chart = artifact.Model as TopologyChart ?? (artifact.Model is FlowArtifact flow ? flow.ToTopologyChart() : null);
            // An untitled Mermaid diagram gets a family name such as "Mermaid flowchart"; that labels the source, not the drawing.
            if (chart != null && artifact.Kind == VisualArtifactKind.Mermaid && chart.Title != null && chart.Title.StartsWith("Mermaid ", StringComparison.Ordinal)) {
                artifact.Metadata.TryGetValue("sourceLine", out string? line);
                VisualMarkupBlock? block = scan.Blocks.FirstOrDefault(item => item.FenceLine.ToString(CultureInfo.InvariantCulture) == line);
                if (block == null || block.Payload.IndexOf("title", StringComparison.OrdinalIgnoreCase) < 0) chart.Title = string.Empty;
            }
            return chart;
        }

        private static string MermaidType(VisualArtifact artifact) =>
            artifact.Id.StartsWith("mermaid-", StringComparison.Ordinal) ? artifact.Id.Substring("mermaid-".Length) : artifact.Id;

        private static string MermaidFence(string source) {
            // Choose a fence longer than any backtick run in the source so the content cannot close it.
            int longest = 0, run = 0;
            foreach (char c in source) { run = c == '`' ? run + 1 : 0; if (run > longest) longest = run; }
            string fence = new string('`', Math.Max(3, longest + 1));
            return fence + "mermaid\n" + source + (source.EndsWith("\n", StringComparison.Ordinal) ? string.Empty : "\n") + fence + "\n";
        }

        private static bool IsDiagram(VisualMarkupBlock block) => block.Kind == VisualMarkupKind.Architecture
            || block.Kind == VisualMarkupKind.Topology || block.Kind == VisualMarkupKind.Flow || block.Kind == VisualMarkupKind.Mermaid;

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
