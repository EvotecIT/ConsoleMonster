using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ConsoleMonster.Export;
using ConsoleMonster.Sessions;
using Xunit;
using ConsoleMonster.Tests.Rendering;

namespace ConsoleMonster.Tests.Sessions {
    public sealed class SessionRecordingTests : IDisposable {
        private readonly string _folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ConsoleMonster-recording-" + Guid.NewGuid().ToString("N"));
        public SessionRecordingTests() { Directory.CreateDirectory(_folder); }
        private SessionRecordingOptions Options(string name = "session") => new SessionRecordingOptions { Path = System.IO.Path.Combine(_folder, name + ".cast"), Width = 30, Height = 6, InProcess = true };

        [Fact]
        public void Redaction_CustomLabelPreservesContextAndStyles() {
            SessionRecordingOptions options = Options("label");
            options.Width = 60;
            options.RedactionText = "•••••••";
            options.Redact.Add("synthetic-value");
            using (SessionRecorder recorder = SessionRecorder.Start(options)) {
                recorder.CapturePowerShellOutput("\u001b[90mAPI_TOKEN=synthetic-");
                Thread.Sleep(30);
                recorder.CapturePowerShellOutput("value (redacted)\u001b[0m\r\n");
                recorder.AddMarker("Report");
            }
            SessionRecording recording = SessionRecording.Read(options.Path!);
            Assert.DoesNotContain("synthetic-", File.ReadAllText(options.Path!));
            Assert.Contains("API_TOKEN=••••••• (redacted)", recording.Export(ExportFormat.Text));
            Assert.Equal(ConsoleMonster.Styling.Color.FromAnsi16(8), recording.FinalFrame()[0, 0].Style.Foreground);
            Assert.Contains("Report", recording.ExportPlayerData());
        }

        [Theory]
        [InlineData("")]
        [InlineData("\u001b[31m")]
        [InlineData("mask\r\n")]
        public void Redaction_RejectsEmptyOrControlBearingLabels(string label) {
            SessionRecordingOptions options = Options("invalid-label"); options.RedactionText = label;
            Assert.Throws<ArgumentException>(() => SessionRecorder.Start(options));
            Assert.False(File.Exists(options.Path!));
        }

        [Fact]
        public void Writer_AutoNamesUseTheConfiguredFolderAndDescribeTheRecording() {
            SessionRecordingOptions options = Options();
            options.Path = null;
            options.Folder = System.IO.Path.Combine(_folder, "automatic");
            options.Name = "Automatic recording";
            options.Redact.Add("\\A" + System.Text.RegularExpressions.Regex.Escape(Environment.MachineName) + "\\z");
            long earliest = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string first, second;
            using (SessionRecorder recorder = SessionRecorder.Start(options)) { first = recorder.Path; recorder.CapturePowerShellOutput("first\n"); }
            using (SessionRecorder recorder = SessionRecorder.Start(options)) { second = recorder.Path; recorder.CapturePowerShellOutput("second\n"); }
            long latest = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Assert.NotEqual(first, second);
            Assert.Equal(options.Folder, System.IO.Path.GetDirectoryName(first));
            Assert.Equal(options.Folder, System.IO.Path.GetDirectoryName(second));
            Assert.Equal(".cast", System.IO.Path.GetExtension(first));
            Assert.Contains("first", SessionRecording.Read(first).Export(ExportFormat.Text));
            Assert.Contains("second", SessionRecording.Read(second).Export(ExportFormat.Text));
            var header = (Dictionary<string, object?>)MiniJson.Read(File.ReadAllLines(first)[0])!;
            Assert.InRange(long.Parse((string)header["timestamp"]!), earliest, latest);
            Assert.Equal("Automatic recording", header["title"]);
            var metadata = (Dictionary<string, object?>)header["consolemonster"]!;
            Assert.True((bool)metadata["recording"]!);
            Assert.Equal("in-process PowerShell output only", metadata["mode"]);
            Assert.Equal("[REDACTED]", metadata["host"]);
        }

