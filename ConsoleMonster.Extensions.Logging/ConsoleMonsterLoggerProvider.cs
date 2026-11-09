using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using ConsoleMonster.Logging;
using Microsoft.Extensions.Logging;

namespace ConsoleMonster.Extensions.Logging {
    /// <summary>
    /// Writes Microsoft.Extensions.Logging entries the ConsoleMonster way: the level tag and colour of Write-ConsoleLogEntry and
    /// the log region, a timestamp, the category and scopes, then the message (and the exception under it). While a live
    /// session with a log region runs (a dashboard), entries go into that region, so they never print over the view;
    /// otherwise they are printed as lines, plain when the output is redirected. Thread-safe.
    /// </summary>
    public sealed class ConsoleMonsterLoggerProvider : ILoggerProvider, ISupportExternalScope {
        private readonly ConcurrentDictionary<string, ConsoleMonsterLogger> _loggers = new ConcurrentDictionary<string, ConsoleMonsterLogger>(StringComparer.Ordinal);
        private readonly LogEntryRenderer _renderer = new LogEntryRenderer();
        private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

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
        public static string LevelName(LogLevel level) => LogEntryRenderer.LevelName((int)level);

        internal void Write(string category, LogLevel level, EventId eventId, LogMessage message, Exception? exception) {
            _renderer.Write(new LogEntryOptions {
                TimestampFormat = Options.TimestampFormat,
                UseUtcTimestamp = Options.UseUtcTimestamp,
                IncludeCategory = Options.Category != LogCategoryFormat.None,
                FullCategory = Options.Category == LogCategoryFormat.Full,
                MaxExceptionLines = Options.MaxExceptionLines,
                LogRegion = Options.LogRegion,
                Profile = Options.Profile,
                Writer = Options.Writer
            }, category, (int)level, Options.IncludeEventId ? eventId.Id : 0, message, exception,
                Options.IncludeScopes ? ScopeText() : null);
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
            List<LogProperty> properties = new List<LogProperty>();
            string? template = null;
            if (state is IEnumerable<KeyValuePair<string, object?>> structured) {
                foreach (KeyValuePair<string, object?> property in structured) {
                    if (property.Key == "{OriginalFormat}") { template = property.Value as string; }
                    else { properties.Add(new LogProperty(property.Key, property.Value)); }
                }
            }
            _provider.Write(_category, logLevel, eventId,
                LogMessage.Create(template, message, properties, preserveFormattedMessage: true), exception);
        }
    }
}
