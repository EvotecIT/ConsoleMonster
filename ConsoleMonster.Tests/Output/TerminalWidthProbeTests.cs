using ConsoleMonster.Output;
using ConsoleMonster.Text;
using System;
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

        [Fact]
        public void Invalidate_ClearsProbeCacheWithoutTouchingConsoleInput() {
            TerminalWidthProbe.TryProbe(new NonConsoleWriter(), TimeSpan.FromMilliseconds(1), out _, out _);
            Assert.True(TerminalWidthProbe.HasCachedResult);
            TerminalWidthProbe.Invalidate();
            Assert.False(TerminalWidthProbe.HasCachedResult);
        }

        private sealed class NonConsoleWriter : ITerminalWriter {
            public bool IsConsole => false;
            public void Write(string text) { }
            public void Flush() { }
            public void Dispose() { }
        }
    }
}
