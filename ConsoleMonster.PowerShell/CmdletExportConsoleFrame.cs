using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Export;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Exports a widget, a rendered frame or a live-session snapshot to PNG, HTML, SVG, Markdown or text.</para>
    /// <para type="description">Pipe any New-Console* widget (or Show-ConsoleTable -AsWidget) and get a self-contained HTML page (inline CSS, selectable text, a dark and a light rendering that follow the viewer's prefers-color-scheme), an SVG image (box, block and braille characters drawn as shapes, selectable text, no external assets) or plain text. Cell geometry is kept exactly. OSC 8 links become links (web and mail links only; javascript: and other schemes are dropped). Markdown uses the bundled ConsoleMonster.Markdown add-on lazily: tables, records and trees retain their semantics; FencedText preserves the terminal layout. Other objects use the automatic view. Several widgets are stacked. Without -Path the document is written to the pipeline.</para>
    /// </summary>
    /// <example>
    ///   <para>A table as an HTML page</para>
    ///   <code>Get-Process | Select-Object -First 8 Name, Id | Show-ConsoleTable -AsWidget | Export-ConsoleFrame -Format Html</code>
    /// </example>
    /// <example>
    ///   <para>A dashboard as an SVG in a window frame, dark only</para>
    ///   <code>New-ConsoleText 'Nightly run' | Export-ConsoleFrame -Format Svg -Width 120 -Height 36 -Theme Dark -Title 'Nightly run' -WindowChrome</code>
    /// </example>
    [Cmdlet(VerbsData.Export, "ConsoleFrame")]
    [OutputType(typeof(string), typeof(byte[]), typeof(FileInfo))]
    public sealed class CmdletExportConsoleFrame : PSCmdlet {
        private readonly List<object> _items = new List<object>();

        /// <summary><para type="description">A widget, a rendered frame (CellBuffer), a live session, text, or objects to show as a table.</para></summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [AllowNull]
        public object? InputObject { get; set; }

        /// <summary><para type="description">The format: Png, Html, Svg, Markdown (semantic tables, records and trees), FencedText (a preformatted block) or Text (default: from the -Path extension, including .md/.markdown, else Html).</para></summary>
        [Parameter]
        [ValidateSet("Html", "Svg", "Text", "Markdown", "FencedText", "Png")]
        public string? Format { get; set; }

        /// <summary><para type="description">The file to write (binary PNG, or UTF-8 text without a byte order mark). Without it PNG bytes or the text document go to the pipeline.</para></summary>
        [Parameter(Position = 1)]
        public string? Path { get; set; }

        /// <summary><para type="description">Width in cells for widgets (default 0: terminal width, falling back to 100).</para></summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int Width { get; set; }

        /// <summary><para type="description">Height in cells for widgets (default: as tall as the widget measures).</para></summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int Height { get; set; }

        /// <summary><para type="description">Auto (both HTML renderings), Dark, Light or HighContrast. Raster Auto uses Dark.</para></summary>
        [Parameter]
        public ExportTheme Theme { get; set; } = ExportTheme.Auto;

        /// <summary><para type="description">The page title and, with -WindowChrome, the title bar text.</para></summary>
        [Parameter]
        public string? Title { get; set; }

        /// <summary><para type="description">Draws a window frame with a title bar around the screen.</para></summary>
        [Parameter]
        public SwitchParameter WindowChrome { get; set; }

        /// <summary><para type="description">MacOS (default), Windows, Linux or None. Specifying a style enables chrome unless -WindowChrome:$false is explicit. None always hides chrome.</para></summary>
        [Parameter]
        [PSDefaultValue(Value = WindowChromeStyle.MacOS, Help = "MacOS")]
        public WindowChromeStyle ChromeStyle { get; set; } = WindowChromeStyle.MacOS;

        /// <summary><para type="description">Natural-size font in pixels (default 14). Slide presets fit automatically, up to 72 pixels.</para></summary>
        [Parameter]
        [ValidateRange(4, 96)]
        public double FontSize { get; set; } = 14;

        /// <summary><para type="description">With -Path, also writes the file to the pipeline.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary><para type="description">Graphics canvas: Docs, Slide16x9, Slide4x3 or Social.</para></summary>
        [Parameter, ValidateSet("Docs", "Slide16x9", "Slide4x3", "Social")]
        public string Preset { get; set; } = "Docs";

        /// <summary><para type="description">Slide fitting: Content trims unused rows and columns with two trailing columns of padding; Terminal keeps the complete grid. Animations use one stable size throughout the clip.</para></summary>
        [Parameter, ValidateSet("Content", "Terminal")] public string Fit { get; set; } = "Content";
        /// <summary><para type="description">Slide background: Theme (default), Solid or Gradient.</para></summary>
        [Parameter, ValidateSet("Theme", "Solid", "Gradient")] public string Background { get; set; } = "Theme";
        /// <summary><para type="description">Solid background or gradient start as #RGB or #RRGGBB.</para></summary>
        [Parameter] public string? BackgroundColor { get; set; }
        /// <summary><para type="description">Gradient end as #RGB or #RRGGBB; omitted uses the theme background.</para></summary>
        [Parameter] public string? BackgroundEndColor { get; set; }

        /// <summary><para type="description">Installed raster font; omitted uses embedded JetBrains Mono.</para></summary>
        [Parameter] public string? FontFamily { get; set; }

        /// <summary><para type="description">Typed C# ExportOptions, including custom themes, glyphs, culture and document settings. Use this instead of individual frame-rendering settings; the object is not modified.</para></summary>
        [Parameter, ValidateNotNull] public ExportOptions? Options { get; set; }

        /// <summary><para type="description">Maximum raster font size in pixels; default 72.</para></summary>
        [Parameter, ValidateRange(0.01, 4096)] public double MaxFontSize { get; set; } = 72;
        /// <summary><para type="description">Additional raster font-family fallbacks, in preferred order.</para></summary>
        [Parameter] public string[]? FontFallbacks { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (InputObject != null) {
                _items.Add(InputObject);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (_items.Count == 0) {
                return;
            }
            if (Path != null && string.Equals(System.IO.Path.GetExtension(Path), ".mp4", System.StringComparison.OrdinalIgnoreCase)) {
                throw new System.ArgumentException("MP4 export isn't available yet; use GIF for slides (see https://consolemonster.dev/docs/talks-and-demos/).");
            }
            string? requested = Format ?? (Path == null ? null : System.IO.Path.GetExtension(Path).TrimStart('.'));
            bool graphics = string.Equals(requested, "Png", System.StringComparison.OrdinalIgnoreCase);
            string format = graphics ? "Svg" : PSOutputExport.FormatFor(Format, Path);
            if (format == "Asciicast") { format = "Text"; }
            ExportOptions options = PSOptions.Resolve(this, Options, () => new ExportOptions {
                Width = Width,
                Height = Height,
                Theme = Theme,
                Title = Title,
                WindowChrome = (MyInvocation.BoundParameters.ContainsKey(nameof(WindowChrome)) ? WindowChrome.IsPresent : MyInvocation.BoundParameters.ContainsKey(nameof(ChromeStyle))), ChromeStyle = ChromeStyle,
                FontSize = FontSize
            }, nameof(Width), nameof(Height), nameof(Theme), nameof(Title), nameof(WindowChrome), nameof(ChromeStyle), nameof(FontSize));
            if (graphics) {
                string? target = Path == null ? null : SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
                object result = GraphicsExportBridge.Frame(_items, target, Preset, FontFamily, Fit, Background, BackgroundColor, BackgroundEndColor, options, this, MaxFontSize, FontFallbacks);
                if (target == null || PassThru.IsPresent) { WriteObject(result, false); }
                return;
            }
            string document = PSOutputExport.Document(_items, format, options, this);
            if (Path == null) {
                WriteObject(document);
                return;
            }
            string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
            File.WriteAllText(path, document, new UTF8Encoding(false));
            if (PassThru.IsPresent) {
                WriteObject(new FileInfo(path));
            }
        }

    }
}
