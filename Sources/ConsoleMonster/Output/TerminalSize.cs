using System;
using ConsoleMonster.Interop;

namespace ConsoleMonster.Output {
    /// <summary>Reads the visible window size, rather than the scrollback buffer; safe to call every frame.</summary>
    public static class TerminalSize {
        /// <summary>Tries to read the visible size without throwing.</summary>
        /// <param name="width">Columns, or zero when unavailable.</param>
        /// <param name="height">Rows, or zero when unavailable.</param>
        /// <returns>Whether a positive size was read.</returns>
        public static bool TryGet(out int width, out int height) {
            width = 0;
            height = 0;
            try {
                if (Console.IsOutputRedirected) {
                    return false;
                }
                if (WindowsConsole.IsWindows) {
                    if (WindowsConsole.TryGetSize(WindowsConsole.StandardOutput, out width, out height)) {
                        return true;
                    }
                } else {
                    width = Console.WindowWidth;
                    height = Console.WindowHeight;
                    if (width > 0 && height > 0) {
                        return true;
                    }
                }
            } catch (Exception) {
                // Size queries are best effort, including hosts without a console.
            }
            width = height = 0;
            return false;
        }
    }
}
