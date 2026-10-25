using System;
using System.Globalization;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Export {
    /// <summary>Which colours an export uses.</summary>
    public enum ExportTheme {
        /// <summary>Both: the dark rendering by default and the light one when the viewer prefers light (<c>prefers-color-scheme</c>).</summary>
        Auto = 0,

        /// <summary>The dark theme on a dark background.</summary>
        Dark = 1,

        /// <summary>The light theme on a light background.</summary>
        Light = 2,

        /// <summary>The built-in high-contrast theme on black.</summary>
        HighContrast = 3
    }

    /// <summary>A file format for frame and recording exports.</summary>
    public enum ExportFormat {
        /// <summary>A self-contained HTML page (inline CSS; a recording gets a small inline player).</summary>
        Html = 0,

        /// <summary>A self-contained SVG image (a recording becomes a CSS-animated SVG).</summary>
        Svg = 1,

        /// <summary>Plain text (frames only).</summary>
        Text = 2,

        /// <summary>An asciicast file for terminal players: v2 for terminal sessions, v3 for dashboard recordings.</summary>
        Asciicast = 3


    }

    /// <summary>
    /// How a widget, a rendered frame or a live-session snapshot is exported to HTML, SVG or text: the size in cells, the
    /// theme, the font, and an optional window frame with a title bar.
    /// </summary>
    public sealed class ExportOptions {
        /// <summary>
        /// The font stack. The first fonts draw box drawing and block characters on the cell grid; braille needs a font that
        /// has the braille block (Cascadia Mono, DejaVu Sans Mono, Menlo do; Consolas does not and falls back to a system font).
        /// SVG exports draw box, block and braille characters as shapes, so they do not depend on the font.
        /// </summary>
        public const string DefaultFontFamily = "'Cascadia Mono', 'Cascadia Code', 'DejaVu Sans Mono', Menlo, Consolas, 'Liberation Mono', 'Courier New', monospace";

        /// <summary>Width in cells for widgets (frames and snapshots keep their own size). Zero resolves the current target width, falling back to 100 cells.</summary>
        public int Width { get; set; }

        /// <summary>Height in cells for widgets; 0 (default) uses the height the widget measures at <see cref="Width"/>.</summary>
        public int Height { get; set; }

        /// <summary>The colours (default <see cref="ExportTheme.Auto"/>).</summary>
        public ExportTheme Theme { get; set; } = ExportTheme.Auto;

        /// <summary>A custom theme for the dark rendering (default <see cref="Styling.Theme.Dark"/>).</summary>
        public ITheme? DarkTheme { get; set; }

        /// <summary>A custom theme for the light rendering (default <see cref="Styling.Theme.Light"/>).</summary>
        public ITheme? LightTheme { get; set; }

        /// <summary>The page title and, with <see cref="WindowChrome"/>, the title bar text.</summary>
        public string? Title { get; set; }

        /// <summary>Enables the chosen <see cref="ChromeStyle"/> (default MacOS). False always hides chrome; style None also hides it.</summary>
        public bool WindowChrome { get; set; }

        /// <summary>The title bar style, used only when <see cref="WindowChrome"/> is true. Default MacOS; None hides chrome.</summary>
        public WindowChromeStyle ChromeStyle { get; set; } = WindowChromeStyle.MacOS;

        internal WindowChromeStyle EffectiveChrome => WindowChrome ? ChromeStyle : WindowChromeStyle.None;

        /// <summary>The font stack (see <see cref="DefaultFontFamily"/>).</summary>
        public string FontFamily { get; set; } = DefaultFontFamily;
        /// <summary>SVG: writes one text element per grapheme instead of compact x-coordinate lists, for consumers with a limited SVG text model.</summary>
        public bool IndividualTextElements { get; set; }

        /// <summary>The font size in pixels (default 14).</summary>
        public double FontSize { get; set; } = 14;

        /// <summary>The cell height as a multiple of the font size (default 1.2).</summary>
        public double LineHeight { get; set; } = 1.2;

        /// <summary>The glyph tier widgets render with (default <see cref="GlyphTier.Full"/>).</summary>
        public GlyphTier Glyphs { get; set; } = GlyphTier.Full;

        /// <summary>The colour depth widgets render with (default <see cref="ColorDepth.TrueColor"/>).</summary>
        public ColorDepth ColorDepth { get; set; } = ColorDepth.TrueColor;

        /// <summary>The culture for numbers and dates in widgets (default: the current culture).</summary>
        public CultureInfo? Culture { get; set; }

        /// <summary>Space around the screen in pixels (default 12).</summary>
        public int Padding { get; set; } = 12;

        /// <summary>The session clock widgets show (header clocks, spinners) when a widget is exported; default zero.</summary>
        public TimeSpan Elapsed { get; set; }

        /// <summary>HTML only: writes a fragment (a styled element) instead of a whole page, for embedding.</summary>
        public bool Fragment { get; set; }

        internal double CellHeight => Math.Round(FontSize * LineHeight, 2);

        internal double CellWidth => Math.Round(FontSize * 0.6, 2);

        internal ExportOptions Clone() => (ExportOptions)MemberwiseClone();

        internal void Validate() {
            if (ChromeStyle < WindowChromeStyle.None || ChromeStyle > WindowChromeStyle.Linux) {
                throw new ArgumentOutOfRangeException(nameof(ChromeStyle));
            }
            if (Width < 1 || Width > 1000) {
                throw new ArgumentOutOfRangeException(nameof(Width), "Width must be between 1 and 1000 cells.");
            }
            if (Height < 0 || Height > 1000) {
                throw new ArgumentOutOfRangeException(nameof(Height), "Height must be between 0 and 1000 cells.");
            }
            if (double.IsNaN(FontSize) || FontSize < 4 || FontSize > 96) {
                throw new ArgumentOutOfRangeException(nameof(FontSize), "FontSize must be between 4 and 96 pixels.");
            }
            if (double.IsNaN(LineHeight) || LineHeight < 0.8 || LineHeight > 3) {
                throw new ArgumentOutOfRangeException(nameof(LineHeight), "LineHeight must be between 0.8 and 3.");
            }
            if (!ExportText.IsSafeFontFamily(FontFamily)) {
                throw new ArgumentException("FontFamily may contain font names, quotes, commas, spaces, dots, dashes and underscores only.", nameof(FontFamily));
            }
            if (Padding < 0 || Padding > 200) {
                throw new ArgumentOutOfRangeException(nameof(Padding), "Padding must be between 0 and 200 pixels.");
            }
        }
    }
}
