using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ConsoleMonster.Export;
using ConsoleMonster.Serialization;
using ConsoleMonster.Sessions;
using Xunit;

namespace ConsoleMonster.Tests.Sessions {
    public sealed class SessionLongRecordTests : IDisposable {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "ConsoleMonster-long-record-" + Guid.NewGuid().ToString("N"));
        private SessionRecordingOptions Options() => new SessionRecordingOptions {
            Path = Path.Combine(_folder, "session.cast"), Width = 120, Height = 40, InProcess = true,
            MaxBytes = 8 * 1024 * 1024
        };

        [Fact]
        public void SingleLine_PreservesTwoHundredKiBAndSurrogatePairs() {
            SessionRecordingOptions options = Options();
            string text = new string('a', 16383) + "\U0001f680" + new string('b', 200 * 1024 - 16385);
            using (SessionRecorder recorder = SessionRecorder.Start(options)) {
                // A caller can split a surrogate pair even though the native UTF-8 decoder does not.
                recorder.CapturePowerShellOutput(text.Substring(0, 16384));
                recorder.CapturePowerShellOutput(text.Substring(16384));
                Assert.True(new FileInfo(options.Path!).Length > 1000, "Long output must stream before finalization.");
            }
            Assert.Equal(text, Output(options.Path!));
            Assert.Equal(120, SessionRecording.Read(options.Path!).Width);
        }

        [Fact]
        public void CursorRepaints_PreserveEveryControlAndFinalFrame() {
            SessionRecordingOptions options = Options();
            StringBuilder output = new StringBuilder();
            for (int frame = 0; frame < 50; frame++) {
                output.Append("\u001b[1;1HFrame ").Append(frame.ToString("D2"));
                for (int row = 2; row <= 40; row++) {
                    output.Append("\u001b[").Append(row).Append(";1H\u001b[38;2;").Append(frame).Append(";100;200m")
                        .Append(new string((char)('a' + frame % 26), 119));
                }
            }
            string text = output.ToString();
            using (SessionRecorder recorder = SessionRecorder.Start(options)) {
                for (int at = 0; at < text.Length; at += 4096) {
                    recorder.CapturePowerShellOutput(text.Substring(at, Math.Min(4096, text.Length - at)));
                }
            }
            Assert.True(text.Length > 200 * 1024);
            Assert.Equal(text, Output(options.Path!));
            Assert.StartsWith("Frame 49", ConsoleExporter.ToText(SessionRecording.Read(options.Path!).FinalFrame()));
        }

