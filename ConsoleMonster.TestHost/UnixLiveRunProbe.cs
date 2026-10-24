using System;
#if !NETFRAMEWORK
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
#endif

namespace ConsoleMonster.TestHost {
    /// <summary>Real Unix Ctrl+C while LiveRun owns the raw keyboard reader.</summary>
    internal static class UnixLiveRunProbe {
#if !NETFRAMEWORK
        internal static void Run(Action<object> report) {
            using LiveRun run = new LiveRun(new[] { new RunItem("Synthetic work") }, new LiveRunOptions {
                InteractiveKeys = true, ProgressInterval = TimeSpan.Zero,
                Session = new LiveSessionOptions {
                    CaptureConsole = false, TaskbarProgress = false,
                    // This owned PTY is interactive even when the outer test runner is in CI.
                    Profile = TerminalProfile.Fixed(120, 40)
                }
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
            using ClosedInputTarget target = new ClosedInputTarget();
            using LiveRun unavailable = new LiveRun(new[] { new RunItem("No keyboard") }, new LiveRunOptions {
                InteractiveKeys = true, ProgressInterval = TimeSpan.Zero,
                Session = new LiveSessionOptions { Target = target, CaptureConsole = false, TaskbarProgress = false }
            });
            if (!target.Attempted.WaitOne(TimeSpan.FromSeconds(5))) {
                throw new InvalidOperationException("Live run did not attempt to open the target input.");
            }
            unavailable.Complete();
            if (unavailable.Dashboard.ShowKeys) { throw new InvalidOperationException("Closed input still advertises live keys."); }
            report(new { phase = "live_closed_input_pass" });
        }

        private sealed class ClosedInputTarget : ITerminalTarget, IDisposable {
            internal readonly ManualResetEvent Attempted = new ManualResetEvent(false);
            public string Name => "closed keyboard";
            public TerminalProfile Profile => TerminalProfile.Fixed(120, 40);
            public ITheme? Theme => null;
            public ITerminalWriter Writer { get; } = new StringTerminalWriter();
            public IInputSource? Input { get { Attempted.Set(); throw new InputClosedException(); } }
            public void Dispose() { Attempted.Dispose(); }
        }
#endif
    }
}
