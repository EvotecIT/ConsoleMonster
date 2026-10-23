using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Contracts for the public layout toggle.</summary>
    public sealed class ToggleTests {
        /// <summary>Keyboard and programmatic edits notify once, while repeated assignments do not.</summary>
        [Fact]
        public void ValueAndKeyboardEdits_NotifyOnlyForChanges() {
            Toggle toggle = new Toggle("enabled");
            List<AppValueChangedEventArgs> changes = new List<AppValueChangedEventArgs>();
            toggle.Changed += (_, change) => changes.Add(change);
            toggle.Value = true;
            toggle.Value = true;
            Assert.True(toggle.HandleInput(InputEvent.Press(ConsoleKey.LeftArrow), Context()));
            Assert.True(toggle.HandleInput(InputEvent.Press(ConsoleKey.Spacebar), Context()));
            Assert.True(toggle.HandleInput(InputEvent.Press(ConsoleKey.Enter), Context()));
            Assert.False(toggle.Value);
            Assert.Equal(false, toggle.CurrentValue);
            Assert.Equal(4, changes.Count);
            Assert.All(changes, change => Assert.Equal("enabled", change.Name));
            Assert.Equal(false, changes[0].PreviousValue);
            Assert.Equal(true, changes[0].Value);
            Assert.Equal(false, changes[3].Value);
        }

        /// <summary>Disabled inputs ignore keyboard edits and do not emit events.</summary>
        [Fact]
        public void DisabledToggle_IgnoresActivation() {
            Toggle toggle = new Toggle("enabled", true) { Enabled = false };
            int changes = 0;
            toggle.Changed += (_, __) => changes++;
            Assert.False(toggle.HandleInput(InputEvent.Press(ConsoleKey.Spacebar), Context()));
            Assert.True(toggle.Value);
            Assert.Equal(0, changes);
        }

        /// <summary>The layout toggle retains the canonical checkbox and form display text.</summary>
        [Theory]
        [InlineData(GlyphTier.Ascii, "[x]")]
        [InlineData(GlyphTier.Basic, "[√]")]
        [InlineData(GlyphTier.Full, "[✓]")]
        public void Render_UsesCheckboxTierAndConfiguredText(GlyphTier tier, string checkbox) {
            Toggle toggle = new Toggle("enabled", true) { OnText = "Active" };
            CellBuffer buffer = new CellBuffer(40, 5);
            toggle.Render(new Region(buffer), new RenderContext(TerminalProfile.Fixed(40, 5, ColorDepth.None, tier), Theme.Dark));
            Assert.Contains(checkbox + " Active", buffer.ToPlainText());
        }

        private static RenderContext Context() => new RenderContext(TerminalProfile.Fixed(60, 20, ColorDepth.None), Theme.Dark);
    }
}
