using System;
using ConsoleMonster.Interactive;
using ConsoleMonster.Output;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    /// <summary>
    /// On Unix, reading Console.CursorLeft writes a cursor-position query and reads the reply from the keyboard input, which
    /// can swallow the keys a prompt is about to read. Without a safe query the position is never read.
    /// </summary>
    public sealed class ConsoleCursorTests {
        [Fact]
        public void TryGetPosition_WithoutASafeQuery_NeverReadsTheCursor() {
            int reads = 0;
            bool known = ConsoleCursor.TryGetPosition(false, () => { reads++; return 5; }, () => { reads++; return 2; }, out int left, out int top);
            Assert.False(known);
            Assert.Equal(0, reads);
            Assert.Equal(-1, left);
            Assert.Equal(-1, top);
        }

        [Fact]
        public void TryGetPosition_WithASafeQuery_ReadsAndToleratesAMissingConsole() {
            Assert.True(ConsoleCursor.TryGetPosition(true, () => 7, () => 3, out int left, out int top));
            Assert.Equal(7, left);
            Assert.Equal(3, top);
            Assert.False(ConsoleCursor.TryGetPosition(true, () => throw new InvalidOperationException("no console"), () => 0, out left, out _));
            Assert.Equal(-1, left);
        }

        [Fact]
        public void InlinePrompt_OnUnix_StartsOnANewLineWithoutQueryingTheCursor() {
            int reads = 0;
            Assert.True(InlineSurface.StartOnNewLine(false, () => { reads++; return 0; }, () => { reads++; return 4; }));
            Assert.Equal(0, reads);
            Assert.True(InlineSurface.StartOnNewLine(true, () => 12, () => 4));
            Assert.False(InlineSurface.StartOnNewLine(true, () => 0, () => 4));
        }

        [Fact]
        public void CanQuery_IsTrueOnlyOnWindows() {
            Assert.Equal(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows), ConsoleCursor.CanQuery);
        }
    }
}
