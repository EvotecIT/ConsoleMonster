using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ConsoleMonster.FileSystem {
    /// <summary>
    /// Somewhere deleted files can be restored from: the Windows recycle bin, the macOS Trash or the freedesktop.org
    /// (XDG) trash on Linux. Delete operations use it; permanent deletion is a separate, explicit operation.
    /// </summary>
    public interface ITrash {
        /// <summary>The name people know it by, such as <c>Recycle Bin</c> or <c>Trash</c>.</summary>
        string Name { get; }

        /// <summary>Whether this machine has a trash this process can use. When false, <see cref="MoveToTrash"/> always fails.</summary>
        bool IsAvailable { get; }

        /// <summary>Moves a file or folder to the trash. It never deletes permanently in place of trashing.</summary>
        /// <param name="path">The file or folder.</param>
        /// <returns>The item's location inside the trash when the platform reports it; otherwise null.</returns>
        /// <exception cref="IOException">The item could not be trashed; nothing was deleted.</exception>
        string? MoveToTrash(string path);
    }

    /// <summary>The trash of the current platform.</summary>
    public static class FileTrash {
        private static readonly Lazy<ITrash> CurrentTrash = new Lazy<ITrash>(ForCurrentPlatform);

        /// <summary>The recycle bin on Windows, the Trash on macOS and the XDG trash elsewhere.</summary>
        public static ITrash Current => CurrentTrash.Value;

        /// <summary>A trash that is never available, for hosts that must not trash anything.</summary>
        public static ITrash Unavailable { get; } = new UnavailableTrash();

        private static ITrash ForCurrentPlatform() {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return new WindowsRecycleBin(); }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { return new MacTrash(); }
            return new XdgTrash();
        }

        internal static string FullExistingPath(string path) {
            if (string.IsNullOrEmpty(path)) { throw new ArgumentException("A path is required.", nameof(path)); }
            string full = Path.GetFullPath(path);
            string trimmed = full.Length > (Path.GetPathRoot(full)?.Length ?? 0) ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : full;
            if (!File.Exists(trimmed) && !Directory.Exists(trimmed) && !FileOperation.IsLink(trimmed)) { throw new FileNotFoundException("'" + trimmed + "' doesn't exist.", trimmed); }
            return trimmed;
        }

        private sealed class UnavailableTrash : ITrash {
            public string Name => "Trash";
            public bool IsAvailable => false;
            public string? MoveToTrash(string path) => throw new IOException("There's no trash available, so '" + path + "' was left in place.");
        }
    }
}
