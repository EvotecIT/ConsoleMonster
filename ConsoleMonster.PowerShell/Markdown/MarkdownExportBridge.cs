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
        internal static IWidget Parse(string content) => global::ConsoleMonster.Markdown.MarkdownConsole.Parse(content);
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Widget(IWidget widget, ExportOptions options, bool fenced) =>
            System.Text.Encoding.UTF8.GetString(FrameExporter.Export(widget, fenced ? "FencedText" : "Markdown", options, new global::ConsoleMonster.Markdown.MarkdownFrameExportProvider(fenced)));

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Frame(CellBuffer frame) => System.Text.Encoding.UTF8.GetString(FrameExporter.Export(frame, "Markdown", provider: new global::ConsoleMonster.Markdown.MarkdownFrameExportProvider()));

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Recording(SessionRecording recording) => global::ConsoleMonster.Markdown.MarkdownExporter.ExportRecording(recording);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Dashboard(IWidget root, TextReader reader, RecordingExportOptions options) =>
            global::ConsoleMonster.Markdown.MarkdownExporter.ExportRecording(root, LiveRecordingReader.Read(reader), options);
    }
}
