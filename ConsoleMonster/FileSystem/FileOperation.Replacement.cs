using System;
using System.IO;

namespace ConsoleMonster.FileSystem {
    public sealed partial class FileOperation {
        private static string TemporaryName(string folder) => Path.Combine(folder, ".cm-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp");

        private sealed partial class Run {
            private static void MoveEntry(string source, string target, bool directory) {
                if (directory) { Directory.Move(source, target); } else { File.Move(source, target); }
            }

            // Directory links cannot use File.Replace. Keep the previous entry nearby until publication
            // succeeds, and restore it if the rename fails. Never follow the old link while replacing it.
            private static void ReplaceEntry(string source, string target, bool directory) {
                string backup = TemporaryName(Path.GetDirectoryName(target) ?? string.Empty);
                bool oldDirectory = Directory.Exists(target);
                MoveEntry(target, backup, oldDirectory);
                try { MoveEntry(source, target, directory); }
                catch (Exception error) when (IsFileError(error)) {
                    RestoreEntry(backup, target, oldDirectory, error);
                    throw;
                }
                try { DeleteFile(backup); }
                catch (Exception error) when (IsFileError(error)) {
                    throw new IOException("The replacement completed, but the previous entry remains at '" + backup + "'.", error);
                }
            }

            private static void RestoreEntry(string backup, string target, bool directory, Exception cause) {
                try { MoveEntry(backup, target, directory); }
                catch (Exception error) when (IsFileError(error)) {
                    throw new IOException("The rename failed and could not be restored. The original entry remains at '" + backup + "'.", new AggregateException(cause, error));
                }
            }
        }
    }
}
