using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Graphics {
    /// <summary>Connects the existing Graphics PNG encoder to frame and fluent export dispatch.</summary>
    public sealed class PngFrameExportProvider : IFrameExportProvider {
        /// <summary>Creates a PNG provider with optional typed graphics settings.</summary>
        /// <param name="options">Per-provider graphics settings, or the default documentation preset.</param>
        public PngFrameExportProvider(GraphicsExportOptions? options = null) { Options = options; }
        /// <summary>The typed settings; Frame is overridden only when explicit frame options are supplied.</summary>
        public GraphicsExportOptions? Options { get; }
        /// <inheritdoc />
        public string Format => "Png";
        /// <inheritdoc />
        public byte[] Export(IWidget widget, ExportOptions? options = null) => widget.ToPng(Settings(options));
        /// <inheritdoc />
        public byte[] Export(CellBuffer frame, ExportOptions? options = null) => frame.ToPng(Settings(options));
        /// <summary>Registers the default PNG provider for the current application.</summary>
        public static void Register() => FrameExporter.Register(new PngFrameExportProvider());

        private GraphicsExportOptions Settings(ExportOptions? frame) {
            if (frame == null) { return Options ?? new GraphicsExportOptions(); }
            GraphicsExportOptions source = Options ?? new GraphicsExportOptions();
            var settings = new GraphicsExportOptions {
                Frame = frame, Preset = source.Preset, Fit = source.Fit, Background = source.Background,
                BackgroundColor = source.BackgroundColor, BackgroundEndColor = source.BackgroundEndColor,
                MaxFontSize = source.MaxFontSize, FontFamily = source.FontFamily, Playback = source.Playback,
                IdleLimit = source.IdleLimit, LoopCount = source.LoopCount
            };
            foreach (string fallback in source.FontFallbacks) { settings.FontFallbacks.Add(fallback); }
            return settings;
        }
    }
}
