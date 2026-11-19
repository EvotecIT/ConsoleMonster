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
    /// characters, selectable text, no external assets), plain text. HTML, SVG and text keep cell geometry: every glyph sits on
    /// the grid, colours and styles are kept, and OSC 8 links become links (web and mail links only).
    /// </summary>
    public static class ConsoleExporter {
        private static string? _playerScript;
        private static string? _playerStyles;

        /// <summary>Measures the same rounded cell geometry, padding and chrome used by SVG exports.</summary>
        public static ExportFrameSize MeasureFrame(int columns, int rows, ExportOptions? options = null) {
            if (columns < 1 || rows < 1) { throw new ArgumentOutOfRangeException(nameof(columns), "A frame needs at least one column and row."); }
            SvgGeometry geometry = SvgGeometry.For(columns, rows, Ready(options));
            return new ExportFrameSize(geometry.Width, geometry.Height);
        }

        /// <summary>Measures occupied rows, including painted backgrounds, without trailing empty terminal rows. An empty screen retains one row.</summary>
        public static int MeasureContentRows(CellBuffer frame) {
            if (frame == null) { throw new ArgumentNullException(nameof(frame)); }
            for (int y = frame.Height - 1; y >= 0; y--) { if (HtmlFrameWriter.ContentEnd(frame, y) > 0) { return y + 1; } }
            return 1;
        }

        /// <summary>Measures occupied columns across all rows, including wide glyphs, painted backgrounds and decorations. An empty screen retains one column.</summary>
        public static int MeasureContentColumns(CellBuffer frame) {
            if (frame == null) { throw new ArgumentNullException(nameof(frame)); }
            int columns = 1;
            for (int y = 0; y < frame.Height; y++) { columns = Math.Max(columns, HtmlFrameWriter.ContentEnd(frame, y)); }
            return columns;
        }

        /// <summary>
        /// The recording player script (a native asset, <c>player.js</c>): include it once on a page with
        /// <see cref="PlayerStyles"/>, then mark each player with <c>data-cm-recording</c> pointing at data from
        /// <see cref="Recording.ToPlayerData(IWidget, TextReader, RecordingExportOptions)"/> (a URL, or <c>#id</c> of an inline
        /// <c>application/json</c> script element); <c>data-cm-autoplay</c> asks to autoplay. The HTML recording export embeds it.
        /// </summary>
        public static string PlayerScript => _playerScript ??= ReadAsset("player.js");

        /// <summary>The recording player styles (a native asset, <c>player.css</c>); see <see cref="PlayerScript"/>.</summary>
        public static string PlayerStyles => _playerStyles ??= ReadAsset("player.css");

        /// <summary>Reads a native export asset embedded with the assembly.</summary>
        internal static string ReadAsset(string name) {
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
        public static string ToHtml(this IWidget widget, ExportOptions? options = null) {
            ExportOptions ready = Ready(options);
            return HtmlFrameWriter.Write(Layers(widget, ready), ready);
        }

        /// <summary>Exports a rendered frame as an HTML page. Theme Auto keeps the frame's colours and lets default text follow the viewer.</summary>
        /// <param name="frame">The frame.</param>
        /// <param name="options">Theme, font and window frame (the size is the frame's).</param>
        /// <returns>The HTML.</returns>
        public static string ToHtml(this CellBuffer frame, ExportOptions? options = null) {
            ExportOptions ready = Ready(options);
            return HtmlFrameWriter.Write(Layers(frame, ready), ready);
        }

        /// <summary>Exports what a live session shows now as an HTML page.</summary>
        /// <param name="session">The session.</param>
        /// <param name="options">Theme, font and window frame.</param>
        /// <returns>The HTML.</returns>
        public static string ToHtml(this LiveSession session, ExportOptions? options = null) => ToHtml(SnapshotOf(session), options);

        /// <summary>Exports a widget as an SVG image.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="options">Size, theme, font and window frame.</param>
        /// <returns>The SVG.</returns>
        public static string ToSvg(this IWidget widget, ExportOptions? options = null) {
            ExportOptions ready = Ready(options);
            return SvgFrameWriter.Write(Layers(widget, ready), ready);
        }

        /// <summary>Exports a rendered frame as an SVG image.</summary>
        /// <param name="frame">The frame.</param>
        /// <param name="options">Theme, font and window frame.</param>
        /// <returns>The SVG.</returns>
        public static string ToSvg(this CellBuffer frame, ExportOptions? options = null) {
            ExportOptions ready = Ready(options);
            return SvgFrameWriter.Write(Layers(frame, ready), ready);
        }

        /// <summary>Exports what a live session shows now as an SVG image.</summary>
        /// <param name="session">The session.</param>
        /// <param name="options">Theme, font and window frame.</param>
        /// <returns>The SVG.</returns>
        public static string ToSvg(this LiveSession session, ExportOptions? options = null) => ToSvg(SnapshotOf(session), options);

        /// <summary>Exports a widget as plain text (trailing spaces and empty last lines trimmed).</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="options">Size and glyph tier.</param>
        /// <returns>The text.</returns>
        public static string ToText(this IWidget widget, ExportOptions? options = null) {
            ExportOptions o = Ready(options);
            return ToText(Render(widget, o, o.DarkTheme ?? Theme.Dark));
        }

        /// <summary>Exports a rendered frame as plain text.</summary>
        /// <param name="frame">The frame.</param>
        /// <param name="options">Unused; for symmetry with the other formats.</param>
        /// <returns>The text.</returns>
        public static string ToText(this CellBuffer frame, ExportOptions? options = null) {
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
        public static string ToText(this LiveSession session, ExportOptions? options = null) => ToText(SnapshotOf(session), options);

        // The built-in text formats; FrameExporter dispatches the add-on ones.
        internal static string? Text(IWidget widget, OutputFormat format, ExportOptions? options) {
            switch (format) {
                case OutputFormat.Html: return ToHtml(widget, options);
                case OutputFormat.Svg: return ToSvg(widget, options);
                case OutputFormat.Text: return ToText(widget, options);
                default: return null;
            }
        }

        internal static string? Text(CellBuffer frame, OutputFormat format, ExportOptions? options) {
            switch (format) {
                case OutputFormat.Html: return ToHtml(frame, options);
                case OutputFormat.Svg: return ToSvg(frame, options);
                case OutputFormat.Text: return ToText(frame, options);
                default: return null;
            }
        }

        /// <summary>The format a file extension implies.</summary>
        /// <param name="path">The path.</param>
        /// <returns>Html for .html/.htm, Svg for .svg, Asciicast for .cast, Markdown for .md/.markdown, Png for .png, Text otherwise.</returns>
        public static OutputFormat FormatFor(string path) {
            string extension = Path.GetExtension(path ?? string.Empty).ToLowerInvariant();
            switch (extension) {
                case ".html":
                case ".htm":
                    return OutputFormat.Html;
                case ".svg":
                    return OutputFormat.Svg;
                case ".cast":
                    return OutputFormat.Asciicast;
                case ".md":
                case ".markdown":
                    return OutputFormat.Markdown;
                case ".png":
                    return OutputFormat.Png;
                default:
                    return OutputFormat.Text;
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
            options = Ready(options);
            RenderContext context = CreateContext(options, theme, options.Height > 0 ? options.Height : 24);
            if (options.Height > 0) {
                return FrameComposer.Compose(widget, options.Width, options.Height, context);
            }
            CellBuffer block = FrameComposer.RenderBlock(widget, options.Width, context);
            return block.Height > 0 ? block : FrameComposer.Compose(widget, options.Width, 1, context);
        }

        internal static RenderContext CreateContext(ExportOptions options, ITheme theme, int height) {
            TerminalProfile profile = TerminalProfile.Fixed(options.Width, height, options.ColorDepth, options.Glyphs).WithBorder(options.Border).WithStatusMarkers(options.StatusMarkers).WithDensity(options.Density);
            if (options.Culture != null) {
                profile = profile.WithCulture(options.Culture);
            }
            return Terminal.CreateContext(profile, theme).WithClock(0, options.Elapsed);
        }

        internal static ExportOptions Ready(ExportOptions? options) {
            ExportOptions o = (options ?? new ExportOptions()).ResolveCurrent().Clone();
            o.Width = Terminal.ResolveWidth(o.Width);
            o.Validate();
            return o;
        }

        internal static List<ExportLayer> Layers(IWidget widget, ExportOptions options) {
            if (widget == null) {
                throw new ArgumentNullException(nameof(widget));
            }
            List<ExportLayer> layers = new List<ExportLayer>();
            switch (options.Theme) {
                case ExportTheme.HighContrast:
                    layers.Add(new ExportLayer(Render(widget, options, options.DarkTheme ?? Theme.HighContrast), ExportPalette.ForTheme(options.DarkTheme, ExportPalette.HighContrast), null));
                    break;
                case ExportTheme.Dark:
                    layers.Add(new ExportLayer(Render(widget, options, options.DarkTheme ?? Theme.Dark), ExportPalette.ForTheme(options.DarkTheme, ExportPalette.Dark), null));
                    break;
                case ExportTheme.Light:
                    layers.Add(new ExportLayer(Render(widget, options, options.LightTheme ?? Theme.Light), ExportPalette.ForTheme(options.LightTheme, ExportPalette.Light), null));
                    break;
                default:
                    CellBuffer dark = Render(widget, options, options.DarkTheme ?? Theme.Dark);
                    CellBuffer light = Render(widget, options, options.LightTheme ?? Theme.Light);
                    layers.Add(new ExportLayer(dark, ExportPalette.ForTheme(options.DarkTheme, ExportPalette.Dark), "dark"));
                    layers.Add(new ExportLayer(Fit(light, dark.Width, dark.Height), ExportPalette.ForTheme(options.LightTheme, ExportPalette.Light), "light"));
                    break;
            }
            return layers;
        }

        internal static List<ExportLayer> Layers(CellBuffer frame, ExportOptions options) {
            if (frame == null) {
                throw new ArgumentNullException(nameof(frame));
            }
            ExportPalette palette = options.Theme == ExportTheme.HighContrast ? ExportPalette.HighContrast : options.Theme == ExportTheme.Dark ? ExportPalette.Dark : options.Theme == ExportTheme.Light ? ExportPalette.Light : ExportPalette.Adaptive;
            palette = ExportPalette.ForTheme(options.Theme == ExportTheme.Light ? options.LightTheme : options.DarkTheme, palette);
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
