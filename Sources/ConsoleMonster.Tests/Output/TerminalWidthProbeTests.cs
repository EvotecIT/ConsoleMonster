using ConsoleMonster.Output;
using ConsoleMonster.Text;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    public sealed class TerminalWidthProbeTests {
        [Fact]
        public void Derive_WindowsTerminalJoinedEmojiUsesJoinedProfile() {
            TerminalWidthProfile profile = TerminalWidthProbe.Derive(2, 2, 1, 2, 2, 2, 1, 1);
            Assert.Equal(ZwjRendering.Joined, profile.ZwjSequences);
            Assert.Equal(2, profile.EmojiWidth);
            Assert.Equal(1, profile.TextPresentationWidth);
        }

        [Fact]
        public void Derive_ConhostSplitFamilySelectsSplitRendering() {
            TerminalWidthProfile profile = TerminalWidthProbe.Derive(2, 2, 1, 6, 2, 2, 1, 1);
            Assert.Equal(ZwjRendering.Split, profile.ZwjSequences);
        }

        [Fact]
        public void Derive_CjkAmbiguousWideUsesMeasuredTriangleWidth() {
            TerminalWidthProfile profile = TerminalWidthProbe.Derive(2, 2, 1, 2, 2, 2, 2, 1);
            Assert.Equal(2, profile.AmbiguousWidth);
            Assert.Equal("probe", profile.Source);
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(false, false, false)]
        public void CanProbe_RedirectedOrMissingConsoleIsSkipped(bool outputRedirected, bool inputRedirected, bool hasConsole) {
            Assert.False(TerminalWidthProbe.CanProbe(outputRedirected, inputRedirected, hasConsole));
        }

        private static ConsoleMonster.Interop.ConsoleInputRecord Key(char c, bool down) {
            return new ConsoleMonster.Interop.ConsoleInputRecord { EventType = ConsoleMonster.Interop.ConsoleInputRecord.KeyEventType, KeyDown = down ? 1 : 0, UnicodeChar = c, RepeatCount = 1 };
        }

        private static System.Collections.Generic.List<ConsoleMonster.Interop.ConsoleInputRecord> Typed(string text) {
            System.Collections.Generic.List<ConsoleMonster.Interop.ConsoleInputRecord> records = new System.Collections.Generic.List<ConsoleMonster.Interop.ConsoleInputRecord>();
            foreach (char c in text) {
                records.Add(Key(c, true));
                records.Add(Key(c, false));
            }
            return records;
        }

        [Fact]
        public void FindCursorReport_UnrelatedKeystrokesAround_OnlyTheReplyIsMarked() {
            System.Collections.Generic.List<ConsoleMonster.Interop.ConsoleInputRecord> records = Typed("ab");
            records.AddRange(Typed("\u001b[12;7R"));
            records.AddRange(Typed("c"));
            ConsoleMonster.Interop.ConsoleInputRecord[] buffer = records.ToArray();
            bool[] drop = new bool[buffer.Length];
            Assert.True(TerminalWidthProbe.FindCursorReport(buffer, buffer.Length, drop, out int column));
            Assert.Equal(7, column);
            string kept = string.Empty;
            for (int i = 0; i < buffer.Length; i++) {
                if (!drop[i] && buffer[i].IsKeyDown) {
                    kept += buffer[i].UnicodeChar;
                }
            }
            Assert.Equal("abc", kept);
            Assert.Equal(14, System.Linq.Enumerable.Count(drop, d => d));
        }

        [Fact]
        public void FindCursorReport_IncompleteReplyOrPlainEscape_MarksNothing() {
            foreach (string text in new[] { "\u001b[12;", "\u001bx", "hello" }) {
                ConsoleMonster.Interop.ConsoleInputRecord[] buffer = Typed(text).ToArray();
                bool[] drop = new bool[buffer.Length];
                Assert.False(TerminalWidthProbe.FindCursorReport(buffer, buffer.Length, drop, out _));
                Assert.DoesNotContain(true, drop);
            }
        }
    }
}