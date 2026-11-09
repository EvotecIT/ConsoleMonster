using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Extensions.Logging;
using ConsoleMonster.Extensions.NLog;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Sessions;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using Serilog;
using Serilog.Events;
using Xunit;
using MelLevel = Microsoft.Extensions.Logging.LogLevel;

namespace ConsoleMonster.Extensions.Tests {
    [Collection("Logging sessions")]
    public sealed class LoggingAdapterTests {
        private const string Template = "Paid {Count:000} by {Customer} at {When:yyyy-MM-dd}";
        private const string Message = "Paid 042 by A-17 at 2026-10-21";
        private static readonly DateTime When = new DateTime(2026, 10, 21);
        private static TerminalProfile Profile(OutputMode mode) => TerminalProfile.Fixed(140, 40,
            mode == OutputMode.Interactive ? ColorDepth.TrueColor : ColorDepth.None, GlyphTier.Ascii, mode);

        [Theory]
        [InlineData(OutputMode.Plain)]
        [InlineData(OutputMode.Stream)]
        [InlineData(OutputMode.Interactive)]
        public void NativeConfigurations_MatchILoggerForLevelsValuesPropertiesAndExceptions(OutputMode mode) {
            TerminalProfile profile = Profile(mode);
            StringTerminalWriter microsoft = new StringTerminalWriter();
            StringTerminalWriter serilog = new StringTerminalWriter();
            StringTerminalWriter nlog = new StringTerminalWriter();
            Exception failure = new InvalidOperationException("disk full", new TimeoutException("retry later"));
            using (ConsoleMonsterLoggerProvider provider = new ConsoleMonsterLoggerProvider(new ConsoleMonsterLoggerOptions {
                Writer = microsoft, Profile = profile, TimestampFormat = null, LogRegion = null
            }))
            using (global::Serilog.Core.Logger logger = new LoggerConfiguration().MinimumLevel.Verbose()
                .WriteTo.ConsoleMonster(options => {
                    options.Writer = serilog; options.Profile = profile; options.TimestampFormat = null; options.LogRegion = null;
                }).CreateLogger())
            using (LogFactory factory = NLogFactory(nlog, profile)) {
                Microsoft.Extensions.Logging.ILogger mel = provider.CreateLogger("Shop.Services.Orders");
                global::Serilog.ILogger sl = logger.ForContext("SourceContext", "Shop.Services.Orders").ForContext("Host", "node-1");
                global::NLog.Logger nl = factory.GetLogger("Shop.Services.Orders");
                for (int level = 0; level < 6; level++) {
                    Exception? exception = level == 5 ? failure : null;
                    List<KeyValuePair<string, object?>> state = new List<KeyValuePair<string, object?>> {
                        new KeyValuePair<string, object?>("Count", 42),
                        new KeyValuePair<string, object?>("Customer", "A-17"),
                        new KeyValuePair<string, object?>("When", When),
                        new KeyValuePair<string, object?>("Host", "node-1"),
                        new KeyValuePair<string, object?>("{OriginalFormat}", Template)
                    };
                    mel.Log((MelLevel)level, default, state, exception, (_, __) => Message);
                    sl.Write((LogEventLevel)level, exception, Template, 42, "A-17", When);
                    LogEventInfo entry = new LogEventInfo(global::NLog.LogLevel.FromOrdinal(level), nl.Name, CultureInfo.InvariantCulture,
                        Template, new object[] { 42, "A-17", When }, exception);
                    entry.Properties["Host"] = "node-1";
                    nl.Log(entry);
                }
            }
            string expected = microsoft.ToString();
            Assert.Equal(expected, serilog.ToString());
            Assert.Equal(expected, nlog.ToString());
            VtScreen screen = new VtScreen(140, 40);
            screen.Feed(expected.Replace("\r\n", "\n").Replace("\n", "\r\n"));
            string[] rows = Enumerable.Range(0, 40).Select(y => Row(screen.Snapshot(), y)).ToArray();
            Assert.Equal("VERB Orders  " + Message + " Host=node-1", rows[0]);
            Assert.Equal("DBG  Orders  " + Message + " Host=node-1", rows[1]);
            Assert.Equal("     Orders  " + Message + " Host=node-1", rows[2]);
            Assert.Equal("WARN Orders  " + Message + " Host=node-1", rows[3]);
            Assert.Equal("ERR  Orders  " + Message + " Host=node-1", rows[4]);
            Assert.Equal("ERR  Orders  critical: " + Message + " Host=node-1", rows[5]);
            Assert.Contains(rows, row => row.StartsWith("    ", StringComparison.Ordinal) && row.Contains("InvalidOperationException"));
            Assert.Contains(rows, row => row.Contains("TimeoutException"));
            Assert.Contains(rows, row => row.Contains("retry later"));
            if (mode == OutputMode.Interactive) {
                var cells = screen.Snapshot();
                var context = Terminal.CreateContext(profile);
                Assert.Equal(context.Style(ThemeToken.Highlight).Color, cells[rows[3].IndexOf("042", StringComparison.Ordinal), 3].Style.Color);
                Assert.Equal(context.Style(ThemeToken.Pass).Color, cells[rows[3].IndexOf("A-17", StringComparison.Ordinal), 3].Style.Color);
                Assert.Equal(context.Style(ThemeToken.Accent2).Color, cells[rows[3].IndexOf("2026-10-21", StringComparison.Ordinal), 3].Style.Color);
                Assert.Equal(context.Style(ThemeToken.Muted).Color, cells[rows[3].IndexOf("Host=", StringComparison.Ordinal), 3].Style.Color);
            } else {
                Assert.DoesNotContain("\u001b", expected, StringComparison.Ordinal);
                Assert.DoesNotContain("\r", expected.Replace("\r\n", ""), StringComparison.Ordinal);
            }
        }

