using System;
using System.Collections.Generic;
using ConsoleMonster.Export;

namespace ConsoleMonster.Graphics {
    /// <summary>Raster and video formats supplied by the graphics add-on.</summary>
    public enum GraphicsFormat {
        /// <summary>One lossless frame.</summary>
        Png,
        /// <summary>Palette-quantised animation.</summary>
        Gif,
        /// <summary>Lossless animated PNG.</summary>
        Apng,
        /// <summary>H.264 MP4, using ffmpeg on PATH.</summary>
        Mp4
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
    /// <summary>Graphics layout and animation settings. Existing frame and recording options own themes and chrome.</summary>
    public sealed class GraphicsExportOptions {
        /// <summary>Canvas size. Slide presets fit the entire grid without cropping.</summary>
        public GraphicsPreset Preset { get; set; } = GraphicsPreset.Docs;
        /// <summary>Widget dimensions, theme, title, font size and chrome. Presets select a larger font when this is left at 14.</summary>
        public ExportOptions Frame { get; set; } = new ExportOptions { Theme = ExportTheme.Dark };
        /// <summary>Recording frame rate, speed and final hold. The graphics layer also caps idle gaps.</summary>
        public RecordingExportOptions Playback { get; set; } = new RecordingExportOptions();
        /// <summary>Installed font family; null uses embedded JetBrains Mono.</summary>
        public string? FontFamily { get; set; }
        /// <summary>Additional installed fallback families, tried before platform emoji/CJK defaults.</summary>
        public IList<string> FontFallbacks { get; } = new List<string>();
        /// <summary>Maximum time one unchanged screen stays, excluding the final hold. Zero preserves source timing.</summary>
        public TimeSpan IdleLimit { get; set; } = TimeSpan.FromSeconds(2);
        /// <summary>Total plays: zero loops indefinitely, one plays once.</summary>
        public int LoopCount { get; set; }
        internal void Validate() {
            if (Preset < GraphicsPreset.Docs || Preset > GraphicsPreset.Social) { throw new ArgumentOutOfRangeException(nameof(Preset)); }
            if (IdleLimit < TimeSpan.Zero || LoopCount < 0 || LoopCount > 65536) { throw new ArgumentOutOfRangeException(nameof(LoopCount)); }
            if (Frame == null || Playback == null) { throw new ArgumentException("Frame and Playback settings are required."); }
        }
    }
}
