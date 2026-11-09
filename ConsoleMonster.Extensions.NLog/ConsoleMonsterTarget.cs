using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Logging;
using ConsoleMonster.Output;
using NLog;
using NLog.MessageTemplates;
using NLog.Targets;

namespace ConsoleMonster.Extensions.NLog {
    /// <summary>An NLog target named ConsoleMonster, usable from code or an NLog.config extension registration.</summary>
    [Target("ConsoleMonster")]
    public sealed class ConsoleMonsterTarget : Target {
        private readonly LogEntryRenderer _renderer = new LogEntryRenderer();
        private IValueFormatter _valueFormatter = null!;

        /// <summary>The invariant timestamp format, or null to omit timestamps (default HH:mm:ss).</summary>
        public string? TimestampFormat { get; set; } = "HH:mm:ss";
        /// <summary>Whether event timestamps are converted to UTC.</summary>
        public bool UseUtcTimestamp { get; set; }
        /// <summary>Whether the last part of the logger name is shown (default true).</summary>
        public bool IncludeCategory { get; set; } = true;
        /// <summary>The maximum exception frames; zero shows only types and messages (default 12).</summary>
        public int MaxExceptionLines { get; set; } = 12;
        /// <summary>The live log region key (default $log), or null to always print above the view.</summary>
        public string? LogRegion { get; set; } = "$log";
        /// <summary>The terminal profile, or null for automatic detection. Set this from code.</summary>
        public TerminalProfile? Profile { get; set; }
        /// <summary>An explicit output writer, or null for the terminal. The caller retains ownership.</summary>
        public ITerminalWriter? Writer { get; set; }

        /// <inheritdoc />
        protected override void InitializeTarget() {
            base.InitializeTarget();
            // Resolve services before logging: NLog prohibits service lookup in a log-event callback.
            _valueFormatter = (IValueFormatter)LoggingConfiguration.LogFactory.ServiceRepository.GetService(typeof(IValueFormatter));
        }

        /// <inheritdoc />
        protected override void Write(LogEventInfo logEvent) {
            if (logEvent.Level == LogLevel.Off) { return; }
            List<LogProperty> properties = new List<LogProperty>();
            MessageTemplateParameters parameters = logEvent.MessageTemplateParameters;
            HashSet<string> bound = new HashSet<string>(StringComparer.Ordinal);
            foreach (MessageTemplateParameter parameter in parameters) { bound.Add(parameter.Name); }
            foreach (KeyValuePair<object, object> property in logEvent.Properties) {
                string? name = Convert.ToString(property.Key, CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(name) && !bound.Contains(name!)) { properties.Add(new LogProperty(name!, property.Value)); }
            }
            // NLog binds positional as well as named template arguments, including values not stored as event properties.
            List<LogProperty> arguments = new List<LogProperty>();
            foreach (MessageTemplateParameter parameter in parameters) {
                LogProperty property = parameter.CaptureType == CaptureType.Normal
                    ? new LogProperty(parameter.Name, parameter.Value)
                    : new LogProperty(parameter.Name, parameter.Value,
                        format => FormatCaptured(parameter, format, logEvent.FormatProvider ?? CultureInfo.InvariantCulture));
                arguments.Add(property);
                properties.Add(property);
            }
            LogMessage message = LogMessage.Create(parameters.Count == 0 ? null : logEvent.Message, logEvent.FormattedMessage, properties,
                logEvent.FormatProvider ?? CultureInfo.InvariantCulture, templateArguments: arguments);
            _renderer.Write(new LogEntryOptions {
                TimestampFormat = string.IsNullOrEmpty(TimestampFormat) ? null : TimestampFormat,
                UseUtcTimestamp = UseUtcTimestamp,
                IncludeCategory = IncludeCategory,
                MaxExceptionLines = MaxExceptionLines,
                LogRegion = string.IsNullOrEmpty(LogRegion) ? null : LogRegion,
                Profile = Profile,
                Writer = Writer
            }, logEvent.LoggerName ?? string.Empty, logEvent.Level.Ordinal, 0, message, logEvent.Exception,
                timestamp: new DateTimeOffset(logEvent.TimeStamp));
        }
        private string FormatCaptured(MessageTemplateParameter parameter, string? format, IFormatProvider provider) {
            StringBuilder text = new StringBuilder();
            _valueFormatter.FormatValue(parameter.Value, format ?? parameter.Format, parameter.CaptureType, provider, text);
            return text.ToString();
        }
    }
}
