using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ConsoleMonster.Sessions;
using Xunit;

namespace ConsoleMonster.Tests.Sessions {
    public sealed class UnixPseudoTerminalTests {
        [Fact]
        public void FinalOutputArrivingAfterAnEmptyPollIsDrainedBeforeEof() {
            byte[] bytes = new byte[16];
            byte[] tail = Encoding.UTF8.GetBytes("Final output");
            bool pending = false;
            Func<int, bool> waitReadable = _ => pending;
            Func<bool> hasExited = () => { pending = true; return true; };
            int count = UnixPseudoTerminal.ReadUntilOutputOrExit(waitReadable, hasExited, () => {
                tail.CopyTo(bytes, 0); pending = false; return tail.Length;
            });
            Assert.Equal("Final output", Encoding.UTF8.GetString(bytes, 0, count));
            Assert.Equal(0, UnixPseudoTerminal.ReadUntilOutputOrExit(waitReadable, () => true,
                () => throw new InvalidOperationException("There is no more output to read.")));
        }

        [Theory]
        [InlineData(false, 4, 12)]
        [InlineData(false, 8, 16)]
        [InlineData(true, 8, 12)]
        public void NativeChildStatusUsesTheProcessLayout(bool mac, int pointerSize, int pidOffset) {
            const int pid = 12345;
            foreach (int status in new[] { 0, 7 }) {
                byte[] info = new byte[128];
                BitConverter.GetBytes(1).CopyTo(info, 8); // CLD_EXITED
                BitConverter.GetBytes(pid).CopyTo(info, pidOffset);
                BitConverter.GetBytes(501).CopyTo(info, pidOffset + 4); // distinct uid, never a matching child PID
                BitConverter.GetBytes(status).CopyTo(info, pidOffset + 8);
                Assert.True(UnixPseudoTerminal.TryGetExitCode(info, mac, pointerSize, pid, out int exitCode));
                Assert.Equal(status, exitCode);
                Assert.False(UnixPseudoTerminal.TryGetExitCode(new byte[128], mac, pointerSize, pid, out _));
            }
        }

        [UnixFact]
        public void FastChildOutputCanBeDrainedAfterDelayingTheReader() {
            foreach (int exitCode in new[] { 0, 7 }) {
                SessionRecordingOptions options = new SessionRecordingOptions {
                    Shell = "/bin/sh", Width = 60, Height = 8, WorkingDirectory = Path.GetTempPath()
                };
                options.Arguments.Add("-c");
                options.Arguments.Add("printf 'Complete output before exit\\r\\n'; exit " + exitCode);
                using (UnixPseudoTerminal terminal = new UnixPseudoTerminal(options)) {
                    StringBuilder output = new StringBuilder();
                    byte[] bytes = new byte[8192];
                    int count;
                    // Darwin drains a session leader's terminal output during exit. Waiting for exit
                    // before the first read would deadlock that drain, even for a short-lived child.
                    Thread.Sleep(100);
                    while ((count = terminal.Read(bytes)) > 0) { output.Append(Encoding.UTF8.GetString(bytes, 0, count)); }
                    Assert.True(SpinWait.SpinUntil(() => terminal.HasExited, TimeSpan.FromSeconds(10)), "The drained synthetic child did not exit.");
                    Assert.Equal(exitCode, terminal.ExitCode);
                    Assert.Contains("Complete output before exit", output.ToString());
                }
            }
        }

        private sealed class UnixFactAttribute : FactAttribute {
            public UnixFactAttribute() {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    Skip = "A Linux or macOS pseudo-terminal is required.";
                }
            }
        }
    }
}
