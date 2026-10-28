using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ConsoleMonster.FileSystem {
    public sealed partial class FileOperation {
        private sealed partial class Run {
            private static void CreateCopyDirectory(string source, string target, bool created) {
                if (!created || Windows) { Directory.CreateDirectory(target); return; }
                // Populate inside a private directory, even when the source is read-only or the
                // process umask would expose its contents. Apply the exact source mode afterwards.
#if NET8_0_OR_GREATER
                Directory.CreateDirectory(target, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
#else
                _ = UnixDirectoryPermissions.Mode(source);
                UnixDirectoryPermissions.PrivateDirectory(target);
#endif
            }

            private static void FinishCopyDirectory(string source, string target) {
                if (Windows) { return; }
#if NET8_0_OR_GREATER
                File.SetUnixFileMode(target, File.GetUnixFileMode(source));
#else
                UnixDirectoryPermissions.SetMode(target, UnixDirectoryPermissions.Mode(source));
#endif
            }
        }
    }
}
