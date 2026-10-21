using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Extensions.Logging;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ConsoleMonster.Extensions.Tests {
    [CollectionDefinition("Logging sessions", DisableParallelization = true)]
    public sealed class LoggingSessionCollection { }

    [Collection("Logging sessions")]
    public sealed class LoggingConcurrencyTests {
        private static ILoggerFactory Factory(StringTerminalWriter writer, string? region = null) => LoggerFactory.Create(builder => builder.AddConsoleMonster(options => {
            options.Profile = TerminalProfile.Fixed(160, 24, ColorDepth.None, GlyphTier.Ascii, OutputMode.Stream);
            options.Writer = writer;
            options.LogRegion = region;
            options.TimestampFormat = null;
            options.Category = LogCategoryFormat.None;
            options.MaxExceptionLines = 0;
        }));

        [Fact]
        public async Task ConcurrentExceptionEntries_KeepMessagesDetailsAndScopesTogether() {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (ILoggerFactory factory = Factory(writer)) {
                ILogger logger = factory.CreateLogger("Workers");
                await Task.WhenAll(Enumerable.Range(0, 8).Select(producer => Task.Run(() => {
                    using (logger.BeginScope("producer" + producer)) {
                        for (int i = 0; i < 32; i++) {
                            string key = producer + ":" + i;
                            logger.LogError(new InvalidOperationException("fault[" + key + "]"), "head[" + key + "]\ntail[" + key + "]");
                        }
                    }
                })));
            }
            string text = writer.ToString();
            MatchCollection heads = Regex.Matches(text, @"head\[(\d+):(\d+)\]");
            Assert.Equal(8 * 32, heads.Count);
            for (int i = 0; i < heads.Count; i++) {
                Match head = heads[i];
                string key = head.Groups[1].Value + ":" + head.Groups[2].Value;
                int end = i + 1 == heads.Count ? text.Length : heads[i + 1].Index;
                string block = text.Substring(head.Index, end - head.Index);
                Assert.Contains("tail[" + key + "]", block);
                Assert.Contains("fault[" + key + "]", block);
                Assert.Single(Regex.Matches(block, @"fault\["));
                int beginning = text.LastIndexOf('\n', head.Index);
                Assert.Contains("(producer" + head.Groups[1].Value + ")", text.Substring(beginning + 1, head.Index - beginning - 1));
            }
            Assert.Equal(8 * 32, Regex.Matches(text, @"tail\[").Count);
        }

        [Fact]
        public async Task ConcurrentSessionLogging_DeliversEveryEntryAndAnExceptionToItsExplicitSink() {
            const int count = 8 * 64;
            LogRegion log = new LogRegion { Capacity = count };
            StringTerminalWriter screen = new StringTerminalWriter();
            StringTerminalWriter exceptions = new StringTerminalWriter();
            TerminalProfile profile = TerminalProfile.Fixed(160, 24, ColorDepth.None, GlyphTier.Ascii, OutputMode.Stream);
            using (LineOutput output = new LineOutput(screen, profile, false))
            using (LiveSession session = new LiveSession(new Rows(log), new LiveSessionOptions {
                Output = output, Profile = profile, CaptureConsole = false, HandleCancelKey = false, PrintFinalState = false
            }))
            using (ILoggerFactory factory = Factory(exceptions, "$log")) {
                ILogger logger = factory.CreateLogger("Workers");
                session.Start();
                await Task.WhenAll(Enumerable.Range(0, 8).Select(producer => Task.Run(() => {
                    for (int i = 0; i < 64; i++) {
                        logger.LogInformation("entry-" + producer + "-" + i);
                        if (producer == 3 && i == 17) { logger.LogError(new InvalidOperationException("session-fault"), "session-exception\nexception-detail"); }
                    }
                })));
                session.Stop();
                Assert.Equal(count, log.TotalLines);
                Assert.Equal(Enumerable.Range(0, 8).SelectMany(p => Enumerable.Range(0, 64).Select(i => "entry-" + p + "-" + i)).OrderBy(s => s),
                    log.Entries.Select(entry => entry.RichText?.PlainText ?? entry.Text).OrderBy(s => s));
                Assert.Equal(0, session.EventsDropped);
                Assert.Equal(session.EventsPosted, session.EventsApplied);
                Assert.Contains("session-exception", exceptions.ToString());
                Assert.Contains("exception-detail", exceptions.ToString());
                Assert.Contains("session-fault", exceptions.ToString());
                Assert.Contains("InvalidOperationException", exceptions.ToString());
                Assert.DoesNotContain("session-exception", screen.ToString());
            }
        }
    }
}
