using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Live;
using ConsoleMonster.Live.Tails;
using ConsoleMonster.Localization;
using ConsoleMonster.Output;
using Xunit;

namespace ConsoleMonster.Tests.Live.Tails {
    [Collection("Live sessions")]
    public sealed class ConsoleLogTailTests {
        [Fact]
        public void PlainOutputAndFilterUseOriginalLineWhenLiveMessageOmitsMetadata() {
            const string original = "2026-10-22T09:31:09Z ERROR [red]database timeout[/]";
            TailedLogEntry entry = LogLineParser.Parse(original);
            Assert.Equal("[red]database timeout[/]", entry.Message);
            List<string> lines = new List<string>();
            ConsoleLogTail.Run(() => new[] { entry }, new LogTailOptions {
                Live = false, Count = 1, Filter = "ERROR", Highlight = "database", WriteLine = lines.Add,
                HandleCancelKey = false
            });
            Assert.Equal(new[] { original }, lines);
            Assert.False(ConsoleInput.IsAcquired);
        }

        [Fact]
        public void LevelKeepsWarningsAndErrorsAndDropsQuieterLinesAtTheSource() {
            string[] source = { "INFO started", "WARN disk 91%", "DEBUG cache hit", "ERROR backup failed", "VERBOSE probe" };
            List<string> lines = new List<string>();
            ConsoleLogTail.Run(() => source.Select(LogLineParser.Parse).ToList(), new LogTailOptions {
                Live = false, Count = 1, Level = "warn", WriteLine = lines.Add, HandleCancelKey = false
            });
            Assert.Equal(new[] { "WARN disk 91%", "ERROR backup failed" }, lines);
        }

        [Theory]
        [InlineData("plain")]
        [InlineData("redirected")]
        [InlineData("ci")]
        [InlineData("stream")]
        [InlineData("no-vt")]
        [InlineData("non-live-output")]
        public void PlainFallbackEmitsOriginalLinesAndNeverReadsInput(string reason) {
            List<string> lines = new List<string>();
            NeverInput input = new NeverInput();
            TerminalProfile profile = new TerminalProfile(ColorDepth.TrueColor, GlyphTier.Full,
                reason == "stream" ? OutputMode.Plain : OutputMode.Interactive,
                supportsVirtualTerminal: reason != "no-vt", isOutputRedirected: reason == "redirected",
                isContinuousIntegration: reason == "ci");
            FakeOutput output = new FakeOutput(reason != "non-live-output");
            int reads = 0;
            ConsoleLogTail.Run(() => {
                reads++;
                return new[] { new TailedLogEntry("[red]raw[/]", "err"), new TailedLogEntry("[09:31] WARN literal"), new TailedLogEntry("third original") };
            }, new LogTailOptions {
                Profile = profile, Live = reason != "plain", Count = 1, WriteLine = lines.Add, Input = input,
                Output = output, HandleCancelKey = false
            });
            Assert.Equal(new[] { "[red]raw[/]", "[09:31] WARN literal", "third original" }, lines);
            Assert.Equal(1, reads);
            Assert.Equal(0, input.Reads);
            Assert.False(input.Disposed);
            Assert.Empty(output.Calls);
        }

        [Fact]
        public void PlainDefaultWriterPreservesLiteralLinesAndLeavesBorrowedWriterOpen() {
            using (StringWriter writer = new StringWriter())
            using (Terminal.Use(new WriterTarget(writer))) {
                ConsoleLogTail.Run(() => new[] {
                    new TailedLogEntry("[red]raw[/]", "error"), new TailedLogEntry("  tabs\tstay literal", "warn")
                }, new LogTailOptions { Count = 1, HandleCancelKey = false });
                Assert.Equal("[red]raw[/]" + Environment.NewLine + "  tabs\tstay literal" + Environment.NewLine, writer.ToString());
                writer.Write("open");
                Assert.EndsWith("open", writer.ToString());
                Assert.False(ConsoleInput.IsAcquired);
            }
        }

