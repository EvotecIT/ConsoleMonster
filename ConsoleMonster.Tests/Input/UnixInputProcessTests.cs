using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ConsoleMonster.Interactive;
using Xunit;

namespace ConsoleMonster.Tests.Input {
    /// <summary>Actual Unix TTY input and mode restoration, isolated from the test runner's terminal.</summary>
    public sealed class UnixInputProcessTests {
        /// <summary>SGR mouse, Unicode and keys pass through the real backend, and disposal restores the entire termios state.</summary>
        [UnixFact]
        public async Task ActualTtyInput_RoundTripsEventsAndRestoresModeBeforeProcessExit() {
            string directory = Path.Combine(Path.GetTempPath(), "ConsoleMonster-UnixInput-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string script = Path.Combine(directory, "unix_input_pty.py");
            try {
                using (Stream resource = typeof(UnixInputProcessTests).Assembly.GetManifestResourceStream("ConsoleMonster.Tests.Input.Fixtures.unix_input_pty.py")!)
                using (FileStream target = File.Create(script)) { resource.CopyTo(target); }
                string host = typeof(TestHost.Program).Assembly.Location;
                string runtime = Path.ChangeExtension(typeof(UnixInputProcessTests).Assembly.Location, ".runtimeconfig.json");
                ProcessStartInfo info = new ProcessStartInfo("python3", Quote(script) + " " + Quote(host) +
                    " --runtimeconfig " + Quote(runtime) + " --output " + Quote(Path.Combine(directory, "receipt"))) {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
                };
                using (Process process = Process.Start(info)!) {
                    Task<string> output = process.StandardOutput.ReadToEndAsync();
                    Task<string> error = process.StandardError.ReadToEndAsync();
                    // The Python parent owns the child/PTY and enforces its own whole-run watchdog and cleanup.
                    await Task.Run(() => process.WaitForExit());
                    Assert.True(process.ExitCode == 0, await output + await error);
                }
            } finally { Directory.Delete(directory, true); }
        }

        private static string Quote(string text) => CommandLine.Quote(text, CommandStyle.Windows);

        private sealed class UnixFactAttribute : FactAttribute {
            public UnixFactAttribute() {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    Skip = "A Linux or macOS PTY is required for the native terminal oracle.";
                }
            }
        }
    }
}
