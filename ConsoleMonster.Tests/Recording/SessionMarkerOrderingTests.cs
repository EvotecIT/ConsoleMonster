using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ConsoleMonster.Sessions;
using Xunit;

namespace ConsoleMonster.Tests.Sessions {
    /// <summary>Chapter and hold controls must follow the native screen writes they describe, even without idle gaps.</summary>
    public sealed class SessionMarkerOrderingTests {
        [WindowsTheory]
        [InlineData(false)]
        [InlineData(true)]
        public void WindowsControlsFollowPendingRepaints(bool hold) {
            string folder = Path.Combine(Path.GetTempPath(), "cm-marker-order-" + Guid.NewGuid().ToString("N"));
            var options = new SessionRecordingOptions {
                Path = Path.Combine(folder, "capture.cast"), Shell = "pwsh.exe", Width = 100, Height = 46,
                Timeout = TimeSpan.FromSeconds(20)
            };
            var script = new StringBuilder("$ErrorActionPreference = 'Stop'; ");
            void WriteControl(string text) => script.Append("[Console]::Write('").Append(text.Replace("'", "''")).Append("'); ");
            WriteControl(SessionRecorder.MarkerSequence("start"));
            // Same last-row output as format-commands, then adjacent writes exercise intermediate boundaries too.
            script.Append("[Console]::Write((\"`r`n\" * 43)); ");
            const int steps = 12;
            for (int i = 0; i < steps; i++) {
                script.Append("[Console]::Write(\"`rWebApi-").Append(i)
                    .Append(" Worker-").Append(i).Append(" Archive-").Append(i).Append("\"); ");
                WriteControl(hold ? SessionRecorder.MarkerSequence(TimeSpan.FromSeconds(2), "step-" + i)
                    : SessionRecorder.MarkerSequence("step-" + i));
            }
            WriteControl(SessionRecorder.StopSequence);
            options.Arguments.Add("-NoLogo");
            options.Arguments.Add("-NoProfile");
            options.Arguments.Add("-NonInteractive");
            options.Arguments.Add("-EncodedCommand");
            options.Arguments.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(script.ToString())));
            try {
                using (var recorder = SessionRecorder.Start(options)) {
                    recorder.WaitForExit();
                    Assert.Equal(0, recorder.ExitCode);
                }
                SessionRecording recording = SessionRecording.Read(options.Path);
                Assert.Equal(steps + 1, recording.Chapters.Count);
                for (int i = 0; i < steps; i++) {
                    Assert.Equal("WebApi-" + i + " Worker-" + i + " Archive-" + i,
                        recording.ChapterFrame(i).GetRowText(43).Trim());
                }
                Assert.Equal("WebApi-11 Worker-11 Archive-11", recording.FinalFrame().GetRowText(43).Trim());
                if (hold) { Assert.Equal(TimeSpan.FromSeconds(2), recording.Markers[1].Hold); }
            } finally {
                if (Directory.Exists(folder)) { Directory.Delete(folder, true); }
            }
        }

        private sealed class WindowsTheoryAttribute : TheoryAttribute {
            public WindowsTheoryAttribute() {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    Skip = "The repaint-ordering oracle requires native Windows ConPTY and PowerShell 7.";
                }
            }
        }
    }
}
