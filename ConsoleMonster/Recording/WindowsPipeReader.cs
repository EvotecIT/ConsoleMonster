using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ConsoleMonster.Sessions {
    /// <summary>Owns the ConPTY output pipe, including cancellation of a read that outlives console shutdown.</summary>
    internal sealed class WindowsPipeReader : IDisposable {
        private readonly FileStream _stream;
        private readonly SafeFileHandle _handle;
        private volatile bool _cancelled;

        internal WindowsPipeReader(SafeFileHandle handle) {
            _handle = handle;
            // Drain cancellation must see every queued byte through PeekNamedPipe, without a hidden read buffer.
            _stream = new FileStream(handle, FileAccess.Read, 1);
        }

        internal int Read(byte[] bytes) {
            while (true) {
                int count = bytes.Length;
                if (_cancelled) {
                    if (!PeekNamedPipe(_handle, IntPtr.Zero, 0, IntPtr.Zero, out uint available, IntPtr.Zero)) {
                        int error = Marshal.GetLastWin32Error();
                        if (error == 109 || error == 232) { return 0; } // Broken pipe / no data: EOF.
                        throw new IOException("Reading the recorded terminal pipe failed.", new Win32Exception(error));
                    }
                    if (available == 0) { return 0; }
                    count = (int)Math.Min((uint)count, available);
                }
                try { return _stream.Read(bytes, 0, count); }
                catch (OperationCanceledException) when (_cancelled) {
                    // ERROR_OPERATION_ABORTED maps to OperationCanceledException on both Framework and modern .NET.
                    // A slow observer can also resume after cancellation; drain queued output before returning EOF.
                }
            }
        }

        // Closing a SafeFileHandle does not release a synchronous ReadFile holding a reference to it.
        // Keep the handle alive until the reader has joined, and cancel only this pipe's pending I/O.
        internal void Cancel() {
            _cancelled = true;
            CancelIoEx(_handle, IntPtr.Zero);
        }

        public void Dispose() => _stream.Dispose();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CancelIoEx(SafeFileHandle handle, IntPtr overlapped);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool PeekNamedPipe(SafeFileHandle pipe, IntPtr buffer, uint size, IntPtr read, out uint available, IntPtr left);
    }
}
