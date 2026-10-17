using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Input;
using ConsoleMonster.Interop;
using Xunit;

namespace ConsoleMonster.Tests.Input {
    /// <summary>Key chords, event normalization, scripted input, the Windows record translation and the input owner.</summary>
    public sealed class InputCoreTests {
        [Theory]
        [InlineData("Ctrl+K", ConsoleKey.K, KeyModifiers.Control)]
        [InlineData("shift+tab", ConsoleKey.Tab, KeyModifiers.Shift)]
        [InlineData("Alt+Enter", ConsoleKey.Enter, KeyModifiers.Alt)]
        [InlineData("F12", ConsoleKey.F12, KeyModifiers.None)]
        [InlineData("PgDn", ConsoleKey.PageDown, KeyModifiers.None)]
        [InlineData("Ctrl+Shift+Up", ConsoleKey.UpArrow, KeyModifiers.Control | KeyModifiers.Shift)]
        public void Parse_KeyNames_ReturnsKeyChords(string text, ConsoleKey key, KeyModifiers modifiers) {
            KeyChord chord = KeyChord.Parse(text);
            Assert.False(chord.IsCharacter);
            Assert.Equal(key, chord.Key);
            Assert.Equal(modifiers, chord.Modifiers);
        }

        [Fact]
        public void Parse_SingleCharacter_ReturnsCharacterChordThatIgnoresShift() {
            KeyChord chord = KeyChord.Parse("?");
            Assert.True(chord.IsCharacter);
            Assert.True(chord.Matches(InputEvent.Character('?', KeyModifiers.Shift)));
            Assert.False(chord.Matches(InputEvent.Character('/')));
            Assert.Throws<FormatException>(() => KeyChord.Parse("Hyper+K"));
        }

        [Fact]
        public void Matches_ModifiersMustBeExact() {
            KeyChord chord = KeyChord.Parse("Ctrl+K");
            Assert.True(chord.Matches(InputEvent.Press(ConsoleKey.K, KeyModifiers.Control)));
            Assert.False(chord.Matches(InputEvent.Press(ConsoleKey.K, KeyModifiers.Control | KeyModifiers.Shift)));
            Assert.False(chord.Matches(InputEvent.Character('k')));
        }

        [Fact]
        public void Character_DropsShiftAndDerivesKey() {
            InputEvent e = InputEvent.Character('A', KeyModifiers.Shift);
            Assert.Equal(ConsoleKey.A, e.Key);
            Assert.Equal(KeyModifiers.None, e.Modifiers);
            Assert.True(e.IsText);
            Assert.False(InputEvent.Character('a', KeyModifiers.Control).IsText);
            Assert.Equal("Ctrl+Shift+Tab", InputEvent.Press(ConsoleKey.Tab, KeyModifiers.Control | KeyModifiers.Shift).ToString());
            Assert.Equal('\r', InputEvent.Press(ConsoleKey.Enter).Char);
        }

        [Fact]
        public void ScriptedInput_ParsesKeysTextRepeatsAndPastes() {
            IReadOnlyList<InputEvent> events = ScriptedInput.ParseEvents("Down*2 \"ab\" paste:'x y' Ctrl+K resize:80x24 Space");
            Assert.Equal(8, events.Count);
            Assert.True(events[0].Is(ConsoleKey.DownArrow));
            Assert.True(events[1].Is(ConsoleKey.DownArrow));
            Assert.True(events[2].IsChar('a'));
            Assert.True(events[3].IsChar('b'));
            Assert.Equal(InputEventKind.Paste, events[4].Kind);
            Assert.Equal("x y", events[4].Text);
            Assert.True(events[5].Is(ConsoleKey.K, KeyModifiers.Control));
            Assert.Equal(InputEventKind.Resize, events[6].Kind);
            Assert.True(events[7].IsChar(' '));
        }

        [Fact]
        public void ScriptedInput_ReadPastTheEnd_ThrowsInsteadOfHanging() {
            ScriptedInput input = new ScriptedInput("Enter");
            Assert.NotNull(input.Read(TimeSpan.Zero, CancellationToken.None));
            Assert.Throws<InputClosedException>(() => input.Read(TimeSpan.FromSeconds(5), CancellationToken.None));
        }

        [Fact]
        public void ScriptedInput_Paced_ReturnsNullForNonWaitingReads() {
            ScriptedInput input = new ScriptedInput("a b") { Paced = true };
            Assert.Null(input.Read(TimeSpan.Zero, CancellationToken.None));
            Assert.True(input.Read(TimeSpan.FromMilliseconds(10), CancellationToken.None)!.IsChar('a'));
        }

        private static ConsoleInputRecord Key(ushort vk, char c, uint state, bool down = true) {
            return new ConsoleInputRecord { EventType = ConsoleInputNative.KeyEvent, KeyDown = down ? 1 : 0, RepeatCount = 1, VirtualKeyCode = vk, UnicodeChar = c, ControlKeyState = state };
        }

