using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Text;
using ConsoleMonster.Export;
using ConsoleMonster.Graphics;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    // No graphics types appear in cmdlet parameters, fields or return types. This bridge is JITted only for graphics exports.
    internal static class GraphicsExportBridge {
        internal static bool IsGraphics(string? format, string? path) {
            string name = format ?? (path == null ? "" : System.IO.Path.GetExtension(path).TrimStart('.'));
            return string.Equals(name, "Png", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Gif", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Apng", StringComparison.OrdinalIgnoreCase);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static object Frame(IReadOnlyList<object> items, string? path, string preset, string? font, string fit, string background, string? backgroundColor, string? backgroundEndColor, ExportOptions options, PSCmdlet cmdlet, double maxFontSize, string[]? fontFallbacks) {
            GraphicsExportOptions settings = Settings(preset, font, fit, background, backgroundColor, backgroundEndColor, options, maxFontSize, fontFallbacks);
            byte[] bytes;
            object raw = items.Count == 1 ? PSObject.AsPSObject(items[0]).BaseObject : new object();
            if (raw is Canvas.CellBuffer frame) { bytes = FrameExporter.Export(frame, "Png", options, new PngFrameExportProvider(settings)); }
            else if (raw is LiveSession session) { bytes = FrameExporter.Export(session.Snapshot() ?? throw new PSInvalidOperationException("The session has not drawn a frame yet."), "Png", options, new PngFrameExportProvider(settings)); }
            else { bytes = FrameExporter.Export(PSOutputExport.Widget(items, cmdlet), "Png", options, new PngFrameExportProvider(settings)); }
            if (path == null) { return bytes; }
            File.WriteAllBytes(path, bytes); return new FileInfo(path);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IReadOnlyList<string> Recording(string source, string target, string? format, string preset, string? font, string fit, string background, string? backgroundColor, string? backgroundEndColor,
            ExportOptions frame, RecordingExportOptions playback, int loops, IWidget? dashboard, TimeSpan? at, string? chapter, bool perChapter, System.Threading.CancellationToken cancellationToken, double maxFontSize, string[]? fontFallbacks) {
            GraphicsExportOptions settings = Settings(preset, font, fit, background, backgroundColor, backgroundEndColor, frame, maxFontSize, fontFallbacks);
            settings.Playback = playback; settings.LoopCount = loops;
            GraphicsFormat selected = (GraphicsFormat)Enum.Parse(typeof(GraphicsFormat), format ?? System.IO.Path.GetExtension(target).TrimStart('.'), true);
            if (dashboard != null) {
                using (StreamReader reader = new StreamReader(source, Encoding.UTF8)) {
                    dashboard.SaveGraphics(LiveRecordingReader.Read(reader), target, selected, settings, cancellationToken);
                }
            } else {
                SessionRecording recording = SessionRecording.Read(source);
                if (frame.Title == null) { frame.Title = recording.Title; }
                if (perChapter) { return recording.SaveChapterPngs(target, settings, cancellationToken); }
                if (selected == GraphicsFormat.Png) {
                    if (chapter != null) {
                        int found = -1;
                        for (int i = 0; i < recording.Chapters.Count; i++) { if (recording.Chapters[i].Label == chapter) { found = i; break; } }
                        if (found < 0) { throw new ArgumentException("The recording has no chapter named " + chapter + "."); }
                        recording.ChapterFrame(found).SavePng(target, settings);
                    } else { recording.SavePng(target, settings, at); }
                } else { recording.SaveGraphics(target, selected, settings, cancellationToken); }
            }
            return new[] { target };
        }
        private static GraphicsExportOptions Settings(string preset, string? font, string fit, string background, string? color, string? endColor, ExportOptions frame, double maxFontSize, string[]? fontFallbacks) {
            var settings = new GraphicsExportOptions {
            Preset = (GraphicsPreset)Enum.Parse(typeof(GraphicsPreset), preset, true), FontFamily = font, Frame = frame,
            Fit = (GraphicsFit)Enum.Parse(typeof(GraphicsFit), fit, true), Background = (GraphicsBackground)Enum.Parse(typeof(GraphicsBackground), background, true),
            BackgroundColor = color, BackgroundEndColor = endColor, MaxFontSize = maxFontSize
            };
            foreach (string fallback in fontFallbacks ?? Array.Empty<string>()) { settings.FontFallbacks.Add(fallback); }
            return settings;
        }
    }
}
