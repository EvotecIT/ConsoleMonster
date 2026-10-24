using System;
#if !NETFRAMEWORK
using System.Threading;
using ConsoleMonster.Live;
#endif

namespace ConsoleMonster.TestHost {
    /// <summary>Real Unix Ctrl+C while LiveRun owns the raw keyboard reader.</summary>
    internal static class UnixLiveRunProbe {
#if !NETFRAMEWORK
        internal static void Run(Action<object> report) {
            using LiveRun run = new LiveRun(new[] { new RunItem("Synthetic work") }, new LiveRunOptions {
                InteractiveKeys = true, ProgressInterval = TimeSpan.Zero,
                Session = new LiveSessionOptions { CaptureConsole = false, TaskbarProgress = false }
            });
            // The parent observes raw termios before sending Ctrl+C, rather than racing reader startup.
            if (!SpinWait.SpinUntil(() => run.Dashboard.ShowKeys, TimeSpan.FromSeconds(5))) {
                throw new InvalidOperationException("Live run did not acquire its input.");
            }
            report(new { phase = "live_ready" });
            if (!run.Cancellation.WaitHandle.WaitOne(TimeSpan.FromSeconds(8))) {
                throw new InvalidOperationException("Keyboard Ctrl+C did not cancel the live run with keys enabled.");
            }
            run.Complete();
            report(new { phase = "live_pass" });
        }
#endif
    }
}
