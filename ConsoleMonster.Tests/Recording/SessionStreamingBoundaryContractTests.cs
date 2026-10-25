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
    // Long-record correctness checks keep the production regex deadline and must not race
    // other allocation-heavy collections for that budget. Exact content/leak assertions remain active.
    [Collection("Live sessions")]
    public sealed class SessionStreamingBoundaryContractTests {
        public static IEnumerable<object[]> TerminalStringReads() {
            string[][] controls = {
                new[] { "\u001b]0;", "\a" }, new[] { "\u001b]0;", "\u001b\\" },
                new[] { "\u001bP", "\u001b\\" }, new[] { "\u001bX", "\u001b\\" },
                new[] { "\u001b^", "\u001b\\" }, new[] { "\u001b_", "\u001b\\" }
            };
            foreach (string[] control in controls) {
                foreach (string newline in new[] { "\n", "\r\n" }) {
                    foreach (int readSize in new[] { 1, 17, 65536 }) {
                        yield return new object[] { control[0], control[1], newline, readSize };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(TerminalStringReads))]
        public void NewlinesInsideTerminalStringsKeepTheVisibleSecretPending(string start, string end, string newline, int readSize) {
            string folder = Path.Combine(Path.GetTempPath(), "ConsoleMonster-boundary-contract-" + Guid.NewGuid().ToString("N"));
            SessionRecordingOptions options = new SessionRecordingOptions { Path = Path.Combine(folder, "session.cast"), InProcess = true };
            options.Redact.Add("synthetic-secret");
            try {
                using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                    AppendReads(writer, "prefix synthe" + start + "metadata" + newline, readSize);
                    using (FileStream input = new FileStream(options.Path!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader reader = new StreamReader(input, Encoding.UTF8)) {
                        Assert.DoesNotContain("synthe", reader.ReadToEnd());
                    }
                    AppendReads(writer, "metadata" + end + "tic-secret safe" + newline, readSize);
                }
                string output = string.Concat(File.ReadAllLines(options.Path!).Skip(1)
                    .Select(line => (List<object?>)MiniJson.Read(line)!)
                    .Where(e => (string)e[1]! == "o").Select(e => (string)e[2]!));
                Assert.Equal("prefix [REDACTED]" + start + "metadata" + newline + "metadata" + end + " safe" + newline, output);
            } finally {
                if (Directory.Exists(folder)) { Directory.Delete(folder, true); }
            }
        }

        [Theory]
        [InlineData("\n", false)]
        [InlineData("\r\n", false)]
        [InlineData("\n", true)]
        [InlineData("\r\n", true)]
        public void NewlinesInsideCsiKeepTheVisibleSecretPending(string newline, bool builtIn) {
            string folder = Path.Combine(Path.GetTempPath(), "ConsoleMonster-csi-contract-" + Guid.NewGuid().ToString("N"));
            SessionRecordingOptions options = new SessionRecordingOptions { Path = Path.Combine(folder, "session.cast"), InProcess = true };
            if (!builtIn) { options.Redact.Add("synthetic-secret"); }
            string control = "\u001b[31" + newline + "m";
            try {
                using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                    AppendReads(writer, "prefix " + (builtIn ? "pass" : "synthe") + "\u001b[31" + newline, 1);
                    using (FileStream input = new FileStream(options.Path!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader reader = new StreamReader(input, Encoding.UTF8)) {
                        Assert.DoesNotContain(builtIn ? "pass" : "synthe", reader.ReadToEnd());
                    }
                    AppendReads(writer, "m" + (builtIn ? "word=private" : "tic-secret") + newline, 1);
                }
                string output = string.Concat(File.ReadAllLines(options.Path!).Skip(1)
                    .Select(line => (List<object?>)MiniJson.Read(line)!)
                    .Where(e => (string)e[1]! == "o").Select(e => (string)e[2]!));
                Assert.Equal("prefix [REDACTED]" + control + newline, output);
            } finally {
                if (Directory.Exists(folder)) { Directory.Delete(folder, true); }
            }
        }

        [Theory]
        [InlineData("\u001bP\u001b\u001b\\metadata\u001b\\", false)]
        [InlineData("\u001bP\u001b\u001b\\metadata\u001b\\", true)]
        [InlineData("\u001b]metadata\u001b\u001b[31m", false)]
        [InlineData("\u001b]metadata\u001b\u001b[31m", true)]
        [InlineData("\u001b[31\u001b[0m", false)]
        [InlineData("\u001b[31\u001b[0m", true)]
        [InlineData("\u001b((", false)]
        [InlineData("\u001b((", true)]
        [InlineData("\u001b))", false)]
        [InlineData("\u001b))", true)]
        [InlineData("\u001b**", false)]
        [InlineData("\u001b**", true)]
        [InlineData("\u001b++", false)]
        [InlineData("\u001b++", true)]
        [InlineData("\u001b ", false)]
        [InlineData("\u001b ", true)]
        [InlineData("\0", false)]
        [InlineData("\0", true)]
        [InlineData("\a", false)]
        [InlineData("\a", true)]
        [InlineData("\u0001", false)]
        [InlineData("\u0001", true)]
        [InlineData("\u007f", false)]
        [InlineData("\u007f", true)]
        public void EscapesInsideControlBodiesFollowReplayWithoutReleasingSecrets(string control, bool builtIn) {
            string folder = Path.Combine(Path.GetTempPath(), "ConsoleMonster-escape-contract-" + Guid.NewGuid().ToString("N"));
            SessionRecordingOptions options = new SessionRecordingOptions { Path = Path.Combine(folder, "session.cast"), InProcess = true };
            if (!builtIn) { options.Redact.Add("synthetic-secret"); }
            try {
                using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                    AppendReads(writer, "prefix " + (builtIn ? "pass" : "synthe") + control + (builtIn ? "word=private" : "tic-secret") + "\n", 1);
                }
                string output = string.Concat(File.ReadAllLines(options.Path!).Skip(1)
                    .Select(line => (List<object?>)MiniJson.Read(line)!)
                    .Where(e => (string)e[1]! == "o").Select(e => (string)e[2]!));
                Assert.Equal("prefix [REDACTED]" + control + "\n", output);
                string replay = ConsoleExporter.ToText(SessionRecording.Read(options.Path!).FinalFrame());
                Assert.DoesNotContain(builtIn ? "private" : "synthetic-secret", replay);
            } finally {
                if (Directory.Exists(folder)) { Directory.Delete(folder, true); }
            }
        }

        [Theory]
        [InlineData("\u001b]0;", "\a", 'x', "\n")]
        [InlineData("\u001b]0;", "\a", 'x', "\r\n")]
        [InlineData("\u001bP", "\u001b\\", 'x', "\n")]
        [InlineData("\u001bP", "\u001b\\", 'x', "\r\n")]
        [InlineData("\u001b[", "m", '3', "\n")]
        [InlineData("\u001b[", "m", '3', "\r\n")]
        public void OversizedControlsRemainSuppressedUntilAVisibleLineBreak(string start, string end, char body, string newline) {
            string folder = Path.Combine(Path.GetTempPath(), "ConsoleMonster-suppression-contract-" + Guid.NewGuid().ToString("N"));
            SessionRecordingOptions options = new SessionRecordingOptions { Path = Path.Combine(folder, "session.cast"), InProcess = true };
            options.Redact.Add("synthetic-secret");
            try {
                using (SessionCastWriter writer = new SessionCastWriter(options, "test")) {
                    AppendReads(writer, "synthe" + start + new string(body, SessionCastWriter.MaximumPendingCharacters), 8191);
                    writer.Append(newline + end + "tic-secret" + newline + "Recovered" + newline);
                }
                string output = string.Concat(File.ReadAllLines(options.Path!).Skip(1)
                    .Select(line => (List<object?>)MiniJson.Read(line)!)
                    .Where(e => (string)e[1]! == "o").Select(e => (string)e[2]!));
                const string omitted = "[REDACTED: unfinished secret or terminal control exceeds 1,048,576 characters; output omitted through next line break]\r\n";
                Assert.Equal(omitted + (newline == "\r\n" ? "\n" : string.Empty) + "Recovered" + newline, output);
            } finally {
                if (Directory.Exists(folder)) { Directory.Delete(folder, true); }
            }
        }

        [Fact]
        public void OscEscapeBellTerminatesBeforeLongSafeOutputAndRecovery() {
            string folder = Path.Combine(Path.GetTempPath(), "ConsoleMonster-osc-bell-contract-" + Guid.NewGuid().ToString("N"));
            SessionRecordingOptions options = new SessionRecordingOptions { Path = Path.Combine(folder, "session.cast"), InProcess = true };
            string text = "\u001b]0;title\u001b\a" + new string('x', SessionCastWriter.MaximumPendingCharacters + 16) + "\nRecovered\n";
            try {
                using (SessionCastWriter writer = new SessionCastWriter(options, "test")) { AppendReads(writer, text, 8191); }
                string output = string.Concat(File.ReadAllLines(options.Path!).Skip(1)
                    .Select(line => (List<object?>)MiniJson.Read(line)!)
                    .Where(e => (string)e[1]! == "o").Select(e => (string)e[2]!));
                Assert.Equal(text, output);
            } finally {
                if (Directory.Exists(folder)) { Directory.Delete(folder, true); }
            }
        }

        private static void AppendReads(SessionCastWriter writer, string text, int readSize) {
            for (int at = 0; at < text.Length; at += readSize) {
                writer.Append(text.Substring(at, Math.Min(readSize, text.Length - at)));
            }
        }
    }
}
