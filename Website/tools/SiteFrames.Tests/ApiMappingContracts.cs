using System;
using System.IO;
using System.Linq;
using SiteFrames;
using SiteFrames.Capture;
using SiteFrames.Pages;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>The translation notes name actual configured API symbols, using the same map as the parity gate.</summary>
    public sealed class ApiMappingContracts : IDisposable {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "cm-api-map-" + Guid.NewGuid().ToString("N"));
        public ApiMappingContracts() => Directory.CreateDirectory(_folder);
        public void Dispose() => Directory.Delete(_folder, true);

        [Fact]
        public void AliasesInitializersAndNestedChainsResolveToTheSamePanelProperties() {
            string initializer = Source("initializer", "using P = ConsoleMonster.Layout.Panel;\nusing ConsoleMonster.Widgets;\nvar panel = new P { Title = \"Build\", Content = new TextBlock(\"Ready\") };\n");
            string fluent = Source("fluent", "using ConsoleMonster.Layout;\nvar panel = new Panel().Title(\"Build\").Content(c => c.Text(\"Ready\"));\n");
            foreach (string path in new[] { initializer, fluent }) {
                var members = CSharpRunner.ConfiguredMembers(path);
                Assert.Contains("ConsoleMonster.Layout.Panel.Title", members);
                Assert.Contains("ConsoleMonster.Layout.Panel.Content", members);
            }
            var rows = ApiMappingNotes.For(Repository(), initializer, fluent);
            var title = Assert.Single(rows, row => row!["memberId"]!.GetValue<string>() == "ConsoleMonster.Layout.Panel.Title")!;
            Assert.Equal("New-ConsolePanel -Title …", title["powershell"]!.GetValue<string>());
            Assert.Equal("Title = …", title["csharp"]!.GetValue<string>());
            Assert.Equal(".Title(…)", title["fluent"]!.GetValue<string>());
        }

        [Fact]
        public void AnUnrelatedTitlePropertyCannotProduceAWidgetTranslation() {
            string source = Source("unrelated", "var panel = new LocalPanel { Title = \"Build\" };\nsealed class LocalPanel { public string? Title { get; set; } }\n");
            Assert.Empty(CSharpRunner.ConfiguredMembers(source));
            Assert.Throws<InvalidDataException>(() => ApiMappingNotes.For(Repository(), source, source));
        }

        [Fact]
        public void EveryGalleryDemoHasNotesForItsOwnResolvedWidgetMembers() {
            Site site = Repository();
            foreach (var demo in GalleryPage.Read(site)) {
                var notes = ApiMappingNotes.For(site, demo.CSharp, demo.CSharpFluent);
                Assert.InRange(notes.Count, 1, 6);
                var members = CSharpRunner.ConfiguredMembers(demo.CSharp)
                    .Concat(CSharpRunner.ConfiguredMembers(demo.CSharpFluent)).ToHashSet(StringComparer.Ordinal);
                foreach (var row in notes) { Assert.Contains(row!["memberId"]!.GetValue<string>(), members); }
            }
        }

        private string Source(string name, string text) {
            string path = Path.Combine(_folder, name + ".cs");
            File.WriteAllText(path, text);
            return path;
        }

        private static Site Repository() {
            DirectoryInfo? root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "ConsoleMonster.sln"))) { root = root.Parent; }
            return new Site(Path.Combine(root!.FullName, "Website"));
        }
    }
}
