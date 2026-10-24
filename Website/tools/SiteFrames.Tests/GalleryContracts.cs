using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using SiteFrames.Capture;
using SiteFrames.Pages;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Exercises the published single-file gallery programs through their actual app and capture paths.</summary>
    [Collection("Interactive process defaults")]
    public sealed class GalleryContracts {
        private static Site Repository() {
            DirectoryInfo? root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "ConsoleMonster.sln"))) root = root.Parent;
            return new Site(Path.Combine(root!.FullName, "Website"));
        }

        [Fact]
        public void PublishedDetailPagesMatchTheCatalogueMetadata() {
            Site site = Repository();
            GalleryPage.Demo[] demos = GalleryPage.Read(site);
            GalleryPage.ValidateDetailPages(site, demos);
            foreach (GalleryPage.Demo demo in demos) {
                string[] lines = File.ReadAllLines(Path.Combine(site.Website, "content", "pages", "demos", demo.Slug + ".md"));
                Assert.Equal(8, lines.Length);
                Assert.Equal("title: " + demo.Title, lines[1]);
                Assert.Equal(demo.Metadata["pitch"]!.GetValue<string>(),
                    JsonNode.Parse(lines[2].Substring("description: ".Length))!.GetValue<string>());
                Assert.Equal("slug: demos/" + demo.Slug, lines[3]);
                Assert.Equal("layout: demo", lines[4]);
                Assert.Equal("demo: " + demo.Slug, lines[5]);
                Assert.Equal("meta.seo_title: " + demo.Title + " demo | ConsoleMonster", lines[6]);
            }
        }

        [Theory]
        [InlineData("missing")]
        [InlineData("orphaned")]
        [InlineData("stale")]
        public void DetailPageValidationRejectsBrokenCatalogueLinks(string failure) {
            string root = Path.Combine(Path.GetTempPath(), "cm-gallery-pages-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "Website"));
            File.WriteAllText(Path.Combine(root, "ConsoleMonster.sln"), string.Empty);
            try {
                Site site = new Site(Path.Combine(root, "Website"));
                GalleryPage.Demo demo = GalleryPage.Read(Repository()).First();
                demo.Metadata["pitch"] = "Read \"quoted values\" and \\paths without changing the description.";
                GalleryPage.Demo[] demos = new[] { demo };
                GalleryPage.WriteDetailPages(site, demos);
                string folder = Path.Combine(site.Website, "content", "pages", "demos");
                string page = Path.Combine(folder, demo.Slug + ".md");
                string description = File.ReadAllLines(page)[2].Substring("description: ".Length);
                Assert.Equal(demo.Metadata["pitch"]!.GetValue<string>(), JsonNode.Parse(description)!.GetValue<string>());
                if (failure == "missing") File.Delete(page);
                else if (failure == "orphaned") File.WriteAllText(Path.Combine(folder, "unknown-demo.md"), "---\nlayout: demo\n---\n");
                else File.AppendAllText(page, "outdated content\n");
                Assert.Throws<InvalidDataException>(() => GalleryPage.ValidateDetailPages(site, demos));
            } finally {
                Site.RemoveTemporary(root);
            }
        }

        [Fact]
        public void GallerySourcesAndDownloadsPreserveEveryCanonicalByte() {
            Site site = Repository();
            JsonArray entries = JsonNode.Parse(File.ReadAllText(site.Data("gallery")))!["demos"]!.AsArray();
            Assert.Equal(GalleryPage.Read(site).Length, entries.Count);
            Assert.InRange(entries.Count(entry => entry!["featured"]!.GetValue<bool>()), 2, 3);
            Assert.True(File.Exists(site.Static("site/nav/demos-dark.svg")));
            foreach (JsonNode entry in entries.Cast<JsonNode>()) {
                string slug = entry["id"]!.GetValue<string>();
                Assert.True(File.Exists(site.Static("site/nav/demos/" + slug + "-dark.svg")), slug);
                string tier = entry["tier"]!.GetValue<string>();
                Assert.Contains(tier, new[] { "starter", "showcase" });
                Assert.StartsWith("Use it when ", entry["useCase"]!.GetValue<string>());
                if (entry["featured"]!.GetValue<bool>()) Assert.Equal("showcase", tier);
                foreach (string language in new[] { "powershell", "csharp" }) {
                    JsonNode source = entry["source"]![language]!;
                    string file = language == "powershell" ? slug + ".ps1" : "Program.cs";
                    string repo = "Website/examples/gallery/" + slug + "/" + file;
                    Assert.Equal(file, source["file"]!.GetValue<string>());
                    Assert.Equal(repo, source["repo"]!.GetValue<string>());
                    byte[] canonical = File.ReadAllBytes(Path.Combine(site.Website, "..", repo));
                    string text = source["text"]!.GetValue<string>();
                    Assert.False(text.StartsWith('\uFEFF'), slug);
                    Assert.Equal(File.ReadAllText(Path.Combine(site.Website, "..", repo)), text);
                    if (language == "powershell") Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, canonical.Take(3));
                    Assert.Equal(canonical, File.ReadAllBytes(site.Static("examples/gallery/" + slug + "/" + file)));
                    int lines = File.ReadAllLines(Path.Combine(site.Website, "..", repo)).Length;
                    Assert.Equal(lines, entry["lines"]![language]!.GetValue<int>());
                    Assert.InRange(lines, 1, tier == "showcase" ? 450 : 250);
                }
                if (entry["mode"]!.GetValue<string>() == "static") {
                    Assert.Null(entry["replay"]);
                } else {
                    JsonNode replay = entry["replay"]!;
                    string replayFile = site.Static(replay["url"]!.GetValue<string>().TrimStart('/'));
                    JsonNode player = JsonNode.Parse(File.ReadAllText(replayFile))!;
                    Assert.Equal(player["width"]!.GetValue<int>(), replay["w"]!.GetValue<int>());
                    Assert.Equal(player["height"]!.GetValue<int>(), replay["h"]!.GetValue<int>());
                    Assert.True(JsonNode.DeepEquals(entry["frame"], replay["poster"]));
                    Assert.InRange(replay["start"]!.GetValue<long>(), 0, player["duration"]!.GetValue<long>());
                }
            }
        }

        [Theory]
        [InlineData("live-filter", "\"sql\" Escape", "sql-02", "web-01")]
        [InlineData("fleet-filter", "\"missing\" Escape", "No matching hosts", "web-01")]
        [InlineData("task-menu", "Down Enter Escape", "12 jobs waiting", "CPU 24%")]
        [InlineData("setup-basic", "Tab Tab Tab Enter Enter Escape", "Ready to install", "Choose a writable folder")]
        public void ScriptedKeysReachThePublishedAppsResult(string slug, string keys, string expected, string absent) {
            Site site = Repository();
            GalleryPage.Demo demo = GalleryPage.Read(site).Single(d => d.Slug == slug);
            ScreenCapture capture = new ScreenCapture(demo.Width, demo.Height);
            using ScriptedInput input = new ScriptedInput(keys) { Paced = true };
            var profile = CSharpRunner.Profile(demo.Width, demo.Height, ColorDepth.TrueColor, GlyphTier.Full);
            CSharpRunner.Execute(demo.CSharp, profile, Theme.Dark, new InteractiveOptions {
                Input = input, Capture = capture, Mode = InteractionMode.Rich, Profile = profile, Theme = Theme.Dark
            }, new[] { "--interactive" });
            Assert.NotNull(capture.LastFrame);
            string result = ConsoleExporter.ToText(capture.LastFrame!);
            Assert.Contains(expected, result, StringComparison.Ordinal);
            Assert.DoesNotContain(absent, result, StringComparison.Ordinal);
            Assert.True(capture.Frames.Count > 1);
        }

        [Theory]
        [InlineData("worker-progress", "Updates", "2080")]
        [InlineData("build-steps", "Package", "complete")]
        [InlineData("log-tail", "Batch complete", "5 / 5")]
        [InlineData("patch-progress", "5 passed", "1 retry needed")]
        [InlineData("backup-runner", "4 passed", "1 recovered on retry")]
        [InlineData("process-monitor", "CPU 66%", "1768 MiB")]
        public void LiveDriversFinishAndRetainTheirResult(string slug, string expected, string detail) {
            GalleryPage.Demo demo = GalleryPage.Read(Repository()).Single(d => d.Slug == slug);
            var profile = CSharpRunner.Profile(demo.Width, demo.Height, ColorDepth.TrueColor, GlyphTier.Full);
            var target = CSharpRunner.Execute(demo.CSharp, profile, Theme.Dark, null, new[] { "--interactive" });
            string text = Parity.Describe(Parity.Transcript(System.Text.Encoding.UTF8.GetBytes(target.Output), demo.Width, demo.Height));
            Assert.Contains(expected, text, StringComparison.Ordinal);
            Assert.Contains(detail, text, StringComparison.Ordinal);
        }
    }
}
