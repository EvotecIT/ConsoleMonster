using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConsoleMonster.Interactive;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    public sealed partial class SessionProcessTests {
        [Theory]
        [InlineData("summary-locks")]
        [InlineData("frame-locks")]
        public async Task RenderCallback_DefaultOutputAndConcurrentSnapshotCannotDeadlock(string scenario) {
            var result = await Run(scenario);
            Assert.True(result.Exit == 0, result.Error + result.Output);
            Assert.Equal(string.Empty, result.Error);
            Assert.Contains("LOCK_ORDER_OK", result.Output);
            Assert.Single(Regex.Matches(result.Output, "lock-note"));
        }

        [Fact]
        public async Task StopDuringWritePreparation_PrintsTheWholeFallbackExactlyOnceToNativeStdout() {
            var result = await Run("fallback");
            Assert.True(result.Exit == 0, result.Error + result.Output);
            Assert.Equal(string.Empty, result.Error);
            Assert.Equal("fallback-first\nfallback-last\n", result.Output.Replace("\r\n", "\n"));
        }

        [WindowsFact]
        public async Task FirstActualCtrlC_RejectsLaterPostsAndRestoresNativeTerminalAndWriters() {
            string receipt = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
            try {
                var result = await Run("cancel " + CommandLine.Quote(receipt, CommandStyle.Windows));
                string actual = File.Exists(receipt) ? File.ReadAllText(receipt) : "The Ctrl+C child produced no receipt.";
                Assert.True(result.Exit == 0, actual + result.Output + result.Error);
                Assert.Equal("CTRL_C_OK\naccepted=1\napplied=1\nrejected=1\nwriters=restored\nmode=restored\ncursor=restored\n", actual);
            } finally { if (File.Exists(receipt)) { File.Delete(receipt); } }
        }

        [WindowsTheory]
        [InlineData("normal")]
        [InlineData("startup")]
        [InlineData("cancel")]
        public async Task FirstUseApp_RestoresNativeOutputMode(string scenario) {
            string receipt = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
            try {
                var result = await Run("app-mode " + scenario + " " + CommandLine.Quote(receipt, CommandStyle.Windows));
                string actual = File.Exists(receipt) ? File.ReadAllText(receipt) : "The native app child produced no receipt.";
                Assert.True(result.Exit == 0, actual + result.Output + result.Error);
                Assert.Equal("APP_MODE_OK " + scenario + " 3->3\n", actual);
            } finally { if (File.Exists(receipt)) { File.Delete(receipt); } }
        }

        [WindowsTheory]
        [InlineData("accepted")]
        [InlineData("cancelled")]
        [InlineData("escape")]
        [InlineData("unavailable")]
        public async Task FirstUseGrid_RestoresOriginalWindowsOutputMode(string scenario) {
            string receipt = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
            string assembly = typeof(TestHost.Program).Assembly.Location;
#if NETFRAMEWORK
            assembly = new Uri(typeof(TestHost.Program).Assembly.CodeBase!).LocalPath;
            string command = Quote(assembly);
#else
            string runtime = Path.ChangeExtension(typeof(SessionProcessTests).Assembly.Location, ".runtimeconfig.json");
            string command = "dotnet exec --runtimeconfig " + Quote(runtime) + " " + Quote(assembly);
#endif
            try {
                var result = await Run("host " + command + " grid-mode-child " + scenario + " " + Quote(receipt));
                string actual = File.Exists(receipt) ? File.ReadAllText(receipt) : "The grid child produced no receipt.";
                Assert.True(result.Exit == 0, actual + result.Output + result.Error);
                Assert.Equal("GRID_MODE_OK " + scenario + " 3->3\n", actual);
            } finally { if (File.Exists(receipt)) { File.Delete(receipt); } }
        }

        private static async Task<(int Exit, string Output, string Error)> Run(string arguments) {
            string assembly = typeof(TestHost.Program).Assembly.Location;
#if NETFRAMEWORK
            // xUnit shadow-copies referenced assemblies into separate directories; execute the original built host.
            assembly = new Uri(typeof(TestHost.Program).Assembly.CodeBase!).LocalPath;
            ProcessStartInfo info = new ProcessStartInfo(assembly, arguments);
#else
            string runtime = Path.ChangeExtension(typeof(SessionProcessTests).Assembly.Location, ".runtimeconfig.json");
            if (arguments.StartsWith("cancel ", StringComparison.Ordinal) || arguments.StartsWith("app-mode ", StringComparison.Ordinal)) { arguments += " " + Quote(runtime); }
            ProcessStartInfo info = new ProcessStartInfo("dotnet", "exec --runtimeconfig " + Quote(runtime) + " " + Quote(assembly) + " " + arguments);
#endif
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            using (Process process = Process.Start(info)!) {
                Task<string> output = process.StandardOutput.ReadToEndAsync();
                Task<string> error = process.StandardError.ReadToEndAsync();
                Task exited = Task.Run(() => process.WaitForExit());
                try {
                    Assert.Same(exited, await Task.WhenAny(exited, Task.Delay(TimeSpan.FromSeconds(45))));
                    await exited;
                    return (process.ExitCode, await output, await error);
                } finally { if (!process.HasExited) { process.Kill(); } }
            }
        }

        private static string Quote(string value) => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? CommandLine.Quote(value, CommandStyle.Windows) : "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        private sealed class WindowsTheoryAttribute : TheoryAttribute {
            public WindowsTheoryAttribute() {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { Skip = "An isolated native Windows console is required for the output-mode oracle."; }
            }
        }

        private sealed class WindowsFactAttribute : FactAttribute {
            public WindowsFactAttribute() {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { Skip = "An isolated native Windows console is required for the Ctrl+C signal oracle."; }
            }
        }

    }
}
