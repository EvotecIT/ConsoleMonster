using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Input;
using Xunit;

namespace ConsoleMonster.Tests.Input {
    public sealed class VtInputDecoderTests {
        [Theory]
        [InlineData("\u001b\u007f", ConsoleKey.Backspace, KeyModifiers.Alt)]
        [InlineData("\u001bb", ConsoleKey.B, KeyModifiers.Alt)]
        [InlineData("\u001bf", ConsoleKey.F, KeyModifiers.Alt)]
        [InlineData("\u001bOM", ConsoleKey.Enter, KeyModifiers.None)]
        public void TerminalEditingSequencesSurviveEveryChunkBoundary(string sequence, ConsoleKey key, KeyModifiers modifiers) {
            for (int split = 0; split <= sequence.Length; split++) {
                VtInputDecoder decoder = new VtInputDecoder();
                List<InputEvent> events = new List<InputEvent>();
                decoder.Feed(sequence.Substring(0, split), events);
                decoder.Feed(sequence.Substring(split), events);
                Assert.True(Assert.Single(events).Is(key, modifiers));
            }
        }

        [Fact]
        public void DelayedOscBackgroundReplyNeverBecomesText() {
            const string reply = "\u001b]11;rgb:ffff/ffff/ffff\u001b\\";
            for (int split = 2; split < reply.Length; split++) {
                VtInputDecoder decoder = new VtInputDecoder { IgnoreBackgroundReplies = true };
                List<InputEvent> events = new List<InputEvent>();
                decoder.Feed(reply.Substring(0, split), events);
                decoder.Flush(events);
                decoder.Feed(reply.Substring(split) + "hello", events);
                Assert.Equal("hello", new string(events.ConvertAll(e => e.Char).ToArray()));
            }
            InputEvent altBracket = Assert.Single(Decode("\u001b]"));
            Assert.Equal(']', altBracket.Char);
            Assert.Equal(KeyModifiers.Alt, altBracket.Modifiers);
        }

        [Fact]
        public void AbandonedBackgroundReplyDoesNotTrapSubsequentTyping() {
            VtInputDecoder decoder = new VtInputDecoder { IgnoreBackgroundReplies = true };
            List<InputEvent> events = new List<InputEvent>();
            decoder.Feed("\u001b]11;rgb:ffff/", events);
            System.Threading.Thread.Sleep(300);
            decoder.Flush(events);
            Assert.False(decoder.HasPending);
            decoder.Feed("abc\r\u0003", events);
            Assert.Equal("abc", new string(events.FindAll(e => e.IsText).ConvertAll(e => e.Char).ToArray()));
            Assert.True(events[3].Is(ConsoleKey.Enter));
            Assert.True(events[4].Is(ConsoleKey.C, KeyModifiers.Control));
        }

        [Theory]
        [InlineData("\u001b]11;rgb:ffff/hello\r\u0003", "hello")]
        [InlineData("\u001b]11;rgb:ffff/\r\u0003", "")]
        public void MalformedBackgroundReplyPreservesUnrelatedKeys(string text, string expected) {
            VtInputDecoder decoder = new VtInputDecoder { IgnoreBackgroundReplies = true };
            List<InputEvent> events = new List<InputEvent>();
            decoder.Feed(text, events);
            Assert.Equal(expected, new string(events.FindAll(e => e.IsText).ConvertAll(e => e.Char).ToArray()));
            Assert.True(events[events.Count - 2].Is(ConsoleKey.Enter));
            Assert.True(events[events.Count - 1].Is(ConsoleKey.C, KeyModifiers.Control));
            Assert.False(decoder.HasPending);
        }

        [Fact]
        public void OverlongBackgroundReplyCannotKeepBufferingInput() {
            VtInputDecoder decoder = new VtInputDecoder { IgnoreBackgroundReplies = true };
            List<InputEvent> events = new List<InputEvent>();
            decoder.Feed("\u001b]11;rgb:" + new string('a', 4096) + "\r\u0003", events);
            Assert.False(decoder.HasPending);
            Assert.True(events[events.Count - 2].Is(ConsoleKey.Enter));
            Assert.True(events[events.Count - 1].Is(ConsoleKey.C, KeyModifiers.Control));
        }

        [Fact]
        public void Feed_PrintableAndControls_NormalizesEvents() {
            List<InputEvent> events = Decode("a!\r\n\t\b\u007f\0\u0001\u001a\u001c\u001f");
            Assert.Equal(9, events.Count);
            Assert.True(events[0].IsChar('a'));
            Assert.True(events[1].IsChar('!'));
            Assert.True(events[2].Is(ConsoleKey.Enter));
            Assert.True(events[3].Is(ConsoleKey.Tab));
            Assert.True(events[4].Is(ConsoleKey.Backspace));
            Assert.True(events[5].Is(ConsoleKey.Backspace));
            Assert.True(events[6].Is(ConsoleKey.Spacebar, KeyModifiers.Control));
            Assert.True(events[7].Is(ConsoleKey.A, KeyModifiers.Control));
            Assert.True(events[8].Is(ConsoleKey.Z, KeyModifiers.Control));
        }

