using System;
using System.Threading;

namespace ConsoleMonster.Interop {
    /// <summary>
    /// Capability detection turns on VT processing for stdout, and output written between ConsoleMonster calls (encoded
    /// host writes, PowerShell's own colours) relies on it for the rest of the process. The console mode belongs to the
    /// console, not the process, so the shell that started the process would keep it afterwards. The mode found before
    /// the first change is restored when the process exits, unless something else has changed the mode since.
    /// </summary>
    internal static class DetectionOutputMode {
        private static int _registered;
        private static IntPtr _handle;
        private static uint _original;
        private static uint _enabled;

        /// <summary>Enables VT processing on a console handle; the first change in the process is undone at exit.</summary>
        /// <param name="handle">The stdout console handle.</param>
        /// <param name="mode">The handle's current mode.</param>
        /// <returns>Whether VT processing is on.</returns>
        internal static bool Enable(IntPtr handle, uint mode) {
            if ((mode & WindowsConsole.EnableVirtualTerminalProcessing) != 0) {
                return true;
            }
            uint enabled = mode | WindowsConsole.EnableVirtualTerminalProcessing;
            if (!WindowsConsole.TrySetMode(handle, enabled)) {
                return false;
            }
            if (Interlocked.CompareExchange(ref _registered, 1, 0) == 0) {
                _handle = handle;
                _original = mode;
                _enabled = enabled;
                AppDomain.CurrentDomain.ProcessExit += (sender, e) => Restore();
            }
            return true;
        }

        private static void Restore() {
            try {
                if (WindowsConsole.TryGetMode(_handle, out uint current) && current == _enabled) {
                    WindowsConsole.TrySetMode(_handle, _original);
                }
            } catch (Exception) {
                // An exit handler must not fail the process.
            }
        }
    }
}
