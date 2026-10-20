using System;
using System.Linq;
using System.Threading;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Extensions.Logging;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Widgets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ConsoleMonster.Extensions.Tests {
    /// <summary>The logging provider: line layout, levels, scopes, exceptions, the live log region and the DI extension.</summary>
    public sealed class LoggingTests {
        private static (ILoggerFactory Factory, StringTerminalWriter Writer) Factory(Action<ConsoleMonsterLoggerOptions>? configure = null, OutputMode mode = OutputMode.Plain) {
            StringTerminalWriter writer = new StringTerminalWriter();
            ILoggerFactory factory = LoggerFactory.Create(logging => logging.SetMinimumLevel(LogLevel.Trace).AddConsoleMonster(options => {
                options.Profile = TerminalProfile.Fixed(120, 30, ColorDepth.None, GlyphTier.Ascii, mode);
                options.Writer = writer;
                options.TimestampFormat = null;
                options.LogRegion = null;
                configure?.Invoke(options);
            }));
            return (factory, writer);
        }

        [Fact]
        public void Log_WritesTagCategoryScopesAndMessage() {
            (ILoggerFactory factory, StringTerminalWriter writer) = Factory();
            using (factory) {
                ILogger log = factory.CreateLogger("Shop.Services.Orders");
                using (log.BeginScope("order {Id}", 1042)) {
                    log.LogInformation("Payment accepted");
                    log.LogWarning("Stock is low for {Sku}", "A-17");
                }
                log.LogError("Gateway down");
            }
            string[] lines = writer.ToString().Replace("\r", string.Empty).Split('\n').Where(line => line.Length > 0).ToArray();
            Assert.Equal(3, lines.Length);
            Assert.Matches(@"^\s+Orders \(order 1042\)  Payment accepted$", lines[0]);
            Assert.Matches(@"^WARN Orders \(order 1042\)  Stock is low for A-17$", lines[1]);
            Assert.Matches(@"^ERR\s+Orders  Gateway down$", lines[2]);
            // Messages start in the same column.
            Assert.Equal(lines[0].IndexOf("Orders", StringComparison.Ordinal), lines[1].IndexOf("Orders", StringComparison.Ordinal));
        }

        [Fact]
        public void Log_TimestampCategoryAndMinimumLevelFollowTheOptions() {
            (ILoggerFactory factory, StringTerminalWriter writer) = Factory(options => {
                options.TimestampFormat = "yyyy";
                options.Category = LogCategoryFormat.Full;
                options.MinimumLevel = LogLevel.Information;
                options.IncludeEventId = true;
            });
            using (factory) {
                ILogger log = factory.CreateLogger("Shop.Orders");
                log.LogDebug("hidden");
                log.LogInformation(new EventId(7), "shown");
            }
            string text = writer.ToString();
            Assert.DoesNotContain("hidden", text);
            Assert.Contains(DateTime.Now.Year.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ", text);
            Assert.Contains("Shop.Orders[7]  shown", text);
        }

        [Fact]
        public void Log_AnException_PrintsItThroughTheErrorViewUnderTheEntry() {
            (ILoggerFactory factory, StringTerminalWriter writer) = Factory(options => options.MaxExceptionLines = 1);
            using (factory) {
                ILogger log = factory.CreateLogger("Jobs");
                try {
                    throw new InvalidOperationException("disk full", new TimeoutException("slow"));
                } catch (InvalidOperationException ex) {
                    log.LogCritical(ex, "Backup failed");
                }
            }
            string text = writer.ToString();
            Assert.Contains("critical: Backup failed", text);
            // ConsoleMonster's error view, indented under the entry: the type, the message and the inner exception.
            string[] lines = text.Replace("\r", string.Empty).Split('\n');
            Assert.Contains(lines, line => line.StartsWith("    ", StringComparison.Ordinal) && line.Contains("InvalidOperationException"));
            Assert.Contains(lines, line => line.StartsWith("    ", StringComparison.Ordinal) && line.Contains("disk full"));
            Assert.Contains("TimeoutException", text);
        }

        [Fact]
        public void Log_AnExceptionInARedirectedLog_IsNotWrappedOrShortened() {
            (ILoggerFactory factory, StringTerminalWriter writer) = Factory();
            string message = string.Join(" ", Enumerable.Repeat("the archive folder could not be reached", 5));
            using (factory) {
                factory.CreateLogger("Jobs").LogError(new InvalidOperationException(message), "Archive failed");
            }
            Assert.Contains(message, writer.ToString());
            Assert.DoesNotContain("…", writer.ToString());
        }

        [Fact]
        public void Log_WhileALiveSessionWithALogRuns_WritesIntoTheRegion() {
            StringTerminalWriter screen = new StringTerminalWriter();
            LogRegion region = new LogRegion("log");
            using (LiveSession session = new LiveSession(new Rows(region), new LiveSessionOptions {
                Mode = LiveMode.Inline,
                Profile = TerminalProfile.Fixed(80, 20, ColorDepth.None, GlyphTier.Ascii),
                Output = new InlineOutput(screen, TerminalProfile.Fixed(80, 20, ColorDepth.None, GlyphTier.Ascii), false),
                HandleCancelKey = false,
                PrintFinalState = false,
                CaptureConsole = false
            })) {
                session.Start();
                StringTerminalWriter printed = new StringTerminalWriter();
                using (ILoggerFactory factory = LoggerFactory.Create(logging => logging.AddConsoleMonster(options => {
                    options.Writer = printed;
                    options.Profile = TerminalProfile.Fixed(80, 20, ColorDepth.None, GlyphTier.Ascii, OutputMode.Plain);
                    options.IncludeEventId = true;
                }))) {
                    factory.CreateLogger("Shop.Orders").LogWarning(new EventId(7), "payment slow");
                }
                for (int i = 0; i < 100 && region.Entries.Count == 0; i++) {
                    Thread.Sleep(20);
                }
                Assert.Empty(printed.ToString());
                LogEntry entry = Assert.Single(region.Entries);
                Assert.Equal("warn", entry.Level);
                Assert.Contains("payment slow", entry.RichText != null ? entry.RichText.PlainText : entry.Text);
                Assert.Contains("[7]", entry.RichText != null ? entry.RichText.PlainText : entry.Text);
                session.Stop();
            }
        }

        [Fact]
        public void AddConsoleMonster_RegistersOneProvider() {
            ServiceCollection services = new ServiceCollection();
            services.AddLogging(logging => logging.AddConsoleMonster().AddConsoleMonster());
            using (ServiceProvider provider = services.BuildServiceProvider()) {
                Assert.Single(provider.GetServices<ILoggerProvider>().OfType<ConsoleMonsterLoggerProvider>());
            }
        }

        [Theory]
        [InlineData(LogLevel.Trace, "verbose")]
        [InlineData(LogLevel.Debug, "debug")]
        [InlineData(LogLevel.Information, "info")]
        [InlineData(LogLevel.Warning, "warn")]
        [InlineData(LogLevel.Error, "err")]
        [InlineData(LogLevel.Critical, "err")]
        public void LevelName_MapsToConsoleMonsterLevels(LogLevel level, string expected) {
            Assert.Equal(expected, ConsoleMonsterLoggerProvider.LevelName(level));
        }
    }
}
