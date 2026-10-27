using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ConsoleMonster.FileSystem {
    /// <summary>
    /// The freedesktop.org trash used by Linux desktops (GNOME Files, Dolphin, <c>gio trash</c>): items move to
    /// <c>Trash/files</c> with a <c>.trashinfo</c> record in <c>Trash/info</c>, so file managers can restore them.
    /// </summary>
    /// <remarks>
    /// Items on the home volume go to <c>$XDG_DATA_HOME/Trash</c> (normally <c>~/.local/share/Trash</c>). Items on
    /// another mounted volume go to that volume's <c>.Trash/$uid</c> or <c>.Trash-$uid</c>, as the specification asks,
    /// because a trash on another volume would turn a rename into a full copy.
    /// </remarks>
    public sealed class XdgTrash : ITrash {
        private readonly bool _volumeTrash;

        /// <summary>Uses the current user's home trash and per-volume trash directories.</summary>
        public XdgTrash() : this(DefaultHome(), true) { }

        /// <summary>Uses one trash directory for every item, such as a test folder or a portable install.</summary>
        /// <param name="homeTrash">The trash directory; <c>files</c> and <c>info</c> are created inside it.</param>
        public XdgTrash(string homeTrash) : this(homeTrash, false) { }

        private XdgTrash(string homeTrash, bool volumeTrash) {
            if (string.IsNullOrEmpty(homeTrash)) { throw new ArgumentException("A trash directory is required.", nameof(homeTrash)); }
            HomeTrash = Path.GetFullPath(homeTrash);
            _volumeTrash = volumeTrash;
        }

        /// <summary>The home trash directory.</summary>
        public string HomeTrash { get; }

        /// <inheritdoc />
        public string Name => "Trash";

        /// <inheritdoc />
        public bool IsAvailable => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !_volumeTrash;

        /// <inheritdoc />
        public string? MoveToTrash(string path) {
            if (!IsAvailable) { throw new IOException("The XDG trash isn't used on Windows, so '" + path + "' was left in place."); }
            string full = FileTrash.FullExistingPath(path);
            if (IsInside(full, HomeTrash) || IsInside(HomeTrash, full)) { throw new IOException("'" + full + "' is part of the trash itself."); }
            string? volume = _volumeTrash ? VolumeOf(full) : null;
            bool sameVolume = !_volumeTrash || volume == null || volume == VolumeOf(HomeTrash);
            if (sameVolume) {
                try { return Trash(full, HomeTrash, null); }
                catch (IOException) when (_volumeTrash && volume != null && volume != "/") { }
            }
            string? topTrash = volume == null ? null : TopTrash(volume);
            if (topTrash == null) { throw new IOException("'" + full + "' is on a volume without a usable trash, so it was left in place."); }
            return Trash(full, topTrash, volume);
        }

        private static string Trash(string full, string trash, string? topDirectory) {
            string files = Path.Combine(trash, "files");
            string info = Path.Combine(trash, "info");
            Directory.CreateDirectory(files);
            Directory.CreateDirectory(info);
            string name = Path.GetFileName(full);
            string location = topDirectory == null ? full : full.Substring(topDirectory.TrimEnd('/').Length + 1);
            string record = "[Trash Info]\nPath=" + Escape(location) + "\nDeletionDate=" + DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture) + "\n";
            for (int attempt = 1; attempt < 10000; attempt++) {
                string candidate = attempt == 1 ? name : Path.GetFileNameWithoutExtension(name) + "." + attempt.ToString(CultureInfo.InvariantCulture) + Path.GetExtension(name);
                string infoPath = Path.Combine(info, candidate + ".trashinfo");
                string target = Path.Combine(files, candidate);
                // The info file is created exclusively first: it reserves the name against other trashing processes.
                FileStream reserved;
                try { reserved = new FileStream(infoPath, FileMode.CreateNew, FileAccess.Write, FileShare.None); }
                catch (IOException) when (File.Exists(infoPath)) { continue; }
                using (reserved) {
                    byte[] bytes = Encoding.UTF8.GetBytes(record);
                    reserved.Write(bytes, 0, bytes.Length);
                }
                if (File.Exists(target) || Directory.Exists(target)) { File.Delete(infoPath); continue; }
                try {
                    if (Directory.Exists(full) && !FileOperation.IsLink(full)) { Directory.Move(full, target); } else { File.Move(full, target); }
                } catch {
                    File.Delete(infoPath);
                    throw;
                }
                return target;
            }
            throw new IOException("The trash has no free name for '" + name + "'.");
        }

        private static string? TopTrash(string topDirectory) {
            string? uid = UserId();
            if (uid == null) { return null; }
            string shared = Path.Combine(topDirectory, ".Trash");
#if NET8_0_OR_GREATER
            // The shared .Trash is only trusted when it's a real, sticky directory.
            if (Directory.Exists(shared) && !FileOperation.IsLink(shared) && (File.GetUnixFileMode(shared) & UnixFileMode.StickyBit) != 0) {
                string own = Path.Combine(shared, uid);
                try { Directory.CreateDirectory(own); return own; } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
#endif
            string personal = Path.Combine(topDirectory, ".Trash-" + uid);
            try {
                if (!Directory.Exists(personal)) {
#if NET8_0_OR_GREATER
                    Directory.CreateDirectory(personal, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
#else
                    Directory.CreateDirectory(personal);
#endif
                }
                return FileOperation.IsLink(personal) ? null : personal;
            } catch (IOException) { return null; } catch (UnauthorizedAccessException) { return null; }
        }

        private static string DefaultHome() {
            string? data = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (string.IsNullOrEmpty(data) || !Path.IsPathRooted(data)) {
                data = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local"), "share");
            }
            return Path.Combine(data!, "Trash");
        }

        private static string? UserId() {
            try {
                foreach (string line in File.ReadAllLines("/proc/self/status")) {
                    if (line.StartsWith("Uid:", StringComparison.Ordinal)) {
                        string[] parts = line.Substring(4).Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        return parts.Length > 0 ? parts[0] : null;
                    }
                }
            } catch (IOException) { } catch (UnauthorizedAccessException) { }
            return null;
        }

        // The mount point that holds a path: the longest mount prefix.
        internal static string? VolumeOf(string path) {
            string? best = null;
            try {
                foreach (DriveInfo drive in DriveInfo.GetDrives()) {
                    string root = drive.Name;
                    if (IsInside(path, root) && (best == null || root.TrimEnd('/').Length > best.TrimEnd('/').Length)) { best = root; }
                }
            } catch (IOException) { } catch (UnauthorizedAccessException) { }
            return best;
        }

        private static bool IsInside(string path, string folder) {
            string root = folder.TrimEnd('/', '\\');
            if (root.Length == 0) { return path.StartsWith("/", StringComparison.Ordinal); }
            return path.Equals(root, StringComparison.Ordinal) || path.StartsWith(root + "/", StringComparison.Ordinal) || path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        }

        // The specification stores paths as URL-escaped bytes with '/' kept.
        private static string Escape(string path) {
            List<string> parts = new List<string>();
            foreach (string part in path.Split('/')) { parts.Add(Uri.EscapeDataString(part)); }
            return string.Join("/", parts.ToArray());
        }
    }
}
