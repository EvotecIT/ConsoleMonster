using System;
using System.Linq;
using SiteFrames;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>The rules that keep the code a page shows equal to the code that ran: region splitting and the code guard.</summary>
    public sealed class ShownCodeContracts {
        [Fact]
        public void Split_MoreRegion_KeepsFirstLineInShortFormAndAllInFull() {
            (string shortForm, string full) = SourceSnippets.Split("$a = 1\n#region more\n$b = 2\n$c = 3\n#endregion\nWrite-Output $a");
            Assert.Equal("$a = 1\n$b = 2\n# … 1 more line in the full program\nWrite-Output $a", shortForm);
            Assert.Equal("$a = 1\n$b = 2\n$c = 3\nWrite-Output $a", full);
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
        public void FrameSources_FrameWithoutCode_IsReported(string json, int expected) {
            Assert.Equal(expected, FrameSources.Find(System.Text.Json.Nodes.JsonNode.Parse(json), "data").Count());
        }

        [Fact]
        public void FrameSources_HiddenScript_MustBeShownElsewhereOnThePage() {
            System.Text.Json.Nodes.JsonObject figures = System.Text.Json.Nodes.JsonNode.Parse("{\"a\":{\"script\":\"Get-A\"},\"b\":{\"script\":\"Get-A\"},\"c\":{\"script\":\"Get-C\"}}")!.AsObject();
            Assert.Empty(FrameSources.FindOnPage("{{< cm-frame id=\"a\" >}}\n{{< cm-frame id=\"b\" script=\"false\" >}}", figures, "p.md"));
            Assert.Empty(FrameSources.FindOnPage("```powershell\nGet-C\n```\n{{< cm-frame id=\"c\" script=\"false\" >}}", figures, "p.md"));
            Assert.Single(FrameSources.FindOnPage("{{< cm-frame id=\"c\" script=\"false\" >}}", figures, "p.md"));
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
