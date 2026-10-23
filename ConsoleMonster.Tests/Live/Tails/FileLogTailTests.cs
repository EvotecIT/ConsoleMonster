using System;
using System.IO;
using System.Linq;
using System.Text;
using ConsoleMonster.Live.Tails;
using Xunit;

namespace ConsoleMonster.Tests.Live.Tails {
    public sealed class FileLogTailTests : IDisposable {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "ConsoleMonster-tails-" + Guid.NewGuid().ToString("N"));
        private readonly string _path;

        public FileLogTailTests() {
            Directory.CreateDirectory(_directory);
            _path = Path.Combine(_directory, "current.log");
            File.WriteAllBytes(_path, Array.Empty<byte>());
        }

        [Fact]
        public void DefaultHistoryReturnsLastTenCompletedLinesThenOnlyNewAppends() {
            Write(string.Join("\n", Enumerable.Range(1, 15)) + "\npending");
            using (FileLogTail tail = new FileLogTail(_path)) {
                Assert.Equal(Enumerable.Range(6, 10).Select(i => i.ToString()), tail.ReadAvailable().Select(e => e.Text));
                Assert.Empty(tail.ReadAvailable());
                Append(" complete\nnext\n");
                Assert.Equal(new[] { "pending complete", "next" }, tail.ReadAvailable().Select(e => e.Text));
                Assert.Empty(tail.ReadAvailable());
            }
        }

        [Fact]
        public void ZeroHistorySkipsCompletedHistoryButRetainsExistingUnfinishedLine() {
            Write("old\r\nunfinished");
            using (FileLogTail tail = new FileLogTail(_path, initialLineCount: 0)) {
                Assert.Empty(tail.ReadAvailable());
                Append(" done\nnew\n");
                Assert.Equal(new[] { "unfinished done", "new" }, tail.ReadAvailable().Select(e => e.Text));
                Assert.Empty(tail.ReadAvailable());
            }
        }

        [Fact]
        public void MixedTerminatorsAndCrLfAcrossPollsEmitEachLineOnce() {
            using (FileLogTail tail = new FileLogTail(_path)) {
                Append("a\r");
                Assert.Equal("a", Assert.Single(tail.ReadAvailable()).Text);
                Assert.Empty(tail.ReadAvailable());
                Append("\nb\nc\rd\r\n\npart");
                Assert.Equal(new[] { "b", "c", "d", "" }, tail.ReadAvailable().Select(e => e.Text));
                Assert.Empty(tail.ReadAvailable());
                Append("ial");
                Assert.Empty(tail.ReadAvailable());
                Append("\n");
                Assert.Equal("partial", Assert.Single(tail.ReadAvailable()).Text);
            }
        }

        [Fact]
        public void Utf8ScalarBytesSplitAcrossPollsAndBufferBoundaryRemainIntact() {
            using (FileLogTail tail = new FileLogTail(_path)) {
                string text = new string('x', 8191) + "🙂日本語é";
                byte[] bytes = Encoding.UTF8.GetBytes(text + "\n");
                Append(bytes.Take(8192).ToArray());
                Assert.Empty(tail.ReadAvailable());
                foreach (byte value in bytes.Skip(8192).Take(bytes.Length - 8193)) {
                    Append(new[] { value });
                    Assert.Empty(tail.ReadAvailable());
                }
                Append(new[] { bytes[bytes.Length - 1] });
                Assert.Equal(text, Assert.Single(tail.ReadAvailable()).Text);
                Assert.Empty(tail.ReadAvailable());
            }
        }

