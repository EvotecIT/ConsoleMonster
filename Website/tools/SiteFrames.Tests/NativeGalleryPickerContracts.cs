using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using SiteFrames.Capture;
using SiteFrames.Vt;
using Xunit;
using Xunit.Abstractions;

namespace SiteFrames.Tests {
    /// <summary>Checks the first command with a real console input and a separately redirected output handle.</summary>
    public sealed class NativeGalleryPickerContracts {
        private readonly ITestOutputHelper _output;
        public NativeGalleryPickerContracts(ITestOutputHelper output) => _output = output;

        [NativeEditorTheory]
        [InlineData("ps5", false)]
        [InlineData("ps7", false)]
        [InlineData("ps5", true)]
        [InlineData("ps7", true)]
        public void OutputOnlyRedirectionRequiresASlugButNamedDemosStillRun(string hostId, bool namedDemo) {
            string repository = Repository();
            string modules = PowerShellRunner.ModuleLinkRoot(new Site(Path.Combine(repository, "Website")));
            string folder = Path.Combine(Path.GetTempPath(), "cm-picker-redirect-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string script = Path.Combine(folder, "picker.ps1");
            string log = Path.Combine(folder, "stdout.txt");
            string pid = Path.Combine(folder, "child.pid");
            string capture = Path.Combine(folder, "parent.ansi");
            string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
            string command = namedDemo ? "Show-ConsoleDemo welcome-compact" : "Show-ConsoleDemo";
            File.WriteAllText(script,
                "$ErrorActionPreference = 'Stop'\n" +
                "[IO.File]::WriteAllText(" + Quote(pid) + ", $PID.ToString() + '|' + [Diagnostics.Process]::GetCurrentProcess().StartTime.ToUniversalTime().Ticks)\n" +
                "[Console]::Error.WriteLine('INPUT=' + [Console]::IsInputRedirected + ';OUTPUT=' + [Console]::IsOutputRedirected)\n" +
                "$env:PSModulePath = " + Quote(modules) + " + [IO.Path]::PathSeparator + $env:PSModulePath\n" +
                "Import-Module ConsoleMonster -Force\n" +
                "try { " + command + "; [Console]::Error.WriteLine('DEMO_RETURNED') }\n" +
                "catch { [Console]::Error.WriteLine('DEMO_ERROR=' + $_.FullyQualifiedErrorId) }\n" +
                "[Console]::Error.WriteLine('DEMO_FINISHED')\n", new UTF8Encoding(true));
            var host = PowerShellHost.All.Single(value => value.Id == hostId);
            var environment = PowerShellRunner.Environment("dark");
            environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = NativeEditorTheoryAttribute.Configuration;
            environment["CONSOLEMONSTER_INPUT"] = null;
            environment["CONSOLEMONSTER_SNAPSHOT"] = null;
            environment["CONSOLEMONSTER_NONINTERACTIVE"] = null;
            environment["CONSOLEMONSTER_ACCESSIBLE"] = null;
            // cmd changes only stdout: stdin remains the attached ConPTY console. No -NonInteractive switch is used.
            string launch = "cmd.exe /d /s /c \"\"" + host.Executable + "\" -NoLogo -NoProfile -File \"" + script + "\" > \"" + log + "\"\"";
            try {
                var chunks = PseudoConsole.Run(launch, 100, 24, environment, repository, TimeSpan.FromSeconds(12),
                    readyText: "INPUT=False;OUTPUT=True", inputKeys: string.Empty, capturePath: capture);
                string output = string.Concat(chunks.Select(chunk => Encoding.UTF8.GetString(chunk.Data)));
                Assert.Contains("INPUT=False;OUTPUT=True", output, StringComparison.Ordinal);
                Assert.Contains("DEMO_FINISHED", output, StringComparison.Ordinal);
                if (namedDemo) {
                    Assert.Contains("DEMO_RETURNED", output, StringComparison.Ordinal);
                    Assert.DoesNotContain("DEMO_ERROR=", output, StringComparison.Ordinal);
                    Assert.False(string.IsNullOrWhiteSpace(File.ReadAllText(log)));
                } else {
                    Assert.Contains("DEMO_ERROR=ConsoleDemoSlugRequired", output, StringComparison.Ordinal);
                    Assert.DoesNotContain("Which demo?", File.ReadAllText(log), StringComparison.Ordinal);
                }
            } finally {
                // The reported PID and creation time belong to this fixture's child, including a timed-out read.
                if (File.Exists(pid)) {
                    string[] identity = File.ReadAllText(pid).Split('|');
                    try {
                        using Process child = Process.GetProcessById(int.Parse(identity[0], System.Globalization.CultureInfo.InvariantCulture));
                        if (!child.HasExited && child.StartTime.ToUniversalTime().Ticks == long.Parse(identity[1], System.Globalization.CultureInfo.InvariantCulture)) {
                            child.Kill(entireProcessTree: true);
                            child.WaitForExit(5000);
                        }
                    } catch (ArgumentException) { }
                }
                foreach (string file in new[] { log, capture }) {
                    if (File.Exists(file)) { _output.WriteLine(Path.GetFileName(file) + " base64: " + Convert.ToBase64String(File.ReadAllBytes(file))); }
                }
                Directory.Delete(folder, recursive: true);
            }
        }

        private static string Repository([CallerFilePath] string source = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", ".."));
    }
}
