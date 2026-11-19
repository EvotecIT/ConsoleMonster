using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Markdown {
    /// <summary>Connects the add-on's existing Markdown writer to frame and fluent export dispatch.</summary>
    public sealed class MarkdownFrameExportProvider : IFrameExportProvider {
        /// <summary>Creates a semantic Markdown or fenced-text provider.</summary>
        /// <param name="fenced">Preserves terminal layout instead of semantic widget structure.</param>
        public MarkdownFrameExportProvider(bool fenced = false) { Fenced = fenced; }
        /// <summary>Whether widgets export as fenced terminal text.</summary>
        public bool Fenced { get; }
        /// <inheritdoc />
        public OutputFormat Format => Fenced ? OutputFormat.FencedText : OutputFormat.Markdown;
        /// <inheritdoc />
        public byte[] Render(IWidget widget, ExportOptions? options = null) => new UTF8Encoding(false).GetBytes(MarkdownExporter.ToMarkdown(widget, options, Fenced));
        /// <inheritdoc />
        public byte[] Render(CellBuffer frame, ExportOptions? options = null) => new UTF8Encoding(false).GetBytes(MarkdownExporter.ToMarkdown(frame));
        /// <summary>Registers both Markdown formats for an application; safe under trimming and NativeAOT.</summary>
        public static void Register() {
            FrameExporter.Register(new MarkdownFrameExportProvider());
            FrameExporter.Register(new MarkdownFrameExportProvider(true));
        }
    }
}
