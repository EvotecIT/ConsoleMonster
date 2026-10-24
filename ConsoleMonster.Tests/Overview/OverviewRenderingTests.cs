using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Overview;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Overview {
    public sealed class OverviewRenderingTests {
        [Theory]
        [InlineData(GlyphTier.Full, " · ")]
        [InlineData(GlyphTier.Ascii, " | ")]
        public void MissingUptimeDoesNotLeaveAnEmptyHeaderSegment(GlyphTier glyphs, string separator) {
            OverviewSnapshot snapshot = new OverviewSnapshot("demo-host", "Example OS 1.0", null,
                new DateTimeOffset(2026, 10, 22, 12, 34, 0, TimeSpan.FromHours(2)), Array.Empty<OverviewSection>());
            string text = Terminal.RenderText(new OverviewWidget(snapshot), TerminalProfile.Fixed(120, 24, ColorDepth.None, glyphs));
            Assert.Equal("demo-host" + separator + "Example OS 1.0" + separator + "12:34 +02:00", text.Split('\n')[0].TrimEnd());
        }

        [Theory]
        [InlineData(120, false)]
        [InlineData(120, true)]
        [InlineData(60, false)]
        [InlineData(60, true)]
        public void SyntheticOverviewMatchesGoldenAndPreservesObservations(int width, bool light) {
            TerminalProfile profile = TerminalProfile.Fixed(width, 40, ColorDepth.TrueColor, GlyphTier.Full);
            string text = Terminal.RenderText(new OverviewWidget(Scene()), profile, light ? Theme.Light : Theme.Dark);
            Golden.Match("overview." + width + "." + (light ? "light" : "dark") + ".txt", text);
            string encoded = Terminal.RenderEncoded(new OverviewWidget(Scene()), profile, light ? Theme.Light : Theme.Dark);
            Golden.Match("overview." + width + "." + (light ? "light" : "dark") + ".vt.txt", encoded.Replace("\u001b", "\\e"));
            string[] lines = text.TrimEnd('\n').Split('\n');
            if (width >= 80) { Assert.InRange(lines.Length, 1, 24); }
            Assert.All(lines, line => Assert.InRange(DisplayWidth.Measure(line), 0, width));
            Assert.Contains("demo-host", text);
            Assert.Contains("up 3d 04:05", text);
            Assert.Contains("12:34 +02:00", text);
            Assert.Contains("CPU", text);
            Assert.Contains("Memory", text);
            Assert.Contains("12 / 32 GiB", text);
            Assert.Contains("82 / 100 GiB", text);
            Assert.Contains("91 / 100 GiB", text);
            Assert.Contains("8 logical CPUs", text);
            Assert.Contains("Reboot required", text);
            Assert.Contains("Security", text);
            Assert.DoesNotContain("N/A", text);
            Assert.Matches(@"/data\s+[^%]+82% high", text);
            Assert.Matches(@"/archive\s+[^%]+91% critical", text);
            Assert.Contains("demo-user", text);
            Assert.Contains("test-user", text);
            Assert.Contains("sample-user", text);
            Assert.DoesNotContain("more", text);
        }

        [Theory]
        [InlineData(79, false)]
        [InlineData(80, true)]
        public void PanelsReflowAtEightyColumns(int width, bool sideBySide) {
            string text = Terminal.RenderText(new OverviewWidget(Scene()), TerminalProfile.Fixed(width, 40, ColorDepth.None, GlyphTier.Ascii));
            Assert.Equal(sideBySide, text.Split('\n').Any(line => line.Contains("CPU / memory") && line.Contains("Disks")));
        }

        [Theory]
        [InlineData(120, ColorDepth.TrueColor, GlyphTier.Full)]
        [InlineData(60, ColorDepth.TrueColor, GlyphTier.Full)]
        [InlineData(60, ColorDepth.Ansi16, GlyphTier.Basic)]
        [InlineData(60, ColorDepth.None, GlyphTier.Ascii)]
        public void PanelBarsShareCellColumnsAcrossLabelWidths(int width, ColorDepth depth, GlyphTier glyphs) {
            TerminalProfile profile = TerminalProfile.Fixed(width, 40, depth, glyphs);
            CellBuffer frame = Terminal.Render(new OverviewWidget(Scene()), profile);
            string text = Terminal.RenderText(new OverviewWidget(Scene()), profile);
            string[] lines = text.TrimEnd('\n').Split('\n');
            AssertPanelColumns(frame, lines, new[] { "CPU ", "Memory " }, glyphs, width >= 80 ? 0 : -1);
            AssertPanelColumns(frame, lines, new[] { "/ ", "/data ", "/archive " }, glyphs, width >= 80 ? 1 : -1);
            Assert.Matches(@"/data\s+[^%]+82% high", text);
            Assert.Matches(@"/archive\s+[^%]+91% critical", text);
        }

        private static void AssertPanelColumns(CellBuffer frame, string[] lines, string[] labels, GlyphTier glyphs, int panel) {
            int left = panel == 1 ? frame.Width / 2 : 0;
            int right = panel == 0 ? frame.Width / 2 : frame.Width;
            int? start = null;
            int? end = null;
            int? percent = null;
            foreach (string label in labels) {
                int y = Assert.Single(Enumerable.Range(0, lines.Length), row => {
                    string panelText = string.Concat(Enumerable.Range(left, right - left).Select(x => frame[x, row].Glyph));
                    return panelText.TrimStart(' ', '|', '│').StartsWith(label, StringComparison.Ordinal) && panelText.Contains("%");
                });
                int[] bars = Enumerable.Range(left, right - left).Where(x =>
                    glyphs == GlyphTier.Full ? frame[x, y].Glyph == "▆" :
                    glyphs == GlyphTier.Basic ? frame[x, y].Glyph == "▄" :
                    frame[x, y].Glyph == "#" || frame[x, y].Glyph == "-").ToArray();
                Assert.NotEmpty(bars);
                int percentCell = Assert.Single(Enumerable.Range(left, right - left), x => frame[x, y].Glyph == "%");
                Assert.Equal(start ?? bars.First(), bars.First());
                Assert.Equal(end ?? bars.Last(), bars.Last());
                Assert.Equal(percent ?? percentCell, percentCell);
                start = bars.First();
                end = bars.Last();
                percent = percentCell;
            }
        }

        [Theory]
        [InlineData(79.9, ThemeToken.Pass)]
        [InlineData(80.0, ThemeToken.Warn)]
        [InlineData(89.9, ThemeToken.Warn)]
        [InlineData(90.0, ThemeToken.Fail)]
        public void DiskBarsUseThresholdTonesAndTextualStatus(double used, ThemeToken tone) {
            OverviewSnapshot snapshot = Snapshot(new OverviewSection("Disks", "Disks", new[] { new OverviewItem("/data", "Synthetic capacity", used) }));
            TerminalProfile profile = TerminalProfile.Fixed(60, 24, ColorDepth.TrueColor, GlyphTier.Full);
            RenderContext context = new RenderContext(profile, Theme.Dark);
            CellBuffer buffer = Terminal.Render(new OverviewWidget(snapshot), profile, Theme.Dark);
            Assert.Contains(Enumerable.Range(0, buffer.Height).SelectMany(y => Enumerable.Range(0, buffer.Width).Select(x => buffer[x, y])),
                cell => cell.Glyph == "▆" && cell.Style.Foreground.Equals(context.Style(tone).Foreground));
            string text = Terminal.RenderText(new OverviewWidget(snapshot), profile);
            if (used >= 90) { Assert.Contains("critical", text); }
            else if (used >= 80) { Assert.Contains("high", text); }
            else { Assert.DoesNotContain("high", text); }
        }

        [Theory]
        [InlineData(120)]
        [InlineData(60)]
        public void PendingAndUnavailableSectionsRenderWithoutMarkupOrControlInjection(int width) {
            OverviewSnapshot snapshot = Snapshot(
                new OverviewSection("Pending", "Slow [warn]literal[/]", Array.Empty<OverviewItem>(), true),
                new OverviewSection("Resources", "CPU / memory", Array.Empty<OverviewItem>(), true),
                new OverviewSection("Empty", "Hidden empty", Array.Empty<OverviewItem>()),
                new OverviewSection("Text", "Literal", new[] { new OverviewItem("[fail]label[/]", "value\n\u001b[31m") }));
            string text = Terminal.RenderText(new OverviewWidget(snapshot), TerminalProfile.Fixed(width, 40, ColorDepth.None, GlyphTier.Ascii));
            Assert.Contains("...", text);
            Assert.Contains("CPU: measuring...", text);
            Assert.DoesNotContain("Hidden empty", text);
            Assert.Contains("[fail]label[/]", text);
            Assert.DoesNotContain("\u001b", text, StringComparison.Ordinal);
            Assert.All(text.Where(character => character != '\n'), character => Assert.InRange((int)character, 32, 126));
        }

        [Fact]
        public void LargeSnapshotsStayBoundedAndAnnounceOmittedItemsAndSections() {
            OverviewSection[] sections = Enumerable.Range(0, 24).Select(index => new OverviewSection("Section" + index, "Panel " + index,
                Enumerable.Range(0, 20).Select(item => new OverviewItem("Synthetic " + item, new string('x', 180))))).ToArray();
            string text = Terminal.RenderText(new OverviewWidget(Snapshot(sections)), TerminalProfile.Fixed(200, 100, ColorDepth.None, GlyphTier.Ascii));
            Assert.All(text.Split('\n'), line => Assert.InRange(DisplayWidth.Measure(line), 0, 120));
            Assert.InRange(text.TrimEnd('\n').Split('\n').Length, 1, 24);
            Assert.Contains("more", text);
            Assert.Contains("more sections", text);
            // Ten visible panels leave one body row each. Every panel must still convey an observation.
            Assert.Equal(10, System.Text.RegularExpressions.Regex.Matches(text, "Synthetic 0:").Count);
            Assert.Equal(10, System.Text.RegularExpressions.Regex.Matches(text, @"\+19 more").Count);
        }

        [Fact]
        public void BreakpointPanelsPreserveMeasuredValuesWhenThereIsNoRoomForBars() {
            string text = Terminal.RenderText(new OverviewWidget(Scene()), TerminalProfile.Fixed(80, 40, ColorDepth.None, GlyphTier.Ascii));
            Assert.Contains("12 / 32 GiB", text);
            Assert.Contains("82 / 100 GiB 82% high", text);
            Assert.Contains("91 / 100 GiB 91% critical", text);
            Assert.All(text.Split('\n'), line => Assert.InRange(DisplayWidth.Measure(line), 0, 80));
        }

        [Theory]
        [InlineData(120)]
        [InlineData(60)]
        public void HealthyPercentageObservationsPreserveValuesWithoutAPressureSuffix(int width) {
            OverviewSnapshot snapshot = Snapshot(new OverviewSection("Resources", "Resources", new[] { new OverviewItem("Memory", "12 / 32 GiB", 37.5) }));
            string text = Terminal.RenderText(new OverviewWidget(snapshot), TerminalProfile.Fixed(width, 24, ColorDepth.None, GlyphTier.Ascii));
            Assert.Contains("12 / 32 GiB", text);
            Assert.Contains("37%", text);
        }

        [Fact]
        public void WideDiskOverflowKeepsCriticalVolumesAheadOfHealthyVolumes() {
            OverviewItem[] volumes = Enumerable.Range(0, 12).Select(index => new OverviewItem("/healthy" + index, "Synthetic", 20)).ToArray();
            volumes[11] = new OverviewItem("/critical-volume", "Synthetic", 95);
            string text = Terminal.RenderText(new OverviewWidget(Snapshot(new OverviewSection("Disks", "Disks", volumes))), TerminalProfile.Fixed(120, 24, ColorDepth.None, GlyphTier.Ascii));
            Assert.Matches(@"/critical-volume\s+[^%]+95% critical", text);
            Assert.Contains("more", text);
        }

        [Fact]
        public void SnapshotCopiesCollectionsAndRejectsInvalidPercentages() {
            OverviewItem[] items = { new OverviewItem("CPU", "Synthetic", 42) };
            OverviewSection section = new OverviewSection("Resources", "Resources", items);
            items[0] = new OverviewItem("Changed", "Changed");
            Assert.Equal("CPU", section.Items[0].Label);
            Assert.Throws<ArgumentOutOfRangeException>(() => new OverviewItem("CPU", "Synthetic", double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OverviewItem("CPU", "Synthetic", 101));
        }

        internal static OverviewSnapshot Scene() => Snapshot(
            new OverviewSection("Resources", "CPU / memory", new[] { new OverviewItem("CPU", "8 logical CPUs", 24), new OverviewItem("Memory", "12 / 32 GiB", 37.5) }),
            new OverviewSection("Disks", "Disks", new[] { new OverviewItem("/", "54 / 100 GiB", 54), new OverviewItem("/data", "82 / 100 GiB", 82), new OverviewItem("/archive", "91 / 100 GiB", 91) }),
            new OverviewSection("Network", "Network", new[] { new OverviewItem("eth0", "192.0.2.10"), new OverviewItem("eth1", "2001:db8::10") }),
            new OverviewSection("Services", "Key services", new[] { new OverviewItem("Status", "All 3 running", null, ThemeToken.Pass) }),
            new OverviewSection("Updates", "Updates / reboot", new[] { new OverviewItem("Pending", "4 updates"), new OverviewItem("Reboot", "Reboot required", null, ThemeToken.Warn) }),
            new OverviewSection("Security", "Security", new[] { new OverviewItem("Failed logons (24 h)", "2", null, ThemeToken.Warn), new OverviewItem("12:00", "demo-user"), new OverviewItem("11:00", "test-user"), new OverviewItem("10:00", "sample-user") }));

        private static OverviewSnapshot Snapshot(params OverviewSection[] sections) => new OverviewSnapshot("demo-host", "Example OS 1.0", new TimeSpan(3, 4, 5, 0), new DateTimeOffset(2026, 10, 22, 12, 34, 0, TimeSpan.FromHours(2)), sections);
    }
}
