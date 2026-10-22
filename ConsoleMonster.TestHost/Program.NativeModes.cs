using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Live.Tails;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Text;

namespace ConsoleMonster.TestHost {
    public static partial class Program {
        private static int NativeModes(string receipt, string scenario) {
            try {
                // The owned hidden child must exercise native detection regardless of the test runner's CI/TERM settings.
                foreach (string name in new[] { "CI", "GITHUB_ACTIONS", "TF_BUILD", "BUILD_BUILDID", "JENKINS_URL", "GITLAB_CI", "TEAMCITY_VERSION" }) {
                    Environment.SetEnvironmentVariable(name, null);
                }
                Environment.SetEnvironmentVariable("TERM", "xterm-256color");
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_MODE", "interactive");
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_EMOJI_WIDTH", "2");
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_AMBIGUOUS_WIDTH", "1");
                IntPtr stdout = GetStdHandle(-11);
                uint inherited = ReadMode(stdout);
                uint baseline = inherited & ~4u;
                SetMode(stdout, baseline);
                try {
                    switch (scenario) {
                        case "repeated": RepeatedNativeTails(stdout, baseline); break;
                        case "outputs": NativeOutputs(stdout, baseline, true); break;
                        case "recording": NativeOutputs(stdout, baseline, false); break;
                        default: throw new ArgumentException("Unknown native mode contract.");
                    }
                    RequireMode(stdout, baseline, "contract completion");
                    File.WriteAllText(receipt, "NATIVE_MODES_OK\n" + scenario + "\nmode=exactly-restored\n");
                } finally { SetMode(stdout, inherited); }
                return 0;
            } catch (Exception error) {
                File.WriteAllText(receipt, error.ToString());
                return 1;
            }
        }

        private static void RepeatedNativeTails(IntPtr stdout, uint baseline) {
            // Leave Terminal untouched until the first Run, then reuse its naturally cached detected profile.
            foreach (LiveMode layout in new[] { LiveMode.FullScreen, LiveMode.Inline }) {
                foreach (string ending in new[] { "normal", "normal", "cancel", "source-failure" }) {
                    using (CancellationTokenSource stop = new CancellationTokenSource()) {
                        bool read = false;
                        bool failed = false;
                        try {
                            ConsoleLogTail.Run(() => {
                                read = true;
                                RequireMode(stdout, baseline | 4u, layout + "/" + ending + " source read");
                                if (ending == "cancel") { stop.Cancel(); }
                                if (ending == "source-failure") { throw new NativeSourceException(); }
                                return Array.Empty<TailedLogEntry>();
                            }, new LogTailOptions { Count = ending == "cancel" ? 0 : 1, Mode = layout, HandleCancelKey = false }, stop.Token);
                        } catch (NativeSourceException) { failed = true; }
                        if (!read || failed != (ending == "source-failure")) { throw new InvalidOperationException("Unexpected source completion."); }
                        RequireMode(stdout, baseline, layout + "/" + ending + " restored");
                        if (!Terminal.GetCurrentProfile().SupportsVirtualTerminal) { throw new InvalidOperationException("The cached profile lost VT support."); }
                    }
                }
            }
        }

