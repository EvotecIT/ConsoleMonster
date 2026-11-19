using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    // The colour of a name in a file listing, from theme roles so it follows the theme, the colour depth and NO_COLOR:
    // folders accent, links accent2, executables pass, hidden and generated files muted, then scripts and source code,
    // archives and packages, and images and media each in their own series colour. Everything else stays plain text.
    internal static class FileKinds {
        private static readonly bool Windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private static readonly Dictionary<string, ThemeToken> Extensions = Build();

        private static Dictionary<string, ThemeToken> Build() {
            Dictionary<string, ThemeToken> map = new Dictionary<string, ThemeToken>(StringComparer.OrdinalIgnoreCase);
            // Amber, as PowerShell's own $PSStyle.FileInfo draws PowerShell files.
            Add(map, ThemeToken.Series3, ".ps1", ".psm1", ".psd1", ".ps1xml", ".cs", ".csx", ".fs", ".fsx", ".vb", ".js", ".mjs", ".cjs", ".ts", ".mts",
                ".cts", ".tsx", ".jsx", ".py", ".rb", ".go", ".rs", ".java", ".kt", ".kts", ".swift", ".c", ".h", ".cpp", ".cc", ".cxx", ".hpp", ".m",
                ".php", ".lua", ".pl", ".r", ".dart", ".scala", ".sh", ".bash", ".zsh", ".fish", ".sql", ".html", ".htm", ".css", ".scss", ".less",
                ".vue", ".svelte", ".astro");
            Add(map, ThemeToken.Series4, ".zip", ".7z", ".rar", ".tar", ".gz", ".tgz", ".bz2", ".xz", ".zst", ".cab", ".nupkg", ".snupkg", ".jar",
                ".war", ".whl", ".iso", ".dmg", ".pkg", ".deb", ".rpm", ".apk", ".appx", ".msix", ".vhd", ".vhdx");
            Add(map, ThemeToken.Series6, ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".svg", ".webp", ".tif", ".tiff", ".heic", ".avif", ".psd",
                ".mp3", ".wav", ".flac", ".ogg", ".m4a", ".aac", ".mp4", ".mkv", ".mov", ".avi", ".webm", ".wmv");
            Add(map, ThemeToken.Muted, ".log", ".tmp", ".temp", ".bak", ".old", ".orig", ".swp", ".pdb", ".lock", ".cache");
            return map;
        }

        private static readonly HashSet<string> WindowsExecutables = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            ".exe", ".com", ".bat", ".cmd", ".msi"
        };

        private static void Add(Dictionary<string, ThemeToken> map, ThemeToken role, params string[] extensions) {
            foreach (string extension in extensions) { map[extension] = role; }
        }

        /// <summary>The role for a name, or null for plain text; <paramref name="item"/> adds the link, hidden and executable cues when it is known.</summary>
        internal static ThemeToken? Role(FileSystemInfo? item, string name, bool folder) {
            FileAttributes attributes = Attributes(item);
            if ((attributes & FileAttributes.ReparsePoint) != 0) { return ThemeToken.Accent2; }
            if (folder) { return ThemeToken.Accent; }
            if (name.StartsWith(".", StringComparison.Ordinal) || (attributes & FileAttributes.Hidden) != 0) { return ThemeToken.Muted; }
            string extension = Path.GetExtension(name);
            if (IsExecutable(item, extension)) { return ThemeToken.Pass; }
            return extension.Length > 0 && Extensions.TryGetValue(extension, out ThemeToken role) ? role : (ThemeToken?)null;
        }

        /// <summary>
        /// The name as markup in its role's colour, linked to the file in terminals that show links (markup links elsewhere
        /// would print the address after every name). Names stay literal.
        /// </summary>
        internal static string Markup(FileSystemInfo? item, string name, bool folder, string suffix = "") {
            string text = Text.Markup.Escape(TextSanitizer.Clean(name)) + suffix;
            ThemeToken? role = Role(item, name, folder);
            string? target = item == null || !Terminal.GetCurrentProfile().SupportsHyperlinks ? null : HyperlinkTarget.From(item.FullName);
            // A target with markup brackets or spaces cannot be written in a tag; the name is still coloured.
            if (target != null && target.IndexOfAny(new[] { '[', ']', ' ' }) >= 0) { target = null; }
            string style = (role == null ? "" : role.Value.ToString().ToLowerInvariant()) + (target == null ? "" : (role == null ? "" : " ") + "link=" + target);
            return style.Length == 0 ? text : "[" + style + "]" + text + "[/]";
        }

        // The Name column of the file views: the column script reads the item's own name cell through this type.
        internal const string NameScript = "([ConsoleMonster.PowerShell.CmdletEnableConsoleFormatting].Assembly.GetType('ConsoleMonster.PowerShell.FileKinds'))::Cell($_)";

        /// <summary>The Name cell of a file listing (called from <see cref="NameScript"/>); other values keep their text.</summary>
        public static object? Cell(object? value) {
            object? raw = value is PSObject wrapped ? wrapped.BaseObject : value;
            return raw is FileSystemInfo item ? new TableCell(Markup(item, item.Name, item is DirectoryInfo)) : raw;
        }

        private static FileAttributes Attributes(FileSystemInfo? item) {
            if (item == null) { return 0; }
            // An item removed since it was listed reports every flag (-1); it gets no cue from them.
            try { FileAttributes attributes = item.Attributes; return (int)attributes == -1 ? 0 : attributes; }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { return 0; }
        }

        private static bool IsExecutable(FileSystemInfo? item, string extension) {
            if (Windows) { return WindowsExecutables.Contains(extension); }
            if (!(item is FileInfo)) { return false; }
#if NET7_0_OR_GREATER
            try { return (item.UnixFileMode & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0; }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { return false; }
#else
            return false;
#endif
        }
    }
}
