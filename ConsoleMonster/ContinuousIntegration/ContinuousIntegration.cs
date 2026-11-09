using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using ConsoleMonster.Widgets;

namespace ConsoleMonster {
    /// <summary>A log-command protocol; None keeps ordinary terminal output.</summary>
    public enum ContinuousIntegrationProvider {
        /// <summary>No provider commands or summary files.</summary>
        None,
        /// <summary>GitHub Actions workflow commands.</summary>
        GitHub,
        /// <summary>Azure Pipelines logging commands.</summary>
        Azure,
        /// <summary>GitLab CI collapsible sections.</summary>
        GitLab
    }

    /// <summary>The severity of a CI annotation or terminal status line.</summary>
    public enum AnnotationLevel {
        /// <summary>An error; this does not set the process exit code.</summary>
        Error,
        /// <summary>A warning.</summary>
        Warning,
        /// <summary>Information; Azure and GitLab use an ordinary line.</summary>
        Notice
    }

    /// <summary>Provider-native groups, annotations and job summaries with ordinary terminal fallbacks.</summary>
    public static partial class ContinuousIntegration {
        private static readonly AsyncLocal<ContinuousIntegrationProvider?> Override = new AsyncLocal<ContinuousIntegrationProvider?>();
        internal static readonly object OutputGate = new object();

        /// <summary>The scoped override, CONSOLEMONSTER_CI, or the provider's own environment variable, in that order.</summary>
        public static ContinuousIntegrationProvider Provider {
            get {
                if (Override.Value.HasValue) { return Override.Value.Value; }
                string? configured = Environment.GetEnvironmentVariable("CONSOLEMONSTER_CI");
                if (!string.IsNullOrWhiteSpace(configured)) {
                    switch (configured!.Trim().ToLowerInvariant()) {
                        case "github": return ContinuousIntegrationProvider.GitHub;
                        case "azure": return ContinuousIntegrationProvider.Azure;
                        case "gitlab": return ContinuousIntegrationProvider.GitLab;
                        // Unknown overrides fail closed rather than enabling a different protocol.
                        default: return ContinuousIntegrationProvider.None;
                    }
                }
                if (Enabled("GITHUB_ACTIONS")) { return ContinuousIntegrationProvider.GitHub; }
                if (Enabled("TF_BUILD")) { return ContinuousIntegrationProvider.Azure; }
                return Enabled("GITLAB_CI") ? ContinuousIntegrationProvider.GitLab : ContinuousIntegrationProvider.None;
            }
        }

        /// <summary>Overrides detection for this async flow until disposed; None explicitly disables provider output.</summary>
        /// <param name="provider">The protocol to use.</param>
        /// <returns>A scope that restores the preceding choice.</returns>
        public static IDisposable UseProvider(ContinuousIntegrationProvider provider) {
            if (!Enum.IsDefined(typeof(ContinuousIntegrationProvider), provider)) { throw new ArgumentOutOfRangeException(nameof(provider)); }
            var previous = Override.Value;
            Override.Value = provider;
            return new Scope(() => Override.Value = previous);
        }