        private static List<InputEvent> Translate(params ConsoleInputRecord[] records) {
            List<InputEvent> output = new List<InputEvent>();
            char high = '\0';
            WindowsConsoleInput.Translate(records, records.Length, output, ref high);
            return output;
        }

        [Fact]
        public void Translate_CtrlLetter_BecomesControlKeyWithoutCharacter() {
            List<InputEvent> events = Translate(Key(0x4B, (char)0x0B, ConsoleInputNative.LeftCtrl), Key(0x4B, (char)0x0B, ConsoleInputNative.LeftCtrl, false));
            Assert.Single(events);
            Assert.True(events[0].Is(ConsoleKey.K, KeyModifiers.Control));
            Assert.False(events[0].IsText);
        }

        [Fact]
        public void Translate_AltGrCharacter_IsPlainText() {
            List<InputEvent> events = Translate(Key(0x45, '€', ConsoleInputNative.RightAlt | ConsoleInputNative.LeftCtrl));
            Assert.Single(events);
            Assert.True(events[0].IsChar('€'));
        }

        [Fact]
        public void Translate_ShiftTabAndModifierKeys() {
            List<InputEvent> events = Translate(Key(0x10, '\0', ConsoleInputNative.ShiftPressed), Key(0x09, '\t', ConsoleInputNative.ShiftPressed));
            Assert.Single(events);
            Assert.True(events[0].Is(ConsoleKey.Tab, KeyModifiers.Shift));
        }

        [Fact]
        public void Translate_BurstOfCharacters_BecomesOnePasteAndKeepsTrailingEnter() {
            List<InputEvent> events = Translate(Key(0x41, 'a', 0), Key(0x42, 'b', 0), Key(0x20, ' ', 0), Key(0x43, 'c', 0), Key(0x0D, '\r', 0));
            Assert.Equal(2, events.Count);
            Assert.Equal(InputEventKind.Paste, events[0].Kind);
            Assert.Equal("ab c", events[0].Text);
            Assert.True(events[1].Is(ConsoleKey.Enter));
        }

        [Fact]
        public void Translate_TwoCharacters_StayTyping() {
            List<InputEvent> events = Translate(Key(0x41, 'a', 0), Key(0x42, 'b', 0));
            Assert.Equal(2, events.Count);
            Assert.True(events[0].IsChar('a'));
        }

        [Fact]
        public void Translate_SurrogatePairAndAltNumpad() {
            List<InputEvent> pair = Translate(Key(0, '\ud83d', 0), Key(0, '\ude00', 0));
            Assert.Single(pair);
            Assert.Equal("😀", pair[0].Text);
            List<InputEvent> numpad = Translate(Key(0x12, 'é', 0, false));
            Assert.Single(numpad);
            Assert.True(numpad[0].IsChar('é'));
        }

        [Fact]
        public void Translate_RepeatCount_RepeatsTheKey() {
            ConsoleInputRecord down = Key(0x28, '\0', 0);
            down.RepeatCount = 3;
            List<InputEvent> events = Translate(down);
            Assert.Equal(3, events.Count);
            Assert.All(events, e => Assert.True(e.Is(ConsoleKey.DownArrow)));
        }

        [Fact]
        public void Acquire_ExplicitSource_IsIndependentOfTheConsoleOwner() {
            ScriptedInput source = new ScriptedInput("Enter");
            using (InputLease lease = ConsoleInput.Acquire(source)) {
                Assert.Same(source, lease.Source);
                Assert.False(ConsoleInput.IsAcquired);
            }
        }
    }

    /// <summary>The console owner is process-wide state, so these tests do not run in parallel with live sessions.</summary>
    [Collection("Live sessions")]
    public sealed class ConsoleOwnerTests {
        [Fact]
        public void Acquire_IsReentrantOnTheOwningThreadAndExclusiveAcrossThreads() {
            ScriptedInput source = new ScriptedInput("Enter") { EndsQuietly = true };
            ConsoleInput.Override = source;
            try {
                using (InputLease outer = ConsoleInput.Acquire()) {
                    using (InputLease inner = ConsoleInput.Acquire()) {
                        Assert.Same(source, inner.Source);
                    }
                    Assert.True(ConsoleInput.IsAcquired);
                    Exception? error = null;
                    Thread other = new Thread(() => {
                        try {
                            using (ConsoleInput.Acquire()) {
                            }
                        } catch (Exception ex) {
                            error = ex;
                        }
                    });
                    other.Start();
                    other.Join();
                    Assert.IsType<InvalidOperationException>(error);
                }
                Assert.False(ConsoleInput.IsAcquired);
            } finally {
                ConsoleInput.Override = null;
            }
        }
    }
}
