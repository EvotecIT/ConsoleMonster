using System;
using System.Collections.Generic;
using System.IO;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Export {
    /// <summary>A format an add-on renders. Add-ons own conversion and encoding.</summary>
    public interface IFrameExportProvider {
        /// <summary>The format this provider renders.</summary>
        OutputFormat Format { get; }
        /// <summary>Renders a widget with its semantic structure intact.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="options">Frame size, theme and typography.</param>
        /// <returns>The encoded output.</returns>
        byte[] Render(IWidget widget, ExportOptions? options = null);
        /// <summary>Renders an already rendered frame.</summary>
        /// <param name="frame">The frame.</param>
        /// <param name="options">Frame size, theme and typography.</param>
        /// <returns>The encoded output.</returns>
        byte[] Render(CellBuffer frame, ExportOptions? options = null);
    }

    /// <summary>One core/add-on dispatch boundary for frame output, without runtime assembly discovery.</summary>
    public static class FrameExporter {
        private static readonly object Gate = new object();
        private static readonly Dictionary<OutputFormat, IFrameExportProvider> Providers = new Dictionary<OutputFormat, IFrameExportProvider>();

        /// <summary>Registers an add-on format for this application. Existing registrations are replaced explicitly.</summary>
        /// <param name="provider">The add-on's typed provider; no reflection is used.</param>
        public static void Register(IFrameExportProvider provider) {
            if (provider == null) { throw new ArgumentNullException(nameof(provider)); }
            if (IsCore(provider.Format)) { throw new ArgumentException("An add-on cannot replace a core format.", nameof(provider)); }
            lock (Gate) { Providers[provider.Format] = provider; }
        }

        /// <summary>Reads a format name (<c>html</c>, <c>svg</c>, <c>text</c>, <c>markdown</c>, <c>png</c>...) or, without one, the format a path implies.</summary>
        /// <param name="format">The name, or null to infer it from <paramref name="path"/>.</param>
        /// <param name="path">The output path, when available.</param>
        /// <returns>The format; Html when neither is given.</returns>
        public static OutputFormat ResolveFormat(string? format = null, string? path = null) {
            if (string.IsNullOrWhiteSpace(format)) {
                return string.IsNullOrEmpty(path) ? OutputFormat.Html : ConsoleExporter.FormatFor(path!);
            }
            switch (format!.Trim().TrimStart('.').ToLowerInvariant()) {
                case "html": case "htm": return OutputFormat.Html;
                case "svg": return OutputFormat.Svg;
                case "txt": case "text": return OutputFormat.Text;
                case "cast": case "asciicast": return OutputFormat.Asciicast;
                case "md": case "markdown": return OutputFormat.Markdown;
                case "fencedtext": return OutputFormat.FencedText;
                case "png": return OutputFormat.Png;
                default: throw new ArgumentException("Unknown output format '" + format + "'.", nameof(format));
            }
        }

        /// <summary>Renders a widget in a core or registered add-on format.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="format">The format.</param>
        /// <param name="options">Frame size, theme and typography.</param>
        /// <param name="provider">Optional per-call add-on provider with typed add-on settings.</param>
        /// <returns>The rendered output.</returns>
        public static RenderedOutput Render(IWidget widget, OutputFormat format, ExportOptions? options = null, IFrameExportProvider? provider = null) {
            if (widget == null) { throw new ArgumentNullException(nameof(widget)); }
            string? text = ConsoleExporter.Text(widget, format, options);
            return text != null ? RenderedOutput.FromText(format, text) : new RenderedOutput(format, ResolveProvider(format, provider).Render(widget, options));
        }

        /// <summary>Renders a frame through the same core/add-on dispatch.</summary>
        /// <param name="frame">The frame.</param>
        /// <param name="format">The format.</param>
        /// <param name="options">Theme and typography (the size is the frame's).</param>
        /// <param name="provider">Optional per-call add-on provider.</param>
        /// <returns>The rendered output.</returns>
        public static RenderedOutput Render(CellBuffer frame, OutputFormat format, ExportOptions? options = null, IFrameExportProvider? provider = null) {
            if (frame == null) { throw new ArgumentNullException(nameof(frame)); }
            string? text = ConsoleExporter.Text(frame, format, options);
            return text != null ? RenderedOutput.FromText(format, text) : new RenderedOutput(format, ResolveProvider(format, provider).Render(frame, options));
        }

        /// <summary>Writes a widget to a file, in the given format or the one its extension implies.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="path">The file.</param>
        /// <param name="format">The format, or null to infer it from the extension.</param>
        /// <param name="options">Frame size, theme and typography.</param>
        /// <param name="provider">Optional per-call add-on provider.</param>
        public static void Save(IWidget widget, string path, OutputFormat? format = null, ExportOptions? options = null, IFrameExportProvider? provider = null) {
            if (path == null) { throw new ArgumentNullException(nameof(path)); }
            File.WriteAllBytes(path, Render(widget, format ?? ConsoleExporter.FormatFor(path), options, provider).Bytes);
        }

        /// <summary>Writes a widget to a stream in a format.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="stream">The stream; it stays open.</param>
        /// <param name="format">The format.</param>
        /// <param name="options">Frame size, theme and typography.</param>
        /// <param name="provider">Optional per-call add-on provider.</param>
        public static void Save(IWidget widget, Stream stream, OutputFormat format, ExportOptions? options = null, IFrameExportProvider? provider = null) {
            if (stream == null) { throw new ArgumentNullException(nameof(stream)); }
            byte[] bytes = Render(widget, format, options, provider).Bytes;
            stream.Write(bytes, 0, bytes.Length);
        }

        private static bool IsCore(OutputFormat format) => format == OutputFormat.Html || format == OutputFormat.Svg || format == OutputFormat.Text || format == OutputFormat.Asciicast;

        private static IFrameExportProvider ResolveProvider(OutputFormat format, IFrameExportProvider? supplied) {
            if (format == OutputFormat.Asciicast) { throw new ArgumentException("Asciicast is a recording format; frames render to Html, Svg, Text or an add-on format.", nameof(format)); }
            if (supplied != null) {
                if (format != supplied.Format) { throw new ArgumentException("The provider renders " + supplied.Format + ", not " + format + ".", nameof(supplied)); }
                return supplied;
            }
            lock (Gate) { if (Providers.TryGetValue(format, out IFrameExportProvider? registered)) { return registered; } }
            string package = format == OutputFormat.Png ? "ConsoleMonster.Graphics" : "ConsoleMonster.Markdown";
            throw new NotSupportedException(format + " output requires the " + package + " package. Register its frame provider or pass it to Save or Render.");
        }
    }
}
