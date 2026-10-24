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
        /// <summary>Returns the completed view of a chapter, immediately before the next chapter begins.</summary>
        public static CellBuffer ChapterFrame(this SessionRecording recording, int index) {
            IReadOnlyList<RecordingMarker> chapters = recording.Chapters;
            if (index < 0 || index >= chapters.Count) { throw new ArgumentOutOfRangeException(nameof(index)); }
            return index + 1 < chapters.Count ? recording.FrameAt(chapters[index + 1].Time > TimeSpan.Zero ? chapters[index + 1].Time - TimeSpan.FromTicks(1) : TimeSpan.Zero) : recording.FinalFrame();
        }
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
        /// <summary>Encodes a terminal recording to GIF, APNG or optional ffmpeg MP4. PNG selects the final frame.</summary>
        public static void SaveGraphics(this SessionRecording recording, string path, GraphicsFormat format, GraphicsExportOptions? options = null, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            GraphicsExportOptions settings = options ?? new GraphicsExportOptions();
            if (format == GraphicsFormat.Png) { recording.SavePng(path, settings); return; }
            SaveFrames(recording.CaptureFrames(Playback(settings)), path, format, settings, cancellationToken);
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
        private static void SaveFrames(IReadOnlyList<RecordedFrame> frames, string path, GraphicsFormat format, GraphicsExportOptions settings, CancellationToken token) {
            settings.Validate(); token.ThrowIfCancellationRequested();
            if (frames.Count == 0) { throw new ArgumentException("The recording has no frames."); }
            if (format == GraphicsFormat.Png) { frames[frames.Count - 1].Frame.SavePng(path, settings); return; }
            // Render on demand: GIF's palette and encoding passes retain only bounded RGBA working buffers.
            RgbaImage first = frames[0].Frame.ToRaster(settings);
            RasterAnimationSource source = new RasterAnimationSource(first.Width, first.Height, frames.Count, (index, cancellation) => {
                cancellation.ThrowIfCancellationRequested();
                double duration = frames[index].Duration;
                if (index < frames.Count - 1 && settings.IdleLimit > TimeSpan.Zero) { duration = Math.Min(duration, settings.IdleLimit.TotalMilliseconds); }
                return new RasterAnimationFrame(index == 0 ? first : frames[index].Frame.ToRaster(settings), TimeSpan.FromMilliseconds(Math.Max(10, duration)));
            });
            if (format == GraphicsFormat.Mp4) { FfmpegWriter.Write(path, source, settings.Playback.FramesPerSecond, token); return; }
            if (format != GraphicsFormat.Gif && format != GraphicsFormat.Apng) { throw new ArgumentOutOfRangeException(nameof(format)); }
            GraphicsOutput.Write(path, stream => RasterAnimationEncoder.WriteTo(stream, source,
                format == GraphicsFormat.Gif ? RasterAnimationFormat.Gif : RasterAnimationFormat.Apng,
                new RasterAnimationOptions { PlayCount = settings.LoopCount }, token));
        }
    }
}
