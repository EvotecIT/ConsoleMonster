using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace ConsoleMonster.Extensions.Logging {
    /// <summary>Adds the ConsoleMonster logging provider to a logging builder.</summary>
    public static class ConsoleMonsterLoggingExtensions {
        /// <summary>
        /// Adds the ConsoleMonster provider: entries with ConsoleMonster's level tags and colours, a timestamp, category and
        /// scopes; into a running dashboard's log region when there is one, as lines otherwise (plain when redirected).
        /// The provider is added once: a second call keeps the first call's options.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="configure">Changes the options, or null for the defaults.</param>
        /// <returns>The builder.</returns>
        public static ILoggingBuilder AddConsoleMonster(this ILoggingBuilder builder, Action<ConsoleMonsterLoggerOptions>? configure = null) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            ConsoleMonsterLoggerOptions options = new ConsoleMonsterLoggerOptions();
            configure?.Invoke(options);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider>(new ConsoleMonsterLoggerProvider(options)));
            return builder;
        }

        /// <summary>Adds the ConsoleMonster provider to a logger factory (without dependency injection).</summary>
        /// <param name="factory">The logger factory.</param>
        /// <param name="configure">Changes the options, or null for the defaults.</param>
        /// <returns>The factory.</returns>
        public static ILoggerFactory AddConsoleMonster(this ILoggerFactory factory, Action<ConsoleMonsterLoggerOptions>? configure = null) {
            if (factory == null) {
                throw new ArgumentNullException(nameof(factory));
            }
            ConsoleMonsterLoggerOptions options = new ConsoleMonsterLoggerOptions();
            configure?.Invoke(options);
            factory.AddProvider(new ConsoleMonsterLoggerProvider(options));
            return factory;
        }
    }
}
