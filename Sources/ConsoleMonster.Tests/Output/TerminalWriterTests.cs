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
    }
}
