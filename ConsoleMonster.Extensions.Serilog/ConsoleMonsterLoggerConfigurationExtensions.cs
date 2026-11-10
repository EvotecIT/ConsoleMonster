using System;
using ConsoleMonster.Extensions.Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Serilog {
    /// <summary>Adds ConsoleMonster output to a Serilog logger.</summary>
    public static class ConsoleMonsterLoggerConfigurationExtensions {
        /// <summary>Writes structured events through ConsoleMonster, coordinating output with active live views.</summary>
        /// <param name="sinkConfiguration">The logger's WriteTo configuration.</param>
        /// <param name="configure">Changes the rendering options.</param>
        /// <param name="restrictedToMinimumLevel">The minimum sink level, after the logger's own filters.</param>
        /// <returns>The logger configuration for chaining.</returns>
        public static LoggerConfiguration ConsoleMonster(this LoggerSinkConfiguration sinkConfiguration,
            Action<ConsoleMonsterSinkOptions>? configure = null, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose) {
            if (sinkConfiguration == null) { throw new ArgumentNullException(nameof(sinkConfiguration)); }
            ConsoleMonsterSinkOptions options = new ConsoleMonsterSinkOptions();
            configure?.Invoke(options);
            return sinkConfiguration.Sink(new ConsoleMonsterSink(options), restrictedToMinimumLevel);
        }
    }
}
