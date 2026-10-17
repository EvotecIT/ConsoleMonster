using System;
using System.Runtime.InteropServices;
using ConsoleMonster.Interop;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Puts text on the clipboard without dependencies: the Win32 clipboard on Windows; elsewhere an OSC 52 request that
    /// most terminals (Windows Terminal, iTerm2, kitty, WezTerm, tmux with set-clipboard) honour.
    /// </summary>
    public static class Clipboard {
        private const uint UnicodeText = 13;
        private const uint MoveableMemory = 0x0002;

        /// <summary>Copies text; returns whether the clipboard accepted it (OSC 52 cannot be confirmed and counts as sent).</summary>
        /// <param name="text">The text.</param>
        /// <param name="osc52">Writes an OSC 52 sequence through this callback when the Win32 clipboard is not available.</param>
        /// <returns>Whether it worked.</returns>
        public static bool TrySetText(string text, Action<string>? osc52 = null) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            if (WindowsConsole.IsWindows) {
                for (int attempt = 0; attempt < 5; attempt++) {
                    if (TrySetWin32(text)) {
                        return true;
                    }
                    System.Threading.Thread.Sleep(30);
                }
                return false;
            }
            if (osc52 == null) {
                return false;
            }
            osc52("\u001b]52;c;" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text)) + "\u0007");
            return true;
        }

        private static bool TrySetWin32(string text) {
            if (!OpenClipboard(IntPtr.Zero)) {
                return false;
            }
            try {
                if (!EmptyClipboard()) {
                    return false;
                }
                int bytes = (text.Length + 1) * 2;
                IntPtr memory = GlobalAlloc(MoveableMemory, (UIntPtr)bytes);
                if (memory == IntPtr.Zero) {
                    return false;
                }
                IntPtr target = GlobalLock(memory);
                if (target == IntPtr.Zero) {
                    GlobalFree(memory);
                    return false;
                }
                try {
                    char[] chars = (text + "\0").ToCharArray();
                    Marshal.Copy(chars, 0, target, chars.Length);
                } finally {
                    GlobalUnlock(memory);
                }
                if (SetClipboardData(UnicodeText, memory) == IntPtr.Zero) {
                    GlobalFree(memory);
                    return false;
                }
                return true;
            } finally {
                CloseClipboard();
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
        private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr memory);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalUnlock(IntPtr memory);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalFree(IntPtr memory);
    }
}
