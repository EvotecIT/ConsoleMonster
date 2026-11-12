using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ConsoleMonster.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Parsing;

namespace ConsoleMonster.Extensions.Serilog {
    /// <summary>Renders Serilog events with the same entry renderer as ConsoleMonster's ILogger provider.</summary>
    public sealed class ConsoleMonsterSink : ILogEventSink {
        private readonly LogEntryRenderer _renderer = new LogEntryRenderer();
        private static readonly JsonValueFormatter Compact = new JsonValueFormatter(typeTagName: null);

        /// <summary>Creates a sink with the supplied options, or the defaults.</summary>
        /// <param name="options">Line rendering and destination options.</param>
        public ConsoleMonsterSink(ConsoleMonsterSinkOptions? options = null) { Options = options ?? new ConsoleMonsterSinkOptions(); }

        /// <summary>The sink's rendering options.</summary>
        public ConsoleMonsterSinkOptions Options { get; }

        /// <summary>Renders a Serilog event with its template values, properties and exception through ConsoleMonster.</summary>
        /// <param name="logEvent">The event to render.</param>
        /// <exception cref="ArgumentNullException">The event is null.</exception>
        public void Emit(LogEvent logEvent) {
            if (logEvent == null) { throw new ArgumentNullException(nameof(logEvent)); }
            List<LogProperty> properties = new List<LogProperty>();
            string category = string.Empty;
            foreach (KeyValuePair<string, LogEventPropertyValue> property in logEvent.Properties) {
                if (property.Key == Constants.SourceContextPropertyName) {
                    category = (property.Value as ScalarValue)?.Value as string ?? string.Empty;
                    if (!logEvent.MessageTemplate.Tokens.OfType<PropertyToken>().Any(token => token.PropertyName == property.Key)) { continue; }
                }
                if (property.Value is ScalarValue scalar) { properties.Add(new LogProperty(property.Key, scalar.Value)); }
                else {
                    LogEventPropertyValue value = property.Value;
                    properties.Add(new LogProperty(property.Key, value, _ => CompactText(value)));
                }
            }
            LogMessage message = LogMessage.Create(logEvent.MessageTemplate.Text, string.Empty, properties);
            _renderer.Write(new LogEntryOptions {
                TimestampFormat = Options.TimestampFormat,
                UseUtcTimestamp = Options.UseUtcTimestamp,
                IncludeCategory = Options.IncludeCategory,
                MaxExceptionLines = Options.MaxExceptionLines,
                LogRegion = Options.LogRegion,
                Profile = Options.Profile,
                Writer = Options.Writer
            }, category, (int)logEvent.Level, 0, message, logEvent.Exception, timestamp: logEvent.Timestamp);
        }

        private static string CompactText(LogEventPropertyValue value) {
            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture)) {
                Compact.Format(value, writer);
                return writer.ToString();
            }
        }
    }
}