        [Fact]
        public void Writer_RedactsAcrossReadsAndProtectsChapterMetadata() {
            SessionRecordingOptions options = Options();
            options.Redact.Add("private-project");
            options.Name = "token=title-secret";
            using (SessionRecorder recorder = SessionRecorder.Start(options)) {
                recorder.CapturePowerShellOutput("Bearer syn");
                recorder.CapturePowerShellOutput("thetic-token\r\napi_key=sample-key\r\nprivate-");
                recorder.CapturePowerShellOutput("project\r\n");
                recorder.AddMarker("Deploy private-project password=chapter-secret");
                recorder.CapturePowerShellOutput("sk-abcdefgh12345678\r\n");
                recorder.CapturePowerShellOutput("password=colour-\u001b[31msplit-secret\u001b[0m\r\n");
                recorder.CapturePowerShellOutput("Server=localhost;Password='quoted secret value';Database=sample\r\n");
            }
            string cast = File.ReadAllText(options.Path!), metadata = File.ReadAllText(options.Path + ".json");
            foreach (string secret in new[] { "synthetic-token", "sample-key", "private-project", "chapter-secret", "title-secret", "sk-abcdefgh12345678", "colour-", "split-secret", "quoted secret value" }) {
                Assert.DoesNotContain(secret, cast); Assert.DoesNotContain(secret, metadata);
            }
            Assert.Contains("[REDACTED]", cast);
            Assert.Contains("in-process PowerShell output only", cast);
            Assert.Contains("Deploy [REDACTED] [REDACTED]", SessionRecording.Read(options.Path!).ExportPlayerData());
        }

        [Fact]
        public void Writer_IdleLimitCapsPlaybackGaps() {
            SessionRecordingOptions options = Options(); options.IdleLimit = TimeSpan.FromMilliseconds(10);
            using (SessionRecorder recorder = SessionRecorder.Start(options)) {
                recorder.CapturePowerShellOutput("first\n");
                Thread.Sleep(50);
                recorder.CapturePowerShellOutput("second\n");
            }
            string[] lines = File.ReadAllLines(options.Path!);
            double last = double.Parse(lines[2].Substring(1, lines[2].IndexOf(',') - 1), System.Globalization.CultureInfo.InvariantCulture);
            Assert.InRange(last, 0, 0.020001);
            Assert.Contains("second", ConsoleExporter.ToText(SessionRecording.Read(options.Path!).FinalFrame()));
        }

        [Fact]
        public void Redaction_PreservesTerminalStyleForFollowingOutput() {
            SessionRecordingOptions options = Options();
            using (SessionRecorder recorder = SessionRecorder.Start(options)) {
                recorder.CapturePowerShellOutput("password=hidden-value\u001b[32m\r\nGreen next line\u001b[0m\r\n");
            }
            var frame = SessionRecording.Read(options.Path!).FinalFrame();
            Assert.DoesNotContain("hidden-value", File.ReadAllText(options.Path!));
            Assert.Equal(ConsoleMonster.Styling.Color.FromAnsi16(2), frame[0, 1].Style.Foreground);
            Assert.Contains("Green next line", ConsoleExporter.ToText(frame));
        }

        [Fact]
        public void Writer_SizeCapKeepsCompleteJsonAndFinalizesSidecar() {
            SessionRecordingOptions options = Options(); options.MaxBytes = 1024;
            using (SessionRecorder recorder = SessionRecorder.Start(options)) {
                recorder.CapturePowerShellOutput(new string('a', 2000) + "\n");
                Assert.True(recorder.LimitReached);
            }
            Assert.InRange(new FileInfo(options.Path!).Length, 1, options.MaxBytes);
            var metadata = (Dictionary<string, object?>)MiniJson.Read(File.ReadAllText(options.Path + ".json"))!;
            Assert.True((bool)metadata["sizeLimitReached"]!);
            Assert.NotNull(SessionRecording.Read(options.Path!).FinalFrame());
        }

