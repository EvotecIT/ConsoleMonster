using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ConsoleMonster.Interactive;
using Xunit;

namespace ConsoleMonster.Tests.Clipboard {
    public sealed class InteractiveClipboardProcessTests {
        [UnixTheory]
        [InlineData("form")]
        [InlineData("menu")]
        [InlineData("console")]
        public async Task DefaultCopyActionHonorsItsTargetAndBypassesLiveCapture(string scenario) {
            string directory = Path.Combine(Path.GetTempPath(), "ConsoleMonster-Copy-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "copied");
            try {
                string runtime = Path.ChangeExtension(typeof(InteractiveClipboardProcessTests).Assembly.Location, ".runtimeconfig.json");
                string host = typeof(TestHost.Program).Assembly.Location;
                ProcessStartInfo info = new ProcessStartInfo("dotnet", "exec --runtimeconfig " + Quote(runtime) + " " + Quote(host) + " copy-command " + scenario + " " + Quote(path)) {
                    UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
                };
                info.EnvironmentVariables["SSH_TTY"] = "synthetic-ssh-session";
                using (Process process = Process.Start(info)!) {
                    Task<string> stdout = process.StandardOutput.ReadToEndAsync(), stderr = process.StandardError.ReadToEndAsync();
                    if (!process.WaitForExit(30000)) { process.Kill(); process.WaitForExit(); throw new TimeoutException("The scripted copy interaction did not exit."); }
                    string output = await stdout, error = await stderr;
                    Assert.True(process.ExitCode == 0, error + output);
                    string expected = "\u001b]52;c;" + Convert.ToBase64String(Encoding.UTF8.GetBytes("Invoke-Thing zażółć 日本")) + "\u0007";
                    if (scenario == "console") { Assert.Contains(expected, output); Assert.Equal("", File.ReadAllText(path)); }
                    else { Assert.Equal(expected, File.ReadAllText(path)); Assert.DoesNotContain(expected, output); }
                }
            } finally { Directory.Delete(directory, true); }
        }

        private static string Quote(string value) => CommandLine.Quote(value, CommandStyle.Windows);
        private sealed class UnixTheoryAttribute : TheoryAttribute {
            public UnixTheoryAttribute() {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    Skip = "The SSH clipboard output oracle requires Unix.";
                }
            }
        }
    }
}
