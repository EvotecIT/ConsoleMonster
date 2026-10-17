using System;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.TestRuns;
using Xunit;

namespace ConsoleMonster.Tests.TestRuns {
    /// <summary>The test run view, summary and stream lines, fed with synthetic test events (no test framework involved).</summary>
    public sealed class TestRunViewTests {
        private const string Sample = @"C:\repo\Tests\Sample.Tests.ps1";
        private const string Broken = @"C:\repo\Tests\Broken.Tests.ps1";
        private const string Module = @"C:\repo\Tests\Module.Tests.ps1";

        private static TimeSpan _now;

        private static TestRun NewRun() {
            _now = TimeSpan.Zero;
            return new TestRun(() => _now);
        }

        private static TestRecord Test(string container, string name, StatusKind status, int ms, params string[] blocks) {
            return new TestRecord(container, name, status) { Duration = TimeSpan.FromMilliseconds(ms), Blocks = blocks.Length == 0 ? new[] { "Math" } : blocks, File = container, Line = 2 };
        }

        // Three files: Broken fails in BeforeAll, Sample has two assertion failures and a skip, Module is still running.
        private static TestRun MidRun() {
            TestRun run = NewRun();
            run.Discovered(12);
            run.ContainerStarted(Broken, "Broken.Tests.ps1", Broken, 1);
            run.TestFinished(new TestRecord(Broken, "Broken", StatusKind.Fail) { IsBlockFailure = true, Message = "setup broke", File = Broken, Line = 2 });
            run.TestFinished(new TestRecord(Broken, "never", StatusKind.Fail) { Blocks = new[] { "Broken" } });
            _now = TimeSpan.FromMilliseconds(40);
            run.ContainerFinished(Broken);
            run.ContainerStarted(Sample, "Sample.Tests.ps1", Sample, 6);
            run.TestFinished(Test(Sample, "adds", StatusKind.Pass, 104));
            run.TestFinished(new TestRecord(Sample, "fails string", StatusKind.Fail) {
                Blocks = new[] { "Math" }, Duration = TimeSpan.FromMilliseconds(27), File = Sample, Line = 3,
                Message = "Expected strings to be the same, but they were different.\nString lengths are both 5.\nStrings differ at index 2.\nExpected: 'abxde'\nBut was:  'abcde'\n           --^"
            });
            run.TestFinished(new TestRecord(Sample, "fails number", StatusKind.Fail) { Blocks = new[] { "Math" }, Duration = TimeSpan.FromMilliseconds(4), File = Sample, Line = 4, Message = "Expected 4, but got 3." });
            run.TestFinished(Test(Sample, "skips", StatusKind.Skip, 1));
            run.TestFinished(Test(Sample, "slow 1", StatusKind.Pass, 149, "Math", "nested"));
            run.TestFinished(Test(Sample, "slow 2", StatusKind.Pass, 1210, "Math", "nested"));
            _now = TimeSpan.FromMilliseconds(1600);
            run.ContainerFinished(Sample);
            run.ContainerStarted(Module, "Module.Tests.ps1", Module, 5);
            run.TestFinished(Test(Module, "imports", StatusKind.Pass, 320, "Module"));
            run.TestFinished(Test(Module, "exports", StatusKind.Pass, 12, "Module"));
            run.TestStarted(Module, new[] { "Module", "Help" }, "has help for every cmdlet");
            return run;
        }

