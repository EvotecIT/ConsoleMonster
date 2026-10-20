using System;
using System.Runtime.InteropServices;

namespace ConsoleMonster.Output {
    /// <summary>
    /// Where the console cursor is, without side effects. On Windows the position comes from the console API. On Unix, .NET
    /// answers Console.CursorLeft/CursorTop by writing a cursor-position query (ESC[6n) and reading the reply from the
    /// keyboard input: a terminal that does not answer (or answers late) leaves the query waiting, and keys the user types,
    /// or that a prompt is about to read, are consumed or corrupted. So on Unix the position is never queried.
    /// </summary>
    internal static class ConsoleCursor {
        /// <summary>Whether the cursor position can be read without writing a query to the terminal.</summary>
        internal static bool CanQuery { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>The cursor position, or false when it is unknown (Unix, no console).</summary>
        internal static bool TryGetPosition(out int left, out int top) => TryGetPosition(CanQuery, () => Console.CursorLeft, () => Console.CursorTop, out left, out top);

        /// <summary>The cursor position through the given readers, never called when <paramref name="canQuery"/> is false.</summary>
        internal static bool TryGetPosition(bool canQuery, Func<int> readLeft, Func<int> readTop, out int left, out int top) {
            left = -1;
            top = -1;
            if (!canQuery) {
                return false;
            }
            try {
                left = readLeft();
                top = readTop();
                return true;
            } catch (Exception ex) when (ex is System.IO.IOException || ex is InvalidOperationException || ex is PlatformNotSupportedException) {
                left = -1;
                top = -1;
                return false;
            }
        }
    }
}