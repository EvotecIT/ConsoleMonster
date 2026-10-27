using System.IO;
using System.Text;
using ConsoleMonster.Live;
namespace ConsoleMonster.PowerShell {
    public sealed partial class CmdletShowConsoleReplay {
        // Automatic playback retains the existing event engine and host-thread relay.
        private void PlayAutomatic(string path, Replay.ReplayOptions replay) {

            LiveSessionOptions options = replay.Session ?? new LiveSessionOptions();
            bool suppliedSession = replay.Session != null;
            replay.Session = options;
            HostQueueWriter? queue = null;
            Capabilities.TerminalProfile profile = Terminal.GetCurrentProfile();
            if (!suppliedSession && HostOutput.UseHost(this, profile)) {
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
                    Replay.ReplayPlayer.Play(Dashboard, reader, replay, (_stop ??= new System.Threading.CancellationTokenSource()).Token);
                }
            } catch (System.OperationCanceledException) when (_stop?.IsCancellationRequested == true) {
                // A stopped PowerShell pipeline ends automatic playback too.
            } finally {
                queue?.Drain(this, true);
                if (!suppliedSession) { options.Output?.Dispose(); }
                _stop?.Dispose();
            }
        }
    }
}