        [Theory]
        [InlineData(24)]
        [InlineData(256)]
        [InlineData(8192)]
        public void LiteralSecret_StraddlesTheStreamingReadWithoutPersistingItsPrefix(int length) {
            SessionRecordingOptions options = Options();
            string secret = "synthetic-" + new string('q', length - 17) + "-secret";
            options.Redact.Add(secret);
            int half = secret.Length / 2;
            string prefix = new string('a', 65536 - half);
            using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                writer.Append(prefix + secret.Substring(0, half));
                Assert.True(new FileInfo(options.Path!).Length > 1000);
                Assert.DoesNotContain(secret.Substring(0, half), ReadLiveCast(options.Path!));
                writer.Append(secret.Substring(half) + " safe\n");
            }
            Assert.Equal(prefix + "[REDACTED] safe\n", Output(options.Path!));
        }

        [Theory]
        [InlineData(@"(?i)\bsecret-[A-Z]{4}\b", 4)]
        [InlineData(@"(?:secret|private)-[A-Z0-9]{2,4}", 4)]
        [InlineData(@"(?i)\bsecret-[A-Z]{96}\b", 96)]
        public void BoundedRegex_ProtectsASecretCrossingTheCommitPointAndColourCodes(string pattern, int length) {
            SessionRecordingOptions options = Options(); options.Redact.Add(pattern);
            string styled = "sec\u001b[31mret-" + (length == 4 ? "ABCD" : new string('Q', length)) + "\u001b[0m";
            int prefixLength = length > 32 ? 65536 - styled.Length / 2 : 65500;
            string prefix = new string('a', prefixLength - 1) + " ";
            string suffix = "!" + new string('z', 200 * 1024);
            string text = prefix + styled + suffix;
            using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                writer.Append(text.Substring(0, 65536));
                writer.Append(text.Substring(65536));
            }
            Assert.Equal(prefix + "[REDACTED]\u001b[31m\u001b[0m" + suffix, Output(options.Path!));
        }

        [Fact]
        public void BuiltInSecret_ProtectsAnUnterminatedQuoteLongerThanAFlush() {
            SessionRecordingOptions options = Options();
            string prefix = new string('a', 70000) + " ";
            using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                writer.Append(prefix + "password='" + new string('x', 70000));
                Assert.DoesNotContain(new string('x', 100), ReadLiveCast(options.Path!));
                writer.Append("'; visible\n");
            }
            Assert.Equal(prefix + "[REDACTED]; visible\n", Output(options.Path!));
        }

        [Fact]
        public void LiveCastReader_DoesNotPermitAnotherWriterOnWindows() {
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) { return; }
            SessionRecordingOptions options = Options();
            using (SessionCastWriter writer = new SessionCastWriter(options, "test"))
            using (FileStream reader = new FileStream(options.Path!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                Assert.Equal((int)'{', reader.ReadByte());
                Assert.Throws<IOException>(() => {
                    using (FileStream other = new FileStream(options.Path!, FileMode.Open, FileAccess.Write, FileShare.ReadWrite)) { }
                });
            }
        }

        [Theory]
        [InlineData("\u001b]0;first\a", "\u001b]0;second\a")]
        [InlineData("\u001b]0;first\u001b\\", "\u001b]0;second\u001b\\")]
        [InlineData("\u001b7", "\u001b8")]
        [InlineData("\u001b(B", "\u001b)0")]
        [InlineData("\u001bPfirst\u001b\\", "\u001bPsecond\u001b\\")]
        public void LiteralSecret_RemainsPrivateAcrossTerminalControls(string first, string second) {
            SessionRecordingOptions options = Options(); options.Redact.Add("synthetic-secret");
            string prefix = new string('a', 65500) + " ";
            string text = prefix + "synthe" + first + "tic-" + second + "secret" + new string('z', 70000);
            using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                writer.Append(text.Substring(0, 65536)); writer.Append(text.Substring(65536));
            }
            Assert.Equal(prefix + "[REDACTED]" + first + second + new string('z', 70000), Output(options.Path!));
        }

        [Theory]
        [InlineData("\u001b]0;", "\a")]
        [InlineData("\u001bP", "\u001b\\")]
        public void IncompleteTerminalString_DoesNotReleaseTheVisibleSecretPrefix(string start, string end) {
            SessionRecordingOptions options = Options(); options.Redact.Add("synthetic-secret");
            string prefix = new string('a', 65500) + " ";
            string control = start + new string('x', 70000) + end;
            using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                writer.Append(prefix + "synthe" + control.Substring(0, control.Length - end.Length));
                Assert.DoesNotContain("synthe", ReadLiveCast(options.Path!));
                writer.Append(end + "tic-secret safe\n");
            }
            Assert.Equal(prefix + "[REDACTED]" + control + " safe\n", Output(options.Path!));
        }

        [Theory]
        [InlineData("\u001b]0;", "\a", 4093, "")]
        [InlineData("\u001b]0;", "\u001b\\", 65536, "\n")]
        [InlineData("\u001bP", "\u001b\\", 997, "\r\n")]
        [InlineData("\u001bX", "\u001b\\", 70000, "")]
        [InlineData("\u001b^", "\u001b\\", 32768, "\n")]
        [InlineData("\u001b_", "\u001b\\", 8191, "\r\n")]
        public void LongTerminalStrings_RedactAcrossSplitIntroducersBodiesAndTerminators(string start, string end, int chunkSize, string lineEnding) {
            SessionRecordingOptions options = Options(); options.Redact.Add("synthetic-secret");
            string prefix = new string('a', 65500) + " ";
            string body = new string('x', 70000);
            using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                writer.Append(prefix + "synthe");
                foreach (char c in start) { writer.Append(c.ToString()); }
                for (int at = 0; at < body.Length; at += chunkSize) {
                    writer.Append(body.Substring(at, Math.Min(chunkSize, body.Length - at)));
                }
                Assert.DoesNotContain("synthe", ReadLiveCast(options.Path!));
                foreach (char c in end) { writer.Append(c.ToString()); }
                writer.Append("tic-");
                Assert.DoesNotContain("synthe", ReadLiveCast(options.Path!));
                writer.Append("secret safe" + lineEnding);
            }
            Assert.Equal(prefix + "[REDACTED]" + start + body + end + " safe" + lineEnding, Output(options.Path!));
        }

        [Theory]
        [InlineData("\u001b]0;", "\a")]
        [InlineData("\u001bP", "\u001b\\")]
        public void LongTerminalStrings_RedactSecretsInsideTheirNonPrintingBodies(string start, string end) {
            SessionRecordingOptions options = Options(); options.Redact.Add("synthetic-secret");
            string body = new string('x', 70000) + " ";
            using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                writer.Append("visible" + start + body + "synthetic-");
                writer.Append("secret" + end + "tail");
            }
            Assert.Equal("visible" + start + body + "[REDACTED]" + end + "tail", Output(options.Path!));
        }

        [Theory]
        [InlineData("\u001b]0;")]
        [InlineData("\u001bP")]
        public void UnterminatedControlBodies_DoNotRestartAtNestedEscapeIntroducers(string start) {
            RecordingRedactor redactor = new RecordingRedactor(new[] { "synthetic-secret" });
            // Each nested ESC P used to restart an unsuccessful search for the missing terminator.
            string body = string.Concat(Enumerable.Repeat("\u001bP", 35000));
            string text = "left synthe" + start + body;
            Assert.Equal(0, redactor.PrefixLength(text));
            Assert.Equal("left [REDACTED]" + start + body + "\u001b\\ right",
                redactor.Apply(text + "\u001b\\tic-secret right"));
        }

        [Theory]
        [InlineData("end")]
        [InlineData("newline")]
        [InlineData("stop")]
        public void UnfinishedQuotedCredential_RemainsPrivateWhenTheRecordEnds(string boundary) {
            SessionRecordingOptions options = Options();
            string prefix = new string('a', 100) + " ";
            using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                writer.Append(prefix + "password='" + new string('x', 70000));
                if (boundary == "newline") { writer.Append("\nVisible\n"); }
                if (boundary == "stop") { writer.Append(SessionRecorder.StopSequence + "unrecorded"); }
            }
            Assert.Equal(prefix + "[REDACTED]" + (boundary == "newline" ? "\nVisible\n" : string.Empty), Output(options.Path!));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void OverlappingRules_MatchOriginalOutputInEitherOrder(bool reverse) {
            SessionRecordingOptions options = Options();
            options.Redact.Add(reverse ? "private-value" : "value");
            options.Redact.Add(reverse ? "value" : "private-value");
            string prefix = new string('a', 65530);
            using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                writer.Append(prefix + "private-"); writer.Append("value safe\n");
            }
            Assert.Equal(prefix + "[REDACTED] safe\n", Output(options.Path!));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AdjacentMatches_DoNotRestartInsideCommittedContext(bool styled) {
            SessionRecordingOptions options = Options(); options.Redact.Add("[A-F0-9]{8}");
            string prefix = styled ? "\u001b[31m" + new string('x', 65491) : new string('x', 65496);
            using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                writer.Append(prefix + "0123456789ABCDEF" + new string('z', 24)); writer.Append("\n");
            }
            Assert.Equal(prefix + "[REDACTED][REDACTED]" + new string('z', 24) + "\n", Output(options.Path!));
        }

        [Fact]
        public void CompleteRecordMatching_PreservesOrderAcrossManyEqualTimeChunks() {
            SessionRecordingOptions options = Options(); options.Redact.Add("BEGIN.*END");
            string text = string.Concat(Enumerable.Range(0, 48001).Select(i => i.ToString("D6") + "abcdefghij")) + "\n";
            using (SessionCastWriter writer = new SessionCastWriter(options, "test")) { writer.Append(text); }
            Assert.Equal(text, Output(options.Path!));
        }

        [Fact]
        public void ArbitraryRegex_MatchesTheCompleteLongRecordWithoutLosingSafeOutput() {
            SessionRecordingOptions options = Options(); options.Redact.Add("BEGIN.*END");
            string prefix = new string('a', 100000), suffix = new string('z', 110000);
            using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                writer.Append(prefix + "BEGINprivate-");
                writer.Append("valueEND" + suffix + "\n");
            }
            Assert.Equal(prefix + "[REDACTED]" + suffix + "\n", Output(options.Path!));
        }

        [Fact]
        public void CompleteRecordFallback_ReportsOmissionWithoutClaimingTheTotalByteCap() {
            SessionRecordingOptions options = Options(); options.Redact.Add("BEGIN.*END");
            using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                writer.Append(new string('a', SessionCastWriter.MaximumPendingCharacters + 100));
                writer.Append("\nRecovered\n");
                Assert.False(writer.LimitReached);
            }
            string text = Output(options.Path!);
            Assert.Contains("complete-record regex buffer exceeds 1,048,576 characters; output omitted through next line break", text);
            Assert.EndsWith("Recovered\n", text);
            Dictionary<string, object?> metadata = (Dictionary<string, object?>)MiniJson.Read(File.ReadAllText(options.Path + ".json"))!;
            Assert.False((bool)metadata["sizeLimitReached"]!);
            Assert.Equal("1", metadata["redactionFallbacks"]);
        }

        [Fact]
        public void StreamingRedaction_PreservesMarkerAndResizeOrderAcrossASecret() {
            SessionRecordingOptions options = Options(); options.Redact.Add("synthetic-secret");
            using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                writer.Append(new string('a', 65530) + "synthe");
                writer.Append("tic-" + SessionRecorder.MarkerSequence("Chapter") + "secret");
                writer.Resize(140, 40);
                writer.Append("\nDone\n");
            }
            string cast = File.ReadAllText(options.Path!);
            Assert.DoesNotContain("synthe", cast); Assert.DoesNotContain("secret", cast);
            List<List<object?>> events = Events(options.Path!);
            int marker = events.FindIndex(e => (string)e[1]! == "m");
            int resize = events.FindIndex(e => (string)e[1]! == "r");
            int done = events.FindIndex(e => (string)e[1]! == "o" && ((string)e[2]!).Contains("Done"));
            Assert.True(marker > 0 && marker < resize && resize < done);
            Assert.Equal(140, SessionRecording.Read(options.Path!).FinalFrame().Width);
        }

        private static List<List<object?>> Events(string path) => File.ReadAllLines(path).Skip(1)
            .Select(line => (List<object?>)MiniJson.Read(line)!).ToList();
        private static string Output(string path) => string.Concat(Events(path).Where(e => (string)e[1]! == "o").Select(e => (string)e[2]!));
        private static string ReadLiveCast(string path) {
            // Permit the existing writer on Windows; its FileShare.Read still denies other writers.
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8)) {
                return reader.ReadToEnd();
            }
        }
        public void Dispose() { if (Directory.Exists(_folder)) { Directory.Delete(_folder, true); } }
    }
}
