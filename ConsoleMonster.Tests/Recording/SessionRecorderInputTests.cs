using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using ConsoleMonster.Sessions;
using Xunit;

namespace ConsoleMonster.Tests {
    /// <summary>Real PTY contract used by scripted terminal consumers, including the website recorder.</summary>
    public sealed class SessionRecorderInputTests : IDisposable {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "cm-input-" + Guid.NewGuid().ToString("N"));

        [Fact]
        public void ReadinessWaitSeesEarlyOutputAndInputResponseBeforeDrain() {
            var options = Options();
            options.Shell = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "powershell.exe" : "/bin/sh";
            foreach (string argument in RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new[] { "-NoLogo", "-NoProfile", "-Command", "[Console]::Write('READY'); $line = [Console]::ReadLine(); [Console]::Write('ACK:' + $line)" }
                : new[] { "-c", "printf READY; read line; printf 'ACK:%s' \"$line\"" }) { options.Arguments.Add(argument); }
            using (var recorder = SessionRecorder.Start(options)) {
                recorder.WaitForOutput("READY", TimeSpan.FromSeconds(10));
                recorder.SendInputAfter(TimeSpan.FromMilliseconds(10), "hello\r");
                recorder.WaitForOutput("ACK:hello", TimeSpan.FromSeconds(10));
                recorder.WaitForExit(default); // Existing CancellationToken call remains unambiguous.
                Assert.Equal(0, recorder.ExitCode);
                Assert.Throws<ObjectDisposedException>(() => recorder.WaitForOutput("READY", TimeSpan.FromSeconds(1)));
            }
            Assert.Contains("ACK:hello", File.ReadAllText(options.Path!));
        }

