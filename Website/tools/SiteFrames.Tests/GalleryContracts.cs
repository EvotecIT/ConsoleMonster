using System;
using System.IO;
using System.Linq;
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
    public sealed class GalleryContracts {
        private static Site Repository() {
            DirectoryInfo? root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "ConsoleMonster.sln"))) root = root.Parent;
            return new Site(Path.Combine(root!.FullName, "Website"));
        }

        [Theory]
        [InlineData("live-filter", "\"sql\" Escape", "sql-02", "web-01")]
        [InlineData("fleet", "\"missing\" Escape", "No matching hosts", "web-01")]
        [InlineData("toolbox", "Down Enter Escape", "12 jobs waiting", "CPU 24%")]
        [InlineData("setup", "Tab Tab Tab Enter Enter Escape", "Preview ready", "Choose a writable folder")]
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
        [InlineData("stress", "Updates", "2080")]
        [InlineData("pipeline", "Package", "complete")]
        [InlineData("log-tail", "Batch complete", "5 / 5")]
        [InlineData("parallel-patch", "5 passed", "1 retry needed")]
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
