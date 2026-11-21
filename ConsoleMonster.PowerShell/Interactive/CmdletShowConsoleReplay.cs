using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Replay;

namespace ConsoleMonster.PowerShell {
    /// <summary>Rewinds a recorded run: an in-terminal player with a timeline, markers, failures, warnings and log search.</summary>
    /// <remarks>
    /// <para>Plays a dashboard recording created with RecordingPath or AutoRecord on Invoke-ConsoleDashboard. Without RecordingPath, it uses the newest automatic recording.</para>
    /// <para>### Replay player</para>
    /// <para>The player displays the reconstructed dashboard and a timeline with the recording's markers. Space plays or pauses. Left and Right step through frames, PgUp and PgDn scrub, Home and End jump to the ends, and + or - changes speed. Use n or p for the next or previous marker, f or Shift+F for failures, w or Shift+W for warnings, and l or Shift+L for log entries. Press / to search log text and Enter for the next match. Press q to close the player. Inline places it below the current cursor.</para>
    /// <para>### Unattended playback and marker output</para>
    /// <para>PlaybackMode Automatic replays without reading keys. When interactive playback is requested in a session without an interactive console, the command prints the markers as lines.</para>
    /// </remarks>
    /// <example>
    /// <para>Play an existing matching dashboard recording</para>
    /// <code># Requires build.jsonl recorded with this dashboard definition.
    /// $dashboardOptions = @{
    ///     Title = 'Build'
    ///     Content = {
    ///         $statusListOptions = @{
    ///             WidgetId = 'steps'
    ///             Content = { 'Compile' }
    ///         }
    ///         New-ConsoleStatusList @statusListOptions
    ///     }
    /// }
    /// $dashboard = New-ConsoleDashboard @dashboardOptions
    /// $replayOptions = @{
    ///     Dashboard = $dashboard
    ///     RecordingPath = './build.jsonl'
    /// }
    /// Show-ConsoleReplay @replayOptions</code>
    /// </example>
    /// <example>
    /// <para>Open an existing recording inline and paused. Press Right to step one frame, Space to play or pause, and q to close the player.</para>
    /// <code># Requires build.jsonl recorded with this dashboard definition.
    /// $dashboardOptions = @{
    ///     Title = 'Build'
    ///     Content = {
    ///         $statusListOptions = @{
    ///             WidgetId = 'steps'
    ///             Content = { 'Compile' }
    ///         }
    ///         New-ConsoleStatusList @statusListOptions
    ///     }
    /// }
    /// $dashboard = New-ConsoleDashboard @dashboardOptions
    /// $replayOptions = @{
    ///     Inline = $true
    ///     AutoPlay = $false
    ///     Dashboard = $dashboard
    ///     RecordingPath = './build.jsonl'
    /// }
    /// Show-ConsoleReplay @replayOptions</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleReplay", HelpUri = "https://consolemonster.dev/powershell/commands/show-consolereplay/")]
    public sealed partial class CmdletShowConsoleReplay : NoPipelineInputCmdlet {
        private CancellationTokenSource? _stop;

        /// <summary><para type="description">The dashboard (or any widget tree) whose widget names match the recording.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public IWidget Dashboard { get; set; } = null!;

        /// <summary><para type="description">The recording file; default the newest file in the automatic recording folder.</para></summary>
        [Parameter(Position = 1)]
        public string? RecordingPath { get; set; }

        /// <summary><para type="description">The run name in the header (default the recorded dashboard title, or the file name for other widget trees). A recorded dashboard's own header and footer are not replayed: its title, subtitle and status show in the player's header.</para></summary>
        [Parameter]
        public string? Title { get; set; }

        /// <summary><para type="description">The starting speed, 0.25 to 16 (default 1).</para></summary>
        [Parameter]
        [ValidateRange(0.25, 16)]
        [PSDefaultValue(Help = "1")]
        public double Speed { get; set; } = 1;

        /// <summary><para type="description">Plays below the cursor instead of on the whole window.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Inline { get; set; }

        /// <summary><para type="description">Starts playback immediately; use -AutoPlay:$false to open paused.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$true")]
        public SwitchParameter AutoPlay { get; set; } = new SwitchParameter(true);

        /// <summary><para type="description">The height of the recorded screen in rows (default the window, or the dashboard's own height inline).</para></summary>
        [Parameter]
        [ValidateRange(1, 500)]
        [PSDefaultValue(Help = "0")]
        public int Height { get; set; }

        /// <summary><para type="description">Interactive player controls (default), or automatic playback without reading keys.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "Interactive")]
        public PlaybackMode PlaybackMode { get; set; }

        /// <summary><para type="description">Samples the interactive recording at this frame rate; default ten.</para></summary>
        [Parameter, ValidateRange(0.01, 1000)]
        [PSDefaultValue(Help = "10")]
        public double FramesPerSecond { get; set; } = 10;

        /// <summary><para type="description">Maximum sampled interactive frames; default 3000.</para></summary>
        [Parameter, ValidateRange(1, int.MaxValue)]
        [PSDefaultValue(Help = "3000")]
        public int MaxFrames { get; set; } = 3000;

        /// <inheritdoc />
        protected override void EndProcessing() {
            string path = RecordingPath == null ? Newest() : SessionState.Path.GetUnresolvedProviderPathFromPSPath(RecordingPath);
            ReplayOptions options = new ReplayOptions {
                Title = Title,
                Speed = Speed,
                PlaybackMode = PlaybackMode,
                FramesPerSecond = FramesPerSecond,
                MaxFrames = MaxFrames,
                Inline = Inline.IsPresent,
                AutoPlay = AutoPlay.IsPresent,
                Height = Height > 0 ? Height : (int?)null
            };
            options.Title ??= Dashboard is Widgets.Dashboard ? null : System.IO.Path.GetFileNameWithoutExtension(path);
            if (options.PlaybackMode == Replay.PlaybackMode.Automatic) {
                PlayAutomatic(path, options);
                return;
            }
            InteractiveOptions interactive = options.Interactive?.Clone() ?? PSInteractive.Options(this, null);
            if (interactive.ResolveMode() != InteractionMode.Rich && interactive.LineConsole == null && Host?.UI != null) {
                interactive.LineConsole = new HostLineConsole(Host.UI);
            }
            options.Interactive = interactive;
            try {
                using (StreamReader reader = new StreamReader(path, Encoding.UTF8)) {
                    ReplayPlayer.Play(Dashboard, reader, options, (_stop ??= new CancellationTokenSource()).Token);
                }
            } catch (OperationCanceledException) {
                // Ctrl+C or a stopped pipeline ends the player.
            }
        }

        /// <inheritdoc />
        protected override void StopProcessing() {
            try {
                _stop?.Cancel();
            } catch (ObjectDisposedException) {
            }
            base.StopProcessing();
        }

        private string Newest() {
            string folder = LiveSessionOptions.DefaultRecordingFolder;
            FileInfo? newest = Directory.Exists(folder) ? new DirectoryInfo(folder).GetFiles("*.jsonl").OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault() : null;
            if (newest == null) {
                ThrowTerminatingError(new ErrorRecord(new FileNotFoundException("No automatic recordings in '" + folder + "'. Record with Invoke-ConsoleDashboard -AutoRecord or pass -RecordingPath."), "NoRecording", ErrorCategory.ObjectNotFound, folder));
            }
            return newest!.FullName;
        }
    }
}
