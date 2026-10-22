using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ConsoleMonster.Live.Tails {
    /// <summary>Identifies an open file independently of its name, timestamps and length.</summary>
    internal readonly struct FileTailIdentity : IEquatable<FileTailIdentity> {
        private readonly long _device;
        private readonly long _file;

        private FileTailIdentity(long device, long file) { _device = device; _file = file; }

        /// <summary>Reads the platform identity without acquiring a lock or opening another file.</summary>
        internal static FileTailIdentity Read(FileStream stream) {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                if (!GetFileInformationByHandle(stream.SafeFileHandle, out WindowsInformation info)) { throw Failure(); }
                return new FileTailIdentity(info.Volume, ((long)info.IndexHigh << 32) | info.IndexLow);
            }
            bool mac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            Architecture arch = RuntimeInformation.ProcessArchitecture;
            if ((!mac && !RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) ||
                    (arch != Architecture.X64 && arch != Architecture.Arm64)) {
                throw new PlatformNotSupportedException("File tail identity requires Windows, or 64-bit Linux/macOS (x64/arm64).");
            }
            // Native stat layouts differ after the identity fields. Reserve more than either complete struct;
            // read only dev_t and ino_t at their ABI-defined offsets for the supported architectures.
            IntPtr buffer = Marshal.AllocHGlobal(512);
            try {
                int result = mac && arch == Architecture.X64
                    ? MacFStat(stream.SafeFileHandle, buffer) : FStat(stream.SafeFileHandle, buffer);
                if (result != 0) { throw Failure(); }
                return new FileTailIdentity(mac ? Marshal.ReadInt32(buffer) : Marshal.ReadInt64(buffer), Marshal.ReadInt64(buffer, 8));
            } finally {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <inheritdoc/>
        public bool Equals(FileTailIdentity other) => _device == other._device && _file == other._file;

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is FileTailIdentity other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => _device.GetHashCode() ^ _file.GetHashCode();

        private static IOException Failure() => new IOException("Could not identify the tailed file.", new Win32Exception(Marshal.GetLastWin32Error()));

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowsInformation {
            internal uint Attributes;
            internal System.Runtime.InteropServices.ComTypes.FILETIME Creation;
            internal System.Runtime.InteropServices.ComTypes.FILETIME Access;
            internal System.Runtime.InteropServices.ComTypes.FILETIME Write;
            internal uint Volume;
            internal uint SizeHigh;
            internal uint SizeLow;
            internal uint Links;
            internal uint IndexHigh;
            internal uint IndexLow;
        }

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out WindowsInformation information);

        [DllImport("libc", EntryPoint = "fstat", SetLastError = true)]
        private static extern int FStat(SafeFileHandle handle, IntPtr information);

        [DllImport("libc", EntryPoint = "fstat$INODE64", SetLastError = true)]
        private static extern int MacFStat(SafeFileHandle handle, IntPtr information);
    }
}
