using System;
using System.Globalization;
using System.IO;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Testing {
    /// <summary>The public test helpers, the activity list fed by Write-Progress, and the culture scope.</summary>
    public sealed class TestingApiTests {
        [Fact]
        public void RenderText_IsFixedSizeTierAndCulture() {
            Panel panel = new Panel(new TextBlock("hello"), "Box");
            Assert.Equal("╭─ Box ──────╮\n│ hello      │\n╰────────────╯", TestTerminal.RenderText(panel, 14));
            Assert.Equal("+- Box ------+\n| hello      |\n+------------+", TestTerminal.RenderText(panel, 14, glyphs: GlyphTier.Ascii));
            Assert.Equal(5, TestTerminal.Render(panel, 14, 5).Height);
        }

        [Fact]
        public void Interactive_DrivesAPromptIntoACapture() {
            ScreenCapture capture = new ScreenCapture(40, 8, inline: true);
            bool answer = new ConfirmPrompt("Continue?", false).Show(TestTerminal.Interactive("y", capture));
            Assert.True(answer);
            Assert.NotNull(capture.FindFrame("Continue?"));
        }

        [Fact]
        public void GoldenFile_CreatesThenComparesWithAReadableDiff() {
            string folder = Path.Combine(Path.GetTempPath(), "cm-golden-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            string path = Path.Combine(folder, "sample.txt");
            try {
                GoldenFileMismatchException created = Assert.Throws<GoldenFileMismatchException>(() => GoldenFile.Match(path, "one\ntwo\nthree"));
                Assert.Contains("was created", created.Message);
                GoldenFile.Match(path, "one\r\ntwo\r\nthree");
                GoldenFileMismatchException diff = Assert.Throws<GoldenFileMismatchException>(() => GoldenFile.Match(path, "one\ntwo!\nthree"));
                Assert.Contains("at line 2, column 4", diff.Message);
                Assert.Contains("  - two\n", diff.Message);
                Assert.Contains("  + two!\n", diff.Message);
                Assert.Contains("CONSOLEMONSTER_UPDATE_GOLDEN=1", diff.Message);
                Assert.Equal(path, diff.Path);
            } finally {
                if (Directory.Exists(folder)) {
                    Directory.Delete(folder, true);
                }
            }
        }

        [Fact]
        public void ActivityList_NestsChildrenAndForgetsCompletedActivities() {
            ActivityList list = new ActivityList();
            Assert.Equal(0, TestTerminal.Render(list, 60).Height);
            list.Apply(new LiveEvent(ActivityList.ProgressTarget, "update", key: "1", text: "Backup", status: "File 3 of 10", value: 30, total: 42));
            list.Apply(new LiveEvent(ActivityList.ProgressTarget, "update", key: "2/1", text: "Compressing", status: "chunk 1", value: 50, total: -1));
            list.Apply(new LiveEvent(ActivityList.ProgressTarget, "update", key: "3", text: "Waiting", value: -1, total: -1));
            Golden.Match("live/activities.txt", TestTerminal.RenderText(list, 70));
            list.Apply(new LiveEvent(ActivityList.ProgressTarget, "complete", key: "1"));
            Assert.Equal(1, list.Count);
            Assert.Contains("Waiting", TestTerminal.RenderText(list, 70));
        }

        [Fact]
        public void CultureScope_RendersItsChildWithTheCulture() {
            IWidget probe = new CultureProbe();
            Assert.Equal("pl-PL 1,5", TestTerminal.RenderText(new CultureScope(probe, new CultureInfo("pl-PL")), 20));
            Assert.Equal("en-US 1.5", TestTerminal.RenderText(new CultureScope(probe, new CultureInfo("en-US")), 20, culture: new CultureInfo("pl-PL")));
        }

        private sealed class CultureProbe : IWidget {
            public Size Measure(Constraints constraints, RenderContext context) => new Size(constraints.MaxWidth, 1);

            public void Render(Region region, RenderContext context) {
                region.Write(0, 0, context.Culture.Name + " " + 1.5.ToString(context.Culture), Styling.Style.Plain);
            }
        }
    }
}
