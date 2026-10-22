using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    /// <summary>Contracts for synchronous button activation through the shared input surface.</summary>
    public sealed class ButtonTests {
        /// <summary>Enter and Space activate through InputControl and emit one count change and click each.</summary>
        [Fact]
        public void EnterAndSpace_ActivateThroughBaseSurface() {
            Button button = new Button("run", "Run");
            InputControl input = button;
            int clicks = 0;
            List<AppValueChangedEventArgs> changes = new List<AppValueChangedEventArgs>();
            button.Clicked += (sender, _) => { Assert.Same(button, sender); clicks++; };
            button.Changed += (_, change) => changes.Add(change);
            Assert.True(input.HandleInput(InputEvent.Press(ConsoleKey.Enter), Context()));
            Assert.Equal(1, clicks);
            Assert.True(input.HandleInput(InputEvent.Character(' '), Context()));
            Assert.Equal(2, clicks);
            Assert.Equal(2, button.Value);
            Assert.Equal(2, input.CurrentValue);
            Assert.Equal(2, changes.Count);
            Assert.Equal("run", changes[0].Name);
            Assert.Equal(0, changes[0].PreviousValue);
            Assert.Equal(1, changes[0].Value);
            Assert.Equal(1, changes[1].PreviousValue);
            Assert.Equal(2, changes[1].Value);
            Assert.False(input.HandleInput(InputEvent.Press(ConsoleKey.DownArrow), Context()));
            button.Enabled = false;
            Assert.False(input.HandleInput(InputEvent.Press(ConsoleKey.Enter), Context()));
            Assert.Equal(2, clicks);
            Assert.Equal(2, button.Value);
        }

        /// <summary>A callback can synchronously wait for a different thread reading the control.</summary>
        [Fact]
        public void Clicked_RunsSynchronouslyOutsideControlLock() {
            Button button = new Button("run");
            bool completed = false;
            button.Clicked += (_, __) => {
                int value = -1;
                Thread reader = new Thread(() => value = button.Value) { IsBackground = true };
                reader.Start();
                Assert.True(reader.Join(TimeSpan.FromSeconds(5)), "Clicked must not hold the control's state lock.");
                Assert.Equal(1, value);
                completed = true;
            };
            Assert.True(button.HandleInput(InputEvent.Press(ConsoleKey.Enter), Context()));
            Assert.True(completed);
        }

        /// <summary>The public button keeps the existing bracketed focus treatment in a monochrome terminal.</summary>
        [Fact]
        public void Render_UsesCanonicalButtonCaption() {
            Button button = new Button("run", "Apply");
            button.Focus();
            CellBuffer buffer = new CellBuffer(40, 5);
            button.Render(new Region(buffer), Context());
            Assert.Contains("[Apply]", buffer.ToPlainText());
        }

        private static RenderContext Context() => new RenderContext(TerminalProfile.Fixed(60, 20, ColorDepth.None), Theme.Dark);
    }
}
