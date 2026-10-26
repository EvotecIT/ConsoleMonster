using System;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>The interactive grid: filter, sort, marks, columns, details, paging and the result.</summary>
    public sealed class GridTests {
        private static readonly string[] Roles = { "web", "sql", "dc", "file", "app" };

        private static Grid Servers(GridSelection selection = GridSelection.Multiple) {
            Grid grid = new Grid("Servers") { OutputMode = selection, Presentation = new PresentationOptions { Culture = CultureInfo.InvariantCulture } };
            grid.AddColumn(new TableColumn("Name") { IsKey = true });
            grid.AddColumn("Role");
            grid.AddColumn("CPU %");
            grid.AddColumn(new TableColumn("Status") { Presenter = CellPresenters.Status });
            grid.AddColumn("Last seen");
            for (int i = 1; i <= 30; i++) {
                string name = "SRV" + i.ToString("00", CultureInfo.InvariantCulture);
                int cpu = (i * 37) % 100;
                string status = cpu > 85 ? "fail" : cpu > 70 ? "warn" : "pass";
                grid.AddRow(name, name, Roles[i % Roles.Length], cpu, status, new DateTime(2026, 10, 17, 8, 0, 0).AddMinutes(i * 7));
            }
            return grid;
        }

        private static GridResult Show(Grid grid, string keys, out RecordingSurface surface, GlyphTier tier = GlyphTier.Full, int width = 90, int height = 20, GridOptions? options = null, ITheme? theme = null) {
            RecordingSurface s = new RecordingSurface(width, height, false);
            surface = s;
            GridOptions o = options ?? new GridOptions();
            o.Interactive = PromptHarness.Options(keys, s);
            o.Interactive.Profile = TerminalProfile.Fixed(width, height, ColorDepth.TrueColor, tier);
            o.Interactive.Theme = theme ?? Theme.Dark;
            return PromptHarness.Guard(s, () => grid.Show(o));
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void FilterAndMarks_MatchGolden(GlyphTier tier) {
            GridResult result = Show(Servers(), "\"web\" Space Down Space Ctrl+Enter", out RecordingSurface surface, tier);
            Assert.True(result.Accepted);
            Assert.Equal(new object?[] { "SRV05", "SRV10" }, result.Items.ToArray());
            string frame = surface.Frames.Last(f => f.ToPlainText().Contains("2 selected")).ToPlainText();
            Golden.Match("interactive/grid.filter." + tier.ToString().ToLowerInvariant() + ".txt", frame.TrimEnd() + "\n");
            Assert.DoesNotContain("SRV01 ", frame);
        }

        [Fact]
        public void Sort_ByANumericColumn_AndReverse() {
            GridResult top = Show(Servers(GridSelection.Single), "F3 F3 F3 F4 Ctrl+Home Ctrl+Enter", out RecordingSurface surface);
            Assert.Equal("SRV27", top.Items.Single()); // 99 %
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("CPU % ↓"));
            GridResult first = Show(Servers(GridSelection.Single), "F3 F4 Ctrl+Home Ctrl+Enter", out _);
            Assert.Equal("SRV30", first.Items.Single());
        }

        [Fact]
        public void ColumnChooser_HidesAColumn() {
            Show(Servers(), "F2 Down Space Escape Escape", out RecordingSurface surface);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("[ ] Role"));
            string last = surface.Frames.Last().ToPlainText();
            Assert.Contains("Name", last);
            Assert.DoesNotContain("Role", last);
        }

        [Fact]
        public void Details_ShowEveryColumnOfTheFocusedRow() {
            Show(Servers(), "Down Down Enter Down Escape Escape", out RecordingSurface surface);
            string detail = surface.Frames.First(f => f.ToPlainText().Contains("Last seen") && f.ToPlainText().Contains("SRV03") && f.ToPlainText().Contains("esc close")).ToPlainText();
            Golden.Match("interactive/grid.detail.txt", detail.TrimEnd() + "\n");
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("SRV04") && f.ToPlainText().Contains("esc close"));
        }

        [Fact]
        public void Escape_ClearsTheFilterFirst_ThenCancels() {
            GridResult result = Show(Servers(), "\"sql\" Escape Escape", out RecordingSurface surface);
            Assert.False(result.Accepted);
            Assert.Empty(result.Items);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("6 of 30 rows"));
        }

        [Fact]
        public void Paging_KeepsTheFocusedRowVisible() {
            GridResult result = Show(Servers(GridSelection.Single), "PageDown PageDown Ctrl+Enter", out RecordingSurface surface, height: 14);
            string name = (string)result.Items.Single()!;
            Assert.True(string.CompareOrdinal(name, "SRV10") > 0, name);
            Assert.Contains(name, surface.Frames.Last().ToPlainText());
        }

        [Fact]
        public void NoMatches_SaysSo() {
            Show(Servers(), "\"zzz\" Escape Escape", out RecordingSurface surface);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("No matches") && f.ToPlainText().Contains("0 of 30 rows"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Footer_AtSixtyColumns_KeepsMoveSelectDoneAndCancel(bool light) {
            ITheme theme = light ? Theme.Light : Theme.Dark;
            GridResult result = Show(Servers(), "Space Down Space Ctrl+Enter", out RecordingSurface surface, width: 60, theme: theme);
            Assert.True(result.Accepted);
            Assert.Equal(new object?[] { "SRV01", "SRV02" }, result.Items.ToArray());
            var frame = surface.Frames.First();
            string footer = frame.GetRowText(19);
            Assert.Equal(" ↑↓ move  space select  ctrl+enter done  esc cancel", footer.TrimEnd());
            Assert.Equal(20, frame.Height);
            Assert.StartsWith("╭", frame.GetRowText(3));
            Assert.Contains("SRV01", frame.GetRowText(6));
            Assert.Contains("SRV12", frame.GetRowText(17));
            Assert.StartsWith("╰", frame.GetRowText(18));
            var context = Terminal.CreateContext(TerminalProfile.Fixed(60, 20), theme);
            foreach (string keys in new[] { "↑↓", "space", "ctrl+enter", "esc" }) {
                int start = footer.IndexOf(keys, StringComparison.Ordinal);
                for (int x = start; x < start + keys.Length; x++) {
                    Assert.Equal(context.Style(ThemeToken.Accent), frame[x, 19].Style);
                }
            }
            foreach (string label in new[] { "move", "select", "done", "cancel" }) {
                int start = footer.IndexOf(label, StringComparison.Ordinal);
                for (int x = start; x < start + label.Length; x++) {
                    Assert.Equal(context.Style(ThemeToken.Muted), frame[x, 19].Style);
                }
            }
        }

        [Theory]
        [InlineData(60)]
        [InlineData(120)]
        public void Footer_SecondaryHints_RemainInWideFooterAndFullKeyMap(int width) {
            GridResult result = Show(Servers(), "F1 Escape F10", out RecordingSurface surface, width: width);
            Assert.True(result.Accepted);
            Assert.Equal("SRV01", result.Items.Single());
            string footer = surface.Frames.First().GetRowText(19);
            if (width == 120) {
                foreach (string hint in new[] { "enter details", "f3 sort", "f2 columns", "? keys" }) {
                    Assert.Contains(hint, footer);
                }
            }
            string keyMap = surface.Frames.First(f => f.ToPlainText().Contains("pgup/pgdn")).ToPlainText();
            foreach (string keys in new[] { "ctrl+a", "ctrl+enter", "enter", "esc", "f2", "f3", "f4" }) {
                Assert.Contains(keys, keyMap);
            }
            GridResult cancelled = Show(Servers(), "F1 Escape Escape", out _, width: width);
            Assert.False(cancelled.Accepted);
            Assert.Empty(cancelled.Items);
        }
    }
}
