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
            using (StreamReader reader = new StreamReader(path, Encoding.UTF8)) {
                LiveSession.Replay(Dashboard, reader, new LiveSessionOptions(), Speed);
            }
        }
    }
}
