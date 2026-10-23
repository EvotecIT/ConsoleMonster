using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>
    /// The site's own scripts, run by Node on a controlled clock: replay playback (play once, the final-screen hold, and
    /// what cancels it) and the PowerShell | C# switch of a Try it block.
    /// </summary>
    public sealed class ScriptContracts {
        [Theory]
        [InlineData("playback.test.cjs")]
        [InlineData("lang-choice.test.cjs")]
        public async System.Threading.Tasks.Task Script_Contracts_Pass(string script) {
            string website = WebsiteRoot();
            string path = Path.Combine(website, "tools", "SiteFrames.Tests", "Scripts", script);
            ProcessStartInfo info = new ProcessStartInfo("node") {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            info.ArgumentList.Add(path);
            info.ArgumentList.Add(website);
            Process process;
            try {
                process = Process.Start(info)!;
            } catch (System.ComponentModel.Win32Exception ex) {
                throw new InvalidOperationException("These contracts need Node.js on PATH (" + ex.Message + ").", ex);
            }
            using (process) {
                // Both streams are read while the script runs, so neither can fill up and stall it.
                System.Threading.Tasks.Task<string> output = process.StandardOutput.ReadToEndAsync();
                System.Threading.Tasks.Task<string> errors = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(60000)) {
                    process.Kill(entireProcessTree: true);
                    Assert.Fail(script + " did not finish within a minute.");
                }
                await process.WaitForExitAsync();
                Assert.True(process.ExitCode == 0, script + " failed:" + Environment.NewLine + await output + await errors);
            }
        }

        private static string WebsiteRoot() {
            DirectoryInfo? repository = new DirectoryInfo(AppContext.BaseDirectory);
            while (repository != null && !File.Exists(Path.Combine(repository.FullName, "ConsoleMonster.sln"))) {
                repository = repository.Parent;
            }
            Assert.NotNull(repository);
            return Path.Combine(repository!.FullName, "Website");
        }
    }
}
