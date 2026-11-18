using System;
using System.IO;
using System.Text;
using ConsoleMonster.Output;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    public sealed class TerminalWriterTests {
        [Fact]
        public void StreamWriterBuffersAndWritesUtf8WithoutBom() {
            using (MemoryStream stream = new MemoryStream()) {
                using (StreamTerminalWriter writer = new StreamTerminalWriter(stream)) {
                    writer.Write("Zażółć 中文 ");
                    writer.Write("\uD83D");
                    writer.Write("\uDE00\u001b[31m\r\n");
                    Assert.Equal(0, stream.Length);
                    writer.Flush();
                    byte[] expected = new UTF8Encoding(false).GetBytes("Zażółć 中文 😀\u001b[31m\r\n");
                    Assert.Equal(expected, stream.ToArray());
                    writer.Flush();
                    Assert.Equal(expected, stream.ToArray());
                    Assert.False(writer.IsConsole);
                }
                Assert.True(stream.CanWrite);
            }
        }

        [Fact]
        public void DisposeFlushesOnceAndHonorsStreamOwnership() {
            MemoryStream stream = new MemoryStream();
            StreamTerminalWriter writer = new StreamTerminalWriter(stream, true);
            writer.Write("pending");
            writer.Dispose();
            writer.Dispose();
            Assert.Equal(Encoding.UTF8.GetBytes("pending"), stream.ToArray());
            Assert.False(stream.CanWrite);
            Assert.Throws<ObjectDisposedException>(() => writer.Write("late"));
            Assert.Throws<ObjectDisposedException>(() => writer.Flush());
        }

        [Fact]
        public void ConsoleChunksNeverSplitASurrogatePair() {
            string text = new string('a', 8191) + "😀" + new string('b', 8191) + "👩‍💻";
            StringBuilder rebuilt = new StringBuilder();
            int offset = 0;
            while (offset < text.Length) {
                int count = ConsoleHandleWriter.ChunkLength(text, offset);
                Assert.InRange(count, 1, 8192);
                Assert.False(char.IsHighSurrogate(text[offset + count - 1]));
                Assert.False(char.IsLowSurrogate(text[offset]));
                rebuilt.Append(text, offset, count);
                offset += count;
            }
            Assert.Equal(text, rebuilt.ToString());
        }

        [Theory]
        [InlineData(false, true, 28591, 28591)]
        [InlineData(false, true, 1200, 1200)]
        [InlineData(true, true, 28591, 65001)]
        [InlineData(false, false, 28591, 65001)]
        public void RedirectedOutputUsesTheConsoleEncodingOnlyOnWindowsOutsideCi(bool ci, bool windows, int console, int expectedCodePage) {
            Encoding chosen = StreamEncoding.Choose(ci, windows, Encoding.GetEncoding(console));

            Assert.Equal(expectedCodePage, chosen.CodePage);
        }

        [Fact]
        public void ConsoleEncodingReplacesCharactersItCannotCarryInsteadOfThrowing() {
            Encoding strict = Encoding.GetEncoding(28591, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

            Encoding chosen = StreamEncoding.Choose(false, true, strict);

            Assert.Equal(new byte[] { 0x61, 0x3F, 0x3F }, chosen.GetBytes("a\u4E2D\u6587"));
        }

        [Fact]
        public void StreamWriterWritesInTheGivenEncoding() {
            using (MemoryStream stream = new MemoryStream()) {
                using (StreamTerminalWriter writer = new StreamTerminalWriter(stream, Encoding.GetEncoding(28591))) {
                    writer.Write("caf\u00e9");
                }
                Assert.Equal(new byte[] { 0x63, 0x61, 0x66, 0xE9 }, stream.ToArray());
            }
        }
    }
}
