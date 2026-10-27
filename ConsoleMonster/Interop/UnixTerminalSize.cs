using System;
using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    /// <summary>Reads the kernel's current TTY size without the CLR's SIGWINCH cache.</summary>
    internal static class UnixTerminalSize {
        internal static bool TryGet(out int width, out int height) {
            width = height = 0;
            bool mac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            if (!mac && !RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return false; }
            WindowSize size = default;
            UIntPtr request = mac ? (UIntPtr)0x40087468 : (UIntPtr)0x5413;
            int result = mac && RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? IoctlMacArm64(1, request, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref size)
                : Ioctl(1, request, ref size);
            if (result != 0 || size.Columns == 0 || size.Rows == 0) { return false; }
            width = size.Columns; height = size.Rows;
            return true;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowSize { internal ushort Rows, Columns, PixelWidth, PixelHeight; }
        [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
        private static extern int Ioctl(int descriptor, UIntPtr request, ref WindowSize size);
        // Darwin arm64 puts variadic arguments on the stack after the fixed register arguments.
        [DllImport("libc", EntryPoint = "ioctl", SetLastError = true)]
        private static extern int IoctlMacArm64(int descriptor, UIntPtr request, IntPtr x2, IntPtr x3, IntPtr x4, IntPtr x5, IntPtr x6, IntPtr x7, ref WindowSize size);
    }
}
