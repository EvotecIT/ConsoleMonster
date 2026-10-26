using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;

namespace ConsoleMonster {
    /// <summary>Display and export endings for a fluent widget chain.</summary>
    public static class WidgetPipeline {
        /// <summary>Displays a widget, just as piping it to Show-Console does.</summary>
        /// <param name="widget">The configured widget.</param>
        /// <param name="profile">Optional terminal profile.</param>
        /// <param name="writer">Optional embedding writer.</param>
        public static void Show(this IWidget widget, TerminalProfile? profile = null, ITerminalWriter? writer = null) => Terminal.Write(widget, profile, writer);

        /// <summary>Ends a widget chain by exporting to a core or add-on format.</summary>
        /// <param name="widget">The configured widget.</param>
        /// <param name="path">The output file.</param>
        /// <param name="format">Optional format; otherwise inferred from path.</param>
        /// <param name="options">Frame configuration.</param>
        /// <param name="provider">Optional typed add-on provider; a registered provider is used when omitted.</param>
        public static void Export(this IWidget widget, string path, string? format = null, ExportOptions? options = null, IFrameExportProvider? provider = null) =>
            FrameExporter.Save(widget, path, format, options, provider);

        /// <summary>Exports with freshly configured frame options through the same core or add-on dispatcher.</summary>
        /// <param name="widget">The configured widget.</param><param name="path">The destination.</param>
        /// <param name="configure">Frame settings.</param><param name="format">Optional format.</param><param name="provider">Optional add-on provider.</param>
        public static void Export(this IWidget widget, string path, Action<ExportOptions> configure, string? format = null, IFrameExportProvider? provider = null) =>
            FrameExporter.Save(widget, path, format, OptionConfiguration.Create(configure), provider);
    }
}