        [Theory]
        [InlineData(true, "     Orders  From Shop.Orders")]
        [InlineData(false, "     From Shop.Orders")]
        public void Serilog_SourceContextRemainsAvailableToTheMessageTemplate(bool category, string expected) {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (var logger = new LoggerConfiguration().WriteTo.ConsoleMonster(options => {
                options.Writer = writer; options.Profile = Profile(OutputMode.Plain); options.TimestampFormat = null;
                options.IncludeCategory = category;
            }).CreateLogger()) {
                logger.ForContext("SourceContext", "Shop.Orders").Information("From {SourceContext}");
            }
            Assert.Equal(expected + Environment.NewLine, writer.ToString());
        }

        [Fact]
        public void NLog_DestructuringSerializesFieldsAndStringifyUsesTheNativeCapture() {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (LogFactory factory = NLogFactory(writer, Profile(OutputMode.Plain))) {
                var log = factory.GetLogger("Orders");
                var payload = new Payload { Id = 42, Customer = "Ada" };
                log.Info("Payload {@Payload}", payload);
                log.Info("Opaque {$Payload}", payload);
            }
            string text = writer.ToString();
            Assert.Contains("Payload {", text);
            Assert.Contains("\"Id\":42", text);
            Assert.Contains("\"Customer\":\"Ada\"", text);
            Assert.Contains("Opaque \"opaque\"", text);
            Assert.Equal(2, text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Length);
        }

        private sealed class Payload {
            public int Id { get; set; }
            public string Customer { get; set; } = string.Empty;
            public override string ToString() => "opaque";
        }

        [Fact]
        public void Serilog_DestructuredObjectsStayCompactInMessagesAndUnusedProperties() {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (global::Serilog.Core.Logger logger = new LoggerConfiguration().WriteTo.ConsoleMonster(options => {
                options.Writer = writer; options.Profile = Profile(OutputMode.Plain); options.TimestampFormat = null;
            }).CreateLogger()) {
                logger.ForContext("Extra", new { Host = "node-1", Online = true }, destructureObjects: true)
                    .Information("Order {@Order}", new { Id = 42, Customer = "A-17" });
            }
            Assert.Equal("     Order {\"Id\":42,\"Customer\":\"A-17\"} Extra={\"Host\":\"node-1\",\"Online\":true}" + Environment.NewLine, writer.ToString());
        }

        [Fact]
        public void NLogConfig_LoadsTheRegisteredTargetAndAppliesItsAttributes() {
            string path = Path.Combine(Path.GetTempPath(), "consolemonster-nlog-" + Guid.NewGuid().ToString("N") + ".config");
            try {
                File.WriteAllText(path, """
                    <nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
                          xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" throwConfigExceptions="true">
                      <extensions><add assembly="ConsoleMonster.Extensions.NLog" /></extensions>
                      <targets><target xsi:type="ConsoleMonster" name="readable" timestampFormat="yyyy"
                          useUtcTimestamp="true" includeCategory="false" maxExceptionLines="0" logRegion="" /></targets>
                      <rules><logger name="*" minlevel="Trace" writeTo="readable" /></rules>
                    </nlog>
                    """);
                using (LogFactory factory = new LogFactory()) {
                    factory.Configuration = new XmlLoggingConfiguration(path, factory);
                    ConsoleMonsterTarget target = Assert.IsType<ConsoleMonsterTarget>(factory.Configuration.FindTargetByName("readable"));
                    Assert.True(target.UseUtcTimestamp);
                    Assert.False(target.IncludeCategory);
                    Assert.Equal(0, target.MaxExceptionLines);
                    Assert.Equal(string.Empty, target.LogRegion);
                    StringTerminalWriter writer = new StringTerminalWriter();
                    target.Writer = writer; target.Profile = Profile(OutputMode.Plain);
                    LogEventInfo entry = new LogEventInfo(global::NLog.LogLevel.Trace, "Hidden.Category", "loaded {Count}") {
                        Parameters = new object[] { 42 }, TimeStamp = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc)
                    };
                    factory.GetLogger("Hidden.Category").Log(entry);
                    Assert.Equal("2026 VERB loaded 42" + Environment.NewLine, writer.ToString());
                }
            } finally { File.Delete(path); }
        }

