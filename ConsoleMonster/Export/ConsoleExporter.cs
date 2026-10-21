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
    /// characters, selectable text, no external assets), fenced preformatted Markdown or plain text. Cell geometry is kept exactly: every glyph sits on
    /// the grid, colours and styles are kept, and OSC 8 links become links (web and mail links only).
    /// </summary>
    public static class ConsoleExporter {
        private static string? _playerScript;
        private static string? _playerStyles;

        /// <summary>
        /// The recording player script (a native asset, <c>player.js</c>): include it once on a page with
        /// <see cref="PlayerStyles"/>, then mark each player with <c>data-cm-recording</c> pointing at data from
        /// <see cref="Recording.ExportPlayerData(IWidget, TextReader, RecordingExportOptions)"/> (a URL, or <c>#id</c> of an inline
        /// <c>application/json</c> script element); <c>data-cm-autoplay</c> asks to autoplay. The HTML recording export embeds it.
        /// </summary>
        public static string PlayerScript => _playerScript ??= ReadAsset("player.js");

        /// <summary>The recording player styles (a native asset, <c>player.css</c>); see <see cref="PlayerScript"/>.</summary>
        public static string PlayerStyles => _playerStyles ??= ReadAsset("player.css");

        private static string ReadAsset(string name) {
            using (Stream? stream = typeof(ConsoleExporter).Assembly.GetManifestResourceStream("ConsoleMonster.Export.Assets." + name)) {
                if (stream == null) {
                    throw new InvalidOperationException("The embedded asset " + name + " is missing.");
                }
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8)) {
                    return reader.ReadToEnd();
                }
            }
        }

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

        /// <summary>Exports a widget as fenced preformatted Markdown, preserving the rendered layout rather than inferring table semantics.</summary>
        /// <param name="widget">The widget to render.</param>
        /// <param name="options">Size, culture and glyph tier.</param>
        /// <returns>A Markdown code block without terminal escape sequences.</returns>
        public static string ToMarkdown(IWidget widget, ExportOptions? options = null) => MarkdownFrameWriter.Write(ToText(widget, options));

        /// <summary>Exports a frame as fenced preformatted Markdown.</summary>
        /// <param name="frame">The rendered frame.</param>
        /// <param name="options">Unused; the frame keeps its size.</param>
        /// <returns>A Markdown code block with a fence longer than any backtick run in the content.</returns>
        public static string ToMarkdown(CellBuffer frame, ExportOptions? options = null) => MarkdownFrameWriter.Write(ToText(frame, options));

        /// <summary>Exports the current live-session snapshot as fenced preformatted Markdown.</summary>
        /// <param name="session">A session that has drawn a frame.</param>
        /// <param name="options">Unused; the snapshot keeps its size.</param>
        /// <returns>A Markdown code block.</returns>
        public static string ToMarkdown(LiveSession session, ExportOptions? options = null) => ToMarkdown(SnapshotOf(session), options);

        /// <summary>Exports a widget in a format.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="format">Html, Svg, Text or Markdown.</param>
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
                case ExportFormat.Markdown:
                    return ToMarkdown(widget, options);
                default:
                    throw new ArgumentException("Frames export to Html, Svg, Text or Markdown; Asciicast is for recordings.", nameof(format));
            }
        }

        /// <summary>Exports a frame in a format.</summary>
        /// <param name="frame">The frame.</param>
        /// <param name="format">Html, Svg, Text or Markdown.</param>
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
                case ExportFormat.Markdown:
                    return ToMarkdown(frame, options);
                default:
                    throw new ArgumentException("Frames export to Html, Svg, Text or Markdown; Asciicast is for recordings.", nameof(format));
            }
        }

        /// <summary>Writes an export to a file as UTF-8 without a byte order mark.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="path">The file.</param>
        /// <param name="format">The format; null picks it from the extension (.html/.htm, .svg, .md/.markdown, anything else text).</param>
        /// <param name="options">The options.</param>
        public static void Save(IWidget widget, string path, ExportFormat? format = null, ExportOptions? options = null) {
            File.WriteAllText(path ?? throw new ArgumentNullException(nameof(path)), Export(widget, format ?? FormatFor(path), options), new UTF8Encoding(false));
        }

        /// <summary>The format a file extension implies.</summary>
        /// <param name="path">The path.</param>
        /// <returns>Html for .html/.htm, Svg for .svg, Markdown for .md/.markdown, Asciicast for .cast, Text otherwise.</returns>
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
                case ".md":
                case ".markdown":
                    return ExportFormat.Markdown;
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
            return Terminal.CreateContext(profile, theme).WithClock(0, options.Elapsed);
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
