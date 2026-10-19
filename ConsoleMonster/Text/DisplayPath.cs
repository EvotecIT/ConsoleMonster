using System;

namespace ConsoleMonster.Text {
    /// <summary>
    /// Paths for display that may come from another system: recorded test runs, imported stack traces and error records
    /// carry Windows paths that a Unix host's path APIs would not split (a backslash is not a separator there), and the
    /// reverse. These helpers work on the text of the path, so the same path looks the same on every host; paths that
    /// belong to the host still go through its own path APIs for I/O.
    /// </summary>
    public static class DisplayPath {
        private static readonly char[] Separators = { '\\', '/' };

        /// <summary>The last segment of a path, splitting on both <c>\</c> and <c>/</c>.</summary>
        /// <param name="path">The path.</param>
        /// <returns>The file name, or the path itself when it has no separator.</returns>
        public static string FileName(string? path) {
            if (string.IsNullOrEmpty(path)) {
                return string.Empty;
            }
            // As Path.GetFileName on Windows: a path ending in a separator has no file name.
            int last = path!.LastIndexOfAny(Separators);
            return last >= 0 ? path.Substring(last + 1) : path;
        }

        /// <summary>Whether a path belongs to another kind of system than the current host (a drive or UNC path on Unix, a rooted Unix path on Windows).</summary>
        /// <param name="path">The path.</param>
        /// <returns>True for a foreign path.</returns>
        public static bool IsForeign(string? path) => IsForeign(path, Environment.OSVersion.Platform == PlatformID.Win32NT);

        /// <summary>Whether a path belongs to another kind of system than a host of the given kind.</summary>
        /// <param name="path">The path.</param>
        /// <param name="hostIsWindows">Whether the host is Windows.</param>
        /// <returns>True for a foreign path.</returns>
        public static bool IsForeign(string? path, bool hostIsWindows) {
            if (string.IsNullOrEmpty(path)) {
                return false;
            }
            bool windowsPath = IsWindowsRooted(path!);
            bool unixPath = path![0] == '/' && !(path.Length > 1 && path[1] == '/');
            return hostIsWindows ? unixPath : windowsPath;
        }

        /// <summary>
        /// A short form of a path as text: relative to <paramref name="basePath"/> when under it (compared without regard to
        /// case or separator style), otherwise its folder and file name, keeping the path's own separator.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="basePath">The base path, or null.</param>
        /// <returns>The short path.</returns>
        public static string Short(string path, string? basePath) {
            if (string.IsNullOrEmpty(path)) {
                return path ?? string.Empty;
            }
            char separator = path.IndexOf('\\') >= 0 ? '\\' : '/';
            string normalized = Normalize(path, separator);
            if (!string.IsNullOrEmpty(basePath)) {
                string root = Normalize(basePath!, separator).TrimEnd(separator) + separator;
                if (normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase) && normalized.Length > root.Length) {
                    return normalized.Substring(root.Length);
                }
            }
            string trimmed = normalized.TrimEnd(separator);
            int last = trimmed.LastIndexOf(separator);
            if (last <= 0) {
                return trimmed;
            }
            int previous = trimmed.LastIndexOf(separator, last - 1);
            string folder = trimmed.Substring(previous + 1, last - previous - 1);
            return folder.Length == 0 || (folder.Length == 2 && folder[1] == ':') ? trimmed.Substring(last + 1) : folder + separator + trimmed.Substring(last + 1);
        }

        private static string Normalize(string path, char separator) => path.Replace(separator == '\\' ? '/' : '\\', separator);

        private static bool IsWindowsRooted(string path) =>
            (path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && (path[2] == '\\' || path[2] == '/'))
            || path.StartsWith("\\\\", StringComparison.Ordinal);
    }
}