        [Fact]
        public void Feed_CrLfInSameCall_EmitsOneEnter() {
            Assert.Single(Decode("\r\n"));
        }

        [Fact]
        public void Feed_ControlRange_MapsEveryByteToItsDefinedEvent() {
            StringBuilder input = new StringBuilder();
            for (int value = 1; value <= 0x1a; value++) input.Append((char)value);
            List<InputEvent> events = Decode(input.ToString());
            Assert.Equal(0x1a, events.Count);
            for (int value = 1; value <= 0x1a; value++) {
                InputEvent actual = events[value - 1];
                if (value == 8) Assert.True(actual.Is(ConsoleKey.Backspace));
                else if (value == 9) Assert.True(actual.Is(ConsoleKey.Tab));
                else if (value == 10 || value == 13) Assert.True(actual.Is(ConsoleKey.Enter));
                else Assert.True(actual.Is((ConsoleKey)((int)ConsoleKey.A + value - 1), KeyModifiers.Control));
            }
        }

        [Fact]
        public void Feed_SurrogatePairAndUnpairedSurrogates_EmitsOnlyPairAsPaste() {
            List<InputEvent> events = Decode("x\ud83d\ude03\ud800y\udc00");
            Assert.Equal(3, events.Count);
            Assert.True(events[0].IsChar('x'));
            Assert.Equal(InputEventKind.Paste, events[1].Kind);
            Assert.Equal("😃", events[1].Text);
            Assert.True(events[2].IsChar('y'));
        }

        [Fact]
        public void Feed_SurrogatePairSplitAcrossCalls_EmitsSinglePaste() {
            VtInputDecoder decoder = new VtInputDecoder();
            List<InputEvent> events = new List<InputEvent>();
            decoder.Feed("\ud83d", events);
            Assert.True(decoder.HasPending);
            decoder.Feed("\ude03", events);
            Assert.False(decoder.HasPending);
            Assert.Equal("😃", Assert.Single(events).Text);
        }

        [Theory]
        [InlineData("\u001b[A", ConsoleKey.UpArrow)]
        [InlineData("\u001b[B", ConsoleKey.DownArrow)]
        [InlineData("\u001b[C", ConsoleKey.RightArrow)]
        [InlineData("\u001b[D", ConsoleKey.LeftArrow)]
        [InlineData("\u001b[H", ConsoleKey.Home)]
        [InlineData("\u001b[F", ConsoleKey.End)]
        [InlineData("\u001bOP", ConsoleKey.F1)]
        [InlineData("\u001bOQ", ConsoleKey.F2)]
        [InlineData("\u001bOR", ConsoleKey.F3)]
        [InlineData("\u001bOS", ConsoleKey.F4)]
        [InlineData("\u001b[P", ConsoleKey.F1)]
        [InlineData("\u001b[Q", ConsoleKey.F2)]
        [InlineData("\u001b[R", ConsoleKey.F3)]
        [InlineData("\u001b[S", ConsoleKey.F4)]
        [InlineData("\u001b[11~", ConsoleKey.F1)]
        [InlineData("\u001b[12~", ConsoleKey.F2)]
        [InlineData("\u001b[13~", ConsoleKey.F3)]
        [InlineData("\u001b[14~", ConsoleKey.F4)]
        [InlineData("\u001b[15~", ConsoleKey.F5)]
        [InlineData("\u001b[17~", ConsoleKey.F6)]
        [InlineData("\u001b[18~", ConsoleKey.F7)]
        [InlineData("\u001b[19~", ConsoleKey.F8)]
        [InlineData("\u001b[20~", ConsoleKey.F9)]
        [InlineData("\u001b[21~", ConsoleKey.F10)]
        [InlineData("\u001b[23~", ConsoleKey.F11)]
        [InlineData("\u001b[24~", ConsoleKey.F12)]
        [InlineData("\u001bOA", ConsoleKey.UpArrow)]
        [InlineData("\u001bOB", ConsoleKey.DownArrow)]
        [InlineData("\u001bOC", ConsoleKey.RightArrow)]
        [InlineData("\u001bOD", ConsoleKey.LeftArrow)]
        [InlineData("\u001bOH", ConsoleKey.Home)]
        [InlineData("\u001bOF", ConsoleKey.End)]
        [InlineData("\u001b[1~", ConsoleKey.Home)]
        [InlineData("\u001b[7~", ConsoleKey.Home)]
        [InlineData("\u001b[2~", ConsoleKey.Insert)]
        [InlineData("\u001b[3~", ConsoleKey.Delete)]
        [InlineData("\u001b[4~", ConsoleKey.End)]
        [InlineData("\u001b[8~", ConsoleKey.End)]
        [InlineData("\u001b[5~", ConsoleKey.PageUp)]
        [InlineData("\u001b[6~", ConsoleKey.PageDown)]
        public void Feed_KeySequence_EmitsExpectedKey(string sequence, ConsoleKey key) {
            Assert.True(Assert.Single(Decode(sequence)).Is(key));
        }

