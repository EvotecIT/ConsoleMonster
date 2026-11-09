using System;
using System.IO;
using System.Text;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;

namespace ConsoleMonster {
    public static partial class ContinuousIntegration {
        /// <summary>Appends Markdown to GitHub's step summary or uploads a retained Markdown file in Azure. Other hosts only return the Markdown.</summary>
        /// <param name="markdown">The Markdown document.</param>
        /// <param name="write">Optional host stdout sink for Azure's upload command.</param>
        /// <returns>The supplied Markdown, also when no provider summary is available.</returns>
        public static string AddSummary(string markdown, Action<string>? write = null) {
            if (markdown == null) { throw new ArgumentNullException(nameof(markdown)); }
            ContinuousIntegrationProvider provider = Provider;
            lock (OutputGate) {
                if (provider == ContinuousIntegrationProvider.GitHub) {
                    string? path = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
                    if (!string.IsNullOrWhiteSpace(path)) { File.AppendAllText(path!, markdown + "\n", new UTF8Encoding(false)); }
                } else if (provider == ContinuousIntegrationProvider.Azure) {
                    // The agent consumes the file asynchronously. Keep it in its job-owned workspace for upload/cleanup.
                    string root = Environment.GetEnvironmentVariable("SYSTEM_DEFAULTWORKINGDIRECTORY") ?? Path.GetTempPath();
                    string path = Path.Combine(root, "consolemonster-summary-" + Guid.NewGuid().ToString("N") + ".md");
                    File.WriteAllText(path, markdown + "\n", new UTF8Encoding(false));
                    Write("##vso[task.uploadsummary]" + AzureEscape(Path.GetFullPath(path)), write);
                }
            }
            return markdown;
        }

        /// <summary>Exports a widget with the existing semantic Markdown provider, then adds it to the job summary.</summary>
        /// <param name="widget">The table, record, tree or other widget.</param>
        /// <param name="options">Export sizing and formatting.</param>
        /// <param name="provider">A Markdown provider from ConsoleMonster.Markdown, or null to use a registered provider.</param>
        /// <param name="write">Optional host stdout sink.</param>
        /// <returns>The exported Markdown.</returns>
        public static string AddSummary(IWidget widget, ExportOptions? options = null, IFrameExportProvider? provider = null, Action<string>? write = null) =>
            AddSummary(Encoding.UTF8.GetString(FrameExporter.Export(widget, "Markdown", options, provider)), write);
    }
}
