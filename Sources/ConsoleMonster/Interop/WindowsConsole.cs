using System;
using System.Runtime.InteropServices;

namespace ConsoleMonster.Interop {
    // Private entry points prevent callers from bypassing the platform guard.
    internal static class WindowsConsole {
        internal const uint EnableVirtualTerminalProcessing = 0x0004;
        internal static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        internal static IntPtr StandardOutput => IsWindows ? GetStdHandle(-11) : IntPtr.Zero;

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
            return info.Window.TryGetSize(out width, out height);
        }

        internal static bool TryGetBackgroundColor(IntPtr handle, out uint color, out int attributeIndex) {
            color = 0;
            attributeIndex = 0;
            ConsoleScreenBufferInfoEx info = new ConsoleScreenBufferInfoEx {
                Size = 96,
                ColorTable = new uint[16]
            };
            if (!IsWindows || !GetConsoleScreenBufferInfoEx(handle, ref info)) {
                return false;
            }
            attributeIndex = (info.Attributes >> 4) & 0x0F;
            color = info.ColorTable[attributeIndex];
            return true;
        }

        internal static bool TryGetFont(IntPtr handle, out string? face, out bool raster) {
            return TryGetFont(handle, out face, out raster, out _);
        }

        internal static bool TryGetFont(IntPtr handle, out string? face, out bool raster, out int height) {
            face = null;
            raster = false;
            height = 0;
            // Fixed native size, including 32 WCHARs. No reflection-based struct sizing.
            ConsoleFontInfo info = new ConsoleFontInfo { Size = 84, FaceName = string.Empty };
            if (!IsWindows || !GetCurrentConsoleFontEx(handle, false, ref info)) {
                return false;
            }
            face = info.FaceName;
            raster = (info.FontFamily & 4) == 0; // TMPF_TRUETYPE
            height = info.FontSize.Y;
            return true;
        }

        internal static uint FileType(IntPtr handle) => IsWindows ? GetFileType(handle) : 0;

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
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "GetConsoleScreenBufferInfoEx")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetConsoleScreenBufferInfoEx(IntPtr handle, ref ConsoleScreenBufferInfoEx info);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetFileType(IntPtr handle);
        [DllImport("ntdll.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern int RtlGetVersion(ref WindowsVersionInfo info);
    }
}
