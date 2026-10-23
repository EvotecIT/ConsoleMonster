using System;
using System.Linq;
using SiteFrames;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>The rules that keep the code a page shows equal to the code that ran: region splitting and the code guard.</summary>
    public sealed class ShownCodeContracts {
        [Fact]
        public void Split_MoreRegion_KeepsFirstAndLastLineInShortFormAndAllInFull() {
            (string shortForm, string full) = SourceSnippets.Split("$a = 1\n#region more\n$b = 2\n$c = 3\n$d = 4\n$e = 5\n#endregion\nWrite-Output $a");
            Assert.Equal("$a = 1\n$b = 2\n# … 2 more lines in the full program\n$e = 5\nWrite-Output $a", shortForm);
            Assert.Equal("$a = 1\n$b = 2\n$c = 3\n$d = 4\n$e = 5\nWrite-Output $a", full);
        }

        [Fact]
        public void Split_FoldOfOneLine_ShowsTheLine() {
            (string shortForm, string full) = SourceSnippets.Split("#region more\n$b = 2\n$c = 3\n$d = 4\n#endregion");
            Assert.Equal(full, shortForm);
        }

        [Fact]
        public void Split_SeveralStatements_ShowEachOpeningWithItsClose() {
            (string shortForm, string _) = SourceSnippets.Split(
                "#region more\n$hosts = @(\n    'WEB01'\n    'WEB02'\n    'SQL01'\n)\n$match = {\n    param($State)\n    $hosts | Where-Object { $_ -like $State }\n    'done'\n}\n#endregion\nShow-It");
            Assert.Equal("$hosts = @(\n    # … 3 more lines in the full program\n)\n$match = {\n    # … 3 more lines in the full program\n}\nShow-It", shortForm);
        }

        [Fact]
        public void Split_ContinuedOrCommentedLastLine_IsFolded() {
            (string csharpShort, string _) = SourceSnippets.Split(
                "// <more>\nvoid Build() {\n    Run();\n    Wait();\n}\n\nstatic int Worst(int a) =>\n    a > 1 ? 2\n    : 0;\n// </more>");
            Assert.Equal("void Build() {\n    // … 2 more lines in the full program\n}\n// … 4 more lines in the full program", csharpShort);
            (string powershellShort, string _) = SourceSnippets.Split("#region more\n$a = 1\n$b = 2\n# A sentence that goes on\n# to the next line.\n#endregion");
            Assert.Equal("$a = 1\n# … 3 more lines in the full program", powershellShort);
        }

        [Fact]
        public void Split_EveryExample_CountsOnlyTheLinesItHides() {
            System.IO.DirectoryInfo? repository = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (repository != null && !System.IO.File.Exists(System.IO.Path.Combine(repository.FullName, "ConsoleMonster.sln"))) { repository = repository.Parent; }
            Assert.NotNull(repository);
            System.Text.RegularExpressions.Regex fold = new System.Text.RegularExpressions.Regex(@"^\s*(?:#|//) … (\d+) more lines? in the full program$");
            int checkedFiles = 0;
            foreach (string path in System.IO.Directory.EnumerateFiles(System.IO.Path.Combine(repository!.FullName, "Website", "examples"), "*.*", System.IO.SearchOption.AllDirectories)) {
                if (!path.EndsWith(".ps1", StringComparison.Ordinal) && !path.EndsWith(".cs", StringComparison.Ordinal)) {
                    continue;
                }
                string code = System.IO.File.ReadAllText(path);
                if (code.Contains("#region hidden", StringComparison.OrdinalIgnoreCase) || code.Contains("// <hidden>", StringComparison.Ordinal)) {
                    continue;
                }
                (string shortForm, string full) = SourceSnippets.Split(code);
                int shown = 0;
                foreach (string line in shortForm.Split('\n')) {
                    System.Text.RegularExpressions.Match match = fold.Match(line);
                    shown += match.Success ? int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 1;
                }
                Assert.True(full.Split('\n').Length == shown, path + ": the short form accounts for " + shown + " lines, the full program has " + full.Split('\n').Length);
                checkedFiles++;
            }
            Assert.True(checkedFiles > 20);
        }

        [Fact]
        public void Split_BracketsInStringsAndComments_AreNotCounted() {
            (string shortForm, string _) = SourceSnippets.Split("#region more\n$a = '(' + \"{ `\" [\" # )\n$b = 2\n$c = 3\n$d = 4\n#endregion");
            Assert.Equal("$a = '(' + \"{ `\" [\" # )\n# … 2 more lines in the full program\n$d = 4", shortForm);
        }

        [Fact]
        public void Split_MoreRegion_OpeningAStatement_ShowsWhereItCloses() {
            (string shortForm, string _) = SourceSnippets.Split("#region more\n$servers = foreach ($server in @(\n    'DC01'\n    'DC02'\n    'DC03'\n)) {\n    $server\n}\n#endregion\n$servers");
            Assert.Equal("$servers = foreach ($server in @(\n    # … 5 more lines in the full program\n}\n$servers", shortForm);
        }

        [Fact]
        public void Split_CSharpMoreBlock_ShowsItsClosingBraceAndCountsOnlyHiddenLines() {
            (string shortForm, string _) = SourceSnippets.Split("var rows = new[] {\n    // <more>\n    new Row(1),\n    new Row(2),\n    new Row(3),\n    new Row(4),\n    // </more>\n};");
            Assert.Equal("var rows = new[] {\n    new Row(1),\n    // … 2 more lines in the full program\n    new Row(4),\n};", shortForm);
        }

        [Fact]
        public void Split_TwoLineRegion_IsShownWhole() {
            (string shortForm, string full) = SourceSnippets.Split("#region more\n$a = @(\n)\n#endregion\n$a");
            Assert.Equal("$a = @(\n)\n$a", shortForm);
            Assert.Equal(full, shortForm);
        }

        [Fact]
        public void Split_TrailingBlankLines_StayAfterTheLastCodeLine() {
            (string shortForm, string _) = SourceSnippets.Split("#region more\n$a = 1\n$b = 2\n$c = 3\n$e = 5\n\n#endregion\n$d = 4");
            Assert.Equal("$a = 1\n# … 2 more lines in the full program\n$e = 5\n\n$d = 4", shortForm);
        }

        [Fact]
        public void Split_RegionMarkers_IgnoreCase() {
            (string _, string full) = SourceSnippets.Split("#Region more\n$x = 1\n#EndRegion\n$z = 3");
            Assert.Equal("$x = 1\n$z = 3", full);
        }

        [Fact]
        public void Split_NestedRegion_IsKeptAsWritten() {
            (string _, string full) = SourceSnippets.Split("#region more\n#region inner\n$x = 1\n#endregion\n$y = 2\n#endregion\n$z = 3");
            Assert.Equal("#region inner\n$x = 1\n#endregion\n$y = 2\n$z = 3", full);
        }

        [Fact]
        public void Split_LabelledEndRegion_ClosesTheBlock() {
            (string _, string full) = SourceSnippets.Split("#region more\n$x = 1\n#endregion more\n$z = 3");
            Assert.Equal("$x = 1\n$z = 3", full);
        }

        [Theory]
        [InlineData("#region more\n$x = 1")]
        [InlineData("// <more>\nint x = 1;")]
        public void Split_UnclosedBlock_Throws(string code) {
            Assert.Throws<InvalidOperationException>(() => SourceSnippets.Split(code));
        }

        [Theory]
        [InlineData("#region harness\nImport-Module ./x.psd1\n#endregion")]
        [InlineData("// <harness>\nvar x = 1;\n// </harness>")]
        [InlineData("#region more\n#region harness\n$x\n#endregion\n#endregion")]
        public void Split_HarnessRegion_Throws(string code) {
            Assert.Throws<InvalidOperationException>(() => SourceSnippets.Split(code));
        }

        [Theory]
        [InlineData("& ../../../Module/Examples/Example-LoginOverview.ps1 -Sample")]
        [InlineData(". ../x.ps1")]
        [InlineData("Import-Module (Join-Path $PSScriptRoot '..\\ConsoleMonster.psd1')")]
        [InlineData("Join-Path $PSScriptRoot '..'")]
        [InlineData("Import-Module \"$PSScriptRoot\\..\\ConsoleMonster.psd1\" -Force")]
        [InlineData("Import-Module ../ConsoleMonster.psd1")]
        [InlineData(". .\\artifacts.ps1")]
        [InlineData("// Website/tools/SiteFrames/Catalog/PageExamples.cs")]
        [InlineData("dotnet run --project ConsoleMonster.Examples/ConsoleMonster.Examples.csproj")]
        [InlineData("Get-Content Module/Examples/Example-Tables.ps1")]
        [InlineData("& ./Module/Examples/X.ps1")]
        [InlineData("Import-Module .\\Module\\ConsoleMonster.psd1")]
        [InlineData("Split-Path $PSScriptRoot -Parent")]
        [InlineData("Push-Location ..")]
        [InlineData("Split-Path -Parent $PSScriptRoot")]
        [InlineData("Split-Path $PSScriptRoot")]
        [InlineData("Split-Path (Split-Path $PSCommandPath)")]
        [InlineData("Join-Path -Path $r -ChildPath '..'")]
        [InlineData("cd ..")]
        [InlineData("Get-Item C:/Support/GitHub/ConsoleMonster/x")]
        [InlineData("Path = 'C:\\Support\\GitHub\\ConsoleMonster\\Artefacts\\a.zip'")]
        public void Guard_RepositoryReferences_AreFound(string code) {
            Assert.NotEmpty(CodeGuard.Find(code));
        }

        [Theory]
        [InlineData("& ./Example-LoginOverview.ps1 -Sample")]
        [InlineData("1..5 | ForEach-Object { $_ }")]
        [InlineData("Invoke-WebRequest https://consolemonster.dev/examples/Example-LoginOverview.ps1 -OutFile x.ps1")]
        [InlineData("Import-Module ConsoleMonster")]
        [InlineData("$here = $PSScriptRoot")]
        [InlineData("dotnet add package ConsoleMonster")]
        [InlineData("using ConsoleMonster.Widgets;")]
        [InlineData("Write-Host \"$PSScriptRoot...\"")]
        [InlineData("<# .SYNOPSIS #>")]
        [InlineData("Split-Path $PSScriptRoot -Leaf")]
        public void Guard_ReaderCode_Passes(string code) {
            Assert.Empty(CodeGuard.Find(code));
        }

        [Theory]
        [InlineData("using ConsoleMonster;\n\nTerminal.Write(x);", true)]
        [InlineData("// A comment first.\n\nusing ConsoleMonster;\nTerminal.Write(x);", true)]
        [InlineData("global using ConsoleMonster;\nTerminal.Write(x);", true)]
        [InlineData("#nullable enable\nusing ConsoleMonster;\nTerminal.Write(x);", true)]
        [InlineData("/* A program\n   in two lines. */\nusing ConsoleMonster;\nTerminal.Write(x);", true)]
        [InlineData("using Cm = ConsoleMonster.Terminal;\nCm.Write(x);", true)]
        [InlineData("using static ConsoleMonster.Ui;\nWrite(x);", true)]
        [InlineData("Terminal.Write(x);", false)]
        [InlineData("using (logger.BeginScope(\"x\")) {\n}", false)]
        [InlineData("using ILoggerFactory factory = LoggerFactory.Create(b => b.AddConsoleMonster());", false)]
        public void Guard_IsProgram_KnowsCompleteProgramsFromSnippets(string code, bool program) {
            Assert.Equal(program, CodeGuard.IsProgram(code));
        }

        [Theory]
        [InlineData("{\"a\":{\"frame\":{\"dark\":\"x\"}}}", 1)]
        [InlineData("{\"a\":{\"script\":\"Write-ConsoleRule\",\"frame\":{\"dark\":\"x\"}}}", 0)]
        [InlineData("{\"a\":{\"csharp\":\"x\",\"hosts\":[{\"frame\":{\"dark\":\"x\"}}]}}", 0)]
        [InlineData("{\"a\":{\"programRepo\":\"P/Program.cs\",\"frame\":{\"dark\":\"x\"}}}", 0)]
        [InlineData("{\"a\":{\"source\":\"x\"},\"b\":{\"sourceOf\":\"a\",\"poster\":{\"dark\":\"x\"}}}", 0)]
        [InlineData("{\"a\":{},\"b\":{\"sourceOf\":\"a\",\"poster\":{\"dark\":\"x\"}}}", 1)]
        [InlineData("{\"a\":{\"script\":\"\",\"frame\":{\"dark\":\"x\"}}}", 1)]
        [InlineData("{\"a\":{\"csharp\":{\"url\":\"/r.json\"},\"frame\":{\"dark\":\"x\"}}}", 1)]
        [InlineData("{\"a\":{\"source\":{\"csharp\":\"x\"},\"frame\":{\"dark\":\"x\"}}}", 0)]
        [InlineData("{\"a\":{\"frame\":{\"title\":\"Text\",\"body\":\"not a frame\"}}}", 0)]
        [InlineData("{\"a\":{\"code\":[{\"lang\":\"bash\",\"text\":\"dotnet run\"}],\"frame\":{\"dark\":\"x\"}}}", 0)]
        [InlineData("{\"frame\":{\"dark\":\"x\"}}", 1)]
        public void FrameSources_FrameWithoutCode_IsReported(string json, int expected) {
            Assert.Equal(expected, FrameSources.Find(System.Text.Json.Nodes.JsonNode.Parse(json), "data").Count());
        }

        [Fact]
        public void FrameSources_HiddenScript_MustBeShownElsewhereOnThePage() {
            System.Text.Json.Nodes.JsonObject figures = System.Text.Json.Nodes.JsonNode.Parse("{\"a\":{\"script\":\"Get-A\"},\"b\":{\"script\":\"Get-A\"},\"c\":{\"script\":\"Get-C\"}}")!.AsObject();
            Assert.Empty(FrameSources.FindOnPage("{{< cm-frame id=\"a\" >}}\n{{< cm-frame id=\"b\" script=\"false\" >}}", figures, "p.md"));
            Assert.Empty(FrameSources.FindOnPage("```powershell\nGet-C\n```\n{{< cm-frame id=\"c\" script=\"false\" >}}", figures, "p.md"));
            Assert.Single(FrameSources.FindOnPage("{{< cm-frame id=\"c\" script=\"false\" >}}", figures, "p.md"));
            Assert.Single(FrameSources.FindOnPage("{{< cm-frame id=\"c\" script=\"false\" caption=\"`6>&1` keeps lines\" >}}", figures, "p.md"));
            Assert.Empty(FrameSources.FindOnPage("1. Step:\n\n   ```powershell\n   Get-C\n   ```\n{{< cm-frame id=\"c\" script=\"false\" >}}", figures, "p.md"));
        }

        [Fact]
        public void PowerShellSyntax_ParseErrors_AreReportedWithTheirPageLine() {
            System.Collections.Generic.List<string> problems = SiteFrames.Capture.PowerShellSyntax.Check(new[] {
                ("ok.md", 3, "Import-Module ConsoleMonster\nGet-Service | Format-ConsoleTable"),
                ("broken.md", 10, "Import-Module ConsoleMonster\nif ($x { 'missing paren' }")
            });
            Assert.NotEmpty(problems);
            Assert.All(problems, problem => Assert.StartsWith("broken.md:11:", problem, StringComparison.Ordinal));
        }

        [Fact]
        public void PowerShellSyntax_SyntaxOnlyPowerShell7Knows_IsReportedForWindowsPowerShell() {
            if (!System.IO.File.Exists(System.IO.Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"))) {
                return;
            }
            string problem = Assert.Single(SiteFrames.Capture.PowerShellSyntax.Check(new[] { ("seven.md", 1, "$name = $env:NAME ?? 'world'") }));
            Assert.Contains("Windows PowerShell 5.1 does not parse", problem, StringComparison.Ordinal);
            Assert.DoesNotContain("PowerShell 7 and", problem, StringComparison.Ordinal);
        }
        [Fact]
        public void CopySnippets_FindsPowerShellInstallStepsAndRunLines() {
            System.Collections.Generic.List<(string, int, string)> found = new System.Collections.Generic.List<(string, int, string)>();
            CodeGuard.CopySnippets(System.Text.Json.Nodes.JsonNode.Parse(
                "{\"a\":{\"use\":{\"install\":\"Install-Module X\"}},\"b\":{\"use\":{\"lang\":\"bash\",\"install\":\"dotnet run\"}}," +
                "\"c\":{\"steps\":[{\"text\":\"Prose, not code.\",\"code\":[{\"lang\":\"powershell\",\"text\":\"./x.ps1\"},{\"lang\":\"bash\",\"text\":\"ls\"}]}]}," +
                "\"d\":{\"run\":\"pwsh -File ./x.ps1\"}}"), "copy.json", found);
            Assert.Equal(new[] { "Install-Module X", "./x.ps1", "pwsh -File ./x.ps1" }, found.Select(f => f.Item3).ToArray());
        }

        [Fact]
        public void Guard_Fences_ReadEveryCodeBlock() {
            (int Line, string Code)[] fences = CodeGuard.Fences("text\n```powershell\nGet-Thing\n```\nmore\n```\nx\ny\n```").ToArray();
            Assert.Equal(2, fences.Length);
            Assert.Equal((3, "Get-Thing"), fences[0]);
            Assert.Equal("x\ny", fences[1].Code);
        }
    }
}
