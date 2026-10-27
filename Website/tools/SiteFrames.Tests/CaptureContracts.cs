using System;
using System.Collections.Generic;
using System.IO;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using SiteFrames;
using SiteFrames.Capture;
using SiteFrames.Frames;
using SiteFrames.Pages;
using SiteFrames.Replay;
using SiteFrames.Vt;
using Xunit;

namespace SiteFrames.Tests {
    public sealed class CaptureContracts {
        [Theory]
        [InlineData('\u257D', 1, 2)]
        [InlineData('\u257F', 2, 1)]
        public void MixedWeightVerticalGlyphs_KeepTheirUnicodeDirection(char glyph, int expectedUp, int expectedDown) {
            Assert.True(GlyphPaint.TrySegments(glyph, out int up, out int right, out int down, out int left));
            Assert.Equal(expectedUp, up);
            Assert.Equal(expectedDown, down);
            Assert.Equal(0, right);
            Assert.Equal(0, left);
        }

        [Fact]
        public void Benchmarks_RejectUnparseableCapturedNumbers() {
            string root = Path.Combine(Path.GetTempPath(), "siteframes-benchmarks-" + Guid.NewGuid().ToString("N"));
            int before = Program.Failures;
            try {
                Directory.CreateDirectory(Path.Combine(root, "Website", "captures", "benchmarks"));
                File.WriteAllText(Path.Combine(root, "ConsoleMonster.sln"), string.Empty);
                File.WriteAllText(Path.Combine(root, "Website", "captures", "benchmarks", "benchmarks.cast"), "{\"width\":80,\"height\":5}\n[0,\"o\",\"Unrecognized benchmark output\"]\n");
                Site site = new Site(Path.Combine(root, "Website"));
                BenchmarksPage.Render(site, new System.Text.Json.Nodes.JsonObject());
                Assert.True(Program.Failures > before);
                Assert.False(File.Exists(site.Data("benchmarks")));
            } finally {
                Program.Failures = before;
                if (Directory.Exists(root)) { Directory.Delete(root, true); }
            }
        }

        [Fact]
        public void Parity_RejectsDecoratedBlanksAndDifferentGeometry() {
            CellBuffer expected = new CellBuffer(4, 1);
            CellBuffer actual = new CellBuffer(4, 1);
            actual.Set(0, 0, " ", Style.Plain.With(TextDecoration.Reverse));
            Assert.False(Parity.Compare(expected, actual).Identical);
            expected.Set(0, 0, "x", Style.Plain, 2);
            actual = new CellBuffer(4, 1);
            actual.Set(0, 0, "x", Style.Plain);
            Assert.False(Parity.Compare(expected, actual).Identical);
            Assert.True(Parity.Compare(expected, expected).Identical);
        }

        [Fact]
        public void Parity_IgnoresOnlyInvisibleSpaceForegroundAndKeepsBackgroundAndDecoration() {
            CellBuffer expected = new CellBuffer(4, 1), actual = new CellBuffer(4, 1);
            Color background = Color.FromHex("#1f2a3c");
            expected.Set(0, 0, " ", new Style(Color.FromHex("#e2e8f0"), background));
            actual.Set(0, 0, " ", new Style(Color.Default, background));
            Assert.True(Parity.Compare(expected, actual).Identical);
            actual.Set(0, 0, " ", new Style(Color.Default, Color.Default));
            Assert.False(Parity.Compare(expected, actual).Identical);
            actual.Set(0, 0, " ", new Style(Color.Default, background, TextDecoration.Underline));
            Assert.False(Parity.Compare(expected, actual).Identical);
        }

        [Fact]
        public void CastSampling_DoesNotPublishAnIncompleteSynchronizedFrame() {
            string cast = "{\"width\":8,\"height\":1}\n"
                + "[0,\"o\",\"old\"]\n"
                + "[0.1,\"o\",\"\\u001b[?2026h\\rpartial\"]\n"
                + "[0.5,\"o\",\"\\rnew    \\u001b[?2026l\"]\n";
            var frames = Replays.FromCast(cast, 10, false, out _, out _, out _);
            Assert.Contains(frames, f => f.Frame.GetRowText(0).Trim() == "old");
            Assert.Contains(frames, f => f.Frame.GetRowText(0).Trim() == "new");
            Assert.DoesNotContain(frames, f => f.Frame.GetRowText(0).Contains("partial"));
        }

        [Fact]
        public void CaptureEnvironment_ClearsEveryKnownCiMarker() {
            var environment = PowerShellRunner.Environment("dark");
            foreach (string marker in new[] { "CI", "GITHUB_ACTIONS", "TF_BUILD", "BUILD_BUILDID", "JENKINS_URL", "GITLAB_CI", "TEAMCITY_VERSION", "CODEX_CI" }) {
                Assert.Null(environment[marker]);
            }
            Assert.Equal("xterm-256color", environment["TERM"]);
        }

        [Fact]
        public void Capture_RejectsANonzeroExitInsteadOfSavingItAsOutput() {
            string executable = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh";
            string[] arguments = OperatingSystem.IsWindows() ? new[] { "/d", "/c", "exit 7" } : new[] { "-c", "exit 7" };
            var error = Assert.Throws<InvalidOperationException>(() => TerminalCapture.Run(executable, arguments, 20, 5,
                new Dictionary<string, string?>(), Path.GetTempPath(), TimeSpan.FromSeconds(10)));
            Assert.Contains("code 7", error.Message);
        }

        [Fact]
        public void Render_RejectsAreaSelectionBeforeTouchingOutput() {
            Assert.Equal(2, Program.Main(new[] { "render", "nonexistent-website", "widgets" }));
        }
    }
}