        [Theory]
        [InlineData("\u001b[1;2A", ConsoleKey.UpArrow, KeyModifiers.Shift)]
        [InlineData("\u001b[1;3A", ConsoleKey.UpArrow, KeyModifiers.Alt)]
        [InlineData("\u001b[1;5A", ConsoleKey.UpArrow, KeyModifiers.Control)]
        [InlineData("\u001b[1;6A", ConsoleKey.UpArrow, KeyModifiers.Shift | KeyModifiers.Control)]
        [InlineData("\u001b[1;7A", ConsoleKey.UpArrow, KeyModifiers.Alt | KeyModifiers.Control)]
        [InlineData("\u001b[1;8A", ConsoleKey.UpArrow, KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Control)]
        [InlineData("\u001b[3;2~", ConsoleKey.Delete, KeyModifiers.Shift)]
        public void Feed_ModifiedKeySequence_UsesModifierBitMask(string sequence, ConsoleKey key, KeyModifiers modifiers) {
            Assert.True(Assert.Single(Decode(sequence)).Is(key, modifiers));
        }

        [Fact]
        public void Feed_ShiftTabAndFocusSequences_EmitsNormalizedEvents() {
            List<InputEvent> events = Decode("\u001b[Z\u001b[I\u001b[O");
            Assert.Equal(3, events.Count);
            Assert.True(events[0].Is(ConsoleKey.Tab, KeyModifiers.Shift));
            Assert.Equal(InputEventKind.FocusGained, events[1].Kind);
            Assert.Equal(InputEventKind.FocusLost, events[2].Kind);
        }

        [Fact]
        public void Feed_WindowSizeReport_UsesColumnsAsWidth() {
            InputEvent resized = Assert.Single(Decode("\u001b[8;24;80t"));
            Assert.Equal(InputEventKind.Resize, resized.Kind);
            Assert.Equal(80, resized.Width);
            Assert.Equal(24, resized.Height);
        }

        [Fact]
        public void Feed_CursorAndUnknownSequences_SwallowsSequenceBytes() {
            Assert.Empty(Decode("\u001b[12;34R\u001b[999z\u001b[1;;A\u001b[1\u0001A"));
        }

        [Fact]
        public void Feed_CsiAndSs3SplitAtEveryIndex_EmitsSameEvent() {
            string[] sequences = { "\u001b[A", "\u001b[1;5A", "\u001bOP", "\u001b[8;24;80t", "\u001b[200~hello\u001b[201~" };
            foreach (string sequence in sequences) {
                List<InputEvent> expected = Decode(sequence);
                for (int split = 1; split < sequence.Length; split++) {
                    VtInputDecoder decoder = new VtInputDecoder();
                    List<InputEvent> actual = new List<InputEvent>();
                    decoder.Feed(sequence.Substring(0, split), actual);
                    decoder.Feed(sequence.Substring(split), actual);
                    AssertEvents(expected, actual);
                    Assert.False(decoder.HasPending);
                }
            }
        }

        [Fact]
        public void Feed_BracketedPasteWithEscapeLikeText_PreservesExactText() {
            string value = "before\u001b[A\r\n\u001b[200~inside";
            InputEvent pasted = Assert.Single(Decode("\u001b[200~" + value + "\u001b[201~"));
            Assert.Equal(InputEventKind.Paste, pasted.Kind);
            Assert.Equal(value, pasted.Text);
        }

        [Fact]
        public void Feed_IncompletePasteAcrossCalls_RemainsPendingUntilTerminator() {
            VtInputDecoder decoder = new VtInputDecoder();
            List<InputEvent> events = new List<InputEvent>();
            decoder.Feed("\u001b[200~one\u001b[20", events);
            decoder.Flush(events);
            Assert.True(decoder.HasPending);
            Assert.Empty(events);
            decoder.Feed("1~", events);
            Assert.False(decoder.HasPending);
            Assert.Equal("one", Assert.Single(events).Text);
        }

