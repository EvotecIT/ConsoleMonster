using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Examples;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    public sealed class LargeRunGoldenTests {
        public static IEnumerable<object[]> Frames() {
            foreach (int width in new[] { 120, 60 }) foreach (int height in new[] { 24, 50 })
            foreach (bool light in new[] { false, true }) foreach (string state in new[] { "mid-run", "short-terminal", "ci", "final-summary" }) {
                yield return new object[] { width, height, light, state };
            }
        }
        [Theory] [MemberData(nameof(Frames))]
        public void TerminalStatesMatchGoldenFrames(int width, int height, bool light, string state) {
            CellBuffer frame = LargeRunScene.Frame(width, height, light ? Theme.Light : Theme.Dark, state);
            Assert.Equal(width, frame.Width); Assert.Equal(height, frame.Height);
            Golden.Match("large-run/" + state + "-" + width + "x" + height + "-" + (light ? "light" : "dark") + ".txt", frame.ToPlainText().Replace("\r\n", "\n").TrimEnd('\n') + "\n");
            if (state == "mid-run") { Assert.Contains("Fleet checks", frame.ToPlainText()); Assert.Contains("Problems", frame.ToPlainText()); }
        }
        [Theory] [InlineData(60)] [InlineData(120)]
        public void FinalSummaryKeepsAllProblemsAndFullDetailsOutsideTheViewport(int width) {
            RunTrackerResult(width);
        }
        private static void RunTrackerResult(int width) {
            var tracker = LargeRunScene.Tracker(true);
            var profile = TerminalProfile.Fixed(width, 24, ColorDepth.None, GlyphTier.Ascii);
            string full = FrameComposer.RenderBlock(LargeRunScene.Summary(tracker), width, Terminal.CreateContext(profile)).ToPlainText();
            foreach (var item in tracker.Snapshot().Items) {
                if (item.Status != StatusKind.Fail && item.Status != StatusKind.Warn) { continue; }
                Assert.Contains(item.Item.Label, full);
            }
            Assert.Contains("reports/fleet-checks.html", full);
            Assert.Contains("deadline", full); Assert.Contains("retry", full);
        }
    }
}
