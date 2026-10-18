using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConsoleMonster.Output;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    /// <summary>Appending to existing logs keeps their encoding (Write-Color writes UTF-16 "Unicode" logs).</summary>
    public sealed class ConsoleLogEncodingTests : IDisposable {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "cm-log-" + Guid.NewGuid().ToString("N"));

        public ConsoleLogEncodingTests() {
            Directory.CreateDirectory(_folder);
        }

        public void Dispose() {
            try {
                Directory.Delete(_folder, true);
            } catch (IOException) {
                // Best effort.
            }
        }

        private static ConsoleLogRecord Record(string message) => new ConsoleLogRecord(new DateTime(2026, 10, 17, 12, 0, 0), message);

        [Fact]
        public void Append_ExistingUtf16Log_StaysUtf16() {
            string path = Path.Combine(_folder, "writecolor.log");
            File.WriteAllText(path, "[2026-09-30 08:00:00] Zażółć gęślą jaźń" + Environment.NewLine, new UnicodeEncoding(false, true));
            ConsoleLogFile log = new ConsoleLogFile(path);
            Assert.True(log.Append(Record("Łódź ✓")));
            byte[] bytes = File.ReadAllBytes(path);
            Assert.Equal(0xFF, bytes[0]);
            Assert.Equal(0xFE, bytes[1]);
            string text = Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            Assert.Equal("[2026-09-30 08:00:00] Zażółć gęślą jaźń" + Environment.NewLine + "[2026-10-17 12:00:00] Łódź ✓" + Environment.NewLine, text);
        }

        [Theory]
        [InlineData("bigendianunicode")]
        [InlineData("utf32")]
        [InlineData("utf8BOM")]
        public void Append_ExistingLogWithAByteOrderMark_KeepsItsEncoding(string name) {
            string path = Path.Combine(_folder, name + ".log");
            Encoding encoding = ConsoleLogFile.ResolveEncoding(name);
            File.WriteAllText(path, "first" + Environment.NewLine, encoding);
            ConsoleLogFile log = new ConsoleLogFile(path) { IncludeTime = false };
            Assert.True(log.Append(Record("second ą")));
            Assert.Equal("first" + Environment.NewLine + "second ą" + Environment.NewLine, File.ReadAllText(path, encoding));
            Assert.Equal(encoding.CodePage, ConsoleLogFile.DetectEncoding(path)!.CodePage);
        }

        [Fact]
        public void Append_Utf16LogWithoutAByteOrderMark_IsDetected() {
            string path = Path.Combine(_folder, "nobom.log");
            File.WriteAllBytes(path, Encoding.Unicode.GetBytes("first line" + Environment.NewLine));
            Assert.Equal(1200, ConsoleLogFile.DetectEncoding(path)!.CodePage);
            new ConsoleLogFile(path) { IncludeTime = false }.Append(Record("second"));
            Assert.Equal("first line" + Environment.NewLine + "second" + Environment.NewLine, Encoding.Unicode.GetString(File.ReadAllBytes(path)));
        }

        [Fact]
        public void Append_NewFile_UsesTheConfiguredEncoding() {
            string utf8 = Path.Combine(_folder, "new-utf8.log");
            new ConsoleLogFile(utf8) { IncludeTime = false }.Append(Record("ą"));
            Assert.Equal(new UTF8Encoding(false).GetBytes("ą" + Environment.NewLine), File.ReadAllBytes(utf8));
            Assert.Null(ConsoleLogFile.DetectEncoding(utf8));

            string unicode = Path.Combine(_folder, "new-unicode.log");
            new ConsoleLogFile(unicode) { IncludeTime = false, Encoding = ConsoleLogFile.ResolveEncoding("Unicode") }.Append(Record("ą"));
            byte[] bytes = File.ReadAllBytes(unicode);
            Assert.Equal(0xFF, bytes[0]);
            Assert.Equal(0xFE, bytes[1]);
            Assert.Equal("ą" + Environment.NewLine, Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2));
        }

        [Fact]
        public void Append_ExplicitEncodingThatContradictsTheFile_WarnsOnceAndKeepsTheFileEncoding() {
            string path = Path.Combine(_folder, "mismatch.log");
            File.WriteAllText(path, "first" + Environment.NewLine, new UnicodeEncoding(false, true));
            List<string> warnings = new List<string>();
            ConsoleLogFile log = new ConsoleLogFile(path) {
                IncludeTime = false,
                Encoding = ConsoleLogFile.ResolveEncoding("utf8"),
                EncodingSpecified = true,
                Warning = warnings.Add
            };
            log.Append(Record("second"));
            log.Append(Record("third"));
            Assert.Single(warnings);
            Assert.Contains("utf-16", warnings[0]);
            Assert.Equal("first" + Environment.NewLine + "second" + Environment.NewLine + "third" + Environment.NewLine, File.ReadAllText(path, Encoding.Unicode));
        }

        [Fact]
        public void Append_KeepExistingEncodingOff_UsesTheConfiguredEncoding() {
            string path = Path.Combine(_folder, "forced.log");
            File.WriteAllText(path, "a", new UnicodeEncoding(false, true));
            new ConsoleLogFile(path) { IncludeTime = false, KeepExistingEncoding = false }.Append(Record("b"));
            byte[] bytes = File.ReadAllBytes(path);
            Assert.Equal((byte)'b', bytes[4]);
        }
    }
}
