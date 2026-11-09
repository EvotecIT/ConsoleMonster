using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests {
    [Collection("Live sessions")]
    public sealed class ContinuousIntegrationTests {
        [Fact]
        public void WorkflowCommandsAndDefaultRunCompletionsBypassAnActiveLiveConsoleCapture() {
            var target = global::ConsoleMonster.Testing.TestTerminal.Target();
            TextWriter original = Console.Out;
            using var stdout = new StringWriter();
            Console.SetOut(stdout);
            try {
                using (ContinuousIntegration.UseProvider(ContinuousIntegrationProvider.GitHub))
                using (var session = new LiveSession(new LogRegion(), new LiveSessionOptions {
                    Target = target, CaptureConsole = true, Discoverable = false, HandleCancelKey = false, PrintFinalState = false, StreamHeartbeat = TimeSpan.Zero
                })) {
                    session.Start();
                    using (ContinuousIntegration.Group("Build")) { ContinuousIntegration.Annotate(AnnotationLevel.Error, "failure"); }
                    using (var monitor = new RunMonitor(1, new RunOptions { Live = false })) {
                        monitor.Started(0, "Compile");
                        monitor.Finished(0, StatusKind.Pass, "Compile", TimeSpan.Zero);
                    }
                    session.Stop();
                }
                string text = stdout.ToString().Replace("\r\n", "\n");
                Assert.StartsWith("::group::Build\n::error::failure\n::endgroup::\n", text);
                Assert.Matches("::group::Compile\n[^\n]+ Compile \\(0ms\\)\n::endgroup::\n", text);
                Assert.DoesNotContain("::error", target.Text);
                Assert.DoesNotContain("Compile", target.Text);
            } finally { Console.SetOut(original); }
        }

        [Fact]
        public void ForcedProviderKeepsRunsAppendOnlyEvenWhenLiveWasRequested() {
            using (ContinuousIntegration.UseProvider(ContinuousIntegrationProvider.GitHub))
            using (var monitor = new RunMonitor(0, new RunOptions { Live = true, Print = _ => { } })) {
                Assert.False(monitor.IsLive);
                Assert.Null(monitor.Session);
            }
        }
        [Fact]
        public void TerminalFallbackKeepsSeverityAndLiteralMarkupWithoutProviderCommands() {
            var target = global::ConsoleMonster.Testing.TestTerminal.Target(width: 300);
            using (Terminal.Use(target))
            using (ContinuousIntegration.UseProvider(ContinuousIntegrationProvider.None)) {
                using (ContinuousIntegration.Group("Build [literal]")) {
                    ContinuousIntegration.Annotate(AnnotationLevel.Error, "literal [pass]value[/]", file: "build.ps1", line: 4);
                }
            }
            Assert.StartsWith("Build [literal]", target.Text);
            Assert.Contains("Error: literal [pass]value[/] (build.ps1:4", target.Text);
            Assert.DoesNotContain("::", target.Text);
        }
        [Fact]
        public async Task ConcurrentRunCompletionsAreWholeGroupsAndKeepTheirResults() {
            var lines = new List<string>();
            using (ContinuousIntegration.UseProvider(ContinuousIntegrationProvider.GitHub)) {
                var results = await ConsoleRun.ForEachAsync(new[] { "Build", "Test" }, (item, _) => Task.FromResult(item),
                    new RunOptions { Live = false, ThrottleLimit = 2, Print = lines.Add });
                Assert.All(results, result => Assert.True(result.Succeeded));
            }
            Assert.Equal(7, lines.Count);
            for (int i = 0; i < 6; i += 3) {
                string title = lines[i].Substring("::group::".Length);
                Assert.Contains(title, new[] { "Build", "Test" });
                Assert.Contains(title + " (", lines[i + 1]);
                Assert.Equal("::endgroup::", lines[i + 2]);
            }
            Assert.StartsWith("2 items: 2 passed, 0 failed", lines[6]);
        }
        [Theory]
        [InlineData(ContinuousIntegrationProvider.GitHub, "::group::Build%25%0D%0Asecond", "::endgroup::")]
        [InlineData(ContinuousIntegrationProvider.Azure, "##[group]Build%  second", "##[endgroup]")]
        [InlineData(ContinuousIntegrationProvider.None, "Build%  second", null)]
        public void GroupsNestAndCloseOnceWhenWorkThrows(ContinuousIntegrationProvider provider, string opening, string? closing) {
            var lines = new List<string>();
            using (ContinuousIntegration.UseProvider(provider)) {
                Assert.Throws<InvalidOperationException>((Action)(() => {
                    using (ContinuousIntegration.Group("Build%\r\nsecond", lines.Add)) {
                        IDisposable inner = ContinuousIntegration.Group("Test", lines.Add);
                        try { throw new InvalidOperationException("failed"); }
                        finally { inner.Dispose(); inner.Dispose(); }
                    }
                }));
            }
            Assert.Equal(opening, lines[0]);
            Assert.Equal(closing == null ? new[] { opening, "Test" } : new[] { opening, provider == ContinuousIntegrationProvider.GitHub ? "::group::Test" : "##[group]Test", closing, closing }, lines);
        }

        [Fact]
        public void GitLabUsesCollapsedSectionsWithUniqueMatchingNamesAndUnixTimes() {
            var lines = new List<string>();
            using (ContinuousIntegration.UseProvider(ContinuousIntegrationProvider.GitLab)) {
                using (ContinuousIntegration.Group("Same title\r\nnext\x1b", lines.Add)) {
                    using (ContinuousIntegration.Group("Same title", lines.Add)) { }
                }
            }
            Match first = Regex.Match(lines[0], "^\x1b\\[0Ksection_start:([0-9]+):(consolemonster_[0-9]+)\\[collapsed=true\\]\r\x1b\\[0KSame title  next $");
            Match second = Regex.Match(lines[1], "^\x1b\\[0Ksection_start:([0-9]+):(consolemonster_[0-9]+)\\[collapsed=true\\]\r\x1b\\[0KSame title$");
            Assert.True(first.Success, lines[0]);
            Assert.True(second.Success, lines[1]);
            Assert.NotEqual(first.Groups[2].Value, second.Groups[2].Value);
            Assert.InRange(long.Parse(first.Groups[1].Value), DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(), DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            Assert.Matches("^\x1b\\[0Ksection_end:[0-9]+:" + second.Groups[2].Value + "\r\x1b\\[0K$", lines[2]);
            Assert.Matches("^\x1b\\[0Ksection_end:[0-9]+:" + first.Groups[2].Value + "\r\x1b\\[0K$", lines[3]);
        }

        [Theory]
        [InlineData(AnnotationLevel.Error, "error")]
        [InlineData(AnnotationLevel.Warning, "warning")]
        [InlineData(AnnotationLevel.Notice, "notice")]
        public void GitHubEscapesLiteralMessageAndProperties(AnnotationLevel level, string command) {
            var lines = new List<string>();
            using (ContinuousIntegration.UseProvider(ContinuousIntegrationProvider.GitHub)) {
                ContinuousIntegration.Annotate(level, "literal %0A\r\n::error::x,:", "Title,:\r\n%", "a,b:c%\r\n.ps1", 12, 3, lines.Add);
            }
            Assert.Equal("::" + command + " file=a%2Cb%3Ac%25%0D%0A.ps1,line=12,col=3,title=Title%2C%3A%0D%0A%25::literal %250A%0D%0A::error::x,:", Assert.Single(lines));
        }

        [Theory]
        [InlineData(AnnotationLevel.Error, "error")]
        [InlineData(AnnotationLevel.Warning, "warning")]
        public void AzureEscapesItsOwnParserDelimiters(AnnotationLevel level, string command) {
            var lines = new List<string>();
            string file = Path.GetFullPath("source;]%.ps1");
            using (ContinuousIntegration.UseProvider(ContinuousIntegrationProvider.Azure)) {
                ContinuousIntegration.Annotate(level, "text %3B;]\r\n##vso[x]", "Title", file, 12, 3, lines.Add);
            }
            Assert.Equal("##vso[task.logissue type=" + command + ";sourcepath=" + Path.Combine(Environment.CurrentDirectory, "source%3B%5D%AZP25.ps1") + ";linenumber=12;columnnumber=3]Title: text %AZP253B%3B%5D%0D%0A##vso[x%5D", Assert.Single(lines));
        }

        [Fact]
        public void AzureLiteralTitlesNoticesAndRunDetailsCannotBecomeAgentCommands() {
            var lines = new List<string>();
            var target = global::ConsoleMonster.Testing.TestTerminal.Target(width: 200);
            using (ContinuousIntegration.UseProvider(ContinuousIntegrationProvider.Azure)) {
                using (ContinuousIntegration.Group("Build ##vso[task.setvariable variable=X]changed", lines.Add)) { }
                ContinuousIntegration.Annotate(AnnotationLevel.Notice, "##vso[task.complete result=Failed]no", write: lines.Add);
                using (var monitor = new RunMonitor(1, new RunOptions { Live = false, Print = lines.Add })) {
                    monitor.Finished(0, StatusKind.Fail, "##vso[task.setvariable variable=X]changed", TimeSpan.Zero, "##[error]literal");
                }
                using (Terminal.Use(target)) { Terminal.Write(TextBlock.Plain("##vso[task.setvariable variable=X]changed")); }
            }
            Assert.Equal("##[group]Build ##vso%5Btask.setvariable variable=X]changed", lines[0]);
            Assert.Equal("Notice: ##vso%5Btask.complete result=Failed]no", lines[2]);
            Assert.Contains(lines, line => line.Contains("##vso%5Btask.setvariable variable=X]changed (0ms): ##%5Berror]literal"));
            Assert.All(lines, line => Assert.DoesNotContain("##vso[", line));
            Assert.Contains("##vso%5Btask.setvariable variable=X]changed", target.Text);
            Assert.DoesNotContain("##vso[", target.Text);
        }

        [Theory]
        [InlineData(ContinuousIntegrationProvider.Azure, AnnotationLevel.Notice)]
        [InlineData(ContinuousIntegrationProvider.GitLab, AnnotationLevel.Error)]
        [InlineData(ContinuousIntegrationProvider.None, AnnotationLevel.Warning)]
        public void ProvidersWithoutAnnotationsUseOneOrdinaryStatusLine(ContinuousIntegrationProvider provider, AnnotationLevel level) {
            var lines = new List<string>();
            using (ContinuousIntegration.UseProvider(provider)) { ContinuousIntegration.Annotate(level, "message\n::error::no", file: "source.ps1", line: 2, write: lines.Add); }
            Assert.Equal(level + ": message ::error::no (source.ps1:2)", Assert.Single(lines));
        }

        [Fact]
        public void AutomaticErrorsHaveOneTenAnnotationBudgetPerRunAndOneOverflowLine() {
            var lines = new List<string>();
            using (ContinuousIntegration.UseProvider(ContinuousIntegrationProvider.GitHub)) {
                using (var batch = new AnnotationBatch(lines.Add)) {
                    for (int i = 0; i < 13; i++) { batch.Error("failure", "test", "test.ps1", i + 1); }
                    batch.Dispose();
                }
                using (var next = new AnnotationBatch(lines.Add)) { next.Error("next run", null, null, 0); }
            }
            Assert.Equal(12, lines.Count);
            Assert.Equal(10, lines.Take(10).Count(line => line.StartsWith("::error file=test.ps1,")));
            Assert.Equal("3 additional errors omitted from CI annotations.", lines[10]);
            Assert.Equal("::error::next run", lines[11]);
        }

        [Theory]
        [InlineData(ContinuousIntegrationProvider.GitHub)]
        [InlineData(ContinuousIntegrationProvider.Azure)]
        [InlineData(ContinuousIntegrationProvider.GitLab)]
        [InlineData(ContinuousIntegrationProvider.None)]
        public void SummariesAppendToGitHubOrUploadUtf8AzureFilesOtherwiseOnlyReturnMarkdown(ContinuousIntegrationProvider provider) {
            string root = Path.Combine(Path.GetTempPath(), "ConsoleMonster-CI-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string summary = Path.Combine(root, "github.md");
            string? oldSummary = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            string? oldRoot = Environment.GetEnvironmentVariable("SYSTEM_DEFAULTWORKINGDIRECTORY");
            try {
                Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", summary);
                Environment.SetEnvironmentVariable("SYSTEM_DEFAULTWORKINGDIRECTORY", root);
                var lines = new List<string>();
                using (ContinuousIntegration.UseProvider(provider)) {
                    Assert.Equal("| Check | Result |\n| --- | --- |\n| Łódź | Passed |", ContinuousIntegration.AddSummary("| Check | Result |\n| --- | --- |\n| Łódź | Passed |", lines.Add));
                    ContinuousIntegration.AddSummary("second", lines.Add);
                }
                if (provider == ContinuousIntegrationProvider.GitHub) {
                    Assert.Equal("| Check | Result |\n| --- | --- |\n| Łódź | Passed |\nsecond\n", File.ReadAllText(summary));
                    Assert.Empty(lines);
                } else if (provider == ContinuousIntegrationProvider.Azure) {
                    Assert.Equal(2, lines.Count);
                    string[] files = Directory.GetFiles(root, "consolemonster-summary-*.md").OrderBy(File.GetCreationTimeUtc).ToArray();
                    Assert.Equal(2, files.Length);
                    Assert.All(files, file => Assert.Contains("##vso[task.uploadsummary]" + file, lines));
                    Assert.Contains(files, file => File.ReadAllText(file) == "| Check | Result |\n| --- | --- |\n| Łódź | Passed |\n");
                    Assert.All(files, file => Assert.False(File.ReadAllBytes(file).Take(3).SequenceEqual(new byte[] { 239, 187, 191 })));
                } else { Assert.Empty(Directory.GetFiles(root)); Assert.Empty(lines); }
            } finally {
                Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", oldSummary);
                Environment.SetEnvironmentVariable("SYSTEM_DEFAULTWORKINGDIRECTORY", oldRoot);
                Directory.Delete(root, true);
            }
        }
    }
}
