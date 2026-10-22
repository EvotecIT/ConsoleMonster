using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using SiteFrames;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>The threads page's guarantees table is generated from Docs/Concurrency-Guarantees.md and cannot drift from it.</summary>
    public sealed class GuaranteesContracts {
        private const string Document = "# Concurrent work\n\nText.\n\n## Guarantees and their tests\n\n" +
            "These tests assert ordering.\nCore tests run on net8.0; PowerShell tests run on 5.1 and 7.\n\n" +
            "| Requirement | Evidence |\n| --- | --- |\n" +
            "| Whole blocks | [`Blocks_StayWhole`](../ConsoleMonster.Tests/Live/BlockTests.cs) |\n" +
            "| One admission | `Admission_IsOnce` in the same file |\n" +
            "| Host lines keep `a|b` | `Lines_Keep` in the same file; [`keeps lines in <Mode>`](../Module/Tests/Lines.Tests.ps1) |\n";

        [Fact]
        public void Parse_TheGuaranteesSection_GivesRowsWithTestsAndRuntimes() {
            List<string> problems = new List<string>();
            (List<string> notes, List<Guarantees.Row> rows) = Guarantees.Parse(Document, file => file.StartsWith("ConsoleMonster.Tests/", StringComparison.Ordinal) ? new[] { ".NET 8" } : null, problems);
            Assert.Empty(problems);
            Assert.Equal(new[] { "These tests assert ordering. Core tests run on net8.0; PowerShell tests run on 5.1 and 7." }, notes);
            Assert.Equal(3, rows.Count);
            Assert.Equal("Whole blocks", rows[0].Guarantee);
            Assert.Equal("<a href=\"https://github.com/EvotecIT/ConsoleMonster/blob/master/ConsoleMonster.Tests/Live/BlockTests.cs\"><code>Blocks_<wbr>Stay<wbr>Whole</code></a>", rows[0].Evidence);
            Assert.Equal(new[] { ".NET 8" }, rows[0].Runtimes);
            Assert.Equal(new[] { ".NET 8" }, rows[1].Runtimes);
            Assert.Equal("Host lines keep <code>a|b</code>", rows[2].Guarantee);
            Assert.Contains("<code>keeps lines in &lt;Mode&gt;</code>", rows[2].Evidence, StringComparison.Ordinal);
            Assert.Equal(new[] { ".NET 8", "PowerShell 5.1 and 7" }, rows[2].Runtimes);
        }

        [Theory]
        [InlineData("# Concurrent work\n\nNo table here.\n")]
        [InlineData("## Guarantees and their tests\n\nText only.\n")]
        [InlineData("## Guarantees and their tests\n\n| Requirement | Evidence |\n| --- | --- |\n| One cell |\n")]
        [InlineData("## Guarantees and their tests\n\n| Requirement | Evidence |\n| --- | --- |\n| Blocks | [`T`](../Nowhere/T.cs) |\n")]
        [InlineData("## Guarantees and their tests\n\nPowerShell tests run on 7.\n\n| Requirement | Evidence |\n| --- | --- |\n| Blocks | `T` with no file |\n")]
        [InlineData("## Guarantees and their tests\n\nCore tests run on net8.0.\n\n| Requirement | Evidence |\n| --- | --- |\n| Lines | [`keeps lines`](../Module/Tests/Lines.Tests.ps1) |\n")]
        public void Parse_AMissingOrBrokenSection_IsAProblem(string markdown) {
            List<string> problems = new List<string>();
            Guarantees.Parse(markdown, file => null, problems);
            Assert.NotEmpty(problems);
        }

        [Fact]
        public void GeneratedTable_MatchesTheRepositoryDocument() {
            string root = AppContext.BaseDirectory;
            while (!File.Exists(Path.Combine(root, "ConsoleMonster.sln"))) {
                root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("The repository root was not found.");
            }
            Site site = new Site(Path.Combine(root, "Website"));
            List<string> problems = new List<string>();
            (List<string> notes, List<Guarantees.Row> rows) = Guarantees.Parse(File.ReadAllText(Path.Combine(root, "Docs", "Concurrency-Guarantees.md")), file => Guarantees.Runtimes(site, file), problems);
            Assert.Empty(problems);
            JsonNode data = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "Website", "data", "guarantees.json")))!;
            Assert.Equal(notes, data["notes"]!.AsArray().Select(n => n!.GetValue<string>()));
            Assert.Equal(
                rows.Select(r => r.Guarantee + "\n" + r.Evidence + "\n" + string.Join("|", r.Runtimes)),
                data["rows"]!.AsArray().Select(r => r!["guarantee"]!.GetValue<string>() + "\n" + r["evidence"]!.GetValue<string>() + "\n" + string.Join("|", r["runtimes"]!.AsArray().Select(x => x!.GetValue<string>()))));
        }
    }
}
