using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;
using Xunit;
using Grid = ConsoleMonster.Interactive.Grid;

namespace ConsoleMonster.Tests.Interactive.App {
    public sealed class DataGridTests {
        private static readonly Canvas.RenderContext Context = Terminal.CreateContext(TerminalProfile.Fixed(100, 30));
        private static Grid Listing(params string[] names) {
            var grid = new Grid("Files"); grid.AddColumn("Name");
            foreach (string name in names) { grid.AddRow(new GridRow(name, name) { Id = name }); }
            return grid;
        }

        [Fact]
        public void TwoGridsKeepIndependentCursorMarksAndOriginalObjectResults() {
            using var left = new DataGrid("Left", Listing("one", "two"));
            using var right = new DataGrid("Right", Listing("other", "last"));
            ConsoleAppOptions options = AppTestHarness.Options("Down Space Tab Space Escape", out _);
            AppState result = ConsoleApp.Run(new Columns(left, right), options: options);
            Assert.Equal("two", left.CurrentRow!.Item);
            Assert.Equal("other", right.CurrentRow!.Item);
            Assert.Equal(new object?[] { "two" }, result.Get<object?[]>("Left"));
            Assert.Equal(new object?[] { "other" }, result.Get<object?[]>("Right"));
            Assert.False(left.Focused); Assert.True(right.Focused);
        }

        [Fact]
        public void StableIdentitiesRetainMarksAndFocusAcrossReplacementAndSorting() {
            using var grid = new DataGrid("Rows", Listing("a", "b", "c"));
            grid.Move(1); grid.ToggleMark();
            object replacement = new object();
            grid.SetRows(new[] { new GridRow("C", "c") { Id = "c" }, new GridRow(replacement, "b") { Id = "b" } });
            Assert.Same(replacement, grid.CurrentRow!.Item);
            Assert.Same(replacement, Assert.Single(grid.Selection));
            grid.Sort(0); Assert.Same(replacement, grid.CurrentRow.Item);
            grid.SetRows(new[] { new GridRow("C", "c") { Id = "c" } });
            Assert.Empty(grid.Marks); Assert.Equal("C", Assert.Single(grid.Selection));
        }

        [Fact]
        public void PrefixSearchPatternMarksAndHiddenMarksUseDisplayOrder() {
            using var grid = new DataGrid("Rows", Listing("beta.txt", "alpha.log", "alpine.txt"));
            Assert.True(grid.SearchPrefix("alp")); Assert.Equal("alpha.log", grid.CurrentRow!.Item);
            grid.MarkPattern("*.txt"); grid.UnmarkPattern("b?ta.*");
            Assert.Equal("alpine.txt", Assert.Single(grid.Marks));
            grid.InvertMarks(); grid.Sort(0); grid.Filter = "beta";
            Assert.Equal(new object?[] { "alpha.log", "beta.txt" }, grid.Selection);
            grid.ClearSearch(); Assert.Empty(grid.Search);
        }

        [Fact]
        public async Task LargeListingOnlyStartsVisibleCellsAndRepaintsCompletedValues() {
            int started = 0;
            var firstStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var listing = new Grid("Large"); listing.AddColumn("Name"); listing.AddColumn("Size");
            for (int i = 0; i < 10000; i++) {
                listing.AddRow(new GridRow(i, "row" + i.ToString("D5"), new DataGridCell(_ => { Interlocked.Increment(ref started); firstStarted.TrySetResult(true); return completion.Task; }, "pending")) { Id = i });
            }
            using var grid = new DataGrid("Rows", listing) { Focused = true };
            var repainted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            grid.Invalidated += () => repainted.TrySetResult(true);
            string loading = FrameComposer.Compose(grid, 80, 12, Context).ToPlainText();
            Assert.Contains("pending", loading);
            Assert.Same(firstStarted.Task, await Task.WhenAny(firstStarted.Task, Task.Delay(TimeSpan.FromSeconds(5))));
            Assert.InRange(started, 1, 12);
            completion.SetResult("ready");
            Assert.Same(repainted.Task, await Task.WhenAny(repainted.Task, Task.Delay(TimeSpan.FromSeconds(5))));
            Assert.Contains("ready", FrameComposer.Compose(grid, 80, 12, Context).ToPlainText());
            grid.Move(9999);
            Assert.Contains("row09999", FrameComposer.Compose(grid, 80, 12, Context).ToPlainText());
            Assert.InRange(started, 1, 24);
        }

        [Fact]
        public void PointerHitTestingKeepsTwoGridSelectionsSeparate() {
            using var left = new DataGrid("Left", Listing("one", "two"));
            using var right = new DataGrid("Right", Listing("other", "last"));
            var layout = new Columns(left, right);
            using var focus = new FocusManager(layout);
            var frame = FrameComposer.Compose(layout, 100, 15, Context);
            int row = Enumerable.Range(0, frame.Height).First(y => frame.GetRowText(y).Contains("last"));
            Assert.True(focus.FocusAt(65, row));
            Assert.True(right.HandleInput(InputEvent.Mouse(65, row), Context));
            Assert.Equal("last", right.CurrentRow!.Item); Assert.Equal("one", left.CurrentRow!.Item);
        }

        [Fact]
        public void RegistryCanClaimTabF6AndF8WithoutLosingControlCCancellation() {
            using var grid = new DataGrid("Rows", Listing("one", "two"));
            ConsoleAppOptions options = AppTestHarness.Options("Tab F6 F8 Ctrl+C", out _);
            options.Keys.NextFocus = null; options.Keys.CancelAction = null; options.Keys.Actions = null;
            int calls = 0;
            options.HandleInput = (_, input) => { if (input.Is(ConsoleKey.Tab) || input.Is(ConsoleKey.F6) || input.Is(ConsoleKey.F8)) { calls++; return true; } return false; };
            ConsoleApp.Run(grid, options: options);
            Assert.Equal(3, calls);
        }
    }
}