        [Fact]
        public void NLog_KeepsNativeLiteralAndRepeatedArgumentBinding() {
            StringTerminalWriter writer = new StringTerminalWriter();
            using (LogFactory factory = NLogFactory(writer, Profile(OutputMode.Plain))) {
                var logger = factory.GetLogger("Orders").WithProperty("Count", 42);
                logger.Info("literal {{cart}} {Count}");
                logger.Info("repeated {Count} and {Count}", 1, 2);
                logger.Info("positional {{cart}} {1:000}/{0:000}", 3, 4);
            }
            Assert.Equal("     Orders  literal {{cart}} {Count} Count=42" + Environment.NewLine
                + "     Orders  repeated 1 and 2" + Environment.NewLine
                + "     Orders  positional {cart} 004/003 Count=42" + Environment.NewLine, writer.ToString());
        }

        [Theory]
        [InlineData(false, "14:00 +02:00")]
        [InlineData(true, "12:00 +00:00")]
        public void Serilog_UsesTheEventTimestampAndConvertsItsOffsetOnlyWhenRequested(bool utc, string expected) {
            StringTerminalWriter writer = new StringTerminalWriter();
            var sink = new ConsoleMonster.Extensions.Serilog.ConsoleMonsterSink(new ConsoleMonster.Extensions.Serilog.ConsoleMonsterSinkOptions {
                Writer = writer, Profile = Profile(OutputMode.Plain), TimestampFormat = "HH:mm zzz", UseUtcTimestamp = utc
            });
            sink.Emit(new LogEvent(new DateTimeOffset(2026, 10, 21, 14, 0, 0, TimeSpan.FromHours(2)), LogEventLevel.Warning,
                null, new global::Serilog.Parsing.MessageTemplateParser().Parse("delayed event"), Array.Empty<LogEventProperty>()));
            Assert.Equal(expected + " WARN delayed event" + Environment.NewLine, writer.ToString());
        }

        [Theory]
        [InlineData("Microsoft")]
        [InlineData("Serilog")]
        [InlineData("NLog")]
        public void LogDuringLiveProgress_IsPrintedAboveTheRegion(string adapter) {
            TerminalProfile profile = TerminalProfile.Fixed(80, 16, ColorDepth.None, GlyphTier.Ascii);
            StringTerminalWriter writer = new StringTerminalWriter();
            ProgressBar progress = new ProgressBar("Build", "build") { Total = 4 };
            using (Terminal.Use(ConsoleTarget.Instance))
            using (LiveSession session = new LiveSession(progress, new LiveSessionOptions {
                Mode = LiveMode.Inline, Profile = profile, Output = new InlineOutput(writer, profile, false),
                HandleCancelKey = false, CaptureConsole = true, PrintFinalState = true,
                WindowSource = () => new[] { 0, 0, 80, 16 }
            })) {
                session.Start();
                // A borrowed output does not claim the physical console; bind it as the active console for this fixture.
                Terminal.ClaimConsole(session);
                if (adapter == "Microsoft") {
                    using (ConsoleMonsterLoggerProvider provider = new ConsoleMonsterLoggerProvider(new ConsoleMonsterLoggerOptions {
                        Profile = profile, TimestampFormat = null, LogRegion = null
                    })) { provider.CreateLogger("Build").LogWarning("Compiling {Count} files", 42); }
                } else if (adapter == "Serilog") {
                    using (global::Serilog.Core.Logger logger = new LoggerConfiguration().WriteTo.ConsoleMonster(options => {
                        options.Profile = profile; options.TimestampFormat = null; options.LogRegion = null;
                    }).CreateLogger()) { logger.ForContext("SourceContext", "Build").Warning("Compiling {Count} files", 42); }
                } else {
                    using (LogFactory factory = NLogFactory(null, profile)) { factory.GetLogger("Build").Warn("Compiling {Count} files", 42); }
                }
                session.Post(new LiveEvent("build", "advance", value: 4));
                session.Stop();
            }
            VtScreen screen = new VtScreen(80, 16);
            screen.Feed(writer.ToString());
            string[] rows = Enumerable.Range(0, 16).Select(y => Row(screen.Snapshot(), y)).ToArray();
            Assert.Equal("WARN Build  Compiling 42 files", rows[0]);
            Assert.StartsWith("Build", rows[1]);
            Assert.Contains("100%", rows[1]);
            Assert.Equal(1, rows.Count(row => row.Contains("Compiling")));
        }