        [Fact]
        public void HeadlessTerminalAnswersTheCursorQueryThroughTheOwnedPty() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; } // ConPTY supplies the Windows console response itself.
            var options = Options();
            options.Shell = "/bin/sh";
            options.RespondToCursorQueries = true;
            options.Arguments.Add("-c");
            options.Arguments.Add("stty -echo -icanon min 1 time 0; printf '\\033[3;4H\\033[6n'; answer=$(dd bs=1 count=6 2>/dev/null); test \"$answer\" = \"$(printf '\\033[3;4R')\" && printf DONE");
            using (var recorder = SessionRecorder.Start(options)) {
                recorder.WaitForOutput("DONE", TimeSpan.FromSeconds(10));
                recorder.WaitForExit();
                Assert.Equal(0, recorder.ExitCode);
            }
        }

        [Fact]
        public void CursorReplyTracksSplitQueriesWideCellsAndIgnoresPrivateRequests() {
            string? reply = null;
            var screen = new VtScreen(40, 10) { TerminalResponse = text => reply = text };
            screen.Feed("\u001b[3;4H界\u001b[");
            screen.Feed("6n");
            Assert.Equal("\u001b[3;6R", reply);
            reply = null;
            screen.Feed("\u001b[?6n");
            Assert.Null(reply);
            screen.Feed("\u001b[3;8r\u001b[?6h\u001b[1;4H\u001b[6n");
            Assert.Equal("\u001b[1;4R", reply);
        }

        [Fact]
        public void CachedReadinessStillHonoursCancellationAndStopsTheChild() {
            var options = WaitingOptions();
            using var recorder = SessionRecorder.Start(options);
            recorder.WaitForOutput("READY", TimeSpan.FromSeconds(10));
            using var cancel = new CancellationTokenSource();
            cancel.Cancel();
            Assert.Throws<OperationCanceledException>(() => recorder.WaitForOutput("READY", TimeSpan.FromSeconds(10), cancel.Token));
            Assert.True(recorder.HasExited);
        }

        [Fact]
        public void ZeroDelayInputCannotOutliveTheConfiguredSession() {
            var options = WaitingOptions();
            options.SessionTimeout = TimeSpan.FromSeconds(3);
            using var recorder = SessionRecorder.Start(options);
            recorder.WaitForOutput("READY", TimeSpan.FromSeconds(10));
            Thread.Sleep(TimeSpan.FromMilliseconds(3100));
            Assert.Throws<TimeoutException>(() => recorder.SendInputAfter(TimeSpan.Zero, "late"));
            Assert.True(recorder.HasExited);
        }

        [Fact]
        public void TimedInterruptUsesTheOwnedControllingTerminal() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }
            var options = Options();
            options.Shell = "/bin/sh";
            options.Arguments.Add("-c");
            options.Arguments.Add("trap 'printf STOPPED; exit 0' INT; printf READY; while :; do sleep 1; done");
            using var recorder = SessionRecorder.Start(options);
            recorder.WaitForOutput("READY", TimeSpan.FromSeconds(10));
            recorder.WaitForExitWithInterrupt(TimeSpan.FromMilliseconds(200));
            Assert.True(recorder.Interrupted);
            Assert.Equal(0, recorder.ExitCode);
            Assert.Contains("STOPPED", File.ReadAllText(options.Path!));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UnreadTerminalRepliesCannotBlockTimeoutCleanup(bool interrupt) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }
            var options = Options();
            options.Shell = "/bin/sh";
            options.RespondToCursorQueries = true;
            options.Timeout = TimeSpan.FromMilliseconds(500);
            options.Arguments.Add("-c");
            options.Arguments.Add("stty -echo -icanon -isig; printf READY; i=0; while [ \"$i\" -lt 10000 ]; do printf '\\033[6n'; i=$((i+1)); done; sleep 30");
            using var recorder = SessionRecorder.Start(options);
            recorder.WaitForOutput("READY", TimeSpan.FromSeconds(10));
            Assert.Throws<TimeoutException>(() => {
                if (interrupt) { recorder.WaitForExitWithInterrupt(TimeSpan.FromMilliseconds(250)); }
                else { recorder.WaitForExit(); }
            });
            Assert.True(recorder.HasExited);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ScriptedInputBackpressureHonoursCancellationOrSessionDeadline(bool cancel) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return; }
            var options = Options();
            options.Shell = "/bin/sh";
            options.RespondToCursorQueries = true;
            options.SessionTimeout = cancel ? null : TimeSpan.FromMilliseconds(500);
            options.Arguments.Add("-c");
            options.Arguments.Add("stty -echo -icanon -isig; printf READY; i=0; while [ \"$i\" -lt 10000 ]; do printf '\\033[6n'; i=$((i+1)); done; sleep 30");
            using var recorder = SessionRecorder.Start(options);
            recorder.WaitForOutput("READY", TimeSpan.FromSeconds(10));
            using var cancellation = new CancellationTokenSource();
            if (cancel) {
                cancellation.CancelAfter(250);
                Assert.Throws<OperationCanceledException>(() => recorder.SendInputAfter(TimeSpan.FromMilliseconds(100), "input", cancellation.Token));
            } else {
                Assert.Throws<TimeoutException>(() => recorder.SendInputAfter(TimeSpan.FromMilliseconds(100), "input"));
            }
            Assert.True(recorder.HasExited);
        }

        private SessionRecordingOptions WaitingOptions() {
            var options = Options();
            options.Shell = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "powershell.exe" : "/bin/sh";
            foreach (string argument in RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new[] { "-NoLogo", "-NoProfile", "-Command", "[Console]::Write('READY'); Start-Sleep -Seconds 30" }
                : new[] { "-c", "printf READY; sleep 30" }) { options.Arguments.Add(argument); }
            return options;
        }

        [Fact]
        public void ReadinessTimeoutStopsAndFinalizesTheOwnedChild() {
            var options = Options();
            options.Shell = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "powershell.exe" : "/bin/sh";
            foreach (string argument in RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new[] { "-NoLogo", "-NoProfile", "-Command", "Start-Sleep -Seconds 30" }
                : new[] { "-c", "sleep 30" }) { options.Arguments.Add(argument); }
            using (var recorder = SessionRecorder.Start(options)) {
                Assert.Throws<TimeoutException>(() => recorder.WaitForOutput("absent", TimeSpan.FromMilliseconds(100)));
                Assert.True(recorder.HasExited);
            }
            Assert.True(File.Exists(options.Path! + ".json"));
        }

        private SessionRecordingOptions Options() {
            Directory.CreateDirectory(_folder);
            return new SessionRecordingOptions { Path = Path.Combine(_folder, "input.cast"), Width = 40, Height = 10, Timeout = TimeSpan.FromSeconds(15) };
        }

        public void Dispose() { if (Directory.Exists(_folder)) { Directory.Delete(_folder, true); } }
    }
}
