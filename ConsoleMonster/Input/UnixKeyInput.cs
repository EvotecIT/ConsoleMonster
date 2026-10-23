using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ConsoleMonster.Interop;

namespace ConsoleMonster.Input {
    /// <summary>Reads an exclusively leased Unix TTY through the shared VT decoder and restores its exact saved mode.</summary>
    internal sealed class UnixKeyInput : IDisposable {
        private readonly IntPtr _saved;
        private readonly int _descriptor;
        private readonly Decoder _utf8 = new UTF8Encoding(false).GetDecoder();
        private readonly byte[] _bytes = new byte[512];
        private readonly char[] _chars = new char[Encoding.UTF8.GetMaxCharCount(512)];
        private readonly List<InputEvent> _events = new List<InputEvent>();
        private VtInputDecoder _decoder = new VtInputDecoder();
        private readonly Stopwatch _quiet = new Stopwatch();
        private bool _active;
        private bool _disposed;

        private UnixKeyInput(int descriptor, IntPtr saved) { _descriptor = descriptor; _saved = saved; }

        internal static UnixKeyInput? TryOpen() {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { return null; }
            int descriptor = UnixInputNative.Duplicate(0);
            if (descriptor < 0) { return null; }
            IntPtr saved = Marshal.AllocHGlobal(UnixInputNative.AttributesCapacity);
            Marshal.Copy(new byte[UnixInputNative.AttributesCapacity], 0, saved, UnixInputNative.AttributesCapacity);
            if (UnixInputNative.GetAttributes(descriptor, saved) == 0) { return new UnixKeyInput(descriptor, saved); }
            Marshal.FreeHGlobal(saved);
            UnixInputNative.Close(descriptor);
            return null;
        }

        internal InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken) {
            if (_disposed) { throw new ObjectDisposedException(nameof(UnixKeyInput)); }
            if (!_active) { Activate(); }
            Stopwatch wait = Stopwatch.StartNew();
            bool polled = false;
            do {
                cancellationToken.ThrowIfCancellationRequested();
                if (_events.Count > 0) {
                    InputEvent next = _events[0];
                    _events.RemoveAt(0);
                    return next;
                }
                if (polled && wait.Elapsed >= timeout) { return null; }
                polled = true;
                if (UnixInputNative.Ready(_descriptor)) {
                    long count = UnixInputNative.Read(_descriptor, _bytes, new UIntPtr((uint)_bytes.Length)).ToInt64();
                    if (count == 0) { throw new InputClosedException(ClosedMessage()); }
                    if (count < 0) {
                        if (Marshal.GetLastWin32Error() == 4) { continue; }
                        throw new InputClosedException(ClosedMessage());
                    }
                    int length = _utf8.GetChars(_bytes, 0, (int)count, _chars, 0, false);
                    if (length > 0) { _decoder.Feed(new string(_chars, 0, length), _events); }
                    _quiet.Restart();
                    continue;
                }
                if (_decoder.HasPending && _quiet.ElapsedMilliseconds >= 50) {
                    _decoder.Flush(_events);
                    if (_events.Count > 0) { continue; }
                }
                if (wait.Elapsed >= timeout) { return null; }
                cancellationToken.WaitHandle.WaitOne(10);
            } while (true);
        }

        internal void Suspend() {
            if (_disposed) { throw new ObjectDisposedException(nameof(UnixKeyInput)); }
            if (_active) { Restore(); _active = false; }
        }

        internal void Resume() {
            if (_disposed) { throw new ObjectDisposedException(nameof(UnixKeyInput)); }
            _events.Clear();
            _decoder = new VtInputDecoder();
            _utf8.Reset();
            _quiet.Reset();
            Activate(true);
        }

        private void Activate(bool discardInput = false) {
            IntPtr raw = Marshal.AllocHGlobal(UnixInputNative.AttributesCapacity);
            try {
                byte[] attributes = new byte[UnixInputNative.AttributesCapacity];
                Marshal.Copy(_saved, attributes, 0, attributes.Length);
                Marshal.Copy(attributes, 0, raw, attributes.Length);
                UnixInputNative.MakeRaw(raw);
                // cfmakeraw also disables output processing. Keep output exactly as the caller configured it.
                int outputOffset = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 8 : 4;
                int outputSize = outputOffset;
                for (int i = 0; i < outputSize; i++) { Marshal.WriteByte(raw, outputOffset + i, attributes[outputOffset + i]); }
                // TCSAFLUSH is 2 on both supported ABIs: resume discards input typed while work owned the console.
                if (UnixInputNative.SetAttributes(_descriptor, discardInput ? 2 : 0, raw) != 0) { throw new InputClosedException(ClosedMessage()); }
                _active = true;
            } finally { Marshal.FreeHGlobal(raw); }
        }

        private static string ClosedMessage() => Localization.LabelText.Get("Input.Closed");

        private void Restore() {
            int result;
            do { result = UnixInputNative.SetAttributes(_descriptor, 0, _saved); }
            while (result != 0 && Marshal.GetLastWin32Error() == 4);
            if (result != 0) { throw new InputClosedException(ClosedMessage()); }
        }

        public void Dispose() {
            if (_disposed) { return; }
            _disposed = true;
            try {
                if (_active) {
                    Restore();
                }
            } finally {
                Marshal.FreeHGlobal(_saved);
                UnixInputNative.Close(_descriptor);
            }
        }
    }
}
