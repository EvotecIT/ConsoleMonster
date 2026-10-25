using System;
using System.Collections.Generic;
using ConsoleMonster.Export;

namespace ConsoleMonster.Graphics {
    /// <summary>Raster formats supplied by the graphics add-on.</summary>
    public enum GraphicsFormat {
        /// <summary>One lossless frame.</summary>
        Png,
        /// <summary>Palette-quantised animation.</summary>
        Gif,
        /// <summary>Lossless animated PNG.</summary>
        Apng
    }
    /// <summary>Output canvas presets.</summary>
    public enum GraphicsPreset {
        /// <summary>Natural grid size, suitable for documentation.</summary>
        Docs,
        /// <summary>1920 by 1080 pixels.</summary>
        Slide16x9,
        /// <summary>1440 by 1080 pixels.</summary>
        Slide4x3,
        /// <summary>1200 by 630 pixels.</summary>
        Social
    }
    /// <summary>Grid area retained when fitting a slide window.</summary>
    public enum GraphicsFit {
        /// <summary>Use occupied rows and columns with two trailing columns of padding; animations reserve the maximum extent throughout the clip.</summary>
        Content,
        /// <summary>Keep the full terminal grid, including trailing empty rows and columns.</summary>
        Terminal
    }
    /// <summary>Background behind a slide's terminal window.</summary>
    public enum GraphicsBackground {
        /// <summary>The terminal theme's background colour.</summary>
        Theme,
        /// <summary>One colour supplied by BackgroundColor.</summary>
        Solid,
        /// <summary>A soft diagonal gradient, with optional start and end colours.</summary>
        Gradient
    }
    /// <summary>Graphics layout and animation settings. Existing frame and recording options own themes and chrome.</summary>
    public sealed class GraphicsExportOptions {
        /// <summary>Canvas size. Slide presets fit the entire grid without cropping.</summary>
        public GraphicsPreset Preset { get; set; } = GraphicsPreset.Docs;
        /// <summary>Slide content fitting. Content is the default; Docs retains its natural-size behaviour.</summary>
        public GraphicsFit Fit { get; set; } = GraphicsFit.Content;
        /// <summary>Slide background. Docs has no surrounding slide canvas.</summary>
        public GraphicsBackground Background { get; set; } = GraphicsBackground.Theme;
        /// <summary>Solid colour, or gradient start, as #RGB or #RRGGBB.</summary>
        public string? BackgroundColor { get; set; }
        /// <summary>Gradient end as #RGB or #RRGGBB; omitted uses the theme background.</summary>
        public string? BackgroundEndColor { get; set; }
        /// <summary>Maximum slide font size in pixels. The window otherwise fills the safe area.</summary>
        public double MaxFontSize { get; set; } = 72;
        /// <summary>Widget dimensions, theme, title, natural-size typography and window chrome.</summary>
        public ExportOptions Frame { get; set; } = new ExportOptions { Theme = ExportTheme.Dark };
        /// <summary>Recording frame rate, speed and final hold. The graphics layer also caps idle gaps.</summary>
        public RecordingExportOptions Playback { get; set; } = new RecordingExportOptions();
        /// <summary>Installed font family; null uses embedded JetBrains Mono.</summary>
        public string? FontFamily { get; set; }
        /// <summary>Additional installed fallback families, tried before platform emoji/CJK defaults.</summary>
        public IList<string> FontFallbacks { get; } = new List<string>();
        /// <summary>Maximum time one unchanged screen stays, excluding the final hold. Zero preserves source timing.</summary>
        public TimeSpan IdleLimit { get => _idleLimit ?? Playback.IdleLimit; set => _idleLimit = value; }
        private TimeSpan? _idleLimit;
        /// <summary>Total plays: zero loops indefinitely, one plays once.</summary>
        public int LoopCount { get; set; }
        internal void Validate() {
            if (Preset < GraphicsPreset.Docs || Preset > GraphicsPreset.Social) { throw new ArgumentOutOfRangeException(nameof(Preset)); }
            if (Fit < GraphicsFit.Content || Fit > GraphicsFit.Terminal) { throw new ArgumentOutOfRangeException(nameof(Fit)); }
            if (Background < GraphicsBackground.Theme || Background > GraphicsBackground.Gradient) { throw new ArgumentOutOfRangeException(nameof(Background)); }
            if (double.IsNaN(MaxFontSize) || MaxFontSize < 4 || MaxFontSize > 96) { throw new ArgumentOutOfRangeException(nameof(MaxFontSize)); }
            ValidateColor(BackgroundColor); ValidateColor(BackgroundEndColor);
            if (Background == GraphicsBackground.Solid && BackgroundColor == null) { throw new ArgumentException("A solid slide background needs BackgroundColor."); }
            if (IdleLimit < TimeSpan.Zero || LoopCount < 0 || LoopCount > 65536) { throw new ArgumentOutOfRangeException(nameof(LoopCount)); }
            if (Frame == null || Playback == null) { throw new ArgumentException("Frame and Playback settings are required."); }
        }
        private static void ValidateColor(string? value) {
            if (value == null) { return; }
            if ((value.Length != 4 && value.Length != 7) || value[0] != '#') { throw new ArgumentException("Slide colours use #RGB or #RRGGBB."); }
            for (int i = 1; i < value.Length; i++) { if (!Uri.IsHexDigit(value[i])) { throw new ArgumentException("Slide colours use #RGB or #RRGGBB."); } }
        }
    }
}