        [Fact]
        public void CountIncludesEmptySnapshotsAndProcessesEveryLineInEachSnapshot() {
            int reads = 0;
            List<string> lines = new List<string>();
            ConsoleLogTail.Run(() => ++reads == 1 ? Array.Empty<TailedLogEntry>() : new[] { new TailedLogEntry("L" + reads) },
                new LogTailOptions { Live = false, WriteLine = lines.Add, Count = 3, PollInterval = TimeSpan.FromMilliseconds(1), HandleCancelKey = false });
            Assert.Equal(new[] { "L2", "L3" }, lines);
            Assert.Equal(3, reads);
        }

        [Fact]
        public void LiteralFilterIsCaseInsensitiveAndPlainHighlightNeverChangesOriginalText() {
            List<string> lines = new List<string>();
            ConsoleLogTail.Run(() => new[] {
                new TailedLogEntry("[red]WARN.A[/]"), new TailedLogEntry("WARNxA"), new TailedLogEntry("warn.a")
            }, new LogTailOptions {
                Live = false, Count = 1, Filter = "warn.a", Highlight = "[red]", WriteLine = lines.Add, HandleCancelKey = false
            });
            Assert.Equal(new[] { "[red]WARN.A[/]", "warn.a" }, lines);
        }

        [Fact]
        public void ReaderContractErrorUsesProfileLabelResources() {
            TerminalProfile profile = TerminalProfile.Fixed(80, 10).WithLanguage(System.Globalization.CultureInfo.GetCultureInfo("pl-PL"));
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => ConsoleLogTail.Run(
                () => new TailedLogEntry[1], new LogTailOptions {
                    Profile = profile, Live = false, Count = 1, WriteLine = _ => { }, HandleCancelKey = false
                }));
            Assert.Equal(WidgetLabels.For(profile.Language).TailReaderReturnedNullEntry, error.Message);
            Assert.Equal(error.Message, LabelCatalog.Entries("pl")["Widget.TailReaderReturnedNullEntry"]);
            Assert.NotEqual(LabelCatalog.Entries("en")["Widget.TailReaderReturnedNullEntry"], error.Message);
        }

        [Fact]
        public void FilteredEmptySnapshotStillCountsAndDoesNotReadAgain() {
            int reads = 0;
            List<string> lines = new List<string>();
            ConsoleLogTail.Run(() => {
                reads++;
                return new[] { new TailedLogEntry("excluded") };
            }, new LogTailOptions { Live = false, Count = 1, Filter = "absent", WriteLine = lines.Add, HandleCancelKey = false });
            Assert.Equal(1, reads);
            Assert.Empty(lines);
        }

        [Fact]
        public void PlainCancellationStopsWaitingWithoutAnotherRead() {
            using (CancellationTokenSource stop = new CancellationTokenSource()) {
                int reads = 0;
                List<string> lines = new List<string>();
                ConsoleLogTail.Run(() => { reads++; return new[] { new TailedLogEntry("last") }; }, new LogTailOptions {
                    Live = false, WriteLine = text => { lines.Add(text); stop.Cancel(); }, HandleCancelKey = false
                }, stop.Token);
                Assert.Single(lines);
                Assert.Equal(1, reads);
            }
        }

        [Fact]
        public void PrecancelledRunDoesNotCallReaderOrAcquireInput() {
            using (CancellationTokenSource stop = new CancellationTokenSource()) {
                stop.Cancel();
                ConsoleLogTail.Run(() => throw new InvalidOperationException("reader invoked"), cancellationToken: stop.Token);
            }
            Assert.False(ConsoleInput.IsAcquired);
        }

        [Theory]
        [InlineData("q")]
        [InlineData("Q")]
        [InlineData("Escape")]
        [InlineData("Ctrl+C")]
        public void InteractiveQuitRestoresSessionAndReleasesGlobalInputLease(string script) {
            FakeOutput output = new FakeOutput();
            ScriptedInput input = new ScriptedInput(script);
            ConsoleInput.Override = input;
            int reads = 0;
            try {
                ConsoleLogTail.Run(() => {
                    Assert.True(ConsoleInput.IsAcquired);
                    reads++;
                    return new[] { new TailedLogEntry("literal") };
                }, Options(output));
                Assert.Equal(1, reads);
                Assert.Equal(0, input.Remaining);
                Assert.False(ConsoleInput.IsAcquired);
                Assert.Equal(1, output.Calls.Count(c => c == "begin"));
                Assert.Equal(1, output.Calls.Count(c => c == "end"));
                Assert.DoesNotContain("dispose", output.Calls);
                Assert.Null(LiveSession.Current);
            } finally { ConsoleInput.Override = null; input.Dispose(); }
        }

