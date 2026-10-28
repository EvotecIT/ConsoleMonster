using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ConsoleMonster.Export;
using ConsoleMonster.Sessions;
using SiteFrames.Capture;
using SiteFrames.Replay;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Checks the actual frames used by the automatic-formatting demonstration.</summary>
    public sealed class FormattingCaptureContracts {
        [Fact]
        public void AutomaticFormatting_AfterFramesContainNoNativeTableHeaders() {
            DirectoryInfo root = Repository();
            string cast = File.ReadAllText(Path.Combine(root!.FullName, "Website", "captures", "demos", "formatting.cast"));
            var frames = Replays.FromCast(cast, 5, false, out _, out _, out _);
            string[] after = frames.Select(frame => ConsoleExporter.ToText(frame.Frame))
                .Where(text => text.Contains("# The same objects now", StringComparison.Ordinal))
                .ToArray();
            Assert.NotEmpty(after);
            Assert.Contains(after, text => text.Contains("WebApi", StringComparison.Ordinal));
            Assert.Contains(after, text => text.Contains("Minor", StringComparison.Ordinal) && text.Contains("Domain", StringComparison.Ordinal));
            Assert.All(after, text => Assert.DoesNotMatch(new Regex(@"(?m)^[ \t]*-{2,}(?:[ \t]+-{2,})*[ \t]*$"), text));
            Assert.All(after, text => Assert.DoesNotContain("Out-Default:", text, StringComparison.Ordinal));
            Assert.DoesNotContain("Show-ConsoleTable", File.ReadAllText(Path.Combine(root.FullName, "Website", "examples", "demos", "formatting.ps1")), StringComparison.Ordinal);
        }

        [Fact]
        public void ModuleViews_AfterCaptureShowsTheModuleView() {
            DirectoryInfo root = Repository();
            var recording = SessionRecording.Read(Path.Combine(root!.FullName, "Website", "captures", "demos", "module-views.cast"));
            string text = ConsoleExporter.ToText(recording.FinalFrame());
            int start = text.IndexOf("# The module's", StringComparison.Ordinal);
            Assert.True(start >= 0, "The module view's after-state must be present.");
            string after = text.Substring(start);
            Assert.Contains("Health", after, StringComparison.Ordinal);
            Assert.Contains("Degraded", after, StringComparison.Ordinal);
            Assert.DoesNotMatch(new Regex(@"(?m)^[ \t]*-{2,}(?:[ \t]+-{2,})*[ \t]*$"), after);
            Assert.DoesNotContain("PS> exit", text, StringComparison.Ordinal);
        }

        [Fact]
        public void FormatCommands_AfterFramesContainNoNativeTableHeaders() {
            DirectoryInfo root = Repository();
            string cast = File.ReadAllText(Path.Combine(root!.FullName, "Website", "captures", "demos", "format-commands.cast"));
            var frames = Replays.FromCast(cast, 5, false, out _, out _, out _);
            string[] after = frames.Select(frame => ConsoleExporter.ToText(frame.Frame))
                .Where(text => text.Contains("# The same commands, drawn by ConsoleMonster", StringComparison.Ordinal))
                .Select(text => text.Substring(text.IndexOf("# The same commands, drawn by ConsoleMonster", StringComparison.Ordinal)))
                .ToArray();
            Assert.NotEmpty(after);
            Assert.Contains(after, text => text.Contains("WebApi", StringComparison.Ordinal));
            Assert.All(after, text => Assert.DoesNotMatch(new Regex(@"(?m)^[ \t]*-{2,}(?:[ \t]+-{2,})*[ \t]*$"), text));
        }

        [Fact]
        public void ReadmeFormattingImage_StartsAtTheEnabledExample() {
            DirectoryInfo root = Repository();
            var image = Readme.Images(new Site(Path.Combine(root!.FullName, "Website"))).Single(item => item.Id == "formatting");
            string plain = System.Net.WebUtility.HtmlDecode(Regex.Replace(image.Dark, "<[^>]*>", string.Empty));
            Assert.StartsWith("PS> Enable-ConsoleFormatting", plain.TrimStart(), StringComparison.Ordinal);
            Assert.Contains("WebApi", plain, StringComparison.Ordinal);
            Assert.Contains("Domain", plain, StringComparison.Ordinal);
            Assert.DoesNotMatch(new Regex(@"(?m)^[ \t]*-{2,}(?:[ \t]+-{2,})*[ \t]*$"), plain);
        }

        internal static DirectoryInfo Repository() {
            DirectoryInfo? root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "ConsoleMonster.sln"))) { root = root.Parent; }
            Assert.NotNull(root);
            return root!;
        }
    }

    /// <summary>Runs the shown script at an interactive prompt on each installed PowerShell host.</summary>
    [Collection("Interactive process defaults")]
    public sealed class InteractiveFormattingCaptureContracts {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void InteractiveSession_UsesTheInstalledDefaultOutput(bool desktop) {
            if (desktop && !OperatingSystem.IsWindows()) { return; }
            DirectoryInfo root = FormattingCaptureContracts.Repository();
            var site = new Site(Path.Combine(root!.FullName, "Website"));
            string? recorded = null;
            PowerShellRunner.Run(site, desktop ? PowerShellHost.Desktop : PowerShellHost.Core,
                Path.Combine(site.Website, "examples", "demos", "formatting.ps1"), 100, 40,
                PowerShellRunner.Environment("dark"), session: true, interactiveSession: true, recording: value => recorded = value);
            Assert.NotNull(recorded);
            var session = SessionRecording.Read(new StringReader(recorded!));
            Assert.Equal("site:end", session.Chapters.Last().Label);
            string text = ConsoleExporter.ToText(session.ChapterFrame(session.Chapters.Count - 1));
            int start = text.IndexOf("# The same objects now", StringComparison.Ordinal);
            Assert.True(start >= 0, text);
            string after = text.Substring(start);
            Assert.Contains("WebApi", after, StringComparison.Ordinal);
            Assert.Contains("Minor", after, StringComparison.Ordinal);
            Assert.Contains("Domain", after, StringComparison.Ordinal);
            Assert.DoesNotContain("Out-Default:", after, StringComparison.Ordinal);
            Assert.DoesNotContain("cmSiteDone", after, StringComparison.Ordinal);
            Assert.DoesNotMatch(new Regex(@"(?m)^[ \t]*-{2,}(?:[ \t]+-{2,})*[ \t]*$"), after);
        }
    }
}
