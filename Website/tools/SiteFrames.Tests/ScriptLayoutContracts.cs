using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Example PowerShell keeps lines within 100 columns, laid out by build/Format-ExampleScript.ps1.</summary>
    public sealed class ScriptLayoutContracts {
        [Fact]
        public void ExampleScriptsAreLaidOut() {
            string website = WebsiteRoot();
            string examples = Path.Combine(website, "examples");
            string[] scripts = Directory.GetFiles(examples, "*.ps1", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            string list = Path.Combine(Path.GetTempPath(), "cm-script-layout-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllLines(list, scripts);
            try {
                // -Check also proves the layout settles and changes nothing but whitespace or splatting.
                string command = "Get-Content -LiteralPath '" + list.Replace("'", "''") + "' | & '" + Path.Combine(website, "build", "Format-ExampleScript.ps1").Replace("'", "''") + "' -Check";
                var (exit, output) = Run(command);
                Assert.True(exit == 0, output);
            } finally {
                File.Delete(list);
            }
        }

        [Fact]
        public void ScriptLinesOverTheWidthAreStringsOrComments() {
            string website = WebsiteRoot();
            string examples = Path.Combine(website, "examples").Replace("'", "''");
            // A token that cannot break (a string, a here-string or a comment) on every line over 100 columns.
            string command =
                "Get-ChildItem -LiteralPath '" + examples + "' -Filter *.ps1 -Recurse | ForEach-Object {\n" +
                "    $relative = [IO.Path]::GetRelativePath('" + examples + "', $_.FullName).Replace('\\', '/')\n" +
                "    $tokens = $null; $errors = $null\n" +
                "    $null = [System.Management.Automation.Language.Parser]::ParseFile($_.FullName, [ref] $tokens, [ref] $errors)\n" +
                "    $lines = [IO.File]::ReadAllLines($_.FullName)\n" +
                "    for ($i = 0; $i -lt $lines.Count; $i++) {\n" +
                "        if ($lines[$i].Length -le 100) { continue }\n" +
                "        $n = $i + 1\n" +
                "        $ok = $tokens | Where-Object { $_.Extent.StartLineNumber -le $n -and $_.Extent.EndLineNumber -ge $n -and $_.Kind -in 'StringLiteral', 'StringExpandable', 'HereStringLiteral', 'HereStringExpandable', 'Comment' }\n" +
                "        if (-not $ok) { \"${relative}:${n}: $($lines[$i])\" }\n" +
                "    }\n" +
                "}\n";
            var (exit, output) = Run(command);
            Assert.True(exit == 0 && output.Trim().Length == 0, output);
        }

        private static (int Exit, string Output) Run(string command) {
            ProcessStartInfo info = new ProcessStartInfo("pwsh") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            info.ArgumentList.Add("-NoProfile");
            info.ArgumentList.Add("-Command");
            info.ArgumentList.Add(command);
            using Process process = Process.Start(info)!;
            System.Threading.Tasks.Task<string> output = process.StandardOutput.ReadToEndAsync();
            System.Threading.Tasks.Task<string> errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(600000)) {
                process.Kill(entireProcessTree: true);
                Assert.Fail("The script layout check did not finish within ten minutes.");
            }
            process.WaitForExit();
            return (process.ExitCode, output.Result + errors.Result);
        }

        private static string WebsiteRoot() {
            DirectoryInfo? repository = new DirectoryInfo(AppContext.BaseDirectory);
            while (repository != null && !File.Exists(Path.Combine(repository.FullName, "ConsoleMonster.sln"))) { repository = repository.Parent; }
            Assert.NotNull(repository);
            return Path.Combine(repository!.FullName, "Website");
        }
    }
}
