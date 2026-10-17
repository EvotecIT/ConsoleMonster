using System;
using System.IO;
using System.Text;

namespace ConsoleMonster.Testing {
    /// <summary>
    /// Snapshot ("golden file") comparison for console output. The first run writes the file and fails so it gets reviewed;
    /// later runs compare and fail with a readable diff (the first differing line with context, both versions, and the
    /// column where they part). Set <c>CONSOLEMONSTER_UPDATE_GOLDEN=1</c> to rewrite the files after an intended change.
    /// Line endings are normalised to <c>\n</c>; files are UTF-8 without a byte order mark.
    /// </summary>
    public static class GoldenFile {
        /// <summary>The environment variable that rewrites golden files instead of comparing.</summary>
        public const string UpdateVariable = "CONSOLEMONSTER_UPDATE_GOLDEN";

        /// <summary>Whether golden files are being rewritten in this process.</summary>
        public static bool Updating => Environment.GetEnvironmentVariable(UpdateVariable) == "1";

        /// <summary>Compares text with a golden file; throws <see cref="GoldenFileMismatchException"/> when they differ.</summary>
        /// <param name="path">The golden file.</param>
        /// <param name="actual">The text produced now.</param>
        public static void Match(string path, string actual) {
            if (path == null) {
                throw new ArgumentNullException(nameof(path));
            }
            string normalized = (actual ?? string.Empty).Replace("\r\n", "\n");
            bool exists = File.Exists(path);
            if (Updating || !exists) {
                string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
                if (!string.IsNullOrEmpty(directory)) {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(path, normalized, new UTF8Encoding(false));
                if (!exists && !Updating) {
                    throw new GoldenFileMismatchException(path, "Golden file " + path + " did not exist and was created; review it and run the tests again.");
                }
                return;
            }
            string expected = File.ReadAllText(path, Encoding.UTF8).Replace("\r\n", "\n");
            if (string.Equals(expected, normalized, StringComparison.Ordinal)) {
                return;
            }
            throw new GoldenFileMismatchException(path, Diff(path, expected, normalized));
        }

        /// <summary>A readable description of where two texts differ.</summary>
        /// <param name="path">The golden file (for the message).</param>
        /// <param name="expected">The golden text.</param>
        /// <param name="actual">The text produced now.</param>
        /// <returns>The description.</returns>
        public static string Diff(string path, string expected, string actual) {
            string[] e = expected.Split('\n');
            string[] a = actual.Split('\n');
            int line = 0;
            while (line < e.Length && line < a.Length && string.Equals(e[line], a[line], StringComparison.Ordinal)) {
                line++;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("Output differs from ").Append(path).Append(" at line ").Append(line + 1);
            string el = line < e.Length ? e[line] : "<end of file>";
            string al = line < a.Length ? a[line] : "<end of output>";
            int column = 0;
            while (column < el.Length && column < al.Length && el[column] == al[column]) {
                column++;
            }
            sb.Append(", column ").Append(column + 1).Append(" (expected ").Append(e.Length).Append(" lines, got ").Append(a.Length).Append(").\n");
            sb.Append("Set ").Append(UpdateVariable).Append("=1 to accept the new output.\n\n");
            for (int i = Math.Max(0, line - 2); i < line; i++) {
                sb.Append("    ").Append(e[i]).Append('\n');
            }
            sb.Append("  - ").Append(el).Append('\n');
            sb.Append("  + ").Append(al).Append('\n');
            sb.Append("    ").Append(new string(' ', column)).Append("^\n");
            for (int i = line + 1; i < Math.Min(line + 3, Math.Max(e.Length, a.Length)); i++) {
                string ei = i < e.Length ? e[i] : string.Empty;
                string ai = i < a.Length ? a[i] : string.Empty;
                if (string.Equals(ei, ai, StringComparison.Ordinal)) {
                    sb.Append("    ").Append(ei).Append('\n');
                } else {
                    sb.Append("  - ").Append(ei).Append('\n').Append("  + ").Append(ai).Append('\n');
                }
            }
            return sb.ToString();
        }
    }

    /// <summary>Thrown when console output does not match its golden file.</summary>
    public sealed class GoldenFileMismatchException : Exception {
        /// <summary>Creates the exception.</summary>
        /// <param name="path">The golden file.</param>
        /// <param name="message">The readable difference.</param>
        public GoldenFileMismatchException(string path, string message) : base(message) {
            Path = path;
        }

        /// <summary>The golden file.</summary>
        public string Path { get; }
    }
}
