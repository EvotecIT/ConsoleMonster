using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>Equal-priority key hints retain their declaration order on every supported runtime.</summary>
    public sealed class KeyFooterTests {
        [Fact]
        public void HelpDialogKeepsTheLastBindingInsideItsMeasuredFrame() {
            KeyMapWidget map = new KeyMapWidget(new[] {
                new KeyBinding("move", () => { }, new KeyChord(ConsoleKey.DownArrow)) { Group = "Movement" },
                new KeyBinding("last action", () => { }, new KeyChord('?')) { Group = "Help" }
            }, new PromptLabels());
            string text = Terminal.RenderText(map, TerminalProfile.Fixed(60, 30, ColorDepth.None), Theme.Dark);
            Assert.Contains("last action", text);
            Assert.Contains("any key closes", text);
        }

        /// <summary>Priority sorts groups, while ties stay in declaration order rather than depending on List.Sort.</summary>
        [Fact]
        public void EqualPriorityHints_PreserveDeclarationOrder() {
            KeyFooter footer = new KeyFooter(new[] {
                new KeyBinding("next", () => { }, new KeyChord(ConsoleKey.Tab)),
                new KeyBinding("back", () => { }, new KeyChord(ConsoleKey.Tab, KeyModifiers.Shift)),
                new KeyBinding("quit", () => { }, new KeyChord(ConsoleKey.Escape)),
                new KeyBinding("first", () => { }, new KeyChord(ConsoleKey.F1)) { Order = 0 }
            });
            CellBuffer frame = new CellBuffer(100, 1);
            footer.Render(new Region(frame), new RenderContext(TerminalProfile.Fixed(100, 1, ColorDepth.None), Theme.Dark));
            string text = frame.ToPlainText();
            Assert.True(text.IndexOf("first", StringComparison.Ordinal) < text.IndexOf("next", StringComparison.Ordinal));
            Assert.True(text.IndexOf("next", StringComparison.Ordinal) < text.IndexOf("back", StringComparison.Ordinal));
            Assert.True(text.IndexOf("back", StringComparison.Ordinal) < text.IndexOf("quit", StringComparison.Ordinal));
        }
    }
}
