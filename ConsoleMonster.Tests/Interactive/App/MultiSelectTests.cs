using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Contracts for layout multiple-selection edits and detached event values.</summary>
    public sealed class MultiSelectTests {
        /// <summary>Space and Enter toggle enabled rows, retaining prior event snapshots.</summary>
        [Fact]
        public void KeyboardToggles_PreserveSnapshotsAndRejectDisabled() {
            MultiSelect select = new MultiSelect("targets", new[] {
                new Choice("blocked") { DisabledReason = "Unavailable", Selected = true },
                new Choice("alpha"), new Choice("beta")
            });
            List<AppValueChangedEventArgs> changes = new List<AppValueChangedEventArgs>();
            select.Changed += (_, change) => changes.Add(change);
            Assert.Empty(select.Value);
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.Spacebar), Context()));
            Assert.Empty(changes);
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.DownArrow), Context()));
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.Spacebar), Context()));
            IReadOnlyList<object?> snapshot = select.Value;
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.DownArrow), Context()));
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.Enter), Context()));
            Assert.Equal(new object?[] { "alpha", "beta" }, select.Value);
            Assert.Equal(new object?[] { "alpha" }, snapshot);
            Assert.Equal(new object?[] { "alpha" }, Assert.IsAssignableFrom<IReadOnlyList<object?>>(changes[0].Value));
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.Enter), Context()));
            Assert.Equal(new object?[] { "alpha" }, select.Value);
            Assert.Equal(3, changes.Count);
        }

        /// <summary>Assignments validate atomically, use declaration order, and compare values structurally.</summary>
        [Fact]
        public void ValueAssignment_IsAtomicAndStructurallyCompared() {
            MultiSelect select = new MultiSelect("targets", new[] {
                new Choice("alpha") { Selected = true }, new Choice("beta"),
                new Choice("blocked") { DisabledReason = "Unavailable" }
            });
            int changes = 0;
            select.Changed += (_, __) => changes++;
            select.Value = new object?[] { "beta", "alpha", "beta" };
            Assert.Equal(new object?[] { "alpha", "beta" }, select.Value);
            select.Value = new object?[] { "alpha", "beta" };
            Assert.Throws<ArgumentException>(() => select.Value = new object?[] { "beta", "blocked" });
            Assert.Throws<ArgumentException>(() => select.Value = new object?[] { "missing" });
            Assert.Equal(new object?[] { "alpha", "beta" }, select.Value);
            Assert.Equal(1, changes);
        }

        /// <summary>Filtering and bulk selection share the canonical visible-item and disabled rules.</summary>
        [Fact]
        public void FilteredBulkSelection_OnlyChangesEnabledMatches() {
            MultiSelect select = new MultiSelect("targets", new[] {
                new Choice(1, "Alpha"), new Choice(2, "Beta"),
                new Choice(3, "Beta blocked") { DisabledReason = "Unavailable" }
            });
            Assert.True(select.HandleInput(InputEvent.Paste("Beta"), Context()));
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.A, KeyModifiers.Control), Context()));
            Assert.Equal(new object?[] { 2 }, select.Value);
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.Escape), Context()));
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.A, KeyModifiers.Control), Context()));
            Assert.Equal(new object?[] { 1, 2 }, select.Value);
            Assert.True(select.HandleInput(InputEvent.Press(ConsoleKey.N, KeyModifiers.Control), Context()));
            Assert.Empty(select.Value);
        }

        private static RenderContext Context() => new RenderContext(TerminalProfile.Fixed(60, 20, ColorDepth.None), Theme.Dark);
    }
}
