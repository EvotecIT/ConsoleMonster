using System;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    /// <summary>Replaceable regions (Slot) and keyed live tables.</summary>
    public sealed class LiveContentTests {
        private static LiveSession Session(RecordingTarget target, IWidget root) {
            return new LiveSession(root, new LiveSessionOptions { Target = target, CaptureConsole = false, HandleCancelKey = false, StreamTimestamps = false });
        }

        [Fact]
        public void Slot_SetReplacesTheContentInsideItsFrame() {
            Slot slot = new Slot("main", new TextBlock("loading"));
            Panel panel = new Panel(slot, "Result");
            Assert.Contains("loading", TestTerminal.RenderText(panel, 30, glyphs: GlyphTier.Ascii));
            slot.Apply(new LiveEvent("main", "set") { Payload = new TextBlock("[pass]done[/]") });
            string text = TestTerminal.RenderText(panel, 30, glyphs: GlyphTier.Ascii);
            Assert.Contains("done", text);
            Assert.DoesNotContain("loading", text);
            Assert.StartsWith("+- Result", text);
            slot.Apply(new LiveEvent("main", "clear"));
            Assert.Equal(0, slot.Measure(new Constraints(30), Terminal.CreateContext(TestTerminal.Profile(30))).Height);
        }

        [Fact]
        public void Region_SwapsThroughTheQueueAndAdoptsLiveWidgetsInTheNewContent() {
            RecordingTarget target = TestTerminal.Target(60);
            Slot slot = new Slot("main", new TextBlock("waiting"));
            using (LiveSession session = Session(target, new Rows(slot))) {
                session.Start();
                Lanes lanes = new Lanes("servers");
                session.Region("main").Set(lanes);
                LanesHandle handle = lanes.Handle(session);
                handle.Add("srv1", "SRV1", 1);
                handle.Start("srv1");
                Assert.Throws<ArgumentException>(() => session.Region("servers"));
            }
            Assert.Same(slot.Content, slot.Children[0]);
            Assert.IsType<Lanes>(slot.Content);
            Assert.Contains("SRV1 started", target.Text);
        }

        private static LiveTable Jobs() {
            Table table = new Table().AddColumn("Job").AddColumn("State").AddColumn("Took");
            return new LiveTable(table, "jobs");
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Basic)]
        [InlineData(GlyphTier.Ascii)]
        public void LiveTable_AddUpdateRemove_RenderPerTier(GlyphTier tier) {
            LiveTable jobs = Jobs();
            jobs.Set("1", "backup-database-nightly", StatusKind.Running, TimeSpan.FromSeconds(3));
            jobs.Set("2", "rotate-logs", StatusKind.Pass, TimeSpan.FromSeconds(1));
            jobs.Set("3", "sync", StatusKind.Pending, null);
            jobs.Set("1", "backup-database-nightly", StatusKind.Fail, TimeSpan.FromSeconds(42));
            jobs.Remove("2");
            Assert.Equal(new[] { "1", "3" }, jobs.Keys);
            Golden.Match("livetable." + tier.ToString().ToLowerInvariant() + ".txt", TestTerminal.RenderText(jobs, 60, glyphs: tier));
        }

        [Fact]
        public void LiveTable_ColumnWidthsNeverShrink() {
            LiveTable jobs = Jobs();
            jobs.Set("1", "a-very-long-job-name", StatusKind.Pass, TimeSpan.FromSeconds(1));
            string wide = TestTerminal.RenderText(jobs, 60, glyphs: GlyphTier.Ascii);
            jobs.Remove("1");
            jobs.Set("2", "x", StatusKind.Pass, TimeSpan.FromSeconds(1));
            string narrow = TestTerminal.RenderText(jobs, 60, glyphs: GlyphTier.Ascii);
            Assert.Equal(wide.Split('\n')[0].Length, narrow.Split('\n')[0].Length);
        }

        [Fact]
        public void LiveTable_KeepsTheMostRecentRowsAndSortsOnInsert() {
            LiveTable jobs = Jobs();
            jobs.MaxRows = 3;
            jobs.SortColumn = 2;
            jobs.SortDescending = true;
            for (int i = 1; i <= 5; i++) {
                jobs.Set(i.ToString(), "job" + i, StatusKind.Pass, TimeSpan.FromSeconds(i % 3));
            }
            // Rows 1 and 2 fell out of the window; the rest is ordered by duration, largest first.
            Assert.Equal(new[] { "5", "4", "3" }, jobs.Keys);
            jobs.Clear();
            Assert.Empty(jobs.Keys);
        }

        [Fact]
        public void LiveTable_HandleUpdatesThroughTheSessionAndStreamsRows() {
            RecordingTarget target = TestTerminal.Target(80);
            LiveTable jobs = Jobs();
            using (LiveSession session = Session(target, jobs)) {
                session.Start();
                LiveTableHandle handle = jobs.Handle(session);
                handle.AddRow("1", "backup", StatusKind.Running, TimeSpan.Zero);
                handle.UpdateRow("1", "backup", StatusKind.Pass, TimeSpan.FromSeconds(12));
                handle.AddRow("2", "sync", StatusKind.Running, TimeSpan.Zero);
                handle.RemoveRow("2");
            }
            Assert.Equal(new[] { "1" }, jobs.Keys);
            Assert.Contains("backup  Pass  00:00:12", target.Text);
        }
    }
}
