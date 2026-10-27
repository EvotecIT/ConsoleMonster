using System;
using System.Globalization;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;
using Grid = ConsoleMonster.Interactive.Grid;

namespace ConsoleMonster.Tests.Interactive.App {
    public sealed class Phase3GoldenTests {
        [Theory]
        [InlineData(120, 40, GlyphTier.Full, false, false)]
        [InlineData(80, 24, GlyphTier.Ascii, true, false)]
        [InlineData(40, 12, GlyphTier.Ascii, false, true)]
        [InlineData(20, 6, GlyphTier.Ascii, false, true)]
        public void RegionAndContextBarStatesHaveStableFrames(int width, int height, GlyphTier tier, bool light, bool plain) {
            Grid Listing(string title) {
                var model = new Grid(title); model.AddColumn(new TableColumn("Name") { IsKey = true }); model.AddColumn(new TableColumn("Size") { Priority = 5 }); model.AddColumn(new TableColumn("Modified") { Priority = 1 });
                for (int i = 0; i < 8; i++) { model.AddRow(i, "entry-" + i + (i % 2 == 0 ? ".log" : ".json"), i * 1024, "2026-10-26"); }
                return model;
            }
            using var left = new DataGrid("Left", Listing("Primary"));
            using var right = new DataGrid("Right", Listing("Replica"));
            left.ToggleMark(); right.Move(2); right.ToggleMark();
            var regions = new LayoutRegions().Add("left", left).Add("right", right);
            var options = new ConsoleAppOptions { MinimumWidth = 40, MinimumHeight = 12, Footer = new FooterBar { FixedSlots = true, HintsSource = () => new[] { new KeyHint("F2", "Options"), new KeyHint("F5", "Copy"), new KeyHint("F6", "Move", false), new KeyHint("F8", "Delete", false), new KeyHint("Alt+0", "Quit") } } };
            var profile = TerminalProfile.Fixed(width, height, plain ? ColorDepth.None : ColorDepth.TrueColor, tier).WithCulture(CultureInfo.InvariantCulture);
            var context = Terminal.CreateContext(profile, light ? Theme.Light : Theme.Dark);
            using var app = new AppController(regions, new AppState(), context, 60, options);
            right.Focus();
            string frame = app.PrepareFrame(width, height).ToPlainText();
            Golden.Match("interactive/phase3.regions." + width + "x" + height + ".txt", frame.TrimEnd() + "\n");
            if (width >= 80) { Assert.Contains("Primary", frame); Assert.Contains("Replica", frame); }
            else if (width >= 40) { Assert.DoesNotContain("Primary", frame); Assert.Contains("Replica", frame); }
            else { Assert.Contains("Make the window", frame); }
        }

        [Fact]
        public async System.Threading.Tasks.Task FooterRegistryIsEvaluatedOnTheInputThread() {
            int thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
            var footer = new FooterBar { HintsSource = () => {
                Assert.Equal(thread, System.Threading.Thread.CurrentThread.ManagedThreadId);
                return new[] { new KeyHint("F5", "Run") };
            } };
            var options = new ConsoleAppOptions { Footer = footer };
            var context = Terminal.CreateContext(TerminalProfile.Fixed(80, 24));
            using var app = new AppController(new TextInput("Value"), new AppState(), context, 60, options);
            var worker = System.Threading.Tasks.Task.Run(() => app.PrepareFrame(80, 24));
            Assert.Same(worker, await System.Threading.Tasks.Task.WhenAny(worker, System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5))));
            Assert.Contains("F5 Run", (await worker).ToPlainText());
        }
    }
}
