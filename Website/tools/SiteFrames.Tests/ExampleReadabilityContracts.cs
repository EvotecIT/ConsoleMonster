using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using SiteFrames.Capture;
using SiteFrames.Pages;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Copyable source stays readable without changing the compiled program or its translation aid.</summary>
    [Collection("Interactive process defaults")]
    public sealed class ExampleReadabilityContracts {
        [Fact]
        public void PublishedExamplesUsePositionalArguments() {
            string folder = Path.Combine(Repository().Website, "examples");
            var named = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Replace('\\', '/').Contains("/bin/", StringComparison.Ordinal)
                    && !path.Replace('\\', '/').Contains("/obj/", StringComparison.Ordinal))
                .SelectMany(path => SyntaxFactory.ParseCompilationUnit(File.ReadAllText(path)).DescendantNodes()
                    .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ArgumentSyntax>()
                    .Where(argument => argument.NameColon != null)
                    .Select(argument => Path.GetRelativePath(folder, path) + ": " + argument.ToString()));
            Assert.Empty(named);
        }

        [Fact]
        public void MainConstructorsAndChartKindsFollowTheProgramRatherThanTheMappingFile() {
            Site site = Repository();
            var backup = GalleryPage.Read(site).Single(demo => demo.Slug == "backup-runner");
            var backupRows = ApiMappingNotes.ForPair(site, backup.CSharp, backup.CSharpFluent, backup.PowerShell);
            Assert.StartsWith("New-ConsoleTable", backupRows[0]!["powershell"]!.GetValue<string>());
            var ordered = backupRows.Select(row => row!["powershell"]!.GetValue<string>()).ToArray();
            Assert.True(Array.IndexOf(ordered, "New-ConsoleRows") < Array.IndexOf(ordered, "New-ConsoleRows -Content { … }"));
            Assert.True(Array.IndexOf(ordered, "New-ConsoleTable -Expand") < Array.IndexOf(ordered, "New-ConsoleRows -Content { … }"));
            Assert.True(Array.IndexOf(ordered, "New-ConsoleRows -Content { … }") < Array.IndexOf(ordered, "New-ConsoleText -Markup $text"));
            var process = GalleryPage.Read(site).Single(demo => demo.Slug == "process-monitor");
            var chart = Assert.Single(ApiMappingNotes.ForPair(site, process.CSharp, process.CSharpFluent, process.PowerShell),
                row => row!["memberId"]!.GetValue<string>() == "M:ConsoleMonster.Ui.Chart")!;
            Assert.Equal("New-ConsoleChart -Type Sparkline", chart["powershell"]!.GetValue<string>());
            Assert.Equal("Ui.Chart().Type(ChartType.Sparkline)", chart["fluent"]!.GetValue<string>());
            Assert.Equal("new Sparkline(…)", chart["csharp"]!.GetValue<string>());
        }

        private static Site Repository() {
            DirectoryInfo? root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "ConsoleMonster.sln"))) { root = root.Parent; }
            return new Site(Path.Combine(root!.FullName, "Website"));
        }
    }
}
