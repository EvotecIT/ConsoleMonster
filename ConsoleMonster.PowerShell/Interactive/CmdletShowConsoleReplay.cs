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
    /// <summary>
    /// <para type="synopsis">Rewinds a recorded run: an in-terminal player with a timeline, markers, failures, warnings and log search.</para>
    /// <para type="description">Plays a recording (written by Invoke-ConsoleDashboard -RecordingPath or -AutoRecord) into the same dashboard and lets you move through it: space plays and pauses, left and right step frame by frame, + and - change the speed, PgUp and PgDn scrub, Home and End jump to the ends, n/p jump to the next or previous marker, f/F to failures, w/W to warnings, l/L to log entries, and / searches the log text (Enter for the next match). The replay timeline includes markers already present in the dashboard recording. Without -RecordingPath the newest automatic recording is played. Without an interactive console the markers are printed as lines.</para>
    /// </summary>
    /// <example>
    /// <para>Play an existing matching dashboard recording</para>
    /// <code># Requires build.jsonl recorded with this dashboard definition.
    /// $dashboard = New-ConsoleDashboard -Title Build -Content { New-ConsoleStatusList -WidgetId steps -Content { 'Compile' } }
    /// Show-ConsoleReplay -Dashboard $dashboard -RecordingPath ./build.jsonl</code>
    /// </example>
    /// <example>
    /// <para>Start an existing recording inline and paused</para>
    /// <code># Requires build.jsonl recorded with this dashboard definition.
    /// $dashboard = New-ConsoleDashboard -Title Build -Content { New-ConsoleStatusList -WidgetId steps -Content { 'Compile' } }
    /// Show-ConsoleReplay -Dashboard $dashboard -RecordingPath ./build.jsonl -Inline -Paused</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleReplay")]
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
        public double Speed { get; set; } = 1;

        /// <summary><para type="description">Plays below the cursor instead of on the whole window.</para></summary>
        [Parameter]
        public SwitchParameter Inline { get; set; }

        /// <summary><para type="description">Opens paused at the start.</para></summary>
        [Parameter]
        public SwitchParameter Paused { get; set; }

        /// <summary><para type="description">The height of the recorded screen in rows (default the window, or the dashboard's own height inline).</para></summary>
        [Parameter]
        [ValidateRange(1, 500)]
        public int Height { get; set; }

        /// <summary><para type="description">Interactive player controls (default), or automatic playback without reading keys.</para></summary>
        [Parameter]
        public PlaybackMode PlaybackMode { get; set; }

        /// <summary><para type="description">Samples the interactive recording at this frame rate; default ten.</para></summary>
        [Parameter, ValidateRange(0.01, 1000)]
        public double FramesPerSecond { get; set; } = 10;

        /// <summary><para type="description">Maximum sampled interactive frames; default 3000.</para></summary>
        [Parameter, ValidateRange(1, int.MaxValue)]
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
                AutoPlay = !Paused.IsPresent,
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
