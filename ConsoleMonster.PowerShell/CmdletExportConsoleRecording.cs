using System.IO;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Exports a dashboard recording to an HTML player, an animated SVG or asciicast.</para>
    /// <para type="description">Recordings (written by Start-ConsoleDashboard -RecordPath) hold updates, not pictures, so they are played into the same dashboard definition at a fixed size and frame rate. Html writes a self-contained page with a small player (play/pause, speed, scrubbing; add #t=30000 to the address to open at 30 seconds). Svg writes an animated image with CSS keyframes and no script, for READMEs. Asciicast writes asciicast v3 for asciinema and other players (output events only). Identical frames are merged; a long recording is sampled less often.</para>
    /// <example>
    ///   <para>An animated SVG and an HTML player of the same run</para>
    ///   <code>Export-ConsoleRecording -Dashboard $dashboard -Path .\run.jsonl -Format Svg -Destination .\run.svg
    /// Export-ConsoleRecording -Dashboard $dashboard -Path .\run.jsonl -Format Html -Destination .\run.html -Width 120 -Height 36</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsData.Export, "ConsoleRecording")]
    [OutputType(typeof(string), typeof(FileInfo))]
    public sealed class CmdletExportConsoleRecording : PSCmdlet {
        /// <summary><para type="description">The dashboard (or widget tree) the recording was made with.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public IWidget Dashboard { get; set; } = null!;

        /// <summary><para type="description">The recording (.jsonl).</para></summary>
        [Parameter(Mandatory = true, Position = 1)]
        public string Path { get; set; } = string.Empty;

        /// <summary><para type="description">Html, Svg, Asciicast or PlayerData (default: from the -Destination extension, .json for PlayerData, else Html). PlayerData is the JSON the native player script plays, for sites that include the script once (see ConsoleExporter.PlayerScript).</para></summary>
        [Parameter]
        [ValidateSet("Html", "Svg", "Asciicast", "PlayerData")]
        public string? Format { get; set; }

        /// <summary><para type="description">The file to write. Without it the document goes to the pipeline.</para></summary>
        [Parameter(Position = 2)]
        [Alias("OutFile")]
        public string? Destination { get; set; }

        /// <summary><para type="description">Screen width in cells (default 100).</para></summary>
        [Parameter]
        [ValidateRange(10, 1000)]
        public int Width { get; set; } = 100;

        /// <summary><para type="description">Screen height in cells (default 30).</para></summary>
        [Parameter]
        [ValidateRange(3, 1000)]
        public int Height { get; set; } = 30;

        /// <summary><para type="description">Frames per second sampled from the recording (default 10).</para></summary>
        [Parameter]
        [ValidateRange(0.1, 60)]
        public double FramesPerSecond { get; set; } = 10;

        /// <summary><para type="description">Playback speed baked into the export (default 1).</para></summary>
        [Parameter]
        [ValidateRange(0.01, 100)]
        public double Speed { get; set; } = 1;

        /// <summary><para type="description">Dark (default) or Light.</para></summary>
        [Parameter]
        public ExportTheme Theme { get; set; } = ExportTheme.Dark;

        /// <summary><para type="description">The title (page title, title bar, asciicast title).</para></summary>
        [Parameter]
        public string? Title { get; set; }

        /// <summary><para type="description">Draws a window frame with a title bar (Html and Svg).</para></summary>
        [Parameter]
        public SwitchParameter WindowChrome { get; set; }

        /// <summary><para type="description">With -Destination, also writes the file to the pipeline.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            string source = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
            bool playerData = string.Equals(Format, "PlayerData", System.StringComparison.OrdinalIgnoreCase)
                || (Format == null && Destination != null && string.Equals(System.IO.Path.GetExtension(Destination), ".json", System.StringComparison.OrdinalIgnoreCase));
            ExportFormat format = playerData ? ExportFormat.Html : Format != null
                ? (ExportFormat)System.Enum.Parse(typeof(ExportFormat), Format, true)
                : Destination != null ? ConsoleExporter.FormatFor(Destination) : ExportFormat.Html;
            if (format == ExportFormat.Text) {
                format = ExportFormat.Html;
            }
            RecordingExportOptions options = new RecordingExportOptions {
                Width = Width,
                Height = Height,
                FramesPerSecond = FramesPerSecond,
                Speed = Speed,
                Theme = Theme,
                Title = Title,
                WindowChrome = WindowChrome.IsPresent
            };
            string document;
            using (StreamReader reader = new StreamReader(source, Encoding.UTF8)) {
                document = playerData ? Recording.ExportPlayerData(Dashboard, reader, options) : Recording.Export(Dashboard, reader, format, options);
            }
            if (Destination == null) {
                WriteObject(document);
                return;
            }
            string target = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Destination);
            File.WriteAllText(target, document, new UTF8Encoding(false));
            if (PassThru.IsPresent) {
                WriteObject(new FileInfo(target));
            }
        }
    }
}
