using System;
using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    // Private entry points prevent callers from bypassing the platform guard.
    internal static class WindowsConsole {
        internal const uint EnableVirtualTerminalProcessing = 0x0004;

        /// <summary>Input mode flag that delivers VT sequences (such as cursor position reports) to the application.</summary>
        internal const uint EnableVirtualTerminalInput = 0x0200;

        internal static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        internal static IntPtr StandardOutput => IsWindows ? GetStdHandle(-11) : IntPtr.Zero;

        internal static IntPtr StandardInput => IsWindows ? GetStdHandle(-10) : IntPtr.Zero;

        internal static bool TryGetMode(IntPtr handle, out uint mode) {
            mode = 0;
            return IsWindows && GetConsoleMode(handle, out mode);
        }

        internal static bool TrySetMode(IntPtr handle, uint mode) => IsWindows && SetConsoleMode(handle, mode);

        internal static bool TryWrite(IntPtr handle, string text, out int written) {
            written = 0;
            if (!IsWindows) {
                return false;
            }
            bool success = WriteConsoleW(handle, text, (uint)text.Length, out uint count, IntPtr.Zero);
            written = (int)count;
            return success;
        }

        internal static bool TryGetSize(IntPtr handle, out int width, out int height) {
            width = 0;
            height = 0;
            if (!IsWindows || !GetConsoleScreenBufferInfo(handle, out ConsoleBufferInfo info)) {
                return false;
            }
            width = info.Window.Right - info.Window.Left + 1;
            height = info.Window.Bottom - info.Window.Top + 1;
            return width > 0 && height > 0;
        }

        internal static bool TryGetFont(IntPtr handle, out string? face, out bool raster) {
            face = null;
            raster = false;
            // Fixed native size, including 32 WCHARs. No reflection-based struct sizing.
            ConsoleFontInfo info = new ConsoleFontInfo { Size = 84, FaceName = string.Empty };
            if (!IsWindows || !GetCurrentConsoleFontEx(handle, false, ref info)) {
                return false;
            }
            face = info.FaceName;
            raster = (info.FontFamily & 4) == 0; // TMPF_TRUETYPE
            return true;
        }

        internal static uint FileType(IntPtr handle) => IsWindows ? GetFileType(handle) : 0;

        /// <summary>Copies pending input records without removing them from the queue.</summary>
        internal static int PeekInput(IntPtr handle, ConsoleInputRecord[] buffer) {
            if (!IsWindows || !GetNumberOfConsoleInputEvents(handle, out uint pending) || pending == 0) {
                return 0;
            }
            uint length = Math.Min(pending, (uint)buffer.Length);
            return PeekConsoleInputW(handle, buffer, length, out uint read) ? (int)read : 0;
        }

        /// <summary>Removes the first <paramref name="count"/> input records from the queue.</summary>
        internal static int ReadInput(IntPtr handle, ConsoleInputRecord[] buffer, int count) {
            if (!IsWindows || count <= 0) {
                return 0;
            }
            return ReadConsoleInputW(handle, buffer, (uint)count, out uint read) ? (int)read : 0;
        }

        /// <summary>Appends input records to the queue (used to give back keys read together with a terminal reply).</summary>
        internal static bool WriteInput(IntPtr handle, ConsoleInputRecord[] records, int count) {
            if (!IsWindows || count <= 0) {
                return true;
            }
            return WriteConsoleInputW(handle, records, (uint)count, out uint written) && written == count;
        }

        internal static int BuildNumber() {
            if (!IsWindows) {
                return 0;
            }
            // Avoid the application-manifest version lie on .NET Framework.
            WindowsVersionInfo info = new WindowsVersionInfo { Size = 276, ServicePack = string.Empty };
            return RtlGetVersion(ref info) == 0 ? (int)info.Build : 0;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int standardHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetConsoleMode(IntPtr handle, out uint mode);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetConsoleMode(IntPtr handle, uint mode);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteConsoleW(IntPtr handle, string buffer, uint count, out uint written, IntPtr reserved);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCurrentConsoleFontEx(IntPtr handle, [MarshalAs(UnmanagedType.Bool)] bool maximumWindow, ref ConsoleFontInfo info);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetConsoleScreenBufferInfo(IntPtr handle, out ConsoleBufferInfo info);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetFileType(IntPtr handle);
        [DllImport("ntdll.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern int RtlGetVersion(ref WindowsVersionInfo info);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetNumberOfConsoleInputEvents(IntPtr handle, out uint count);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PeekConsoleInputW(IntPtr handle, [Out] ConsoleInputRecord[] buffer, uint length, out uint read);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadConsoleInputW(IntPtr handle, [Out] ConsoleInputRecord[] buffer, uint length, out uint read);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteConsoleInputW(IntPtr handle, ConsoleInputRecord[] buffer, uint length, out uint written);
    }
}
