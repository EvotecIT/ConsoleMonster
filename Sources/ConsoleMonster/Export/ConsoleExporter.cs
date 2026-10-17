using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Export {
    /// <summary>
    /// Exports a widget, a rendered frame or a live-session snapshot to self-contained HTML (inline CSS, selectable text, a
    /// light and a dark rendering that follow <c>prefers-color-scheme</c>), SVG (crisp shapes for box, block and braille
    /// characters, selectable text, no external assets) or plain text. Cell geometry is kept exactly: every glyph sits on
    /// the grid, colours and styles are kept, and OSC 8 links become links (web and mail links only).
    /// </summary>
    public static class ConsoleExporter {
        /// <summary>Exports a widget as an HTML page.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="options">Size, theme, font and window frame.</param>
        /// <returns>The HTML.</returns>
        public static string ToHtml(IWidget widget, ExportOptions? options = null) => HtmlFrameWriter.Write(Layers(widget, Ready(options)), Ready(options));

        /// <summary>Exports a rendered frame as an HTML page. Theme Auto keeps the frame's colours and lets default text follow the viewer.</summary>
        /// <param name="frame">The frame.</param>
        /// <param name="options">Theme, font and window frame (the size is the frame's).</param>
        /// <returns>The HTML.</returns>
        public static string ToHtml(CellBuffer frame, ExportOptions? options = null) => HtmlFrameWriter.Write(Layers(frame, Ready(options)), Ready(options));

        /// <summary>Exports what a live session shows now as an HTML page.</summary>
        /// <param name="session">The session.</param>
        /// <param name="options">Theme, font and window frame.</param>
        /// <returns>The HTML.</returns>
        public static string ToHtml(LiveSession session, ExportOptions? options = null) => ToHtml(SnapshotOf(session), options);

        /// <summary>Exports a widget as an SVG image.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="options">Size, theme, font and window frame.</param>
        /// <returns>The SVG.</returns>
        public static string ToSvg(IWidget widget, ExportOptions? options = null) => SvgFrameWriter.Write(Layers(widget, Ready(options)), Ready(options));

        /// <summary>Exports a rendered frame as an SVG image.</summary>
        /// <param name="frame">The frame.</param>
        /// <param name="options">Theme, font and window frame.</param>
        /// <returns>The SVG.</returns>
        public static string ToSvg(CellBuffer frame, ExportOptions? options = null) => SvgFrameWriter.Write(Layers(frame, Ready(options)), Ready(options));

        /// <summary>Exports what a live session shows now as an SVG image.</summary>
        /// <param name="session">The session.</param>
        /// <param name="options">Theme, font and window frame.</param>
        /// <returns>The SVG.</returns>
        public static string ToSvg(LiveSession session, ExportOptions? options = null) => ToSvg(SnapshotOf(session), options);

        /// <summary>Exports a widget as plain text (trailing spaces and empty last lines trimmed).</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="options">Size and glyph tier.</param>
        /// <returns>The text.</returns>
        public static string ToText(IWidget widget, ExportOptions? options = null) {
            ExportOptions o = Ready(options);
            return ToText(Render(widget, o, o.DarkTheme ?? Theme.Dark));
        }

        /// <summary>Exports a rendered frame as plain text.</summary>
        /// <param name="frame">The frame.</param>
        /// <param name="options">Unused; for symmetry with the other formats.</param>
        /// <returns>The text.</returns>
        public static string ToText(CellBuffer frame, ExportOptions? options = null) {
            if (frame == null) {
                throw new ArgumentNullException(nameof(frame));
            }
            string[] lines = frame.ToPlainText().Split('\n');
            int count = lines.Length;
            while (count > 0 && lines[count - 1].Length == 0) {
                count--;
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++) {
                sb.Append(lines[i]).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Exports what a live session shows now as plain text.</summary>
        /// <param name="session">The session.</param>
        /// <param name="options">Unused; for symmetry with the other formats.</param>
        /// <returns>The text.</returns>
        public static string ToText(LiveSession session, ExportOptions? options = null) => ToText(SnapshotOf(session), options);

        /// <summary>Exports a widget in a format.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="format">Html, Svg or Text.</param>
        /// <param name="options">The options.</param>
        /// <returns>The exported document.</returns>
        public static string Export(IWidget widget, ExportFormat format, ExportOptions? options = null) {
            switch (format) {
                case ExportFormat.Html:
                    return ToHtml(widget, options);
                case ExportFormat.Svg:
                    return ToSvg(widget, options);
                case ExportFormat.Text:
                    return ToText(widget, options);
                default:
                    throw new ArgumentException("Frames export to Html, Svg or Text; Asciicast is for recordings.", nameof(format));
            }
        }

        /// <summary>Exports a frame in a format.</summary>
        /// <param name="frame">The frame.</param>
        /// <param name="format">Html, Svg or Text.</param>
        /// <param name="options">The options.</param>
        /// <returns>The exported document.</returns>
        public static string Export(CellBuffer frame, ExportFormat format, ExportOptions? options = null) {
            switch (format) {
                case ExportFormat.Html:
                    return ToHtml(frame, options);
                case ExportFormat.Svg:
                    return ToSvg(frame, options);
                case ExportFormat.Text:
                    return ToText(frame, options);
                default:
                    throw new ArgumentException("Frames export to Html, Svg or Text; Asciicast is for recordings.", nameof(format));
            }
        }

        /// <summary>Writes an export to a file as UTF-8 without a byte order mark.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="path">The file.</param>
        /// <param name="format">The format; null picks it from the extension (.html/.htm, .svg, anything else text).</param>
        /// <param name="options">The options.</param>
        public static void Save(IWidget widget, string path, ExportFormat? format = null, ExportOptions? options = null) {
            File.WriteAllText(path ?? throw new ArgumentNullException(nameof(path)), Export(widget, format ?? FormatFor(path), options), new UTF8Encoding(false));
        }

        /// <summary>The format a file extension implies.</summary>
        /// <param name="path">The path.</param>
        /// <returns>Html for .html/.htm, Svg for .svg, Asciicast for .cast, Text otherwise.</returns>
        public static ExportFormat FormatFor(string path) {
            string extension = Path.GetExtension(path ?? string.Empty).ToLowerInvariant();
            switch (extension) {
                case ".html":
                case ".htm":
                    return ExportFormat.Html;
                case ".svg":
                    return ExportFormat.Svg;
                case ".cast":
                    return ExportFormat.Asciicast;
                default:
                    return ExportFormat.Text;
            }
        }

        /// <summary>Renders a widget the way an export does (fixed size, the export's glyph tier and colour depth).</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="options">The options.</param>
        /// <param name="theme">The theme.</param>
        /// <returns>The frame.</returns>
        public static CellBuffer Render(IWidget widget, ExportOptions options, ITheme theme) {
            if (widget == null) {
                throw new ArgumentNullException(nameof(widget));
            }
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            RenderContext context = CreateContext(options, theme, options.Height > 0 ? options.Height : 24);
            if (options.Height > 0) {
                return FrameComposer.Compose(widget, options.Width, options.Height, context);
            }
            CellBuffer block = FrameComposer.RenderBlock(widget, options.Width, context);
            return block.Height > 0 ? block : FrameComposer.Compose(widget, options.Width, 1, context);
        }

        internal static RenderContext CreateContext(ExportOptions options, ITheme theme, int height) {
            TerminalProfile profile = TerminalProfile.Fixed(options.Width, height, options.ColorDepth, options.Glyphs);
            if (options.Culture != null) {
                profile = profile.WithCulture(options.Culture);
            }
            return Terminal.CreateContext(profile, theme);
        }

        internal static ExportOptions Ready(ExportOptions? options) {
            ExportOptions o = options ?? new ExportOptions();
            o.Validate();
            return o;
        }

        internal static List<ExportLayer> Layers(IWidget widget, ExportOptions options) {
            if (widget == null) {
                throw new ArgumentNullException(nameof(widget));
            }
            List<ExportLayer> layers = new List<ExportLayer>();
            switch (options.Theme) {
                case ExportTheme.Dark:
                    layers.Add(new ExportLayer(Render(widget, options, options.DarkTheme ?? Theme.Dark), ExportPalette.Dark, null));
                    break;
                case ExportTheme.Light:
                    layers.Add(new ExportLayer(Render(widget, options, options.LightTheme ?? Theme.Light), ExportPalette.Light, null));
                    break;
                default:
                    CellBuffer dark = Render(widget, options, options.DarkTheme ?? Theme.Dark);
                    CellBuffer light = Render(widget, options, options.LightTheme ?? Theme.Light);
                    layers.Add(new ExportLayer(dark, ExportPalette.Dark, "dark"));
                    layers.Add(new ExportLayer(Fit(light, dark.Width, dark.Height), ExportPalette.Light, "light"));
                    break;
            }
            return layers;
        }

        internal static List<ExportLayer> Layers(CellBuffer frame, ExportOptions options) {
            if (frame == null) {
                throw new ArgumentNullException(nameof(frame));
            }
            ExportPalette palette = options.Theme == ExportTheme.Dark ? ExportPalette.Dark : options.Theme == ExportTheme.Light ? ExportPalette.Light : ExportPalette.Adaptive;
            return new List<ExportLayer> { new ExportLayer(frame, palette, null) };
        }

        // Both renderings share one size, so the page does not jump when the scheme changes.
        private static CellBuffer Fit(CellBuffer frame, int width, int height) {
            if (frame.Width == width && frame.Height == height) {
                return frame;
            }
            CellBuffer fitted = new CellBuffer(width, height);
            fitted.Blit(frame, 0, 0, Math.Min(width, frame.Width), Math.Min(height, frame.Height), 0, 0);
            return fitted;
        }

        private static CellBuffer SnapshotOf(LiveSession session) {
            if (session == null) {
                throw new ArgumentNullException(nameof(session));
            }
            return session.Snapshot() ?? throw new InvalidOperationException("The session has not drawn a frame yet (or draws no frames: Stream and Plain outputs).");
        }
    }
}
