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
                    if (UnixTerminalSize.TryGet(out width, out height)) { return true; }
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

        /// <summary>
        /// Reads the visible window's position in the screen buffer and its size. On Windows consoles the position changes
        /// when the user scrolls the window; elsewhere it is always (0, 0).
        /// </summary>
        /// <param name="left">The window's left column in the buffer.</param>
        /// <param name="top">The window's top row in the buffer.</param>
        /// <param name="width">The window width in cells.</param>
        /// <param name="height">The window height in cells.</param>
        /// <returns>True when the window could be read.</returns>
        public static bool TryGetWindow(out int left, out int top, out int width, out int height) {
            left = 0;
            top = 0;
            try {
                if (!Console.IsOutputRedirected && WindowsConsole.IsWindows && WindowsConsole.TryGetWindow(WindowsConsole.StandardOutput, out left, out top, out width, out height)) {
                    return true;
                }
            } catch (Exception) {
                // Best effort.
            }
            left = 0;
            top = 0;
            return TryGet(out width, out height);
        }

        internal static bool TryComputeWindowSize(int left, int top, int right, int bottom, out int width, out int height) {
            return ConsoleRectangle.TryComputeSize(left, top, right, bottom, out width, out height);
        }
    }
}
