using System;
using System.Runtime.InteropServices;

namespace ConsoleMonster.Clipboard {
    /// <summary>Owns a movable Unicode buffer until SetClipboardData transfers it to Windows.</summary>
    internal static class WindowsClipboard {
        internal static bool TryCopy(string text, out string? error) {
            error = "The Windows clipboard is unavailable or busy. Try again in your local desktop session.";
            if (text.IndexOf('\0') >= 0) {
                error = "Clipboard text cannot contain a null character.";
                return false;
            }
            IntPtr memory = GlobalAlloc(0x0002, new UIntPtr(checked((uint)((text.Length + 1L) * 2))));
            if (memory == IntPtr.Zero) {
                return false;
            }
            bool opened = false;
            IntPtr owner = IntPtr.Zero;
            try {
                IntPtr buffer = GlobalLock(memory);
                if (buffer == IntPtr.Zero) {
                    return false;
                }
                try {
                    char[] chars = (text + '\0').ToCharArray();
                    Marshal.Copy(chars, 0, buffer, chars.Length);
                } finally {
                    GlobalUnlock(memory);
                }
                // EmptyClipboard assigns ownership to this window. A null owner would make SetClipboardData fail.
                owner = CreateWindowEx(0, "STATIC", string.Empty, 0, 0, 0, 0, 0, new IntPtr(-3), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                opened = owner != IntPtr.Zero && OpenClipboard(owner);
                if (!opened || !EmptyClipboard() || SetClipboardData(13, memory) == IntPtr.Zero) {
                    return false;
                }
                memory = IntPtr.Zero;
                error = null;
                return true;
            } finally {
                if (opened) {
                    CloseClipboard();
                }
                if (memory != IntPtr.Zero) {
                    GlobalFree(memory);
                }
                if (owner != IntPtr.Zero) {
                    DestroyWindow(owner);
                }
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenClipboard(IntPtr owner);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseClipboard();
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EmptyClipboard();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint format, IntPtr memory);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint flags, UIntPtr size);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr memory);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalUnlock(IntPtr memory);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalFree(IntPtr memory);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowEx(uint extendedStyle, string className, string windowName, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr window);
    }
}
