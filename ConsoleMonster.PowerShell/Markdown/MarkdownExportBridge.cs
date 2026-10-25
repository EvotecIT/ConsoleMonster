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
        internal static string Widget(IWidget widget, ExportOptions options, bool fenced) =>
            global::ConsoleMonster.Markdown.MarkdownExporter.Export(widget, options, fenced);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Frame(CellBuffer frame) => global::ConsoleMonster.Markdown.MarkdownExporter.Export(frame);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Recording(SessionRecording recording) => global::ConsoleMonster.Markdown.MarkdownExporter.ExportRecording(recording);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Dashboard(IWidget root, TextReader reader, RecordingExportOptions options) =>
            global::ConsoleMonster.Markdown.MarkdownExporter.ExportRecording(root, LiveRecordingReader.Read(reader), options);
    }
}
