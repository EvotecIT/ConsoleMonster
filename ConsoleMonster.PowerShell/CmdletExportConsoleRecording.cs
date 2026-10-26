using System.IO;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.Sessions;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Exports a terminal session or dashboard recording to slide images, animations, video, a player or asciicast.</para>
    /// <para type="description">PNG selects the final frame, -AtSeconds a source time, -Chapter a completed chapter, or -PerChapter numbered slides. GIF and APNG are managed animations without an external executable. Without -Dashboard, reads a whole-session asciicast v2 from Invoke-ConsoleRecording -Interactive or Invoke-ConsoleRecording. Html writes a self-contained player with chapters, speed, scrubbing and copy text; Svg is animated; Text and Markdown include scrollback; -FinalFrame writes a static SVG. With -Dashboard, replays Invoke-ConsoleDashboard widget updates into the same definition, preserving the dashboard HTML, SVG and asciicast v3 exports; Markdown writes timestamped fenced frames through the bundled add-on. Add #t=30000 to an HTML address to open at 30 seconds.</para>
    /// </summary>
    /// <example>
    ///   <para>Record a script, then make a paced 1920x1080 GIF and a closing PNG for a slide</para>
    ///   <code>
    /// Invoke-ConsoleRecording { Get-Service | Show-ConsoleTable } -Destination .\services.cast
    /// Export-ConsoleRecording .\services.cast .\services.gif -Pace Slow -Preset Slide16x9
    /// Export-ConsoleRecording .\services.cast .\services.png -Preset Slide16x9</code>
    /// </example>
    /// <example>
    ///   <para>An animated SVG and an HTML player of the same run</para>
    ///   <code>
    /// Export-ConsoleRecording -Dashboard $dashboard -Path .\run.jsonl -Format Svg -Destination .\run.svg
    /// Export-ConsoleRecording -Dashboard $dashboard -Path .\run.jsonl -Format Html -Destination .\run.html -Width 120 -Height 36</code>
    /// </example>
    [Cmdlet(VerbsData.Export, "ConsoleRecording", DefaultParameterSetName = "Session")]
    [OutputType(typeof(string), typeof(FileInfo))]
    public sealed partial class CmdletExportConsoleRecording : PSCmdlet {
        private readonly System.Threading.CancellationTokenSource _exportStop = new System.Threading.CancellationTokenSource();
        /// <summary><para type="description">The dashboard (or widget tree) the recording was made with.</para></summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Dashboard")]
        public IWidget Dashboard { get; set; } = null!;

        /// <summary><para type="description">Session .cast file, or dashboard .jsonl with -Dashboard.</para></summary>
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "Dashboard")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Session")]
        public string Path { get; set; } = string.Empty;

        /// <summary><para type="description">Png, Gif, Apng, Html, Svg, Asciicast, Text, Markdown, FencedText or PlayerData. The destination extension selects a default; .json means PlayerData. Text applies to terminal sessions; Markdown also exports dashboard recordings as timestamped fenced frames.</para></summary>
        [Parameter]
        [ValidateSet("Html", "Svg", "Asciicast", "PlayerData", "Text", "Markdown", "FencedText", "Png", "Gif", "Apng")]
        public string? Format { get; set; }

        /// <summary><para type="description">The file to write, or a directory with -PerChapter. Graphics require a destination; text formats can go to the pipeline.</para></summary>
        [Parameter(Position = 2, ParameterSetName = "Dashboard")]
        [Parameter(Position = 1, ParameterSetName = "Session")]

        public string? Destination { get; set; }

        /// <summary><para type="description">Dashboard playback width in cells (default 0: terminal width, falling back to 100). Session casts retain their recorded dimensions.</para></summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int Width { get; set; }

        /// <summary><para type="description">Dashboard playback height in cells (default 30). Session casts retain their recorded dimensions.</para></summary>
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

        /// <summary><para type="description">Dark (default), Light or HighContrast.</para></summary>
        [Parameter]
        public ExportTheme Theme { get; set; } = ExportTheme.Dark;

        /// <summary><para type="description">The title (page title, title bar, asciicast title).</para></summary>
        [Parameter]
        public string? Title { get; set; }

        /// <summary><para type="description">Draws a window frame with a title bar in HTML, SVG and graphics exports.</para></summary>
        [Parameter]
        public SwitchParameter WindowChrome { get; set; }

        /// <summary><para type="description">MacOS (default), Windows, Linux or None. Specifying a style enables chrome unless -WindowChrome:$false is explicit. None always hides chrome.</para></summary>
        [Parameter]
        [PSDefaultValue(Value = WindowChromeStyle.MacOS, Help = "MacOS")]
        public WindowChromeStyle ChromeStyle { get; set; } = WindowChromeStyle.MacOS;

        /// <summary><para type="description">With -Destination, also writes the file to the pipeline.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary><para type="description">With a session cast, exports the final visible frame as static SVG or PNG.</para></summary>
        [Parameter(ParameterSetName = "Session")]
        public SwitchParameter FinalFrame { get; set; }

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
        /// <summary><para type="description">Source recording time for PNG, before playback speed or idle compression.</para></summary>
        [Parameter(ParameterSetName = "Session")] public double AtSeconds { get; set; }
        private System.TimeSpan At => System.TimeSpan.FromSeconds(AtSeconds);
        /// <summary><para type="description">Exports the completed view of this chapter as PNG.</para></summary>
        [Parameter(ParameterSetName = "Session")] public string? Chapter { get; set; }
        /// <summary><para type="description">Writes numbered PNG slides to the destination directory, one per chapter.</para></summary>
        [Parameter(ParameterSetName = "Session")] public SwitchParameter PerChapter { get; set; }
        /// <summary><para type="description">Maximum source delay on an unchanged screen; zero disables capping. Reading and explicit holds are never capped.</para></summary>
        [Parameter, ValidateRange(0, 86400)] public double IdleLimitSeconds { get; set; } = 2;
        /// <summary><para type="description">Total animation plays: zero repeats forever, one plays once.</para></summary>
        [Parameter, ValidateRange(0, 65536)] public int LoopCount { get; set; }

        /// <summary><para type="description">Typed C# RecordingExportOptions, including sampling limits, markers, custom themes and playback settings. Use this instead of individual playback settings; the object is not modified.</para></summary>
        [Parameter, ValidateNotNull] public RecordingExportOptions? Options { get; set; }

        /// <summary><para type="description">Maximum raster font size in pixels; default 72.</para></summary>
        [Parameter, ValidateRange(0.01, 4096)] public double MaxFontSize { get; set; } = 72;
        /// <summary><para type="description">Additional raster font-family fallbacks, in preferred order.</para></summary>
        [Parameter] public string[]? FontFallbacks { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            try { ExportRecording(); } finally { _exportStop.Dispose(); }
        }
        /// <inheritdoc />
        protected override void StopProcessing() { try { _exportStop.Cancel(); } catch (System.ObjectDisposedException) { } }
        private void ExportRecording() {
            if (Destination != null && string.Equals(System.IO.Path.GetExtension(Destination), ".mp4", System.StringComparison.OrdinalIgnoreCase)) {
                throw new System.ArgumentException("MP4 export isn't available yet; use GIF for slides (see https://consolemonster.dev/docs/talks-and-demos/).");
            }
            RecordingExportOptions options = PSOptions.Resolve(this, Options, PlaybackOptions,
                nameof(Width), nameof(Height), nameof(FramesPerSecond), nameof(Speed), nameof(Theme), nameof(Title), nameof(WindowChrome), nameof(ChromeStyle),
                nameof(IdleLimitSeconds), nameof(Pace), nameof(ChapterHoldSeconds), nameof(HoldLastFrameSeconds), nameof(TrimStartSeconds), nameof(TrimEndSeconds), nameof(TrimStartChapter), nameof(TrimEndChapter), nameof(ChapterSpeeds));
            string source = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
            bool graphics = GraphicsExportBridge.IsGraphics(Format, Destination);
            if (graphics) {
                string selected = Format ?? System.IO.Path.GetExtension(Destination!).TrimStart('.');
                bool png = string.Equals(selected, "Png", System.StringComparison.OrdinalIgnoreCase);
                int selectors = (MyInvocation.BoundParameters.ContainsKey(nameof(AtSeconds)) ? 1 : 0) + (Chapter == null ? 0 : 1) + (PerChapter.IsPresent ? 1 : 0) + (FinalFrame.IsPresent ? 1 : 0);
                if (selectors > 1 || (!png && selectors > 0)) { throw new System.ArgumentException("Choose one PNG selector: -AtSeconds, -Chapter, -PerChapter or -FinalFrame."); }
                if (Destination == null) { throw new System.ArgumentException("Graphics recording exports need -Destination."); }
                string graphicsTarget = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Destination);
                var paths = GraphicsExportBridge.Recording(source, graphicsTarget, selected, Preset, FontFamily, Fit, Background, BackgroundColor, BackgroundEndColor,
                    options.ToFrameOptions(), options,
                    LoopCount, ParameterSetName == "Dashboard" ? Dashboard : null,
                    MyInvocation.BoundParameters.ContainsKey(nameof(AtSeconds)) ? (System.TimeSpan?)At : null, Chapter, PerChapter.IsPresent, _exportStop.Token, MaxFontSize, FontFallbacks);
                if (PassThru.IsPresent) { foreach (string path in paths) { WriteObject(new FileInfo(path)); } }
                return;
            }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(AtSeconds)) || Chapter != null || PerChapter.IsPresent) { throw new System.ArgumentException("-AtSeconds, -Chapter and -PerChapter require -Format Png."); }
            if (FinalFrame.IsPresent && ((Format != null && !string.Equals(Format, "Svg", System.StringComparison.OrdinalIgnoreCase))
                || (Destination != null && !string.Equals(System.IO.Path.GetExtension(Destination), ".svg", System.StringComparison.OrdinalIgnoreCase)))) {
                throw new System.ArgumentException("FinalFrame writes SVG; use -Format Svg or an .svg destination.");
            }
            bool playerData = string.Equals(Format, "PlayerData", System.StringComparison.OrdinalIgnoreCase)
                || (Format == null && Destination != null && string.Equals(System.IO.Path.GetExtension(Destination), ".json", System.StringComparison.OrdinalIgnoreCase));
            string selectedFormat = PSOutputExport.FormatFor(playerData ? "Html" : Format, Destination);
            bool markdown = PSOutputExport.IsMarkdown(selectedFormat);
            ExportFormat format = markdown ? ExportFormat.Html : PSOutputExport.CoreFormat(selectedFormat);
            if (ParameterSetName == "Dashboard" && format == ExportFormat.Text) { format = ExportFormat.Html; }

            string document;
            using (StreamReader reader = new StreamReader(source, Encoding.UTF8)) {
                if (ParameterSetName == "Session") {
                    SessionRecording session = SessionRecording.Read(reader);
                    document = FinalFrame.IsPresent ? session.ExportFinalSvg(options.ToFrameOptions())
                        : playerData ? session.ExportPlayerData(options) : markdown ? MarkdownExportBridge.Recording(session) : session.Export(format, options);
                } else {
                    document = playerData ? Export.Recording.ExportPlayerData(Dashboard, reader, options)
                        : markdown ? MarkdownExportBridge.Dashboard(Dashboard, reader, options) : Export.Recording.Export(Dashboard, reader, format, options);
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
