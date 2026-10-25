using System;
using System.IO;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames;
using SiteFrames.Capture;
using SiteFrames.Pages;
using System.Text.Json.Nodes;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Generated tabs remain runnable translations and preserve observable initializer behavior.</summary>
    [Collection("Interactive process defaults")]
    public sealed class FluentSourceContracts : IDisposable {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "cm-fluent-source-" + Guid.NewGuid().ToString("N"));
        public FluentSourceContracts() => Directory.CreateDirectory(_folder);
        public void Dispose() => Directory.Delete(_folder, true);

        [Fact]
        public void ImplicitCreationAndNestedInitializersBecomeCompilableChainsWithTheSameOutput() {
            string path = Source("using ConsoleMonster;\nusing ConsoleMonster.Layout;\nusing ConsoleMonster.Widgets;\nPanel panel = new() { Title = \"Build\", Content = new TextBlock(\"Passed\") };\nTerminal.Write(panel);\n");
            string fluent = CSharpRunner.FluentSource(path);
            Assert.Contains(".Title(\"Build\")", fluent, StringComparison.Ordinal);
            Assert.Contains(".Content(", fluent, StringComparison.Ordinal);
            Assert.Contains("panel.Show()", fluent, StringComparison.Ordinal);
            CSharpRunner.Check(path, fluent);
            Assert.Equal(CSharpRunner.Execute(path, CSharpRunner.Profile(60, 20, ColorDepth.TrueColor, GlyphTier.Full), Theme.Dark, null).Output,
                CSharpRunner.Execute(path, CSharpRunner.Profile(60, 20, ColorDepth.TrueColor, GlyphTier.Full), Theme.Dark, null, fluent: true).Output);
        }

        [Fact]
        public void MixedInitializersKeepCallbackAndPropertyOrder() {
            string path = Source("using ConsoleMonster.Interactive;\nvar item = new MenuItem(\"Build\") { Detail = () => \"Ready\", Action = _ => { }, Description = \"Run\" };\n");
            string fluent = CSharpRunner.FluentSource(path);
            Assert.Contains("Detail = () => \"Ready\"", fluent, StringComparison.Ordinal);
            Assert.Contains("Action = _ =>", fluent, StringComparison.Ordinal);
            Assert.DoesNotContain(".Detail(\"Ready\")", fluent, StringComparison.Ordinal);
            CSharpRunner.Check(path, fluent);
        }

        [Fact]
        public void WidgetProgramsHaveCompilableFluentTabs() {
            string website = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            foreach (string path in Directory.GetFiles(Path.Combine(website, "examples", "widgets"), "*.cs")) {
                CSharpRunner.Check(path, CSharpRunner.FluentSource(path));
            }
        }

        [Fact]
        public void StandardProgramsHaveCompiledTranslationsAndResolvedMappingNotes() {
            string website = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            Site site = new Site(website);
            foreach (string folder in new[] { "widgets", "home", "tables", "docs", "errors", "themes", "interactive", "app", "tails", "recording", "migration" }) {
                foreach (string path in Directory.GetFiles(Path.Combine(website, "examples", folder), "*.cs")) {
                    var entry = SourceStyles.Add(site, new JsonObject(), path);
                    Assert.False(string.IsNullOrWhiteSpace(entry["csharpFluent"]!.GetValue<string>()));
                    Assert.NotEmpty(entry["apiMap"]!.AsArray());
                }
            }
        }

        [Fact]
        public void HostedPowerShellExamplesCompileAgainstTheirActualModuleOwnersInBothCSharpStyles() {
            string website = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            Site site = new Site(website);
            foreach (string path in Directory.GetFiles(Path.Combine(website, "examples", "demos"), "*.host.cs")) {
                CSharpRunner.Check(path, Site.ReadText(path));
                Assert.StartsWith("Invoke-", CSharpRunner.HostCommand(path), StringComparison.Ordinal);
                var entry = SourceStyles.Add(site, new JsonObject(), path);
                Assert.NotEmpty(entry["apiMap"]!.AsArray());
            }
        }

        [Fact]
        public void MultiFileExamplesPublishCompiledFluentCompanions() {
            string main = Source("using ConsoleMonster;\nTerminal.Write(Factory.Build());\n");
            string companion = Source("using ConsoleMonster.Layout;\nusing ConsoleMonster.Widgets;\nstatic class Factory { public static Panel Build() => new Panel { Title = \"Build\", Content = new TextBlock(\"Passed\") }; }\n");
            var sources = CSharpRunner.FluentSources(main, companion);
            CSharpRunner.CheckSources(main, sources, companion);
            Assert.Contains(".Title(\"Build\")", sources[companion], StringComparison.Ordinal);
            Assert.DoesNotContain("Title =", sources[companion], StringComparison.Ordinal);
        }

        [Fact]
        public void StandaloneDemosAndTheFleetProgramHaveCompiledThreeStyleSources() {
            string website = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            Site site = new Site(website);
            foreach (string file in new[] { "watch", "errors", "login", "parallel", "threads-naive", "threads-live", "record", "session" }) {
                string path = Path.Combine(website, "examples", "demos", file + ".cs");
                CSharpRunner.Check(path, Site.ReadText(path));
                SourceStyles.Add(site, new JsonObject(), path);
            }
            SourceStyles.Add(site, new JsonObject(), Path.Combine(website, "examples", "demos", "fleet.cs"),
                Path.Combine(site.Repository, "ConsoleMonster.Examples", "Showcases", "FleetHealth.cs"),
                Path.Combine(site.Repository, "ConsoleMonster.Examples", "Showcases", "FleetData.cs"));
        }

        [Fact]
        public void IntegrationProjectsCompileBothStylesAndDocumentEveryAddonOption() {
            string website = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            Site site = new Site(website);
            var map = JsonNode.Parse(File.ReadAllText(Path.Combine(site.Repository, "Tests", "Api", "round2-parity.json")))!;
            foreach (var (folder, projectName) in new[] { ("hosting", "Orders"), ("logging", "Shop") }) {
                string source = Path.Combine(website, "examples", "demos", folder, "Program.cs");
                string project = Path.Combine(website, "examples", "demos", folder, projectName + ".csproj");
                SourceStyles.AddProject(site, new JsonObject(), source, project);
                foreach (var option in map["IntegrationOptions"]!.AsArray()) {
                    if (option!["Package"]!.GetValue<string>() != (folder == "hosting" ? "ConsoleMonster.Hosting" : "ConsoleMonster.Extensions.Logging")) { continue; }
                    var actual = CSharpRunner.ProjectOptionMembers(source, project, option["CSharpType"]!.GetValue<string>());
                    Assert.Equal(option["Members"]!.AsArray().Select(member => member!.GetValue<string>()).OrderBy(name => name, StringComparer.Ordinal), actual);
                    Assert.False(string.IsNullOrWhiteSpace(option["Reason"]!.GetValue<string>()));
                }
            }
            SourceStyles.AddProject(site, new JsonObject(),
                Path.Combine(site.Repository, "Templates", "ConsoleMonster.Templates", "content", "ConsoleMonsterApp", "Program.cs"),
                Path.Combine(website, "tools", "TemplateDemo", "TemplateDemo.csproj"));
        }

        private string Source(string text) {
            string path = Path.Combine(_folder, Guid.NewGuid().ToString("N") + ".cs");
            File.WriteAllText(path, text);
            return path;
        }
    }
}
