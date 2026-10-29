using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ConsoleMonster.FileSystem {
    internal static class UnixDirectoryPermissions {
        // Linux statx has a fixed, architecture-independent layout. Unlike managed UnixFileMode,
        // it also proves ownership and, with AT_SYMLINK_NOFOLLOW, the identity of the directory entry.
        private static bool TryDirectoryStatus(string path, out DirectoryStatus status) {
            status = default;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                DarwinStatus darwin;
                int result = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? DarwinStatusX64(path, out darwin) : DarwinStatusArm64(path, out darwin);
                status.Mask = 0x0B;
                status.UserId = darwin.UserId;
                status.Mode = darwin.Mode;
                return result == 0 && (status.Mode & 0xF000) == 0x4000;
            }
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return false; }
            try { return Statx(-100, path, 0x100, 0x0B, out status) == 0 && (status.Mask & 0x0B) == 0x0B && (status.Mode & 0xF000) == 0x4000; }
            catch (EntryPointNotFoundException) { return false; }
            catch (DllNotFoundException) { return false; }
        }

        internal static bool IsStickyDirectory(string path) => TryDirectoryStatus(path, out DirectoryStatus status) && (status.Mode & 0x200) != 0;

        /// <summary>Creates a private trash directory atomically, or verifies that an existing one is owned and private. Never repairs another user's directory.</summary>
        internal static void PrivateDirectory(string path) {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { throw new IOException("Private directories require a supported Unix filesystem."); }
            string? parent = Path.GetDirectoryName(path);
            if (parent != null && !Directory.Exists(parent)) { Directory.CreateDirectory(parent); }
            // mkdir applies 0700 before the entry is visible; umask can only make it more restrictive.
            try {
                int created = MakeDirectory(path, 0x1C0);
                if (created != 0 && Marshal.GetLastWin32Error() != 17) { throw new IOException("Cannot create private trash directory '" + path + "'."); }
                if (!TryDirectoryStatus(path, out DirectoryStatus status) || status.UserId != EffectiveUserId() || (status.Mode & 0x3F) != 0) {
                    throw new IOException("Trash directory '" + path + "' must be a real directory owned by the current user with no group or other permissions.");
                }
            } catch (EntryPointNotFoundException error) { throw new IOException("Cannot verify private trash ownership on this system.", error); }
        }

        internal static uint Mode(string path) {
            if (!TryDirectoryStatus(path, out DirectoryStatus status)) { throw new IOException("Cannot verify directory permissions for '" + path + "'."); }
            return (uint)(status.Mode & 0xFFF);
        }

        internal static void SetMode(string path, uint mode) {
            if (ChangeMode(path, mode) != 0) { throw new IOException("Cannot preserve directory permissions for '" + path + "'."); }
        }

        [DllImport("libc", EntryPoint = "chmod", SetLastError = true)]
        private static extern int ChangeMode(string path, uint mode);

        [StructLayout(LayoutKind.Explicit, Size = 256)]
        private struct DirectoryStatus {
            [FieldOffset(0)] internal uint Mask;
            [FieldOffset(20)] internal uint UserId;
            [FieldOffset(28)] internal ushort Mode;
        }

        // Darwin's 64-bit-inode stat ABI has mode_t after dev_t and uid_t after ino_t.
        // Intel exposes that ABI as lstat$INODE64; Apple Silicon uses it for lstat directly.
        [StructLayout(LayoutKind.Explicit, Size = 256)]
        private struct DarwinStatus {
            [FieldOffset(4)] internal ushort Mode;
            [FieldOffset(16)] internal uint UserId;
        }

        [DllImport("libc", EntryPoint = "lstat$INODE64", SetLastError = true)]
        private static extern int DarwinStatusX64(string path, out DarwinStatus status);

        [DllImport("libc", EntryPoint = "lstat", SetLastError = true)]
        private static extern int DarwinStatusArm64(string path, out DarwinStatus status);

        [DllImport("libc", EntryPoint = "statx", SetLastError = true)]
        private static extern int Statx(int directory, string path, int flags, uint mask, out DirectoryStatus status);

        [DllImport("libc", EntryPoint = "mkdir", SetLastError = true)]
        private static extern int MakeDirectory(string path, uint mode);

        [DllImport("libc", EntryPoint = "geteuid")]
        private static extern uint EffectiveUserId();
    }
}

