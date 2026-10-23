using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class RecordValueTests {
        [Fact]
        public void AddField_StringValue_PreservesLiteralBracketsAndPresenterInput() {
            RecordView view = new RecordView();
            view.AddField("Identity", "[green]Operators[/]");
            Assert.True(Assert.Single(view.Items).Cell!.HasValue);
            Assert.Equal("[green]Operators[/]", Assert.Single(view.Items).Cell!.Value);
            Assert.Contains("[green]Operators[/]", TestTerminal.RenderText(view, width: 100));
        }

        [Fact]
        public void AddField_ExplicitCellAndNullableValue_RetainsCellIntentAndEmptyValue() {
            RecordView view = new RecordView { HideEmpty = false };
            TableCell status = TableCell.ForStatus(Styling.StatusKind.Warn, "Needs review");
            string? absent = null;
            view.AddField("Status", status);
            view.AddField("Owner", absent);
            Assert.Same(status, view.Items[0].Cell);
            Assert.True(view.Items[1].Cell!.HasValue);
            Assert.Null(view.Items[1].Cell!.Value);
            Assert.Contains("Needs review", TestTerminal.RenderText(view, width: 100));
        }
    }
}