        private static TestRun Finished() {
            TestRun run = MidRun();
            run.TestFinished(Test(Module, "has help for every cmdlet", StatusKind.Pass, 60, "Module", "Help"));
            run.TestFinished(Test(Module, "has examples", StatusKind.Pass, 8, "Module", "Help"));
            run.TestFinished(new TestRecord(Module, "is signed", StatusKind.Warn) { Blocks = new[] { "Module" }, Duration = TimeSpan.FromMilliseconds(2) });
            _now = TimeSpan.FromMilliseconds(2100);
            run.ContainerFinished(Module);
            run.Finished();
            return run;
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void FullLayout_MidRun_MatchesGolden(GlyphTier tier) {
            string text = TestTerminal.RenderText(new TestRunView(MidRun()) { Title = "Pester" }, 110, 34, tier);
            Golden.Match("testrun/full.midrun." + tier.ToString().ToLowerInvariant() + ".txt", text);
        }

        [Fact]
        public void FullLayout_Finished_MatchesGolden() {
            string text = TestTerminal.RenderText(new TestRunView(Finished()) { Title = "Pester" }, 110, 34);
            Golden.Match("testrun/full.finished.txt", text);
        }

        [Theory]
        [InlineData(GlyphTier.Full, 100)]
        [InlineData(GlyphTier.Basic, 80)]
        public void CompactLayout_MatchesGolden(GlyphTier tier, int width) {
            TestRunView view = new TestRunView(MidRun()) { Title = "Pester", Compact = true };
            string text = TestTerminal.RenderText(view, width, 0, tier);
            Golden.Match("testrun/compact." + tier.ToString().ToLowerInvariant() + "." + width + ".txt", text);
        }

        [Fact]
        public void CompactLayout_HeightFollowsContent() {
            TestRun run = NewRun();
            run.ContainerStarted(Sample, "Sample.Tests.ps1", Sample, 2);
            TestRunView view = new TestRunView(run) { Compact = true };
            int before = TestTerminal.Render(view, 80).Height;
            run.TestFinished(new TestRecord(Sample, "fails", StatusKind.Fail) { Message = "Expected 1, but got 2.", File = Sample, Line = 9 });
            int after = TestTerminal.Render(view, 80).Height;
            Assert.Equal(8, before);
            Assert.True(after > before + 3, "the failures panel adds rows: " + before + " -> " + after);
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void Summary_MatchesGolden(GlyphTier tier) {
            string text = TestTerminal.RenderText(new TestRunSummary(Finished().Snapshot()), 100, 0, tier);
            Golden.Match("testrun/summary." + tier.ToString().ToLowerInvariant() + ".txt", text);
        }

        [Fact]
        public void StreamLines_OnePerFailurePlusSummary() {
            TestRunSnapshot snapshot = Finished().Snapshot();
            StringBuilder sb = new StringBuilder();
            foreach (IGlyphSet glyphs in new IGlyphSet[] { GlyphSet.Full, GlyphSet.Ascii }) {
                foreach (TestRecord failure in snapshot.Failures) {
                    sb.Append(TestRunText.FailureLine(failure, glyphs)).Append('\n');
                }
                sb.Append(TestRunText.SummaryLine(snapshot, glyphs)).Append('\n');
            }
            Golden.Match("testrun/stream.txt", sb.ToString());
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void LongNames_AreTruncatedWithTheTierEllipsis(GlyphTier tier) {
            TestRun run = NewRun();
            run.ContainerStarted(Sample, "Sample.Tests.ps1", Sample, 3);
            run.TestFinished(Test(Sample, "writes the pieces on one line and survives a narrow window without cutting words", StatusKind.Pass, 2700));
            run.TestFinished(new TestRecord(Sample, "fails with a message that names the parameter and the value it could not convert", StatusKind.Fail) {
                Blocks = new[] { "Interactive cmdlets", "Without a user (redirected input, -NonInteractive)" }, File = Sample, Line = 40,
                Message = "Cannot convert the value 'abc' of the parameter -Count to the type System.Int32 because it is not a number at all"
            });
            run.TestStarted(Sample, new[] { "Rich text cmdlets", "Write-ConsoleText with Write-Color parameters" }, "applies StartTab, StartSpaces, LinesBefore and LinesAfter");
            string text = TestTerminal.RenderText(new TestRunView(run) { Title = "Pester" }, 90, 30, tier);
            Golden.Match("testrun/truncation." + tier.ToString().ToLowerInvariant() + ".txt", text);
            string ellipsis = tier == GlyphTier.Ascii ? "..." : "…";
            Assert.Contains("writes the pieces on", text);
            foreach (string line in text.Split('\n')) {
                if (line.Contains("writes the pieces") || line.Contains("applies StartTab") || line.Contains("Cannot convert")) {
                    Assert.Contains(ellipsis, line);
                }
            }
        }
        [Theory]
        [InlineData("Expected 4, but got 3.", "4", "3")]
        [InlineData("Expected strings to be the same, but they were different.\nExpected: 'abxde'\nBut was:  'abcde'", "abxde", "abcde")]
        [InlineData("Expected [int] 4, but got [int] 3.", "[int] 4", "[int] 3")]
        [InlineData("Expected $true, but got $false.", "$true", "$false")]
        public void TryParseExpectation_ReadsExpectedAndActual(string message, string expected, string actual) {
            Assert.True(TestRunText.TryParseExpectation(message, out string e, out string a));
            Assert.Equal(expected, e);
            Assert.Equal(actual, a);
        }

        [Fact]
        public void TryParseExpectation_OtherMessages_ReturnFalse() {
            Assert.False(TestRunText.TryParseExpectation("setup broke", out _, out _));
            Assert.False(TestRunText.TryParseExpectation(null, out _, out _));
        }

        [Fact]
        public void Run_CountsAndStatuses() {
            TestRunSnapshot snapshot = Finished().Snapshot();
            Assert.Equal(12, snapshot.Total);
            Assert.Equal(7, snapshot.Passed);
            Assert.Equal(3, snapshot.Failed);
            Assert.Equal(1, snapshot.Skipped);
            Assert.Equal(1, snapshot.Inconclusive);
            Assert.Equal(1, snapshot.BlockFailures);
            Assert.Equal(StatusKind.Fail, snapshot.Status);
            Assert.Equal(3, snapshot.Failures.Count);
            Assert.Equal("slow 2", snapshot.Slowest[0].Name);
            Assert.Equal(new[] { StatusKind.Fail, StatusKind.Fail, StatusKind.Warn }, new List<TestContainerState>(snapshot.Containers).ConvertAll(c => c.Status).ToArray());
            Assert.Equal(TimeSpan.FromMilliseconds(2100), snapshot.Elapsed);
        }
    }
}
