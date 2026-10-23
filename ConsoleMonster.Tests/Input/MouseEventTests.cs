using System;
using System.Threading;
using ConsoleMonster.Input;
using Xunit;

namespace ConsoleMonster.Tests.Input {
    /// <summary>Protects the normalized public mouse contract and reuse of the scripted source.</summary>
    public sealed class MouseEventTests {
        [Fact]
        public void Mouse_DefaultsToLeftPressAtZeroBasedPosition() {
            InputEvent mouse = InputEvent.Mouse(0, 7);
            Assert.Equal(InputEventKind.Mouse, mouse.Kind);
            Assert.Equal(0, mouse.Column);
            Assert.Equal(7, mouse.Row);
            Assert.Equal(1, mouse.MouseButton);
            Assert.True(mouse.MousePressed);
            Assert.Equal(KeyModifiers.None, mouse.Modifiers);
            Assert.False(mouse.IsText);
            Assert.False(mouse.IsChar('a'));
            Assert.False(mouse.Is(ConsoleKey.Enter));
            Assert.Equal((ConsoleKey)0, mouse.Key);
            Assert.Equal('\0', mouse.Char);
            Assert.Null(mouse.Text);
        }

        [Theory]
        [InlineData(-1, 0, 1, "column")]
        [InlineData(0, -1, 1, "row")]
        [InlineData(0, 0, 0, "button")]
        [InlineData(0, 0, 4, "button")]
        public void Mouse_InvalidPositionOrButton_Throws(int column, int row, int button, string parameter) {
            Assert.Equal(parameter, Assert.Throws<ArgumentOutOfRangeException>(() => InputEvent.Mouse(column, row, button)).ParamName);
        }

        [Fact]
        public void ScriptedInput_MouseReleaseAndKeyboard_RetainsOrderAndModifiers() {
            KeyModifiers modifiers = KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Control;
            InputEvent release = InputEvent.Mouse(int.MaxValue, int.MaxValue, 3, false, modifiers);
            ScriptedInput input = new ScriptedInput(new[] { release, InputEvent.Press(ConsoleKey.Tab), InputEvent.Character('x') });
            Assert.Same(release, input.Read(TimeSpan.Zero, CancellationToken.None));
            Assert.Equal(int.MaxValue, release.Column);
            Assert.Equal(int.MaxValue, release.Row);
            Assert.Equal(3, release.MouseButton);
            Assert.False(release.MousePressed);
            Assert.Equal(modifiers, release.Modifiers);
            Assert.True(input.Read(TimeSpan.Zero, CancellationToken.None)!.Is(ConsoleKey.Tab));
            Assert.True(input.Read(TimeSpan.Zero, CancellationToken.None)!.IsChar('x'));
        }
    }
}
