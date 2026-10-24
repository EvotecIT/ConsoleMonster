using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Examples.Showcases;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>
    /// The assessment dashboard mid-run at the widths a user drags through (wide, 100, 80, 60) and below its minimum size
    /// (compact status line plus priority widgets), and a sweep of sizes that must never throw.
    /// </summary>
    public sealed class ResizeLayoutTests {
        private static Dashboard MidRun(int untilMilliseconds = 24000) {
            Dashboard dashboard = ParallelRun.Build(out _);
            Dictionary<string, ILiveTarget> targets = new Dictionary<string, ILiveTarget>(StringComparer.Ordinal);
            Discover(dashboard, targets);
            foreach (LiveEvent liveEvent in ParallelRunRecording.Generate()) {
                if (liveEvent.Time > untilMilliseconds) {
                    break;
                }
                if (targets.TryGetValue(liveEvent.WidgetId, out ILiveTarget? target)) {
                    target.Apply(liveEvent);
                }
            }
            return dashboard;
        }

        private static void Discover(IWidget widget, Dictionary<string, ILiveTarget> targets) {
            if (widget is ILiveTarget target && !targets.ContainsKey(target.WidgetId)) {
                targets[target.WidgetId] = target;
            }
            if (widget is IContainer container) {
                foreach (IWidget child in container.Children) {
                    Discover(child, targets);
                }
            }
        }

        private static string Compose(IWidget root, int width, int height, GlyphTier tier = GlyphTier.Full) {
            RenderContext context = new RenderContext(TerminalProfile.Fixed(width, height, ColorDepth.None, tier), Theme.Dark, tick: 3, elapsed: TimeSpan.FromSeconds(24));
            return TableShowcaseGoldenTests.WithInvariantCulture(() => FrameComposer.Compose(root, width, height, context).ToPlainText());
        }

        [Theory]
        [InlineData(140, 45)]
        [InlineData(100, 40)]
        [InlineData(80, 36)]
        [InlineData(60, 30)]
        [InlineData(50, 12)]
        [InlineData(30, 6)]
        [InlineData(12, 2)]
        public void Dashboard_EachWindowSize_MatchesGolden(int width, int height) {
            string text = Compose(MidRun(), width, height);
            // The frame always has exactly one line per row; the golden stores it without trailing empty rows (and with
            // one final newline) so the fixture stays clean under `git diff --check`.
            Assert.Equal(height, text.Split('\n').Length);
            Golden.Match("resize." + width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture) + ".txt", text.TrimEnd('\n') + "\n");
        }

        [Fact]
        public void Dashboard_TooSmall_ShowsTheCompactStatusLine() {
            string text = Compose(MidRun(), 79, 12);
            Assert.StartsWith("Nightly checks", text);
            Assert.Contains("window too small: 79\u00D712", text);
            Assert.Contains("needs 60\u00D720", Compose(MidRun(), 50, 12));
        }

        [Fact]
        public void Dashboard_SizeSweep_NeverThrowsOrWritesOutsideTheWindow() {
            Dashboard dashboard = MidRun();
            foreach (GlyphTier tier in new[] { GlyphTier.Full, GlyphTier.Ascii }) {
                for (int width = 1; width <= 150; width += 7) {
                    for (int height = 1; height <= 50; height += 3) {
                        string text = Compose(dashboard, width, height, tier);
                        string[] lines = text.Split('\n');
                        Assert.True(lines.Length <= height, width + "x" + height);
                    }
                }
            }
        }
    }
}
