using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Contracts for committing and filtering a layout single selection.</summary>
    public sealed class SelectTests {
        /// <summary>Navigation does not commit, disabled choices cannot commit, and enabled choices notify once.</summary>
        [Fact]
        public void NavigationAndCommit_RejectDisabledChoices() {
            Select select = new Select("target", new[] {
                new Choice("blocked") { DisabledReason = "Unavailable" }, new Choice("ready")
            });
            List<AppValueChangedEventArgs> changes = new List<AppValueChangedEventArgs>();
            select.Changed += (_, change) => changes.Add(change);
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.Enter), Context()));
            Assert.Null(select.Value);
            Assert.Empty(changes);
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.DownArrow), Context()));
            Assert.Null(select.Value);
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.Spacebar), Context()));
            Assert.Equal("ready", select.Value);
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.Enter), Context()));
            AppValueChangedEventArgs change = Assert.Single(changes);
            Assert.Equal("target", change.Name);
            Assert.Null(change.PreviousValue);
            Assert.Equal("ready", change.Value);
        }

        /// <summary>Pasted filtering selects the matching row and Escape restores normal navigation.</summary>
        [Fact]
        public void FilterAndEscape_UseCanonicalChoiceEditing() {
            Select select = new Select("target", new[] { new Choice(1, "Alpha"), new Choice(2, "Beta") });
            Assert.True(select.HandleInput(InputEvent.Paste("Beta"), Context()));
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.Enter), Context()));
            Assert.Equal(2, select.Value);
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.Escape), Context()));
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.Home), Context()));
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.Enter), Context()));
            Assert.Equal(1, select.Value);
        }

        /// <summary>Programmatic edits reject unavailable values without changing the last committed value.</summary>
        [Fact]
        public void ValueAssignment_ValidatesAndNotifies() {
            Select select = new Select("target", new[] {
                new Choice("ready"), new Choice("blocked") { DisabledReason = "Unavailable" }
            });
            int changes = 0;
            select.Changed += (_, __) => changes++;
            select.Value = "ready";
            select.Value = "ready";
            Assert.Throws<ArgumentException>(() => select.Value = "blocked");
            Assert.Throws<ArgumentException>(() => select.Value = "missing");
            Assert.Equal("ready", select.CurrentValue);
            Assert.Equal(1, changes);
            select.Value = null;
            Assert.Null(select.Value);
            Assert.Equal(2, changes);
        }

        /// <summary>Hotkeys retain enabled checks and slash starts explicit filtering.</summary>
        [Fact]
        public void Hotkeys_RejectDisabledAndAllowExplicitFiltering() {
            Select select = new Select("target", new[] {
                new Choice(1, "Alpha") { Hotkey = 'a', DisabledReason = "Unavailable" },
                new Choice(2, "Beta") { Hotkey = 'b' }
            });
            Assert.True(select.HandleInput(InputEvent.Character('a'), Context()));
            Assert.Null(select.Value);
            Assert.True(select.HandleInput(InputEvent.Character('/'), Context()));
            Assert.True(select.HandleInput(InputEvent.Paste("Beta"), Context()));
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.Enter), Context()));
            Assert.Equal(2, select.Value);
        }

        /// <summary>The rendered list follows its filter while retaining the surrounding layout label.</summary>
        [Fact]
        public void Render_ShowsOnlyFilteredChoices() {
            Select select = new Select("target", new[] { new Choice(1, "Alpha"), new Choice(2, "Beta") });
            Assert.True(select.HandleInput(InputEvent.Paste("Beta"), Context()));
            CellBuffer buffer = new CellBuffer(60, 10);
            select.Render(new Region(buffer), Context());
            string text = buffer.ToPlainText();
            Assert.Contains("target", text);
            Assert.Contains("Beta", text);
            Assert.DoesNotContain("Alpha", text);
        }

        private static RenderContext Context() => new RenderContext(TerminalProfile.Fixed(60, 20, ColorDepth.None), Theme.Dark);
    }
}