        [Theory]
        [InlineData("Microsoft")]
        [InlineData("Serilog")]
        [InlineData("NLog")]
        public void NativeAdapters_PostTypedLiteralValuesAndMutedPropertiesToTheLiveLog(string adapter) {
            TerminalProfile profile = Profile(OutputMode.Stream).WithColorDepth(ColorDepth.TrueColor);
            StringTerminalWriter printed = new StringTerminalWriter();
            StringTerminalWriter screen = new StringTerminalWriter();
            LogRegion region = new LogRegion("log");
            using (LiveSession session = new LiveSession(region, new LiveSessionOptions {
                Output = new LineOutput(screen, profile, false), Profile = profile, CaptureConsole = false,
                HandleCancelKey = false, PrintFinalState = false
            })) {
                session.Start();
                if (adapter == "Microsoft") {
                    using var provider = new ConsoleMonsterLoggerProvider(new ConsoleMonsterLoggerOptions {
                        Writer = printed, Profile = profile, TimestampFormat = null
                    });
                    provider.CreateLogger("Shop.Orders").Log(MelLevel.Warning, default, new[] {
                        new KeyValuePair<string, object?>("Host", "node-1"),
                        new KeyValuePair<string, object?>("Count", 42),
                        new KeyValuePair<string, object?>("Customer", "A[fail]da[/]"),
                        new KeyValuePair<string, object?>("{OriginalFormat}", "Paid {Count} by {Customer}")
                    }, null, (_, __) => "Paid 42 by A[fail]da[/]");
                } else if (adapter == "Serilog") {
                    using var logger = new LoggerConfiguration().WriteTo.ConsoleMonster(options => {
                        options.Writer = printed; options.Profile = profile; options.TimestampFormat = null;
                    }).CreateLogger();
                    logger.ForContext("SourceContext", "Shop.Orders").ForContext("Host", "node-1")
                        .Warning("Paid {Count} by {Customer}", 42, "A[fail]da[/]");
                } else {
                    using var factory = NLogFactory(printed, profile, "$log");
                    factory.GetLogger("Shop.Orders").WithProperty("Host", "node-1")
                        .Warn("Paid {Count} by {Customer}", 42, "A[fail]da[/]");
                }
                session.Stop();
            }
            Assert.Empty(printed.ToString());
            LogEntry entry = Assert.Single(region.Entries);
            Assert.Equal("warn", entry.Level);
            RichText text = Assert.IsType<RichText>(entry.RichText);
            Assert.Equal("Orders Paid 42 by A[fail]da[/] Host=node-1", text.PlainText);
            var context = Terminal.CreateContext(profile);
            StyledLine line = Assert.Single(text.ToStyledText(context).Lines);
            Assert.Contains(line, span => span.Text == "42" && span.Style.Color.Equals(context.Style(ThemeToken.Highlight).Color));
            Assert.Contains(line, span => span.Text == "A[fail]da[/]" && span.Style.Color.Equals(context.Style(ThemeToken.Pass).Color));
            Assert.Contains(line, span => span.Text.Contains("Host=node-1") && span.Style.Color.Equals(context.Style(ThemeToken.Muted).Color));
        }

        private static LogFactory NLogFactory(ITerminalWriter? writer, TerminalProfile profile, string? region = null) {
            LogFactory factory = new LogFactory();
            LoggingConfiguration configuration = new LoggingConfiguration();
            configuration.AddRule(global::NLog.LogLevel.Trace, global::NLog.LogLevel.Fatal, new ConsoleMonsterTarget {
                Name = "readable", Writer = writer, Profile = profile, TimestampFormat = null, LogRegion = region
            });
            factory.Configuration = configuration;
            return factory;
        }

        private static string Row(ConsoleMonster.Canvas.CellBuffer buffer, int y) => string.Concat(
            Enumerable.Range(0, buffer.Width).Select(x => buffer[x, y].Glyph)).TrimEnd();
    }
}
