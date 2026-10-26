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
        public string Format => Fenced ? "FencedText" : "Markdown";
        /// <inheritdoc />
        public byte[] Export(IWidget widget, ExportOptions? options = null) => new UTF8Encoding(false).GetBytes(MarkdownExporter.Export(widget, options, Fenced));
        /// <inheritdoc />
        public byte[] Export(CellBuffer frame, ExportOptions? options = null) => new UTF8Encoding(false).GetBytes(MarkdownExporter.Export(frame));
        /// <summary>Registers both Markdown formats for an application; safe under trimming and NativeAOT.</summary>
        public static void Register() {
            FrameExporter.Register(new MarkdownFrameExportProvider());
            FrameExporter.Register(new MarkdownFrameExportProvider(true));
        }
    }
}
