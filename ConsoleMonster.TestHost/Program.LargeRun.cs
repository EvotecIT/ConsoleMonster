using System;
using System.IO;
using System.Linq;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.TestHost {
    public static partial class Program {
        private static void CancellationRun(string receipt) {
            IntPtr input = GetStdHandle(-10), output = GetStdHandle(-11);
            if (!GetConsoleMode(input, out uint inputBefore) || !GetConsoleMode(output, out uint outputBefore) || !GetConsoleCursorInfo(output, out CursorInfo cursorBefore)) {
                throw new InvalidOperationException("The isolated child needs a native console.");
            }
            var originalOut = Console.Out;
            var originalError = Console.Error;
            var profile = new TerminalProfile(ColorDepth.None, GlyphTier.Ascii, OutputMode.Interactive, 60, 24, supportsVirtualTerminal: true, isConsoleHandle: true);
            using (var run = new LiveRun(new[] { new RunItem("first"), new RunItem("last") }, new LiveRunOptions {
                InteractiveKeys = true, ProgressInterval = TimeSpan.Zero,
                Session = new LiveSessionOptions { Profile = profile, TaskbarProgress = false }
            })) {
                run.Started(0); run.Started(1);
                if (!SpinWait.SpinUntil(() => run.Dashboard.ShowKeys, TimeSpan.FromSeconds(5))) { throw new TimeoutException("The optional key reader did not open."); }
                if (!GetConsoleMode(input, out uint reading) || (reading & 1) == 0) { throw new InvalidOperationException("The key reader disabled normal Ctrl+C processing."); }
                if (!GenerateConsoleCtrlEvent(0, 0) || !run.Cancellation.WaitHandle.WaitOne(TimeSpan.FromSeconds(5))) { throw new TimeoutException("Ctrl+C did not reach the monitor."); }
                run.Finished(0, StatusKind.Pass); run.Finished(1, StatusKind.Warn, "last worker drained after cancellation");
                if (!run.Flush(TimeSpan.FromSeconds(5))) { throw new TimeoutException("Completion output did not drain."); }
                RunSnapshot snapshot = run.Complete();
                if (snapshot.Passed != 1 || snapshot.Warned != 1 || !run.Dashboard.Log.Entries.Any(entry => entry.Text.Contains("last worker drained"))) {
                    throw new InvalidOperationException("The last worker's completion was lost.");
                }
            }
            if (!GetConsoleMode(input, out uint inputAfter) || inputBefore != inputAfter || !GetConsoleMode(output, out uint outputAfter) || outputBefore != outputAfter
                || !GetConsoleCursorInfo(output, out CursorInfo cursorAfter) || cursorBefore.Visible != cursorAfter.Visible
                || !ReferenceEquals(originalOut, Console.Out) || !ReferenceEquals(originalError, Console.Error)) {
                throw new InvalidOperationException("Input, output, cursor or console writers were not restored.");
            }
            File.WriteAllText(receipt, "RUN_CTRL_C_OK\nkeys=enabled\nlast-worker=drained\ninput=restored\noutput=restored\nwriters=restored\n");
        }
    }
}
