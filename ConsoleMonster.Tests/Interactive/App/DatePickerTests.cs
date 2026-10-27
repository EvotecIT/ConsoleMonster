using System;
using System.Globalization;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Dates;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Rendering;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    public sealed class DatePickerTests {
        private static readonly Canvas.RenderContext Context = Terminal.CreateContext(TerminalProfile.Fixed(60, 20));
        [Fact]
        public void PointerRangeUsesSharedMonthGeometryAndLimitsEdgeNavigation() {
            var picker = new DatePicker("Window", new DateTime(2026, 10, 8)) { Range = true, Culture = CultureInfo.GetCultureInfo("en-US") };
            string frame = FrameComposer.Compose(picker, 60, 20, Context).ToPlainText();
            string[] rows = frame.Split('\n');
            int dayRow = Array.FindIndex(rows, row => row.Contains("04  05  06  07  08  09  10"));
            Assert.True(dayRow >= 0, frame);
            int x = rows[dayRow].IndexOf("08", StringComparison.Ordinal);
            picker.HandleInput(InputEvent.Mouse(x, dayRow), Context);
            picker.HandleInput(InputEvent.Pointer(x + 8, dayRow, MouseAction.Move, heldButtons: 1), Context);
            picker.HandleInput(InputEvent.Pointer(x + 8, dayRow, MouseAction.Up, 1), Context);
            var range = Assert.IsType<DateRange>(picker.Value);
            Assert.Equal(new DateTime(2026, 10, 8), range.Start); Assert.Equal(new DateTime(2026, 10, 10), range.End);
            picker.HandleInput(InputEvent.Mouse(x, dayRow), Context);
            picker.HandleInput(InputEvent.Pointer(x, 30, MouseAction.Move, heldButtons: 1), Context);
            picker.HandleInput(InputEvent.Pointer(x, 30, MouseAction.Move, heldButtons: 1), Context);
            Assert.Equal(11, picker.Cursor.Month);
            picker.HandleInput(InputEvent.Pointer(x, 30, MouseAction.Up, 1), Context);
            Assert.Equal(new DateTime(2026, 11, 8), Assert.IsType<DateRange>(picker.Value).End);
        }
        [Fact]
        public void NavigationClampsMonthAndYearWithoutCommittingOrConvertingTimeZones() {
            var picker = new DatePicker("Date", new DateTime(2024, 1, 31));
            picker.HandleInput(InputEvent.Press(ConsoleKey.PageDown), Context);
            Assert.Equal(new DateTime(2024, 2, 29), picker.Cursor); Assert.Null(picker.Value);
            picker.HandleInput(InputEvent.Press(ConsoleKey.PageDown, KeyModifiers.Control), Context);
            Assert.Equal(new DateTime(2025, 2, 28), picker.Cursor);
            picker.HandleInput(InputEvent.Press(ConsoleKey.Enter), Context);
            Assert.Equal(new DateTime(2025, 2, 28), picker.Value); Assert.Equal(DateTimeKind.Unspecified, ((DateTime)picker.Value!).Kind);
        }

        [Fact]
        public void RangeRejectsUnavailableInteriorDateAndCanBeCancelledBeforeCommit() {
            var picker = new DatePicker("Range", new DateTime(2026, 10, 8)) { Range = true, Unavailable = date => date.Day == 9 };
            picker.HandleInput(InputEvent.Press(ConsoleKey.Spacebar), Context);
            picker.HandleInput(InputEvent.Press(ConsoleKey.RightArrow), Context);
            picker.HandleInput(InputEvent.Press(ConsoleKey.RightArrow), Context);
            picker.HandleInput(InputEvent.Press(ConsoleKey.Enter), Context);
            Assert.Null(picker.Value); Assert.Contains("unavailable", picker.ValidationMessage);
            Assert.True(picker.HandleInput(InputEvent.Press(ConsoleKey.Escape), Context));
            Assert.False(picker.HandleInput(InputEvent.Press(ConsoleKey.Escape), Context));
        }

        [Fact]
        public void ScalarRangeDefaultBecomesOneInclusiveDateRangeAndInvalidDefaultsFailUnattended() {
            var prompt = new DatePrompt("Window") { Range = true, Default = new DateTime(2026, 10, 8, 15, 10, 0, DateTimeKind.Utc) };
            var range = Assert.IsType<DateRange>(prompt.Show(new InteractiveOptions { Mode = InteractionMode.None }));
            Assert.Equal(range.Start, range.End); Assert.Equal(new DateTime(2026, 10, 8), range.Start);
            prompt.Selection.Minimum = new DateTime(2026, 10, 9);
            Assert.Throws<ArgumentException>(() => prompt.Show(new InteractiveOptions { Mode = InteractionMode.None }));
        }

        [Fact]
        public void PromptReturnsTypedRangeAndEscapeUsesSharedCancellation() {
            var surface = new RecordingSurface(60, 20, false);
            var prompt = new DatePrompt("Window") { Range = true, Selection = new DateSelectionOptions { Today = new DateTime(2026, 10, 8), Culture = CultureInfo.GetCultureInfo("en-GB") } };
            var range = Assert.IsType<DateRange>(prompt.Show(PromptHarness.Options("Space Right Right Enter", surface)));
            Assert.Equal(new DateTime(2026, 10, 8), range.Start); Assert.Equal(new DateTime(2026, 10, 10), range.End);
            Assert.Throws<PromptCanceledException>(() => prompt.Show(PromptHarness.Options("Escape", new RecordingSurface(60, 20, false))));
            Assert.Contains("October 2026", FrameComposer.Compose(new DatePicker("Date", new DateTime(2026, 10, 8)) { Culture = CultureInfo.GetCultureInfo("en-GB") }, 60, 20, Context).ToPlainText());
        }
    }
}
