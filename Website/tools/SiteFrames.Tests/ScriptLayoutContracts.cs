using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Example PowerShell keeps lines within 100 columns, laid out by build/Format-ExampleScript.ps1.</summary>
    public sealed class ScriptLayoutContracts {
        // Captured by Windows PowerShell 5.1 or in a Windows pseudo console, whose captures are stamped with these scripts;
        // they are laid out with the next Windows capture of their areas.
        private static readonly string[] WindowsCaptured = {
            "demos/errors.ps1", "demos/fixtures/MailDomain.Tests.ps1", "demos/progress.ps1", "demos/record.ps1", "demos/session.ps1",
            "demos/shaping.ps1", "demos/threads-dashboard.ps1", "docs/pipeline-charts.ps1", "docs/profile.ps1", "home/controllers.ps1",
            "interactive/grid.ps1", "interactive/multi.ps1", "interactive/select.ps1", "interactive/text.ps1", "interactive/typed.ps1",
            "powershell/format-consolelist.ps1", "powershell/format-consoletable.ps1", "powershell/format-consoletree.ps1",
            "powershell/format-list.ps1", "powershell/format-table.ps1", "powershell/job.ps1", "powershell/write-consoletext.ps1",
            "tails/file.ps1", "widgets/align.ps1", "widgets/columns.ps1", "widgets/histogram.ps1", "widgets/log.ps1", "widgets/panel.ps1",
            "widgets/sparkline.ps1", "widgets/spinner.ps1", "widgets/stack.ps1", "widgets/tree.ps1"
        };

        [Fact]
        public void ExampleScriptsAreLaidOut() {
            string website = WebsiteRoot();
            string examples = Path.Combine(website, "examples");
            string[] scripts = Directory.GetFiles(examples, "*.ps1", SearchOption.AllDirectories)
                .Where(path => !WindowsCaptured.Contains(Path.GetRelativePath(examples, path).Replace('\\', '/'), StringComparer.Ordinal))
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
            string skip = string.Join(",", WindowsCaptured.Select(path => "'" + path + "'"));
            // A token that cannot break (a string, a here-string or a comment) on every line over 100 columns.
            string command =
                "$skip = @(" + skip + ")\n" +
                "Get-ChildItem -LiteralPath '" + examples + "' -Filter *.ps1 -Recurse | ForEach-Object {\n" +
                "    $relative = [IO.Path]::GetRelativePath('" + examples + "', $_.FullName).Replace('\\', '/')\n" +
                "    if ($skip -contains $relative) { return }\n" +
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
