using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ChartForgeX.Raster;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Sessions;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Graphics {
    /// <summary>Image and animation extensions alongside ConsoleExporter. External engines stay in this add-on.</summary>
    public static class ConsoleGraphicsExtensions {
        /// <summary>Rasterises a frame with the core's geometric glyphs and ChartForgeX typography.</summary>
        public static RgbaImage ToRaster(this CellBuffer frame, GraphicsExportOptions? options = null) => GraphicsRenderer.Render(frame, options ?? new GraphicsExportOptions());
        /// <summary>Encodes a rendered frame as PNG.</summary>
        public static byte[] ToPng(this CellBuffer frame, GraphicsExportOptions? options = null) => GraphicsRenderer.Png(frame, options ?? new GraphicsExportOptions());
        /// <summary>Renders a widget and encodes PNG.</summary>
        public static byte[] ToPng(this IWidget widget, GraphicsExportOptions? options = null) {
            GraphicsExportOptions settings = options ?? new GraphicsExportOptions();
            return Render(widget, settings).ToPng(settings);
        }
        /// <summary>Encodes the latest live-session snapshot as PNG.</summary>
        public static byte[] ToPng(this LiveSession session, GraphicsExportOptions? options = null) =>
            (session.Snapshot() ?? throw new InvalidOperationException("The live session has not drawn a frame.")).ToPng(options);
        /// <summary>Saves a frame as PNG.</summary>
        public static void SavePng(this CellBuffer frame, string path, GraphicsExportOptions? options = null) => GraphicsOutput.Bytes(path, frame.ToPng(options));
        /// <summary>Renders a widget and saves PNG.</summary>
        public static void SavePng(this IWidget widget, string path, GraphicsExportOptions? options = null) => GraphicsOutput.Bytes(path, widget.ToPng(options));
        /// <summary>Saves a recording's final frame, or the frame at a source time.</summary>
        public static void SavePng(this SessionRecording recording, string path, GraphicsExportOptions? options = null, TimeSpan? at = null) =>
            (at.HasValue ? recording.FrameAt(at.Value) : recording.FinalFrame()).SavePng(path, options);
        /// <summary>Saves one numbered PNG per chapter. Destination is a directory; chapter labels never become paths.</summary>
        public static IReadOnlyList<string> SaveChapterPngs(this SessionRecording recording, string directory, GraphicsExportOptions? options = null, CancellationToken cancellationToken = default) {
            if (recording.Chapters.Count == 0) { throw new InvalidOperationException("The recording has no chapters. Add # demo: chapter directives."); }
            Directory.CreateDirectory(directory);
            List<string> paths = new List<string>();
            for (int i = 0; i < recording.Chapters.Count; i++) {
                cancellationToken.ThrowIfCancellationRequested();
                string path = Path.Combine(directory, "slide-" + (i + 1).ToString("D3", System.Globalization.CultureInfo.InvariantCulture) + ".png");
                recording.ChapterFrame(i).SavePng(path, options); paths.Add(path);
            }
            return paths.AsReadOnly();
        }
        /// <summary>Encodes a terminal recording to GIF or APNG. PNG selects the final frame.</summary>
        public static void SaveGraphics(this SessionRecording recording, string path, GraphicsFormat format, GraphicsExportOptions? options = null, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            GraphicsOutput.ValidatePath(path);
            GraphicsExportOptions settings = options ?? new GraphicsExportOptions();
            if (format == GraphicsFormat.Png) { recording.SavePng(path, settings); return; }
            SaveFrames(recording.CaptureFrames(Playback(settings), trimEmptyRows: settings.Preset == GraphicsPreset.Docs), path, format, settings, cancellationToken);
        }
        /// <summary>Encodes recorded dashboard events through the existing widget replay engine.</summary>
        public static void SaveGraphics(this IWidget dashboard, IEnumerable<LiveEvent> events, string path, GraphicsFormat format,
            GraphicsExportOptions? options = null, CancellationToken cancellationToken = default) {
            GraphicsExportOptions settings = options ?? new GraphicsExportOptions();
            SaveFrames(Recording.CaptureFrames(dashboard, events, Playback(settings)), path, format, settings, cancellationToken);
        }
        private static RecordingExportOptions Playback(GraphicsExportOptions settings) {
            settings.Validate();
            RecordingExportOptions source = settings.Playback;
            return new RecordingExportOptions {
                Width = source.Width, Height = source.Height, Theme = settings.Frame.Theme,
                CustomTheme = settings.Frame.Theme == ExportTheme.Light ? settings.Frame.LightTheme : settings.Frame.DarkTheme,
                // Speed-up must not turn a ten-fps export into a sixty-fps animation.
                FramesPerSecond = source.FramesPerSecond / Math.Max(1, source.Speed), MaxFrames = source.MaxFrames,
                Speed = source.Speed, HoldLastFrame = source.HoldLastFrame, Glyphs = source.Glyphs,
                Culture = source.Culture, Title = source.Title
            };
        }
        private static CellBuffer Render(IWidget widget, GraphicsExportOptions settings) => ConsoleExporter.Render(widget, settings.Frame,
            settings.Frame.Theme == ExportTheme.Light ? settings.Frame.LightTheme ?? Theme.Light : settings.Frame.DarkTheme ?? (settings.Frame.Theme == ExportTheme.HighContrast ? Theme.HighContrast : Theme.Dark));
        private static bool IsEmpty(CellBuffer frame) {
            for (int y = 0; y < frame.Height; y++) {
                for (int x = 0; x < frame.Width; x++) {
                    if (!string.IsNullOrWhiteSpace(frame[x, y].Glyph)) { return false; }
                }
            }
            return true;
        }
        private static void SaveFrames(IReadOnlyList<RecordedFrame> frames, string path, GraphicsFormat format, GraphicsExportOptions settings, CancellationToken token) {
            settings.Validate(); token.ThrowIfCancellationRequested();
            GraphicsOutput.ValidatePath(path);
            if (format == GraphicsFormat.Gif) {
                int start = 0;
                while (start < frames.Count - 1 && IsEmpty(frames[start].Frame)) { start++; }
                if (start > 0) {
                    List<RecordedFrame> visible = new List<RecordedFrame>();
                    for (int i = start; i < frames.Count; i++) { visible.Add(frames[i]); }
                    frames = visible;
                }
            }
            if (frames.Count == 0) { throw new ArgumentException("The recording has no frames."); }
            if (format == GraphicsFormat.Png) { frames[frames.Count - 1].Frame.SavePng(path, settings); return; }
            // Render on demand: GIF's palette and encoding passes retain only bounded RGBA working buffers.
            int columns = 1, rows = 1;
            foreach (RecordedFrame frame in frames) {
                columns = Math.Max(columns, GraphicsRenderer.ContentColumns(frame.Frame, settings));
                rows = Math.Max(rows, settings.Preset != GraphicsPreset.Docs && settings.Fit == GraphicsFit.Content ? ConsoleExporter.MeasureContentRows(frame.Frame) : frame.Frame.Height);
            }
            GraphicsLayout layout = GraphicsRenderer.Layout(frames[0].Frame, settings, columns, rows);
            RgbaImage first = GraphicsRenderer.Render(frames[0].Frame, settings, layout);
            RasterAnimationSource source = new RasterAnimationSource(first.Width, first.Height, frames.Count, (index, cancellation) => {
                cancellation.ThrowIfCancellationRequested();
                double duration = frames[index].Duration;
                double hold = index == frames.Count - 1 ? Math.Min(duration, settings.Playback.HoldLastFrame.TotalMilliseconds) : 0;
                if (settings.IdleLimit > TimeSpan.Zero) { duration = Math.Min(duration - hold, settings.IdleLimit.TotalMilliseconds) + hold; }
                return new RasterAnimationFrame(index == 0 ? first : GraphicsRenderer.Render(frames[index].Frame, settings, layout), TimeSpan.FromMilliseconds(Math.Max(10, duration)));
            });
            if (format != GraphicsFormat.Gif && format != GraphicsFormat.Apng) { throw new ArgumentOutOfRangeException(nameof(format)); }
            GraphicsOutput.Write(path, stream => RasterAnimationEncoder.WriteTo(stream, source,
                format == GraphicsFormat.Gif ? RasterAnimationFormat.Gif : RasterAnimationFormat.Apng,
                new RasterAnimationOptions { PlayCount = settings.LoopCount }, token));
        }
    }
}
