using System;

namespace ConsoleMonster.Hosting {
    /// <summary>When the console dashboard is shown.</summary>
    public enum ConsoleDashboardMode {
        /// <summary>On an interactive terminal only: not when the output is redirected, in CI, or as a Windows service or daemon (the default).</summary>
        Auto,

        /// <summary>Whenever the terminal can draw it.</summary>
        Always,

        /// <summary>Never; statuses are still tracked and can be read from <see cref="IConsoleDashboard"/>.</summary>
        Never
    }

    /// <summary>Options for <see cref="ConsoleDashboardServiceCollectionExtensions.AddConsoleDashboard"/>.</summary>
    public sealed class ConsoleDashboardOptions {
        /// <summary>The title in the header, or null for the application name.</summary>
        public string? Title { get; set; }

        /// <summary>When the dashboard is shown (default <see cref="ConsoleDashboardMode.Auto"/>).</summary>
        public ConsoleDashboardMode Mode { get; set; } = ConsoleDashboardMode.Auto;

        /// <summary>Whether the hosted services are listed (default true).</summary>
        public bool ShowServices { get; set; } = true;

        /// <summary>Whether the log region is shown (default true); ConsoleMonster.Extensions.Logging writes into it.</summary>
        public bool ShowLog { get; set; } = true;

        /// <summary>The log region's height in rows (default 8).</summary>
        public int LogHeight { get; set; } = 8;

        /// <summary>Whether the dashboard's final state stays in the scrollback when the host stops (default true).</summary>
        public bool KeepFinalState { get; set; } = true;

        /// <summary>
        /// Gives a hosted service its name in the dashboard, or null for its type name (generic arguments and a
        /// "Service" or "HostedService" suffix left out).
        /// </summary>
        public Func<Type, string>? ServiceName { get; set; }
    }
}
