using System;
using System.Text;

namespace ConsoleMonster.Output {

    /// <summary>
    /// Chooses the encoding for redirected standard output. A process that captures this one (a parent PowerShell,
    /// cmd's <c>for /f</c>) decodes it with the console code page, so redirected output on Windows follows
    /// <see cref="Console.OutputEncoding"/>. CI logs are read as UTF-8 whatever the code page, and other platforms
    /// use UTF-8.
    /// </summary>
    internal static class StreamEncoding {
        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        /// <summary>The encoding for redirected output from this process.</summary>
        /// <param name="continuousIntegration">Whether the output goes to a CI log.</param>
        /// <returns>The encoding; its preamble is never written.</returns>
        internal static Encoding ForStandardOutput(bool continuousIntegration)
            => Choose(continuousIntegration, Interop.WindowsConsole.IsWindows, CurrentConsoleEncoding());

        /// <summary>The console output code page, or zero when it cannot be read.</summary>
        internal static int CurrentCodePage() => CurrentConsoleEncoding()?.CodePage ?? 0;

        /// <summary>Whether text in this code page can carry any character; zero (unknown) counts as UTF-8.</summary>
        /// <param name="codePage">The code page.</param>
        internal static bool IsUnicode(int codePage)
            => codePage == 0 || codePage == 65001 || codePage == 1200 || codePage == 1201 || codePage == 12000 || codePage == 12001;

        internal static Encoding Choose(bool continuousIntegration, bool windows, Encoding? console) {
            if (continuousIntegration || !windows || console == null || console.CodePage == 65001) {
                return Utf8;
            }
            // Keep the configured encoding (a legacy code page or UTF-16), but never throw on a character it cannot
            // carry: a console encoding may come with an exception fallback.
            try {
                var encoding = (Encoding)console.Clone();
                encoding.EncoderFallback = EncoderFallback.ReplacementFallback;
                return encoding;
            } catch (Exception) {
                return console;
            }
        }

        private static Encoding? CurrentConsoleEncoding() {
            try {
                return Console.OutputEncoding;
            } catch (Exception) {
                // Hosts without a console keep UTF-8.
                return null;
            }
        }
    }
}
