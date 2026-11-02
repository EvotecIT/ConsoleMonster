using System;
using System.Linq;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>The screen stack through a running app: navigation, inputs inside screens, focus per screen and the path.</summary>
    [Collection("Live sessions")]
    public sealed class AppScreensTests {
        /// <summary>
        /// Inputs inside a pushed screen commit to state, Back returns to the screen below with its own focused input
        /// restored, and only the current screen is drawn.
        /// </summary>
        [Fact]
        public void PushAndBack_KeepEachScreensInputsAndFocus() {
            TextInput first = new TextInput("first");
            TextInput second = new TextInput("second");
            TextInput note = new TextInput("note");
            AppScreens screens = new AppScreens(
                new AppScreen("list", "List", new Rows(first, second)),
                new AppScreen("detail", "Detail", new Rows(TextBlock.Plain("DETAIL SCREEN"), note)));
            ConsoleAppOptions options = AppTestHarness.Options("Tab F2 \"x\" Escape \"y\"", out ScreenCapture capture);
            options.KeyBindings.Add(new KeyBinding("open", () => screens.Push("detail"), new KeyChord(ConsoleKey.F2)));

            AppState result = ConsoleApp.Run(screens, options: options);

            Assert.Equal("x", result["note"]);
            Assert.Equal("y", result["second"]);
            Assert.Equal("", result["first"]);
            Assert.Equal("list", result["Screen"]);
            Assert.True(second.Focused);
            Assert.DoesNotContain("DETAIL SCREEN", capture.LastFrame!.ToPlainText());
        }

        /// <summary>
        /// Inputs on screens that are not shown never have focus, from the first frame on (a snapshot included): the footer does not offer a
        /// hidden dropdown's keys. Backspace goes back when the focused input leaves it unused, and focus then moves to an
        /// input that is shown.
        /// </summary>
        [Fact]
        public void HiddenScreenInputs_NeverHaveFocus_AndBackspaceGoesBack() {
            Select site = new Select("site", new[] { new Choice("North"), new Choice("West") }) { Dropdown = true };
            TextInput note = new TextInput("note");
            AppScreens screens = new AppScreens(new AppScreen("home", "Home", TextBlock.Plain("home")), new AppScreen("pick", "Pick", site));
            ConsoleAppOptions first = AppTestHarness.Options("", out ScreenCapture start);
            first.Interactive!.Mode = InteractionMode.Snapshot;
            ConsoleApp.Run(new Rows(screens, note), options: first);
            Assert.True(note.Focused);
            Assert.DoesNotContain("enter open", start.LastFrame!.ToPlainText());

            ConsoleAppOptions options = AppTestHarness.Options("F2 Backspace", out _);
            bool onPick = false;
            options.KeyBindings.Add(new KeyBinding("pick", () => { screens.Push("pick"); onPick = site.Focused; }, new KeyChord(ConsoleKey.F2)));
            AppState result = ConsoleApp.Run(new Rows(screens, note), options: options);
            Assert.True(onPick);
            Assert.Equal("home", result["Screen"]);
            Assert.True(note.Focused);
        }

        /// <summary>
        /// Assigning the state value opens a screen (the PowerShell route), a header bar without crumbs of its own shows
        /// the path with titles built from state, and assigning a screen already on the path returns to it.
        /// </summary>
        [Fact]
        public void StateValue_OpensScreensAndHeaderShowsThePath() {
            AppState state = new AppState { ["item"] = "web01" };
            AppScreens screens = new AppScreens(
                new AppScreen("list", "Servers", TextBlock.Plain("list")),
                new AppScreen("item", "Item", TextBlock.Plain("item")) { TitleSource = values => values.Get("item", "?") },
                new AppScreen("disk", "Disks", TextBlock.Plain("disks")));
            string? deep = null;
            int step = 0;
            ConsoleAppOptions options = AppTestHarness.Options("F2 F2 F2", out ScreenCapture capture);
            options.KeyBindings.Add(new KeyBinding("next", () => {
                step++;
                if (step == 1) { state["Screen"] = "item"; }
                else if (step == 2) { state["Screen"] = "disk"; }
                else { deep = string.Join(" > ", screens.Path.Select(screen => screen.Key)); state["Screen"] = "item"; }
            }, new KeyChord(ConsoleKey.F2)));

            ConsoleApp.Run(new Rows().Add(new HeaderBar("Fleet"), LayoutSize.Fixed(1)).Add(screens, LayoutSize.Fraction()), state, options);

            Assert.Equal("list > item > disk", deep);
            Assert.Equal(new[] { "list", "item" }, screens.Path.Select(screen => screen.Key));
            Assert.StartsWith(" Fleet › Servers › web01", capture.LastFrame!.ToPlainText());
        }
    }
}