        [Theory]
        [InlineData("utf8")]
        [InlineData("utf16-le")]
        [InlineData("utf16-be")]
        [InlineData("utf32-le")]
        [InlineData("utf32-be")]
        public void SplitBomOverridesFallbackAndSplitEncodedTextSurvives(string encodingName) {
            Encoding encoding = EncodingFor(encodingName);
            using (FileLogTail tail = new FileLogTail(_path, encoding: Encoding.ASCII)) {
                byte[] bytes = encoding.GetPreamble().Concat(encoding.GetBytes("[WARN] 🙂é\r\n")).ToArray();
                // Single-byte polls cover split UTF-8 scalars, UTF-16 code units and surrogate pairs, and BOM prefixes.
                string[] emitted = Array.Empty<string>();
                foreach (byte value in bytes) {
                    Append(new[] { value });
                    emitted = emitted.Concat(tail.ReadAvailable().Select(e => e.Text)).ToArray();
                }
                Assert.Equal(new[] { "[WARN] 🙂é" }, emitted);
                Assert.Empty(tail.ReadAvailable());
            }
        }

        [Theory]
        [InlineData("utf8")]
        [InlineData("utf16-le")]
        [InlineData("utf16-be")]
        [InlineData("utf32-le")]
        [InlineData("utf32-be")]
        public void InitialBomHistoryUsesPlatformDecoder(string encodingName) {
            Encoding encoding = EncodingFor(encodingName);
            File.WriteAllBytes(_path, encoding.GetPreamble().Concat(encoding.GetBytes("古い\n[INFO] 新しい\n")).ToArray());
            using (FileLogTail tail = new FileLogTail(_path, 1, Encoding.ASCII)) {
                TailedLogEntry entry = Assert.Single(tail.ReadAvailable());
                Assert.Equal("[INFO] 新しい", entry.Text);
                Assert.Equal("info", entry.Level);
                Assert.Empty(tail.ReadAvailable());
            }
        }

        [Fact]
        public void ExplicitBomlessEncodingHandlesSplitUtf16Writes() {
            Encoding encoding = new UnicodeEncoding(true, false);
            using (FileLogTail tail = new FileLogTail(_path, encoding: encoding)) {
                foreach (byte value in encoding.GetBytes("é🙂\n")) { Append(new[] { value }); }
                Assert.Equal("é🙂", Assert.Single(tail.ReadAvailable()).Text);
            }
        }

        [Fact]
        public void ByteCapBoundsEachPollAndPreservesBacklogAndSplitLines() {
            using (FileLogTail tail = new FileLogTail(_path, maxBytesPerRead: 4)) {
                Append("a\nb\nc\nd\ne");
                Assert.Equal(new[] { "a", "b" }, tail.ReadAvailable().Select(e => e.Text));
                Append("\nf\ng\n");
                Assert.Equal(new[] { "c", "d" }, tail.ReadAvailable().Select(e => e.Text));
                Assert.Equal(new[] { "e", "f" }, tail.ReadAvailable().Select(e => e.Text));
                Assert.Equal("g", Assert.Single(tail.ReadAvailable()).Text);
                Assert.Empty(tail.ReadAvailable());
            }
        }

        [Fact]
        public void TruncationDiscardsOldPartialAndResetsDecoderAndCrState() {
            Write("history long enough\r\nold unfinished");
            using (FileLogTail tail = new FileLogTail(_path, 0)) {
                Assert.Empty(tail.ReadAvailable());
                Write("\nnew\n");
                Assert.Equal(new[] { "", "new" }, tail.ReadAvailable().Select(e => e.Text));
                Append(new byte[] { 0xF0, 0x9F });
                Assert.Empty(tail.ReadAvailable());
                Write("ok\n");
                Assert.Equal("ok", Assert.Single(tail.ReadAvailable()).Text);
                Assert.Empty(tail.ReadAvailable());
            }
        }

        [Fact]
        public void TruncateAndRegrowBeyondConsumedLengthResetsFromChangedCheckpoint() {
            Write("old\n");
            using (FileLogTail tail = new FileLogTail(_path)) {
                Assert.Equal("old", Assert.Single(tail.ReadAvailable()).Text);
                Write("replacement is longer\n");
                Assert.Equal("replacement is longer", Assert.Single(tail.ReadAvailable()).Text);
                Assert.Empty(tail.ReadAvailable());
            }
        }