        [Fact]
        public void Writer_StopControlRemainsActiveAfterSizeCap() {
            SessionRecordingOptions options = Options(); options.MaxBytes = 1024;
            using (SessionCastWriter writer = new SessionCastWriter(options, "in-process PowerShell output only")) {
                writer.Append(new string('x', 2000) + "\n");
                Assert.True(writer.LimitReached);
                writer.Append(SessionRecorder.StopSequence);
                Assert.True(writer.StopRequested);
            }
            Assert.NotNull(SessionRecording.Read(options.Path!).FinalFrame());
        }

        [Fact]
        public void Writer_InterruptedPrivateMarkerDoesNotPersistEncodedLabel() {
            SessionRecordingOptions options = Options();
            string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("private-label"));
            using (SessionCastWriter writer = new SessionCastWriter(options, "in-process PowerShell output only")) {
                writer.Append(SessionCastWriter.ControlPrefix + "marker;" + encoded);
                writer.Append("\u001b[32mVisible\r\n");
            }
            string cast = File.ReadAllText(options.Path!);
            Assert.DoesNotContain(encoded, cast); Assert.DoesNotContain("private-label", cast);
            Assert.Contains("Visible", SessionRecording.Read(options.Path!).Export(ExportFormat.Text));
        }

        [Fact]
        public void Writer_PreservesPartialWriteTimingAndResizeOrder() {
            SessionRecordingOptions options = Options(); options.Width = 4; options.Height = 4; options.IdleLimit = TimeSpan.Zero;
            using (SessionCastWriter writer = new SessionCastWriter(options, "in-process PowerShell output only")) {
                writer.Append("ABCDEF");
                Thread.Sleep(80);
                writer.Resize(8, 4);
                writer.Marker("Expanded");
                writer.Append("\r\n");
            }
            string[] lines = File.ReadAllLines(options.Path!);
            Assert.Contains("ABCDEF", lines[1]);
            Assert.Contains("\"r\",\"8x4\"", lines[2]);
            Assert.Contains("\"m\",\"Expanded\"", lines[3]);
            List<object?> first = (List<object?>)MiniJson.Read(lines[1])!;
            List<object?> resize = (List<object?>)MiniJson.Read(lines[2])!;
            double gap = double.Parse((string)resize[0]!, System.Globalization.CultureInfo.InvariantCulture) - double.Parse((string)first[0]!, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(gap >= 0.02, "Buffered output lost its original arrival time.");
            string text = SessionRecording.Read(options.Path!).Export(ExportFormat.Text);
            Assert.StartsWith("ABCD", text);
            Assert.Contains("EF", text.Split('\n')[1]);
        }

        [Fact]
        public void Writer_RedactsAcrossResizeWithoutPersistingASecretPrefix() {
            SessionRecordingOptions options = Options();
            using (SessionCastWriter writer = new SessionCastWriter(options, "in-process PowerShell output only")) {
                writer.Append("password=split-"); writer.Resize(40, 8); writer.Append("secret\r\n");
            }
            string cast = File.ReadAllText(options.Path!);
            Assert.DoesNotContain("split-", cast); Assert.DoesNotContain("secret", cast);
            Assert.Contains("[REDACTED]", cast);
            Assert.Equal(40, SessionRecording.Read(options.Path!).FinalFrame().Width);
        }

        [Fact]
        public void Playback_ShowsInitialAndInterveningStatesDuringIdleGaps() {
            SessionRecording recording = SessionRecording.Read(new StringReader("{\"version\":2,\"width\":12,\"height\":4}\n[0,\"o\",\"Ready\"]\n[0.05,\"o\",\"!\"]\n[5,\"o\",\"\\rDone\"]\n"));
            Dictionary<string, object?> data = (Dictionary<string, object?>)MiniJson.Read(recording.ExportPlayerData())!;
            List<object?> frames = (List<object?>)data["frames"]!;
            List<object?> rows = (List<object?>)data["rows"]!;
            List<object?> first = (List<object?>)frames[0]!;
            List<object?> firstRows = (List<object?>)first[1]!;
            Assert.Equal("0", first[0]);
            Assert.Contains("Ready", (string)rows[int.Parse((string)firstRows[0]!)]!);
            List<object?> second = (List<object?>)frames[1]!;
            Assert.Equal("100", second[0]);
            Assert.Contains("Ready!", (string)rows[int.Parse((string)((List<object?>)second[1]!)[0]!)]!);
            Assert.DoesNotContain("infinite", recording.Export(ExportFormat.Svg, new RecordingExportOptions { Loop = false }));
            Dictionary<string, object?> bounded = (Dictionary<string, object?>)MiniJson.Read(recording.ExportPlayerData(new RecordingExportOptions { MaxFrames = 2 }))!;
            var boundedFrames = (List<object?>)bounded["frames"]!;
            Assert.InRange(boundedFrames.Count, 1, 2);
            var finalSample = (List<object?>)boundedFrames[boundedFrames.Count - 1]!;
            int finalRow = int.Parse((string)((List<object?>)finalSample[1]!)[0]!);
            Assert.Contains("Done", (string)((List<object?>)bounded["rows"]!)[finalRow]!);
        }

        [Fact]
        public void Writer_LongUnbrokenSecretRecordIsSuppressed() {
            SessionRecordingOptions options = Options();
            using (SessionRecorder recorder = SessionRecorder.Start(options)) {
                recorder.CapturePowerShellOutput("password=" + new string('x', 70000));
                recorder.CapturePowerShellOutput("end-secret\nVisible\n");
            }
            string cast = File.ReadAllText(options.Path!);
            Assert.DoesNotContain("end-secret", cast);
            Assert.DoesNotContain(new string('x', 100), cast);
            Assert.Contains("Visible", cast);
        }

        [Fact]
        public void Reader_ReconstructsColoursCursorResizeAndScrollback() {
            string cast = "{\"version\":2,\"width\":12,\"height\":4}\n"
                + "[0,\"o\",\"\\u001b[32mReady\\u001b[0m\\r\\nline two\"]\n"
                + "[0.1,\"r\",\"16x5\"]\n"
                + "[0.2,\"o\",\"\\r\\u001b[2KDone\\u001b[1;1HOK\"]\n";
            SessionRecording recording = SessionRecording.Read(new StringReader(cast));
            var frame = recording.FinalFrame();
            Assert.Equal(16, frame.Width); Assert.Equal(5, frame.Height);
            Assert.StartsWith("OKady", ConsoleExporter.ToText(frame));
            Assert.Contains("Done", ConsoleExporter.ToText(frame));
            Assert.Equal(ConsoleMonster.Styling.Color.FromAnsi16(2), frame[2, 0].Style.Foreground);
            Assert.Contains("```", recording.Export(ExportFormat.Markdown));
            Assert.Contains("Done", recording.Export(ExportFormat.Text));
            Assert.Contains("16x5", recording.ToAsciicast());
        }

        [Fact]
        public void Reader_InvalidHeaderOrEventFailsWithoutRendering() {
            foreach (string cast in new[] {
                "{\"version\":3,\"width\":12,\"height\":4}",
                "{\"version\":2,\"width\":100000,\"height\":4}",
                "{\"version\":2,\"width\":12,\"height\":4}\n[1,\"r\",\"0x4\"]",
                "{\"version\":2,\"width\":12,\"height\":4}\n[1,\"o\",\"a\"]\n[0,\"o\",\"b\"]"
            }) { Assert.Throws<FormatException>(() => SessionRecording.Read(new StringReader(cast))); }
        }

        [Fact]
        public void Export_HtmlAndSvgMatchGoldenFrames() {
            SessionRecording recording = GoldenRecording();
            Golden.Match("recording/session.html", recording.Export(ExportFormat.Html, ExportOptions()));
            Golden.Match("recording/session.svg", recording.Export(ExportFormat.Svg, ExportOptions()));
            Assert.Contains("Working 50%", recording.ExportPlayerData());
            Assert.Contains("Chapters", recording.Export(ExportFormat.Html));
            Assert.DoesNotContain("</script><img", recording.ExportPlayerData());
            Assert.Contains("\\u003c/script\\u003e", recording.ExportPlayerData());
            Assert.Contains("Done", recording.ExportFinalSvg(new ExportOptions { Theme = ExportTheme.Light }));
        }
        private static RecordingExportOptions ExportOptions() => new RecordingExportOptions { Theme = ExportTheme.Dark, Title = "Recorded session", WindowChrome = true, HoldLastFrame = TimeSpan.FromSeconds(1) };
        private static SessionRecording GoldenRecording() => SessionRecording.Read(new StringReader(
            "{\"version\":2,\"width\":16,\"height\":4,\"title\":\"Recorded session\"}\n"
            + "[0,\"o\",\"\\u001b[32mBuild\\u001b[0m\\r\\n\"]\n"
            + "[0.2,\"m\",\"Build </script><img>\"]\n"
            + "[0.5,\"o\",\"Working 50%\\r\"]\n"
            + "[1,\"o\",\"\\u001b[2KDone\\r\\n\"]\n"));

        [Fact]
        public void NativeSession_RecordsColouredProgramResizeAndNoEchoInput() {
            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            SessionRecordingOptions options = Options("native");
            options.InProcess = false; options.Timeout = TimeSpan.FromSeconds(30);
            using (ManualResetEventSlim prompt = new ManualResetEventSlim()) {
                if (windows) {
                    options.Shell = "powershell.exe";
                    string script = "[Console]::Write([char]27 + '[32mNative colour' + [char]27 + '[0m' + \"`r`n\"); git --version; 1..3 | % { [Console]::Write(\"`rProgress $_/3\"); Start-Sleep -Milliseconds 30 }; [Console]::Write(\"`r`nPassword: \"); do { $k=[Console]::ReadKey($true) } while ($k.Key -ne 'Enter'); [Console]::Write(\"`r`nNative size $([Console]::WindowWidth)x$([Console]::WindowHeight)`r`npassword=output-secret`r`nFinished`r`n\")";
                    options.Arguments.Add("-NoProfile"); options.Arguments.Add("-EncodedCommand"); options.Arguments.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
                } else {
                    options.Shell = "/bin/sh";
                    options.Arguments.Add("-c");
                    options.Arguments.Add(@"/usr/bin/printf '\033[32mNative colour\033[0m\n'; git --version; printf '\rProgress 3/3\n'; stty -echo; printf 'Password: '; IFS= read -r secret; stty echo; printf '\nNative rows/cols '; stty size; printf 'password=output-secret\nFinished\n'");
                }
                StringBuilder live = new StringBuilder();
                using (SessionRecorder recorder = SessionRecorder.Start(options, text => { lock (live) { live.Append(text); if (live.ToString().Contains("Password:")) { prompt.Set(); } } })) {
                    Assert.True(prompt.Wait(TimeSpan.FromSeconds(15)), "The child did not reach its password prompt. Output: " + live);
                    recorder.Resize(40, 10);
                    recorder.SendInput("input-secret" + (windows ? "\r" : "\n"));
                    recorder.WaitForExit();
                    Assert.Equal(0, recorder.ExitCode);
                }
            }
            string raw = File.ReadAllText(options.Path!);
            Assert.DoesNotContain("input-secret", raw); Assert.DoesNotContain("output-secret", raw);
            Assert.DoesNotContain("\"i\",", raw);
            Assert.Contains("40x10", raw); Assert.Contains("[REDACTED]", raw);
            SessionRecording recording = SessionRecording.Read(options.Path!);
            Assert.Equal(40, recording.FinalFrame().Width);
            Assert.Contains("Finished", ConsoleExporter.ToText(recording.FinalFrame()));
            Assert.Contains("git version", recording.Export(ExportFormat.Text));
            Assert.Contains(windows ? "Native size 40x10" : "Native rows/cols 10 40", recording.Export(ExportFormat.Text));
            Assert.Contains("consolemonster-player", recording.Export(ExportFormat.Html));
        }

        [Fact]
        public void ExistingCastIsNeverOverwritten() {
            SessionRecordingOptions options = Options();
            File.WriteAllText(options.Path!, "existing recording");
            Assert.Throws<IOException>(() => SessionRecorder.Start(options));
            Assert.Equal("existing recording", File.ReadAllText(options.Path!));
        }

        [Fact]
        public void Writer_RecognizesSplitMarkerAndStopWithoutPersistingControls() {
            SessionRecordingOptions options = Options();
            using (SessionCastWriter writer = new SessionCastWriter(options, "in-process PowerShell output only")) {
                string marker = SessionRecorder.MarkerSequence("Deploy");
                writer.Append(marker.Substring(0, 8)); writer.Append(marker.Substring(8));
                writer.Append("Complete\r\n"); writer.Append(SessionRecorder.StopSequence);
                writer.Append("After stop must not be stored\r\n");
                Assert.True(writer.StopRequested);
            }
            string cast = File.ReadAllText(options.Path!);
            Assert.Contains("\"m\",\"Deploy\"", cast);
            Assert.DoesNotContain("777;ConsoleMonster", cast);
            Assert.DoesNotContain("After stop", cast);
        }

        [Fact]
        public void NativeSession_TimeoutFinalizesTheCastAndStopsItsChild() {
            SessionRecordingOptions options = Options("timeout"); options.InProcess = false; options.Timeout = TimeSpan.FromMilliseconds(100);
            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            options.Shell = windows ? "powershell.exe" : "/bin/sh";
            options.Arguments.Add(windows ? "-Command" : "-c");
            options.Arguments.Add(windows ? "Start-Sleep -Seconds 30" : "sleep 30");
            using (SessionRecorder recorder = SessionRecorder.Start(options)) { Assert.Throws<TimeoutException>(() => recorder.WaitForExit()); }
            Assert.NotNull(SessionRecording.Read(options.Path!).FinalFrame());
            Assert.True(File.Exists(options.Path + ".json"));
        }

        [Fact]
        public void NativeSession_NonzeroExitIsAvailableWithoutLosingOutput() {
            SessionRecordingOptions options = Options("failure"); options.InProcess = false; options.Timeout = TimeSpan.FromSeconds(20);
            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            options.Shell = windows ? "powershell.exe" : "/bin/sh";
            options.Arguments.Add(windows ? "-Command" : "-c");
            options.Arguments.Add(windows ? "[Console]::WriteLine('Failed safely'); exit 7" : "printf 'Failed safely\n'; exit 7");
            using (SessionRecorder recorder = SessionRecorder.Start(options)) { recorder.WaitForExit(); Assert.Equal(7, recorder.ExitCode); }
            Assert.Contains("Failed safely", SessionRecording.Read(options.Path!).Export(ExportFormat.Text));
        }

        [Fact]
        public void Options_RejectNulArgumentsBeforeCreatingArtifacts() {
            SessionRecordingOptions options = Options(); options.Arguments.Add("one\0two");
            Assert.Throws<ArgumentException>(() => SessionRecorder.Start(options));
            Assert.False(File.Exists(options.Path));
        }

        public void Dispose() { if (Directory.Exists(_folder)) { Directory.Delete(_folder, true); } }
    }
}
