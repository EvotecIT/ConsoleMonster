using System.IO;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Exports a terminal session or dashboard recording to a player, SVG or asciicast.</para>
    /// <para type="description">Without -Dashboard, reads a whole-session asciicast v2 from Start-ConsoleRecording or Invoke-ConsoleRecording. Html writes a self-contained player with chapters, speed, scrubbing and copy text; Svg is animated; Text and Markdown include scrollback; -FinalFrame writes a static SVG. With -Dashboard, replays Start-ConsoleDashboard widget updates into the same definition, preserving the existing dashboard HTML, SVG and asciicast v3 exports. Add #t=30000 to an HTML address to open at 30 seconds.</para>
    /// </summary>
    /// <example>
    ///   <para>An animated SVG and an HTML player of the same run</para>
    ///   <code>
    /// Export-ConsoleRecording -Dashboard $dashboard -Path .\run.jsonl -Format Svg -Destination .\run.svg
    /// Export-ConsoleRecording -Dashboard $dashboard -Path .\run.jsonl -Format Html -Destination .\run.html -Width 120 -Height 36</code>
    /// </example>
    [Cmdlet(VerbsData.Export, "ConsoleRecording", DefaultParameterSetName = "Session")]
    [OutputType(typeof(string), typeof(FileInfo))]
    public sealed class CmdletExportConsoleRecording : PSCmdlet {
        /// <summary><para type="description">The dashboard (or widget tree) the recording was made with.</para></summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Dashboard")]
        public IWidget Dashboard { get; set; } = null!;

        /// <summary><para type="description">Session .cast file, or dashboard .jsonl with -Dashboard.</para></summary>
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Dashboard")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Session")]
        public string Path { get; set; } = string.Empty;

        /// <summary><para type="description">Html, Svg, Asciicast, Text, Markdown, FencedText or PlayerData. The destination extension selects a default; .json means PlayerData. Text and Markdown apply to terminal sessions.</para></summary>
        [Parameter]
        [ValidateSet("Html", "Svg", "Asciicast", "PlayerData", "Text", "Markdown", "FencedText")]
        public string? Format { get; set; }

        /// <summary><para type="description">The file to write. Without it the document goes to the pipeline.</para></summary>
        [Parameter(Position = 2, ParameterSetName = "Dashboard")]
        [Parameter(Position = 1, ParameterSetName = "Session")]
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

        /// <summary><para type="description">MacOS (default), Windows, Linux or None. Specifying a style enables chrome unless -WindowChrome:$false is explicit. None always hides chrome.</para></summary>
        [Parameter]
        [PSDefaultValue(Value = WindowChromeStyle.MacOS)]
        public WindowChromeStyle ChromeStyle { get; set; } = WindowChromeStyle.MacOS;

        /// <summary><para type="description">With -Destination, also writes the file to the pipeline.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary><para type="description">With a session cast, exports the final visible frame as static SVG.</para></summary>
        [Parameter(ParameterSetName = "Session")]
        public SwitchParameter FinalFrame { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            string source = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
            if (FinalFrame.IsPresent && ((Format != null && !string.Equals(Format, "Svg", System.StringComparison.OrdinalIgnoreCase))
                || (Destination != null && !string.Equals(System.IO.Path.GetExtension(Destination), ".svg", System.StringComparison.OrdinalIgnoreCase)))) {
                throw new System.ArgumentException("FinalFrame writes SVG; use -Format Svg or an .svg destination.");
            }
            bool playerData = string.Equals(Format, "PlayerData", System.StringComparison.OrdinalIgnoreCase)
                || (Format == null && Destination != null && string.Equals(System.IO.Path.GetExtension(Destination), ".json", System.StringComparison.OrdinalIgnoreCase));
            ExportFormat format = playerData ? ExportFormat.Html : Format != null
                ? (ExportFormat)System.Enum.Parse(typeof(ExportFormat), Format, true)
                : Destination != null ? ConsoleExporter.FormatFor(Destination) : ExportFormat.Html;
            if (ParameterSetName == "Dashboard" && (format == ExportFormat.Text || format == ExportFormat.Markdown || format == ExportFormat.FencedText)) {
                format = ExportFormat.Html;
            }
            RecordingExportOptions options = new RecordingExportOptions {
                Width = Width,
                Height = Height,
                FramesPerSecond = FramesPerSecond,
                Speed = Speed,
                Theme = Theme,
                Title = Title,
                WindowChrome = (MyInvocation.BoundParameters.ContainsKey(nameof(WindowChrome)) ? WindowChrome.IsPresent : MyInvocation.BoundParameters.ContainsKey(nameof(ChromeStyle))), ChromeStyle = ChromeStyle
            };
            string document;
            using (StreamReader reader = new StreamReader(source, Encoding.UTF8)) {
                if (ParameterSetName == "Session") {
                    SessionRecording session = SessionRecording.Read(reader);
                    document = FinalFrame.IsPresent ? session.ExportFinalSvg(new ExportOptions { Theme = Theme, Title = Title, WindowChrome = (MyInvocation.BoundParameters.ContainsKey(nameof(WindowChrome)) ? WindowChrome.IsPresent : MyInvocation.BoundParameters.ContainsKey(nameof(ChromeStyle))), ChromeStyle = ChromeStyle })
                        : playerData ? session.ExportPlayerData(options) : session.Export(format, options);
                } else {
                    document = playerData ? Export.Recording.ExportPlayerData(Dashboard, reader, options) : Export.Recording.Export(Dashboard, reader, format, options);
                }
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
