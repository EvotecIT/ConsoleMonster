using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Export;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Exports a widget, a rendered frame or a live-session snapshot to PNG, HTML, SVG, Markdown or text.</para>
    /// <para type="description">Pipe any New-Console* widget (or Format-ConsoleTable -AsWidget) and get a self-contained HTML page (inline CSS, selectable text, a dark and a light rendering that follow the viewer's prefers-color-scheme), an SVG image (box, block and braille characters drawn as shapes, selectable text, no external assets) or plain text. Cell geometry is kept exactly. OSC 8 links become links (web and mail links only; javascript: and other schemes are dropped). Other objects are shown as a table. Several widgets are stacked. Without -Path the document is written to the pipeline.</para>
    /// </summary>
    /// <example>
    ///   <para>A table as an HTML page</para>
    ///   <code>Get-Service | Select-Object -First 8 Name, Status, StartType | Format-ConsoleTable -AsWidget | Export-ConsoleFrame -Path .\services.html</code>
    /// </example>
    /// <example>
    ///   <para>A dashboard as an SVG in a window frame, dark only</para>
    ///   <code>$dashboard | Export-ConsoleFrame -Format Svg -Path .\run.svg -Width 120 -Height 36 -Theme Dark -Title 'Nightly run' -WindowChrome</code>
    /// </example>
    [Cmdlet(VerbsData.Export, "ConsoleFrame")]
    [OutputType(typeof(string), typeof(byte[]), typeof(FileInfo))]
    public sealed class CmdletExportConsoleFrame : PSCmdlet {
        private readonly List<object> _items = new List<object>();

        /// <summary><para type="description">A widget, a rendered frame (CellBuffer), a live session, text, or objects to show as a table.</para></summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [AllowNull]
        public object? InputObject { get; set; }

        /// <summary><para type="description">The format: Png, Html, Svg, Markdown (a fenced preformatted block) or Text (default: from the -Path extension, including .md/.markdown, else Html).</para></summary>
        [Parameter]
        [ValidateSet("Html", "Svg", "Text", "Markdown", "Png")]
        public string? Format { get; set; }

        /// <summary><para type="description">The file to write (binary PNG, or UTF-8 text without a byte order mark). Without it PNG bytes or the text document go to the pipeline.</para></summary>
        [Parameter(Position = 1)]
        public string? Path { get; set; }

        /// <summary><para type="description">Width in cells for widgets (default 100).</para></summary>
        [Parameter]
        [ValidateRange(1, 1000)]
        public int Width { get; set; } = 100;

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
        [PSDefaultValue(Value = WindowChromeStyle.MacOS)]
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
            bool graphics = GraphicsExportBridge.IsGraphics(Format, Path);
            ExportFormat format = graphics ? ExportFormat.Svg : Format != null
                ? (ExportFormat)System.Enum.Parse(typeof(ExportFormat), Format, true)
                : Path != null ? ConsoleExporter.FormatFor(Path) : ExportFormat.Html;
            if (format == ExportFormat.Asciicast) {
                format = ExportFormat.Text;
            }
            ExportOptions options = new ExportOptions {
                Width = Width,
                Height = Height,
                Theme = Theme,
                Title = Title,
                WindowChrome = (MyInvocation.BoundParameters.ContainsKey(nameof(WindowChrome)) ? WindowChrome.IsPresent : MyInvocation.BoundParameters.ContainsKey(nameof(ChromeStyle))), ChromeStyle = ChromeStyle,
                FontSize = FontSize
            };
            if (graphics) {
                string? target = Path == null ? null : SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
                object result = GraphicsExportBridge.Frame(_items, target, Preset, FontFamily, Fit, Background, BackgroundColor, BackgroundEndColor, options, this);
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
