using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Export {
    /// <summary>A typed optional frame format. Add-ons own conversion and encoding.</summary>
    public interface IFrameExportProvider {
        /// <summary>The public format name used by the command and fluent API.</summary>
        string Format { get; }
        /// <summary>Exports a widget with its semantic structure intact.</summary>
        byte[] Export(IWidget widget, ExportOptions? options = null);
        /// <summary>Exports an already rendered frame.</summary>
        byte[] Export(CellBuffer frame, ExportOptions? options = null);
    }

    /// <summary>One core/add-on dispatch boundary for frame exports, without runtime assembly discovery.</summary>
    public static class FrameExporter {
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, IFrameExportProvider> Providers = new Dictionary<string, IFrameExportProvider>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Registers an add-on format for this application. Existing registrations are replaced explicitly.</summary>
        /// <param name="provider">The add-on's typed provider; no reflection is used.</param>
        public static void Register(IFrameExportProvider provider) {
            if (provider == null) { throw new ArgumentNullException(nameof(provider)); }
            if (string.IsNullOrWhiteSpace(provider.Format)) { throw new ArgumentException("A format name is required.", nameof(provider)); }
            if (CoreFormat(provider.Format).HasValue) { throw new ArgumentException("An add-on cannot replace a core format.", nameof(provider)); }
            lock (Gate) { Providers[provider.Format] = provider; }
        }

        /// <summary>Resolves a format name or file extension without loading an add-on.</summary>
        /// <param name="format">The explicit name, or null to infer it from path.</param>
        /// <param name="path">The output path, when available.</param>
        /// <returns>The normalized public format name.</returns>
        public static string ResolveFormat(string? format = null, string? path = null) {
            string name = format ?? Path.GetExtension(path ?? string.Empty).TrimStart('.');
            switch (name.ToLowerInvariant()) {
                case "": case "html": case "htm": return "Html";
                case "svg": return "Svg";
                case "txt": case "text": return "Text";
                case "md": case "markdown": return "Markdown";
                case "fencedtext": return "FencedText";
                case "png": return "Png";
                default: return name;
            }
        }

        /// <summary>Exports a widget as deterministic bytes in a core or registered add-on format.</summary>
        /// <param name="widget">The widget.</param>
        /// <param name="format">The public format name.</param>
        /// <param name="options">Frame size, theme and typography.</param>
        /// <param name="provider">Optional per-call add-on provider with typed add-on settings.</param>
        /// <returns>The encoded document or image.</returns>
        public static byte[] Export(IWidget widget, string format, ExportOptions? options = null, IFrameExportProvider? provider = null) {
            if (widget == null) { throw new ArgumentNullException(nameof(widget)); }
            string name = ResolveFormat(format);
            ExportFormat? core = CoreFormat(name);
            return core.HasValue ? new UTF8Encoding(false).GetBytes(ConsoleExporter.Export(widget, core.Value, options))
                : ResolveProvider(name, provider).Export(widget, options);
        }

        /// <summary>Exports a rendered frame through the same core/add-on dispatch.</summary>
        public static byte[] Export(CellBuffer frame, string format, ExportOptions? options = null, IFrameExportProvider? provider = null) {
            if (frame == null) { throw new ArgumentNullException(nameof(frame)); }
            string name = ResolveFormat(format);
            ExportFormat? core = CoreFormat(name);
            return core.HasValue ? new UTF8Encoding(false).GetBytes(ConsoleExporter.Export(frame, core.Value, options))
                : ResolveProvider(name, provider).Export(frame, options);
        }

        /// <summary>Saves a widget through the same dispatch used by a fluent Export ending.</summary>
        public static void Save(IWidget widget, string path, string? format = null, ExportOptions? options = null, IFrameExportProvider? provider = null) =>
            File.WriteAllBytes(path ?? throw new ArgumentNullException(nameof(path)), Export(widget, ResolveFormat(format, path), options, provider));

        private static ExportFormat? CoreFormat(string name) {
            if (string.Equals(name, "Html", StringComparison.OrdinalIgnoreCase)) { return ExportFormat.Html; }
            if (string.Equals(name, "Svg", StringComparison.OrdinalIgnoreCase)) { return ExportFormat.Svg; }
            if (string.Equals(name, "Text", StringComparison.OrdinalIgnoreCase)) { return ExportFormat.Text; }
            return null;
        }

        private static IFrameExportProvider ResolveProvider(string format, IFrameExportProvider? supplied) {
            if (supplied != null) {
                if (!string.Equals(format, supplied.Format, StringComparison.OrdinalIgnoreCase)) { throw new ArgumentException("The provider exports " + supplied.Format + ", not " + format + ".", nameof(supplied)); }
                return supplied;
            }
            lock (Gate) { if (Providers.TryGetValue(format, out IFrameExportProvider? registered)) { return registered; } }
            string package = format == "Markdown" || format == "FencedText" ? "ConsoleMonster.Markdown" : format == "Png" ? "ConsoleMonster.Graphics" : string.Empty;
            throw new NotSupportedException(package.Length == 0 ? "Unknown frame format '" + format + "'. Register its add-on provider."
                : format + " export requires the " + package + " package. Register its frame provider or pass it to Export.");
        }
    }
}
