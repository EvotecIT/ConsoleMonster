using System;
using System.Globalization;
using System.Threading;

namespace ConsoleMonster {
    public static partial class ContinuousIntegration {
        private static long _section;

        /// <summary>Opens a collapsible provider group, or writes a muted heading in an ordinary terminal. Dispose closes it, including during exception unwinding.</summary>
        /// <param name="title">Literal heading.</param>
        /// <param name="write">Optional host stdout sink, also retained for the closing marker.</param>
        /// <returns>The idempotent group scope. Nest groups and dispose them in reverse order.</returns>
        public static IDisposable Group(string title, Action<string>? write = null) {
            if (title == null) { throw new ArgumentNullException(nameof(title)); }
            ContinuousIntegrationProvider provider = Provider;
            string id = "consolemonster_" + Interlocked.Increment(ref _section).ToString(CultureInfo.InvariantCulture);
            lock (OutputGate) {
                switch (provider) {
                    case ContinuousIntegrationProvider.GitHub: Write("::group::" + GitHubData(title), write); break;
                    case ContinuousIntegrationProvider.Azure: Write("##[group]" + AzureLiteral(SingleLine(title)), write); break;
                    case ContinuousIntegrationProvider.GitLab: Write("\x1b[0Ksection_start:" + Timestamp() + ":" + id + "[collapsed=true]\r\x1b[0K" + SingleLine(title), write); break;
                    default:
                        if (write == null) { Terminal.WriteMarkup("[muted]" + Text.Markup.Escape(title) + "[/]"); }
                        else { Write(SingleLine(title), write); }
                        break;
                }
            }
            return new Scope(() => {
                lock (OutputGate) {
                    switch (provider) {
                        case ContinuousIntegrationProvider.GitHub: Write("::endgroup::", write); break;
                        case ContinuousIntegrationProvider.Azure: Write("##[endgroup]", write); break;
                        case ContinuousIntegrationProvider.GitLab: Write("\x1b[0Ksection_end:" + Timestamp() + ":" + id + "\r\x1b[0K", write); break;
                    }
                }
            });
        }

        private static string Timestamp() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
    }
}
