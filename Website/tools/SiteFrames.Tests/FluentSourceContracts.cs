using System;
using System.IO;
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
            foreach (string folder in new[] { "widgets", "home", "tables", "docs", "errors", "themes", "interactive", "app", "tails", "recording" }) {
                foreach (string path in Directory.GetFiles(Path.Combine(website, "examples", folder), "*.cs")) {
                    var entry = SourceStyles.Add(site, new JsonObject(), path);
                    Assert.False(string.IsNullOrWhiteSpace(entry["csharpFluent"]!.GetValue<string>()));
                    Assert.NotEmpty(entry["apiMap"]!.AsArray());
                }
            }
        }

        private string Source(string text) {
            string path = Path.Combine(_folder, Guid.NewGuid().ToString("N") + ".cs");
            File.WriteAllText(path, text);
            return path;
        }
    }
}
