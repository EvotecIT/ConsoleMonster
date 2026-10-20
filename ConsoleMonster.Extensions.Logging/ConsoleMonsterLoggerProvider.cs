using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Microsoft.Extensions.Logging;

namespace ConsoleMonster.Extensions.Logging {
    /// <summary>
    /// Writes Microsoft.Extensions.Logging entries the ConsoleMonster way: the level tag and colour of Write-ConsoleLog and
    /// the log region, a timestamp, the category and scopes, then the message (and the exception under it). While a live
    /// session with a log region runs (a dashboard), entries go into that region, so they never print over the view;
    /// otherwise they are printed as lines, plain when the output is redirected. Thread-safe.
    /// </summary>
    public sealed class ConsoleMonsterLoggerProvider : ILoggerProvider, ISupportExternalScope {
        private readonly ConcurrentDictionary<string, ConsoleMonsterLogger> _loggers = new ConcurrentDictionary<string, ConsoleMonsterLogger>(StringComparer.Ordinal);
        private readonly object _gate = new object();
        private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();
        private int _tagWidth;

        /// <summary>Creates the provider.</summary>
        /// <param name="options">Options, or null for the defaults.</param>
        public ConsoleMonsterLoggerProvider(ConsoleMonsterLoggerOptions? options = null) {
            Options = options ?? new ConsoleMonsterLoggerOptions();
        }

        /// <summary>The options.</summary>
        public ConsoleMonsterLoggerOptions Options { get; }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName ?? string.Empty, name => new ConsoleMonsterLogger(name, this));

        /// <inheritdoc />
        public void SetScopeProvider(IExternalScopeProvider scopeProvider) {
            _scopes = scopeProvider ?? new LoggerExternalScopeProvider();
        }

        internal IExternalScopeProvider Scopes => _scopes;

        /// <inheritdoc />
        public void Dispose() {
            _loggers.Clear();
        }

        /// <summary>The ConsoleMonster level (info, warn, err, verbose, debug) for a logging level.</summary>
        /// <param name="level">The logging level.</param>
        /// <returns>The level name.</returns>
        public static string LevelName(LogLevel level) {
            switch (level) {
                case LogLevel.Trace:
                    return "verbose";
                case LogLevel.Debug:
                    return "debug";
                case LogLevel.Warning:
                    return "warn";
                case LogLevel.Error:
                case LogLevel.Critical:
                    return "err";
                default:
                    return "info";
            }
        }

        internal void Write(string category, LogLevel level, EventId eventId, string message, Exception? exception) {
            string levelName = LevelName(level);
            string? scopes = Options.IncludeScopes ? ScopeText() : null;
            string? shortCategory = CategoryText(category);
            if (level == LogLevel.Critical) {
                message = "critical: " + message;
            }
            // An entry with an exception prints above a live view (with its details kept in the scrollback) instead of
            // going into the log region, which keeps only its newest lines.
            if (Options.LogRegion != null && exception == null && TryPost(levelName, shortCategory, scopes, eventId, message)) {
                return;
            }
            TerminalProfile profile = Options.Profile ?? Terminal.GetCurrentProfile();
            RenderContext context = Terminal.CreateContext(profile);
            List<StyledLine> lines = new List<StyledLine>();
            Style muted = context.Style(ThemeToken.Muted);
            List<TextSpan> spans = new List<TextSpan>();
            if (Options.TimestampFormat != null) {
                DateTimeOffset now = Options.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
                spans.Add(new TextSpan(now.ToString(Options.TimestampFormat, CultureInfo.InvariantCulture) + " ", muted));
            }
            // Status markers only on an interactive terminal; logs that are redirected or collected keep the word alone.
            RenderContext? tagContext = profile.Mode == OutputMode.Interactive ? context : null;
            string tag = LogTags.Tag(levelName, tagContext);
            lock (_gate) {
                // Messages start in one column: the tag column only grows.
                _tagWidth = Math.Max(_tagWidth, Math.Max(DisplayWidth.Measure(tag), DisplayWidth.Measure(LogTags.Tag("warn", tagContext))));
            }
            spans.Add(new TextSpan(tag + new string(' ', Math.Max(0, _tagWidth - DisplayWidth.Measure(tag))) + " ", LogTags.Style(levelName, context)));
            StringBuilder origin = new StringBuilder();
            if (shortCategory != null) {
                origin.Append(shortCategory);
            }
            if (Options.IncludeEventId && eventId.Id != 0) {
                origin.Append('[').Append(eventId.Id.ToString(CultureInfo.InvariantCulture)).Append(']');
            }
            if (!string.IsNullOrEmpty(scopes)) {
                origin.Append(origin.Length > 0 ? " " : string.Empty).Append('(').Append(scopes).Append(')');
            }
            if (origin.Length > 0) {
                spans.Add(new TextSpan(origin.ToString() + "  ", muted));
            }
            Style text = LogTags.IsQuiet(levelName) ? muted : context.Style(ThemeToken.Text);
            string[] messageLines = message.Replace("\r\n", "\n").Split('\n');
            spans.Add(new TextSpan(messageLines[0], text));
            lines.Add(new StyledLine(spans));
            for (int i = 1; i < messageLines.Length; i++) {
                lines.Add(StyledLine.From("    " + messageLines[i], text));
            }
            if (exception != null) {
                AddException(lines, exception, context);
            }
            lock (_gate) {
                Terminal.WriteLines(lines, true, profile, Options.Writer);
            }
        }

