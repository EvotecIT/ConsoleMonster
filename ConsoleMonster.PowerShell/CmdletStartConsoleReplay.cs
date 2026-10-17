using System.IO;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Replays a recorded dashboard session (JSON lines) into a dashboard.</para>
    /// <para type="description">Recordings are written by Start-ConsoleDashboard -RecordPath. Replays keep the original timing, scaled by -Speed (0 plays as fast as possible). Useful for demos, documentation captures, tests and bug reports.</para>
    /// <example>
    ///   <para>Replay twice as fast</para>
    ///   <code>Start-ConsoleReplay -Dashboard $dashboard -Path .\run.jsonl -Speed 2</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Start, "ConsoleReplay")]
    public sealed class CmdletStartConsoleReplay : PSCmdlet {
        /// <summary><para type="description">The dashboard (or any widget tree) whose widget names match the recording.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public IWidget Dashboard { get; set; } = null!;

        /// <summary><para type="description">The recording file.</para></summary>
        [Parameter(Position = 1, Mandatory = true)]
        public string Path { get; set; } = string.Empty;

        /// <summary><para type="description">Replay speed (default 1).</para></summary>
        [Parameter]
        public double Speed { get; set; } = 1;

        /// <inheritdoc />
        protected override void EndProcessing() {
            string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
            LiveSessionOptions options = new LiveSessionOptions();
            HostQueueWriter? queue = null;
            Capabilities.TerminalProfile profile = Terminal.GetCurrentProfile();
            if (HostOutput.UseHost(this, profile)) {
                // Outside a real console the replay narrates through the host; the waits between events run here, on the
                // pipeline thread, and flush the queued lines as they go.
                queue = new HostQueueWriter();
                Capabilities.TerminalProfile hostProfile = HostOutput.HostProfile(this, profile, false);
                options.Profile = hostProfile;
                options.Output = Terminal.CreateOutput(hostProfile, queue);
                HostQueueWriter lines = queue;
                options.ReplayDelay = (delay, token) => {
                    while (delay > System.TimeSpan.Zero) {
                        System.TimeSpan slice = delay > System.TimeSpan.FromMilliseconds(50) ? System.TimeSpan.FromMilliseconds(50) : delay;
                        if (token.WaitHandle.WaitOne(slice)) {
                            token.ThrowIfCancellationRequested();
                        }
                        delay -= slice;
                        lines.Drain(this);
                    }
                };
            }
            try {
                using (StreamReader reader = new StreamReader(path, Encoding.UTF8)) {
                    LiveSession.Replay(Dashboard, reader, options, Speed);
                }
            } finally {
                queue?.Drain(this, true);
                options.Output?.Dispose();
            }
        }
    }
}