        private static void NativeOutputs(IntPtr stdout, uint baseline, bool console) {
            // Warm the process-wide profile without allowing detection's VT activation to leak into the test baseline.
            using (TerminalGuard guard = new TerminalGuard(() => { }, false)) { Terminal.GetCurrentProfile(); }
            RequireMode(stdout, baseline, "profile warmup");
            foreach (uint entryMode in new[] { baseline, baseline | 4u }) {
                SetMode(stdout, entryMode);
                foreach (bool inline in new[] { false, true }) {
                    TerminalProfile profile = new TerminalProfile(ColorDepth.Ansi16, GlyphTier.Ascii, OutputMode.Interactive,
                        40, 6, supportsVirtualTerminal: true, isConsoleHandle: true);
                    using (ModeCheckingWriter writer = new ModeCheckingWriter(stdout, console, entryMode))
                    using (ITerminalOutput output = inline ? (ITerminalOutput)new InlineOutput(writer, profile, false) : new VtOutput(writer, profile, false)) {
                        for (int run = 0; run < 2; run++) {
                            ConsoleLogTail.Run(() => {
                                RequireMode(stdout, console ? entryMode | 4u : entryMode, "borrowed output source");
                                return Array.Empty<TailedLogEntry>();
                            }, new LogTailOptions { Count = 1, Profile = profile, Output = output, HandleCancelKey = false });
                            RequireMode(stdout, entryMode, "borrowed output EndLive");
                            if (writer.Disposed) { throw new InvalidOperationException("A borrowed writer was disposed."); }
                        }
                        // Scrollback and standalone writes happen outside the live lifetime, including final summaries.
                        CellBuffer frame = new CellBuffer(10, 2);
                        output.WriteBlock(frame);
                        output.WriteLine(StyledLine.From("summary"));
                        output.WriteInline(StyledLine.From("inline"));
                        RequireMode(stdout, entryMode, "one-shot writes");
                        foreach (string phase in new[] { "begin-write", "begin-flush", "end-write", "end-flush", "block-write", "block-flush" }) {
                            bool failed = false;
                            try {
                                if (phase.StartsWith("end", StringComparison.Ordinal)) { output.BeginLive(); }
                                writer.FailFlush = phase.EndsWith("flush", StringComparison.Ordinal);
                                writer.FailWrite = !writer.FailFlush;
                                if (phase.StartsWith("begin", StringComparison.Ordinal)) { output.BeginLive(); }
                                else if (phase.StartsWith("end", StringComparison.Ordinal)) { output.EndLive(); }
                                else { output.WriteBlock(frame); }
                            } catch (NativeWriterException) { failed = true; }
                            finally { writer.FailWrite = false; writer.FailFlush = false; }
                            if (!failed) { throw new InvalidOperationException("The writer fault did not escape: " + phase); }
                            RequireMode(stdout, entryMode, phase + " restored");
                            output.EndLive();
                            // A failed lifetime must leave the output reusable and must not dispose the borrowed writer.
                            output.BeginLive();
                            output.EndLive();
                            RequireMode(stdout, entryMode, "reuse after " + phase);
                        }
                    }
                    RequireMode(stdout, entryMode, "output disposal");
                }
            }
            SetMode(stdout, baseline);
        }

        private static uint ReadMode(IntPtr stdout) {
            if (!GetConsoleMode(stdout, out uint mode)) { throw new InvalidOperationException("The hidden child has no native console."); }
            return mode;
        }

        private static void RequireMode(IntPtr stdout, uint expected, string phase) {
            uint actual = ReadMode(stdout);
            if (actual != expected) { throw new InvalidOperationException(phase + ": mode " + expected + " expected, got " + actual + "."); }
        }

        private static void SetMode(IntPtr stdout, uint mode) {
            if (!SetConsoleMode(stdout, mode)) { throw new InvalidOperationException("Cannot set native output mode."); }
        }

        private sealed class ModeCheckingWriter : ITerminalWriter {
            private readonly IntPtr _stdout;
            private readonly uint _expected;
            private readonly ITerminalWriter? _native;
            internal bool FailWrite, FailFlush, Disposed;
            internal ModeCheckingWriter(IntPtr stdout, bool console, uint entryMode) {
                _stdout = stdout;
                IsConsole = console;
                _expected = console ? entryMode | 4u : entryMode;
                _native = console ? new ConsoleHandleWriter() : null;
            }
            public bool IsConsole { get; }
            public void Write(string text) {
                RequireMode(_stdout, _expected, "writer.Write");
                if (FailWrite) { throw new NativeWriterException(); }
                _native?.Write(text);
            }
            public void Flush() {
                RequireMode(_stdout, _expected, "writer.Flush");
                if (FailFlush) { throw new NativeWriterException(); }
                _native?.Flush();
            }
            public void Dispose() { Disposed = true; _native?.Dispose(); }
        }

        private sealed class NativeSourceException : Exception { }
        private sealed class NativeWriterException : IOException { }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr handle, uint mode);
    }
}
