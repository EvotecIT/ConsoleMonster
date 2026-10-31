using System;
using System.Linq;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>The app overlay layer: popups over later content, placement by available room, dialogs above popups.</summary>
    [Collection("Live sessions")]
    public sealed class AppOverlayTests {
        private static Select Schedule() => new Select("schedule", new[] { new Choice("Hourly"), new Choice("Nightly"), new Choice("Weekly") }) { Dropdown = true, Value = "Nightly" };

        /// <summary>
        /// An open dropdown draws its list over the content arranged below it, not inside its own row; choosing closes it,
        /// commits the value and uncovers that content again.
        /// </summary>
        [Fact]
        public void Dropdown_DrawsItsListOverLaterContentAndCommitsOnEnter() {
            Rows layout = new Rows(Schedule(), TextBlock.Plain("FIRST BELOW"), TextBlock.Plain("SECOND BELOW"), TextBlock.Plain("THIRD BELOW"));

            ConsoleApp.Run(layout, options: AppTestHarness.Options("Enter Down", out ScreenCapture open));
            string[] rows = open.LastFrame!.ToPlainText().Split('\n');
            Assert.Contains(rows, row => row.Contains("Weekly"));
            Assert.DoesNotContain(rows, row => row.Contains("FIRST BELOW"));
            // The field keeps its three rows; the list's box starts on the next row, where FIRST BELOW was arranged.
            Assert.StartsWith("╭", rows[3].TrimStart());
            Assert.Contains("type to filter", rows[4]);
            // While open the dropdown takes Enter and Escape itself, so the footer says so and drops "esc quit".
            Assert.Equal("↑↓ move  enter select  esc close  tab next  shift+tab back", Footer(rows));

            AppState result = ConsoleApp.Run(new Rows(Schedule(), TextBlock.Plain("FIRST BELOW")), options: AppTestHarness.Options("Enter Down Enter", out ScreenCapture closed));
            string frame = closed.LastFrame!.ToPlainText();
            Assert.Equal("Weekly", result["schedule"]);
            Assert.Contains("FIRST BELOW", frame);
            Assert.DoesNotContain("Hourly", frame);
            Assert.Equal("enter open  tab next  shift+tab back  esc quit", Footer(frame.Split('\n')));
        }

        private static string Footer(string[] rows) => rows.Last(row => row.Trim().Length > 0).Trim();

        [Fact]
        public void Dropdown_OutsideClickClosesWithoutReopeningAndCanFocusAnotherInput() {
            Select schedule = Schedule();
            Button next = new Button("next", "Next");
            Rows layout = new Rows().Add(schedule, LayoutSize.Fixed(3))
                .Add(TextBlock.Plain("Background"), LayoutSize.Fraction()).Add(next, LayoutSize.Fixed(3));
            using AppController app = new AppController(layout, new AppState(), Terminal.CreateContext(), 60);
            app.PrepareFrame(80, 24);

            Click(70, 18);
            Assert.DoesNotContain("Hourly", Frame());
            Click(5, 1);
            Assert.Contains("Hourly", Frame());
            Click(70, 18);
            Assert.DoesNotContain("Hourly", Frame());
            Click(5, 1);
            Assert.Contains("Hourly", Frame());
            Click(5, 21);
            Assert.DoesNotContain("Hourly", Frame());
            Assert.True(next.Focused);

            void Click(int column, int row) {
                app.Handle(InputEvent.Mouse(column, row), true);
                app.Handle(InputEvent.Mouse(column, row, pressed: false), true);
                app.Refresh(null);
            }
            string Frame() => app.PrepareFrame(80, 24).ToPlainText();
        }

        /// <summary>A dropdown at the bottom of the screen opens upwards, where there is room.</summary>
        [Fact]
        public void Dropdown_NearTheBottom_OpensAbove() {
            Rows layout = new Rows().Add(TextBlock.Plain("TOP"), LayoutSize.Fraction()).Add(Schedule(), LayoutSize.Fixed(3));
            ConsoleApp.Run(layout, options: AppTestHarness.Options("Enter", out ScreenCapture capture));
            string[] rows = capture.LastFrame!.ToPlainText().Split('\n');
            int field = Array.FindIndex(rows, row => row.Contains("schedule"));
            int list = Array.FindIndex(rows, row => row.Contains("Weekly"));
            Assert.True(list >= 0 && list < field, "the list is drawn above the field");
        }

        /// <summary>
        /// A dialog opened while a dropdown is open covers it; closing the dialog returns to the same focused input with its
        /// list, and a toast from the dialog's callback replaces the footer hints.
        /// </summary>
        [Fact]
        public void Dialog_CoversAnOpenDropdownAndFocusReturnsToIt() {
            string covered = Run("Enter F2", out Select open);
            Assert.Contains("Dialog body", covered);
            Assert.DoesNotContain("Weekly", covered);

            string reopened = Run("Enter F2 Escape", out _);
            Assert.Contains("Weekly", reopened);
            Assert.Equal("✓ Dialog closed", reopened.Split('\n').Last(line => line.Trim().Length > 0).Trim());

            Run("Enter F2 Escape Down Enter", out Select schedule);
            Assert.Equal("Weekly", schedule.Value);
        }

        private static string Run(string keys, out Select schedule) {
            schedule = Schedule();
            AppDialogs dialogs = new AppDialogs();
            ConsoleAppOptions options = AppTestHarness.Options(keys, out ScreenCapture capture);
            options.Dialogs = dialogs;
            options.KeyBindings.Add(new KeyBinding("ask", () => dialogs.Show(new FormDialog<bool>("Really?", TextBlock.Plain("Dialog body"), _ => true), _ => dialogs.Show(new Toast("Dialog closed"))), new KeyChord(ConsoleKey.F2)));
            ConsoleApp.Run(new Rows(schedule, TextBlock.Plain("BELOW")), options: options);
            return capture.LastFrame!.ToPlainText();
        }
    }
}