        [Fact]
        public void Flush_LoneEscapeAndIncompleteIntroducers_ResolvesPlausibleKeys() {
            VtInputDecoder decoder = new VtInputDecoder();
            List<InputEvent> events = new List<InputEvent>();
            decoder.Feed("\u001b", events);
            Assert.True(decoder.HasPending);
            decoder.Flush(events);
            Assert.True(Assert.Single(events).Is(ConsoleKey.Escape));
            decoder.Feed("\u001b[", events);
            decoder.Flush(events);
            Assert.True(events[1].IsChar('[') == false);
            Assert.Equal(KeyModifiers.Alt, events[1].Modifiers);
            Assert.Equal('[', events[1].Char);
            decoder.Feed("\u001bO", events);
            decoder.Flush(events);
            Assert.Equal('O', events[2].Char);
            Assert.Equal(KeyModifiers.Alt, events[2].Modifiers);
        }

        [Fact]
        public void Feed_AltAndControlAltSequences_EmitsModifiers() {
            List<InputEvent> events = Decode("\u001bx\u001b\u0003\u001b\u001b");
            Assert.Equal(4, events.Count);
            Assert.Equal(KeyModifiers.Alt, events[0].Modifiers);
            Assert.Equal('x', events[0].Char);
            Assert.True(events[1].Is(ConsoleKey.C, KeyModifiers.Control | KeyModifiers.Alt));
            Assert.True(events[2].Is(ConsoleKey.Escape));
            Assert.True(events[3].Is(ConsoleKey.Escape));
        }

        [Fact]
        public void Feed_Win32InputMode_HandlesModifiersRepeatAndModifierKeys() {
            List<InputEvent> events = Decode("\u001b[65;0;97;1;0;2_\u001b[65;0;97;0;0;1_\u001b[16;0;0;1;0;1_\u001b[65;0;233;1;15;1_\u001b[65;0;1;1;12;1_");
            Assert.Equal(4, events.Count);
            Assert.True(events[0].IsChar('a'));
            Assert.True(events[1].IsChar('a'));
            Assert.True(events[2].IsChar('é'));
            Assert.True(events[3].Is(ConsoleKey.A, KeyModifiers.Control));
        }

        [Theory]
        [InlineData("\u001b[97;1u", 'a', KeyModifiers.None)]
        [InlineData("\u001b[97;3u", 'a', KeyModifiers.Alt)]
        [InlineData("\u001b[97;5u", 'a', KeyModifiers.Control)]
        public void Feed_KittyCharacter_EmitsCharacterWithModifiers(string sequence, char character, KeyModifiers modifiers) {
            InputEvent input = Assert.Single(Decode(sequence));
            if ((modifiers & KeyModifiers.Control) != 0) {
                Assert.True(input.Is(ConsoleKey.A, KeyModifiers.Control));
            } else {
                Assert.Equal(character, input.Char);
                Assert.Equal(modifiers, input.Modifiers);
            }
        }

        [Fact]
        public void Feed_KittyControlKeys_EmitsNormalizedKeys() {
            List<InputEvent> events = Decode("\u001b[13u\u001b[9u\u001b[27u\u001b[127u\u001b[99;5u");
            Assert.Equal(5, events.Count);
            Assert.True(events[0].Is(ConsoleKey.Enter));
            Assert.True(events[1].Is(ConsoleKey.Tab));
            Assert.True(events[2].Is(ConsoleKey.Escape));
            Assert.True(events[3].Is(ConsoleKey.Backspace));
            Assert.True(events[4].Is(ConsoleKey.C, KeyModifiers.Control));
        }

        [Fact]
        public void Feed_OverlongCsi_DropsSequence() {
            Assert.Empty(Decode("\u001b[" + new string('1', 70) + "A"));
        }

        [Fact]
        public void Feed_OverlongCsiSplitAcrossCalls_DropsUntilFinalByte() {
            VtInputDecoder decoder = new VtInputDecoder();
            List<InputEvent> events = new List<InputEvent>();
            decoder.Feed("\u001b[" + new string('1', 70), events);
            Assert.True(decoder.HasPending);
            decoder.Feed("Aq", events);
            Assert.False(decoder.HasPending);
            Assert.True(Assert.Single(events).IsChar('q'));
        }

        private static List<InputEvent> Decode(string text) {
            VtInputDecoder decoder = new VtInputDecoder();
            List<InputEvent> events = new List<InputEvent>();
            decoder.Feed(text, events);
            if (decoder.HasPending) decoder.Flush(events);
            return events;
        }

        private static void AssertEvents(IReadOnlyList<InputEvent> expected, IReadOnlyList<InputEvent> actual) {
            Assert.Equal(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++) {
                Assert.Equal(expected[i].Kind, actual[i].Kind);
                Assert.Equal(expected[i].Key, actual[i].Key);
                Assert.Equal(expected[i].Char, actual[i].Char);
                Assert.Equal(expected[i].Modifiers, actual[i].Modifiers);
                Assert.Equal(expected[i].Text, actual[i].Text);
                Assert.Equal(expected[i].Width, actual[i].Width);
                Assert.Equal(expected[i].Height, actual[i].Height);
            }
        }
    }
}
