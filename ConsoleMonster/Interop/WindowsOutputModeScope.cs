using System;
using System.Threading;
using ConsoleMonster.Output;

namespace ConsoleMonster.Interop {
    /// <summary>Saves stdout's native console mode and restores it once, optionally enabling VT for the lifetime.</summary>
    internal sealed class WindowsOutputModeScope : IDisposable {
        private readonly IntPtr _handle;
        private readonly uint _originalMode;
        private readonly bool _captured;
        private int _disposed;

        /// <summary>Captures the exact current mode before any capability detection or output changes it.</summary>
        /// <param name="enableVirtualTerminal">Whether to enable VT processing until disposal.</param>
        internal WindowsOutputModeScope(bool enableVirtualTerminal = false) {
            _handle = WindowsConsole.StandardOutput;
            _captured = WindowsConsole.TryGetMode(_handle, out _originalMode);
            if (_captured && enableVirtualTerminal && (_originalMode & WindowsConsole.EnableVirtualTerminalProcessing) == 0) {
                WindowsConsole.TrySetMode(_handle, _originalMode | WindowsConsole.EnableVirtualTerminalProcessing);
            }
        }

        /// <summary>Activates VT only for a writer targeting the console; recordings and redirected sinks leave it alone.</summary>
        /// <param name="writer">The actual output sink, independent of the cached profile.</param>
        /// <returns>A scope to dispose after the console write, or null for a non-console sink.</returns>
        internal static WindowsOutputModeScope? Activate(ITerminalWriter writer) {
            return writer.IsConsole ? new WindowsOutputModeScope(true) : null;
        }

        /// <inheritdoc />
        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) == 0 && _captured) {
                WindowsConsole.TrySetMode(_handle, _originalMode);
            }
        }
    }
}
