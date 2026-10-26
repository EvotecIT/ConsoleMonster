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
        public void LongChainsAndArgumentsWrapWithoutChangingTokens() {
            const string source = "var table = Ui.Table(rows).Column(row => row.Name).Column(row => row.Size).Column(row => row.Count).Expand().Border(BorderStyle.Rounded);";
            string formatted = CSharpRunner.FormatSource(source);
            Assert.All(formatted.Split('\n'), line => Assert.True(line.Length <= 108, line));
            Assert.Contains("\n    .Column", formatted);
            Assert.Equal(SyntaxFactory.ParseCompilationUnit(source).DescendantTokens().Select(token => token.Text),
                SyntaxFactory.ParseCompilationUnit(formatted).DescendantTokens().Select(token => token.Text));
            Assert.Equal(formatted, CSharpRunner.FormatSource(formatted));
        }

        [Theory]
        [InlineData("Row[] rows = [new(\"{0}\", 1)];")]
        [InlineData("var rows = new Row[] {{ new Row(\"{0}\", 1) }};")]
        public void RecordRowsStayOnOneLineInBothArraySyntaxes(string template) {
            string source = string.Format(template, new string('x', 110));
            string formatted = CSharpRunner.FormatSource(source);
            string row = Assert.Single(formatted.Split('\n'), line => line.Contains("new(", StringComparison.Ordinal) || line.Contains("new Row(", StringComparison.Ordinal));
            Assert.Contains("\"" + new string('x', 110) + "\", 1)", row);
            Assert.Equal(formatted, CSharpRunner.FormatSource(formatted));
        }

        [Fact]
        public void MultipleCallbacksAndCollectionItemsPreserveTokensAndComments() {
            const string source = "var widget = Ui.Panel().Configure(first => { first.Text(\"First callback value\"); }, second => { second.Text(\"Second callback value\"); }).Title(\"Build\");\nobject[] Values() => [new(\"one\", 1), /* keep the item note */ new(\"two\", 2), new(\"three\", 3), new(\"four\", 4), new(\"five\", 5)];";
            string formatted = CSharpRunner.FormatSource(source);
            Assert.Contains("keep the item note", formatted);
            Assert.Equal(SyntaxFactory.ParseCompilationUnit(source).DescendantTokens().Select(token => token.Text),
                SyntaxFactory.ParseCompilationUnit(formatted).DescendantTokens().Select(token => token.Text));
            Assert.Equal(formatted, CSharpRunner.FormatSource(formatted));
            string literal = "var panel = Ui.Panel().Title(\"" + new string('x', 140) + "\").Content(c => { c.Text(\"Ready\"); });";
            string wrapped = CSharpRunner.FormatSource(literal);
            Assert.Equal(wrapped, CSharpRunner.FormatSource(wrapped));
            const string filter = "try { } catch (Exception error) when (error != null) { }";
            Assert.Equal(filter + "\n", CSharpRunner.FormatSource(filter));
            Assert.Equal(CSharpRunner.FormatSource(filter), CSharpRunner.FormatSource(CSharpRunner.FormatSource(filter)));
        }

        [Fact]
        public void CatchFiltersRetainCommentAndDirectiveBoundaries() {
            foreach (string source in new[] {
                "try { } catch (Exception e)\n// A recoverable failure.\nwhen (e != null) { }",
                "try { } catch (Exception e) // A recoverable failure.\nwhen (e != null) { }",
                "try { } catch (Exception e)\n#if DEBUG\nwhen (e.Message.Length > 0)\n#else\nwhen (e != null)\n#endif\n{ }"
            }) {
                string formatted = CSharpRunner.FormatSource(source);
                Assert.DoesNotContain(SyntaxFactory.ParseCompilationUnit(formatted).GetDiagnostics(), diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
                Assert.Equal(source + "\n", formatted);
                Assert.Equal(formatted, CSharpRunner.FormatSource(formatted));
            }
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
