using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ConsoleMonster.FileSystem {
    /// <summary>The Windows recycle bin through the shell's own delete, so Explorer can restore the items.</summary>
    internal sealed class WindowsRecycleBin : ITrash {
        private const uint Delete = 3;
        private const ushort Silent = 0x0004, NoConfirmation = 0x0010, AllowUndo = 0x0040, NoErrorUi = 0x0400, WantNukeWarning = 0x4000;

        public string Name => "Recycle Bin";

        public bool IsAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public string? MoveToTrash(string path) {
            if (!IsAvailable) { throw new IOException("The recycle bin is only available on Windows."); }
            string full = FileTrash.FullExistingPath(path);
            // The shell API stops at MAX_PATH and would otherwise fail with an undocumented code.
            if (full.Length >= 260) { throw new PathTooLongException("'" + full + "' is too long for the recycle bin."); }
            string root = Path.GetPathRoot(full) ?? full;
            // Drives without a recycle bin (network shares, some removable media) make the shell delete permanently.
            // Refuse those up front; the nuke warning is the backstop for items too large to recycle.
            int query;
            if (IntPtr.Size == 4) {
                ShQueryRbInfo32 info = new ShQueryRbInfo32 { Size = 20 };
                query = SHQueryRecycleBin32(root, ref info);
            } else {
                ShQueryRbInfo64 info = new ShQueryRbInfo64 { Size = 24 };
                query = SHQueryRecycleBin64(root, ref info);
            }
            if (query != 0) { throw new IOException("The drive '" + root + "' has no recycle bin, so '" + full + "' was left in place."); }
            ushort flags = (ushort)(Silent | NoConfirmation | AllowUndo | NoErrorUi | WantNukeWarning);
            int result;
            bool aborted;
            if (IntPtr.Size == 4) {
                ShFileOpStruct32 operation = new ShFileOpStruct32 { Function = Delete, From = full + "\0", Flags = flags };
                result = SHFileOperation32(ref operation);
                aborted = operation.AnyOperationsAborted != 0;
            } else {
                ShFileOpStruct64 operation = new ShFileOpStruct64 { Function = Delete, From = full + "\0", Flags = flags };
                result = SHFileOperation64(ref operation);
                aborted = operation.AnyOperationsAborted != 0;
            }
            if (result != 0 || aborted) { throw new IOException("The shell couldn't recycle '" + full + "' (code 0x" + result.ToString("X", System.Globalization.CultureInfo.InvariantCulture) + ")."); }
            if (File.Exists(full) || Directory.Exists(full)) { throw new IOException("'" + full + "' is still in place after recycling."); }
            return null;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ShFileOpStruct64 {
            public IntPtr Window;
            public uint Function;
            public string From;
            public string? To;
            public ushort Flags;
            public int AnyOperationsAborted;
            public IntPtr NameMappings;
            public string? ProgressTitle;
        }

        // shellapi.h packs this structure to one byte on 32-bit Windows.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
        private struct ShFileOpStruct32 {
            public IntPtr Window;
            public uint Function;
            public string From;
            public string? To;
            public ushort Flags;
            public int AnyOperationsAborted;
            public IntPtr NameMappings;
            public string? ProgressTitle;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ShQueryRbInfo64 {
            public int Size;
            public long TotalSize;
            public long NumItems;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ShQueryRbInfo32 {
            public int Size;
            public long TotalSize;
            public long NumItems;
        }

        [DllImport("shell32.dll", EntryPoint = "SHFileOperationW", CharSet = CharSet.Unicode)] private static extern int SHFileOperation64(ref ShFileOpStruct64 operation);
        [DllImport("shell32.dll", EntryPoint = "SHFileOperationW", CharSet = CharSet.Unicode)] private static extern int SHFileOperation32(ref ShFileOpStruct32 operation);
        [DllImport("shell32.dll", EntryPoint = "SHQueryRecycleBinW", CharSet = CharSet.Unicode)] private static extern int SHQueryRecycleBin64(string rootPath, ref ShQueryRbInfo64 info);
        [DllImport("shell32.dll", EntryPoint = "SHQueryRecycleBinW", CharSet = CharSet.Unicode)] private static extern int SHQueryRecycleBin32(string rootPath, ref ShQueryRbInfo32 info);
    }
}
