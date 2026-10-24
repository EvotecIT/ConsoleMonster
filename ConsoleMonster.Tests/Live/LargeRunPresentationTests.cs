using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Examples;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    public sealed class LargeRunPresentationTests {
        [Theory]
        [InlineData(60, ColorDepth.TrueColor, OutputMode.Interactive)]
        [InlineData(120, ColorDepth.TrueColor, OutputMode.Interactive)]
        [InlineData(60, ColorDepth.None, OutputMode.Interactive)]
        [InlineData(60, ColorDepth.TrueColor, OutputMode.Plain)]
        public void CompletionColumnsKeepDurationAtTheRightAndDetailDim(int width, ColorDepth depth, OutputMode mode) {
            var tracker = new RunTracker(new[] { new RunItem("Health check", "node-12"), new RunItem("Disk check", "node-13") });
            tracker.FinishItem(0, StatusKind.Pass, duration: TimeSpan.FromSeconds(1.2));
            tracker.FinishItem(1, StatusKind.Fail, "A response was not returned before the configured deadline and must be retried", TimeSpan.FromSeconds(4.2));
            var context = new RenderContext(TerminalProfile.Fixed(width, 24, depth, mode: mode), Theme.Dark);
            var frames = tracker.Snapshot().Items.Select(item => FrameComposer.RenderBlock(new RunCompletion(item), width, context)).ToArray();
            string pass = frames[0].ToPlainText().TrimEnd();
            string fail = frames[1].ToPlainText().TrimEnd();
            Assert.EndsWith("1.2s", pass); Assert.EndsWith("4.2s", fail);
            Assert.Equal(width - 4, pass.IndexOf("1.2s", StringComparison.Ordinal));
            Assert.Equal(width - 4, fail.IndexOf("4.2s", StringComparison.Ordinal));
            Assert.Equal(pass.IndexOf("node-12", StringComparison.Ordinal), fail.IndexOf("node-13", StringComparison.Ordinal));
            Assert.Contains("Health check", pass); Assert.Contains("Disk check", fail);
            Assert.DoesNotContain("@", fail); Assert.DoesNotContain("(", fail);
            if (depth == ColorDepth.None || mode == OutputMode.Plain) { Assert.Contains("OK", pass); Assert.Contains("ERR", fail); }
            else {
                Assert.DoesNotContain("OK", pass); Assert.DoesNotContain("ERR", fail);
                Assert.Contains("…", fail);
                Assert.Equal(context.Style(ThemeToken.Muted), frames[1][width - 1, 0].Style);
                Assert.Equal(context.Style(ThemeToken.Muted), frames[1][fail.IndexOf("node-13", StringComparison.Ordinal), 0].Style);
                Assert.Equal(context.Style(ThemeToken.Fail), frames[1][2, 0].Style);
            }
        }

        [Fact]
        public void TallDashboardUsesGroupRowsAndOneCompactHeaderBelowItsRule() {
            var frame = LargeRunScene.Frame(120, 50, Theme.Dark);
            string[] rows = frame.ToPlainText().Split('\n');
            int title = Array.FindIndex(rows, row => row.StartsWith("Fleet checks", StringComparison.Ordinal));
            Assert.Contains("━━", rows[title]); Assert.Contains("64%", rows[title]); Assert.Contains("137/214", rows[title]);
            Assert.StartsWith("Elapsed", rows[title + 1]); Assert.Contains("125 passed", rows[title + 2]);
            Assert.Equal(new string('─', 120), rows[title - 1]);
            foreach (string group in new[] { "Identity", "Network", "Storage", "Services" }) { Assert.Contains(rows, row => row.StartsWith(group + " ", StringComparison.Ordinal) && row.Contains("/5")); }
            Assert.DoesNotContain("4 categories", frame.ToPlainText());
            var style = frame[0, title].Style;
            Assert.True((style.Decoration & TextDecoration.Bold) != 0);
            Assert.Equal(new RenderContext(TerminalProfile.Fixed(120, 50), Theme.Dark).Style(ThemeToken.Muted).Foreground, style.Foreground);
        }

        [Fact]
        public void TrendWaitsForFiveSamplesAndSteadyThroughputStaysAtTheFloor() {
            TimeSpan clock = TimeSpan.Zero;
            var tracker = new RunTracker(Enumerable.Range(0, 40).Select(i => new RunItem("check " + i)), () => clock);
            var dashboard = new RunDashboard(tracker) { ShowTrend = true };
            var context = new RenderContext(TerminalProfile.Fixed(120, 50), Theme.Dark);
            for (int i = 0; i < 4; i++) { clock = TimeSpan.FromSeconds(i + 1); tracker.FinishItem(i, StatusKind.Pass); tracker.Snapshot(); }
            Assert.DoesNotContain("▁", FrameComposer.Compose(dashboard, 120, 20, context).ToPlainText());
            for (int i = 4; i < 36; i++) { clock = TimeSpan.FromSeconds(i + 1); tracker.FinishItem(i, StatusKind.Pass); tracker.Snapshot(); }
            string text = FrameComposer.Compose(dashboard, 120, 20, context).ToPlainText();
            Assert.Contains(new string('▁', 12), text); Assert.DoesNotContain("▇", text);
        }

        [Theory]
        [InlineData(60, false, 1)]
        [InlineData(120, false, 1)]
        [InlineData(60, true, 1)]
        [InlineData(120, true, 1)]
        [InlineData(60, false, 3)]
        [InlineData(120, false, 3)]
        [InlineData(60, true, 3)]
        [InlineData(120, true, 3)]
        [InlineData(60, false, 5)]
        [InlineData(120, false, 5)]
        [InlineData(60, true, 5)]
        [InlineData(120, true, 5)]
        [InlineData(60, false, 20)]
        [InlineData(120, false, 20)]
        [InlineData(60, true, 20)]
        [InlineData(120, true, 20)]
        public void DashboardCountersDimZeroAndColourPositiveValues(int width, bool light, int height) {
            var states = new[] { StatusKind.Pass, StatusKind.Fail, StatusKind.Warn, StatusKind.Skip };
            var tokens = new[] { ThemeToken.Pass, ThemeToken.Fail, ThemeToken.Warn, ThemeToken.Skip };
            var tracker = new RunTracker(states.Select(state => new RunItem(state.ToString(), group: "Checks")));
            var dashboard = new RunDashboard(tracker);
            var context = new RenderContext(TerminalProfile.Fixed(width, 50), light ? Theme.Light : Theme.Dark);
            for (int count = 0; count <= 1; count++) {
                if (count > 0) { for (int i = 0; i < states.Length; i++) { tracker.FinishItem(i, states[i]); } }
                CellBuffer frame = FrameComposer.Compose(dashboard, width, height, context);
                string[] rows = frame.ToPlainText().Split('\n');
                int header = Array.FindIndex(rows, row => row.Contains(" fail"));
                int group = Array.FindIndex(rows, row => row.StartsWith("Checks ", StringComparison.Ordinal));
                Assert.True(header >= 0);
                if (height >= 7) { Assert.True(group >= 0); }
                for (int i = 0; i < states.Length; i++) {
                    if (height <= 3 && states[i] != StatusKind.Fail && states[i] != StatusKind.Warn) { continue; }
                    var style = context.Style(count == 0 ? ThemeToken.Muted : tokens[i]);
                    string glyph = context.Glyphs.StatusSymbol(states[i]);
                    string headerCount = height >= 7 ? glyph + " " + count : count + " " + (states[i] == StatusKind.Warn ? "warn" : states[i].ToString().ToLowerInvariant());
                    int x = rows[header].IndexOf(headerCount, StringComparison.Ordinal);
                    Assert.True(x >= 0);
                    for (int cell = x; cell < x + headerCount.Length; cell++) { Assert.Equal(style, frame[cell, header].Style); }
                    if (height < 7 || states[i] == StatusKind.Skip) { continue; }
                    string groupCount = glyph + count;
                    x = rows[group].IndexOf(groupCount, StringComparison.Ordinal);
                    Assert.True(x >= 0);
                    for (int cell = x; cell < x + groupCount.Length; cell++) { Assert.Equal(style, frame[cell, group].Style); }
                }
            }
        }

        [Fact]
        public void ActivityQueueAndProblemLabelsUseTheSameTargetSeparator() {
            var tracker = new RunTracker(new[] { new RunItem("running check", "node-10"), new RunItem("queued check", "node-11"), new RunItem("failed check", "node-12") });
            tracker.StartItem(0); tracker.FinishItem(2, StatusKind.Fail, "Timed out");
            string text = FrameComposer.Compose(new RunDashboard(tracker), 120, 20, new RenderContext(TerminalProfile.Fixed(120, 50), Theme.Dark)).ToPlainText();
            Assert.Contains("running check · node-10", text);
            Assert.Contains("queued check · node-11", text);
            Assert.Contains("failed check · node-12", text);
            Assert.DoesNotContain("@", text);
            Assert.Equal("untargeted", new RunItem("untargeted").DisplayName);
        }

        [Theory]
        [InlineData(60)]
        [InlineData(120)]
        public void SummaryAlignsTitlesAndOrdersProblemsBySeverityThenCompletion(int width) {
            var tracker = new RunTracker(new[] { new RunItem("second failure", group: "Network"), new RunItem("first failure", group: "Network"), new RunItem("warning item", group: "Network") });
            tracker.FinishItem(2, StatusKind.Warn, "Warning detail");
            tracker.FinishItem(1, StatusKind.Fail, "First failure detail");
            tracker.FinishItem(0, StatusKind.Fail, "Second failure detail");
            string text = FrameComposer.RenderBlock(new RunSummary(tracker), width, new RenderContext(TerminalProfile.Fixed(width, 50), Theme.Dark)).ToPlainText();
            Assert.Contains("\nGroups\n", text); Assert.Contains("\nProblems\n", text);
            Assert.True(text.IndexOf("first failure", StringComparison.Ordinal) < text.IndexOf("second failure", StringComparison.Ordinal));
            Assert.True(text.IndexOf("second failure", StringComparison.Ordinal) < text.IndexOf("warning item", StringComparison.Ordinal));
            string first = text.Split('\n').First(row => row.Contains("first failure"));
            if (first.StartsWith("│", StringComparison.Ordinal)) { Assert.Contains("First", first); }
        }

        [Fact]
        public void CiProgressStartsAtColumnZero() {
            string[] rows = LargeRunScene.Frame(60, 24, Theme.Dark, "ci").ToPlainText().Split('\n');
            Assert.StartsWith("progress 137/214", rows[23]);
            Assert.Contains("ETA", rows[23]); Assert.Contains("1m 37s", rows[23]);
        }
    }
}
