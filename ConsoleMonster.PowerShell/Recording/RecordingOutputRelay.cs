using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ConsoleMonster.PowerShell {
    /// <summary>Bounds native live output while the pipeline thread services input and cancellation.</summary>
    internal sealed class RecordingOutputRelay : IDisposable {
        private readonly BlockingCollection<string> _output = new BlockingCollection<string>(64);
        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private readonly object _gate = new object();
        private bool _disposed;
        internal CancellationToken Token => _stop.Token;
        internal void Receive(string text) {
            try { _output.Add(text, _stop.Token); }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        }
        internal bool Drain(Action<string> write) {
            // Each native read has at most 8192 bytes. Yield to input/lifetime checks after eight messages.
            for (int i = 0; i < 8 && _output.TryTake(out string? text); i++) { write(text); }
            return _output.Count > 0;
        }
        internal void Stop() {
            lock (_gate) { if (!_disposed) { _stop.Cancel(); } }
        }
        public void Dispose() {
            lock (_gate) {
                if (_disposed) { return; }
                _stop.Cancel();
                _output.Dispose();
                _stop.Dispose();
                _disposed = true;
            }
        }
    }
}
