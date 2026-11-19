using System.Runtime.CompilerServices;
using System.IO;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    /// <summary>Keeps optional Markdown code outside the JIT path for ordinary core exports.</summary>
    internal static class MarkdownExportBridge {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IWidget Parse(string content) {
            // The bundled Graphics add-on draws Mermaid fences; documents without one never load it.
            if (content.IndexOf("mermaid", System.StringComparison.OrdinalIgnoreCase) >= 0) RegisterDiagrams();
            return global::ConsoleMonster.Markdown.MarkdownConsole.Parse(content);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterDiagrams() => global::ConsoleMonster.Graphics.DiagramRendererProvider.Register();
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Widget(IWidget widget, ExportOptions options, bool fenced) =>
            FrameExporter.Render(widget, fenced ? OutputFormat.FencedText : OutputFormat.Markdown, options, new global::ConsoleMonster.Markdown.MarkdownFrameExportProvider(fenced)).ToText();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Frame(CellBuffer frame) => FrameExporter.Render(frame, OutputFormat.Markdown, provider: new global::ConsoleMonster.Markdown.MarkdownFrameExportProvider()).ToText();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Recording(SessionRecording recording) => global::ConsoleMonster.Markdown.MarkdownExporter.ToMarkdown(recording);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Dashboard(IWidget root, TextReader reader, RecordingExportOptions options) =>
            global::ConsoleMonster.Markdown.MarkdownExporter.ReplayToMarkdown(root, LiveRecordingReader.Read(reader), options);
    }
}
