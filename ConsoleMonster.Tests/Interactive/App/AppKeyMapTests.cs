using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Scoped key bindings drive both the keys and the footer of a running app.</summary>
    [Collection("Live sessions")]
    public sealed class AppKeyMapTests {
        /// <summary>
        /// The focused input's binding runs before the input (Enter opens instead of pressing the button), a screen binding
        /// hides an app binding on the same key in both the keys and the footer, and an unavailable binding is dimmed and
        /// swallows its key instead of letting a hidden binding run.
        /// </summary>
        [Fact]
        public void Bindings_RunInScopeOrderAndTheFooterListsOnlyReachableKeys() {
            List<string> ran = new List<string>();
            Button go = new Button("go", "Go");
            go.Bindings.Add(new KeyBinding("open", () => ran.Add("open"), new KeyChord(ConsoleKey.Enter)));
            AppScreen main = new AppScreen("main", "Main", go);
            main.Bindings.Add(new KeyBinding("screen", () => ran.Add("screen x"), new KeyChord('x')));
            main.Bindings.Add(new KeyBinding("zap", () => ran.Add("zap"), new KeyChord('z')) { Available = () => false });
            ConsoleAppOptions options = AppTestHarness.Options("x z Enter", out ScreenCapture capture);
            options.KeyBindings.Add(new KeyBinding("app", () => ran.Add("app x"), new KeyChord('x')));
            options.KeyBindings.Add(new KeyBinding("app zap", () => ran.Add("app z"), new KeyChord('z')));

            AppState result = ConsoleApp.Run(new AppScreens(main), options: options);

            Assert.Equal(new[] { "screen x", "open" }, ran);
            Assert.Equal(0, result["go"]);
            string footer = capture.LastFrame!.ToPlainText().Split('\n').Last(line => line.Trim().Length > 0).Trim();
            Assert.Equal("enter open  x screen  z zap  tab next  esc quit", footer);
        }

        /// <summary>
        /// With twelve items, one to nine open at once unless they could start a larger number: 1 then 1 opens 11, 5 opens
        /// 5, 1 then Enter opens 1, and 0 alone is left for other bindings, so the footer lists those too. A key taken
        /// before the helper sees it (Tab) forgets a waiting digit. The footer shows the range, then the waiting digit, also
        /// when the focused input takes Enter.
        /// </summary>
        [Fact]
        public void Numbered_OpensOneAndTwoDigitItemsAndShowsThePendingDigit() {
            List<int> opened = new List<int>();
            KeyBinding numbered = KeyBinding.Numbered("open", () => 12, opened.Add);
            int zero = 0;
            ConsoleAppOptions options = AppTestHarness.Options("'1' '1' '5' '1' Enter '0' '9' '1' Tab '2'", out ScreenCapture idle);
            options.KeyBindings.Add(numbered);
            options.KeyBindings.Add(new KeyBinding("zero", () => zero++, new KeyChord('0')));
            ConsoleApp.Run(TextBlock.Plain("items"), options: options);
            Assert.Equal(new[] { 11, 5, 1, 9, 2 }, opened);
            Assert.Equal(1, zero);
            Assert.Equal("1-12", numbered.DisplayKeys());
            Assert.Contains("1-12 open  0 zero", idle.LastFrame!.ToPlainText());

            ConsoleAppOptions pending = AppTestHarness.Options("'1'", out ScreenCapture capture);
            pending.KeyBindings.Add(KeyBinding.Numbered("open", () => 12, _ => { }));
            ConsoleApp.Run(new Button("go"), options: pending);
            Assert.Contains("1_ open", capture.LastFrame!.ToPlainText());
        }

        [Fact]
        public void FocusedNumberedBinding_KeepsDigitsUntilAcceptanceOrFocusChanges() {
            List<int> opened = new List<int>();
            Button first = new Button("first", "First");
            first.Bindings.Add(KeyBinding.Numbered("open", () => 12, opened.Add));
            ConsoleAppOptions options = AppTestHarness.Options("'1' '2' '1' Enter '1' Tab Shift+Tab '2'", out _);

            ConsoleApp.Run(new Rows(first, new Button("second", "Second")), options: options);

            Assert.Equal(new[] { 12, 1, 2 }, opened);
        }

        /// <summary>
        /// The footer leaves out keys the focused input takes: a text input takes typed letters and Space, and an open dropdown on a
        /// nested screen takes Escape (it closes) and Backspace (it edits the filter), so Back is not offered at all and a binding on
        /// Escape or F3 is offered under F3.
        /// </summary>
        [Theory]
        [InlineData("", "f2 next  tab next  esc quit")]
        [InlineData("F2 Enter", "↑↓ move  enter select  esc close  f3 cancel  tab next")]
        public void Footer_LeavesOutKeysTheFocusedInputTakes(string keys, string expected) {
            AppScreens screens = new AppScreens();
            AppScreen search = new AppScreen("search", "Search", new TextInput("query"));
            search.Bindings.Add(new KeyBinding("view", () => { }, new KeyChord('v')));
            search.Bindings.Add(new KeyBinding("pause", () => { }, new KeyChord(ConsoleKey.Spacebar)));
            search.Bindings.Add(new KeyBinding("next", () => screens.Push("pick"), new KeyChord(ConsoleKey.F2)));
            AppScreen pick = new AppScreen("pick", "Pick", new Select("site", new[] { new Choice("North"), new Choice("West") }) { Dropdown = true });
            pick.Bindings.Add(KeyBinding.Create("Escape, F3", "cancel", () => { }));
            screens.Add(search).Add(pick);
            ConsoleApp.Run(screens, options: AppTestHarness.Options(keys, out ScreenCapture capture));
            Assert.Equal(expected, capture.LastFrame!.ToPlainText().Split('\n').Last(line => line.Trim().Length > 0).Trim());
        }

        /// <summary>Tab and Ctrl+C reach the app before any binding, so binding them is refused instead of silently never running.</summary>
        [Fact]
        public void Binding_OnAKeyTheAppTakesFirst_IsRefused() {
            ConsoleAppOptions options = AppTestHarness.Options("Escape", out _);
            options.KeyBindings.Add(new KeyBinding("indent", () => { }, new KeyChord(ConsoleKey.Tab)));
            ArgumentException error = Assert.Throws<ArgumentException>(() => ConsoleApp.Run(TextBlock.Plain("items"), options: options));
            Assert.Contains("indent", error.Message);
        }
    }
}