        // Into a running session's log region: the region adds its own time and level column.
        private bool TryPost(string level, string? category, string? scopes, EventId eventId, string message) {
            LiveSession? session = LiveSession.Current;
            if (session == null || !session.TryGetTarget(Options.LogRegion!, out ILiveTarget? target) || target == null) {
                return false;
            }
            StringBuilder markup = new StringBuilder();
            if (category != null || !string.IsNullOrEmpty(scopes) || (Options.IncludeEventId && eventId.Id != 0)) {
                markup.Append("[muted]");
                markup.Append(Markup.Escape(category ?? string.Empty));
                if (Options.IncludeEventId && eventId.Id != 0) {
                    markup.Append('[').Append(eventId.Id.ToString(CultureInfo.InvariantCulture)).Append(']');
                }
                if (!string.IsNullOrEmpty(scopes)) {
                    markup.Append(category != null ? " " : string.Empty).Append('(').Append(Markup.Escape(scopes!)).Append(')');
                }
                markup.Append("[/] ");
            }
            markup.Append(Markup.Escape(message.Replace("\r\n", " ").Replace('\n', ' ')));
            session.Post(new LiveEvent(Options.LogRegion!, "write", text: markup.ToString(), state: level, isMarkup: true));
            return true;
        }

        private void AddException(List<StyledLine> lines, Exception exception, RenderContext context) {
            Style fail = context.Style(ThemeToken.Fail);
            Style muted = context.Style(ThemeToken.Muted);
            lines.Add(new StyledLine(new[] {
                new TextSpan("    " + exception.GetType().FullName + ": ", fail),
                new TextSpan(exception.Message.Replace("\r\n", " ").Replace('\n', ' '), context.Style(ThemeToken.Text))
            }));
            if (Options.MaxExceptionLines <= 0) {
                return;
            }
            string[] details = exception.ToString().Replace("\r\n", "\n").Split('\n');
            int shown = 0;
            // The first line repeats the type and message.
            for (int i = 1; i < details.Length && shown < Options.MaxExceptionLines; i++) {
                if (details[i].Trim().Length == 0) {
                    continue;
                }
                lines.Add(StyledLine.From("      " + details[i].Trim(), muted));
                shown++;
            }
            int rest = 0;
            for (int i = 1; i < details.Length; i++) {
                if (details[i].Trim().Length > 0) {
                    rest++;
                }
            }
            if (rest > shown) {
                lines.Add(StyledLine.From("      +" + (rest - shown).ToString(CultureInfo.InvariantCulture) + " more", muted));
            }
        }

        private string? CategoryText(string category) {
            if (string.IsNullOrEmpty(category) || Options.Category == LogCategoryFormat.None) {
                return null;
            }
            if (Options.Category == LogCategoryFormat.Full) {
                return category;
            }
            // Generic arguments (Logger<Shop.Orders<Item>>) keep their dots inside the brackets.
            int angle = category.IndexOf('<');
            string head = angle >= 0 ? category.Substring(0, angle) : category;
            int dot = head.LastIndexOf('.');
            return dot >= 0 ? category.Substring(dot + 1) : category;
        }

        private string? ScopeText() {
            StringBuilder sb = new StringBuilder();
            _scopes.ForEachScope((scope, builder) => {
                string? text = scope?.ToString();
                if (string.IsNullOrEmpty(text)) {
                    return;
                }
                if (builder.Length > 0) {
                    builder.Append(" > ");
                }
                builder.Append(text);
            }, sb);
            return sb.Length == 0 ? null : sb.ToString();
        }
    }

    internal sealed class ConsoleMonsterLogger : ILogger {
        private readonly string _category;
        private readonly ConsoleMonsterLoggerProvider _provider;

        internal ConsoleMonsterLogger(string category, ConsoleMonsterLoggerProvider provider) {
            _category = category;
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _provider.Scopes.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= _provider.Options.MinimumLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            if (!IsEnabled(logLevel) || formatter == null) {
                return;
            }
            string message = formatter(state, exception) ?? string.Empty;
            if (message.Length == 0 && exception == null) {
                return;
            }
            _provider.Write(_category, logLevel, eventId, message, exception);
        }
    }
}
