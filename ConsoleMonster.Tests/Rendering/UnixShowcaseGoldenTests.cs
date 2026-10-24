using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Examples.Showcases;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>Real showcase layouts with stable mid-run values at everyday terminal widths.</summary>
    public sealed class UnixShowcaseGoldenTests {
        [Theory]
        [InlineData(100)]
        [InlineData(120)]
        public void StressAndAssessmentPreserveLabelsAndShortValues(int width) {
            Dashboard stress = StressTest.Build();
            stress.ShowClock = false;
            KeyValueList stats = Widgets(stress).OfType<KeyValueList>().Single();
            stats.Add("Updates posted", "12,345").Add("Updates applied", "12,345")
                .Add("Update rate", "1,234/s").Add("Frames", "42").Add("Frame rate", "15.0 fps")
                .Add("CPU", "89% (0.9 cores)").Add("CPU share", "8.9% of 10 cores")
                .Add("Stray writes", "12").Add("Dropped / errors", "0 / 0");
            Dashboard assessment = ParallelRun.Build(out _);
            assessment.ShowClock = false;
            Dictionary<string, ILiveTarget> targets = Widgets(assessment).OfType<ILiveTarget>().GroupBy(target => target.Name).ToDictionary(group => group.Key, group => group.First());
            foreach (LiveEvent update in ParallelRunRecording.Generate().TakeWhile(update => update.Time <= 24000)) {
                if (targets.TryGetValue(update.Target, out ILiveTarget? target)) { target.Apply(update); }
            }
            TerminalProfile profile = TerminalProfile.Fixed(width, 45, ColorDepth.None, GlyphTier.Full).WithCulture(CultureInfo.GetCultureInfo("en-US"));
            string session = Terminal.RenderText(stress, profile, Theme.Dark);
            string checks = Terminal.RenderText(assessment, profile, Theme.Dark);
            Golden.Match("unix.stress." + width + ".txt", session);
            Golden.Match("unix.assessment." + width + ".txt", checks);
            Assert.Contains("Updates posted", session);
            Assert.Contains("Updates applied", session);
            Assert.Contains("Dropped / errors", session);
            Assert.Contains("89% (0.9 cores)", session);
            Assert.Contains("8.9% of 10 cores", session);
            Assert.Contains("Targets failing", checks);
            Assert.Contains("Domain controllers", checks);
            Assert.Contains("Group Policy", checks);
        }

        private static IEnumerable<IWidget> Widgets(IWidget root) {
            yield return root;
            if (root is IContainer container) {
                foreach (IWidget child in container.Children) {
                    foreach (IWidget descendant in Widgets(child)) { yield return descendant; }
                }
            }
        }
    }
}
