using System;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;
using Xunit;
using Grid = ConsoleMonster.Interactive.Grid;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Action grids distinguish actual data rows from blank space and selection-mode hints.</summary>
    public sealed class GridPointerTests {
        private static readonly Canvas.RenderContext Context = Terminal.CreateContext(TerminalProfile.Fixed(100, 30));

        [Theory]
        [InlineData(GridSelection.None, false)]
        [InlineData(GridSelection.Single, false)]
        [InlineData(GridSelection.Multiple, true)]
        public void ActionFooter_OnlyOffersMarksForMultipleSelection(GridSelection selection, bool marks) {
            Grid grid = new Grid("Services") { Selection = selection };
            grid.AddColumn("Name"); grid.AddRow("one", "One");
            GridOptions options = new GridOptions();
            options.Commands.Add(new AppCommand("run", "Run", (_, __) => Task.CompletedTask));
            using GridView view = new GridView(grid, options, new PromptLabels());
            string text = FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context).ToPlainText();
            Assert.Equal(marks, text.Contains("Space mark"));
        }

        [Fact]
        public void Pointer_FilteringDuringDragCancelsTheOldRangeAndAllowsANewDrag() {
            Grid grid = new Grid("Services"); grid.AddColumn("Name");
            grid.AddRow("keep-one", "Keep One"); grid.AddRow("keep-two", "Keep Two");
            for (int index = 2; index < 6; index++) { grid.AddRow("other-" + index, "Other " + index); }
            using GridView view = new GridView(grid, new GridOptions(), new PromptLabels());
            string[] initial = FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context).ToPlainText().Split('\n');
            int last = Array.FindIndex(initial, line => line.Contains("Other 5"));
            Assert.True(last >= 0);
            view.Handle(InputEvent.Mouse(5, last));
            foreach (char letter in "Keep") { view.Handle(InputEvent.Character(letter)); }
            string[] filtered = FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context).ToPlainText().Split('\n');
            int first = Array.FindIndex(filtered, line => line.Contains("Keep One"));
            int second = Array.FindIndex(filtered, line => line.Contains("Keep Two"));
            Assert.True(first >= 0 && second > first);
            view.Handle(InputEvent.Pointer(5, first, MouseAction.Move, heldButtons: 1));
            Assert.Single(view.ChosenItems());
            view.Handle(InputEvent.Mouse(5, first, pressed: false));
            view.Handle(InputEvent.Mouse(5, first));
            view.Handle(InputEvent.Pointer(5, second, MouseAction.Move, heldButtons: 1));
            view.Handle(InputEvent.Mouse(5, second, pressed: false));
            Assert.Equal(new object?[] { "keep-one", "keep-two" }, view.ChosenItems());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Pointer_RejectsBlankSpaceAndAcceptsEveryLineOfADataRow(bool widgetCell) {
            Grid grid = new Grid("Services"); grid.AddColumn("Name");
            grid.AddRow("one", "One");
            grid.AddRow("two", widgetCell ? (object)new TextBlock("Two\nSecond line") : "Two");
            using GridView view = new GridView(grid, new GridOptions(), new PromptLabels());
            string[] lines = FrameComposer.Compose(view.BuildFrame(new ViewFrame(Context, 100, 30, false, TimeSpan.Zero)), 100, 30, Context).ToPlainText().Split('\n');
            int last = Array.FindIndex(lines, line => line.Contains(widgetCell ? "Second line" : "Two"));
            Assert.True(last >= 0);
            view.Handle(InputEvent.Mouse(5, last + 2));
            Assert.Equal("one", Assert.Single(view.ChosenItems()));
            view.Handle(InputEvent.Mouse(5, last));
            Assert.Equal("two", Assert.Single(view.ChosenItems()));
        }
    }
}
