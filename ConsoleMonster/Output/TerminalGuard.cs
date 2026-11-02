using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Interop;

namespace ConsoleMonster.Output {

    /// <summary>Restores the screen and saved Windows output mode on disposal, termination or a second Ctrl+C.</summary>
    public sealed class TerminalGuard : IDisposable {
        private static readonly object ActiveGate = new object();
        private static readonly List<TerminalGuard> Active = new List<TerminalGuard>();

        static TerminalGuard() {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private readonly Action _restore;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly CancellationToken _cancellation;
        private readonly bool _handleCancelKey;
        private readonly WindowsOutputModeScope _outputMode;
        private int _restored;
        private int _cancelPresses;
        private int _disposed;

        /// <summary>Creates a guard, saving the output mode and hooking process termination events.</summary>
        /// <param name="restore">Restores the screen, cursor, attributes and host output routing.</param>
        /// <param name="handleCancelKey">Whether to intercept Ctrl+C.</param>
        public TerminalGuard(Action restore, bool handleCancelKey = true) {
            _restore = restore ?? throw new ArgumentNullException(nameof(restore));
            _cancellation = _cts.Token;
            _handleCancelKey = handleCancelKey;
            _outputMode = new WindowsOutputModeScope();
            if (handleCancelKey) {
                Console.CancelKeyPress += OnCancelKeyPress;
            }
            lock (ActiveGate) { Active.Add(this); }
        }

        /// <summary>Cancelled on the first Ctrl+C or when the guard is disposed.</summary>
        public CancellationToken Cancellation => _cancellation;

        /// <summary>Whether the restore callback and mode-restoration attempt have completed.</summary>
        public bool RestoreCompleted => Volatile.Read(ref _restored) == 2;

        /// <summary>Restores once; callback and native errors never escape this method.</summary>
        public void Restore() {
            if (Interlocked.CompareExchange(ref _restored, 1, 0) != 0) {
                return;
            }
            try {
                try {
                    _restore();
                } finally {
                    _outputMode.Dispose();
                }
            } catch (Exception) {
                // An exit handler must not cause a second failure.
            } finally {
                Volatile.Write(ref _restored, 2);
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) {
                return;
            }
            if (_handleCancelKey) {
                Console.CancelKeyPress -= OnCancelKeyPress;
            }
            Restore();
            lock (ActiveGate) { Active.Remove(this); }
            Cancel();
            _cts.Dispose();
        }

        // Returns the event's Cancel value. Kept separate from ConsoleCancelEventArgs so the
        // cancellation policy is also usable by a host's own signal handler.
        internal bool RequestCancellation() {
            if (Interlocked.Increment(ref _cancelPresses) == 1) {
                Cancel();
                return true;
            }
            RestoreAll();
            return false;
        }

        private void Cancel() {
            try {
                _cts.Cancel();
            } catch (Exception) {
                // Includes cancellation callbacks and a racing disposal.
            }
        }

        private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e) => e.Cancel = RequestCancellation();
        private static void RestoreAll() {
            TerminalGuard[] guards;
            lock (ActiveGate) { guards = Active.ToArray(); }
            // Native mode snapshots nest with their resources. The entry snapshot must be restored last.
            for (int i = guards.Length - 1; i >= 0; i--) { guards[i].Restore(); }
        }

        private static void OnProcessExit(object? sender, EventArgs e) => RestoreAll();
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) => RestoreAll();
    }
}
