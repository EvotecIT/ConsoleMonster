using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Export;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Exports a widget, a rendered frame or a live-session snapshot to HTML, SVG, Markdown or text.</para>
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
    [OutputType(typeof(string), typeof(FileInfo))]
    public sealed class CmdletExportConsoleFrame : PSCmdlet {
        private readonly List<object> _items = new List<object>();

        /// <summary><para type="description">A widget, a rendered frame (CellBuffer), a live session, text, or objects to show as a table.</para></summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [AllowNull]
        public object? InputObject { get; set; }

        /// <summary><para type="description">The format: Html, Svg, Markdown (a fenced preformatted block) or Text (default: from the -Path extension, including .md/.markdown, else Html).</para></summary>
        [Parameter]
        [ValidateSet("Html", "Svg", "Text", "Markdown")]
        public string? Format { get; set; }

        /// <summary><para type="description">The file to write (UTF-8 without a byte order mark). Without it the document goes to the pipeline.</para></summary>
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

        /// <summary><para type="description">Auto (both renderings, following the viewer), Dark or Light.</para></summary>
        [Parameter]
        public ExportTheme Theme { get; set; } = ExportTheme.Auto;

        /// <summary><para type="description">The page title and, with -WindowChrome, the title bar text.</para></summary>
        [Parameter]
        public string? Title { get; set; }

        /// <summary><para type="description">Draws a window frame with a title bar around the screen.</para></summary>
        [Parameter]
        public SwitchParameter WindowChrome { get; set; }

        /// <summary><para type="description">The font size in pixels (default 14).</para></summary>
        [Parameter]
        [ValidateRange(4, 96)]
        public double FontSize { get; set; } = 14;

        /// <summary><para type="description">With -Path, also writes the file to the pipeline.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

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
            ExportFormat format = Format != null
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
                WindowChrome = WindowChrome.IsPresent,
                FontSize = FontSize
            };
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