        /// <summary>Writes an annotation, or a styled status line when the provider has no annotation API.</summary>
        /// <param name="level">Error, Warning or Notice.</param>
        /// <param name="message">Literal message, escaped for the selected protocol.</param>
        /// <param name="title">Optional annotation title (prepended to the message outside GitHub).</param>
        /// <param name="file">Optional source file.</param>
        /// <param name="line">One-based source line, or null.</param>
        /// <param name="column">One-based source column, or null.</param>
        /// <param name="write">Optional host stdout sink; receives complete unstyled command lines.</param>
        public static void Annotate(AnnotationLevel level, string message, string? title = null, string? file = null, int? line = null, int? column = null, Action<string>? write = null) {
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (!Enum.IsDefined(typeof(AnnotationLevel), level)) { throw new ArgumentOutOfRangeException(nameof(level)); }
            if (line < 1) { throw new ArgumentOutOfRangeException(nameof(line)); }
            if (column < 1) { throw new ArgumentOutOfRangeException(nameof(column)); }
            ContinuousIntegrationProvider provider = Provider;
            lock (OutputGate) {
                if (provider == ContinuousIntegrationProvider.GitHub) {
                    var properties = new List<string>();
                    Property(properties, "file", file, GitHubProperty);
                    Property(properties, "line", line?.ToString(CultureInfo.InvariantCulture), GitHubProperty);
                    Property(properties, "col", column?.ToString(CultureInfo.InvariantCulture), GitHubProperty);
                    Property(properties, "title", title, GitHubProperty);
                    Write("::" + level.ToString().ToLowerInvariant() + (properties.Count == 0 ? "" : " " + string.Join(",", properties)) + "::" + GitHubData(message), write);
                } else if (provider == ContinuousIntegrationProvider.Azure && level != AnnotationLevel.Notice) {
                    var properties = new List<string> { "type=" + level.ToString().ToLowerInvariant() };
                    Property(properties, "sourcepath", string.IsNullOrEmpty(file) ? null : System.IO.Path.GetFullPath(file), AzureEscape);
                    Property(properties, "linenumber", line?.ToString(CultureInfo.InvariantCulture), AzureEscape);
                    Property(properties, "columnnumber", column?.ToString(CultureInfo.InvariantCulture), AzureEscape);
                    Write("##vso[task.logissue " + string.Join(";", properties) + "]" + AzureEscape(Message(title, message)), write);
                } else {
                    string location = string.IsNullOrEmpty(file) ? "" : file + (line.HasValue ? ":" + line.Value.ToString(CultureInfo.InvariantCulture) : "");
                    string text = Message(title, message) + (location.Length == 0 ? "" : " (" + location + ")");
                    if ((provider == ContinuousIntegrationProvider.None || provider == ContinuousIntegrationProvider.GitLab) && write == null) {
                        Terminal.WriteMarkup(AnnotationMarkup(level, message, title, file, line));
                    } else { WriteLog(level + ": " + SingleLine(text), write); }
                }
            }
        }

        private static bool Enabled(string variable) => string.Equals(Environment.GetEnvironmentVariable(variable), "true", StringComparison.OrdinalIgnoreCase);
        internal static string AnnotationMarkup(AnnotationLevel level, string message, string? title, string? file, int? line) {
            string markup = Text.Markup.Escape(level + ": " + Message(title, message));
            if (!string.IsNullOrEmpty(file)) {
                string location = Text.Markup.Escape(file!) + (line.HasValue ? ":" + line.Value.ToString(CultureInfo.InvariantCulture) : "");
                string? link = TestRuns.TestRunText.Link(new TestRuns.TestRecord("", "", Styling.StatusKind.Fail) { File = file });
                markup += " (" + (link == null ? location : "[link=" + link + "]" + location + "[/]") + ")";
            }
            return "[" + (level == AnnotationLevel.Error ? "fail" : level == AnnotationLevel.Warning ? "warn" : "muted") + "]" + markup + "[/]";
        }
        private static string Message(string? title, string message) => string.IsNullOrEmpty(title) ? message : title + ": " + message;
        private static void Property(List<string> properties, string key, string? value, Func<string, string> escape) {
            if (!string.IsNullOrEmpty(value)) { properties.Add(key + "=" + escape(value!)); }
        }
        internal static string GitHubData(string value) => value.Replace("%", "%25").Replace("\r", "%0D").Replace("\n", "%0A");
        private static string GitHubProperty(string value) => GitHubData(value).Replace(":", "%3A").Replace(",", "%2C");
        internal static string AzureEscape(string value) => value.Replace("%", "%AZP25").Replace(";", "%3B").Replace("\r", "%0D").Replace("\n", "%0A").Replace("]", "%5D");
        // Azure scans for agent commands anywhere in stdout, including inside a group title or plain notice.
        internal static string AzureLiteral(string value) => value.Replace("##vso[", "##vso%5B").Replace("##[", "##%5B");
        internal static void WriteLog(string line, Action<string>? write = null) {
            ContinuousIntegrationProvider provider = Provider;
            if (provider == ContinuousIntegrationProvider.None && write != null) { write(line); }
            else { Write(provider == ContinuousIntegrationProvider.Azure ? AzureLiteral(line) : line, write); }
        }
        private static string SingleLine(string value) => value.Replace('\r', ' ').Replace('\n', ' ').Replace('\x1b', ' ');
        internal static void Write(string line, Action<string>? write) {
            // A live session's console capture must not turn workflow commands into ordinary log text.
            using (Live.LiveSession.SuspendCapture()) {
                if (write != null) { write(line); }
                else { Console.Out.WriteLine(line); Console.Out.Flush(); }
            }
        }

        private sealed class Scope : IDisposable {
            private Action? _end;
            internal Scope(Action end) { _end = end; }
            public void Dispose() => Interlocked.Exchange(ref _end, null)?.Invoke();
        }
    }
}