        [Fact]
        public void InteractiveCountDrainsAcceptedUpdatesWithoutReadingKeys() {
            FakeOutput output = new FakeOutput();
            NeverInput input = new NeverInput();
            LogTailOptions options = Options(output);
            options.Input = input;
            options.Count = 1;
            ConsoleLogTail.Run(() => new[] { new TailedLogEntry("one"), new TailedLogEntry("two") }, options);
            Assert.Contains("end", output.Calls);
            Assert.DoesNotContain("line", output.Calls);
            Assert.Equal(0, input.Reads);
            Assert.False(input.Disposed);
            Assert.False(ConsoleInput.IsAcquired);
        }

        [Fact]
        public void ReaderFailureRestoresSessionAndReleasesLeaseBeforeRethrowing() {
            FakeOutput output = new FakeOutput();
            ConsoleInput.Override = new ScriptedInput("q");
            try {
                InvalidOperationException expected = new InvalidOperationException("reader failed");
                Exception actual = Assert.Throws<InvalidOperationException>(() => ConsoleLogTail.Run(() => throw expected, Options(output)));
                Assert.Same(expected, actual);
                Assert.False(ConsoleInput.IsAcquired);
                Assert.Contains("end", output.Calls);
            } finally { ConsoleInput.Override.Dispose(); ConsoleInput.Override = null; }
        }

        [Fact]
        public void InteractiveInputClosureEndsNormally() {
            FakeOutput output = new FakeOutput();
            LogTailOptions options = Options(output);
            options.Input = new ScriptedInput("");
            ConsoleLogTail.Run(() => Array.Empty<TailedLogEntry>(), options);
            Assert.Contains("end", output.Calls);
        }

        [Fact]
        public void InteractiveCallerCancellationReleasesLeaseAndRestoresSession() {
            FakeOutput output = new FakeOutput();
            using (CancellationTokenSource stop = new CancellationTokenSource()) {
                LogTailOptions options = Options(output);
                options.Input = new NeverInput();
                ConsoleLogTail.Run(() => { stop.Cancel(); return new[] { new TailedLogEntry("unaccepted") }; }, options, stop.Token);
                Assert.Contains("end", output.Calls);
                Assert.False(ConsoleInput.IsAcquired);
            }
        }

        [Fact]
        public void PlainCallbackFailurePropagatesTheOriginalException() {
            InvalidOperationException expected = new InvalidOperationException("writer failed");
            Exception actual = Assert.Throws<InvalidOperationException>(() => ConsoleLogTail.Run(
                () => new[] { new TailedLogEntry("literal") },
                new LogTailOptions { Live = false, WriteLine = _ => throw expected, HandleCancelKey = false }));
            Assert.Same(expected, actual);
            Assert.False(ConsoleInput.IsAcquired);
        }

        [Fact]
        public void InvalidOptionsFailBeforeReaderOrInput() {
            LogTailOptions[] invalid = {
                new LogTailOptions { Capacity = 0 }, new LogTailOptions { Count = -1 },
                new LogTailOptions { PollInterval = TimeSpan.Zero },
                new LogTailOptions { PollInterval = TimeSpan.FromMilliseconds((double)int.MaxValue + 1) },
                new LogTailOptions { Mode = (LiveMode)99 }
            };
            foreach (LogTailOptions options in invalid) {
                Assert.Throws<ArgumentOutOfRangeException>(() => ConsoleLogTail.Run(() => throw new Exception("read"), options));
            }
            Assert.False(ConsoleInput.IsAcquired);
        }

        private static LogTailOptions Options(FakeOutput output) => new LogTailOptions {
            Profile = output.Profile, Output = output, HandleCancelKey = false, PollInterval = TimeSpan.FromSeconds(1)
        };

        private sealed class NeverInput : IInputSource {
            internal int Reads;
            internal bool Disposed;
            public string Name => "never";
            public InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken) {
                Reads++;
                throw new InvalidOperationException("Input must not be read in this test.");
            }
            public void Dispose() { Disposed = true; }
        }
    }
}
