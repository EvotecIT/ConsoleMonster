using System;
using System.IO;

namespace ConsoleMonster.Tests.FileSystem {
    /// <summary>A throwaway folder under the system temp directory; tests never touch anything outside it.</summary>
    internal sealed class TempFolder : IDisposable {
        internal TempFolder() {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cm-files-" + Guid.NewGuid().ToString("N").Substring(0, 10));
            Directory.CreateDirectory(Path);
        }

        internal string Path { get; }

        internal string Combine(params string[] parts) {
            string result = Path;
            foreach (string part in parts) { result = System.IO.Path.Combine(result, part); }
            return result;
        }

        internal string File(string relative, string content = "data", DateTime? modified = null) {
            string full = Combine(relative.Split('/'));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            System.IO.File.WriteAllText(full, content);
            if (modified.HasValue) { System.IO.File.SetLastWriteTimeUtc(full, modified.Value); }
            return full;
        }

        internal string Folder(string relative) {
            string full = Combine(relative.Split('/'));
            Directory.CreateDirectory(full);
            return full;
        }

        public void Dispose() {
            try {
                foreach (string file in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories)) {
                    try { System.IO.File.SetAttributes(file, FileAttributes.Normal); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
                Directory.Delete(Path, true);
            } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
