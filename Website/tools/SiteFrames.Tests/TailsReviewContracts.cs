using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using SiteFrames.Pages;
using Xunit;

namespace SiteFrames.Tests {
    public sealed class TailsReviewContracts {
        [Theory]
        [InlineData(60, false)]
        [InlineData(120, false)]
        [InlineData(60, true)]
        [InlineData(120, true)]
        public void ReviewExportsAppliedCounterSourceDaysAndOverrideAsRealSvgAndText(int width, bool light) {
            var frames = TailsPage.ReviewStates(width, light ? Theme.Light : Theme.Dark);
            Assert.Equal(3, frames.Count);
            Assert.Contains("⏸ Paused · viewing history · 2 new lines", ConsoleExporter.ToText(frames["paused-new-lines"]));
            string days = ConsoleExporter.ToText(frames["source-days"]);
            Assert.Contains("2026-10-22 23:59:59", days);
            Assert.Contains("2026-10-23 00:00:00", days);
            Assert.DoesNotContain("new lines", days);
            string folder = Path.Combine(Path.GetTempPath(), "cm-tails-review-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try {
                TailsPage.WriteReviewFrame(folder, "override", frames["timestamp-override"], light ? ExportTheme.Light : ExportTheme.Dark);
                string text = File.ReadAllText(Path.Combine(folder, "override.txt"));
                Assert.Contains("2026-10-23 00:00:00 +00:00", text);
                Assert.Contains("q quit · PgUp history · End follow", text);
                Assert.EndsWith("End follow\n", text.Replace("\r\n", "\n"));
                Assert.DoesNotContain("0 history lines discarded", text);
                XDocument svg = XDocument.Load(Path.Combine(folder, "override.svg"));
                Assert.Equal("svg", svg.Root!.Name.LocalName);
                // SVG omits blank cells and draws styled words separately; the text export above checks exact spacing.
                Assert.Contains("databaseconnected", string.Concat(svg.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value)).Replace(" ", string.Empty));
                Assert.Contains("connected", File.ReadAllText(Path.Combine(folder, "override.html")));
            } finally {
                foreach (string file in Directory.GetFiles(folder)) { File.Delete(file); }
                Directory.Delete(folder);
            }
        }
    }
}
