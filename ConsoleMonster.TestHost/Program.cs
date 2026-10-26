using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Live;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.TestHost {
    // A separate executable is required: Terminal uses OS stdout, and a real Ctrl+C must never reach the test runner.
    public static partial class Program {
        private static int Main(string[] args) {
            try {
                switch (args[0]) {
                    case "handoff-probe": return TerminalHandoffProbe.Run(args[1]);
                    case "handoff-windows": return TerminalHandoffProbe.Windows(args[1]);
                    case "activity-frame": WriteActivityFrame(); break;
                    case "background": return BackgroundProbe.Run(args[1], args.Length > 2 ? args[2] : string.Empty);
                    case "unix-recording-child": return UnixRecordingChildProbe.Run();
                    case "copy-command": InteractiveClipboardProbe.Run(args[1], args[2]); break;
                    case "fallback": Fallback(); break;
                    case "cancel": return LaunchCancellation(args[1], args.Length > 2 ? args[2] : null);
                    case "cancel-child": Cancellation(args[1]); break;
                    case "cancel-run": return LaunchCancellation(args[1], args.Length > 2 ? args[2] : null, true);
                    case "cancel-run-child": CancellationRun(args[1]); break;
                    case "app-mode": return LaunchAppMode(args[1], args[2], args.Length > 3 ? args[3] : null);
                    case "app-mode-child": AppOutputModeProbe.Run(args[1], args[2]); break;
                    case "grid-mode-child": GridOutputModeProbe.Run(args[1], args[2]); break;
                    case "host": return LaunchHidden(args.Skip(1));
                    case "native-modes": return NativeModes(args[1], args[2]);
                    case "summary-locks": RenderLockOrder(true); break;
                    case "frame-locks": RenderLockOrder(false); break;
                    case "unix-input-probe": return UnixInputProbe.Run(args[1]);
                    default: throw new ArgumentException("Unknown process contract.");
                }
                return 0;
            } catch (Exception error) {
                if (args.Length > 2 && (args[0] == "app-mode-child" || args[0] == "grid-mode-child")) { File.WriteAllText(args[2], error.ToString()); }
                else if (args.Length > 1 && (args[0].StartsWith("cancel", StringComparison.Ordinal) || args[0] == "handoff-windows")) { File.WriteAllText(args[1], error.ToString()); }
                else { Console.Error.WriteLine(error); }
                return 1;
            }
        }

        private static void RenderLockOrder(bool summary) {
            TerminalProfile profile = TerminalProfile.Fixed(60, 12, ColorDepth.None, mode: summary ? OutputMode.Stream : OutputMode.Interactive);
            Terminal.Profile = profile;
            using (ManualResetEventSlim entered = new ManualResetEventSlim())
            using (ManualResetEventSlim writerRendering = new ManualResetEventSlim()) {
                CallbackWidget callback = new CallbackWidget(() => {
                    entered.Set();
                    if (!writerRendering.Wait(TimeSpan.FromSeconds(30))) { throw new TimeoutException("The concurrent renderer did not enter."); }
                    // Deliberately use the default overload: profile lookup must remain safe during a render callback.
                    Terminal.WriteLines(new[] { StyledLine.From("lock-note") });
                });
                using (LiveSession session = new LiveSession(summary ? (IWidget)TextBlock.Plain("work") : callback, new LiveSessionOptions {
                    Profile = profile, CaptureConsole = false, HandleCancelKey = false, PrintFinalState = false,
                    TaskbarProgress = false, Summary = summary ? callback : null, StopTimeout = TimeSpan.FromSeconds(30)
                })) {
                    session.Start();
                    Task? stop = summary ? Task.Run(() => session.Stop()) : null;
                    if (!entered.Wait(TimeSpan.FromSeconds(30))) { throw new TimeoutException("The session renderer did not enter."); }
                    Task writer = Task.Run(() => Terminal.Write(new CallbackWidget(() => {
                        writerRendering.Set();
                        session.Snapshot();
                    }), profile, new StringTerminalWriter()));
                    Task[] work = stop == null ? new[] { writer } : new[] { writer, stop };
                    if (!Task.WaitAll(work, TimeSpan.FromSeconds(30))) {
                        Console.Error.WriteLine("DEADLOCK: default Terminal output versus concurrent Snapshot during " + (summary ? "summary" : "frame"));
                        // The executable is an owned test child. A hard exit avoids running guard callbacks on deadlocked locks.
                        Process.GetCurrentProcess().Kill();
                    }
                    session.Stop();
                }
            }
            Console.WriteLine("LOCK_ORDER_OK");
        }

        private sealed class CallbackWidget : IWidget {
            private readonly Action _render;
            private int _called;
            internal CallbackWidget(Action render) { _render = render; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(1, 1);
            public void Render(Region region, RenderContext context) {
                if (Interlocked.Exchange(ref _called, 1) == 0) { _render(); }
            }
        }

        private static void Fallback() {
            TerminalProfile profile = TerminalProfile.Fixed(60, 12, ColorDepth.None, mode: OutputMode.Stream);
            using (LiveSession session = new LiveSession(new LogRegion(), new LiveSessionOptions {
                Profile = profile, CaptureConsole = false, HandleCancelKey = false, PrintFinalState = false
            })) {
                session.Start();
                Terminal.WriteLines(new InterruptedLines(session.Stop), profile: profile);
            }
        }

        private static void Cancellation(string receipt) {
            IntPtr stdout = GetStdHandle(-11);
            if (!GetConsoleMode(stdout, out uint modeBefore) || !GetConsoleCursorInfo(stdout, out CursorInfo cursorBefore)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            var originalOut = Console.Out;
            var originalError = Console.Error;
            LogRegion log = new LogRegion();
            bool summaryHadVirtualTerminal = false;
            TerminalProfile profile = new TerminalProfile(ColorDepth.None, GlyphTier.Ascii, OutputMode.Interactive,
                60, 12, supportsVirtualTerminal: true, isConsoleHandle: true);
            using (LiveSession session = new LiveSession(log, new LiveSessionOptions {
                Profile = profile, PrintFinalState = false, TaskbarProgress = false, StopTimeout = TimeSpan.FromSeconds(30),
                Summary = new CallbackWidget(() => summaryHadVirtualTerminal = GetConsoleMode(stdout, out uint mode) && (mode & 4) != 0)
            })) {
                session.Start();
                session.Write("before Ctrl+C");
                if (!GenerateConsoleCtrlEvent(0, 0)) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
                if (!session.Cancellation.WaitHandle.WaitOne(TimeSpan.FromSeconds(30))) { throw new TimeoutException("First Ctrl+C did not cancel the session."); }
                session.Write("after Ctrl+C");
                session.Stop();
                if (!summaryHadVirtualTerminal) { throw new InvalidOperationException("Native VT mode was restored before final summary rendering."); }
                if (session.EventsDropped != 1 || session.EventsPosted != session.EventsApplied || session.ApplyErrors != 0) {
                    throw new InvalidOperationException("Cancellation did not close admission and drain accepted work.");
                }
                if (!log.Entries.Select(entry => entry.Text).SequenceEqual(new[] { "before Ctrl+C" })) {
                    throw new InvalidOperationException("The post after Ctrl+C reached the log.");
                }
                if (!ReferenceEquals(originalOut, Console.Out) || !ReferenceEquals(originalError, Console.Error)) {
                    throw new InvalidOperationException("Console writers were not restored.");
                }
                bool readMode = GetConsoleMode(stdout, out uint modeAfter);
                bool readCursor = GetConsoleCursorInfo(stdout, out CursorInfo cursorAfter);
                if (!readMode || modeAfter != modeBefore || !readCursor || cursorAfter.Visible != cursorBefore.Visible) {
                    throw new InvalidOperationException("The native terminal was not restored: mode " + modeBefore + " -> " + modeAfter
                        + ", cursor " + cursorBefore.Visible + " -> " + cursorAfter.Visible + ", reads=" + readMode + "/" + readCursor + ".");
                }
                File.WriteAllText(receipt, "CTRL_C_OK\naccepted=1\napplied=1\nrejected=1\nwriters=restored\nmode=restored\ncursor=restored\n");
            }
        }

        private static int LaunchCancellation(string receipt, string? runtime, bool run = false) {
            // CREATE_NEW_CONSOLE + SW_HIDE gives the signal test its own hidden console. No signal reaches the caller.
            string executable = Process.GetCurrentProcess().MainModule!.FileName!;
            List<string> arguments = new List<string> { executable };
#if !NETFRAMEWORK
            arguments.Add("exec");
            arguments.Add("--runtimeconfig");
            arguments.Add(runtime ?? throw new InvalidOperationException("No host runtime configuration."));
            arguments.Add(typeof(Program).Assembly.Location);
#endif
            arguments.Add(run ? "cancel-run-child" : "cancel-child");
            arguments.Add(receipt);
            return LaunchHidden(arguments);
        }

        private static int LaunchAppMode(string scenario, string receipt, string? runtime) {
            List<string> arguments = new List<string> { Process.GetCurrentProcess().MainModule!.FileName! };
#if !NETFRAMEWORK
            arguments.Add("exec");
            arguments.Add("--runtimeconfig");
            arguments.Add(runtime ?? throw new InvalidOperationException("No host runtime configuration."));
            arguments.Add(typeof(Program).Assembly.Location);
#endif
            arguments.Add("app-mode-child");
            arguments.Add(scenario);
            arguments.Add(receipt);
            return LaunchHidden(arguments);
        }

        private static int LaunchHidden(IEnumerable<string> arguments) {
            StringBuilder command = new StringBuilder(string.Join(" ", arguments.Select(value => CommandLine.Quote(value, CommandStyle.Windows))));
            StartupInfo startup = new StartupInfo { Size = Marshal.SizeOf(typeof(StartupInfo)), Flags = 1, ShowWindow = 0 };
            if (!CreateProcess(null, command, IntPtr.Zero, IntPtr.Zero, false, 0x10, IntPtr.Zero, null, ref startup, out ProcessInfo process)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            try {
                if (WaitForSingleObject(process.Process, 30000) != 0) {
                    TerminateProcess(process.Process, 1);
                    throw new TimeoutException("The Ctrl+C child did not exit.");
                }
                if (!GetExitCodeProcess(process.Process, out int exit)) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
                return exit;
            } finally {
                CloseHandle(process.Thread);
                CloseHandle(process.Process);
            }
        }

        private sealed class InterruptedLines : IReadOnlyList<StyledLine> {
            private readonly Action _between;
            internal InterruptedLines(Action between) { _between = between; }
            public int Count => 2;
            public StyledLine this[int index] => StyledLine.From(index == 0 ? "fallback-first" : "fallback-last");
            public IEnumerator<StyledLine> GetEnumerator() { yield return this[0]; _between(); yield return this[1]; }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CursorInfo { public uint Size; [MarshalAs(UnmanagedType.Bool)] public bool Visible; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StartupInfo {
            public int Size;
            public string? Reserved, Desktop, Title;
            public uint X, Y, XSize, YSize, XChars, YChars, Fill, Flags;
            public short ShowWindow, ReservedSize;
            public IntPtr ReservedPointer, Input, Output, Error;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInfo { public IntPtr Process, Thread; public int ProcessId, ThreadId; }
        [DllImport("kernel32.dll")] private static extern IntPtr GetStdHandle(int id);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetConsoleMode(IntPtr handle, out uint mode);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetConsoleCursorInfo(IntPtr handle, out CursorInfo info);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GenerateConsoleCtrlEvent(uint signal, uint group);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateProcess(string? application, StringBuilder command, IntPtr processSecurity, IntPtr threadSecurity,
            bool inherit, uint flags, IntPtr environment, string? directory, ref StartupInfo startup, out ProcessInfo process);
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetExitCodeProcess(IntPtr handle, out int exit);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateProcess(IntPtr handle, uint exit);
    }
}
