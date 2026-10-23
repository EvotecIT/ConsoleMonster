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
        [UnixFact]
        public void FastExitedChildOutputCanBeDrainedBeforeClosingTheTerminal() {
            foreach (int exitCode in new[] { 0, 7 }) {
                SessionRecordingOptions options = new SessionRecordingOptions {
                    Shell = "/bin/sh", Width = 60, Height = 8, WorkingDirectory = Path.GetTempPath()
                };
                options.Arguments.Add("-c");
                options.Arguments.Add("printf 'Complete output before exit\\r\\n'; exit " + exitCode);
                using (UnixPseudoTerminal terminal = new UnixPseudoTerminal(options)) {
                    Assert.True(SpinWait.SpinUntil(() => terminal.HasExited, TimeSpan.FromSeconds(10)), "The synthetic child did not exit.");
                    Assert.Equal(exitCode, terminal.ExitCode);
                    StringBuilder output = new StringBuilder();
                    byte[] bytes = new byte[8192];
                    int count;
                    // Deliberately do not start reading until after the process has completely exited.
                    while ((count = terminal.Read(bytes)) > 0) { output.Append(Encoding.UTF8.GetString(bytes, 0, count)); }
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