        [Fact]
        public void RenameWithMissingIntervalThenReplacementFollowsNewPathOnly() {
            Write("old\npartial");
            using (FileLogTail tail = new FileLogTail(_path)) {
                Assert.Equal("old", Assert.Single(tail.ReadAvailable()).Text);
                string rotated = Path.Combine(_directory, "rotated.log");
                File.Move(_path, rotated);
                File.AppendAllText(rotated, " should not leak\n", new UTF8Encoding(false));
                Assert.Empty(tail.ReadAvailable());
                Write("fresh\n");
                Assert.Equal("fresh", Assert.Single(tail.ReadAvailable()).Text);
                Assert.Empty(tail.ReadAvailable());
            }
        }

        [Fact]
        public void ReplacementWithIdenticalContentLengthAndTimestampStillResetsByIdentity() {
            Write("same\n");
            using (FileLogTail tail = new FileLogTail(_path)) {
                Assert.Equal("same", Assert.Single(tail.ReadAvailable()).Text);
                DateTime created = File.GetCreationTimeUtc(_path);
                DateTime modified = File.GetLastWriteTimeUtc(_path);
                string replacement = Path.Combine(_directory, "replacement.log");
                File.WriteAllText(replacement, "same\n", new UTF8Encoding(false));
                File.SetCreationTimeUtc(replacement, created);
                File.SetLastWriteTimeUtc(replacement, modified);
                File.Delete(_path);
                File.Move(replacement, _path);
                Assert.Equal("same", Assert.Single(tail.ReadAvailable()).Text);
                Assert.Empty(tail.ReadAvailable());
            }
        }

        [Fact]
        public void ReplacementRedetectsBomAndDropsPendingBytes() {
            using (FileLogTail tail = new FileLogTail(_path)) {
                Append(new byte[] { 0xF0, 0x9F });
                Assert.Empty(tail.ReadAvailable());
                File.Move(_path, Path.Combine(_directory, "old.log"));
                File.WriteAllBytes(_path, Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("new é\n")).ToArray());
                Assert.Equal("new é", Assert.Single(tail.ReadAvailable()).Text);
                Assert.Empty(tail.ReadAvailable());
            }
        }

        [Fact]
        public void OpenWriterCanAppendAndDisposalReleasesOwnedHandle() {
            FileLogTail tail = new FileLogTail(_path);
            try {
                using (FileStream writer = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete)) {
                    byte[] bytes = Encoding.UTF8.GetBytes("live\n");
                    writer.Write(bytes, 0, bytes.Length);
                    writer.Flush();
                    Assert.Equal("live", Assert.Single(tail.ReadAvailable()).Text);
                }
                tail.Dispose();
                tail.Dispose();
                using (FileStream exclusive = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                    Assert.True(exclusive.CanWrite);
                }
                Assert.Throws<ObjectDisposedException>(() => tail.ReadAvailable());
            } finally {
                tail.Dispose();
            }
        }

        [Fact]
        public void InvalidArgumentsAndMissingInitialFileFailWithoutOwningHandle() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileLogTail(_path, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileLogTail(_path, maxBytesPerRead: 0));
            Assert.Throws<FileNotFoundException>(() => new FileLogTail(Path.Combine(_directory, "missing.log")));
            using (FileStream exclusive = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                Assert.Equal(0, exclusive.Length);
            }
        }

        private static Encoding EncodingFor(string name) => name == "utf8" ? new UTF8Encoding(true)
            : name.StartsWith("utf32", StringComparison.Ordinal) ? new UTF32Encoding(name == "utf32-be", true)
            : new UnicodeEncoding(name == "utf16-be", true);

        private void Write(string text) => File.WriteAllBytes(_path, new UTF8Encoding(false).GetBytes(text));

        private void Append(string text) => Append(Encoding.UTF8.GetBytes(text));

        private void Append(byte[] bytes) {
            using (FileStream writer = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete)) {
                writer.Write(bytes, 0, bytes.Length);
            }
        }

        public void Dispose() => Directory.Delete(_directory, true);
    }
}
