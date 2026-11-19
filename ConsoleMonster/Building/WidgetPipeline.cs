using System;
using System.IO;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;

namespace ConsoleMonster {
    /// <summary>
    /// Display and output endings for a fluent widget chain: <c>Show</c> paints to the terminal, <c>Save</c> writes a file
    /// or stream, <c>Render</c> returns the output, and <c>ToSvg</c>, <c>ToHtml</c> and <c>ToText</c> (on
    /// <see cref="ConsoleExporter"/>) return strings.
    /// </summary>
    public static class WidgetPipeline {
        /// <summary>Displays a widget, just as piping it to Show-Console does.</summary>
        /// <param name="widget">The configured widget.</param>
        /// <param name="profile">Optional terminal profile.</param>
        /// <param name="writer">Optional embedding writer.</param>
        public static void Show(this IWidget widget, TerminalProfile? profile = null, ITerminalWriter? writer = null) => Terminal.Write(widget, profile, writer);

        /// <summary>Writes the widget to a file in the format its extension implies (.html, .svg, .txt, .md, .png...).</summary>
        /// <param name="widget">The configured widget.</param>
        /// <param name="path">The output file.</param>
        /// <param name="options">Frame size, theme and typography.</param>
        /// <param name="provider">Optional typed add-on provider; a registered provider is used when omitted.</param>
        public static void Save(this IWidget widget, string path, ExportOptions? options = null, IFrameExportProvider? provider = null) =>
            FrameExporter.Save(widget, path, null, options, provider);

        /// <summary>Writes the widget to a file in an explicit format.</summary>
        /// <param name="widget">The configured widget.</param>
        /// <param name="path">The output file.</param>
        /// <param name="format">The format.</param>
        /// <param name="options">Frame size, theme and typography.</param>
        /// <param name="provider">Optional typed add-on provider.</param>
        public static void Save(this IWidget widget, string path, OutputFormat format, ExportOptions? options = null, IFrameExportProvider? provider = null) =>
            FrameExporter.Save(widget, path, format, options, provider);

        /// <summary>Writes the widget to a file with freshly configured output options.</summary>
        /// <param name="widget">The configured widget.</param>
        /// <param name="path">The output file; its extension picks the format.</param>
        /// <param name="configure">Frame size, theme and typography.</param>
        public static void Save(this IWidget widget, string path, Action<ExportOptions> configure) =>
            FrameExporter.Save(widget, path, null, OptionConfiguration.Create(configure));

        /// <summary>Writes the widget to a stream in a format.</summary>
        /// <param name="widget">The configured widget.</param>
        /// <param name="stream">The stream; it stays open.</param>
        /// <param name="format">The format.</param>
        /// <param name="options">Frame size, theme and typography.</param>
        /// <param name="provider">Optional typed add-on provider.</param>
        public static void Save(this IWidget widget, Stream stream, OutputFormat format, ExportOptions? options = null, IFrameExportProvider? provider = null) =>
            FrameExporter.Save(widget, stream, format, options, provider);

        /// <summary>Renders the widget in a format and returns the output with its media type.</summary>
        /// <param name="widget">The configured widget.</param>
        /// <param name="format">The format.</param>
        /// <param name="options">Frame size, theme and typography.</param>
        /// <param name="provider">Optional typed add-on provider.</param>
        /// <returns>The rendered output.</returns>
        public static RenderedOutput Render(this IWidget widget, OutputFormat format, ExportOptions? options = null, IFrameExportProvider? provider = null) =>
            FrameExporter.Render(widget, format, options, provider);
    }
}
